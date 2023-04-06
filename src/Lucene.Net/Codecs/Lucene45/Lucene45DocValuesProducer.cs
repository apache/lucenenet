using J2N.Numerics;
using J2N.Threading.Atomic;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

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
    using Int64Values = Lucene.Net.Util.Int64Values;
    using MonotonicBlockPackedReader = Lucene.Net.Util.Packed.MonotonicBlockPackedReader;
    using NumericDocValues = Lucene.Net.Index.NumericDocValues;
    using PackedInt32s = Lucene.Net.Util.Packed.PackedInt32s;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using RandomAccessOrds = Lucene.Net.Index.RandomAccessOrds;
    using SegmentReadState = Lucene.Net.Index.SegmentReadState;
    using SortedDocValues = Lucene.Net.Index.SortedDocValues;
    using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /// <summary>
    /// Reader for <see cref="Lucene45DocValuesFormat"/>. </summary>
    public class Lucene45DocValuesProducer : DocValuesProducer // LUCENENET specific - removed IDisposable, it is already implemented in base class
    {
        private readonly IDictionary<int, NumericEntry> numerics;
        private readonly IDictionary<int, BinaryEntry> binaries;
        private readonly IDictionary<int, SortedSetEntry> sortedSets;
        private readonly IDictionary<int, NumericEntry> ords;
        private readonly IDictionary<int, NumericEntry> ordIndexes;
        private readonly AtomicInt64 ramBytesUsed;
        private readonly IndexInput data;
        private readonly int maxDoc;
        private readonly int version;

        // memory-resident structures
        private readonly IDictionary<int, MonotonicBlockPackedReader> addressInstances = new Dictionary<int, MonotonicBlockPackedReader>();

        private readonly IDictionary<int, MonotonicBlockPackedReader> ordIndexInstances = new Dictionary<int, MonotonicBlockPackedReader>();

        /// <summary>
        /// Expert: instantiates a new reader. </summary>
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
                ReadFields(@in /*, state.FieldInfos // LUCENENET: Not read */);

                if (version >= Lucene45DocValuesFormat.VERSION_CHECKSUM)
                {
                    CodecUtil.CheckFooter(@in);
                }
                else
                {
#pragma warning disable 612, 618
                    CodecUtil.CheckEOF(@in);
#pragma warning restore 612, 618
                }

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
                string dataName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, dataExtension);
                data = state.Directory.OpenInput(dataName, state.Context);
                int version2 = CodecUtil.CheckHeader(data, dataCodec, Lucene45DocValuesFormat.VERSION_START, Lucene45DocValuesFormat.VERSION_CURRENT);
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

            ramBytesUsed = new AtomicInt64(RamUsageEstimator.ShallowSizeOfInstance(this.GetType()));
        }

        private void ReadSortedField(int fieldNumber, IndexInput meta /*, FieldInfos infos // LUCENENET: Never read */)
        {
            // sorted = binary + numeric
            if (meta.ReadVInt32() != fieldNumber)
            {
                throw new CorruptIndexException("sorted entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
            }
            if (meta.ReadByte() != Lucene45DocValuesFormat.BINARY)
            {
                throw new CorruptIndexException("sorted entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
            }
            BinaryEntry b = ReadBinaryEntry(meta);
            binaries[fieldNumber] = b;

            if (meta.ReadVInt32() != fieldNumber)
            {
                throw new CorruptIndexException("sorted entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
            }
            if (meta.ReadByte() != Lucene45DocValuesFormat.NUMERIC)
            {
                throw new CorruptIndexException("sorted entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
            }
            NumericEntry n = ReadNumericEntry(meta);
            ords[fieldNumber] = n;
        }

        private void ReadSortedSetFieldWithAddresses(int fieldNumber, IndexInput meta /*, FieldInfos infos // LUCENENET: Never read */)
        {
            // sortedset = binary + numeric (addresses) + ordIndex
            if (meta.ReadVInt32() != fieldNumber)
            {
                throw new CorruptIndexException("sortedset entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
            }
            if (meta.ReadByte() != Lucene45DocValuesFormat.BINARY)
            {
                throw new CorruptIndexException("sortedset entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
            }
            BinaryEntry b = ReadBinaryEntry(meta);
            binaries[fieldNumber] = b;

            if (meta.ReadVInt32() != fieldNumber)
            {
                throw new CorruptIndexException("sortedset entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
            }
            if (meta.ReadByte() != Lucene45DocValuesFormat.NUMERIC)
            {
                throw new CorruptIndexException("sortedset entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
            }
            NumericEntry n1 = ReadNumericEntry(meta);
            ords[fieldNumber] = n1;

            if (meta.ReadVInt32() != fieldNumber)
            {
                throw new CorruptIndexException("sortedset entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
            }
            if (meta.ReadByte() != Lucene45DocValuesFormat.NUMERIC)
            {
                throw new CorruptIndexException("sortedset entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
            }
            NumericEntry n2 = ReadNumericEntry(meta);
            ordIndexes[fieldNumber] = n2;
        }

        private void ReadFields(IndexInput meta /*, FieldInfos infos // LUCENENET: Not read */)
        {
            int fieldNumber = meta.ReadVInt32();
            while (fieldNumber != -1)
            {
                // check should be: infos.fieldInfo(fieldNumber) != null, which incorporates negative check
                // but docvalues updates are currently buggy here (loading extra stuff, etc): LUCENE-5616
                if (fieldNumber < 0)
                {
                    // trickier to validate more: because we re-use for norms, because we use multiple entries
                    // for "composite" types like sortedset, etc.
                    throw new CorruptIndexException("Invalid field number: " + fieldNumber + " (resource=" + meta + ")");
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
                    ReadSortedField(fieldNumber, meta /*, infos // LUCENENET: Never read */);
                }
                else if (type == Lucene45DocValuesFormat.SORTED_SET)
                {
                    SortedSetEntry ss = ReadSortedSetEntry(meta);
                    sortedSets[fieldNumber] = ss;
                    if (ss.Format == Lucene45DocValuesConsumer.SORTED_SET_WITH_ADDRESSES)
                    {
                        ReadSortedSetFieldWithAddresses(fieldNumber, meta/*, infos // LUCENENET: Never read */);
                    }
                    else if (ss.Format == Lucene45DocValuesConsumer.SORTED_SET_SINGLE_VALUED_SORTED)
                    {
                        if (meta.ReadVInt32() != fieldNumber)
                        {
                            throw new CorruptIndexException("sortedset entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
                        }
                        if (meta.ReadByte() != Lucene45DocValuesFormat.SORTED)
                        {
                            throw new CorruptIndexException("sortedset entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
                        }
                        ReadSortedField(fieldNumber, meta/*, infos // LUCENENET: Never read */);
                    }
                    else
                    {
                        throw AssertionError.Create();
                    }
                }
                else
                {
                    throw new CorruptIndexException("invalid type: " + type + ", resource=" + meta);
                }
                fieldNumber = meta.ReadVInt32();
            }
        }

        internal static NumericEntry ReadNumericEntry(IndexInput meta)
        {
            NumericEntry entry = new NumericEntry();
            entry.format = meta.ReadVInt32();
            entry.missingOffset = meta.ReadInt64();
            entry.PackedInt32sVersion = meta.ReadVInt32();
            entry.Offset = meta.ReadInt64();
            entry.Count = meta.ReadVInt64();
            entry.BlockSize = meta.ReadVInt32();
            switch (entry.format)
            {
                case Lucene45DocValuesConsumer.GCD_COMPRESSED:
                    entry.minValue = meta.ReadInt64();
                    entry.gcd = meta.ReadInt64();
                    break;

                case Lucene45DocValuesConsumer.TABLE_COMPRESSED:
                    if (entry.Count > int.MaxValue)
                    {
                        throw new CorruptIndexException("Cannot use TABLE_COMPRESSED with more than MAX_VALUE values, input=" + meta);
                    }
                    int uniqueValues = meta.ReadVInt32();
                    if (uniqueValues > 256)
                    {
                        throw new CorruptIndexException("TABLE_COMPRESSED cannot have more than 256 distinct values, input=" + meta);
                    }
                    entry.table = new long[uniqueValues];
                    for (int i = 0; i < uniqueValues; ++i)
                    {
                        entry.table[i] = meta.ReadInt64();
                    }
                    break;

                case Lucene45DocValuesConsumer.DELTA_COMPRESSED:
                    break;

                default:
                    throw new CorruptIndexException("Unknown format: " + entry.format + ", input=" + meta);
            }
            return entry;
        }

        internal static BinaryEntry ReadBinaryEntry(IndexInput meta)
        {
            BinaryEntry entry = new BinaryEntry();
            entry.format = meta.ReadVInt32();
            entry.missingOffset = meta.ReadInt64();
            entry.minLength = meta.ReadVInt32();
            entry.maxLength = meta.ReadVInt32();
            entry.Count = meta.ReadVInt64();
            entry.offset = meta.ReadInt64();
            switch (entry.format)
            {
                case Lucene45DocValuesConsumer.BINARY_FIXED_UNCOMPRESSED:
                    break;

                case Lucene45DocValuesConsumer.BINARY_PREFIX_COMPRESSED:
                    entry.AddressInterval = meta.ReadVInt32();
                    entry.AddressesOffset = meta.ReadInt64();
                    entry.PackedInt32sVersion = meta.ReadVInt32();
                    entry.BlockSize = meta.ReadVInt32();
                    break;

                case Lucene45DocValuesConsumer.BINARY_VARIABLE_UNCOMPRESSED:
                    entry.AddressesOffset = meta.ReadInt64();
                    entry.PackedInt32sVersion = meta.ReadVInt32();
                    entry.BlockSize = meta.ReadVInt32();
                    break;

                default:
                    throw new CorruptIndexException("Unknown format: " + entry.format + ", input=" + meta);
            }
            return entry;
        }

        internal virtual SortedSetEntry ReadSortedSetEntry(IndexInput meta)
        {
            SortedSetEntry entry = new SortedSetEntry();
            if (version >= Lucene45DocValuesFormat.VERSION_SORTED_SET_SINGLE_VALUE_OPTIMIZED)
            {
                entry.Format = meta.ReadVInt32();
            }
            else
            {
                entry.Format = Lucene45DocValuesConsumer.SORTED_SET_WITH_ADDRESSES;
            }
            if (entry.Format != Lucene45DocValuesConsumer.SORTED_SET_SINGLE_VALUED_SORTED && entry.Format != Lucene45DocValuesConsumer.SORTED_SET_WITH_ADDRESSES)
            {
                throw new CorruptIndexException("Unknown format: " + entry.Format + ", input=" + meta);
            }
            return entry;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override NumericDocValues GetNumeric(FieldInfo field)
        {
            NumericEntry entry = numerics[field.Number];
            return GetNumeric(entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long RamBytesUsed() => ramBytesUsed;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void CheckIntegrity()
        {
            if (version >= Lucene45DocValuesFormat.VERSION_CHECKSUM)
            {
                CodecUtil.ChecksumEntireFile(data);
            }
        }

        internal virtual Int64Values GetNumeric(NumericEntry entry)
        {
            IndexInput data = (IndexInput)this.data.Clone();
            data.Seek(entry.Offset);

            switch (entry.format)
            {
                case Lucene45DocValuesConsumer.DELTA_COMPRESSED:
                    BlockPackedReader reader = new BlockPackedReader(data, entry.PackedInt32sVersion, entry.BlockSize, entry.Count, true);
                    return reader;

                case Lucene45DocValuesConsumer.GCD_COMPRESSED:
                    long min = entry.minValue;
                    long mult = entry.gcd;
                    BlockPackedReader quotientReader = new BlockPackedReader(data, entry.PackedInt32sVersion, entry.BlockSize, entry.Count, true);
                    return new Int64ValuesAnonymousClass(min, mult, quotientReader);

                case Lucene45DocValuesConsumer.TABLE_COMPRESSED:
                    long[] table = entry.table;
                    int bitsRequired = PackedInt32s.BitsRequired(table.Length - 1);
                    PackedInt32s.Reader ords = PackedInt32s.GetDirectReaderNoHeader(data, PackedInt32s.Format.PACKED, entry.PackedInt32sVersion, (int)entry.Count, bitsRequired);
                    return new Int64ValuesAnonymousClass2(table, ords);

                default:
                    throw AssertionError.Create();
            }
        }

        private sealed class Int64ValuesAnonymousClass : Int64Values
        {
            private readonly long min;
            private readonly long mult;
            private readonly BlockPackedReader quotientReader;

            public Int64ValuesAnonymousClass(long min, long mult, BlockPackedReader quotientReader)
            {
                this.min = min;
                this.mult = mult;
                this.quotientReader = quotientReader;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long Get(long id)
            {
                return min + mult * quotientReader.Get(id);
            }
        }

        private sealed class Int64ValuesAnonymousClass2 : Int64Values
        {
            private readonly long[] table;
            private readonly PackedInt32s.Reader ords;

            public Int64ValuesAnonymousClass2(long[] table, PackedInt32s.Reader ords)
            {
                this.table = table;
                this.ords = ords;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                    return GetFixedBinary(/*field, LUCENENET: Never read */ bytes);

                case Lucene45DocValuesConsumer.BINARY_VARIABLE_UNCOMPRESSED:
                    return GetVariableBinary(field, bytes);

                case Lucene45DocValuesConsumer.BINARY_PREFIX_COMPRESSED:
                    return GetCompressedBinary(field, bytes);

                default:
                    throw AssertionError.Create();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BinaryDocValues GetFixedBinary(/* FieldInfo field, // LUCENENET: Never read */ BinaryEntry bytes)
        {
            IndexInput data = (IndexInput)this.data.Clone();

            return new Int64BinaryDocValuesAnonymousClass(bytes, data);
        }

        private sealed class Int64BinaryDocValuesAnonymousClass : Int64BinaryDocValues
        {
            private readonly Lucene45DocValuesProducer.BinaryEntry bytes;
            private readonly IndexInput data;

            public Int64BinaryDocValuesAnonymousClass(Lucene45DocValuesProducer.BinaryEntry bytes, IndexInput data)
            {
                this.bytes = bytes;
                this.data = data;
            }

            public override void Get(long id, BytesRef result)
            {
                long address = bytes.offset + id * bytes.maxLength;
                try
                {
                    data.Seek(address);
                    // NOTE: we could have one buffer, but various consumers (e.g. FieldComparerSource)
                    // assume "they" own the bytes after calling this!
                    var buffer = new byte[bytes.maxLength];
                    data.ReadBytes(buffer, 0, buffer.Length);
                    result.Bytes = buffer;
                    result.Offset = 0;
                    result.Length = buffer.Length;
                }
                catch (Exception e) when (e.IsIOException())
                {
                    throw RuntimeException.Create(e);
                }
            }
        }

        /// <summary>
        /// Returns an address instance for variable-length binary values.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        protected virtual MonotonicBlockPackedReader GetAddressInstance(IndexInput data, FieldInfo field, BinaryEntry bytes)
        {
            MonotonicBlockPackedReader addresses;
            UninterruptableMonitor.Enter(addressInstances);
            try
            {
                if (!addressInstances.TryGetValue(field.Number, out MonotonicBlockPackedReader addrInstance) || addrInstance is null)
                {
                    data.Seek(bytes.AddressesOffset);
                    addrInstance = new MonotonicBlockPackedReader(data, bytes.PackedInt32sVersion, bytes.BlockSize, bytes.Count, false);
                    addressInstances[field.Number] = addrInstance;
                    ramBytesUsed.AddAndGet(addrInstance.RamBytesUsed() + RamUsageEstimator.NUM_BYTES_INT32);
                }
                addresses = addrInstance;
            }
            finally
            {
                UninterruptableMonitor.Exit(addressInstances);
            }
            return addresses;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BinaryDocValues GetVariableBinary(FieldInfo field, BinaryEntry bytes)
        {
            IndexInput data = (IndexInput)this.data.Clone();

            MonotonicBlockPackedReader addresses = GetAddressInstance(data, field, bytes);

            return new Int64BinaryDocValuesAnonymousClass2(bytes, data, addresses);
        }

        private sealed class Int64BinaryDocValuesAnonymousClass2 : Int64BinaryDocValues
        {
            private readonly Lucene45DocValuesProducer.BinaryEntry bytes;
            private readonly IndexInput data;
            private readonly MonotonicBlockPackedReader addresses;

            public Int64BinaryDocValuesAnonymousClass2(Lucene45DocValuesProducer.BinaryEntry bytes, IndexInput data, MonotonicBlockPackedReader addresses)
            {
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
                    // NOTE: we could have one buffer, but various consumers (e.g. FieldComparerSource)
                    // assume "they" own the bytes after calling this!
                    var buffer = new byte[length];
                    data.ReadBytes(buffer, 0, buffer.Length);
                    result.Bytes = buffer;
                    result.Offset = 0;
                    result.Length = length;
                }
                catch (Exception e) when (e.IsIOException())
                {
                    throw RuntimeException.Create(e);
                }
            }
        }

        /// <summary>
        /// Returns an address instance for prefix-compressed binary values.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        protected virtual MonotonicBlockPackedReader GetIntervalInstance(IndexInput data, FieldInfo field, BinaryEntry bytes)
        {
            MonotonicBlockPackedReader addresses;
            long interval = bytes.AddressInterval;
            UninterruptableMonitor.Enter(addressInstances);
            try
            {
                if (!addressInstances.TryGetValue(field.Number, out MonotonicBlockPackedReader addrInstance))
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
                    addrInstance = new MonotonicBlockPackedReader(data, bytes.PackedInt32sVersion, bytes.BlockSize, size, false);
                    addressInstances[field.Number] = addrInstance;
                    ramBytesUsed.AddAndGet(addrInstance.RamBytesUsed() + RamUsageEstimator.NUM_BYTES_INT32);
                }
                addresses = addrInstance;
            }
            finally
            {
                UninterruptableMonitor.Exit(addressInstances);
            }
            return addresses;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            BlockPackedReader ordinals = new BlockPackedReader(data, entry.PackedInt32sVersion, entry.BlockSize, entry.Count, true);

            return new SortedDocValuesAnonymousClass(valueCount, binary, ordinals);
        }

        private sealed class SortedDocValuesAnonymousClass : SortedDocValues
        {
            private readonly int valueCount;
            private readonly BinaryDocValues binary;
            private readonly BlockPackedReader ordinals;

            public SortedDocValuesAnonymousClass(int valueCount, BinaryDocValues binary, BlockPackedReader ordinals)
            {
                this.valueCount = valueCount;
                this.binary = binary;
                this.ordinals = ordinals;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetOrd(int docID)
            {
                return (int)ordinals.Get(docID);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void LookupOrd(int ord, BytesRef result)
            {
                binary.Get(ord, result);
            }

            public override int ValueCount => valueCount;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int LookupTerm(BytesRef key)
            {
                if (binary is CompressedBinaryDocValues compressedBinaryDocValues)
                {
                    return (int)compressedBinaryDocValues.LookupTerm(key);
                }
                else
                {
                    return base.LookupTerm(key);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override TermsEnum GetTermsEnum()
            {
                if (binary is CompressedBinaryDocValues compressedBinaryDocValues)
                {
                    return compressedBinaryDocValues.GetTermsEnum();
                }
                else
                {
                    return base.GetTermsEnum();
                }
            }
        }

        /// <summary>
        /// Returns an address instance for sortedset ordinal lists.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        protected virtual MonotonicBlockPackedReader GetOrdIndexInstance(IndexInput data, FieldInfo field, NumericEntry entry)
        {
            MonotonicBlockPackedReader ordIndex;
            UninterruptableMonitor.Enter(ordIndexInstances);
            try
            {
                if (!ordIndexInstances.TryGetValue(field.Number, out MonotonicBlockPackedReader ordIndexInstance))
                {
                    data.Seek(entry.Offset);
                    ordIndexInstance = new MonotonicBlockPackedReader(data, entry.PackedInt32sVersion, entry.BlockSize, entry.Count, false);
                    ordIndexInstances[field.Number] = ordIndexInstance;
                    ramBytesUsed.AddAndGet(ordIndexInstance.RamBytesUsed() + RamUsageEstimator.NUM_BYTES_INT32);
                }
                ordIndex = ordIndexInstance;
            }
            finally
            {
                UninterruptableMonitor.Exit(ordIndexInstances);
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
                throw AssertionError.Create();
            }

            IndexInput data = (IndexInput)this.data.Clone();
            long valueCount = binaries[field.Number].Count;
            // we keep the byte[]s and list of ords on disk, these could be large
            Int64BinaryDocValues binary = (Int64BinaryDocValues)GetBinary(field);
            Int64Values ordinals = GetNumeric(ords[field.Number]);
            // but the addresses to the ord stream are in RAM
            MonotonicBlockPackedReader ordIndex = GetOrdIndexInstance(data, field, ordIndexes[field.Number]);

            return new RandomAccessOrdsAnonymousClass(valueCount, binary, ordinals, ordIndex);
        }

        private sealed class RandomAccessOrdsAnonymousClass : RandomAccessOrds
        {
            private readonly long valueCount;
            private readonly Lucene45DocValuesProducer.Int64BinaryDocValues binary;
            private readonly Int64Values ordinals;
            private readonly MonotonicBlockPackedReader ordIndex;

            public RandomAccessOrdsAnonymousClass(long valueCount, Lucene45DocValuesProducer.Int64BinaryDocValues binary, Int64Values ordinals, MonotonicBlockPackedReader ordIndex)
            {
                this.valueCount = valueCount;
                this.binary = binary;
                this.ordinals = ordinals;
                this.ordIndex = ordIndex;
            }

            internal long startOffset;
            internal long offset;
            internal long endOffset;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void SetDocument(int docID)
            {
                startOffset = offset = (docID == 0 ? 0 : ordIndex.Get(docID - 1));
                endOffset = ordIndex.Get(docID);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void LookupOrd(long ord, BytesRef result)
            {
                binary.Get(ord, result);
            }

            public override long ValueCount => valueCount;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long LookupTerm(BytesRef key)
            {
                if (binary is CompressedBinaryDocValues compressedBinaryDocValues)
                {
                    return compressedBinaryDocValues.LookupTerm(key);
                }
                else
                {
                    return base.LookupTerm(key);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override TermsEnum GetTermsEnum()
            {
                if (binary is CompressedBinaryDocValues compressedBinaryDocValues)
                {
                    return compressedBinaryDocValues.GetTermsEnum();
                }
                else
                {
                    return base.GetTermsEnum();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long OrdAt(int index)
            {
                return ordinals.Get(startOffset + index);
            }

            public override int Cardinality => (int)(endOffset - startOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IBits GetMissingBits(long offset)
        {
            if (offset == -1)
            {
                return new Bits.MatchAllBits(maxDoc);
            }
            else
            {
                IndexInput @in = (IndexInput)data.Clone();
                return new BitsAnonymousClass(this, offset, @in);
            }
        }

        private sealed class BitsAnonymousClass : IBits
        {
            private readonly Lucene45DocValuesProducer outerInstance;

            private readonly long offset;
            private readonly IndexInput @in;

            public BitsAnonymousClass(Lucene45DocValuesProducer outerInstance, long offset, IndexInput @in)
            {
                this.outerInstance = outerInstance;
                this.offset = offset;
                this.@in = @in;
            }

            public bool Get(int index)
            {
                try
                {
                    @in.Seek(offset + (index >> 3));
                    return (@in.ReadByte() & (1 << (index & 7))) != 0;
                }
                catch (Exception e) when (e.IsIOException())
                {
                    throw RuntimeException.Create(e);
                }
            }

            public int Length => outerInstance.maxDoc;
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
                    throw AssertionError.Create();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                data.Dispose();
        }

        /// <summary>
        /// Metadata entry for a numeric docvalues field. </summary>
        protected internal class NumericEntry
        {
            internal NumericEntry()
            {
            }

            /// <summary>
            /// Offset to the bitset representing docsWithField, or -1 if no documents have missing values. </summary>
            internal long missingOffset;

            /// <summary>
            /// Offset to the actual numeric values. </summary>
            public long Offset { get; set; }

            internal int format;

            /// <summary>
            /// Packed <see cref="int"/>s version used to encode these numerics. 
            /// <para/>
            /// NOTE: This was packedIntsVersion (field) in Lucene
            /// </summary>
            public int PackedInt32sVersion { get; set; }

            /// <summary>
            /// Count of values written. </summary>
            public long Count { get; set; }

            /// <summary>
            /// Packed <see cref="int"/>s blocksize. </summary>
            public int BlockSize { get; set; }

            internal long minValue;
            internal long gcd;
            internal long[] table;
        }

        /// <summary>
        /// Metadata entry for a binary docvalues field. </summary>
        protected internal class BinaryEntry
        {
            internal BinaryEntry()
            {
            }

            /// <summary>
            /// Offset to the bitset representing docsWithField, or -1 if no documents have missing values. </summary>
            internal long missingOffset;

            /// <summary>
            /// Offset to the actual binary values. </summary>
            internal long offset;

            internal int format;

            /// <summary>
            /// Count of values written. </summary>
            public long Count { get; set; }

            internal int minLength;
            internal int maxLength;

            /// <summary>
            /// Offset to the addressing data that maps a value to its slice of the <see cref="T:byte[]"/>. </summary>
            public long AddressesOffset { get; set; }

            /// <summary>
            /// Interval of shared prefix chunks (when using prefix-compressed binary). </summary>
            public long AddressInterval { get; set; }

            /// <summary>
            /// Packed ints version used to encode addressing information.
            /// <para/>
            /// NOTE: This was packedIntsVersion (field) in Lucene.
            /// </summary>
            public int PackedInt32sVersion { get; set; }

            /// <summary>
            /// Packed ints blocksize. </summary>
            public int BlockSize { get; set; }
        }

        /// <summary>
        /// Metadata entry for a sorted-set docvalues field. </summary>
        protected internal class SortedSetEntry
        {
            internal SortedSetEntry()
            {
            }

            internal int Format { get; set; }
        }

        // internally we compose complex dv (sorted/sortedset) from other ones
        /// <summary>
        /// NOTE: This was LongBinaryDocValues in Lucene.
        /// </summary>
        internal abstract class Int64BinaryDocValues : BinaryDocValues
        {
            public override sealed void Get(int docID, BytesRef result)
            {
                Get((long)docID, result);
            }

            public abstract void Get(long id, BytesRef result);
        }

        // in the compressed case, we add a few additional operations for
        // more efficient reverse lookup and enumeration
        internal class CompressedBinaryDocValues : Int64BinaryDocValues
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
                this.numIndexValues = addresses.Count;
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
                catch (Exception e) when (e.IsIOException())
                {
                    throw RuntimeException.Create(e);
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
                catch (Exception bogus) when (bogus.IsIOException())
                {
                    throw RuntimeException.Create(bogus);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            // LUCENENET specific - S1699 - marked non-virtual because calling virtual members from the constructor is not a safe operation in .NET
            internal TermsEnum GetTermsEnum()
            {
                try
                {
                    return GetTermsEnum((IndexInput)data.Clone());
                }
                catch (Exception e) when (e.IsIOException())
                {
                    throw RuntimeException.Create(e);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            // LUCENENET specific - S1699 - marked non-virtual because calling virtual members from the constructor is not a safe operation in .NET
            internal TermsEnum GetTermsEnum(IndexInput input)
            {
                input.Seek(bytes.offset);

                return new TermsEnumAnonymousClass(this, input);
            }

            private sealed class TermsEnumAnonymousClass : TermsEnum
            {
                private readonly CompressedBinaryDocValues outerInstance;

                private readonly IndexInput input;

                public TermsEnumAnonymousClass(CompressedBinaryDocValues outerInstance, IndexInput input)
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

                // LUCENENET specific - factored out DoNext() and made into MoveNext()
                public override bool MoveNext()
                {
                    if (++currentOrd >= outerInstance.numValues)
                    {
                        return false;
                    }
                    else
                    {
                        int start = input.ReadVInt32();
                        int suffix = input.ReadVInt32();
                        input.ReadBytes(termBuffer.Bytes, start, suffix);
                        termBuffer.Length = start + suffix;
                        SetTerm();
                        return true; // LUCENENET: term is readonly so cannot be null
                    }
                }

                [Obsolete("Use MoveNext() and Term instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                public override BytesRef Next()
                {
                    if (MoveNext())
                        return term;
                    return null;
                }

                public override TermsEnum.SeekStatus SeekCeil(BytesRef text)
                {
                    // binary-search just the index values to find the block,
                    // then scan within the block
                    long low = 0;
                    long high = outerInstance.numIndexValues - 1;

                    while (low <= high)
                    {
                        long mid = (low + high).TripleShift(1);
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

                    while (MoveNext())
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

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                        MoveNext();
                    }
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private void SetTerm()
                {
                    // TODO: is there a cleaner way
                    term.Bytes = new byte[termBuffer.Length];
                    term.Offset = 0;
                    term.CopyBytes(termBuffer);
                }

                public override BytesRef Term => term;

                public override long Ord => currentOrd;

                public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

                public override int DocFreq => throw UnsupportedOperationException.Create();

                public override long TotalTermFreq => -1;

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
}