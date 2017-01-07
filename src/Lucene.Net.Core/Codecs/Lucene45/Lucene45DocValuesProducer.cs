using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Codecs.Lucene45
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
    using BytesRef = Lucene.Net.Util.BytesRef;
    using ChecksumIndexInput = Lucene.Net.Store.ChecksumIndexInput;
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using DocValues = Lucene.Net.Index.DocValues;
    using DocValuesType = Lucene.Net.Index.DocValuesType;
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
        private readonly IDictionary<int, NumericEntry> numerics;
        private readonly IDictionary<int, BinaryEntry> binaries;
        private readonly IDictionary<int, SortedSetEntry> sortedSets;
        private readonly IDictionary<int, NumericEntry> ords;
        private readonly IDictionary<int, NumericEntry> ordIndexes;
        private readonly AtomicLong ramBytesUsed;
        private readonly IndexInput data;
        private readonly int maxDoc;
        private readonly int version;

        // memory-resident structures
        private readonly IDictionary<int, MonotonicBlockPackedReader> addressInstances = new Dictionary<int, MonotonicBlockPackedReader>();

        private readonly IDictionary<int, MonotonicBlockPackedReader> ordIndexInstances = new Dictionary<int, MonotonicBlockPackedReader>();

        /// <summary>
        /// expert: instantiates a new reader </summary>
        protected internal Lucene45DocValuesProducer(SegmentReadState state, string dataCodec, string dataExtension, string metaCodec, string metaExtension)
        {
            string metaName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, metaExtension);
            // read in the entries from the metadata file.
            ChecksumIndexInput @in = state.Directory.OpenChecksumInput(metaName, state.Context);
            this.maxDoc = state.SegmentInfo.DocCount;
            bool success = false;
            try
            {
                version = CodecUtil.CheckHeader(@in, metaCodec, Lucene45DocValuesFormat.VERSION_START, Lucene45DocValuesFormat.VERSION_CURRENT);
                numerics = new Dictionary<int, NumericEntry>();
                ords = new Dictionary<int, NumericEntry>();
                ordIndexes = new Dictionary<int, NumericEntry>();
                binaries = new Dictionary<int, BinaryEntry>();
                sortedSets = new Dictionary<int, SortedSetEntry>();
                ReadFields(@in, state.FieldInfos);

                if (version >= Lucene45DocValuesFormat.VERSION_CHECKSUM)
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
                int version2 = CodecUtil.CheckHeader(data, dataCodec, Lucene45DocValuesFormat.VERSION_START, Lucene45DocValuesFormat.VERSION_CURRENT);
                if (version != version2)
                {
                    throw new Exception("Format versions mismatch");
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

            ramBytesUsed = new AtomicLong(RamUsageEstimator.ShallowSizeOfInstance(this.GetType()));
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
            binaries[fieldNumber] = b;

            if (meta.ReadVInt() != fieldNumber)
            {
                throw new Exception("sorted entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
            }
            if (meta.ReadByte() != Lucene45DocValuesFormat.NUMERIC)
            {
                throw new Exception("sorted entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
            }
            NumericEntry n = ReadNumericEntry(meta);
            ords[fieldNumber] = n;
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
            binaries[fieldNumber] = b;

            if (meta.ReadVInt() != fieldNumber)
            {
                throw new Exception("sortedset entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
            }
            if (meta.ReadByte() != Lucene45DocValuesFormat.NUMERIC)
            {
                throw new Exception("sortedset entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
            }
            NumericEntry n1 = ReadNumericEntry(meta);
            ords[fieldNumber] = n1;

            if (meta.ReadVInt() != fieldNumber)
            {
                throw new Exception("sortedset entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
            }
            if (meta.ReadByte() != Lucene45DocValuesFormat.NUMERIC)
            {
                throw new Exception("sortedset entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
            }
            NumericEntry n2 = ReadNumericEntry(meta);
            ordIndexes[fieldNumber] = n2;
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
                    numerics[fieldNumber] = ReadNumericEntry(meta);
                }
                else if (type == Lucene45DocValuesFormat.BINARY)
                {
                    BinaryEntry b = ReadBinaryEntry(meta);
                    binaries[fieldNumber] = b;
                }
                else if (type == Lucene45DocValuesFormat.SORTED)
                {
                    ReadSortedField(fieldNumber, meta, infos);
                }
                else if (type == Lucene45DocValuesFormat.SORTED_SET)
                {
                    SortedSetEntry ss = ReadSortedSetEntry(meta);
                    sortedSets[fieldNumber] = ss;
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
            entry.format = meta.ReadVInt();
            entry.missingOffset = meta.ReadLong();
            entry.PackedIntsVersion = meta.ReadVInt();
            entry.Offset = meta.ReadLong();
            entry.Count = meta.ReadVLong();
            entry.BlockSize = meta.ReadVInt();
            switch (entry.format)
            {
                case Lucene45DocValuesConsumer.GCD_COMPRESSED:
                    entry.minValue = meta.ReadLong();
                    entry.gcd = meta.ReadLong();
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
                    entry.table = new long[uniqueValues];
                    for (int i = 0; i < uniqueValues; ++i)
                    {
                        entry.table[i] = meta.ReadLong();
                    }
                    break;

                case Lucene45DocValuesConsumer.DELTA_COMPRESSED:
                    break;

                default:
                    throw new Exception("Unknown format: " + entry.format + ", input=" + meta);
            }
            return entry;
        }

        internal static BinaryEntry ReadBinaryEntry(IndexInput meta)
        {
            BinaryEntry entry = new BinaryEntry();
            entry.format = meta.ReadVInt();
            entry.missingOffset = meta.ReadLong();
            entry.minLength = meta.ReadVInt();
            entry.maxLength = meta.ReadVInt();
            entry.Count = meta.ReadVLong();
            entry.offset = meta.ReadLong();
            switch (entry.format)
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
                    throw new Exception("Unknown format: " + entry.format + ", input=" + meta);
            }
            return entry;
        }

        internal virtual SortedSetEntry ReadSortedSetEntry(IndexInput meta)
        {
            SortedSetEntry entry = new SortedSetEntry();
            if (version >= Lucene45DocValuesFormat.VERSION_SORTED_SET_SINGLE_VALUE_OPTIMIZED)
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
            NumericEntry entry = numerics[field.Number];
            return GetNumeric(entry);
        }

        public override long RamBytesUsed()
        {
            return ramBytesUsed.Get();
        }

        public override void CheckIntegrity()
        {
            if (version >= Lucene45DocValuesFormat.VERSION_CHECKSUM)
            {
                CodecUtil.ChecksumEntireFile(data);
            }
        }

        internal virtual LongValues GetNumeric(NumericEntry entry)
        {
            IndexInput data = (IndexInput)this.data.Clone();
            data.Seek(entry.Offset);

            switch (entry.format)
            {
                case Lucene45DocValuesConsumer.DELTA_COMPRESSED:
                    BlockPackedReader reader = new BlockPackedReader(data, entry.PackedIntsVersion, entry.BlockSize, entry.Count, true);
                    return reader;

                case Lucene45DocValuesConsumer.GCD_COMPRESSED:
                    long min = entry.minValue;
                    long mult = entry.gcd;
                    BlockPackedReader quotientReader = new BlockPackedReader(data, entry.PackedIntsVersion, entry.BlockSize, entry.Count, true);
                    return new LongValuesAnonymousInnerClassHelper(this, min, mult, quotientReader);

                case Lucene45DocValuesConsumer.TABLE_COMPRESSED:
                    long[] table = entry.table;
                    int bitsRequired = PackedInts.BitsRequired(table.Length - 1);
                    PackedInts.Reader ords = PackedInts.GetDirectReaderNoHeader(data, PackedInts.Format.PACKED, entry.PackedIntsVersion, (int)entry.Count, bitsRequired);
                    return new LongValuesAnonymousInnerClassHelper2(this, table, ords);

                default:
                    throw new Exception();
            }
        }

        private class LongValuesAnonymousInnerClassHelper : LongValues
        {
            private readonly Lucene45DocValuesProducer outerInstance;

            private long min;
            private long mult;
            private BlockPackedReader quotientReader;

            public LongValuesAnonymousInnerClassHelper(Lucene45DocValuesProducer outerInstance, long min, long mult, BlockPackedReader quotientReader)
            {
                this.outerInstance = outerInstance;
                this.min = min;
                this.mult = mult;
                this.quotientReader = quotientReader;
            }

            public override long Get(long id)
            {
                return min + mult * quotientReader.Get(id);
            }
        }

        private class LongValuesAnonymousInnerClassHelper2 : LongValues
        {
            private readonly Lucene45DocValuesProducer outerInstance;

            private long[] table;
            private PackedInts.Reader ords;

            public LongValuesAnonymousInnerClassHelper2(Lucene45DocValuesProducer outerInstance, long[] table, PackedInts.Reader ords)
            {
                this.outerInstance = outerInstance;
                this.table = table;
                this.ords = ords;
            }

            public override long Get(long id)
            {
                return table[(int)ords.Get((int)id)];
            }
        }

        public override BinaryDocValues GetBinary(FieldInfo field)
        {
            BinaryEntry bytes = binaries[field.Number];
            switch (bytes.format)
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
            IndexInput data = (IndexInput)this.data.Clone();

            return new LongBinaryDocValuesAnonymousInnerClassHelper(this, bytes, data);
        }

        private class LongBinaryDocValuesAnonymousInnerClassHelper : LongBinaryDocValues
        {
            private readonly Lucene45DocValuesProducer outerInstance;

            private Lucene45DocValuesProducer.BinaryEntry bytes;
            private IndexInput data;

            public LongBinaryDocValuesAnonymousInnerClassHelper(Lucene45DocValuesProducer outerInstance, Lucene45DocValuesProducer.BinaryEntry bytes, IndexInput data)
            {
                this.outerInstance = outerInstance;
                this.bytes = bytes;
                this.data = data;
            }

            public override void Get(long id, BytesRef result)
            {
                long address = bytes.offset + id * bytes.maxLength;
                try
                {
                    data.Seek(address);
                    // NOTE: we could have one buffer, but various consumers (e.g. FieldComparatorSource)
                    // assume "they" own the bytes after calling this!
                    var buffer = new byte[bytes.maxLength];
                    data.ReadBytes(buffer, 0, buffer.Length);
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
            lock (addressInstances)
            {
                MonotonicBlockPackedReader addrInstance;
                if (!addressInstances.TryGetValue(field.Number, out addrInstance))
                {
                    data.Seek(bytes.AddressesOffset);
                    addrInstance = new MonotonicBlockPackedReader(data, bytes.PackedIntsVersion, bytes.BlockSize, bytes.Count, false);
                    addressInstances[field.Number] = addrInstance;
                    ramBytesUsed.AddAndGet(addrInstance.RamBytesUsed() + RamUsageEstimator.NUM_BYTES_INT);
                }
                addresses = addrInstance;
            }
            return addresses;
        }

        private BinaryDocValues GetVariableBinary(FieldInfo field, BinaryEntry bytes)
        {
            IndexInput data = (IndexInput)this.data.Clone();

            MonotonicBlockPackedReader addresses = GetAddressInstance(data, field, bytes);

            return new LongBinaryDocValuesAnonymousInnerClassHelper2(this, bytes, data, addresses);
        }

        private class LongBinaryDocValuesAnonymousInnerClassHelper2 : LongBinaryDocValues
        {
            private readonly Lucene45DocValuesProducer outerInstance;

            private Lucene45DocValuesProducer.BinaryEntry bytes;
            private IndexInput data;
            private MonotonicBlockPackedReader addresses;

            public LongBinaryDocValuesAnonymousInnerClassHelper2(Lucene45DocValuesProducer outerInstance, Lucene45DocValuesProducer.BinaryEntry bytes, IndexInput data, MonotonicBlockPackedReader addresses)
            {
                this.outerInstance = outerInstance;
                this.bytes = bytes;
                this.data = data;
                this.addresses = addresses;
            }

            public override void Get(long id, BytesRef result)
            {
                long startAddress = bytes.offset + (id == 0 ? 0 : addresses.Get(id - 1));
                long endAddress = bytes.offset + addresses.Get(id);
                int length = (int)(endAddress - startAddress);
                try
                {
                    data.Seek(startAddress);
                    // NOTE: we could have one buffer, but various consumers (e.g. FieldComparatorSource)
                    // assume "they" own the bytes after calling this!
                    var buffer = new byte[length];
                    data.ReadBytes(buffer, 0, buffer.Length);
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
            lock (addressInstances)
            {
                MonotonicBlockPackedReader addrInstance;
                if (!addressInstances.TryGetValue(field.Number, out addrInstance))
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
                    addressInstances[field.Number] = addrInstance;
                    ramBytesUsed.AddAndGet(addrInstance.RamBytesUsed() + RamUsageEstimator.NUM_BYTES_INT);
                }
                addresses = addrInstance;
            }
            return addresses;
        }

        private BinaryDocValues GetCompressedBinary(FieldInfo field, BinaryEntry bytes)
        {
            IndexInput data = (IndexInput)this.data.Clone();

            MonotonicBlockPackedReader addresses = GetIntervalInstance(data, field, bytes);

            return new CompressedBinaryDocValues(bytes, addresses, data);
        }

        public override SortedDocValues GetSorted(FieldInfo field)
        {
            int valueCount = (int)binaries[field.Number].Count;
            BinaryDocValues binary = GetBinary(field);
            NumericEntry entry = ords[field.Number];
            IndexInput data = (IndexInput)this.data.Clone();
            data.Seek(entry.Offset);
            BlockPackedReader ordinals = new BlockPackedReader(data, entry.PackedIntsVersion, entry.BlockSize, entry.Count, true);

            return new SortedDocValuesAnonymousInnerClassHelper(this, valueCount, binary, ordinals);
        }

        private class SortedDocValuesAnonymousInnerClassHelper : SortedDocValues
        {
            private readonly Lucene45DocValuesProducer outerInstance;

            private int valueCount;
            private BinaryDocValues binary;
            private BlockPackedReader ordinals;

            public SortedDocValuesAnonymousInnerClassHelper(Lucene45DocValuesProducer outerInstance, int valueCount, BinaryDocValues binary, BlockPackedReader ordinals)
            {
                this.outerInstance = outerInstance;
                this.valueCount = valueCount;
                this.binary = binary;
                this.ordinals = ordinals;
            }

            public override int GetOrd(int docID)
            {
                return (int)ordinals.Get(docID);
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                binary.Get(ord, result);
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
                if (binary is CompressedBinaryDocValues)
                {
                    return (int)((CompressedBinaryDocValues)binary).LookupTerm(key);
                }
                else
                {
                    return base.LookupTerm(key);
                }
            }

            public override TermsEnum GetTermsEnum()
            {
                if (binary is CompressedBinaryDocValues)
                {
                    return ((CompressedBinaryDocValues)binary).GetTermsEnum();
                }
                else
                {
                    return base.GetTermsEnum();
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
            lock (ordIndexInstances)
            {
                MonotonicBlockPackedReader ordIndexInstance;
                if (!ordIndexInstances.TryGetValue(field.Number, out ordIndexInstance))
                {
                    data.Seek(entry.Offset);
                    ordIndexInstance = new MonotonicBlockPackedReader(data, entry.PackedIntsVersion, entry.BlockSize, entry.Count, false);
                    ordIndexInstances[field.Number] = ordIndexInstance;
                    ramBytesUsed.AddAndGet(ordIndexInstance.RamBytesUsed() + RamUsageEstimator.NUM_BYTES_INT);
                }
                ordIndex = ordIndexInstance;
            }
            return ordIndex;
        }

        public override SortedSetDocValues GetSortedSet(FieldInfo field)
        {
            SortedSetEntry ss = sortedSets[field.Number];
            if (ss.Format == Lucene45DocValuesConsumer.SORTED_SET_SINGLE_VALUED_SORTED)
            {
                SortedDocValues values = GetSorted(field);
                return DocValues.Singleton(values);
            }
            else if (ss.Format != Lucene45DocValuesConsumer.SORTED_SET_WITH_ADDRESSES)
            {
                throw new Exception();
            }

            IndexInput data = (IndexInput)this.data.Clone();
            long valueCount = binaries[field.Number].Count;
            // we keep the byte[]s and list of ords on disk, these could be large
            LongBinaryDocValues binary = (LongBinaryDocValues)GetBinary(field);
            LongValues ordinals = GetNumeric(ords[field.Number]);
            // but the addresses to the ord stream are in RAM
            MonotonicBlockPackedReader ordIndex = GetOrdIndexInstance(data, field, ordIndexes[field.Number]);

            return new RandomAccessOrdsAnonymousInnerClassHelper(this, valueCount, binary, ordinals, ordIndex);
        }

        private class RandomAccessOrdsAnonymousInnerClassHelper : RandomAccessOrds
        {
            private readonly Lucene45DocValuesProducer outerInstance;

            private long valueCount;
            private Lucene45DocValuesProducer.LongBinaryDocValues binary;
            private LongValues ordinals;
            private MonotonicBlockPackedReader ordIndex;

            public RandomAccessOrdsAnonymousInnerClassHelper(Lucene45DocValuesProducer outerInstance, long valueCount, Lucene45DocValuesProducer.LongBinaryDocValues binary, LongValues ordinals, MonotonicBlockPackedReader ordIndex)
            {
                this.outerInstance = outerInstance;
                this.valueCount = valueCount;
                this.binary = binary;
                this.ordinals = ordinals;
                this.ordIndex = ordIndex;
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
                    long ord = ordinals.Get(offset);
                    offset++;
                    return ord;
                }
            }

            public override void SetDocument(int docID)
            {
                startOffset = offset = (docID == 0 ? 0 : ordIndex.Get(docID - 1));
                endOffset = ordIndex.Get(docID);
            }

            public override void LookupOrd(long ord, BytesRef result)
            {
                binary.Get(ord, result);
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
                if (binary is CompressedBinaryDocValues)
                {
                    return ((CompressedBinaryDocValues)binary).LookupTerm(key);
                }
                else
                {
                    return base.LookupTerm(key);
                }
            }

            public override TermsEnum GetTermsEnum()
            {
                if (binary is CompressedBinaryDocValues)
                {
                    return ((CompressedBinaryDocValues)binary).GetTermsEnum();
                }
                else
                {
                    return base.GetTermsEnum();
                }
            }

            public override long OrdAt(int index)
            {
                return ordinals.Get(startOffset + index);
            }

            public override int Cardinality()
            {
                return (int)(endOffset - startOffset);
            }
        }

        private IBits GetMissingBits(long offset)
        {
            if (offset == -1)
            {
                return new Bits.MatchAllBits(maxDoc);
            }
            else
            {
                IndexInput @in = (IndexInput)data.Clone();
                return new BitsAnonymousInnerClassHelper(this, offset, @in);
            }
        }

        private class BitsAnonymousInnerClassHelper : IBits
        {
            private readonly Lucene45DocValuesProducer outerInstance;

            private long offset;
            private IndexInput @in;

            public BitsAnonymousInnerClassHelper(Lucene45DocValuesProducer outerInstance, long offset, IndexInput @in)
            {
                this.outerInstance = outerInstance;
                this.offset = offset;
                this.@in = @in;
            }

            public virtual bool Get(int index)
            {
                try
                {
                    @in.Seek(offset + (index >> 3));
                    return (@in.ReadByte() & (1 << (index & 7))) != 0;
                }
                catch (Exception e)
                {
                    throw;
                }
            }

            public virtual int Length
            {
                get { return outerInstance.maxDoc; }
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
                    BinaryEntry be = binaries[field.Number];
                    return GetMissingBits(be.missingOffset);

                case DocValuesType.NUMERIC:
                    NumericEntry ne = numerics[field.Number];
                    return GetMissingBits(ne.missingOffset);

                default:
                    throw new InvalidOperationException();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                data.Dispose();
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
            internal long missingOffset;

            /// <summary>
            /// offset to the actual numeric values </summary>
            public long Offset { get; set; }

            internal int format;

            /// <summary>
            /// packed ints version used to encode these numerics </summary>
            public int PackedIntsVersion { get; set; }

            /// <summary>
            /// count of values written </summary>
            public long Count { get; set; }

            /// <summary>
            /// packed ints blocksize </summary>
            public int BlockSize { get; set; }

            internal long minValue;
            internal long gcd;
            internal long[] table;
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
            internal long missingOffset;

            /// <summary>
            /// offset to the actual binary values </summary>
            internal long offset;

            internal int format;

            /// <summary>
            /// count of values written </summary>
            public long Count { get; set; }

            internal int minLength;
            internal int maxLength;

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

            public abstract void Get(long id, BytesRef result);
        }

        // in the compressed case, we add a few additional operations for
        // more efficient reverse lookup and enumeration
        internal class CompressedBinaryDocValues : LongBinaryDocValues
        {
            internal readonly BinaryEntry bytes;
            internal readonly long interval;
            internal readonly long numValues;
            internal readonly long numIndexValues;
            internal readonly MonotonicBlockPackedReader addresses;
            internal readonly IndexInput data;
            internal readonly TermsEnum termsEnum;

            public CompressedBinaryDocValues(BinaryEntry bytes, MonotonicBlockPackedReader addresses, IndexInput data)
            {
                this.bytes = bytes;
                this.interval = bytes.AddressInterval;
                this.addresses = addresses;
                this.data = data;
                this.numValues = bytes.Count;
                this.numIndexValues = addresses.Size;
                this.termsEnum = GetTermsEnum(data);
            }

            public override void Get(long id, BytesRef result)
            {
                try
                {
                    termsEnum.SeekExact(id);
                    BytesRef term = termsEnum.Term;
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
                    TermsEnum.SeekStatus status = termsEnum.SeekCeil(key);
                    if (status == TermsEnum.SeekStatus.END)
                    {
                        return -numValues - 1;
                    }
                    else if (status == TermsEnum.SeekStatus.FOUND)
                    {
                        return termsEnum.Ord;
                    }
                    else
                    {
                        return -termsEnum.Ord - 1;
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
                    return GetTermsEnum((IndexInput)data.Clone());
                }
                catch (Exception)
                {
                    throw;
                }
            }

            internal virtual TermsEnum GetTermsEnum(IndexInput input)
            {
                input.Seek(bytes.offset);

                return new TermsEnumAnonymousInnerClassHelper(this, input);
            }

            private class TermsEnumAnonymousInnerClassHelper : TermsEnum
            {
                private readonly CompressedBinaryDocValues outerInstance;

                private IndexInput input;

                public TermsEnumAnonymousInnerClassHelper(CompressedBinaryDocValues outerInstance, IndexInput input)
                {
                    this.outerInstance = outerInstance;
                    this.input = input;
                    currentOrd = -1;
                    termBuffer = new BytesRef(outerInstance.bytes.maxLength < 0 ? 0 : outerInstance.bytes.maxLength);
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
                    if (++currentOrd >= outerInstance.numValues)
                    {
                        return null;
                    }
                    else
                    {
                        int start = input.ReadVInt();
                        int suffix = input.ReadVInt();
                        input.ReadBytes(termBuffer.Bytes, start, suffix);
                        termBuffer.Length = start + suffix;
                        return termBuffer;
                    }
                }

                public override TermsEnum.SeekStatus SeekCeil(BytesRef text)
                {
                    // binary-search just the index values to find the block,
                    // then scan within the block
                    long low = 0;
                    long high = outerInstance.numIndexValues - 1;

                    while (low <= high)
                    {
                        long mid = (int)((uint)(low + high) >> 1);
                        DoSeek(mid * outerInstance.interval);
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

                    if (outerInstance.numIndexValues == 0)
                    {
                        return TermsEnum.SeekStatus.END;
                    }

                    // block before insertion point
                    long block = low - 1;
                    DoSeek(block < 0 ? -1 : block * outerInstance.interval);

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
                    long block = ord / outerInstance.interval;

                    if (ord >= currentOrd && block == currentOrd / outerInstance.interval)
                    {
                        // seek within current block
                    }
                    else
                    {
                        // position before start of block
                        currentOrd = ord - ord % outerInstance.interval - 1;
                        input.Seek(outerInstance.bytes.offset + outerInstance.addresses.Get(block));
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

                public override BytesRef Term
                {
                    get { return term; }
                }

                public override long Ord
                {
                    get { return currentOrd; }
                }

                public override IComparer<BytesRef> Comparator
                {
                    get
                    {
                        return BytesRef.UTF8SortedAsUnicodeComparer;
                    }
                }

                public override int DocFreq
                {
                    get { throw new System.NotSupportedException(); }
                }

                public override long TotalTermFreq
                {
                    get { return -1; }
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
}