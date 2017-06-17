using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Lucene.Net.Codecs.Lucene40
{
    using BytesRef = Lucene.Net.Util.BytesRef;
    using CompoundFileDirectory = Lucene.Net.Store.CompoundFileDirectory;
    using Directory = Lucene.Net.Store.Directory;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOUtils = Lucene.Net.Util.IOUtils;

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

    //using LegacyDocValuesType = Lucene.Net.Codecs.Lucene40.LegacyDocValuesType;
    using PackedInt32s = Lucene.Net.Util.Packed.PackedInt32s;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;

#pragma warning disable 612, 618
    internal class Lucene40DocValuesWriter : DocValuesConsumer
    {
        private readonly Directory Dir;
        private readonly SegmentWriteState State;
        private readonly string LegacyKey;
        private const string SegmentSuffix = "dv";

        // note: intentionally ignores seg suffix
        internal Lucene40DocValuesWriter(SegmentWriteState state, string filename, string legacyKey)
        {
            this.State = state;
            this.LegacyKey = legacyKey;
            this.Dir = new CompoundFileDirectory(state.Directory, filename, state.Context, true);
        }

        public override void AddNumericField(FieldInfo field, IEnumerable<long?> values)
        {
            // examine the values to determine best type to use
            long minValue = long.MaxValue;
            long maxValue = long.MinValue;
            foreach (long? n in values)
            {
                long v = n.GetValueOrDefault();
                minValue = Math.Min(minValue, v);
                maxValue = Math.Max(maxValue, v);
            }

            string fileName = IndexFileNames.SegmentFileName(State.SegmentInfo.Name + "_" + Convert.ToString(field.Number, CultureInfo.InvariantCulture), SegmentSuffix, "dat");
            IndexOutput data = Dir.CreateOutput(fileName, State.Context);
            bool success = false;
            try
            {
                if (minValue >= sbyte.MinValue && maxValue <= sbyte.MaxValue && PackedInt32s.BitsRequired(maxValue - minValue) > 4)
                {
                    // fits in a byte[], would be more than 4bpv, just write byte[]
                    AddBytesField(field, data, values);
                }
                else if (minValue >= short.MinValue && maxValue <= short.MaxValue && PackedInt32s.BitsRequired(maxValue - minValue) > 8)
                {
                    // fits in a short[], would be more than 8bpv, just write short[]
                    AddShortsField(field, data, values);
                }
                else if (minValue >= int.MinValue && maxValue <= int.MaxValue && PackedInt32s.BitsRequired(maxValue - minValue) > 16)
                {
                    // fits in a int[], would be more than 16bpv, just write int[]
                    AddIntsField(field, data, values);
                }
                else
                {
                    AddVarIntsField(field, data, values, minValue, maxValue);
                }
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Dispose(data);
                }
                else
                {
                    IOUtils.DisposeWhileHandlingException(data);
                }
            }
        }

        private void AddBytesField(FieldInfo field, IndexOutput output, IEnumerable<long?> values)
        {
            field.PutAttribute(LegacyKey, LegacyDocValuesType.FIXED_INTS_8.ToString());
            CodecUtil.WriteHeader(output, Lucene40DocValuesFormat.INTS_CODEC_NAME, Lucene40DocValuesFormat.INTS_VERSION_CURRENT);
            output.WriteInt32(1); // size
            foreach (long? n in values)
            {
                output.WriteByte((byte)n.GetValueOrDefault());
            }
        }

        private void AddShortsField(FieldInfo field, IndexOutput output, IEnumerable<long?> values)
        {
            field.PutAttribute(LegacyKey, LegacyDocValuesType.FIXED_INTS_16.ToString());
            CodecUtil.WriteHeader(output, Lucene40DocValuesFormat.INTS_CODEC_NAME, Lucene40DocValuesFormat.INTS_VERSION_CURRENT);
            output.WriteInt32(2); // size
            foreach (long? n in values)
            {
                output.WriteInt16((short)n.GetValueOrDefault());
            }
        }

        private void AddIntsField(FieldInfo field, IndexOutput output, IEnumerable<long?> values)
        {
            field.PutAttribute(LegacyKey, LegacyDocValuesType.FIXED_INTS_32.ToString());
            CodecUtil.WriteHeader(output, Lucene40DocValuesFormat.INTS_CODEC_NAME, Lucene40DocValuesFormat.INTS_VERSION_CURRENT);
            output.WriteInt32(4); // size
            foreach (long? n in values)
            {
                output.WriteInt32((int)n.GetValueOrDefault());
            }
        }

        private void AddVarIntsField(FieldInfo field, IndexOutput output, IEnumerable<long?> values, long minValue, long maxValue)
        {
            field.PutAttribute(LegacyKey, LegacyDocValuesType.VAR_INTS.ToString());

            CodecUtil.WriteHeader(output, Lucene40DocValuesFormat.VAR_INTS_CODEC_NAME, Lucene40DocValuesFormat.VAR_INTS_VERSION_CURRENT);

            long delta = maxValue - minValue;

            if (delta < 0)
            {
                // writes longs
                output.WriteByte((byte)Lucene40DocValuesFormat.VAR_INTS_FIXED_64);
                foreach (long? n in values)
                {
                    output.WriteInt64(n.GetValueOrDefault());
                }
            }
            else
            {
                // writes packed ints
                output.WriteByte((byte)Lucene40DocValuesFormat.VAR_INTS_PACKED);
                output.WriteInt64(minValue);
                output.WriteInt64(0 - minValue); // default value (representation of 0)
                PackedInt32s.Writer writer = PackedInt32s.GetWriter(output, State.SegmentInfo.DocCount, PackedInt32s.BitsRequired(delta), PackedInt32s.DEFAULT);
                foreach (long? n in values)
                {
                    writer.Add(n.GetValueOrDefault() - minValue);
                }
                writer.Finish();
            }
        }

        public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
        {
            // examine the values to determine best type to use
            HashSet<BytesRef> uniqueValues = new HashSet<BytesRef>();
            int minLength = int.MaxValue;
            int maxLength = int.MinValue;

            var vals = values.ToArray();

            for (int i = 0; i < vals.Length; i++)
            {
                var b = vals[i];

                if (b == null)
                {
                    b = vals[i] = new BytesRef(); // 4.0 doesnt distinguish
                }
                if (b.Length > Lucene40DocValuesFormat.MAX_BINARY_FIELD_LENGTH)
                {
                    throw new System.ArgumentException("DocValuesField \"" + field.Name + "\" is too large, must be <= " + Lucene40DocValuesFormat.MAX_BINARY_FIELD_LENGTH);
                }
                minLength = Math.Min(minLength, b.Length);
                maxLength = Math.Max(maxLength, b.Length);
                if (uniqueValues != null)
                {
                    if (uniqueValues.Add(BytesRef.DeepCopyOf(b)))
                    {
                        if (uniqueValues.Count > 256)
                        {
                            uniqueValues = null;
                        }
                    }
                }
            }

            int maxDoc = State.SegmentInfo.DocCount;
            bool @fixed = minLength == maxLength;
            bool dedup = uniqueValues != null && uniqueValues.Count * 2 < maxDoc;

            if (dedup)
            {
                // we will deduplicate and deref values
                bool success = false;
                IndexOutput data = null;
                IndexOutput index = null;
                string dataName = IndexFileNames.SegmentFileName(State.SegmentInfo.Name + "_" + Convert.ToString(field.Number, CultureInfo.InvariantCulture), SegmentSuffix, "dat");
                string indexName = IndexFileNames.SegmentFileName(State.SegmentInfo.Name + "_" + Convert.ToString(field.Number, CultureInfo.InvariantCulture), SegmentSuffix, "idx");
                try
                {
                    data = Dir.CreateOutput(dataName, State.Context);
                    index = Dir.CreateOutput(indexName, State.Context);
                    if (@fixed)
                    {
                        AddFixedDerefBytesField(field, data, index, values, minLength);
                    }
                    else
                    {
                        AddVarDerefBytesField(field, data, index, values);
                    }
                    success = true;
                }
                finally
                {
                    if (success)
                    {
                        IOUtils.Dispose(data, index);
                    }
                    else
                    {
                        IOUtils.DisposeWhileHandlingException(data, index);
                    }
                }
            }
            else
            {
                // we dont deduplicate, just write values straight
                if (@fixed)
                {
                    // fixed byte[]
                    string fileName = IndexFileNames.SegmentFileName(State.SegmentInfo.Name + "_" + Convert.ToString(field.Number, CultureInfo.InvariantCulture), SegmentSuffix, "dat");
                    IndexOutput data = Dir.CreateOutput(fileName, State.Context);
                    bool success = false;
                    try
                    {
                        AddFixedStraightBytesField(field, data, values, minLength);
                        success = true;
                    }
                    finally
                    {
                        if (success)
                        {
                            IOUtils.Dispose(data);
                        }
                        else
                        {
                            IOUtils.DisposeWhileHandlingException(data);
                        }
                    }
                }
                else
                {
                    // variable byte[]
                    bool success = false;
                    IndexOutput data = null;
                    IndexOutput index = null;
                    string dataName = IndexFileNames.SegmentFileName(State.SegmentInfo.Name + "_" + Convert.ToString(field.Number, CultureInfo.InvariantCulture), SegmentSuffix, "dat");
                    string indexName = IndexFileNames.SegmentFileName(State.SegmentInfo.Name + "_" + Convert.ToString(field.Number, CultureInfo.InvariantCulture), SegmentSuffix, "idx");
                    try
                    {
                        data = Dir.CreateOutput(dataName, State.Context);
                        index = Dir.CreateOutput(indexName, State.Context);
                        AddVarStraightBytesField(field, data, index, values);
                        success = true;
                    }
                    finally
                    {
                        if (success)
                        {
                            IOUtils.Dispose(data, index);
                        }
                        else
                        {
                            IOUtils.DisposeWhileHandlingException(data, index);
                        }
                    }
                }
            }
        }

        private void AddFixedStraightBytesField(FieldInfo field, IndexOutput output, IEnumerable<BytesRef> values, int length)
        {
            field.PutAttribute(LegacyKey, LegacyDocValuesType.BYTES_FIXED_STRAIGHT.ToString());

            CodecUtil.WriteHeader(output, Lucene40DocValuesFormat.BYTES_FIXED_STRAIGHT_CODEC_NAME, Lucene40DocValuesFormat.BYTES_FIXED_STRAIGHT_VERSION_CURRENT);

            output.WriteInt32(length);
            foreach (BytesRef v in values)
            {
                if (v != null)
                {
                    output.WriteBytes(v.Bytes, v.Offset, v.Length);
                }
            }
        }

        // NOTE: 4.0 file format docs are crazy/wrong here...
        private void AddVarStraightBytesField(FieldInfo field, IndexOutput data, IndexOutput index, IEnumerable<BytesRef> values)
        {
            field.PutAttribute(LegacyKey, LegacyDocValuesType.BYTES_VAR_STRAIGHT.ToString());

            CodecUtil.WriteHeader(data, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_CODEC_NAME_DAT, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_VERSION_CURRENT);

            CodecUtil.WriteHeader(index, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_CODEC_NAME_IDX, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_VERSION_CURRENT);

            /* values */

            long startPos = data.GetFilePointer();

            foreach (BytesRef v in values)
            {
                if (v != null)
                {
                    data.WriteBytes(v.Bytes, v.Offset, v.Length);
                }
            }

            /* addresses */

            long maxAddress = data.GetFilePointer() - startPos;
            index.WriteVInt64(maxAddress);

            int maxDoc = State.SegmentInfo.DocCount;
            Debug.Assert(maxDoc != int.MaxValue); // unsupported by the 4.0 impl

            PackedInt32s.Writer w = PackedInt32s.GetWriter(index, maxDoc + 1, PackedInt32s.BitsRequired(maxAddress), PackedInt32s.DEFAULT);
            long currentPosition = 0;
            foreach (BytesRef v in values)
            {
                w.Add(currentPosition);
                if (v != null)
                {
                    currentPosition += v.Length;
                }
            }
            // write sentinel
            Debug.Assert(currentPosition == maxAddress);
            w.Add(currentPosition);
            w.Finish();
        }

        private void AddFixedDerefBytesField(FieldInfo field, IndexOutput data, IndexOutput index, IEnumerable<BytesRef> values, int length)
        {
            field.PutAttribute(LegacyKey, LegacyDocValuesType.BYTES_FIXED_DEREF.ToString());

            CodecUtil.WriteHeader(data, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_CODEC_NAME_DAT, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_VERSION_CURRENT);

            CodecUtil.WriteHeader(index, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_CODEC_NAME_IDX, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_VERSION_CURRENT);

            // deduplicate
            SortedSet<BytesRef> dictionary = new SortedSet<BytesRef>();
            foreach (BytesRef v in values)
            {
                dictionary.Add(v == null ? new BytesRef() : BytesRef.DeepCopyOf(v));
            }

            /* values */
            data.WriteInt32(length);
            foreach (BytesRef v in dictionary)
            {
                data.WriteBytes(v.Bytes, v.Offset, v.Length);
            }

            /* ordinals */
            int valueCount = dictionary.Count;
            Debug.Assert(valueCount > 0);
            index.WriteInt32(valueCount);
            int maxDoc = State.SegmentInfo.DocCount;
            PackedInt32s.Writer w = PackedInt32s.GetWriter(index, maxDoc, PackedInt32s.BitsRequired(valueCount - 1), PackedInt32s.DEFAULT);

            BytesRef brefDummy;
            foreach (BytesRef v in values)
            {
                brefDummy = v;

                if (v == null)
                {
                    brefDummy = new BytesRef();
                }
                //int ord = dictionary.HeadSet(brefDummy).Size();
                int ord = dictionary.Count(@ref => @ref.CompareTo(brefDummy) < 0);
                w.Add(ord);
            }
            w.Finish();
        }

        private void AddVarDerefBytesField(FieldInfo field, IndexOutput data, IndexOutput index, IEnumerable<BytesRef> values)
        {
            field.PutAttribute(LegacyKey, LegacyDocValuesType.BYTES_VAR_DEREF.ToString());

            CodecUtil.WriteHeader(data, Lucene40DocValuesFormat.BYTES_VAR_DEREF_CODEC_NAME_DAT, Lucene40DocValuesFormat.BYTES_VAR_DEREF_VERSION_CURRENT);

            CodecUtil.WriteHeader(index, Lucene40DocValuesFormat.BYTES_VAR_DEREF_CODEC_NAME_IDX, Lucene40DocValuesFormat.BYTES_VAR_DEREF_VERSION_CURRENT);

            // deduplicate
            SortedSet<BytesRef> dictionary = new SortedSet<BytesRef>();
            foreach (BytesRef v in values)
            {
                dictionary.Add(v == null ? new BytesRef() : BytesRef.DeepCopyOf(v));
            }

            /* values */
            long startPosition = data.GetFilePointer();
            long currentAddress = 0;
            Dictionary<BytesRef, long> valueToAddress = new Dictionary<BytesRef, long>();
            foreach (BytesRef v in dictionary)
            {
                currentAddress = data.GetFilePointer() - startPosition;
                valueToAddress[v] = currentAddress;
                WriteVShort(data, v.Length);
                data.WriteBytes(v.Bytes, v.Offset, v.Length);
            }

            /* ordinals */
            long totalBytes = data.GetFilePointer() - startPosition;
            index.WriteInt64(totalBytes);
            int maxDoc = State.SegmentInfo.DocCount;
            PackedInt32s.Writer w = PackedInt32s.GetWriter(index, maxDoc, PackedInt32s.BitsRequired(currentAddress), PackedInt32s.DEFAULT);

            foreach (BytesRef v in values)
            {
                w.Add(valueToAddress[v == null ? new BytesRef() : v]);
            }
            w.Finish();
        }

        // the little vint encoding used for var-deref
        private static void WriteVShort(IndexOutput o, int i)
        {
            Debug.Assert(i >= 0 && i <= short.MaxValue);
            if (i < 128)
            {
                o.WriteByte((byte)(sbyte)i);
            }
            else
            {
                o.WriteByte((byte)unchecked((sbyte)(0x80 | (i >> 8))));
                o.WriteByte((byte)unchecked((sbyte)(i & 0xff)));
            }
        }

        public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrd)
        {
            // examine the values to determine best type to use
            int minLength = int.MaxValue;
            int maxLength = int.MinValue;
            foreach (BytesRef b in values)
            {
                minLength = Math.Min(minLength, b.Length);
                maxLength = Math.Max(maxLength, b.Length);
            }

            // but dont use fixed if there are missing values (we are simulating how lucene40 wrote dv...)
            bool anyMissing = false;
            foreach (long n in docToOrd)
            {
                if ((long)n == -1)
                {
                    anyMissing = true;
                    break;
                }
            }

            bool success = false;
            IndexOutput data = null;
            IndexOutput index = null;
            string dataName = IndexFileNames.SegmentFileName(State.SegmentInfo.Name + "_" + Convert.ToString(field.Number, CultureInfo.InvariantCulture), SegmentSuffix, "dat");
            string indexName = IndexFileNames.SegmentFileName(State.SegmentInfo.Name + "_" + Convert.ToString(field.Number, CultureInfo.InvariantCulture), SegmentSuffix, "idx");

            try
            {
                data = Dir.CreateOutput(dataName, State.Context);
                index = Dir.CreateOutput(indexName, State.Context);
                if (minLength == maxLength && !anyMissing)
                {
                    // fixed byte[]
                    AddFixedSortedBytesField(field, data, index, values, docToOrd, minLength);
                }
                else
                {
                    // var byte[]
                    // three cases for simulating the old writer:
                    // 1. no missing
                    // 2. missing (and empty string in use): remap ord=-1 -> ord=0
                    // 3. missing (and empty string not in use): remap all ords +1, insert empty string into values
                    if (!anyMissing)
                    {
                        AddVarSortedBytesField(field, data, index, values, docToOrd);
                    }
                    else if (minLength == 0)
                    {
                        AddVarSortedBytesField(field, data, index, values, MissingOrdRemapper.MapMissingToOrd0(docToOrd));
                    }
                    else
                    {
                        AddVarSortedBytesField(field, data, index, MissingOrdRemapper.InsertEmptyValue(values), MissingOrdRemapper.MapAllOrds(docToOrd));
                    }
                }
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Dispose(data, index);
                }
                else
                {
                    IOUtils.DisposeWhileHandlingException(data, index);
                }
            }
        }

        private void AddFixedSortedBytesField(FieldInfo field, IndexOutput data, IndexOutput index, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrd, int length)
        {
            field.PutAttribute(LegacyKey, LegacyDocValuesType.BYTES_FIXED_SORTED.ToString());

            CodecUtil.WriteHeader(data, Lucene40DocValuesFormat.BYTES_FIXED_SORTED_CODEC_NAME_DAT, Lucene40DocValuesFormat.BYTES_FIXED_SORTED_VERSION_CURRENT);

            CodecUtil.WriteHeader(index, Lucene40DocValuesFormat.BYTES_FIXED_SORTED_CODEC_NAME_IDX, Lucene40DocValuesFormat.BYTES_FIXED_SORTED_VERSION_CURRENT);

            /* values */

            data.WriteInt32(length);
            int valueCount = 0;
            foreach (BytesRef v in values)
            {
                data.WriteBytes(v.Bytes, v.Offset, v.Length);
                valueCount++;
            }

            /* ordinals */

            index.WriteInt32(valueCount);
            int maxDoc = State.SegmentInfo.DocCount;
            Debug.Assert(valueCount > 0);
            PackedInt32s.Writer w = PackedInt32s.GetWriter(index, maxDoc, PackedInt32s.BitsRequired(valueCount - 1), PackedInt32s.DEFAULT);
            foreach (long n in docToOrd)
            {
                w.Add((long)n);
            }
            w.Finish();
        }

        private void AddVarSortedBytesField(FieldInfo field, IndexOutput data, IndexOutput index, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrd)
        {
            field.PutAttribute(LegacyKey, LegacyDocValuesType.BYTES_VAR_SORTED.ToString());

            CodecUtil.WriteHeader(data, Lucene40DocValuesFormat.BYTES_VAR_SORTED_CODEC_NAME_DAT, Lucene40DocValuesFormat.BYTES_VAR_SORTED_VERSION_CURRENT);

            CodecUtil.WriteHeader(index, Lucene40DocValuesFormat.BYTES_VAR_SORTED_CODEC_NAME_IDX, Lucene40DocValuesFormat.BYTES_VAR_SORTED_VERSION_CURRENT);

            /* values */

            long startPos = data.GetFilePointer();

            int valueCount = 0;
            foreach (BytesRef v in values)
            {
                data.WriteBytes(v.Bytes, v.Offset, v.Length);
                valueCount++;
            }

            /* addresses */

            long maxAddress = data.GetFilePointer() - startPos;
            index.WriteInt64(maxAddress);

            Debug.Assert(valueCount != int.MaxValue); // unsupported by the 4.0 impl

            PackedInt32s.Writer w = PackedInt32s.GetWriter(index, valueCount + 1, PackedInt32s.BitsRequired(maxAddress), PackedInt32s.DEFAULT);
            long currentPosition = 0;
            foreach (BytesRef v in values)
            {
                w.Add(currentPosition);
                currentPosition += v.Length;
            }
            // write sentinel
            Debug.Assert(currentPosition == maxAddress);
            w.Add(currentPosition);
            w.Finish();

            /* ordinals */

            int maxDoc = State.SegmentInfo.DocCount;
            Debug.Assert(valueCount > 0);
            PackedInt32s.Writer ords = PackedInt32s.GetWriter(index, maxDoc, PackedInt32s.BitsRequired(valueCount - 1), PackedInt32s.DEFAULT);
            foreach (long n in docToOrd)
            {
                ords.Add((long)n);
            }
            ords.Finish();
        }

        public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrdCount, IEnumerable<long?> ords)
        {
            throw new System.NotSupportedException("Lucene 4.0 does not support SortedSet docvalues");
        }

        protected override void Dispose(bool disposing)
        {
            Dir.Dispose();
        }
    }
#pragma warning restore 612, 618
}