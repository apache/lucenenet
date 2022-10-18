using J2N.Threading.Atomic;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using Lucene.Net.Util.Packed;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Int64 = J2N.Numerics.Int64;

namespace Lucene.Net.Codecs.Memory
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    using Util = Lucene.Net.Util.Fst.Util;

    /// <summary>
    /// TextReader for <see cref="MemoryDocValuesFormat"/>.
    /// </summary>
    internal class MemoryDocValuesProducer : DocValuesProducer
    {
        // metadata maps (just file pointers and minimal stuff)
        private readonly IDictionary<int, NumericEntry> numerics;
        private readonly IDictionary<int, BinaryEntry> binaries;
        private readonly IDictionary<int, FSTEntry> fsts;
        private readonly IndexInput data;

        // ram instances we have already loaded
        private readonly IDictionary<int, NumericDocValues> numericInstances = new Dictionary<int, NumericDocValues>();
        private readonly IDictionary<int, BinaryDocValues> binaryInstances = new Dictionary<int, BinaryDocValues>();
        private readonly IDictionary<int, FST<Int64>> fstInstances = new Dictionary<int, FST<Int64>>();
        private readonly IDictionary<int, IBits> docsWithFieldInstances = new Dictionary<int, IBits>();

        private readonly int maxDoc;
        private readonly AtomicInt64 ramBytesUsed;
        private readonly int version;

        internal const byte NUMBER = 0;
        internal const byte BYTES = 1;
        internal const byte FST = 2;
        
        internal const int BLOCK_SIZE = 4096;

        internal const byte DELTA_COMPRESSED = 0;
        internal const byte TABLE_COMPRESSED = 1;
        internal const byte UNCOMPRESSED = 2;
        internal const byte GCD_COMPRESSED = 3;

        internal const int VERSION_START = 0;
        internal const int VERSION_GCD_COMPRESSION = 1;
        internal const int VERSION_CHECKSUM = 2;
        internal const int VERSION_CURRENT = VERSION_CHECKSUM;


        internal MemoryDocValuesProducer(SegmentReadState state, string dataCodec, string dataExtension,
            string metaCodec, string metaExtension)
        {
            maxDoc = state.SegmentInfo.DocCount;
            var metaName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, metaExtension);
            // read in the entries from the metadata file.
            var @in = state.Directory.OpenChecksumInput(metaName, state.Context);
            bool success = false;
            try
            {
                version = CodecUtil.CheckHeader(@in, metaCodec, VERSION_START, VERSION_CURRENT);
                numerics = new Dictionary<int, NumericEntry>();
                binaries = new Dictionary<int, BinaryEntry>();
                fsts = new Dictionary<int, FSTEntry>();
                ReadFields(@in /*, state.FieldInfos // LUCENENET: Not referenced */);
                if (version >= VERSION_CHECKSUM)
                {
                    CodecUtil.CheckFooter(@in);
                }
                else
                {
#pragma warning disable 612, 618
                    CodecUtil.CheckEOF(@in);
#pragma warning restore 612, 618
                }
                ramBytesUsed = new AtomicInt64(RamUsageEstimator.ShallowSizeOfInstance(this.GetType()));
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Dispose(@in);
                }
                else
                {
                    IOUtils.DisposeWhileHandlingException(@in);
                }
            }

            success = false;
            try
            {
                string dataName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix,
                    dataExtension);
                data = state.Directory.OpenInput(dataName, state.Context);

                int version2 = CodecUtil.CheckHeader(data, dataCodec, VERSION_START, VERSION_CURRENT);
                if (version != version2)
                {
                    throw new CorruptIndexException("Format versions mismatch");
                }

                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(this.data);
                }
            }
        }

        private void ReadFields(IndexInput meta /*, FieldInfos infos // LUCENENET: Not referenced */)
        {
            int fieldNumber = meta.ReadVInt32();
            while (fieldNumber != -1)
            {
                int fieldType = meta.ReadByte();
                if (fieldType == NUMBER)
                {
                    var entry = new NumericEntry {offset = meta.ReadInt64(), missingOffset = meta.ReadInt64()};
                    if (entry.missingOffset != -1)
                    {
                        entry.missingBytes = meta.ReadInt64();
                    }
                    else
                    {
                        entry.missingBytes = 0;
                    }
                    entry.format = meta.ReadByte();
                    switch (entry.format)
                    {
                        case DELTA_COMPRESSED:
                        case TABLE_COMPRESSED:
                        case GCD_COMPRESSED:
                        case UNCOMPRESSED:
                            break;
                        default:
                            throw new CorruptIndexException("Unknown format: " + entry.format + ", input=" + meta);
                    }
                    if (entry.format != UNCOMPRESSED)
                    {
                        entry.packedIntsVersion = meta.ReadVInt32();
                    }
                    numerics[fieldNumber] = entry;
                }
                else if (fieldType == BYTES)
                {
                    var entry = new BinaryEntry
                    {
                        offset = meta.ReadInt64(),
                        numBytes = meta.ReadInt64(),
                        missingOffset = meta.ReadInt64()
                    };
                    if (entry.missingOffset != -1)
                    {
                        entry.missingBytes = meta.ReadInt64();
                    }
                    else
                    {
                        entry.missingBytes = 0;
                    }
                    entry.minLength = meta.ReadVInt32();
                    entry.maxLength = meta.ReadVInt32();
                    if (entry.minLength != entry.maxLength)
                    {
                        entry.packedIntsVersion = meta.ReadVInt32();
                        entry.blockSize = meta.ReadVInt32();
                    }
                    binaries[fieldNumber] = entry;
                }
                else if (fieldType == FST)
                {
                    var entry = new FSTEntry {offset = meta.ReadInt64(), numOrds = meta.ReadVInt64()};
                    fsts[fieldNumber] = entry;
                }
                else
                {
                    throw new CorruptIndexException("invalid entry type: " + fieldType + ", input=" + meta);
                }
                fieldNumber = meta.ReadVInt32();
            }
        }

        public override NumericDocValues GetNumeric(FieldInfo field)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (!numericInstances.TryGetValue(field.Number, out NumericDocValues instance))
                {
                    instance = LoadNumeric(field);
                    numericInstances[field.Number] = instance;
                }
                return instance;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public override long RamBytesUsed() => ramBytesUsed;

        public override void CheckIntegrity()
        {
            if (version >= VERSION_CHECKSUM)
            {
                CodecUtil.ChecksumEntireFile(data);
            }
        }

        private NumericDocValues LoadNumeric(FieldInfo field)
        {
            NumericEntry entry = numerics[field.Number];
            data.Seek(entry.offset + entry.missingBytes);
            switch (entry.format)
            {
                case TABLE_COMPRESSED:
                    int size = data.ReadVInt32();
                    if (size > 256)
                    {
                        throw new CorruptIndexException(
                            "TABLE_COMPRESSED cannot have more than 256 distinct values, input=" + data);
                    }
                    var decode = new long[size];
                    for (int i = 0; i < decode.Length; i++)
                    {
                        decode[i] = data.ReadInt64();
                    }
                    int formatID = data.ReadVInt32();
                    int bitsPerValue = data.ReadVInt32();
                    var ordsReader = PackedInt32s.GetReaderNoHeader(data, PackedInt32s.Format.ById(formatID),
                        entry.packedIntsVersion, maxDoc, bitsPerValue);
                    ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(decode) + ordsReader.RamBytesUsed());
                    return new NumericDocValuesAnonymousClass(decode, ordsReader);
                case DELTA_COMPRESSED:
                    int blockSize = data.ReadVInt32();
                    var reader = new BlockPackedReader(data, entry.packedIntsVersion, blockSize, maxDoc,
                        false);
                    ramBytesUsed.AddAndGet(reader.RamBytesUsed());
                    return reader;
                case UNCOMPRESSED:
                    var bytes = new byte[maxDoc];
                    data.ReadBytes(bytes, 0, bytes.Length);
                    ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(bytes));
                    // LUCENENET: IMPORTANT - some bytes are negative here, so we need to pass as sbyte
                    return new NumericDocValuesAnonymousClass2((sbyte[])(Array)bytes);
                case GCD_COMPRESSED:
                    long min = data.ReadInt64();
                    long mult = data.ReadInt64();
                    int quotientBlockSize = data.ReadVInt32();
                    var quotientReader = new BlockPackedReader(data, entry.packedIntsVersion,
                        quotientBlockSize, maxDoc, false);
                    ramBytesUsed.AddAndGet(quotientReader.RamBytesUsed());
                    return new NumericDocValuesAnonymousClass3(min, mult, quotientReader);
                default:
                    throw AssertionError.Create();
            }
        }

        private sealed class NumericDocValuesAnonymousClass : NumericDocValues
        {
            private readonly long[] decode;
            private readonly PackedInt32s.Reader ordsReader;

            public NumericDocValuesAnonymousClass(long[] decode, PackedInt32s.Reader ordsReader)
            {
                this.decode = decode;
                this.ordsReader = ordsReader;
            }

            public override long Get(int docID)
            {
                return decode[(int) ordsReader.Get(docID)];
            }
        }

        private sealed class NumericDocValuesAnonymousClass2 : NumericDocValues
        {
            private readonly sbyte[] bytes;

            public NumericDocValuesAnonymousClass2(sbyte[] bytes)
            {
                this.bytes = bytes;
            }

            public override long Get(int docID)
            {
                return bytes[docID];
            }
        }

        private sealed class NumericDocValuesAnonymousClass3 : NumericDocValues
        {
            private readonly long min;
            private readonly long mult;
            private readonly BlockPackedReader quotientReader;

            public NumericDocValuesAnonymousClass3(long min, long mult,
                BlockPackedReader quotientReader)
            {
                this.min = min;
                this.mult = mult;
                this.quotientReader = quotientReader;
            }

            public override long Get(int docID)
            {
                return min + mult*quotientReader.Get(docID);
            }
        }

        public override BinaryDocValues GetBinary(FieldInfo field)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (!binaryInstances.TryGetValue(field.Number, out BinaryDocValues instance))
                {
                    instance = LoadBinary(field);
                    binaryInstances[field.Number] = instance;
                }
                return instance;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        private BinaryDocValues LoadBinary(FieldInfo field)
        {
            BinaryEntry entry = binaries[field.Number];
            data.Seek(entry.offset);
            var bytes = new PagedBytes(16);
            bytes.Copy(data, entry.numBytes);
            var bytesReader = bytes.Freeze(true);
            if (entry.minLength == entry.maxLength)
            {
                int fixedLength = entry.minLength;
                ramBytesUsed.AddAndGet(bytes.RamBytesUsed());
                return new BinaryDocValuesAnonymousClass(bytesReader, fixedLength);
            }
            else
            {
                data.Seek(data.Position + entry.missingBytes); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                var addresses = new MonotonicBlockPackedReader(data, entry.packedIntsVersion,
                    entry.blockSize, maxDoc, false);
                ramBytesUsed.AddAndGet(bytes.RamBytesUsed() + addresses.RamBytesUsed());
                return new BinaryDocValuesAnonymousClass2(bytesReader, addresses);
            }
        }

        private sealed class BinaryDocValuesAnonymousClass : BinaryDocValues
        {
            private readonly PagedBytes.Reader bytesReader;
            private readonly int fixedLength;

            public BinaryDocValuesAnonymousClass(PagedBytes.Reader bytesReader, int fixedLength)
            {
                this.bytesReader = bytesReader;
                this.fixedLength = fixedLength;
            }

            public override void Get(int docID, BytesRef result)
            {
                bytesReader.FillSlice(result, fixedLength*(long) docID, fixedLength);
            }
        }

        private sealed class BinaryDocValuesAnonymousClass2 : BinaryDocValues
        {
            private readonly PagedBytes.Reader bytesReader;
            private readonly MonotonicBlockPackedReader addresses;

            public BinaryDocValuesAnonymousClass2(PagedBytes.Reader bytesReader, MonotonicBlockPackedReader addresses)
            {
                this.bytesReader = bytesReader;
                this.addresses = addresses;
            }

            public override void Get(int docID, BytesRef result)
            {
                var startAddress = docID == 0 ? 0 : addresses.Get(docID - 1);
                var endAddress = addresses.Get(docID);
                bytesReader.FillSlice(result, startAddress, (int) (endAddress - startAddress));
            }
        }

        public override SortedDocValues GetSorted(FieldInfo field)
        {
            FSTEntry entry = fsts[field.Number];
            if (entry.numOrds == 0)
            {
                return DocValues.EMPTY_SORTED;
            }
            FST<Int64> instance;
            UninterruptableMonitor.Enter(this);
            try
            {
                if (!fstInstances.TryGetValue(field.Number, out instance))
                {
                    data.Seek(entry.offset);
                    instance = new FST<Int64>(data, PositiveInt32Outputs.Singleton);
                    ramBytesUsed.AddAndGet(instance.GetSizeInBytes());
                    fstInstances[field.Number] = instance;
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
            var docToOrd = GetNumeric(field);
            var fst = instance;

            // per-thread resources
            var @in = fst.GetBytesReader();
            var firstArc = new FST.Arc<Int64>();
            var scratchArc = new FST.Arc<Int64>();
            var scratchInts = new Int32sRef();
            var fstEnum = new BytesRefFSTEnum<Int64>(fst);

            return new SortedDocValuesAnonymousClass(entry, docToOrd, fst, @in, firstArc, scratchArc,
                scratchInts, fstEnum);
        }

        private sealed class SortedDocValuesAnonymousClass : SortedDocValues
        {
            private readonly MemoryDocValuesProducer.FSTEntry entry;
            private readonly NumericDocValues docToOrd;
            private readonly FST<Int64> fst;
            private readonly FST.BytesReader @in;
            private readonly FST.Arc<Int64> firstArc;
            private readonly FST.Arc<Int64> scratchArc;
            private readonly Int32sRef scratchInts;
            private readonly BytesRefFSTEnum<Int64> fstEnum;

            public SortedDocValuesAnonymousClass(FSTEntry fstEntry,
                NumericDocValues numericDocValues, FST<Int64> fst1, FST.BytesReader @in, FST.Arc<Int64> arc, FST.Arc<Int64> scratchArc1,
                Int32sRef intsRef, BytesRefFSTEnum<Int64> bytesRefFstEnum)
            {
                entry = fstEntry;
                docToOrd = numericDocValues;
                fst = fst1;
                this.@in = @in;
                firstArc = arc;
                scratchArc = scratchArc1;
                scratchInts = intsRef;
                fstEnum = bytesRefFstEnum;
            }

            public override int GetOrd(int docID)
            {
                return (int) docToOrd.Get(docID);
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                try
                {
                    @in.Position = 0;
                    fst.GetFirstArc(firstArc);
                    Int32sRef output = Util.GetByOutput(fst, ord, @in, firstArc, scratchArc, scratchInts);
                    result.Bytes = new byte[output.Length];
                    result.Offset = 0;
                    result.Length = 0;
                    Util.ToBytesRef(output, result);
                }
                catch (Exception bogus) when (bogus.IsIOException())
                {
                    throw RuntimeException.Create(bogus);
                }
            }

            public override int LookupTerm(BytesRef key)
            {
                try
                {
                    var o = fstEnum.SeekCeil(key);
                    if (o is null)
                    {
                        return -ValueCount - 1;
                    }
                    else if (o.Input.Equals(key))
                    {
                        return (int) o.Output;
                    }
                    else
                    {
                        return (int) -o.Output - 1;
                    }
                }
                catch (Exception bogus) when (bogus.IsIOException())
                {
                    throw RuntimeException.Create(bogus);
                }
            }

            public override int ValueCount => (int) entry.numOrds;

            public override TermsEnum GetTermsEnum()
            {
                return new FSTTermsEnum(fst);
            }
        }

        public override SortedSetDocValues GetSortedSet(FieldInfo field)
        {
            var entry = fsts[field.Number];
            if (entry.numOrds == 0)
            {
                return DocValues.EMPTY_SORTED_SET; // empty FST!
            }
            FST<Int64> instance;
            UninterruptableMonitor.Enter(this);
            try
            {
                if (!fstInstances.TryGetValue(field.Number, out instance))
                {
                    data.Seek(entry.offset);
                    instance = new FST<Int64>(data, PositiveInt32Outputs.Singleton);
                    ramBytesUsed.AddAndGet(instance.GetSizeInBytes());
                    fstInstances[field.Number] = instance;
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
            var docToOrds = GetBinary(field);
            var fst = instance;

            // per-thread resources
            var @in = fst.GetBytesReader();
            var firstArc = new FST.Arc<Int64>();
            var scratchArc = new FST.Arc<Int64>();
            var scratchInts = new Int32sRef();
            var fstEnum = new BytesRefFSTEnum<Int64>(fst);
            var @ref = new BytesRef();
            var input = new ByteArrayDataInput();
            return new SortedSetDocValuesAnonymousClass(entry, docToOrds, fst, @in, firstArc,
                scratchArc, scratchInts, fstEnum, @ref, input);
        }

        private sealed class SortedSetDocValuesAnonymousClass : SortedSetDocValues
        {
            private readonly MemoryDocValuesProducer.FSTEntry entry;
            private readonly BinaryDocValues docToOrds;
            private readonly FST<Int64> fst;
            private readonly FST.BytesReader @in;
            private readonly FST.Arc<Int64> firstArc;
            private readonly FST.Arc<Int64> scratchArc;
            private readonly Int32sRef scratchInts;
            private readonly BytesRefFSTEnum<Int64> fstEnum;
            private readonly BytesRef @ref;
            private readonly ByteArrayDataInput input;

            private long currentOrd;

            public SortedSetDocValuesAnonymousClass(FSTEntry fstEntry, BinaryDocValues binaryDocValues, FST<Int64> fst1,
                FST.BytesReader @in, FST.Arc<Int64> arc, FST.Arc<Int64> scratchArc1, Int32sRef intsRef, BytesRefFSTEnum<Int64> bytesRefFstEnum,
                BytesRef @ref, ByteArrayDataInput byteArrayDataInput)
            {
                entry = fstEntry;
                docToOrds = binaryDocValues;
                fst = fst1;
                this.@in = @in;
                firstArc = arc;
                scratchArc = scratchArc1;
                scratchInts = intsRef;
                fstEnum = bytesRefFstEnum;
                this.@ref = @ref;
                input = byteArrayDataInput;
            }

            public override long NextOrd()
            {
                if (input.Eof)
                {
                    return NO_MORE_ORDS;
                }
                else
                {
                    currentOrd += input.ReadVInt64();
                    return currentOrd;
                }
            }

            public override void SetDocument(int docID)
            {
                docToOrds.Get(docID, @ref);
                input.Reset(@ref.Bytes, @ref.Offset, @ref.Length);
                currentOrd = 0;
            }

            public override void LookupOrd(long ord, BytesRef result)
            {
                try
                {
                    @in.Position = 0;
                    fst.GetFirstArc(firstArc);
                    Int32sRef output = Util.GetByOutput(fst, ord, @in, firstArc, scratchArc, scratchInts);
                    result.Bytes = new byte[output.Length];
                    result.Offset = 0;
                    result.Length = 0;
                    Util.ToBytesRef(output, result);
                }
                catch (Exception bogus) when (bogus.IsIOException())
                {
                    throw RuntimeException.Create(bogus);
                }
            }

            public override long LookupTerm(BytesRef key)
            {
                try
                {
                    var o = fstEnum.SeekCeil(key);
                    if (o is null)
                    {
                        return -ValueCount - 1;
                    }
                    else if (o.Input.Equals(key))
                    {
                        return o.Output;
                    }
                    else
                    {
                        return -o.Output - 1;
                    }
                }
                catch (Exception bogus) when (bogus.IsIOException())
                {
                    throw RuntimeException.Create(bogus);
                }
            }

            public override long ValueCount => entry.numOrds;

            public override TermsEnum GetTermsEnum()
            {
                return new FSTTermsEnum(fst);
            }
        }

        private IBits GetMissingBits(int fieldNumber, long offset, long length)
        {
            if (offset == -1)
            {
                return new Bits.MatchAllBits(maxDoc);
            }
            else
            {
                IBits instance;
                UninterruptableMonitor.Enter(this);
                try
                {
                    if (!docsWithFieldInstances.TryGetValue(fieldNumber, out instance))
                    {
                        var data = (IndexInput)this.data.Clone();
                        data.Seek(offset);
                        if (Debugging.AssertsEnabled) Debugging.Assert(length % 8 == 0);
                        var bits = new long[(int) length >> 3];
                        for (var i = 0; i < bits.Length; i++)
                        {
                            bits[i] = data.ReadInt64();
                        }
                        instance = new FixedBitSet(bits, maxDoc);
                        docsWithFieldInstances[fieldNumber] = instance;
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
                return instance;
            }
        }

        public override IBits GetDocsWithField(FieldInfo field)
        {
            switch (field.DocValuesType)
            {
                case DocValuesType.SORTED_SET:
                    return DocValues.DocsWithValue(GetSortedSet(field), maxDoc);
                case DocValuesType.SORTED:
                    return DocValues.DocsWithValue(GetSorted(field), maxDoc);
                case DocValuesType.BINARY:
                    var be = binaries[field.Number];
                    return GetMissingBits(field.Number, be.missingOffset, be.missingBytes);
                case DocValuesType.NUMERIC:
                    var ne = numerics[field.Number];
                    return GetMissingBits(field.Number, ne.missingOffset, ne.missingBytes);
                default:
                    throw AssertionError.Create();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                data.Dispose();
            }
        }

        internal class NumericEntry
        {
            internal long offset;
            internal long missingOffset;
            internal long missingBytes;
            internal byte format;
            internal int packedIntsVersion;
        }

        internal class BinaryEntry
        {
            internal long offset;
            internal long missingOffset;
            internal long missingBytes;
            internal long numBytes;
            internal int minLength;
            internal int maxLength;
            internal int packedIntsVersion;
            internal int blockSize;
        }

        internal class FSTEntry
        {
            internal long offset;
            internal long numOrds;
        }

        // exposes FSTEnum directly as a TermsEnum: avoids binary-search next()
        internal class FSTTermsEnum : TermsEnum
        {
            private readonly BytesRefFSTEnum<Int64> input;

            // this is all for the complicated seek(ord)...
            // maybe we should add a FSTEnum that supports this operation?
            private readonly FST<Int64> fst;
            private readonly FST.BytesReader bytesReader;
            private readonly FST.Arc<Int64> firstArc = new FST.Arc<Int64>();
            private readonly FST.Arc<Int64> scratchArc = new FST.Arc<Int64>();
            private readonly Int32sRef scratchInts = new Int32sRef();
            private readonly BytesRef scratchBytes = new BytesRef();

            internal FSTTermsEnum(FST<Int64> fst)
            {
                this.fst = fst;
                input = new BytesRefFSTEnum<Int64>(fst);
                bytesReader = fst.GetBytesReader();
            }

            public override bool MoveNext()
            {
                if (input.MoveNext())
                    return input.Current.Input != null;
                return false;
            }

            [Obsolete("Use MoveNext() and Term instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public override BytesRef Next()
            {
                return !MoveNext() ? null : input.Current.Input;
            }

            public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

            public override SeekStatus SeekCeil(BytesRef text)
            {
                if (input.SeekCeil(text) is null)
                {
                    return SeekStatus.END;
                }
                else if (Term.Equals(text))
                {
                    // TODO: add SeekStatus to FSTEnum like in https://issues.apache.org/jira/browse/LUCENE-3729
                    // to remove this comparision?
                    return SeekStatus.FOUND;
                }
                else
                {
                    return SeekStatus.NOT_FOUND;
                }
            }

            public override bool SeekExact(BytesRef text)
            {
                return input.SeekExact(text) != null;
            }

            public override void SeekExact(long ord)
            {
                // TODO: would be better to make this simpler and faster.
                // but we dont want to introduce a bug that corrupts our enum state!
                bytesReader.Position = 0;
                fst.GetFirstArc(firstArc);
                Int32sRef output = Util.GetByOutput(fst, ord, bytesReader, firstArc, scratchArc, scratchInts);
                scratchBytes.Bytes = new byte[output.Length];
                scratchBytes.Offset = 0;
                scratchBytes.Length = 0;
                Util.ToBytesRef(output, scratchBytes);
                // TODO: we could do this lazily, better to try to push into FSTEnum though?
                input.SeekExact(scratchBytes);
            }

            public override BytesRef Term => input.Current.Input;

            public override long Ord => input.Current.Output;

            public override int DocFreq => throw UnsupportedOperationException.Create();

            public override long TotalTermFreq => throw UnsupportedOperationException.Create();

            public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, DocsFlags flags)
            {
                throw UnsupportedOperationException.Create();
            }

            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags)
            {
                throw UnsupportedOperationException.Create();
            }
        }
    }
}