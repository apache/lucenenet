using System.Diagnostics;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;

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

    /// <summary>
    /// TextReader for <seealso cref="DirectDocValuesFormat"/>
    /// </summary>

    internal class DirectDocValuesProducer : DocValuesProducer
    {
        // metadata maps (just file pointers and minimal stuff)
        private readonly IDictionary<int?, NumericEntry> numerics = new Dictionary<int?, NumericEntry>();
        private readonly IDictionary<int?, BinaryEntry> binaries = new Dictionary<int?, BinaryEntry>();
        private readonly IDictionary<int?, SortedEntry> sorteds = new Dictionary<int?, SortedEntry>();
        private readonly IDictionary<int?, SortedSetEntry> sortedSets = new Dictionary<int?, SortedSetEntry>();
        private readonly IndexInput data;

        // ram instances we have already loaded
        private readonly IDictionary<int?, NumericDocValues> numericInstances = new Dictionary<int?, NumericDocValues>();
        private readonly IDictionary<int?, BinaryDocValues> binaryInstances = new Dictionary<int?, BinaryDocValues>();
        private readonly IDictionary<int?, SortedDocValues> sortedInstances = new Dictionary<int?, SortedDocValues>();
        private readonly IDictionary<int?, SortedSetRawValues> sortedSetInstances = new Dictionary<int?, SortedSetRawValues>();
        private readonly IDictionary<int?, Bits> docsWithFieldInstances = new Dictionary<int?, Bits>();

        private readonly int maxDoc;
        private readonly AtomicLong ramBytesUsed;
        private readonly int version;

        internal const sbyte NUMBER = 0;
        internal const sbyte BYTES = 1;
        internal const sbyte SORTED = 2;
        internal const sbyte SORTED_SET = 3;

        internal const int VERSION_START = 0;
        internal const int VERSION_CHECKSUM = 1;
        internal const int VERSION_CURRENT = VERSION_CHECKSUM;

        internal DirectDocValuesProducer(SegmentReadState state, string dataCodec, string dataExtension, string metaCodec, string metaExtension)
        {
            maxDoc = state.SegmentInfo.DocCount;
            string metaName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, metaExtension);
            // read in the entries from the metadata file.
            ChecksumIndexInput @in = state.Directory.OpenChecksumInput(metaName, state.Context);
            ramBytesUsed = new AtomicLong(RamUsageEstimator.ShallowSizeOfInstance(this.GetType()));
            bool success = false;
            try
            {
                version = CodecUtil.CheckHeader(@in, metaCodec, VERSION_START, VERSION_CURRENT);
                ReadFields(@in);

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

        private static NumericEntry ReadNumericEntry(IndexInput meta)
        {
            var entry = new NumericEntry { offset = meta.ReadLong(), count = meta.ReadInt(), missingOffset = meta.ReadLong() };
            if (entry.missingOffset != -1)
            {
                entry.missingBytes = meta.ReadLong();
            }
            else
            {
                entry.missingBytes = 0;
            }
            entry.byteWidth = meta.ReadByte();

            return entry;
        }

        private BinaryEntry ReadBinaryEntry(IndexInput meta)
        {
            var entry = new BinaryEntry();
            entry.offset = meta.ReadLong();
            entry.numBytes = meta.ReadInt();
            entry.count = meta.ReadInt();
            entry.missingOffset = meta.ReadLong();
            if (entry.missingOffset != -1)
            {
                entry.missingBytes = meta.ReadLong();
            }
            else
            {
                entry.missingBytes = 0;
            }

            return entry;
        }

        private SortedEntry ReadSortedEntry(IndexInput meta)
        {
            var entry = new SortedEntry();
            entry.docToOrd = ReadNumericEntry(meta);
            entry.values = ReadBinaryEntry(meta);
            return entry;
        }

        private SortedSetEntry ReadSortedSetEntry(IndexInput meta)
        {
            var entry = new SortedSetEntry();
            entry.docToOrdAddress = ReadNumericEntry(meta);
            entry.ords = ReadNumericEntry(meta);
            entry.values = ReadBinaryEntry(meta);
            return entry;
        }

        private void ReadFields(IndexInput meta)
        {
            int fieldNumber = meta.ReadVInt();
            while (fieldNumber != -1)
            {
                int fieldType = meta.ReadByte();
                if (fieldType == NUMBER)
                {
                    numerics[fieldNumber] = ReadNumericEntry(meta);
                }
                else if (fieldType == BYTES)
                {
                    binaries[fieldNumber] = ReadBinaryEntry(meta);
                }
                else if (fieldType == SORTED)
                {
                    sorteds[fieldNumber] = ReadSortedEntry(meta);
                }
                else if (fieldType == SORTED_SET)
                {
                    sortedSets[fieldNumber] = ReadSortedSetEntry(meta);
                }
                else
                {
                    throw new CorruptIndexException("invalid entry type: " + fieldType + ", input=" + meta);
                }
                fieldNumber = meta.ReadVInt();
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

        public override NumericDocValues GetNumeric(FieldInfo field)
        {
            lock (this)
            {
                NumericDocValues instance;
                if (!numericInstances.TryGetValue(field.Number, out instance))
                {
                    // Lazy load
                    instance = LoadNumeric(numerics[field.Number]);
                    numericInstances[field.Number] = instance;
                }
                return instance;
            }
        }

        private NumericDocValues LoadNumeric(NumericEntry entry)
        {
            data.Seek(entry.offset + entry.missingBytes);
            switch (entry.byteWidth)
            {
                case 1:
                    {
                        var values = new byte[entry.count];
                        data.ReadBytes(values, 0, entry.count);
                        ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(values));
                        // LUCENENET: IMPORTANT - some bytes are negative here, so we need to pass as sbyte
                        return new NumericDocValuesAnonymousInnerClassHelper((sbyte[])(Array)values);
                    }

                case 2:
                    {
                        var values = new short[entry.count];
                        for (int i = 0; i < entry.count; i++)
                        {
                            values[i] = data.ReadShort();
                        }
                        ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(values));
                        return new NumericDocValuesAnonymousInnerClassHelper2(this, values);
                    }

                case 4:
                    {
                        var values = new int[entry.count];
                        for (var i = 0; i < entry.count; i++)
                        {
                            values[i] = data.ReadInt();
                        }
                        ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(values));
                        return new NumericDocValuesAnonymousInnerClassHelper3(values);
                    }

                case 8:
                    {
                        var values = new long[entry.count];
                        for (int i = 0; i < entry.count; i++)
                        {
                            values[i] = data.ReadLong();
                        }
                        ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(values));
                        return new NumericDocValuesAnonymousInnerClassHelper4(values);
                    }

                default:
                    throw new System.InvalidOperationException();
            }
        }

        private class NumericDocValuesAnonymousInnerClassHelper : NumericDocValues
        {
            private readonly sbyte[] values;

            public NumericDocValuesAnonymousInnerClassHelper(sbyte[] values)
            {
                this.values = values;
            }

            public override long Get(int idx)
            {
                return values[idx];
            }
        }

        private class NumericDocValuesAnonymousInnerClassHelper2 : NumericDocValues
        {
            private readonly short[] values;

            public NumericDocValuesAnonymousInnerClassHelper2(DirectDocValuesProducer outerInstance, short[] values)
            {
                this.values = values;
            }

            public override long Get(int idx)
            {
                return values[idx];
            }
        }

        private class NumericDocValuesAnonymousInnerClassHelper3 : NumericDocValues
        {
            private readonly int[] values;

            public NumericDocValuesAnonymousInnerClassHelper3(int[] values)
            {
                this.values = values;
            }

            public override long Get(int idx)
            {
                return values[idx];
            }
        }

        private class NumericDocValuesAnonymousInnerClassHelper4 : NumericDocValues
        {
            private readonly long[] values;

            public NumericDocValuesAnonymousInnerClassHelper4(long[] values)
            {
                this.values = values;
            }

            public override long Get(int idx)
            {
                return values[idx];
            }
        }

        public override BinaryDocValues GetBinary(FieldInfo field)
        {
            lock (this)
            {
                BinaryDocValues instance;
                if (!binaryInstances.TryGetValue(field.Number, out instance))
                {
                    // Lazy load
                    instance = LoadBinary(binaries[field.Number]);
                    binaryInstances[field.Number] = instance;
                }
                return instance;
            }
        }

        private BinaryDocValues LoadBinary(BinaryEntry entry)
        {
            data.Seek(entry.offset);
            var bytes = new byte[entry.numBytes];
            data.ReadBytes(bytes, 0, entry.numBytes);
            data.Seek(entry.offset + entry.numBytes + entry.missingBytes);

            var address = new int[entry.count + 1];
            for (int i = 0; i < entry.count; i++)
            {
                address[i] = data.ReadInt();
            }
            address[entry.count] = data.ReadInt();

            ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(bytes) + RamUsageEstimator.SizeOf(address));

            return new BinaryDocValuesAnonymousInnerClassHelper(bytes, address);
        }

        private class BinaryDocValuesAnonymousInnerClassHelper : BinaryDocValues
        {
            private readonly byte[] bytes;
            private readonly int[] address;

            public BinaryDocValuesAnonymousInnerClassHelper(byte[] bytes, int[] address)
            {
                this.bytes = bytes;
                this.address = address;
            }

            public override void Get(int docID, BytesRef result)
            {
                result.Bytes = bytes;
                result.Offset = address[docID];
                result.Length = address[docID + 1] - result.Offset;
            }
        }

        public override SortedDocValues GetSorted(FieldInfo field)
        {
            lock (this)
            {
                SortedDocValues instance;
                if (!sortedInstances.TryGetValue(field.Number, out instance))
                {
                    // Lazy load
                    instance = LoadSorted(field);
                    sortedInstances[field.Number] = instance;
                }
                return instance;
            }
        }

        private SortedDocValues LoadSorted(FieldInfo field)
        {
            SortedEntry entry = sorteds[field.Number];
            NumericDocValues docToOrd = LoadNumeric(entry.docToOrd);
            BinaryDocValues values = LoadBinary(entry.values);

            return new SortedDocValuesAnonymousInnerClassHelper(this, entry, docToOrd, values);
        }

        private class SortedDocValuesAnonymousInnerClassHelper : SortedDocValues
        {
            private readonly DirectDocValuesProducer outerInstance;

            private readonly SortedEntry entry;
            private readonly NumericDocValues docToOrd;
            private readonly BinaryDocValues values;

            public SortedDocValuesAnonymousInnerClassHelper(DirectDocValuesProducer outerInstance, SortedEntry entry, NumericDocValues docToOrd, BinaryDocValues values)
            {
                this.outerInstance = outerInstance;
                this.entry = entry;
                this.docToOrd = docToOrd;
                this.values = values;
            }


            public override int GetOrd(int docID)
            {
                return (int)docToOrd.Get(docID);
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                values.Get(ord, result);
            }

            public override int ValueCount
            {
                get
                {
                    return entry.values.count;
                }
            }

            // Leave lookupTerm to super's binary search

            // Leave termsEnum to super
        }

        public override SortedSetDocValues GetSortedSet(FieldInfo field)
        {
            lock (this)
            {
                var entry = sortedSets[field.Number];
                SortedSetRawValues instance;
                if (!sortedSetInstances.TryGetValue(field.Number, out instance))
                {
                    // Lazy load
                    instance = LoadSortedSet(entry);
                    sortedSetInstances[field.Number] = instance;
                }

                var docToOrdAddress = instance.docToOrdAddress;
                var ords = instance.ords;
                var values = instance.values;

                // Must make a new instance since the iterator has state:
                return new RandomAccessOrdsAnonymousInnerClassHelper(this, entry, docToOrdAddress, ords, values);
            }
        }

        private class RandomAccessOrdsAnonymousInnerClassHelper : RandomAccessOrds
        {
            private readonly SortedSetEntry entry;
            private readonly NumericDocValues docToOrdAddress;
            private readonly NumericDocValues ords;
            private readonly BinaryDocValues values;

            public RandomAccessOrdsAnonymousInnerClassHelper(DirectDocValuesProducer outerInstance, SortedSetEntry entry, NumericDocValues docToOrdAddress, NumericDocValues ords, BinaryDocValues values)
            {
                this.entry = entry;
                this.docToOrdAddress = docToOrdAddress;
                this.ords = ords;
                this.values = values;
            }

            private int ordStart;
            private int ordUpto;
            private int ordLimit;

            public override long NextOrd()
            {
                if (ordUpto == ordLimit)
                {
                    return NO_MORE_ORDS;
                }
                else
                {
                    return ords.Get(ordUpto++);
                }
            }

            public override int Document
            {
                set
                {
                    ordStart = ordUpto = (int)docToOrdAddress.Get(value);
                    ordLimit = (int)docToOrdAddress.Get(value + 1);
                }
            }

            public override void LookupOrd(long ord, BytesRef result)
            {
                values.Get((int)ord, result);
            }

            public override long ValueCount
            {
                get
                {
                    return entry.values.count;
                }
            }

            public override long OrdAt(int index)
            {
                return ords.Get(ordStart + index);
            }

            public override int Cardinality()
            {
                return ordLimit - ordStart;
            }

            // Leave lookupTerm to super's binary search

            // Leave termsEnum to super
        }

        private SortedSetRawValues LoadSortedSet(SortedSetEntry entry)
        {
            var instance = new SortedSetRawValues();
            instance.docToOrdAddress = LoadNumeric(entry.docToOrdAddress);
            instance.ords = LoadNumeric(entry.ords);
            instance.values = LoadBinary(entry.values);
            return instance;
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
                        Debug.Assert(length % 8 == 0);
                        var bits = new long[(int)length >> 3];
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
                case DocValuesType_e.SORTED_SET:
                    return DocValues.DocsWithValue(GetSortedSet(field), maxDoc);
                case DocValuesType_e.SORTED:
                    return DocValues.DocsWithValue(GetSorted(field), maxDoc);
                case DocValuesType_e.BINARY:
                    BinaryEntry be = binaries[field.Number];
                    return GetMissingBits(field.Number, be.missingOffset, be.missingBytes);
                case DocValuesType_e.NUMERIC:
                    NumericEntry ne = numerics[field.Number];
                    return GetMissingBits(field.Number, ne.missingOffset, ne.missingBytes);
                default:
                    throw new System.ArgumentOutOfRangeException();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                data.Dispose();
        }

        internal class SortedSetRawValues
        {
            internal NumericDocValues docToOrdAddress;
            internal NumericDocValues ords;
            internal BinaryDocValues values;
        }

        internal class NumericEntry
        {
            internal long offset;
            internal int count;
            internal long missingOffset;
            internal long missingBytes;
            internal byte byteWidth;
            internal int packedIntsVersion;
        }

        internal class BinaryEntry
        {
            internal long offset;
            internal long missingOffset;
            internal long missingBytes;
            internal int count;
            internal int numBytes;
            internal int minLength;
            internal int maxLength;
            internal int packedIntsVersion;
            internal int blockSize;
        }

        internal class SortedEntry
        {
            internal NumericEntry docToOrd;
            internal BinaryEntry values;
        }

        internal class SortedSetEntry
        {
            internal NumericEntry docToOrdAddress;
            internal NumericEntry ords;
            internal BinaryEntry values;
        }

        internal class FSTEntry
        {
            internal long offset;
            internal long numOrds;
        }
    }
}