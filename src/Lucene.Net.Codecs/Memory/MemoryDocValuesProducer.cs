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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Lucene.Net.Codecs.Sep;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using Lucene.Net.Util.Packed;

namespace Lucene.Net.Codecs.Memory
{

    using Util = Lucene.Net.Util.Fst.Util;

    /// <summary>
    /// TextReader for <seealso cref="MemoryDocValuesFormat"/>
    /// </summary>
    internal class MemoryDocValuesProducer : DocValuesProducer
    {
        // metadata maps (just file pointers and minimal stuff)
        private readonly IDictionary<int?, NumericEntry> numerics;
        private readonly IDictionary<int?, BinaryEntry> binaries;
        private readonly IDictionary<int?, FSTEntry> fsts;
        private readonly IndexInput data;

        // ram instances we have already loaded
        private readonly IDictionary<int?, NumericDocValues> numericInstances = new Dictionary<int?, NumericDocValues>();
        private readonly IDictionary<int?, BinaryDocValues> binaryInstances = new Dictionary<int?, BinaryDocValues>();
        private readonly IDictionary<int?, FST<long?>> fstInstances = new Dictionary<int?, FST<long?>>();
        private readonly IDictionary<int?, Bits> docsWithFieldInstances = new Dictionary<int?, Bits>();

        private readonly int maxDoc;
        private readonly AtomicLong ramBytesUsed;
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
                numerics = new Dictionary<int?, NumericEntry>();
                binaries = new Dictionary<int?, BinaryEntry>();
                fsts = new Dictionary<int?, FSTEntry>();
                ReadFields(@in, state.FieldInfos);
                if (version >= VERSION_CHECKSUM)
                {
                    CodecUtil.CheckFooter(@in);
                }
                else
                {
                    CodecUtil.CheckEOF(@in);
                }
                ramBytesUsed = new AtomicLong(RamUsageEstimator.ShallowSizeOfInstance(this.GetType()));
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(@in);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException(@in);
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
                    IOUtils.CloseWhileHandlingException(this.data);
                }
            }
        }

        private void ReadFields(IndexInput meta, FieldInfos infos)
        {
            int fieldNumber = meta.ReadVInt();
            while (fieldNumber != -1)
            {
                int fieldType = meta.ReadByte();
                if (fieldType == NUMBER)
                {
                    var entry = new NumericEntry {offset = meta.ReadLong(), missingOffset = meta.ReadLong()};
                    if (entry.missingOffset != -1)
                    {
                        entry.missingBytes = meta.ReadLong();
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
                        entry.packedIntsVersion = meta.ReadVInt();
                    }
                    numerics[fieldNumber] = entry;
                }
                else if (fieldType == BYTES)
                {
                    var entry = new BinaryEntry
                    {
                        offset = meta.ReadLong(),
                        numBytes = meta.ReadLong(),
                        missingOffset = meta.ReadLong()
                    };
                    if (entry.missingOffset != -1)
                    {
                        entry.missingBytes = meta.ReadLong();
                    }
                    else
                    {
                        entry.missingBytes = 0;
                    }
                    entry.minLength = meta.ReadVInt();
                    entry.maxLength = meta.ReadVInt();
                    if (entry.minLength != entry.maxLength)
                    {
                        entry.packedIntsVersion = meta.ReadVInt();
                        entry.blockSize = meta.ReadVInt();
                    }
                    binaries[fieldNumber] = entry;
                }
                else if (fieldType == FST)
                {
                    var entry = new FSTEntry {offset = meta.ReadLong(), numOrds = meta.ReadVLong()};
                    fsts[fieldNumber] = entry;
                }
                else
                {
                    throw new CorruptIndexException("invalid entry type: " + fieldType + ", input=" + meta);
                }
                fieldNumber = meta.ReadVInt();
            }
        }

        public override NumericDocValues GetNumeric(FieldInfo field)
        {
            lock (this)
            {
                NumericDocValues instance;
                if (!numericInstances.TryGetValue(field.Number, out instance))
                {
                    instance = LoadNumeric(field);
                    numericInstances[field.Number] = instance;
                }
                return instance;
            }
        }

        public override long RamBytesUsed()
        {
            return ramBytesUsed.Get();
        }

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
                    int size = data.ReadVInt();
                    if (size > 256)
                    {
                        throw new CorruptIndexException(
                            "TABLE_COMPRESSED cannot have more than 256 distinct values, input=" + data);
                    }
                    var decode = new long[size];
                    for (int i = 0; i < decode.Length; i++)
                    {
                        decode[i] = data.ReadLong();
                    }
                    int formatID = data.ReadVInt();
                    int bitsPerValue = data.ReadVInt();
                    var ordsReader = PackedInts.GetReaderNoHeader(data, PackedInts.Format.ById(formatID),
                        entry.packedIntsVersion, maxDoc, bitsPerValue);
                    ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(decode) + ordsReader.RamBytesUsed());
                    return new NumericDocValuesAnonymousInnerClassHelper(this, decode, ordsReader);
                case DELTA_COMPRESSED:
                    int blockSize = data.ReadVInt();
                    var reader = new BlockPackedReader(data, entry.packedIntsVersion, blockSize, maxDoc,
                        false);
                    ramBytesUsed.AddAndGet(reader.RamBytesUsed());
                    return reader;
                case UNCOMPRESSED:
                    var bytes = new byte[maxDoc];
                    data.ReadBytes(bytes, 0, bytes.Length);
                    ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(bytes));
                    // LUCENENET: IMPORTANT - some bytes are negative here, so we need to pass as sbyte
                    return new NumericDocValuesAnonymousInnerClassHelper2(this, (sbyte[])(Array)bytes);
                case GCD_COMPRESSED:
                    long min = data.ReadLong();
                    long mult = data.ReadLong();
                    int quotientBlockSize = data.ReadVInt();
                    var quotientReader = new BlockPackedReader(data, entry.packedIntsVersion,
                        quotientBlockSize, maxDoc, false);
                    ramBytesUsed.AddAndGet(quotientReader.RamBytesUsed());
                    return new NumericDocValuesAnonymousInnerClassHelper3(this, min, mult, quotientReader);
                default:
                    throw new InvalidOperationException();
            }
        }

        private class NumericDocValuesAnonymousInnerClassHelper : NumericDocValues
        {
            private readonly MemoryDocValuesProducer outerInstance;

            private readonly long[] decode;
            private readonly PackedInts.Reader ordsReader;

            public NumericDocValuesAnonymousInnerClassHelper(MemoryDocValuesProducer outerInstance, long[] decode,
                PackedInts.Reader ordsReader)
            {
                this.outerInstance = outerInstance;
                this.decode = decode;
                this.ordsReader = ordsReader;
            }

            public override long Get(int docID)
            {
                return decode[(int) ordsReader.Get(docID)];
            }
        }

        private class NumericDocValuesAnonymousInnerClassHelper2 : NumericDocValues
        {
            private readonly MemoryDocValuesProducer outerInstance;
            private readonly sbyte[] bytes;

            public NumericDocValuesAnonymousInnerClassHelper2(MemoryDocValuesProducer outerInstance, sbyte[] bytes)
            {
                this.outerInstance = outerInstance;
                this.bytes = bytes;
            }

            public override long Get(int docID)
            {
                return bytes[docID];
            }
        }

        private class NumericDocValuesAnonymousInnerClassHelper3 : NumericDocValues
        {
            private readonly long min;
            private readonly long mult;
            private readonly BlockPackedReader quotientReader;

            public NumericDocValuesAnonymousInnerClassHelper3(MemoryDocValuesProducer outerInstance, long min, long mult,
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
            lock (this)
            {
                BinaryDocValues instance;
                if (!binaryInstances.TryGetValue(field.Number, out instance))
                {
                    instance = LoadBinary(field);
                    binaryInstances[field.Number] = instance;
                }
                return instance;
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
                return new BinaryDocValuesAnonymousInnerClassHelper(this, bytesReader, fixedLength);
            }
            else
            {
                data.Seek(data.FilePointer + entry.missingBytes);
                var addresses = new MonotonicBlockPackedReader(data, entry.packedIntsVersion,
                    entry.blockSize, maxDoc, false);
                ramBytesUsed.AddAndGet(bytes.RamBytesUsed() + addresses.RamBytesUsed());
                return new BinaryDocValuesAnonymousInnerClassHelper2(this, bytesReader, addresses);
            }
        }

        private class BinaryDocValuesAnonymousInnerClassHelper : BinaryDocValues
        {
            private readonly PagedBytes.Reader bytesReader;
            private readonly int fixedLength;

            public BinaryDocValuesAnonymousInnerClassHelper(MemoryDocValuesProducer outerInstance,
                PagedBytes.Reader bytesReader, int fixedLength)
            {
                this.bytesReader = bytesReader;
                this.fixedLength = fixedLength;
            }

            public override void Get(int docID, BytesRef result)
            {
                bytesReader.FillSlice(result, fixedLength*(long) docID, fixedLength);
            }
        }

        private class BinaryDocValuesAnonymousInnerClassHelper2 : BinaryDocValues
        {
            private readonly MemoryDocValuesProducer outerInstance;

            private readonly PagedBytes.Reader bytesReader;
            private readonly MonotonicBlockPackedReader addresses;

            public BinaryDocValuesAnonymousInnerClassHelper2(MemoryDocValuesProducer outerInstance,
                PagedBytes.Reader bytesReader, MonotonicBlockPackedReader addresses)
            {
                this.outerInstance = outerInstance;
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
            FST<long?> instance;
            lock (this)
            {
                if (!fstInstances.TryGetValue(field.Number, out instance))
                {
                    data.Seek(entry.offset);
                    instance = new FST<long?>(data, PositiveIntOutputs.Singleton);
                    ramBytesUsed.AddAndGet(instance.SizeInBytes());
                    fstInstances[field.Number] = instance;
                }
            }
            var docToOrd = GetNumeric(field);
            var fst = instance;

            // per-thread resources
            var @in = fst.BytesReader;
            var firstArc = new FST.Arc<long?>();
            var scratchArc = new FST.Arc<long?>();
            var scratchInts = new IntsRef();
            var fstEnum = new BytesRefFSTEnum<long?>(fst);

            return new SortedDocValuesAnonymousInnerClassHelper(entry, docToOrd, fst, @in, firstArc, scratchArc,
                scratchInts, fstEnum);
        }

        private class SortedDocValuesAnonymousInnerClassHelper : SortedDocValues
        {
            private readonly MemoryDocValuesProducer.FSTEntry entry;
            private readonly NumericDocValues docToOrd;
            private readonly FST<long?> fst;
            private readonly FST.BytesReader @in;
            private readonly FST.Arc<long?> firstArc;
            private readonly FST.Arc<long?> scratchArc;
            private readonly IntsRef scratchInts;
            private readonly BytesRefFSTEnum<long?> fstEnum;

            public SortedDocValuesAnonymousInnerClassHelper(FSTEntry fstEntry,
                NumericDocValues numericDocValues, FST<long?> fst1, FST.BytesReader @in, FST.Arc<long?> arc, FST.Arc<long?> scratchArc1,
                IntsRef intsRef, BytesRefFSTEnum<long?> bytesRefFstEnum)
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
                    IntsRef output = Util.GetByOutput(fst, ord, @in, firstArc, scratchArc, scratchInts);
                    result.Bytes = new byte[output.Length];
                    result.Offset = 0;
                    result.Length = 0;
                    Util.ToBytesRef(output, result);
                }
                catch (IOException bogus)
                {
                    throw new Exception(bogus.Message, bogus);
                }
            }

            public override int LookupTerm(BytesRef key)
            {
                try
                {
                    var o = fstEnum.SeekCeil(key);
                    if (o == null)
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
                catch (IOException bogus)
                {
                    throw new Exception(bogus.Message, bogus);
                }
            }

            public override int ValueCount
            {
                get { return (int) entry.numOrds; }
            }

            public override TermsEnum TermsEnum()
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
            FST<long?> instance;
            lock (this)
            {
                if (!fstInstances.TryGetValue(field.Number, out instance))
                {
                    data.Seek(entry.offset);
                    instance = new FST<long?>(data, PositiveIntOutputs.Singleton);
                    ramBytesUsed.AddAndGet(instance.SizeInBytes());
                    fstInstances[field.Number] = instance;
                }
            }
            var docToOrds = GetBinary(field);
            var fst = instance;

            // per-thread resources
            var @in = fst.BytesReader;
            var firstArc = new FST.Arc<long?>();
            var scratchArc = new FST.Arc<long?>();
            var scratchInts = new IntsRef();
            var fstEnum = new BytesRefFSTEnum<long?>(fst);
            var @ref = new BytesRef();
            var input = new ByteArrayDataInput();
            return new SortedSetDocValuesAnonymousInnerClassHelper(entry, docToOrds, fst, @in, firstArc,
                scratchArc, scratchInts, fstEnum, @ref, input);
        }

        private class SortedSetDocValuesAnonymousInnerClassHelper : SortedSetDocValues
        {
            private readonly MemoryDocValuesProducer.FSTEntry entry;
            private readonly BinaryDocValues docToOrds;
            private readonly FST<long?> fst;
            private readonly FST.BytesReader @in;
            private readonly FST.Arc<long?> firstArc;
            private readonly FST.Arc<long?> scratchArc;
            private readonly IntsRef scratchInts;
            private readonly BytesRefFSTEnum<long?> fstEnum;
            private readonly BytesRef @ref;
            private readonly ByteArrayDataInput input;

            private long currentOrd;

            public SortedSetDocValuesAnonymousInnerClassHelper(FSTEntry fstEntry, BinaryDocValues binaryDocValues, FST<long?> fst1,
                FST.BytesReader @in, FST.Arc<long?> arc, FST.Arc<long?> scratchArc1, IntsRef intsRef, BytesRefFSTEnum<long?> bytesRefFstEnum,
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
                if (input.Eof())
                {
                    return NO_MORE_ORDS;
                }
                else
                {
                    currentOrd += input.ReadVLong();
                    return currentOrd;
                }
            }

            public override int Document
            {
                set
                {
                    docToOrds.Get(value, @ref);
                    input.Reset(@ref.Bytes, @ref.Offset, @ref.Length);
                    currentOrd = 0;
                }
            }

            public override void LookupOrd(long ord, BytesRef result)
            {
                try
                {
                    @in.Position = 0;
                    fst.GetFirstArc(firstArc);
                    IntsRef output = Util.GetByOutput(fst, ord, @in, firstArc, scratchArc, scratchInts);
                    result.Bytes = new byte[output.Length];
                    result.Offset = 0;
                    result.Length = 0;
                    Util.ToBytesRef(output, result);
                }
                catch (IOException bogus)
                {
                    throw new Exception(bogus.Message, bogus);
                }
            }

            public override long LookupTerm(BytesRef key)
            {
                try
                {
                    var o = fstEnum.SeekCeil(key);
                    if (o == null)
                    {
                        return -ValueCount - 1;
                    }
                    else if (o.Input.Equals(key))
                    {
                        return o.Output.Value;
                    }
                    else
                    {
                        return -o.Output.Value - 1;
                    }
                }
                catch (IOException bogus)
                {
                    throw new Exception(bogus.Message, bogus);
                }
            }

            public override long ValueCount
            {
                get { return entry.numOrds; }
            }

            public override TermsEnum TermsEnum()
            {
                return new FSTTermsEnum(fst);
            }
        }

        private Bits GetMissingBits(int fieldNumber, long offset, long length)
        {
            if (offset == -1)
            {
                return new Bits_MatchAllBits(maxDoc);
            }
            else
            {
                Bits instance;
                lock (this)
                {
                    if (!docsWithFieldInstances.TryGetValue(fieldNumber, out instance))
                    {
                        var data = (IndexInput)this.data.Clone();
                        data.Seek(offset);
                        Debug.Assert(length%8 == 0);
                        var bits = new long[(int) length >> 3];
                        for (var i = 0; i < bits.Length; i++)
                        {
                            bits[i] = data.ReadLong();
                        }
                        instance = new FixedBitSet(bits, maxDoc);
                        docsWithFieldInstances[fieldNumber] = instance;
                    }
                }
                return instance;
            }
        }

        public override Bits GetDocsWithField(FieldInfo field)
        {
            switch (field.DocValuesType)
            {
                case FieldInfo.DocValuesType_e.SORTED_SET:
                    return DocValues.DocsWithValue(GetSortedSet(field), maxDoc);
                case FieldInfo.DocValuesType_e.SORTED:
                    return DocValues.DocsWithValue(GetSorted(field), maxDoc);
                case FieldInfo.DocValuesType_e.BINARY:
                    var be = binaries[field.Number];
                    return GetMissingBits(field.Number, be.missingOffset, be.missingBytes);
                case FieldInfo.DocValuesType_e.NUMERIC:
                    var ne = numerics[field.Number];
                    return GetMissingBits(field.Number, ne.missingOffset, ne.missingBytes);
                default:
                    throw new InvalidOperationException();
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
            internal readonly BytesRefFSTEnum<long?> input;

            // this is all for the complicated seek(ord)...
            // maybe we should add a FSTEnum that supports this operation?
            internal readonly FST<long?> fst;
            internal readonly FST.BytesReader bytesReader;
            internal readonly FST.Arc<long?> firstArc = new FST.Arc<long?>();
            internal readonly FST.Arc<long?> scratchArc = new FST.Arc<long?>();
            internal readonly IntsRef scratchInts = new IntsRef();
            internal readonly BytesRef scratchBytes = new BytesRef();

            internal FSTTermsEnum(FST<long?> fst)
            {
                this.fst = fst;
                input = new BytesRefFSTEnum<long?>(fst);
                bytesReader = fst.BytesReader;
            }

            public override BytesRef Next()
            {
                var io = input.Next();
                return io == null ? null : io.Input;
            }

            public override IComparer<BytesRef> Comparator
            {
                get { return BytesRef.UTF8SortedAsUnicodeComparer; }
            }

            public override SeekStatus SeekCeil(BytesRef text)
            {
                if (input.SeekCeil(text) == null)
                {
                    return SeekStatus.END;
                }
                else if (Term().Equals(text))
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
                IntsRef output = Util.GetByOutput(fst, ord, bytesReader, firstArc, scratchArc, scratchInts);
                scratchBytes.Bytes = new byte[output.Length];
                scratchBytes.Offset = 0;
                scratchBytes.Length = 0;
                Util.ToBytesRef(output, scratchBytes);
                // TODO: we could do this lazily, better to try to push into FSTEnum though?
                input.SeekExact(scratchBytes);
            }

            public override BytesRef Term()
            {
                return input.Current().Input;
            }

            public override long Ord()
            {
                return input.Current().Output.Value;
            }

            public override int DocFreq()
            {
                throw new NotSupportedException();
            }

            public override long TotalTermFreq()
            {
                throw new NotSupportedException();
            }

            public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
            {
                throw new NotSupportedException();
            }

            public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
            {
                throw new NotSupportedException();
            }
        }
    }

}