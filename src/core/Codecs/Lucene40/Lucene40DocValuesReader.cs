using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LegacyDocValuesType = Lucene.Net.Codecs.Lucene40.Lucene40FieldInfosReader.LegacyDocValuesType;

namespace Lucene.Net.Codecs.Lucene40
{
    [Obsolete]
    internal sealed class Lucene40DocValuesReader : DocValuesProducer
    {
        private readonly Directory dir;
        private readonly SegmentReadState state;
        private readonly String legacyKey;
        private const String segmentSuffix = "dv";

        // ram instances we have already loaded
        private readonly IDictionary<int, NumericDocValues> numericInstances =
            new HashMap<int, NumericDocValues>();
        private readonly IDictionary<int, BinaryDocValues> binaryInstances =
            new HashMap<int, BinaryDocValues>();
        private readonly IDictionary<int, SortedDocValues> sortedInstances =
            new HashMap<int, SortedDocValues>();

        internal Lucene40DocValuesReader(SegmentReadState state, String filename, String legacyKey)
        {
            this.state = state;
            this.legacyKey = legacyKey;
            this.dir = new CompoundFileDirectory(state.directory, filename, state.context, false);
        }

        public override NumericDocValues GetNumeric(FieldInfo field)
        {
            lock (this)
            {
                NumericDocValues instance = numericInstances[field.number];
                if (instance == null)
                {
                    String fileName = IndexFileNames.SegmentFileName(state.segmentInfo.name + "_" + field.number.ToString(), segmentSuffix, "dat");
                    IndexInput input = dir.OpenInput(fileName, state.context);
                    bool success = false;
                    try
                    {
                        var type = LegacyDocValuesType.ValueOf(field.GetAttribute(legacyKey));
                        if (type == LegacyDocValuesType.VAR_INTS)
                        {
                            instance = LoadVarIntsField(field, input);
                        }
                        else if (type == LegacyDocValuesType.FIXED_INTS_8)
                        {
                            instance = LoadByteField(field, input);
                        }
                        else if (type == LegacyDocValuesType.FIXED_INTS_16)
                        {
                            instance = LoadShortField(field, input);
                        }
                        else if (type == LegacyDocValuesType.FIXED_INTS_32)
                        {
                            instance = LoadIntField(field, input);
                        }
                        else if (type == LegacyDocValuesType.FIXED_INTS_64)
                        {
                            instance = LoadLongField(field, input);
                        }
                        else if (type == LegacyDocValuesType.FLOAT_32)
                        {
                            instance = LoadFloatField(field, input);
                        }
                        else if (type == LegacyDocValuesType.FLOAT_64)
                        {
                            instance = LoadDoubleField(field, input);
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }

                        if (input.FilePointer != input.Length)
                        {
                            throw new CorruptIndexException("did not read all bytes from file \"" + fileName + "\": read " + input.FilePointer + " vs size " + input.Length + " (resource: " + input + ")");
                        }
                        success = true;
                    }
                    finally
                    {
                        if (success)
                        {
                            IOUtils.Close(input);
                        }
                        else
                        {
                            IOUtils.CloseWhileHandlingException((IDisposable)input);
                        }
                    }
                    numericInstances[field.number] = instance;
                }
                return instance;
            }
        }

        private NumericDocValues LoadVarIntsField(FieldInfo field, IndexInput input)
        {
            CodecUtil.CheckHeader(input, Lucene40DocValuesFormat.VAR_INTS_CODEC_NAME,
                                         Lucene40DocValuesFormat.VAR_INTS_VERSION_START,
                                         Lucene40DocValuesFormat.VAR_INTS_VERSION_CURRENT);
            byte header = input.ReadByte();
            if (header == Lucene40DocValuesFormat.VAR_INTS_FIXED_64)
            {
                int maxDoc = state.segmentInfo.DocCount;
                long[] values = new long[maxDoc];
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = input.ReadLong();
                }
                return new AnonymousVarIntsFixedNumericDocValues(values);
            }
            else if (header == Lucene40DocValuesFormat.VAR_INTS_PACKED)
            {
                long minValue = input.ReadLong();
                long defaultValue = input.ReadLong();
                PackedInts.IReader reader = PackedInts.GetReader(input);
                return new AnonymousVarIntsPackedNumericDocValues(reader, defaultValue, minValue);
            }
            else
            {
                throw new CorruptIndexException("invalid VAR_INTS header byte: " + header + " (resource=" + input + ")");
            }
        }

        private sealed class AnonymousVarIntsFixedNumericDocValues : NumericDocValues
        {
            private readonly long[] values;

            public AnonymousVarIntsFixedNumericDocValues(long[] values)
            {
                this.values = values;
            }

            public override long Get(int docID)
            {
                return values[docID];
            }
        }

        private sealed class AnonymousVarIntsPackedNumericDocValues : NumericDocValues
        {
            private readonly PackedInts.IReader reader;
            private readonly long defaultValue;
            private readonly long minValue;

            public AnonymousVarIntsPackedNumericDocValues(PackedInts.IReader reader, long defaultValue, long minValue)
            {
                this.reader = reader;
                this.defaultValue = defaultValue;
                this.minValue = minValue;
            }

            public override long Get(int docID)
            {
                long value = reader.Get(docID);
                if (value == defaultValue)
                    return 0;
                else
                    return minValue + value;
            }
        }

        private NumericDocValues LoadByteField(FieldInfo field, IndexInput input)
        {
            CodecUtil.CheckHeader(input, Lucene40DocValuesFormat.INTS_CODEC_NAME,
                                         Lucene40DocValuesFormat.INTS_VERSION_START,
                                         Lucene40DocValuesFormat.INTS_VERSION_CURRENT);
            int valueSize = input.ReadInt();
            if (valueSize != 1)
            {
                throw new CorruptIndexException("invalid valueSize: " + valueSize);
            }
            int maxDoc = state.segmentInfo.DocCount;
            byte[] values = new byte[maxDoc];
            input.ReadBytes(values, 0, values.Length);
            return new AnonymousByteNumericDocValues(values);
        }

        private class AnonymousByteNumericDocValues : NumericDocValues
        {
            private readonly byte[] values;

            public AnonymousByteNumericDocValues(byte[] values)
            {
                this.values = values;
            }

            public override long Get(int docID)
            {
                return values[docID];
            }
        }

        private NumericDocValues LoadShortField(FieldInfo field, IndexInput input)
        {
            CodecUtil.CheckHeader(input, Lucene40DocValuesFormat.INTS_CODEC_NAME,
                                         Lucene40DocValuesFormat.INTS_VERSION_START,
                                         Lucene40DocValuesFormat.INTS_VERSION_CURRENT);
            int valueSize = input.ReadInt();
            if (valueSize != 2)
            {
                throw new CorruptIndexException("invalid valueSize: " + valueSize);
            }
            int maxDoc = state.segmentInfo.DocCount;
            short[] values = new short[maxDoc];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = input.ReadShort();
            }
            return new AnonymousShortNumericDocValues(values);
        }

        private sealed class AnonymousShortNumericDocValues : NumericDocValues
        {
            private readonly short[] values;

            public AnonymousShortNumericDocValues(short[] values)
            {
                this.values = values;
            }

            public override long Get(int docID)
            {
                return values[docID];
            }
        }

        private NumericDocValues LoadIntField(FieldInfo field, IndexInput input)
        {
            CodecUtil.CheckHeader(input, Lucene40DocValuesFormat.INTS_CODEC_NAME,
                                         Lucene40DocValuesFormat.INTS_VERSION_START,
                                         Lucene40DocValuesFormat.INTS_VERSION_CURRENT);
            int valueSize = input.ReadInt();
            if (valueSize != 4)
            {
                throw new CorruptIndexException("invalid valueSize: " + valueSize);
            }
            int maxDoc = state.segmentInfo.DocCount;
            int[] values = new int[maxDoc];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = input.ReadInt();
            }
            return new AnonymousIntNumericDocValues(values);
        }

        private sealed class AnonymousIntNumericDocValues : NumericDocValues
        {
            private readonly int[] values;

            public AnonymousIntNumericDocValues(int[] values)
            {
                this.values = values;
            }

            public override long Get(int docID)
            {
                return values[docID];
            }
        }

        private NumericDocValues LoadLongField(FieldInfo field, IndexInput input)
        {
            CodecUtil.CheckHeader(input, Lucene40DocValuesFormat.INTS_CODEC_NAME,
                                         Lucene40DocValuesFormat.INTS_VERSION_START,
                                         Lucene40DocValuesFormat.INTS_VERSION_CURRENT);
            int valueSize = input.ReadInt();
            if (valueSize != 8)
            {
                throw new CorruptIndexException("invalid valueSize: " + valueSize);
            }
            int maxDoc = state.segmentInfo.DocCount;
            long[] values = new long[maxDoc];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = input.ReadLong();
            }
            return new AnonymousLongNumericDocValues(values);
        }

        private sealed class AnonymousLongNumericDocValues : NumericDocValues
        {
            private readonly long[] values;

            public AnonymousLongNumericDocValues(long[] values)
            {
                this.values = values;
            }

            public override long Get(int docID)
            {
                return values[docID];
            }
        }

        private NumericDocValues LoadFloatField(FieldInfo field, IndexInput input)
        {
            CodecUtil.CheckHeader(input, Lucene40DocValuesFormat.FLOATS_CODEC_NAME,
                                         Lucene40DocValuesFormat.FLOATS_VERSION_START,
                                         Lucene40DocValuesFormat.FLOATS_VERSION_CURRENT);
            int valueSize = input.ReadInt();
            if (valueSize != 4)
            {
                throw new CorruptIndexException("invalid valueSize: " + valueSize);
            }
            int maxDoc = state.segmentInfo.DocCount;
            int[] values = new int[maxDoc];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = input.ReadInt();
            }
            return new AnonymousIntNumericDocValues(values); // .NET Port: We can re-use the int type here since values is int[]
        }

        private NumericDocValues LoadDoubleField(FieldInfo field, IndexInput input)
        {
            CodecUtil.CheckHeader(input, Lucene40DocValuesFormat.FLOATS_CODEC_NAME,
                                         Lucene40DocValuesFormat.FLOATS_VERSION_START,
                                         Lucene40DocValuesFormat.FLOATS_VERSION_CURRENT);
            int valueSize = input.ReadInt();
            if (valueSize != 8)
            {
                throw new CorruptIndexException("invalid valueSize: " + valueSize);
            }
            int maxDoc = state.segmentInfo.DocCount;
            long[] values = new long[maxDoc];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = input.ReadLong();
            }
            return new AnonymousLongNumericDocValues(values); // .NET Port: We can re-use the long type here since values is long[]
        }

        public override BinaryDocValues GetBinary(FieldInfo field)
        {
            lock (this)
            {
                BinaryDocValues instance = binaryInstances[field.number];
                if (instance == null)
                {
                    var type = LegacyDocValuesType.ValueOf(field.GetAttribute(legacyKey));

                    if (type == LegacyDocValuesType.BYTES_FIXED_STRAIGHT)
                    {
                        instance = LoadBytesFixedStraight(field);
                    }
                    else if (type == LegacyDocValuesType.BYTES_VAR_STRAIGHT)
                    {
                        instance = LoadBytesVarStraight(field);
                    }
                    else if (type == LegacyDocValuesType.BYTES_FIXED_DEREF)
                    {
                        instance = LoadBytesFixedDeref(field);
                    }
                    else if (type == LegacyDocValuesType.BYTES_VAR_DEREF)
                    {
                        instance = LoadBytesVarDeref(field);
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                    binaryInstances[field.number] = instance;
                }
                return instance;
            }
        }

        private BinaryDocValues LoadBytesFixedStraight(FieldInfo field)
        {
            String fileName = IndexFileNames.SegmentFileName(state.segmentInfo.name + "_" + field.number.ToString(), segmentSuffix, "dat");
            IndexInput input = dir.OpenInput(fileName, state.context);
            bool success = false;
            try
            {
                CodecUtil.CheckHeader(input, Lucene40DocValuesFormat.BYTES_FIXED_STRAIGHT_CODEC_NAME,
                                             Lucene40DocValuesFormat.BYTES_FIXED_STRAIGHT_VERSION_START,
                                             Lucene40DocValuesFormat.BYTES_FIXED_STRAIGHT_VERSION_CURRENT);
                int fixedLength = input.ReadInt();
                PagedBytes bytes = new PagedBytes(16);
                bytes.Copy(input, fixedLength * (long)state.segmentInfo.DocCount);
                PagedBytes.Reader bytesReader = bytes.Freeze(true);
                if (input.FilePointer != input.Length)
                {
                    throw new CorruptIndexException("did not read all bytes from file \"" + fileName + "\": read " + input.FilePointer + " vs size " + input.Length + " (resource: " + input + ")");
                }
                success = true;
                return new AnonymousBytesFixedStraightBinaryDocValues(bytesReader, fixedLength);
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(input);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)input);
                }
            }
        }

        private sealed class AnonymousBytesFixedStraightBinaryDocValues : BinaryDocValues
        {
            private readonly PagedBytes.Reader bytesReader;
            private readonly int fixedLength;

            public AnonymousBytesFixedStraightBinaryDocValues(PagedBytes.Reader bytesReader, int fixedLength)
            {
                this.bytesReader = bytesReader;
                this.fixedLength = fixedLength;
            }

            public override void Get(int docID, BytesRef result)
            {
                bytesReader.FillSlice(result, fixedLength * (long)docID, fixedLength);
            }
        }

        private BinaryDocValues LoadBytesVarStraight(FieldInfo field)
        {
            String dataName = IndexFileNames.SegmentFileName(state.segmentInfo.name + "_" + field.number.ToString(), segmentSuffix, "dat");
            String indexName = IndexFileNames.SegmentFileName(state.segmentInfo.name + "_" + field.number.ToString(), segmentSuffix, "idx");
            IndexInput data = null;
            IndexInput index = null;
            bool success = false;
            try
            {
                data = dir.OpenInput(dataName, state.context);
                CodecUtil.CheckHeader(data, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_CODEC_NAME_DAT,
                                            Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_VERSION_START,
                                            Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_VERSION_CURRENT);
                index = dir.OpenInput(indexName, state.context);
                CodecUtil.CheckHeader(index, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_CODEC_NAME_IDX,
                                             Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_VERSION_START,
                                             Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_VERSION_CURRENT);
                long totalBytes = index.ReadVLong();
                PagedBytes bytes = new PagedBytes(16);
                bytes.Copy(data, totalBytes);
                PagedBytes.Reader bytesReader = bytes.Freeze(true);
                PackedInts.IReader reader = PackedInts.GetReader(index);
                if (data.FilePointer != data.Length)
                {
                    throw new CorruptIndexException("did not read all bytes from file \"" + dataName + "\": read " + data.FilePointer + " vs size " + data.Length + " (resource: " + data + ")");
                }
                if (index.FilePointer != index.Length)
                {
                    throw new CorruptIndexException("did not read all bytes from file \"" + indexName + "\": read " + index.FilePointer + " vs size " + index.Length + " (resource: " + index + ")");
                }
                success = true;
                return new AnonymousBytesVarStraightBinaryDocValues(reader, bytesReader);
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(data, index);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)data, index);
                }
            }
        }

        private sealed class AnonymousBytesVarStraightBinaryDocValues : BinaryDocValues
        {
            private readonly PackedInts.IReader reader;
            private readonly PagedBytes.Reader bytesReader;

            public AnonymousBytesVarStraightBinaryDocValues(PackedInts.IReader reader, PagedBytes.Reader bytesReader)
            {
                this.reader = reader;
                this.bytesReader = bytesReader;
            }

            public override void Get(int docID, BytesRef result)
            {
                long startAddress = reader.Get(docID);
                long endAddress = reader.Get(docID + 1);
                bytesReader.FillSlice(result, startAddress, (int)(endAddress - startAddress));
            }
        }

        private BinaryDocValues LoadBytesFixedDeref(FieldInfo field)
        {
            String dataName = IndexFileNames.SegmentFileName(state.segmentInfo.name + "_" + field.number.ToString(), segmentSuffix, "dat");
            String indexName = IndexFileNames.SegmentFileName(state.segmentInfo.name + "_" + field.number.ToString(), segmentSuffix, "idx");
            IndexInput data = null;
            IndexInput index = null;
            bool success = false;
            try
            {
                data = dir.OpenInput(dataName, state.context);
                CodecUtil.CheckHeader(data, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_CODEC_NAME_DAT,
                                            Lucene40DocValuesFormat.BYTES_FIXED_DEREF_VERSION_START,
                                            Lucene40DocValuesFormat.BYTES_FIXED_DEREF_VERSION_CURRENT);
                index = dir.OpenInput(indexName, state.context);
                CodecUtil.CheckHeader(index, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_CODEC_NAME_IDX,
                                             Lucene40DocValuesFormat.BYTES_FIXED_DEREF_VERSION_START,
                                             Lucene40DocValuesFormat.BYTES_FIXED_DEREF_VERSION_CURRENT);

                int fixedLength = data.ReadInt();
                int valueCount = index.ReadInt();
                PagedBytes bytes = new PagedBytes(16);
                bytes.Copy(data, fixedLength * (long)valueCount);
                PagedBytes.Reader bytesReader = bytes.Freeze(true);
                PackedInts.IReader reader = PackedInts.GetReader(index);
                if (data.FilePointer != data.Length)
                {
                    throw new CorruptIndexException("did not read all bytes from file \"" + dataName + "\": read " + data.FilePointer + " vs size " + data.Length + " (resource: " + data + ")");
                }
                if (index.FilePointer != index.Length)
                {
                    throw new CorruptIndexException("did not read all bytes from file \"" + indexName + "\": read " + index.FilePointer + " vs size " + index.Length + " (resource: " + index + ")");
                }
                success = true;
                return new AnonymousBytesFixedDerefBinaryDocValues(fixedLength, reader, bytesReader);
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(data, index);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)data, index);
                }
            }
        }

        private sealed class AnonymousBytesFixedDerefBinaryDocValues : BinaryDocValues
        {
            private readonly int fixedLength;
            private readonly PackedInts.IReader reader;
            private readonly PagedBytes.Reader bytesReader;

            public AnonymousBytesFixedDerefBinaryDocValues(int fixedLength, PackedInts.IReader reader, PagedBytes.Reader bytesReader)
            {
                this.fixedLength = fixedLength;
                this.reader = reader;
                this.bytesReader = bytesReader;
            }

            public override void Get(int docID, BytesRef result)
            {
                long offset = fixedLength * reader.Get(docID);
                bytesReader.FillSlice(result, offset, fixedLength);
            }
        }

        private BinaryDocValues LoadBytesVarDeref(FieldInfo field)
        {
            String dataName = IndexFileNames.SegmentFileName(state.segmentInfo.name + "_" + field.number.ToString(), segmentSuffix, "dat");
            String indexName = IndexFileNames.SegmentFileName(state.segmentInfo.name + "_" + field.number.ToString(), segmentSuffix, "idx");
            IndexInput data = null;
            IndexInput index = null;
            bool success = false;
            try
            {
                data = dir.OpenInput(dataName, state.context);
                CodecUtil.CheckHeader(data, Lucene40DocValuesFormat.BYTES_VAR_DEREF_CODEC_NAME_DAT,
                                            Lucene40DocValuesFormat.BYTES_VAR_DEREF_VERSION_START,
                                            Lucene40DocValuesFormat.BYTES_VAR_DEREF_VERSION_CURRENT);
                index = dir.OpenInput(indexName, state.context);
                CodecUtil.CheckHeader(index, Lucene40DocValuesFormat.BYTES_VAR_DEREF_CODEC_NAME_IDX,
                                             Lucene40DocValuesFormat.BYTES_VAR_DEREF_VERSION_START,
                                             Lucene40DocValuesFormat.BYTES_VAR_DEREF_VERSION_CURRENT);

                long totalBytes = index.ReadLong();
                PagedBytes bytes = new PagedBytes(16);
                bytes.Copy(data, totalBytes);
                PagedBytes.Reader bytesReader = bytes.Freeze(true);
                PackedInts.IReader reader = PackedInts.GetReader(index);
                if (data.FilePointer != data.Length)
                {
                    throw new CorruptIndexException("did not read all bytes from file \"" + dataName + "\": read " + data.FilePointer + " vs size " + data.Length + " (resource: " + data + ")");
                }
                if (index.FilePointer != index.Length)
                {
                    throw new CorruptIndexException("did not read all bytes from file \"" + indexName + "\": read " + index.FilePointer + " vs size " + index.Length + " (resource: " + index + ")");
                }
                success = true;
                return new AnonymousBytesVarDerefBinaryDocValues(reader, bytesReader);
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(data, index);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)data, index);
                }
            }
        }

        private sealed class AnonymousBytesVarDerefBinaryDocValues : BinaryDocValues
        {
            private readonly PackedInts.IReader reader;
            private readonly PagedBytes.Reader bytesReader;

            public AnonymousBytesVarDerefBinaryDocValues(PackedInts.IReader reader, PagedBytes.Reader bytesReader)
            {
                this.reader = reader;
                this.bytesReader = bytesReader;
            }

            public override void Get(int docID, BytesRef result)
            {
                long startAddress = reader.Get(docID);
                BytesRef lengthBytes = new BytesRef();
                bytesReader.FillSlice(lengthBytes, startAddress, 1);
                sbyte code = lengthBytes.bytes[lengthBytes.offset];
                if ((code & 128) == 0)
                {
                    // length is 1 byte
                    bytesReader.FillSlice(result, startAddress + 1, (int)code);
                }
                else
                {
                    bytesReader.FillSlice(lengthBytes, startAddress + 1, 1);
                    int length = ((code & 0x7f) << 8) | (lengthBytes.bytes[lengthBytes.offset] & 0xff);
                    bytesReader.FillSlice(result, startAddress + 2, length);
                }
            }
        }

        public override SortedDocValues GetSorted(FieldInfo field)
        {
            lock (this)
            {
                SortedDocValues instance = sortedInstances[field.number];
                if (instance == null)
                {
                    String dataName = IndexFileNames.SegmentFileName(state.segmentInfo.name + "_" + field.number.ToString(), segmentSuffix, "dat");
                    String indexName = IndexFileNames.SegmentFileName(state.segmentInfo.name + "_" + field.number.ToString(), segmentSuffix, "idx");
                    IndexInput data = null;
                    IndexInput index = null;
                    bool success = false;
                    try
                    {
                        data = dir.OpenInput(dataName, state.context);
                        index = dir.OpenInput(indexName, state.context);
                        var type = LegacyDocValuesType.ValueOf(field.GetAttribute(legacyKey));

                        if (type == LegacyDocValuesType.BYTES_FIXED_SORTED)
                        {
                            instance = LoadBytesFixedSorted(field, data, index);
                        }
                        else if (type == LegacyDocValuesType.BYTES_VAR_SORTED)
                        {
                            instance = LoadBytesVarSorted(field, data, index);
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }

                        if (data.FilePointer != data.Length)
                        {
                            throw new CorruptIndexException("did not read all bytes from file \"" + dataName + "\": read " + data.FilePointer + " vs size " + data.Length + " (resource: " + data + ")");
                        }
                        if (index.FilePointer != index.Length)
                        {
                            throw new CorruptIndexException("did not read all bytes from file \"" + indexName + "\": read " + index.FilePointer + " vs size " + index.Length + " (resource: " + index + ")");
                        }
                        success = true;
                    }
                    finally
                    {
                        if (success)
                        {
                            IOUtils.Close(data, index);
                        }
                        else
                        {
                            IOUtils.CloseWhileHandlingException((IDisposable)data, index);
                        }
                    }
                    sortedInstances[field.number] = instance;
                }
                return instance;
            }
        }

        private SortedDocValues LoadBytesFixedSorted(FieldInfo field, IndexInput data, IndexInput index)
        {
            CodecUtil.CheckHeader(data, Lucene40DocValuesFormat.BYTES_FIXED_SORTED_CODEC_NAME_DAT,
                                        Lucene40DocValuesFormat.BYTES_FIXED_SORTED_VERSION_START,
                                        Lucene40DocValuesFormat.BYTES_FIXED_SORTED_VERSION_CURRENT);
            CodecUtil.CheckHeader(index, Lucene40DocValuesFormat.BYTES_FIXED_SORTED_CODEC_NAME_IDX,
                                         Lucene40DocValuesFormat.BYTES_FIXED_SORTED_VERSION_START,
                                         Lucene40DocValuesFormat.BYTES_FIXED_SORTED_VERSION_CURRENT);

            int fixedLength = data.ReadInt();
            int valueCount = index.ReadInt();

            PagedBytes bytes = new PagedBytes(16);
            bytes.Copy(data, fixedLength * (long)valueCount);
            PagedBytes.Reader bytesReader = bytes.Freeze(true);
            PackedInts.IReader reader = PackedInts.GetReader(index);

            return CorrectBuggyOrds(new AnonymousBytesFixedSortedDocValues(reader, bytesReader, fixedLength, valueCount));
        }

        private sealed class AnonymousBytesFixedSortedDocValues : SortedDocValues
        {
            private readonly PackedInts.IReader reader;
            private readonly PagedBytes.Reader bytesReader;
            private readonly int fixedLength;
            private readonly int valueCount;

            public AnonymousBytesFixedSortedDocValues(PackedInts.IReader reader, PagedBytes.Reader bytesReader, int fixedLength, int valueCount)
            {
                this.reader = reader;
                this.bytesReader = bytesReader;
                this.fixedLength = fixedLength;
                this.valueCount = valueCount;
            }

            public override int GetOrd(int docID)
            {
                return (int)reader.Get(docID);
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                bytesReader.FillSlice(result, fixedLength * (long)ord, fixedLength);
            }

            public override int ValueCount
            {
                get { return valueCount; }
            }
        }

        private SortedDocValues LoadBytesVarSorted(FieldInfo field, IndexInput data, IndexInput index)
        {
            CodecUtil.CheckHeader(data, Lucene40DocValuesFormat.BYTES_VAR_SORTED_CODEC_NAME_DAT,
                                        Lucene40DocValuesFormat.BYTES_VAR_SORTED_VERSION_START,
                                        Lucene40DocValuesFormat.BYTES_VAR_SORTED_VERSION_CURRENT);
            CodecUtil.CheckHeader(index, Lucene40DocValuesFormat.BYTES_VAR_SORTED_CODEC_NAME_IDX,
                                         Lucene40DocValuesFormat.BYTES_VAR_SORTED_VERSION_START,
                                         Lucene40DocValuesFormat.BYTES_VAR_SORTED_VERSION_CURRENT);

            long maxAddress = index.ReadLong();
            PagedBytes bytes = new PagedBytes(16);
            bytes.Copy(data, maxAddress);
            PagedBytes.Reader bytesReader = bytes.Freeze(true);
            PackedInts.IReader addressReader = PackedInts.GetReader(index);
            PackedInts.IReader ordsReader = PackedInts.GetReader(index);

            int valueCount = addressReader.Size() - 1;

            return CorrectBuggyOrds(new AnonymousBytesVarSortedDocValues(bytesReader, addressReader, ordsReader, valueCount));
        }

        private sealed class AnonymousBytesVarSortedDocValues : SortedDocValues
        {
            private readonly PagedBytes.Reader bytesReader;
            private readonly PackedInts.IReader addressReader;
            private readonly PackedInts.IReader ordsReader;
            private readonly int valueCount;

            public AnonymousBytesVarSortedDocValues(PagedBytes.Reader bytesReader, PackedInts.IReader addressReader, PackedInts.IReader ordsReader, int valueCount)
            {
                this.bytesReader = bytesReader;
                this.addressReader = addressReader;
                this.ordsReader = ordsReader;
                this.valueCount = valueCount;
            }

            public override int GetOrd(int docID)
            {
                return (int)ordsReader.Get(docID);
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                long startAddress = addressReader.Get(ord);
                long endAddress = addressReader.Get(ord + 1);
                bytesReader.FillSlice(result, startAddress, (int)(endAddress - startAddress));
            }

            public override int ValueCount
            {
                get { return valueCount; }
            }
        }

        // detects and corrects LUCENE-4717 in old indexes
        private SortedDocValues CorrectBuggyOrds(SortedDocValues input)
        {
            int maxDoc = state.segmentInfo.DocCount;
            for (int i = 0; i < maxDoc; i++)
            {
                if (input.GetOrd(i) == 0)
                {
                    return input; // ok
                }
            }

            return new AnonymousCorrectBuggyOrdsSortedDocValues(input);
        }

        private sealed class AnonymousCorrectBuggyOrdsSortedDocValues : SortedDocValues
        {
            private readonly SortedDocValues input;

            public AnonymousCorrectBuggyOrdsSortedDocValues(SortedDocValues input)
            {
                this.input = input;
            }

            public override int GetOrd(int docID)
            {
                return input.GetOrd(docID) - 1;
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                input.LookupOrd(ord + 1, result);
            }

            public override int ValueCount
            {
                get { return input.ValueCount - 1; }
            }
        }

        public override SortedSetDocValues GetSortedSet(FieldInfo field)
        {
            throw new InvalidOperationException("Lucene 4.0 does not support SortedSet: how did you pull this off?");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                dir.Dispose();
            }
        }
    }
}
