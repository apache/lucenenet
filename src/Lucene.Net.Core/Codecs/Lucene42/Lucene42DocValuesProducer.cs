using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util.Fst;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Codecs.Lucene42
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

    using BinaryDocValues = Lucene.Net.Index.BinaryDocValues;
    using IBits = Lucene.Net.Util.IBits;
    using BlockPackedReader = Lucene.Net.Util.Packed.BlockPackedReader;
    using ByteArrayDataInput = Lucene.Net.Store.ByteArrayDataInput;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using ChecksumIndexInput = Lucene.Net.Store.ChecksumIndexInput;
    using CorruptIndexException = Lucene.Net.Index.CorruptIndexException;
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using DocValues = Lucene.Net.Index.DocValues;
    using DocValuesType = Lucene.Net.Index.DocValuesType;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IntsRef = Lucene.Net.Util.IntsRef;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using MonotonicBlockPackedReader = Lucene.Net.Util.Packed.MonotonicBlockPackedReader;
    using NumericDocValues = Lucene.Net.Index.NumericDocValues;
    using PackedInts = Lucene.Net.Util.Packed.PackedInts;
    using PagedBytes = Lucene.Net.Util.PagedBytes;
    using PositiveIntOutputs = Lucene.Net.Util.Fst.PositiveIntOutputs;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using SegmentReadState = Lucene.Net.Index.SegmentReadState;
    using SortedDocValues = Lucene.Net.Index.SortedDocValues;
    using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using Util = Lucene.Net.Util.Fst.Util;

    /// <summary>
    /// Reader for <seealso cref="Lucene42DocValuesFormat"/>
    /// </summary>
    internal class Lucene42DocValuesProducer : DocValuesProducer
    {
        // metadata maps (just file pointers and minimal stuff)
        private readonly IDictionary<int, NumericEntry> numerics;

        private readonly IDictionary<int, BinaryEntry> binaries;
        private readonly IDictionary<int, FSTEntry> fsts;
        private readonly IndexInput data;
        private readonly int version;

        // ram instances we have already loaded
        private readonly IDictionary<int, NumericDocValues> numericInstances = new Dictionary<int, NumericDocValues>();

        private readonly IDictionary<int, BinaryDocValues> binaryInstances = new Dictionary<int, BinaryDocValues>();
        private readonly IDictionary<int, FST<long?>> fstInstances = new Dictionary<int, FST<long?>>();

        private readonly int maxDoc;
        private readonly AtomicLong ramBytesUsed;

        internal const sbyte NUMBER = 0;
        internal const sbyte BYTES = 1;
        internal const sbyte FST = 2;

        internal const int BLOCK_SIZE = 4096;

        internal const sbyte DELTA_COMPRESSED = 0;
        internal const sbyte TABLE_COMPRESSED = 1;
        internal const sbyte UNCOMPRESSED = 2;
        internal const sbyte GCD_COMPRESSED = 3;

        internal const int VERSION_START = 0;
        internal const int VERSION_GCD_COMPRESSION = 1;
        internal const int VERSION_CHECKSUM = 2;
        internal const int VERSION_CURRENT = VERSION_CHECKSUM;

        internal Lucene42DocValuesProducer(SegmentReadState state, string dataCodec, string dataExtension, string metaCodec, string metaExtension)
        {
            maxDoc = state.SegmentInfo.DocCount;
            string metaName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, metaExtension);
            // read in the entries from the metadata file.
            ChecksumIndexInput @in = state.Directory.OpenChecksumInput(metaName, state.Context);
            bool success = false;
            ramBytesUsed = new AtomicLong(RamUsageEstimator.ShallowSizeOfInstance(this.GetType()));
            try
            {
                version = CodecUtil.CheckHeader(@in, metaCodec, VERSION_START, VERSION_CURRENT);
                numerics = new Dictionary<int, NumericEntry>();
                binaries = new Dictionary<int, BinaryEntry>();
                fsts = new Dictionary<int, FSTEntry>();
                ReadFields(@in, state.FieldInfos);

                if (version >= VERSION_CHECKSUM)
                {
                    CodecUtil.CheckFooter(@in);
                }
                else
                {
                    CodecUtil.CheckEOF(@in);
                }

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
                string dataName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, dataExtension);
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
                // check should be: infos.fieldInfo(fieldNumber) != null, which incorporates negative check
                // but docvalues updates are currently buggy here (loading extra stuff, etc): LUCENE-5616
                if (fieldNumber < 0)
                {
                    // trickier to validate more: because we re-use for norms, because we use multiple entries
                    // for "composite" types like sortedset, etc.
                    throw new CorruptIndexException("Invalid field number: " + fieldNumber + ", input=" + meta);
                }
                int fieldType = meta.ReadByte();
                if (fieldType == NUMBER)
                {
                    var entry = new NumericEntry {Offset = meta.ReadLong(), Format = (sbyte)meta.ReadByte()};
                    switch (entry.Format)
                    {
                        case DELTA_COMPRESSED:
                        case TABLE_COMPRESSED:
                        case GCD_COMPRESSED:
                        case UNCOMPRESSED:
                            break;

                        default:
                            throw new CorruptIndexException("Unknown format: " + entry.Format + ", input=" + meta);
                    }
                    if (entry.Format != UNCOMPRESSED)
                    {
                        entry.PackedIntsVersion = meta.ReadVInt();
                    }
                    numerics[fieldNumber] = entry;
                }
                else if (fieldType == BYTES)
                {
                    BinaryEntry entry = new BinaryEntry();
                    entry.Offset = meta.ReadLong();
                    entry.NumBytes = meta.ReadLong();
                    entry.MinLength = meta.ReadVInt();
                    entry.MaxLength = meta.ReadVInt();
                    if (entry.MinLength != entry.MaxLength)
                    {
                        entry.PackedIntsVersion = meta.ReadVInt();
                        entry.BlockSize = meta.ReadVInt();
                    }
                    binaries[fieldNumber] = entry;
                }
                else if (fieldType == FST)
                {
                    FSTEntry entry = new FSTEntry();
                    entry.Offset = meta.ReadLong();
                    entry.NumOrds = meta.ReadVLong();
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
                numericInstances.TryGetValue(field.Number, out instance);
                if (instance == null)
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
            data.Seek(entry.Offset);
            switch (entry.Format)
            {
                case TABLE_COMPRESSED:
                    int size = data.ReadVInt();
                    if (size > 256)
                    {
                        throw new CorruptIndexException("TABLE_COMPRESSED cannot have more than 256 distinct values, input=" + data);
                    }
                    var decode = new long[size];
                    for (int i = 0; i < decode.Length; i++)
                    {
                        decode[i] = data.ReadLong();
                    }
                    int formatID = data.ReadVInt();
                    int bitsPerValue = data.ReadVInt();
                    PackedInts.Reader ordsReader = PackedInts.GetReaderNoHeader(data, PackedInts.Format.ById(formatID), entry.PackedIntsVersion, maxDoc, bitsPerValue);
                    ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(decode) + ordsReader.RamBytesUsed());
                    return new NumericDocValuesAnonymousInnerClassHelper(decode, ordsReader);

                case DELTA_COMPRESSED:
                    int blockSize = data.ReadVInt();
                    var reader = new BlockPackedReader(data, entry.PackedIntsVersion, blockSize, maxDoc, false);
                    ramBytesUsed.AddAndGet(reader.RamBytesUsed());
                    return reader;

                case UNCOMPRESSED:
                    byte[] bytes = new byte[maxDoc];
                    data.ReadBytes(bytes, 0, bytes.Length);
                    ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(bytes));
                    return new NumericDocValuesAnonymousInnerClassHelper2(this, bytes);

                case GCD_COMPRESSED:
                    long min = data.ReadLong();
                    long mult = data.ReadLong();
                    int quotientBlockSize = data.ReadVInt();
                    BlockPackedReader quotientReader = new BlockPackedReader(data, entry.PackedIntsVersion, quotientBlockSize, maxDoc, false);
                    ramBytesUsed.AddAndGet(quotientReader.RamBytesUsed());
                    return new NumericDocValuesAnonymousInnerClassHelper3(min, mult, quotientReader);

                default:
                    throw new InvalidOperationException();
            }
        }

        private class NumericDocValuesAnonymousInnerClassHelper : NumericDocValues
        {
            private readonly long[] decode;
            private readonly PackedInts.Reader ordsReader;

            public NumericDocValuesAnonymousInnerClassHelper(long[] decode, PackedInts.Reader ordsReader)
            {
                this.decode = decode;
                this.ordsReader = ordsReader;
            }

            public override long Get(int docID)
            {
                return decode[(int)ordsReader.Get(docID)];
            }
        }

        private class NumericDocValuesAnonymousInnerClassHelper2 : NumericDocValues
        {
            private readonly byte[] bytes;

            public NumericDocValuesAnonymousInnerClassHelper2(Lucene42DocValuesProducer outerInstance, byte[] bytes)
            {
                this.bytes = bytes;
            }

            public override long Get(int docID)
            {
                return (sbyte)bytes[docID];
            }
        }

        private class NumericDocValuesAnonymousInnerClassHelper3 : NumericDocValues
        {
            private readonly long min;
            private readonly long mult;
            private readonly BlockPackedReader quotientReader;

            public NumericDocValuesAnonymousInnerClassHelper3(long min, long mult, BlockPackedReader quotientReader)
            {
                this.min = min;
                this.mult = mult;
                this.quotientReader = quotientReader;
            }

            public override long Get(int docID)
            {
                return min + mult * quotientReader.Get(docID);
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
            data.Seek(entry.Offset);
            PagedBytes bytes = new PagedBytes(16);
            bytes.Copy(data, entry.NumBytes);
            PagedBytes.Reader bytesReader = bytes.Freeze(true);
            if (entry.MinLength == entry.MaxLength)
            {
                int fixedLength = entry.MinLength;
                ramBytesUsed.AddAndGet(bytes.RamBytesUsed());
                return new BinaryDocValuesAnonymousInnerClassHelper(bytesReader, fixedLength);
            }
            else
            {
                MonotonicBlockPackedReader addresses = new MonotonicBlockPackedReader(data, entry.PackedIntsVersion, entry.BlockSize, maxDoc, false);
                ramBytesUsed.AddAndGet(bytes.RamBytesUsed() + addresses.RamBytesUsed());
                return new BinaryDocValuesAnonymousInnerClassHelper2(bytesReader, addresses);
            }
        }

        private class BinaryDocValuesAnonymousInnerClassHelper : BinaryDocValues
        {
            private readonly PagedBytes.Reader bytesReader;
            private readonly int fixedLength;

            public BinaryDocValuesAnonymousInnerClassHelper(PagedBytes.Reader bytesReader, int fixedLength)
            {
                this.bytesReader = bytesReader;
                this.fixedLength = fixedLength;
            }

            public override void Get(int docID, BytesRef result)
            {
                bytesReader.FillSlice(result, fixedLength * (long)docID, fixedLength);
            }
        }

        private class BinaryDocValuesAnonymousInnerClassHelper2 : BinaryDocValues
        {
            private readonly PagedBytes.Reader bytesReader;
            private readonly MonotonicBlockPackedReader addresses;

            public BinaryDocValuesAnonymousInnerClassHelper2(PagedBytes.Reader bytesReader, MonotonicBlockPackedReader addresses)
            {
                this.bytesReader = bytesReader;
                this.addresses = addresses;
            }

            public override void Get(int docID, BytesRef result)
            {
                long startAddress = docID == 0 ? 0 : addresses.Get(docID - 1);
                long endAddress = addresses.Get(docID);
                bytesReader.FillSlice(result, startAddress, (int)(endAddress - startAddress));
            }
        }

        public override SortedDocValues GetSorted(FieldInfo field)
        {
            FSTEntry entry = fsts[field.Number];
            FST<long?> instance;
            lock (this)
            {
                if (!fstInstances.TryGetValue(field.Number, out instance))
                {
                    data.Seek(entry.Offset);
                    instance = new FST<long?>(data, PositiveIntOutputs.Singleton);
                    ramBytesUsed.AddAndGet(instance.SizeInBytes());
                    fstInstances[field.Number] = instance;
                }
            }
            var docToOrd = GetNumeric(field);
            var fst = instance;

            // per-thread resources
            var @in = fst.GetBytesReader();
            var firstArc = new FST.Arc<long?>();
            var scratchArc = new FST.Arc<long?>();
            var scratchInts = new IntsRef();
            var fstEnum = new BytesRefFSTEnum<long?>(fst);

            return new SortedDocValuesAnonymousInnerClassHelper(entry, docToOrd, fst, @in, firstArc, scratchArc, scratchInts, fstEnum);
        }

        private class SortedDocValuesAnonymousInnerClassHelper : SortedDocValues
        {
            private readonly FSTEntry entry;
            private readonly NumericDocValues docToOrd;
            private readonly FST<long?> fst;
            private readonly FST.BytesReader @in;
            private readonly FST.Arc<long?> firstArc;
            private readonly FST.Arc<long?> scratchArc;
            private readonly IntsRef scratchInts;
            private readonly BytesRefFSTEnum<long?> fstEnum;

            public SortedDocValuesAnonymousInnerClassHelper(FSTEntry entry, NumericDocValues docToOrd, FST<long?> fst, FST.BytesReader @in, FST.Arc<long?> firstArc, FST.Arc<long?> scratchArc, IntsRef scratchInts, BytesRefFSTEnum<long?> fstEnum)
            {
                this.entry = entry;
                this.docToOrd = docToOrd;
                this.fst = fst;
                this.@in = @in;
                this.firstArc = firstArc;
                this.scratchArc = scratchArc;
                this.scratchInts = scratchInts;
                this.fstEnum = fstEnum;
            }

            public override int GetOrd(int docID)
            {
                return (int)docToOrd.Get(docID);
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                try
                {
                    @in.Position = 0;
                    fst.GetFirstArc(firstArc);
                    IntsRef output = Lucene.Net.Util.Fst.Util.GetByOutput(fst, ord, @in, firstArc, scratchArc, scratchInts);
                    result.Bytes = new byte[output.Length];
                    result.Offset = 0;
                    result.Length = 0;
                    Util.ToBytesRef(output, result);
                }
                catch (System.IO.IOException bogus)
                {
                    throw bogus;
                }
            }

            public override int LookupTerm(BytesRef key)
            {
                try
                {
                    BytesRefFSTEnum.InputOutput<long?> o = fstEnum.SeekCeil(key);
                    if (o == null)
                    {
                        return -ValueCount - 1;
                    }
                    else if (o.Input.Equals(key))
                    {
                        return (int)o.Output;
                    }
                    else
                    {
                        return (int)-o.Output - 1;
                    }
                }
                catch (System.IO.IOException bogus)
                {
                    throw bogus;
                }
            }

            public override int ValueCount
            {
                get
                {
                    return (int)entry.NumOrds;
                }
            }

            public override TermsEnum GetTermsEnum()
            {
                return new FSTTermsEnum(fst);
            }
        }

        public override SortedSetDocValues GetSortedSet(FieldInfo field)
        {
            FSTEntry entry = fsts[field.Number];
            if (entry.NumOrds == 0)
            {
                return DocValues.EMPTY_SORTED_SET; // empty FST!
            }
            FST<long?> instance;
            lock (this)
            {
                if (!fstInstances.TryGetValue(field.Number, out instance))
                {
                    data.Seek(entry.Offset);
                    instance = new FST<long?>((DataInput)data, Lucene.Net.Util.Fst.PositiveIntOutputs.Singleton);
                    ramBytesUsed.AddAndGet(instance.SizeInBytes());
                    fstInstances[field.Number] = instance;
                }
            }
            BinaryDocValues docToOrds = GetBinary(field);
            FST<long?> fst = instance;

            // per-thread resources
            var @in = fst.GetBytesReader();
            var firstArc = new FST.Arc<long?>();
            var scratchArc = new FST.Arc<long?>();
            var scratchInts = new IntsRef();
            var fstEnum = new BytesRefFSTEnum<long?>(fst);
            var @ref = new BytesRef();
            var input = new ByteArrayDataInput();
            return new SortedSetDocValuesAnonymousInnerClassHelper(entry, docToOrds, fst, @in, firstArc, scratchArc, scratchInts, fstEnum, @ref, input);
        }

        private class SortedSetDocValuesAnonymousInnerClassHelper : SortedSetDocValues
        {
            private readonly FSTEntry entry;
            private readonly BinaryDocValues docToOrds;
            private readonly FST<long?> fst;
            private readonly FST.BytesReader @in;
            private readonly FST.Arc<long?> firstArc;
            private readonly FST.Arc<long?> scratchArc;
            private readonly IntsRef scratchInts;
            private readonly BytesRefFSTEnum<long?> fstEnum;
            private readonly BytesRef @ref;
            private readonly ByteArrayDataInput input;

            public SortedSetDocValuesAnonymousInnerClassHelper(FSTEntry entry, BinaryDocValues docToOrds, FST<long?> fst, FST.BytesReader @in, FST.Arc<long?> firstArc, FST.Arc<long?> scratchArc, IntsRef scratchInts, BytesRefFSTEnum<long?> fstEnum, BytesRef @ref, ByteArrayDataInput input)
            {
                this.entry = entry;
                this.docToOrds = docToOrds;
                this.fst = fst;
                this.@in = @in;
                this.firstArc = firstArc;
                this.scratchArc = scratchArc;
                this.scratchInts = scratchInts;
                this.fstEnum = fstEnum;
                this.@ref = @ref;
                this.input = input;
            }

            private long currentOrd;

            public override long NextOrd()
            {
                if (input.Eof)
                {
                    return NO_MORE_ORDS;
                }
                else
                {
                    currentOrd += input.ReadVLong();
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
                    IntsRef output = Lucene.Net.Util.Fst.Util.GetByOutput(fst, ord, @in, firstArc, scratchArc, scratchInts);
                    result.Bytes = new byte[output.Length];
                    result.Offset = 0;
                    result.Length = 0;
                    Lucene.Net.Util.Fst.Util.ToBytesRef(output, result);
                }
                catch (System.IO.IOException bogus)
                {
                    throw new Exception(bogus.ToString(), bogus);
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
                        return (int)o.Output.Value;
                    }
                    else
                    {
                        return -o.Output.Value - 1;
                    }
                }
                catch (System.IO.IOException bogus)
                {
                    throw new Exception(bogus.ToString(), bogus);
                }
            }

            public override long ValueCount
            {
                get
                {
                    return entry.NumOrds;
                }
            }

            public override TermsEnum GetTermsEnum()
            {
                return new FSTTermsEnum(fst);
            }
        }

        public override IBits GetDocsWithField(FieldInfo field)
        {
            if (field.DocValuesType == DocValuesType.SORTED_SET)
            {
                return DocValues.DocsWithValue(GetSortedSet(field), maxDoc);
            }
            else
            {
                return new Lucene.Net.Util.Bits.MatchAllBits(maxDoc);
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
            internal long Offset { get; set; }
            internal sbyte Format { get; set; }
            internal int PackedIntsVersion { get; set; }
        }

        internal class BinaryEntry
        {
            internal long Offset { get; set; }
            internal long NumBytes { get; set; }
            internal int MinLength { get; set; }
            internal int MaxLength { get; set; }
            internal int PackedIntsVersion { get; set; }
            internal int BlockSize { get; set; }
        }

        internal class FSTEntry
        {
            internal long Offset { get; set; }
            internal long NumOrds { get; set; }
        }

        // exposes FSTEnum directly as a TermsEnum: avoids binary-search next()
        internal class FSTTermsEnum : TermsEnum
        {
            internal readonly BytesRefFSTEnum<long?> @in;

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
                @in = new BytesRefFSTEnum<long?>(fst);
                bytesReader = fst.GetBytesReader();
            }

            public override BytesRef Next()
            {
                var io = @in.Next();
                if (io == null)
                {
                    return null;
                }
                else
                {
                    return io.Input;
                }
            }

            public override IComparer<BytesRef> Comparator
            {
                get
                {
                    return BytesRef.UTF8SortedAsUnicodeComparer;
                }
            }

            public override SeekStatus SeekCeil(BytesRef text)
            {
                if (@in.SeekCeil(text) == null)
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
                if (@in.SeekExact(text) == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            public override void SeekExact(long ord)
            {
                // TODO: would be better to make this simpler and faster.
                // but we dont want to introduce a bug that corrupts our enum state!
                bytesReader.Position = 0;
                fst.GetFirstArc(firstArc);
                IntsRef output = Lucene.Net.Util.Fst.Util.GetByOutput(fst, ord, bytesReader, firstArc, scratchArc, scratchInts);
                scratchBytes.Bytes = new byte[output.Length];
                scratchBytes.Offset = 0;
                scratchBytes.Length = 0;
                Lucene.Net.Util.Fst.Util.ToBytesRef(output, scratchBytes);
                // TODO: we could do this lazily, better to try to push into FSTEnum though?
                @in.SeekExact(scratchBytes);
            }

            public override BytesRef Term
            {
                get { return @in.Current.Input; }
            }

            public override long Ord
            {
                get { return @in.Current.Output.Value; }
            }

            public override int DocFreq
            {
                get { throw new System.NotSupportedException(); }
            }

            public override long TotalTermFreq
            {
                get { throw new System.NotSupportedException(); }
            }

            public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, int flags)
            {
                throw new System.NotSupportedException();
            }

            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, int flags)
            {
                throw new System.NotSupportedException();
            }
        }
    }
}