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
    using Bits = Lucene.Net.Util.Bits;
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
        private readonly IDictionary<int, NumericEntry> Numerics;

        private readonly IDictionary<int, BinaryEntry> Binaries;
        private readonly IDictionary<int, FSTEntry> Fsts;
        private readonly IndexInput Data;
        private readonly int Version;

        // ram instances we have already loaded
        private readonly IDictionary<int, NumericDocValues> NumericInstances = new Dictionary<int, NumericDocValues>();

        private readonly IDictionary<int, BinaryDocValues> BinaryInstances = new Dictionary<int, BinaryDocValues>();
        private readonly IDictionary<int, FST<long?>> FstInstances = new Dictionary<int, FST<long?>>();

        private readonly int MaxDoc;
        private readonly AtomicLong RamBytesUsed_Renamed;

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
            MaxDoc = state.SegmentInfo.DocCount;
            string metaName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, metaExtension);
            // read in the entries from the metadata file.
            ChecksumIndexInput @in = state.Directory.OpenChecksumInput(metaName, state.Context);
            bool success = false;
            RamBytesUsed_Renamed = new AtomicLong(RamUsageEstimator.ShallowSizeOfInstance(this.GetType()));
            try
            {
                Version = CodecUtil.CheckHeader(@in, metaCodec, VERSION_START, VERSION_CURRENT);
                Numerics = new Dictionary<int, NumericEntry>();
                Binaries = new Dictionary<int, BinaryEntry>();
                Fsts = new Dictionary<int, FSTEntry>();
                ReadFields(@in, state.FieldInfos);

                if (Version >= VERSION_CHECKSUM)
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
                Data = state.Directory.OpenInput(dataName, state.Context);
                int version2 = CodecUtil.CheckHeader(Data, dataCodec, VERSION_START, VERSION_CURRENT);
                if (Version != version2)
                {
                    throw new CorruptIndexException("Format versions mismatch");
                }

                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(this.Data);
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
                    Numerics[fieldNumber] = entry;
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
                    Binaries[fieldNumber] = entry;
                }
                else if (fieldType == FST)
                {
                    FSTEntry entry = new FSTEntry();
                    entry.Offset = meta.ReadLong();
                    entry.NumOrds = meta.ReadVLong();
                    Fsts[fieldNumber] = entry;
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
                NumericInstances.TryGetValue(field.Number, out instance);
                if (instance == null)
                {
                    instance = LoadNumeric(field);
                    NumericInstances[field.Number] = instance;
                }
                return instance;
            }
        }

        public override long RamBytesUsed()
        {
            return RamBytesUsed_Renamed.Get();
        }

        public override void CheckIntegrity()
        {
            if (Version >= VERSION_CHECKSUM)
            {
                CodecUtil.ChecksumEntireFile(Data);
            }
        }

        private NumericDocValues LoadNumeric(FieldInfo field)
        {
            NumericEntry entry = Numerics[field.Number];
            Data.Seek(entry.Offset);
            switch (entry.Format)
            {
                case TABLE_COMPRESSED:
                    int size = Data.ReadVInt();
                    if (size > 256)
                    {
                        throw new CorruptIndexException("TABLE_COMPRESSED cannot have more than 256 distinct values, input=" + Data);
                    }
                    var decode = new long[size];
                    for (int i = 0; i < decode.Length; i++)
                    {
                        decode[i] = Data.ReadLong();
                    }
                    int formatID = Data.ReadVInt();
                    int bitsPerValue = Data.ReadVInt();
                    PackedInts.Reader ordsReader = PackedInts.GetReaderNoHeader(Data, PackedInts.Format.ById(formatID), entry.PackedIntsVersion, MaxDoc, bitsPerValue);
                    RamBytesUsed_Renamed.AddAndGet(RamUsageEstimator.SizeOf(decode) + ordsReader.RamBytesUsed());
                    return new NumericDocValuesAnonymousInnerClassHelper(decode, ordsReader);

                case DELTA_COMPRESSED:
                    int blockSize = Data.ReadVInt();
                    var reader = new BlockPackedReader(Data, entry.PackedIntsVersion, blockSize, MaxDoc, false);
                    RamBytesUsed_Renamed.AddAndGet(reader.RamBytesUsed());
                    return reader;

                case UNCOMPRESSED:
                    byte[] bytes = new byte[MaxDoc];
                    Data.ReadBytes(bytes, 0, bytes.Length);
                    RamBytesUsed_Renamed.AddAndGet(RamUsageEstimator.SizeOf(bytes));
                    return new NumericDocValuesAnonymousInnerClassHelper2(this, bytes);

                case GCD_COMPRESSED:
                    long min = Data.ReadLong();
                    long mult = Data.ReadLong();
                    int quotientBlockSize = Data.ReadVInt();
                    BlockPackedReader quotientReader = new BlockPackedReader(Data, entry.PackedIntsVersion, quotientBlockSize, MaxDoc, false);
                    RamBytesUsed_Renamed.AddAndGet(quotientReader.RamBytesUsed());
                    return new NumericDocValuesAnonymousInnerClassHelper3(min, mult, quotientReader);

                default:
                    throw new InvalidOperationException();
            }
        }

        private class NumericDocValuesAnonymousInnerClassHelper : NumericDocValues
        {
            private readonly long[] Decode;
            private readonly PackedInts.Reader OrdsReader;

            public NumericDocValuesAnonymousInnerClassHelper(long[] decode, PackedInts.Reader ordsReader)
            {
                this.Decode = decode;
                this.OrdsReader = ordsReader;
            }

            public override long Get(int docID)
            {
                return Decode[(int)OrdsReader.Get(docID)];
            }
        }

        private class NumericDocValuesAnonymousInnerClassHelper2 : NumericDocValues
        {
            private readonly byte[] Bytes;

            public NumericDocValuesAnonymousInnerClassHelper2(Lucene42DocValuesProducer outerInstance, byte[] bytes)
            {
                this.Bytes = bytes;
            }

            public override long Get(int docID)
            {
                return (sbyte)Bytes[docID];
            }
        }

        private class NumericDocValuesAnonymousInnerClassHelper3 : NumericDocValues
        {
            private readonly long Min;
            private readonly long Mult;
            private readonly BlockPackedReader QuotientReader;

            public NumericDocValuesAnonymousInnerClassHelper3(long min, long mult, BlockPackedReader quotientReader)
            {
                this.Min = min;
                this.Mult = mult;
                this.QuotientReader = quotientReader;
            }

            public override long Get(int docID)
            {
                return Min + Mult * QuotientReader.Get(docID);
            }
        }

        public override BinaryDocValues GetBinary(FieldInfo field)
        {
            lock (this)
            {
                BinaryDocValues instance;
                if (!BinaryInstances.TryGetValue(field.Number, out instance))
                {
                    instance = LoadBinary(field);
                    BinaryInstances[field.Number] = instance;
                }
                return instance;
            }
        }

        private BinaryDocValues LoadBinary(FieldInfo field)
        {
            BinaryEntry entry = Binaries[field.Number];
            Data.Seek(entry.Offset);
            PagedBytes bytes = new PagedBytes(16);
            bytes.Copy(Data, entry.NumBytes);
            PagedBytes.Reader bytesReader = bytes.Freeze(true);
            if (entry.MinLength == entry.MaxLength)
            {
                int fixedLength = entry.MinLength;
                RamBytesUsed_Renamed.AddAndGet(bytes.RamBytesUsed());
                return new BinaryDocValuesAnonymousInnerClassHelper(bytesReader, fixedLength);
            }
            else
            {
                MonotonicBlockPackedReader addresses = new MonotonicBlockPackedReader(Data, entry.PackedIntsVersion, entry.BlockSize, MaxDoc, false);
                RamBytesUsed_Renamed.AddAndGet(bytes.RamBytesUsed() + addresses.RamBytesUsed());
                return new BinaryDocValuesAnonymousInnerClassHelper2(bytesReader, addresses);
            }
        }

        private class BinaryDocValuesAnonymousInnerClassHelper : BinaryDocValues
        {
            private readonly PagedBytes.Reader BytesReader;
            private readonly int FixedLength;

            public BinaryDocValuesAnonymousInnerClassHelper(PagedBytes.Reader bytesReader, int fixedLength)
            {
                this.BytesReader = bytesReader;
                this.FixedLength = fixedLength;
            }

            public override void Get(int docID, BytesRef result)
            {
                BytesReader.FillSlice(result, FixedLength * (long)docID, FixedLength);
            }
        }

        private class BinaryDocValuesAnonymousInnerClassHelper2 : BinaryDocValues
        {
            private readonly PagedBytes.Reader BytesReader;
            private readonly MonotonicBlockPackedReader Addresses;

            public BinaryDocValuesAnonymousInnerClassHelper2(PagedBytes.Reader bytesReader, MonotonicBlockPackedReader addresses)
            {
                this.BytesReader = bytesReader;
                this.Addresses = addresses;
            }

            public override void Get(int docID, BytesRef result)
            {
                long startAddress = docID == 0 ? 0 : Addresses.Get(docID - 1);
                long endAddress = Addresses.Get(docID);
                BytesReader.FillSlice(result, startAddress, (int)(endAddress - startAddress));
            }
        }

        public override SortedDocValues GetSorted(FieldInfo field)
        {
            FSTEntry entry = Fsts[field.Number];
            FST<long?> instance;
            lock (this)
            {
                if (!FstInstances.TryGetValue(field.Number, out instance))
                {
                    Data.Seek(entry.Offset);
                    instance = new FST<long?>(Data, PositiveIntOutputs.Singleton);
                    RamBytesUsed_Renamed.AddAndGet(instance.SizeInBytes());
                    FstInstances[field.Number] = instance;
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

            return new SortedDocValuesAnonymousInnerClassHelper(entry, docToOrd, fst, @in, firstArc, scratchArc, scratchInts, fstEnum);
        }

        private class SortedDocValuesAnonymousInnerClassHelper : SortedDocValues
        {
            private readonly FSTEntry Entry;
            private readonly NumericDocValues DocToOrd;
            private readonly FST<long?> Fst;
            private readonly FST.BytesReader @in;
            private readonly FST.Arc<long?> FirstArc;
            private readonly FST.Arc<long?> ScratchArc;
            private readonly IntsRef ScratchInts;
            private readonly BytesRefFSTEnum<long?> FstEnum;

            public SortedDocValuesAnonymousInnerClassHelper(FSTEntry entry, NumericDocValues docToOrd, FST<long?> fst, FST.BytesReader @in, FST.Arc<long?> firstArc, FST.Arc<long?> scratchArc, IntsRef scratchInts, BytesRefFSTEnum<long?> fstEnum)
            {
                this.Entry = entry;
                this.DocToOrd = docToOrd;
                this.Fst = fst;
                this.@in = @in;
                this.FirstArc = firstArc;
                this.ScratchArc = scratchArc;
                this.ScratchInts = scratchInts;
                this.FstEnum = fstEnum;
            }

            public override int GetOrd(int docID)
            {
                return (int)DocToOrd.Get(docID);
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                try
                {
                    @in.Position = 0;
                    Fst.GetFirstArc(FirstArc);
                    IntsRef output = Lucene.Net.Util.Fst.Util.GetByOutput(Fst, ord, @in, FirstArc, ScratchArc, ScratchInts);
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
                    BytesRefFSTEnum.InputOutput<long?> o = FstEnum.SeekCeil(key);
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
                    return (int)Entry.NumOrds;
                }
            }

            public override TermsEnum TermsEnum()
            {
                return new FSTTermsEnum(Fst);
            }
        }

        public override SortedSetDocValues GetSortedSet(FieldInfo field)
        {
            FSTEntry entry = Fsts[field.Number];
            if (entry.NumOrds == 0)
            {
                return DocValues.EMPTY_SORTED_SET; // empty FST!
            }
            FST<long?> instance;
            lock (this)
            {
                if (!FstInstances.TryGetValue(field.Number, out instance))
                {
                    Data.Seek(entry.Offset);
                    instance = new FST<long?>((DataInput)Data, Lucene.Net.Util.Fst.PositiveIntOutputs.Singleton);
                    RamBytesUsed_Renamed.AddAndGet(instance.SizeInBytes());
                    FstInstances[field.Number] = instance;
                }
            }
            BinaryDocValues docToOrds = GetBinary(field);
            FST<long?> fst = instance;

            // per-thread resources
            var @in = fst.BytesReader;
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
            private readonly FSTEntry Entry;
            private readonly BinaryDocValues DocToOrds;
            private readonly FST<long?> Fst;
            private readonly FST.BytesReader @in;
            private readonly FST.Arc<long?> FirstArc;
            private readonly FST.Arc<long?> ScratchArc;
            private readonly IntsRef ScratchInts;
            private readonly BytesRefFSTEnum<long?> FstEnum;
            private readonly BytesRef @ref;
            private readonly ByteArrayDataInput Input;

            public SortedSetDocValuesAnonymousInnerClassHelper(FSTEntry entry, BinaryDocValues docToOrds, FST<long?> fst, FST.BytesReader @in, FST.Arc<long?> firstArc, FST.Arc<long?> scratchArc, IntsRef scratchInts, BytesRefFSTEnum<long?> fstEnum, BytesRef @ref, ByteArrayDataInput input)
            {
                this.Entry = entry;
                this.DocToOrds = docToOrds;
                this.Fst = fst;
                this.@in = @in;
                this.FirstArc = firstArc;
                this.ScratchArc = scratchArc;
                this.ScratchInts = scratchInts;
                this.FstEnum = fstEnum;
                this.@ref = @ref;
                this.Input = input;
            }

            private long currentOrd;

            public override long NextOrd()
            {
                if (Input.Eof())
                {
                    return NO_MORE_ORDS;
                }
                else
                {
                    currentOrd += Input.ReadVLong();
                    return currentOrd;
                }
            }

            public override void SetDocument(int docID)
            {
                DocToOrds.Get(docID, @ref);
                Input.Reset(@ref.Bytes, @ref.Offset, @ref.Length);
                currentOrd = 0;
            }

            public override void LookupOrd(long ord, BytesRef result)
            {
                try
                {
                    @in.Position = 0;
                    Fst.GetFirstArc(FirstArc);
                    IntsRef output = Lucene.Net.Util.Fst.Util.GetByOutput(Fst, ord, @in, FirstArc, ScratchArc, ScratchInts);
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
                    var o = FstEnum.SeekCeil(key);
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
                    return Entry.NumOrds;
                }
            }

            public override TermsEnum TermsEnum()
            {
                return new FSTTermsEnum(Fst);
            }
        }

        public override Bits GetDocsWithField(FieldInfo field)
        {
            if (field.DocValuesType == DocValuesType.SORTED_SET)
            {
                return DocValues.DocsWithValue(GetSortedSet(field), MaxDoc);
            }
            else
            {
                return new Lucene.Net.Util.Bits_MatchAllBits(MaxDoc);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Data.Dispose();
            }
        }

        internal class NumericEntry
        {
            internal long Offset;
            internal sbyte Format;
            internal int PackedIntsVersion;
        }

        internal class BinaryEntry
        {
            internal long Offset;
            internal long NumBytes;
            internal int MinLength;
            internal int MaxLength;
            internal int PackedIntsVersion;
            internal int BlockSize;
        }

        internal class FSTEntry
        {
            internal long Offset;
            internal long NumOrds;
        }

        // exposes FSTEnum directly as a TermsEnum: avoids binary-search next()
        internal class FSTTermsEnum : TermsEnum
        {
            internal readonly BytesRefFSTEnum<long?> @in;

            // this is all for the complicated seek(ord)...
            // maybe we should add a FSTEnum that supports this operation?
            internal readonly FST<long?> Fst;

            internal readonly FST.BytesReader BytesReader;
            internal readonly FST.Arc<long?> FirstArc = new FST.Arc<long?>();
            internal readonly FST.Arc<long?> ScratchArc = new FST.Arc<long?>();
            internal readonly IntsRef ScratchInts = new IntsRef();
            internal readonly BytesRef ScratchBytes = new BytesRef();

            internal FSTTermsEnum(FST<long?> fst)
            {
                this.Fst = fst;
                @in = new BytesRefFSTEnum<long?>(fst);
                BytesReader = fst.BytesReader;
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
                BytesReader.Position = 0;
                Fst.GetFirstArc(FirstArc);
                IntsRef output = Lucene.Net.Util.Fst.Util.GetByOutput(Fst, ord, BytesReader, FirstArc, ScratchArc, ScratchInts);
                ScratchBytes.Bytes = new byte[output.Length];
                ScratchBytes.Offset = 0;
                ScratchBytes.Length = 0;
                Lucene.Net.Util.Fst.Util.ToBytesRef(output, ScratchBytes);
                // TODO: we could do this lazily, better to try to push into FSTEnum though?
                @in.SeekExact(ScratchBytes);
            }

            public override BytesRef Term()
            {
                return @in.Current().Input;
            }

            public override long Ord()
            {
                return @in.Current().Output.Value;
            }

            public override int DocFreq()
            {
                throw new System.NotSupportedException();
            }

            public override long TotalTermFreq()
            {
                throw new System.NotSupportedException();
            }

            public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
            {
                throw new System.NotSupportedException();
            }

            public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
            {
                throw new System.NotSupportedException();
            }
        }
    }
}