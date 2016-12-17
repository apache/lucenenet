using Lucene.Net.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Codecs.Lucene45
{
    using Lucene.Net.Support;

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
    using BytesRef = Lucene.Net.Util.BytesRef;
    using ChecksumIndexInput = Lucene.Net.Store.ChecksumIndexInput;
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using DocValues = Lucene.Net.Index.DocValues;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using LongValues = Lucene.Net.Util.LongValues;
    using MonotonicBlockPackedReader = Lucene.Net.Util.Packed.MonotonicBlockPackedReader;
    using NumericDocValues = Lucene.Net.Index.NumericDocValues;
    using PackedInts = Lucene.Net.Util.Packed.PackedInts;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using RandomAccessOrds = Lucene.Net.Index.RandomAccessOrds;
    using SegmentReadState = Lucene.Net.Index.SegmentReadState;
    using SortedDocValues = Lucene.Net.Index.SortedDocValues;
    using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /// <summary>
    /// reader for <seealso cref="Lucene45DocValuesFormat"/> </summary>
    public class Lucene45DocValuesProducer : DocValuesProducer, IDisposable
    {
        private readonly IDictionary<int, NumericEntry> Numerics;
        private readonly IDictionary<int, BinaryEntry> Binaries;
        private readonly IDictionary<int, SortedSetEntry> SortedSets;
        private readonly IDictionary<int, NumericEntry> Ords;
        private readonly IDictionary<int, NumericEntry> OrdIndexes;
        private readonly AtomicLong RamBytesUsed_Renamed;
        private readonly IndexInput Data;
        private readonly int MaxDoc;
        private readonly int Version;

        // memory-resident structures
        private readonly IDictionary<int, MonotonicBlockPackedReader> AddressInstances = new Dictionary<int, MonotonicBlockPackedReader>();

        private readonly IDictionary<int, MonotonicBlockPackedReader> OrdIndexInstances = new Dictionary<int, MonotonicBlockPackedReader>();

        /// <summary>
        /// expert: instantiates a new reader </summary>
        protected internal Lucene45DocValuesProducer(SegmentReadState state, string dataCodec, string dataExtension, string metaCodec, string metaExtension)
        {
            string metaName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, metaExtension);
            // read in the entries from the metadata file.
            ChecksumIndexInput @in = state.Directory.OpenChecksumInput(metaName, state.Context);
            this.MaxDoc = state.SegmentInfo.DocCount;
            bool success = false;
            try
            {
                Version = CodecUtil.CheckHeader(@in, metaCodec, Lucene45DocValuesFormat.VERSION_START, Lucene45DocValuesFormat.VERSION_CURRENT);
                Numerics = new Dictionary<int, NumericEntry>();
                Ords = new Dictionary<int, NumericEntry>();
                OrdIndexes = new Dictionary<int, NumericEntry>();
                Binaries = new Dictionary<int, BinaryEntry>();
                SortedSets = new Dictionary<int, SortedSetEntry>();
                ReadFields(@in, state.FieldInfos);

                if (Version >= Lucene45DocValuesFormat.VERSION_CHECKSUM)
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
                int version2 = CodecUtil.CheckHeader(Data, dataCodec, Lucene45DocValuesFormat.VERSION_START, Lucene45DocValuesFormat.VERSION_CURRENT);
                if (Version != version2)
                {
                    throw new Exception("Format versions mismatch");
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

            RamBytesUsed_Renamed = new AtomicLong(RamUsageEstimator.ShallowSizeOfInstance(this.GetType()));
        }

        private void ReadSortedField(int fieldNumber, IndexInput meta, FieldInfos infos)
        {
            // sorted = binary + numeric
            if (meta.ReadVInt() != fieldNumber)
            {
                throw new Exception("sorted entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
            }
            if (meta.ReadByte() != Lucene45DocValuesFormat.BINARY)
            {
                throw new Exception("sorted entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
            }
            BinaryEntry b = ReadBinaryEntry(meta);
            Binaries[fieldNumber] = b;

            if (meta.ReadVInt() != fieldNumber)
            {
                throw new Exception("sorted entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
            }
            if (meta.ReadByte() != Lucene45DocValuesFormat.NUMERIC)
            {
                throw new Exception("sorted entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
            }
            NumericEntry n = ReadNumericEntry(meta);
            Ords[fieldNumber] = n;
        }

        private void ReadSortedSetFieldWithAddresses(int fieldNumber, IndexInput meta, FieldInfos infos)
        {
            // sortedset = binary + numeric (addresses) + ordIndex
            if (meta.ReadVInt() != fieldNumber)
            {
                throw new Exception("sortedset entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
            }
            if (meta.ReadByte() != Lucene45DocValuesFormat.BINARY)
            {
                throw new Exception("sortedset entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
            }
            BinaryEntry b = ReadBinaryEntry(meta);
            Binaries[fieldNumber] = b;

            if (meta.ReadVInt() != fieldNumber)
            {
                throw new Exception("sortedset entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
            }
            if (meta.ReadByte() != Lucene45DocValuesFormat.NUMERIC)
            {
                throw new Exception("sortedset entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
            }
            NumericEntry n1 = ReadNumericEntry(meta);
            Ords[fieldNumber] = n1;

            if (meta.ReadVInt() != fieldNumber)
            {
                throw new Exception("sortedset entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
            }
            if (meta.ReadByte() != Lucene45DocValuesFormat.NUMERIC)
            {
                throw new Exception("sortedset entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
            }
            NumericEntry n2 = ReadNumericEntry(meta);
            OrdIndexes[fieldNumber] = n2;
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
                    throw new Exception("Invalid field number: " + fieldNumber + " (resource=" + meta + ")");
                }
                byte type = meta.ReadByte();
                if (type == Lucene45DocValuesFormat.NUMERIC)
                {
                    Numerics[fieldNumber] = ReadNumericEntry(meta);
                }
                else if (type == Lucene45DocValuesFormat.BINARY)
                {
                    BinaryEntry b = ReadBinaryEntry(meta);
                    Binaries[fieldNumber] = b;
                }
                else if (type == Lucene45DocValuesFormat.SORTED)
                {
                    ReadSortedField(fieldNumber, meta, infos);
                }
                else if (type == Lucene45DocValuesFormat.SORTED_SET)
                {
                    SortedSetEntry ss = ReadSortedSetEntry(meta);
                    SortedSets[fieldNumber] = ss;
                    if (ss.Format == Lucene45DocValuesConsumer.SORTED_SET_WITH_ADDRESSES)
                    {
                        ReadSortedSetFieldWithAddresses(fieldNumber, meta, infos);
                    }
                    else if (ss.Format == Lucene45DocValuesConsumer.SORTED_SET_SINGLE_VALUED_SORTED)
                    {
                        if (meta.ReadVInt() != fieldNumber)
                        {
                            throw new Exception("sortedset entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
                        }
                        if (meta.ReadByte() != Lucene45DocValuesFormat.SORTED)
                        {
                            throw new Exception("sortedset entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
                        }
                        ReadSortedField(fieldNumber, meta, infos);
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
                else
                {
                    throw new Exception("invalid type: " + type + ", resource=" + meta);
                }
                fieldNumber = meta.ReadVInt();
            }
        }

        internal static NumericEntry ReadNumericEntry(IndexInput meta)
        {
            NumericEntry entry = new NumericEntry();
            entry.Format = meta.ReadVInt();
            entry.MissingOffset = meta.ReadLong();
            entry.PackedIntsVersion = meta.ReadVInt();
            entry.Offset = meta.ReadLong();
            entry.Count = meta.ReadVLong();
            entry.BlockSize = meta.ReadVInt();
            switch (entry.Format)
            {
                case Lucene45DocValuesConsumer.GCD_COMPRESSED:
                    entry.MinValue = meta.ReadLong();
                    entry.Gcd = meta.ReadLong();
                    break;

                case Lucene45DocValuesConsumer.TABLE_COMPRESSED:
                    if (entry.Count > int.MaxValue)
                    {
                        throw new Exception("Cannot use TABLE_COMPRESSED with more than MAX_VALUE values, input=" + meta);
                    }
                    int uniqueValues = meta.ReadVInt();
                    if (uniqueValues > 256)
                    {
                        throw new Exception("TABLE_COMPRESSED cannot have more than 256 distinct values, input=" + meta);
                    }
                    entry.Table = new long[uniqueValues];
                    for (int i = 0; i < uniqueValues; ++i)
                    {
                        entry.Table[i] = meta.ReadLong();
                    }
                    break;

                case Lucene45DocValuesConsumer.DELTA_COMPRESSED:
                    break;

                default:
                    throw new Exception("Unknown format: " + entry.Format + ", input=" + meta);
            }
            return entry;
        }

        internal static BinaryEntry ReadBinaryEntry(IndexInput meta)
        {
            BinaryEntry entry = new BinaryEntry();
            entry.Format = meta.ReadVInt();
            entry.MissingOffset = meta.ReadLong();
            entry.MinLength = meta.ReadVInt();
            entry.MaxLength = meta.ReadVInt();
            entry.Count = meta.ReadVLong();
            entry.Offset = meta.ReadLong();
            switch (entry.Format)
            {
                case Lucene45DocValuesConsumer.BINARY_FIXED_UNCOMPRESSED:
                    break;

                case Lucene45DocValuesConsumer.BINARY_PREFIX_COMPRESSED:
                    entry.AddressInterval = meta.ReadVInt();
                    entry.AddressesOffset = meta.ReadLong();
                    entry.PackedIntsVersion = meta.ReadVInt();
                    entry.BlockSize = meta.ReadVInt();
                    break;

                case Lucene45DocValuesConsumer.BINARY_VARIABLE_UNCOMPRESSED:
                    entry.AddressesOffset = meta.ReadLong();
                    entry.PackedIntsVersion = meta.ReadVInt();
                    entry.BlockSize = meta.ReadVInt();
                    break;

                default:
                    throw new Exception("Unknown format: " + entry.Format + ", input=" + meta);
            }
            return entry;
        }

        internal virtual SortedSetEntry ReadSortedSetEntry(IndexInput meta)
        {
            SortedSetEntry entry = new SortedSetEntry();
            if (Version >= Lucene45DocValuesFormat.VERSION_SORTED_SET_SINGLE_VALUE_OPTIMIZED)
            {
                entry.Format = meta.ReadVInt();
            }
            else
            {
                entry.Format = Lucene45DocValuesConsumer.SORTED_SET_WITH_ADDRESSES;
            }
            if (entry.Format != Lucene45DocValuesConsumer.SORTED_SET_SINGLE_VALUED_SORTED && entry.Format != Lucene45DocValuesConsumer.SORTED_SET_WITH_ADDRESSES)
            {
                throw new Exception("Unknown format: " + entry.Format + ", input=" + meta);
            }
            return entry;
        }

        public override NumericDocValues GetNumeric(FieldInfo field)
        {
            NumericEntry entry = Numerics[field.Number];
            return GetNumeric(entry);
        }

        public override long RamBytesUsed()
        {
            return RamBytesUsed_Renamed.Get();
        }

        public override void CheckIntegrity()
        {
            if (Version >= Lucene45DocValuesFormat.VERSION_CHECKSUM)
            {
                CodecUtil.ChecksumEntireFile(Data);
            }
        }

        internal virtual LongValues GetNumeric(NumericEntry entry)
        {
            IndexInput data = (IndexInput)this.Data.Clone();
            data.Seek(entry.Offset);

            switch (entry.Format)
            {
                case Lucene45DocValuesConsumer.DELTA_COMPRESSED:
                    BlockPackedReader reader = new BlockPackedReader(data, entry.PackedIntsVersion, entry.BlockSize, entry.Count, true);
                    return reader;

                case Lucene45DocValuesConsumer.GCD_COMPRESSED:
                    long min = entry.MinValue;
                    long mult = entry.Gcd;
                    BlockPackedReader quotientReader = new BlockPackedReader(data, entry.PackedIntsVersion, entry.BlockSize, entry.Count, true);
                    return new LongValuesAnonymousInnerClassHelper(this, min, mult, quotientReader);

                case Lucene45DocValuesConsumer.TABLE_COMPRESSED:
                    long[] table = entry.Table;
                    int bitsRequired = PackedInts.BitsRequired(table.Length - 1);
                    PackedInts.Reader ords = PackedInts.GetDirectReaderNoHeader(data, PackedInts.Format.PACKED, entry.PackedIntsVersion, (int)entry.Count, bitsRequired);
                    return new LongValuesAnonymousInnerClassHelper2(this, table, ords);

                default:
                    throw new Exception();
            }
        }

        private class LongValuesAnonymousInnerClassHelper : LongValues
        {
            private readonly Lucene45DocValuesProducer OuterInstance;

            private long Min;
            private long Mult;
            private BlockPackedReader QuotientReader;

            public LongValuesAnonymousInnerClassHelper(Lucene45DocValuesProducer outerInstance, long min, long mult, BlockPackedReader quotientReader)
            {
                this.OuterInstance = outerInstance;
                this.Min = min;
                this.Mult = mult;
                this.QuotientReader = quotientReader;
            }

            public override long Get(long id)
            {
                return Min + Mult * QuotientReader.Get(id);
            }
        }

        private class LongValuesAnonymousInnerClassHelper2 : LongValues
        {
            private readonly Lucene45DocValuesProducer OuterInstance;

            private long[] Table;
            private PackedInts.Reader Ords;

            public LongValuesAnonymousInnerClassHelper2(Lucene45DocValuesProducer outerInstance, long[] table, PackedInts.Reader ords)
            {
                this.OuterInstance = outerInstance;
                this.Table = table;
                this.Ords = ords;
            }

            public override long Get(long id)
            {
                return Table[(int)Ords.Get((int)id)];
            }
        }

        public override BinaryDocValues GetBinary(FieldInfo field)
        {
            BinaryEntry bytes = Binaries[field.Number];
            switch (bytes.Format)
            {
                case Lucene45DocValuesConsumer.BINARY_FIXED_UNCOMPRESSED:
                    return GetFixedBinary(field, bytes);

                case Lucene45DocValuesConsumer.BINARY_VARIABLE_UNCOMPRESSED:
                    return GetVariableBinary(field, bytes);

                case Lucene45DocValuesConsumer.BINARY_PREFIX_COMPRESSED:
                    return GetCompressedBinary(field, bytes);

                default:
                    throw new Exception();
            }
        }

        private BinaryDocValues GetFixedBinary(FieldInfo field, BinaryEntry bytes)
        {
            IndexInput data = (IndexInput)this.Data.Clone();

            return new LongBinaryDocValuesAnonymousInnerClassHelper(this, bytes, data);
        }

        private class LongBinaryDocValuesAnonymousInnerClassHelper : LongBinaryDocValues
        {
            private readonly Lucene45DocValuesProducer OuterInstance;

            private Lucene45DocValuesProducer.BinaryEntry Bytes;
            private IndexInput Data;

            public LongBinaryDocValuesAnonymousInnerClassHelper(Lucene45DocValuesProducer outerInstance, Lucene45DocValuesProducer.BinaryEntry bytes, IndexInput data)
            {
                this.OuterInstance = outerInstance;
                this.Bytes = bytes;
                this.Data = data;
            }

            public override void Get(long id, BytesRef result)
            {
                long address = Bytes.Offset + id * Bytes.MaxLength;
                try
                {
                    Data.Seek(address);
                    // NOTE: we could have one buffer, but various consumers (e.g. FieldComparatorSource)
                    // assume "they" own the bytes after calling this!
                    var buffer = new byte[Bytes.MaxLength];
                    Data.ReadBytes(buffer, 0, buffer.Length);
                    result.Bytes = buffer;
                    result.Offset = 0;
                    result.Length = buffer.Length;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// returns an address instance for variable-length binary values.
        ///  @lucene.internal
        /// </summary>
        protected internal virtual MonotonicBlockPackedReader GetAddressInstance(IndexInput data, FieldInfo field, BinaryEntry bytes)
        {
            MonotonicBlockPackedReader addresses;
            lock (AddressInstances)
            {
                MonotonicBlockPackedReader addrInstance;
                if (!AddressInstances.TryGetValue(field.Number, out addrInstance))
                {
                    data.Seek(bytes.AddressesOffset);
                    addrInstance = new MonotonicBlockPackedReader(data, bytes.PackedIntsVersion, bytes.BlockSize, bytes.Count, false);
                    AddressInstances[field.Number] = addrInstance;
                    RamBytesUsed_Renamed.AddAndGet(addrInstance.RamBytesUsed() + RamUsageEstimator.NUM_BYTES_INT);
                }
                addresses = addrInstance;
            }
            return addresses;
        }

        private BinaryDocValues GetVariableBinary(FieldInfo field, BinaryEntry bytes)
        {
            IndexInput data = (IndexInput)this.Data.Clone();

            MonotonicBlockPackedReader addresses = GetAddressInstance(data, field, bytes);

            return new LongBinaryDocValuesAnonymousInnerClassHelper2(this, bytes, data, addresses);
        }

        private class LongBinaryDocValuesAnonymousInnerClassHelper2 : LongBinaryDocValues
        {
            private readonly Lucene45DocValuesProducer OuterInstance;

            private Lucene45DocValuesProducer.BinaryEntry Bytes;
            private IndexInput Data;
            private MonotonicBlockPackedReader Addresses;

            public LongBinaryDocValuesAnonymousInnerClassHelper2(Lucene45DocValuesProducer outerInstance, Lucene45DocValuesProducer.BinaryEntry bytes, IndexInput data, MonotonicBlockPackedReader addresses)
            {
                this.OuterInstance = outerInstance;
                this.Bytes = bytes;
                this.Data = data;
                this.Addresses = addresses;
            }

            public override void Get(long id, BytesRef result)
            {
                long startAddress = Bytes.Offset + (id == 0 ? 0 : Addresses.Get(id - 1));
                long endAddress = Bytes.Offset + Addresses.Get(id);
                int length = (int)(endAddress - startAddress);
                try
                {
                    Data.Seek(startAddress);
                    // NOTE: we could have one buffer, but various consumers (e.g. FieldComparatorSource)
                    // assume "they" own the bytes after calling this!
                    var buffer = new byte[length];
                    Data.ReadBytes(buffer, 0, buffer.Length);
                    result.Bytes = buffer;
                    result.Offset = 0;
                    result.Length = length;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// returns an address instance for prefix-compressed binary values.
        /// @lucene.internal
        /// </summary>
        protected internal virtual MonotonicBlockPackedReader GetIntervalInstance(IndexInput data, FieldInfo field, BinaryEntry bytes)
        {
            MonotonicBlockPackedReader addresses;
            long interval = bytes.AddressInterval;
            lock (AddressInstances)
            {
                MonotonicBlockPackedReader addrInstance;
                if (!AddressInstances.TryGetValue(field.Number, out addrInstance))
                {
                    data.Seek(bytes.AddressesOffset);
                    long size;
                    if (bytes.Count % interval == 0)
                    {
                        size = bytes.Count / interval;
                    }
                    else
                    {
                        size = 1L + bytes.Count / interval;
                    }
                    addrInstance = new MonotonicBlockPackedReader(data, bytes.PackedIntsVersion, bytes.BlockSize, size, false);
                    AddressInstances[field.Number] = addrInstance;
                    RamBytesUsed_Renamed.AddAndGet(addrInstance.RamBytesUsed() + RamUsageEstimator.NUM_BYTES_INT);
                }
                addresses = addrInstance;
            }
            return addresses;
        }

        private BinaryDocValues GetCompressedBinary(FieldInfo field, BinaryEntry bytes)
        {
            IndexInput data = (IndexInput)this.Data.Clone();

            MonotonicBlockPackedReader addresses = GetIntervalInstance(data, field, bytes);

            return new CompressedBinaryDocValues(bytes, addresses, data);
        }

        public override SortedDocValues GetSorted(FieldInfo field)
        {
            int valueCount = (int)Binaries[field.Number].Count;
            BinaryDocValues binary = GetBinary(field);
            NumericEntry entry = Ords[field.Number];
            IndexInput data = (IndexInput)this.Data.Clone();
            data.Seek(entry.Offset);
            BlockPackedReader ordinals = new BlockPackedReader(data, entry.PackedIntsVersion, entry.BlockSize, entry.Count, true);

            return new SortedDocValuesAnonymousInnerClassHelper(this, valueCount, binary, ordinals);
        }

        private class SortedDocValuesAnonymousInnerClassHelper : SortedDocValues
        {
            private readonly Lucene45DocValuesProducer OuterInstance;

            private int valueCount;
            private BinaryDocValues Binary;
            private BlockPackedReader Ordinals;

            public SortedDocValuesAnonymousInnerClassHelper(Lucene45DocValuesProducer outerInstance, int valueCount, BinaryDocValues binary, BlockPackedReader ordinals)
            {
                this.OuterInstance = outerInstance;
                this.valueCount = valueCount;
                this.Binary = binary;
                this.Ordinals = ordinals;
            }

            public override int GetOrd(int docID)
            {
                return (int)Ordinals.Get(docID);
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                Binary.Get(ord, result);
            }

            public override int ValueCount
            {
                get
                {
                    return valueCount;
                }
            }

            public override int LookupTerm(BytesRef key)
            {
                if (Binary is CompressedBinaryDocValues)
                {
                    return (int)((CompressedBinaryDocValues)Binary).LookupTerm(key);
                }
                else
                {
                    return base.LookupTerm(key);
                }
            }

            public override TermsEnum TermsEnum()
            {
                if (Binary is CompressedBinaryDocValues)
                {
                    return ((CompressedBinaryDocValues)Binary).GetTermsEnum();
                }
                else
                {
                    return base.TermsEnum();
                }
            }
        }

        /// <summary>
        /// returns an address instance for sortedset ordinal lists
        /// @lucene.internal
        /// </summary>
        protected internal virtual MonotonicBlockPackedReader GetOrdIndexInstance(IndexInput data, FieldInfo field, NumericEntry entry)
        {
            MonotonicBlockPackedReader ordIndex;
            lock (OrdIndexInstances)
            {
                MonotonicBlockPackedReader ordIndexInstance;
                if (!OrdIndexInstances.TryGetValue(field.Number, out ordIndexInstance))
                {
                    data.Seek(entry.Offset);
                    ordIndexInstance = new MonotonicBlockPackedReader(data, entry.PackedIntsVersion, entry.BlockSize, entry.Count, false);
                    OrdIndexInstances[field.Number] = ordIndexInstance;
                    RamBytesUsed_Renamed.AddAndGet(ordIndexInstance.RamBytesUsed() + RamUsageEstimator.NUM_BYTES_INT);
                }
                ordIndex = ordIndexInstance;
            }
            return ordIndex;
        }

        public override SortedSetDocValues GetSortedSet(FieldInfo field)
        {
            SortedSetEntry ss = SortedSets[field.Number];
            if (ss.Format == Lucene45DocValuesConsumer.SORTED_SET_SINGLE_VALUED_SORTED)
            {
                SortedDocValues values = GetSorted(field);
                return DocValues.Singleton(values);
            }
            else if (ss.Format != Lucene45DocValuesConsumer.SORTED_SET_WITH_ADDRESSES)
            {
                throw new Exception();
            }

            IndexInput data = (IndexInput)this.Data.Clone();
            long valueCount = Binaries[field.Number].Count;
            // we keep the byte[]s and list of ords on disk, these could be large
            LongBinaryDocValues binary = (LongBinaryDocValues)GetBinary(field);
            LongValues ordinals = GetNumeric(Ords[field.Number]);
            // but the addresses to the ord stream are in RAM
            MonotonicBlockPackedReader ordIndex = GetOrdIndexInstance(data, field, OrdIndexes[field.Number]);

            return new RandomAccessOrdsAnonymousInnerClassHelper(this, valueCount, binary, ordinals, ordIndex);
        }

        private class RandomAccessOrdsAnonymousInnerClassHelper : RandomAccessOrds
        {
            private readonly Lucene45DocValuesProducer OuterInstance;

            private long valueCount;
            private Lucene45DocValuesProducer.LongBinaryDocValues Binary;
            private LongValues Ordinals;
            private MonotonicBlockPackedReader OrdIndex;

            public RandomAccessOrdsAnonymousInnerClassHelper(Lucene45DocValuesProducer outerInstance, long valueCount, Lucene45DocValuesProducer.LongBinaryDocValues binary, LongValues ordinals, MonotonicBlockPackedReader ordIndex)
            {
                this.OuterInstance = outerInstance;
                this.valueCount = valueCount;
                this.Binary = binary;
                this.Ordinals = ordinals;
                this.OrdIndex = ordIndex;
            }

            internal long startOffset;
            internal long offset;
            internal long endOffset;

            public override long NextOrd()
            {
                if (offset == endOffset)
                {
                    return NO_MORE_ORDS;
                }
                else
                {
                    long ord = Ordinals.Get(offset);
                    offset++;
                    return ord;
                }
            }

            public override int Document
            {
                set
                {
                    startOffset = offset = (value == 0 ? 0 : OrdIndex.Get(value - 1));
                    endOffset = OrdIndex.Get(value);
                }
            }

            public override void LookupOrd(long ord, BytesRef result)
            {
                Binary.Get(ord, result);
            }

            public override long ValueCount
            {
                get
                {
                    return valueCount;
                }
            }

            public override long LookupTerm(BytesRef key)
            {
                if (Binary is CompressedBinaryDocValues)
                {
                    return ((CompressedBinaryDocValues)Binary).LookupTerm(key);
                }
                else
                {
                    return base.LookupTerm(key);
                }
            }

            public override TermsEnum TermsEnum()
            {
                if (Binary is CompressedBinaryDocValues)
                {
                    return ((CompressedBinaryDocValues)Binary).GetTermsEnum();
                }
                else
                {
                    return base.TermsEnum();
                }
            }

            public override long OrdAt(int index)
            {
                return Ordinals.Get(startOffset + index);
            }

            public override int Cardinality()
            {
                return (int)(endOffset - startOffset);
            }
        }

        private Bits GetMissingBits(long offset)
        {
            if (offset == -1)
            {
                return new Bits_MatchAllBits(MaxDoc);
            }
            else
            {
                IndexInput @in = (IndexInput)Data.Clone();
                return new BitsAnonymousInnerClassHelper(this, offset, @in);
            }
        }

        private class BitsAnonymousInnerClassHelper : Bits
        {
            private readonly Lucene45DocValuesProducer OuterInstance;

            private long Offset;
            private IndexInput @in;

            public BitsAnonymousInnerClassHelper(Lucene45DocValuesProducer outerInstance, long offset, IndexInput @in)
            {
                this.OuterInstance = outerInstance;
                this.Offset = offset;
                this.@in = @in;
            }

            public virtual bool Get(int index)
            {
                try
                {
                    @in.Seek(Offset + (index >> 3));
                    return (@in.ReadByte() & (1 << (index & 7))) != 0;
                }
                catch (Exception e)
                {
                    throw;
                }
            }

            public virtual int Length()
            {
                return OuterInstance.MaxDoc;
            }
        }

        public override Bits GetDocsWithField(FieldInfo field)
        {
            switch (field.DocValuesType)
            {
                case FieldInfo.DocValuesType_e.SORTED_SET:
                    return DocValues.DocsWithValue(GetSortedSet(field), MaxDoc);

                case FieldInfo.DocValuesType_e.SORTED:
                    return DocValues.DocsWithValue(GetSorted(field), MaxDoc);

                case FieldInfo.DocValuesType_e.BINARY:
                    BinaryEntry be = Binaries[field.Number];
                    return GetMissingBits(be.MissingOffset);

                case FieldInfo.DocValuesType_e.NUMERIC:
                    NumericEntry ne = Numerics[field.Number];
                    return GetMissingBits(ne.MissingOffset);

                default:
                    throw new InvalidOperationException();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Data.Dispose();
        }

        /// <summary>
        /// metadata entry for a numeric docvalues field </summary>
        protected internal class NumericEntry
        {
            internal NumericEntry()
            {
            }

            /// <summary>
            /// offset to the bitset representing docsWithField, or -1 if no documents have missing values </summary>
            internal long MissingOffset;

            /// <summary>
            /// offset to the actual numeric values </summary>
            public long Offset { get; set; }

            internal int Format;

            /// <summary>
            /// packed ints version used to encode these numerics </summary>
            public int PackedIntsVersion { get; set; }

            /// <summary>
            /// count of values written </summary>
            public long Count { get; set; }

            /// <summary>
            /// packed ints blocksize </summary>
            public int BlockSize { get; set; }

            internal long MinValue;
            internal long Gcd;
            internal long[] Table;
        }

        /// <summary>
        /// metadata entry for a binary docvalues field </summary>
        protected internal class BinaryEntry
        {
            internal BinaryEntry()
            {
            }

            /// <summary>
            /// offset to the bitset representing docsWithField, or -1 if no documents have missing values </summary>
            internal long MissingOffset;

            /// <summary>
            /// offset to the actual binary values </summary>
            internal long Offset;

            internal int Format;

            /// <summary>
            /// count of values written </summary>
            public long Count { get; set; }

            internal int MinLength;
            internal int MaxLength;

            /// <summary>
            /// offset to the addressing data that maps a value to its slice of the byte[] </summary>
            public long AddressesOffset { get; set; }

            /// <summary>
            /// interval of shared prefix chunks (when using prefix-compressed binary) </summary>
            public long AddressInterval { get; set; }

            /// <summary>
            /// packed ints version used to encode addressing information </summary>
            public int PackedIntsVersion { get; set; }

            /// <summary>
            /// packed ints blocksize </summary>
            public int BlockSize { get; set; }
        }

        /// <summary>
        /// metadata entry for a sorted-set docvalues field </summary>
        protected internal class SortedSetEntry
        {
            internal SortedSetEntry()
            {
            }

            internal int Format { get; set; }
        }

        // internally we compose complex dv (sorted/sortedset) from other ones
        internal abstract class LongBinaryDocValues : BinaryDocValues
        {
            public override sealed void Get(int docID, BytesRef result)
            {
                Get((long)docID, result);
            }

            public abstract void Get(long id, BytesRef Result);
        }

        // in the compressed case, we add a few additional operations for
        // more efficient reverse lookup and enumeration
        internal class CompressedBinaryDocValues : LongBinaryDocValues
        {
            internal readonly BinaryEntry Bytes;
            internal readonly long Interval;
            internal readonly long NumValues;
            internal readonly long NumIndexValues;
            internal readonly MonotonicBlockPackedReader Addresses;
            internal readonly IndexInput Data;
            internal readonly TermsEnum TermsEnum_Renamed;

            public CompressedBinaryDocValues(BinaryEntry bytes, MonotonicBlockPackedReader addresses, IndexInput data)
            {
                this.Bytes = bytes;
                this.Interval = bytes.AddressInterval;
                this.Addresses = addresses;
                this.Data = data;
                this.NumValues = bytes.Count;
                this.NumIndexValues = addresses.Size();
                this.TermsEnum_Renamed = GetTermsEnum(data);
            }

            public override void Get(long id, BytesRef result)
            {
                try
                {
                    TermsEnum_Renamed.SeekExact(id);
                    BytesRef term = TermsEnum_Renamed.Term();
                    result.Bytes = term.Bytes;
                    result.Offset = term.Offset;
                    result.Length = term.Length;
                }
                catch (Exception e)
                {
                    throw;
                }
            }

            internal virtual long LookupTerm(BytesRef key)
            {
                try
                {
                    TermsEnum.SeekStatus status = TermsEnum_Renamed.SeekCeil(key);
                    if (status == TermsEnum.SeekStatus.END)
                    {
                        return -NumValues - 1;
                    }
                    else if (status == TermsEnum.SeekStatus.FOUND)
                    {
                        return TermsEnum_Renamed.Ord();
                    }
                    else
                    {
                        return -TermsEnum_Renamed.Ord() - 1;
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }

            internal virtual TermsEnum GetTermsEnum()
            {
                try
                {
                    return GetTermsEnum((IndexInput)Data.Clone());
                }
                catch (Exception)
                {
                    throw;
                }
            }

            internal virtual TermsEnum GetTermsEnum(IndexInput input)
            {
                input.Seek(Bytes.Offset);

                return new TermsEnumAnonymousInnerClassHelper(this, input);
            }

            private class TermsEnumAnonymousInnerClassHelper : TermsEnum
            {
                private readonly CompressedBinaryDocValues OuterInstance;

                private IndexInput Input;

                public TermsEnumAnonymousInnerClassHelper(CompressedBinaryDocValues outerInstance, IndexInput input)
                {
                    this.OuterInstance = outerInstance;
                    this.Input = input;
                    currentOrd = -1;
                    termBuffer = new BytesRef(outerInstance.Bytes.MaxLength < 0 ? 0 : outerInstance.Bytes.MaxLength);
                    term = new BytesRef();
                }

                private long currentOrd;

                // TODO: maxLength is negative when all terms are merged away...
                private readonly BytesRef termBuffer;

                private readonly BytesRef term;

                public override BytesRef Next()
                {
                    if (DoNext() == null)
                    {
                        return null;
                    }
                    else
                    {
                        SetTerm();
                        return term;
                    }
                }

                private BytesRef DoNext()
                {
                    if (++currentOrd >= OuterInstance.NumValues)
                    {
                        return null;
                    }
                    else
                    {
                        int start = Input.ReadVInt();
                        int suffix = Input.ReadVInt();
                        Input.ReadBytes(termBuffer.Bytes, start, suffix);
                        termBuffer.Length = start + suffix;
                        return termBuffer;
                    }
                }

                public override TermsEnum.SeekStatus SeekCeil(BytesRef text)
                {
                    // binary-search just the index values to find the block,
                    // then scan within the block
                    long low = 0;
                    long high = OuterInstance.NumIndexValues - 1;

                    while (low <= high)
                    {
                        long mid = (int)((uint)(low + high) >> 1);
                        DoSeek(mid * OuterInstance.Interval);
                        int cmp = termBuffer.CompareTo(text);

                        if (cmp < 0)
                        {
                            low = mid + 1;
                        }
                        else if (cmp > 0)
                        {
                            high = mid - 1;
                        }
                        else
                        {
                            // we got lucky, found an indexed term
                            SetTerm();
                            return TermsEnum.SeekStatus.FOUND;
                        }
                    }

                    if (OuterInstance.NumIndexValues == 0)
                    {
                        return TermsEnum.SeekStatus.END;
                    }

                    // block before insertion point
                    long block = low - 1;
                    DoSeek(block < 0 ? -1 : block * OuterInstance.Interval);

                    while (DoNext() != null)
                    {
                        int cmp = termBuffer.CompareTo(text);
                        if (cmp == 0)
                        {
                            SetTerm();
                            return TermsEnum.SeekStatus.FOUND;
                        }
                        else if (cmp > 0)
                        {
                            SetTerm();
                            return TermsEnum.SeekStatus.NOT_FOUND;
                        }
                    }

                    return TermsEnum.SeekStatus.END;
                }

                public override void SeekExact(long ord)
                {
                    DoSeek(ord);
                    SetTerm();
                }

                private void DoSeek(long ord)
                {
                    long block = ord / OuterInstance.Interval;

                    if (ord >= currentOrd && block == currentOrd / OuterInstance.Interval)
                    {
                        // seek within current block
                    }
                    else
                    {
                        // position before start of block
                        currentOrd = ord - ord % OuterInstance.Interval - 1;
                        Input.Seek(OuterInstance.Bytes.Offset + OuterInstance.Addresses.Get(block));
                    }

                    while (currentOrd < ord)
                    {
                        DoNext();
                    }
                }

                private void SetTerm()
                {
                    // TODO: is there a cleaner way
                    term.Bytes = new byte[termBuffer.Length];
                    term.Offset = 0;
                    term.CopyBytes(termBuffer);
                }

                public override BytesRef Term()
                {
                    return term;
                }

                public override long Ord()
                {
                    return currentOrd;
                }

                public override IComparer<BytesRef> Comparator
                {
                    get
                    {
                        return BytesRef.UTF8SortedAsUnicodeComparer;
                    }
                }

                public override int DocFreq()
                {
                    throw new System.NotSupportedException();
                }

                public override long TotalTermFreq()
                {
                    return -1;
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
}