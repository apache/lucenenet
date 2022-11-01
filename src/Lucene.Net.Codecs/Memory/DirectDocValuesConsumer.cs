using Lucene.Net.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;

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

    using BytesRef = Util.BytesRef;
    using FieldInfo = Index.FieldInfo;
    using IndexFileNames = Index.IndexFileNames;
    using IndexOutput = Store.IndexOutput;
    using IOUtils = Util.IOUtils;
    using SegmentWriteState = Index.SegmentWriteState;

    /// <summary>
    /// Writer for <see cref="DirectDocValuesFormat"/>.
    /// </summary>
    internal class DirectDocValuesConsumer : DocValuesConsumer
    {
#pragma warning disable CA2213 // Disposable fields should be disposed
        private IndexOutput data, meta;
#pragma warning restore CA2213 // Disposable fields should be disposed
        //private readonly int maxDoc; // LUCENENET: Not used

        internal DirectDocValuesConsumer(SegmentWriteState state, string dataCodec, string dataExtension,
            string metaCodec, string metaExtension)
        {
            //maxDoc = state.SegmentInfo.DocCount; // LUCENENET: Not used
            bool success = false;
            try
            {
                string dataName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix,
                    dataExtension);
                data = state.Directory.CreateOutput(dataName, state.Context);
                CodecUtil.WriteHeader(data, dataCodec, DirectDocValuesProducer.VERSION_CURRENT);
                string metaName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix,
                    metaExtension);
                meta = state.Directory.CreateOutput(metaName, state.Context);
                CodecUtil.WriteHeader(meta, metaCodec, DirectDocValuesProducer.VERSION_CURRENT);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(this);
                }
            }
        }

        public override void AddNumericField(FieldInfo field, IEnumerable<long?> values)
        {
            meta.WriteVInt32(field.Number);
            meta.WriteByte(MemoryDocValuesProducer.NUMBER);
            AddNumericFieldValues(field, values);
        }

        private void AddNumericFieldValues(FieldInfo field, IEnumerable<long?> values)
        {
            meta.WriteInt64(data.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            long minValue = long.MaxValue;
            long maxValue = long.MinValue;
            bool missing = false;

            long count = 0;
            foreach (var nv in values)
            {
                if (nv != null)
                {
                    var v = nv.Value;
                    minValue = Math.Min(minValue, v);
                    maxValue = Math.Max(maxValue, v);
                }
                else
                {
                    missing = true;
                }
                count++;
                if (count >= DirectDocValuesFormat.MAX_SORTED_SET_ORDS)
                {
                    throw new ArgumentException("DocValuesField \"" + field.Name + "\" is too large, must be <= " +
                                                       DirectDocValuesFormat.MAX_SORTED_SET_ORDS + " values/total ords");
                }
            }
            meta.WriteInt32((int) count);

            if (missing)
            {
                long start = data.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                WriteMissingBitset(values);
                meta.WriteInt64(start);
                meta.WriteInt64(data.Position - start); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            }
            else
            {
                meta.WriteInt64(-1L);
            }

            byte byteWidth;
            if (minValue >= sbyte.MinValue && maxValue <= sbyte.MaxValue)
            {
                byteWidth = 1;
            }
            else if (minValue >= short.MinValue && maxValue <= short.MaxValue)
            {
                byteWidth = 2;
            }
            else if (minValue >= int.MinValue && maxValue <= int.MaxValue)
            {
                byteWidth = 4;
            }
            else
            {
                byteWidth = 8;
            }
            meta.WriteByte(byteWidth);

            foreach (var nv in values)
            {
                long v = nv.GetValueOrDefault();

                switch (byteWidth)
                {
                    case 1:
                        data.WriteByte((byte) v);
                        break;
                    case 2:
                        data.WriteInt16((short) v);
                        break;
                    case 4:
                        data.WriteInt32((int) v);
                        break;
                    case 8:
                        data.WriteInt64(v);
                        break;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;

            var success = false;
            try
            {
                if (meta != null)
                {
                    meta.WriteVInt32(-1); // write EOF marker
                    CodecUtil.WriteFooter(meta); // write checksum
                }
                if (data != null)
                {
                    CodecUtil.WriteFooter(data);
                }
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Dispose(data, meta);
                }
                else
                {
                    IOUtils.DisposeWhileHandlingException(data, meta);
                }
                data = meta = null;
            }
        }

        public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
        {
            meta.WriteVInt32(field.Number);
            meta.WriteByte(MemoryDocValuesProducer.BYTES);
            AddBinaryFieldValues(field, values);
        }

        private void AddBinaryFieldValues(FieldInfo field, IEnumerable<BytesRef> values)
        {
            // write the byte[] data
            long startFP = data.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            bool missing = false;
            long totalBytes = 0;
            int count = 0;
            foreach (BytesRef v in values)
            {
                if (v != null)
                {
                    data.WriteBytes(v.Bytes, v.Offset, v.Length);
                    totalBytes += v.Length;
                    if (totalBytes > DirectDocValuesFormat.MAX_TOTAL_BYTES_LENGTH)
                    {
                        throw new ArgumentException("DocValuesField \"" + field.Name +
                                                           "\" is too large, cannot have more than DirectDocValuesFormat.MAX_TOTAL_BYTES_LENGTH (" +
                                                           DirectDocValuesFormat.MAX_TOTAL_BYTES_LENGTH + ") bytes");
                    }
                }
                else
                {
                    missing = true;
                }
                count++;
            }

            meta.WriteInt64(startFP);
            meta.WriteInt32((int) totalBytes);
            meta.WriteInt32(count);
            if (missing)
            {
                long start = data.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                WriteMissingBitset(values);
                meta.WriteInt64(start);
                meta.WriteInt64(data.Position - start); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            }
            else
            {
                meta.WriteInt64(-1L);
            }

            int addr = 0;
            foreach (BytesRef v in values)
            {
                data.WriteInt32(addr);
                if (v != null)
                {
                    addr += v.Length;
                }
            }
            data.WriteInt32(addr);
        }

        // TODO: in some cases representing missing with minValue-1 wouldn't take up additional space and so on,
        // but this is very simple, and algorithms only check this for values of 0 anyway (doesnt slow down normal decode)
        internal virtual void WriteMissingBitset<T1>(IEnumerable<T1> values)
        {
            long bits = 0;
            int count = 0;
            foreach (object v in values)
            {
                if (count == 64)
                {
                    data.WriteInt64(bits);
                    count = 0;
                    bits = 0;
                }
                if (v != null)
                {
                    bits |= 1L << (count & 0x3f);
                }
                count++;
            }
            if (count > 0)
            {
                data.WriteInt64(bits);
            }
        }

        public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrd)
        {
            meta.WriteVInt32(field.Number);
            meta.WriteByte((byte)DirectDocValuesProducer.SORTED);

            // write the ordinals as numerics
            AddNumericFieldValues(field, docToOrd);

            // write the values as binary
            AddBinaryFieldValues(field, values);
        }

        // note: this might not be the most efficient... but its fairly simple
        public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values,
            IEnumerable<long?> docToOrdCount, IEnumerable<long?> ords)
        {
            meta.WriteVInt32(field.Number);
            meta.WriteByte((byte)DirectDocValuesProducer.SORTED_SET);

            // First write docToOrdCounts, except we "aggregate" the
            // counts so they turn into addresses, and add a final
            // value = the total aggregate:
            AddNumericFieldValues(field, new EnumerableAnonymousClass(docToOrdCount));

            // Write ordinals for all docs, appended into one big
            // numerics:
            AddNumericFieldValues(field, ords);

            // write the values as binary
            AddBinaryFieldValues(field, values);
        }

        private sealed class EnumerableAnonymousClass : IEnumerable<long?>
        {
            private readonly IEnumerable<long?> _docToOrdCount;

            public EnumerableAnonymousClass(IEnumerable<long?> docToOrdCount)
            {
                _docToOrdCount = docToOrdCount;
            }

            // Just aggregates the count values so they become
            // "addresses", and adds one more value in the end
            // (the final sum):
            public IEnumerator<long?> GetEnumerator()
            {
                return new Enumerator( _docToOrdCount);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<long?>
            {
                private readonly IEnumerator<long?> iter;

                public Enumerator(IEnumerable<long?> docToOrdCount)
                {
                    this.iter = docToOrdCount.GetEnumerator();
                }


                private long sum;
                private bool ended;
                private long toReturn;

                public long? Current => toReturn;

                object IEnumerator.Current => Current;

                // LUCENENET: Remove() not supported by .NET

                public bool MoveNext()
                {
                    if (ended) return false;

                    toReturn = sum;
                    if (iter.MoveNext())
                    {
                        long? n = iter.Current;
                        if (n.HasValue)
                        {
                            sum += n.Value;
                        }

                        return true;
                    }
                    else if (!ended)
                    {
                        ended = true;
                        return true;
                    }
                    else
                    { 
                        if (Debugging.AssertsEnabled) Debugging.Assert(false);
                        return false;
                    }
                }

                public void Reset()
                {
                    throw UnsupportedOperationException.Create();
                }

                #region IDisposable Support
                private bool disposedValue = false; // To detect redundant calls
                public void Dispose()
                {
                    if (!disposedValue)
                    {
                        iter.Dispose();
                        disposedValue = true;
                    }
                }

                #endregion
            }
        }
    }
}