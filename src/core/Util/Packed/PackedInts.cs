using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Store;
using Lucene.Net.Support;

namespace Lucene.Net.Util.Packed
{
    public class PackedInts
    {
        /**
         * At most 700% memory overhead, always select a direct implementation.
         */
        public const float FASTEST = 7f;

        /**
         * At most 50% memory overhead, always select a reasonably fast implementation.
         */
        public const float FAST = 0.5f;

        /**
         * At most 20% memory overhead.
         */
        public const float DEFAULT = 0.2f;

        /**
         * No memory overhead at all, but the returned implementation may be slow.
         */
        public const float COMPACT = 0f;

        /**
         * Default amount of memory to use for bulk operations.
         */
        public const int DEFAULT_BUFFER_SIZE = 1024; // 1K

        public const String CODEC_NAME = "PackedInts";
        public const int VERSION_START = 0; // PackedInts were long-aligned
        public const int VERSION_BYTE_ALIGNED = 1;
        public const int VERSION_CURRENT = VERSION_BYTE_ALIGNED;

        /**
         * Check the validity of a version number.
         */
        public static void CheckVersion(int version)
        {
            if (version < VERSION_START)
            {
                throw new ArgumentException("Version is too old, should be at least " + VERSION_START + " (got " +
                                            version + ")");
            }
            else if (version > VERSION_CURRENT)
            {
                throw new ArgumentException("Version is too new, should be at most " + VERSION_CURRENT + " (got " +
                                            version + ")");
            }
        }


        private sealed class PackedFormat : Format
        {
            public PackedFormat()
                : base(0)
            {
            }

            public override long ByteCount(int packedIntsVersion, int valueCount, int bitsPerValue)
            {
                if (packedIntsVersion < VERSION_BYTE_ALIGNED)
                {
                    return 8L * (long)Math.Ceiling((double)valueCount * bitsPerValue / 64);
                }
                return (long)Math.Ceiling((double)valueCount * bitsPerValue / 8);
            }
        }

        private sealed class PackedSingleBlockFormat : Format
        {
            public PackedSingleBlockFormat()
                : base(1)
            {
            }

            public override int LongCount(int packedIntsVersion, int valueCount, int bitsPerValue)
            {
                int valuesPerBlock = 64 / bitsPerValue;
                return (int)Math.Ceiling((double)valueCount / valuesPerBlock);
            }

            public override bool IsSupported(int bitsPerValue)
            {
                return Packed64SingleBlock.IsSupported(bitsPerValue);
            }

            public override float OverheadPerValue(int bitsPerValue)
            {
                int valuesPerBlock = 64 / bitsPerValue;

                int overhead = 64 % bitsPerValue;
                return (float)overhead / valuesPerBlock;
            }
        }

        public class Format
        {
            public static readonly Format PACKED = new PackedFormat();

            public static readonly Format PACKED_SINGLE_BLOCK = new PackedSingleBlockFormat();

            private static readonly Format[] values = new Format[] { PACKED, PACKED_SINGLE_BLOCK };

            public static IEnumerable<Format> Values()
            {
                return values;
            }

            public static Format ById(int id)
            {
                foreach (Format format in Format.Values())
                {
                    if (format.GetId() == id)
                    {
                        return format;
                    }
                }
                throw new ArgumentException("Unknown format id: " + id);
            }

            internal Format(int id)
            {
                this.id = id;
            }

            public int id;

            public int GetId()
            {
                return id;
            }

            public virtual long ByteCount(int packedIntsVersion, int valueCount, int bitsPerValue)
            {
                // assume long-aligned
                return 8L * LongCount(packedIntsVersion, valueCount, bitsPerValue);
            }

            public virtual int LongCount(int packedIntsVersion, int valueCount, int bitsPerValue)
            {
                long byteCount = ByteCount(packedIntsVersion, valueCount, bitsPerValue);

                if ((byteCount % 8) == 0)
                {
                    return (int)(byteCount / 8);
                }
                else
                {
                    return (int)(byteCount / 8 + 1);
                }
            }

            public virtual bool IsSupported(int bitsPerValue)
            {
                return bitsPerValue >= 1 && bitsPerValue <= 64;
            }

            public virtual float OverheadPerValue(int bitsPerValue)
            {
                return 0f;
            }

            public virtual float OverheadRatio(int bitsPerValue)
            {
                return OverheadPerValue(bitsPerValue) / bitsPerValue;
            }
        }

        public class FormatAndBits
        {
            private readonly Format format;
            private readonly int bitsPerValue;

            public FormatAndBits(Format format, int bitsPerValue)
            {
                this.format = format;
                this.bitsPerValue = bitsPerValue;
            }

            public Format Format
            {
                get { return format; }
            }

            public int BitsPerValue
            {
                get { return bitsPerValue; }
            }

            public override string ToString()
            {
                return "FormatAndBits(format=" + Format + " bitsPerValue=" + BitsPerValue + ")";
            }
        }

        public static FormatAndBits FastestFormatAndBits(int valueCount, int bitsPerValue, float acceptableOverheadRatio)
        {
            if (valueCount == -1)
            {
                valueCount = int.MaxValue;
            }

            acceptableOverheadRatio = Math.Max(COMPACT, acceptableOverheadRatio);
            acceptableOverheadRatio = Math.Min(FASTEST, acceptableOverheadRatio);
            float acceptableOverheadPerValue = acceptableOverheadRatio * bitsPerValue; // in bits

            int maxBitsPerValue = bitsPerValue + (int)acceptableOverheadPerValue;

            int actualBitsPerValue = -1;
            Format format = Format.PACKED;

            if (bitsPerValue <= 8 && maxBitsPerValue >= 8)
            {
                actualBitsPerValue = 8;
            }
            else if (bitsPerValue <= 16 && maxBitsPerValue >= 16)
            {
                actualBitsPerValue = 16;
            }
            else if (bitsPerValue <= 32 && maxBitsPerValue >= 32)
            {
                actualBitsPerValue = 32;
            }
            else if (bitsPerValue <= 64 && maxBitsPerValue >= 64)
            {
                actualBitsPerValue = 64;
            }
            else if (valueCount <= Packed8ThreeBlocks.MAX_SIZE && bitsPerValue <= 24 && maxBitsPerValue >= 24)
            {
                actualBitsPerValue = 24;
            }
            else if (valueCount <= Packed16ThreeBlocks.MAX_SIZE && bitsPerValue <= 48 && maxBitsPerValue >= 48)
            {
                actualBitsPerValue = 48;
            }
            else
            {
                for (int bpv = bitsPerValue; bpv <= maxBitsPerValue; ++bpv)
                {
                    if (Format.PACKED_SINGLE_BLOCK.IsSupported(bpv))
                    {
                        float overhead = Format.PACKED_SINGLE_BLOCK.OverheadPerValue(bpv);
                        float acceptableOverhead = acceptableOverheadPerValue + bitsPerValue - bpv;
                        if (overhead <= acceptableOverhead)
                        {
                            actualBitsPerValue = bpv;
                            format = Format.PACKED_SINGLE_BLOCK;
                            break;
                        }
                    }
                }
                if (actualBitsPerValue < 0)
                {
                    actualBitsPerValue = bitsPerValue;
                }
            }

            return new FormatAndBits(format, actualBitsPerValue);
        }

        public interface IDecoder
        {
            int LongBlockCount { get; }

            int LongValueCount { get; }

            int ByteBlockCount { get; }

            int ByteValueCount { get; }

            void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations);

            void Decode(sbyte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations);

            void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations);

            void Decode(sbyte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations);
        }

        public interface IEncoder
        {
            int LongBlockCount { get; }

            int LongValueCount { get; }

            int ByteBlockCount { get; }

            int ByteValueCount { get; }

            void Encode(long[] values, int valuesOffset, long[] blocks, int blocksOffset, int iterations);

            void Encode(long[] values, int valuesOffset, sbyte[] blocks, int blocksOffset, int iterations);

            void Encode(int[] values, int valuesOffset, long[] blocks, int blocksOffset, int iterations);

            void Encode(int[] values, int valuesOffset, sbyte[] blocks, int blocksOffset, int iterations);
        }

        public interface IReader
        {
            long Get(int index);

            int Get(int index, long[] arr, int off, int len);

            int GetBitsPerValue();

            int Size();

            long RamBytesUsed();

            Object GetArray();

            bool HasArray();
        }

        public interface IReaderIterator
        {
            long Next();

            LongsRef Next(int count);

            int GetBitsPerValue();

            int Size();

            int Ord();
        }

        internal abstract class ReaderIterator : IReaderIterator
        {
            protected readonly DataInput input;
            protected readonly int bitsPerValue;
            protected readonly int valueCount;

            protected ReaderIterator(int valueCount, int bitsPerValue, DataInput input)
            {
                this.input = input;
                this.bitsPerValue = bitsPerValue;
                this.valueCount = valueCount;
            }

            public virtual long Next()
            {
                LongsRef nextValues = Next(1);
                //assert nextValues.length > 0;
                long result = nextValues.longs[nextValues.offset];
                ++nextValues.offset;
                --nextValues.length;
                return result;
            }

            public abstract LongsRef Next(int count);

            public virtual int GetBitsPerValue()
            {
                return bitsPerValue;
            }

            public virtual int Size()
            {
                return valueCount;
            }

            public abstract int Ord();
        }

        public interface IMutable : IReader
        {
            void Set(int index, long value);

            int Set(int index, long[] arr, int off, int len);

            void Fill(int fromIndex, int toIndex, long val);

            void Clear();

            void Save(DataOutput output);
        }

        internal abstract class Reader : IReader
        {
            protected readonly int bitsPerValue;
            protected readonly int valueCount;

            protected Reader(int valueCount, int bitsPerValue)
            {
                this.bitsPerValue = bitsPerValue;
                //assert bitsPerValue > 0 && bitsPerValue <= 64 : "bitsPerValue=" + bitsPerValue;
                this.valueCount = valueCount;
            }

            public abstract long Get(int index);

            public virtual int GetBitsPerValue()
            {
                return bitsPerValue;
            }

            public virtual int Size()
            {
                return valueCount;
            }

            public abstract long RamBytesUsed();

            public virtual object GetArray()
            {
                return null;
            }

            public virtual bool HasArray()
            {
                return false;
            }

            public virtual int Get(int index, long[] arr, int off, int len)
            {
                //assert len > 0 : "len must be > 0 (got " + len + ")";
                //assert index >= 0 && index < valueCount;
                //assert off + len <= arr.length;

                int gets = Math.Min(valueCount - index, len);
                for (int i = index, o = off, end = index + gets; i < end; ++i, ++o)
                {
                    arr[o] = Get(i);
                }
                return gets;
            }
        }

        public abstract class Mutable : Reader, IMutable
        {
            protected Mutable(int valueCount, int bitsPerValue)
                : base(valueCount, bitsPerValue)
            {
            }

            public abstract void Set(int index, long value);

            public virtual int Set(int index, long[] arr, int off, int len)
            {
                //assert len > 0 : "len must be > 0 (got " + len + ")";
                //assert index >= 0 && index < valueCount;
                len = Math.Min(len, valueCount - index);
                //assert off + len <= arr.length;

                for (int i = index, o = off, end = index + len; i < end; ++i, ++o)
                {
                    Set(i, arr[o]);
                }
                return len;
            }

            public virtual void Fill(int fromIndex, int toIndex, long val)
            {
                //assert val <= maxValue(bitsPerValue);
                //assert fromIndex <= toIndex;
                for (int i = fromIndex; i < toIndex; ++i)
                {
                    Set(i, val);
                }
            }

            protected virtual Format Format
            {
                get { return Format.PACKED; }
            }

            public abstract void Clear();

            public virtual void Save(DataOutput output)
            {
                Writer writer = GetWriterNoHeader(output, Format,
                    valueCount, bitsPerValue, DEFAULT_BUFFER_SIZE);
                writer.WriteHeader();
                for (int i = 0; i < valueCount; ++i)
                {
                    writer.Add(Get(i));
                }
                writer.Finish();
            }
        }

        public sealed class NullReader : Reader
        {
            private readonly int valueCount;

            /** Sole constructor. */
            public NullReader(int valueCount)
                : base(valueCount, 0)
            {
                this.valueCount = valueCount;
            }

            public override long Get(int index)
            {
                return 0;
            }

            public override int Get(int index, long[] arr, int off, int len)
            {
                return 0;
            }

            public override int GetBitsPerValue()
            {
                return 0;
            }

            public override int Size()
            {
                return valueCount;
            }

            public override long RamBytesUsed()
            {
                return 0;
            }

            public override object GetArray()
            {
                return null;
            }

            public override bool HasArray()
            {
                return false;
            }
        }

        public abstract class Writer
        {
            protected readonly DataOutput output;
            protected readonly int valueCount;
            protected readonly int bitsPerValue;

            protected Writer(DataOutput output, int valueCount, int bitsPerValue)
            {
                //assert bitsPerValue <= 64;
                //assert valueCount >= 0 || valueCount == -1;
                this.output = output;
                this.valueCount = valueCount;
                this.bitsPerValue = bitsPerValue;
            }

            internal virtual void WriteHeader()
            {
                //assert valueCount != -1;
                CodecUtil.WriteHeader(output, CODEC_NAME, VERSION_CURRENT);
                output.WriteVInt(bitsPerValue);
                output.WriteVInt(valueCount);
                output.WriteVInt(Format.GetId());
            }

            protected abstract PackedInts.Format Format { get; }

            public abstract void Add(long v);

            public int BitsPerValue()
            {
                return bitsPerValue;
            }

            public abstract void Finish();

            public abstract int Ord();
        }

        public static IDecoder GetDecoder(Format format, int version, int bitsPerValue)
        {
            CheckVersion(version);
            return BulkOperation.Of(format, bitsPerValue);
        }

        public static IEncoder GetEncoder(Format format, int version, int bitsPerValue)
        {
            CheckVersion(version);
            return BulkOperation.Of(format, bitsPerValue);
        }

        public static IReader GetReaderNoHeader(DataInput input, Format format, int version,
            int valueCount, int bitsPerValue)
        {
            CheckVersion(version);
            if (format == Format.PACKED_SINGLE_BLOCK)
            {
                return Packed64SingleBlock.Create(input, valueCount, bitsPerValue);
            }
            else if (format == Format.PACKED)
            {
                switch (bitsPerValue)
                {
                    case 8:
                        return new Direct8(version, input, valueCount);
                    case 16:
                        return new Direct16(version, input, valueCount);
                    case 32:
                        return new Direct32(version, input, valueCount);
                    case 64:
                        return new Direct64(version, input, valueCount);
                    case 24:
                        if (valueCount <= Packed8ThreeBlocks.MAX_SIZE)
                        {
                            return new Packed8ThreeBlocks(version, input, valueCount);
                        }
                        break;
                    case 48:
                        if (valueCount <= Packed16ThreeBlocks.MAX_SIZE)
                        {
                            return new Packed16ThreeBlocks(version, input, valueCount);
                        }
                        break;
                }
                return new Packed64(version, input, valueCount, bitsPerValue);
            }
            else
            {
                throw new ArgumentException("Unknown Writer format: " + format);
            }
        }

        public static IReader GetReaderNoHeader(DataInput input, Header header)
        {
            return GetReaderNoHeader(input, header.Format, header.Version, header.ValueCount, header.BitsPerValue);
        }

        public static IReader GetReader(DataInput input)
        {
            int version = CodecUtil.CheckHeader(input, CODEC_NAME, VERSION_START, VERSION_CURRENT);
            int bitsPerValue = input.ReadVInt();
            //assert bitsPerValue > 0 && bitsPerValue <= 64: "bitsPerValue=" + bitsPerValue;
            int valueCount = input.ReadVInt();
            Format format = Format.ById(input.ReadVInt());

            return GetReaderNoHeader(input, format, version, valueCount, bitsPerValue);
        }

        public static IReaderIterator GetReaderIteratorNoHeader(DataInput input, Format format, int version,
            int valueCount, int bitsPerValue, int mem)
        {
            CheckVersion(version);
            return new PackedReaderIterator(format, version, valueCount, bitsPerValue, input, mem);
        }

        public static IReaderIterator GetReaderIterator(DataInput input, int mem)
        {
            int version = CodecUtil.CheckHeader(input, CODEC_NAME, VERSION_START, VERSION_CURRENT);
            int bitsPerValue = input.ReadVInt();
            //assert bitsPerValue > 0 && bitsPerValue <= 64: "bitsPerValue=" + bitsPerValue;
            int valueCount = input.ReadVInt();
            Format format = Format.ById(input.ReadVInt());
            return GetReaderIteratorNoHeader(input, format, version, valueCount, bitsPerValue, mem);
        }

        private sealed class AnonymousDirectPackedReaderNoHeader : DirectPackedReader
        {
            private readonly int bitsPerValue;
            private readonly int valueCount;
            private readonly IndexInput input;
            private readonly long endPointer;

            public AnonymousDirectPackedReaderNoHeader(int bitsPerValue, int valueCount, IndexInput input, long endPointer)
                : base(bitsPerValue, valueCount, input)
            {
                this.bitsPerValue = bitsPerValue;
                this.valueCount = valueCount;
                this.input = input;
                this.endPointer = endPointer;
            }

            public override long Get(int index)
            {
                long result = base.Get(index);
                if (index == valueCount - 1)
                {

                    input.Seek(endPointer);

                }
                return result;
            }
        }

        public static Reader GetDirectReaderNoHeader(IndexInput input, Format format,
            int version, int valueCount, int bitsPerValue)
        {
            CheckVersion(version);
            if (format == Format.PACKED)
            {
                long byteCount = format.ByteCount(version, valueCount, bitsPerValue);
                if (byteCount != format.ByteCount(VERSION_CURRENT, valueCount, bitsPerValue))
                {
                    //assert version == VERSION_START;
                    long endPointer = input.FilePointer + byteCount;
                    // Some consumers of direct readers assume that reading the last value
                    // will make the underlying IndexInput go to the end of the packed
                    // stream, but this is not true because packed ints storage used to be
                    // long-aligned and is now byte-aligned, hence this additional
                    // condition when reading the last value
                    return new AnonymousDirectPackedReaderNoHeader(bitsPerValue, valueCount, input, endPointer);
                }
                else
                {
                    return new DirectPackedReader(bitsPerValue, valueCount, input);
                }
            }
            else if (format == Format.PACKED_SINGLE_BLOCK)
            {

                return new DirectPacked64SingleBlockReader(bitsPerValue, valueCount, input);
            }
            else
            {
                throw new ArgumentException("Unknwown format: " + format);
            }
        }

        public static IReader GetDirectReaderNoHeader(IndexInput input, Header header)
        {
            return GetDirectReaderNoHeader(input, header.Format, header.Version, header.ValueCount, header.BitsPerValue);
        }

        public static IReader GetDirectReader(IndexInput input)
        {
            int version = CodecUtil.CheckHeader(input, CODEC_NAME, VERSION_START, VERSION_CURRENT);
            int bitsPerValue = input.ReadVInt();
            //assert bitsPerValue > 0 && bitsPerValue <= 64: "bitsPerValue=" + bitsPerValue;
            int valueCount = input.ReadVInt();
            Format format = Format.ById(input.ReadVInt());
            return GetDirectReaderNoHeader(input, format, version, valueCount, bitsPerValue);
        }

        public static IMutable GetMutable(int valueCount,
            int bitsPerValue, float acceptableOverheadRatio)
        {
            //assert valueCount >= 0;

            FormatAndBits formatAndBits = FastestFormatAndBits(valueCount, bitsPerValue, acceptableOverheadRatio);
            if (formatAndBits.Format == Format.PACKED_SINGLE_BLOCK)
            {
                return Packed64SingleBlock.Create(valueCount, formatAndBits.BitsPerValue);
            }
            else if (formatAndBits.Format == Format.PACKED)
            {
                switch (formatAndBits.BitsPerValue)
                {
                    case 8:
                        return new Direct8(valueCount);
                    case 16:
                        return new Direct16(valueCount);
                    case 32:
                        return new Direct32(valueCount);
                    case 64:
                        return new Direct64(valueCount);
                    case 24:
                        if (valueCount <= Packed8ThreeBlocks.MAX_SIZE)
                        {
                            return new Packed8ThreeBlocks(valueCount);
                        }
                        break;
                    case 48:
                        if (valueCount <= Packed16ThreeBlocks.MAX_SIZE)
                        {
                            return new Packed16ThreeBlocks(valueCount);
                        }
                        break;
                }
                return new Packed64(valueCount, formatAndBits.BitsPerValue);
            }
            else
                throw new ArgumentException();
        }

        public static Writer GetWriterNoHeader(
            DataOutput output, Format format, int valueCount, int bitsPerValue, int mem)
        {
            return new PackedWriter(format, output, valueCount, bitsPerValue, mem);
        }

        public static Writer GetWriter(DataOutput output,
            int valueCount, int bitsPerValue, float acceptableOverheadRatio)
        {
            //assert valueCount >= 0;

            FormatAndBits formatAndBits = FastestFormatAndBits(valueCount, bitsPerValue, acceptableOverheadRatio);
            Writer writer = GetWriterNoHeader(output, formatAndBits.Format, valueCount, formatAndBits.BitsPerValue, DEFAULT_BUFFER_SIZE);
            writer.WriteHeader();
            return writer;
        }

        public static int BitsRequired(long maxValue)
        {
            if (maxValue < 0)
            {
                throw new ArgumentException("maxValue must be non-negative (got: " + maxValue + ")");
            }
            return Math.Max(1, 64 - Number.NumberOfLeadingZeros(maxValue));
        }

        public static long MaxValue(int bitsPerValue)
        {
            return bitsPerValue == 64 ? long.MaxValue : ~(~0L << bitsPerValue);
        }

        public static void Copy(IReader src, int srcPos, IMutable dest, int destPos, int len, int mem)
        {
            //assert srcPos + len <= src.size();
            //assert destPos + len <= dest.size();
            int capacity = Number.URShift(mem, 3);
            if (capacity == 0)
            {
                for (int i = 0; i < len; ++i)
                {
                    dest.Set(destPos++, src.Get(srcPos++));
                }
            }
            else
            {
                // use bulk operations
                long[] buf = new long[Math.Min(capacity, len)];
                int remaining = 0;
                while (len > 0)
                {
                    int read = src.Get(srcPos, buf, remaining, Math.Min(len, buf.Length - remaining));
                    //assert read > 0;
                    srcPos += read;
                    len -= read;
                    remaining += read;
                    int written = dest.Set(destPos, buf, 0, remaining);
                    //assert written > 0;
                    destPos += written;
                    if (written < remaining)
                    {
                        Array.Copy(buf, written, buf, 0, remaining - written);
                    }
                    remaining -= written;
                }
                while (remaining > 0)
                {
                    int written = dest.Set(destPos, buf, 0, remaining);
                    destPos += written;
                    remaining -= written;
                    Array.Copy(buf, written, buf, 0, remaining);
                }
            }
        }

        public static Header ReadHeader(DataInput input)
        {
            int version = CodecUtil.CheckHeader(input, CODEC_NAME, VERSION_START, VERSION_CURRENT);
            int bitsPerValue = input.ReadVInt();
            //assert bitsPerValue > 0 && bitsPerValue <= 64: "bitsPerValue=" + bitsPerValue;
            int valueCount = input.ReadVInt();
            Format format = Format.ById(input.ReadVInt());
            return new Header(format, valueCount, bitsPerValue, version);
        }

        public class Header
        {
            private readonly Format format;
            private readonly int valueCount;
            private readonly int bitsPerValue;
            private readonly int version;

            public Header(Format format, int valueCount, int bitsPerValue, int version)
            {
                this.format = format;
                this.valueCount = valueCount;
                this.bitsPerValue = bitsPerValue;
                this.version = version;
            }

            public Format Format
            {
                get { return format; }
            }

            public int ValueCount
            {
                get { return valueCount; }
            }

            public int BitsPerValue
            {
                get { return bitsPerValue; }
            }

            public int Version
            {
                get { return version; }
            }
        }
    }
}