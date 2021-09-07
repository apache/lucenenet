using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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

    using BlockPackedWriter = Lucene.Net.Util.Packed.BlockPackedWriter;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FormatAndBits = Lucene.Net.Util.Packed.PackedInt32s.FormatAndBits;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using MathUtil = Lucene.Net.Util.MathUtil;
    using PackedInt32s = Lucene.Net.Util.Packed.PackedInt32s;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;

    /// <summary>
    /// Writer for <see cref="Lucene42NormsFormat"/>.
    /// </summary>
    internal class Lucene42NormsConsumer : DocValuesConsumer
    {
        internal const sbyte NUMBER = 0;

        internal const int BLOCK_SIZE = 4096;

        internal const sbyte DELTA_COMPRESSED = 0;
        internal const sbyte TABLE_COMPRESSED = 1;
        internal const sbyte UNCOMPRESSED = 2;
        internal const sbyte GCD_COMPRESSED = 3;

#pragma warning disable CA2213 // Disposable fields should be disposed
        internal IndexOutput data, meta;
#pragma warning restore CA2213 // Disposable fields should be disposed
        internal readonly int maxDoc;
        internal readonly float acceptableOverheadRatio;

        internal Lucene42NormsConsumer(SegmentWriteState state, string dataCodec, string dataExtension, string metaCodec, string metaExtension, float acceptableOverheadRatio)
        {
            this.acceptableOverheadRatio = acceptableOverheadRatio;
            maxDoc = state.SegmentInfo.DocCount;
            bool success = false;
            try
            {
                string dataName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, dataExtension);
                data = state.Directory.CreateOutput(dataName, state.Context);
                CodecUtil.WriteHeader(data, dataCodec, Lucene42DocValuesProducer.VERSION_CURRENT);
                string metaName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, metaExtension);
                meta = state.Directory.CreateOutput(metaName, state.Context);
                CodecUtil.WriteHeader(meta, metaCodec, Lucene42DocValuesProducer.VERSION_CURRENT);
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
            meta.WriteByte((byte)NUMBER);
            meta.WriteInt64(data.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            long minValue = long.MaxValue;
            long maxValue = long.MinValue;
            long gcd = 0;
            // TODO: more efficient?
            JCG.HashSet<long> uniqueValues/* = null*/; // LUCENENET: IDE0059: Remove unnecessary value assignment
            if (true)
            {
                uniqueValues = new JCG.HashSet<long>();

                long count = 0;
                foreach (long? nv in values)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(nv != null);
                    long v = nv.Value;

                    if (gcd != 1)
                    {
                        if (v < long.MinValue / 2 || v > long.MaxValue / 2)
                        {
                            // in that case v - minValue might overflow and make the GCD computation return
                            // wrong results. Since these extreme values are unlikely, we just discard
                            // GCD computation for them
                            gcd = 1;
                        } // minValue needs to be set first
                        else if (count != 0)
                        {
                            gcd = MathUtil.Gcd(gcd, v - minValue);
                        }
                    }

                    minValue = Math.Min(minValue, v);
                    maxValue = Math.Max(maxValue, v);

                    if (uniqueValues != null)
                    {
                        if (uniqueValues.Add(v))
                        {
                            if (uniqueValues.Count > 256)
                            {
                                uniqueValues = null;
                            }
                        }
                    }

                    ++count;
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(count == maxDoc);
            }

            if (uniqueValues != null)
            {
                // small number of unique values
                int bitsPerValue = PackedInt32s.BitsRequired(uniqueValues.Count - 1);
                FormatAndBits formatAndBits = PackedInt32s.FastestFormatAndBits(maxDoc, bitsPerValue, acceptableOverheadRatio);
                if (formatAndBits.BitsPerValue == 8 && minValue >= sbyte.MinValue && maxValue <= sbyte.MaxValue)
                {
                    meta.WriteByte((byte)UNCOMPRESSED); // uncompressed
                    foreach (long? nv in values)
                    {
                        data.WriteByte((byte)nv.GetValueOrDefault());
                    }
                }
                else
                {
                    meta.WriteByte((byte)TABLE_COMPRESSED); // table-compressed
                    var decode = new long[uniqueValues.Count];
                    uniqueValues.CopyTo(decode);
                    var encode = new Dictionary<long, int>();
                    data.WriteVInt32(decode.Length);
                    for (int i = 0; i < decode.Length; i++)
                    {
                        data.WriteInt64(decode[i]);
                        encode[decode[i]] = i;
                    }

                    meta.WriteVInt32(PackedInt32s.VERSION_CURRENT);
                    data.WriteVInt32(formatAndBits.Format.Id);
                    data.WriteVInt32(formatAndBits.BitsPerValue);

                    PackedInt32s.Writer writer = PackedInt32s.GetWriterNoHeader(data, formatAndBits.Format, maxDoc, formatAndBits.BitsPerValue, PackedInt32s.DEFAULT_BUFFER_SIZE);
                    foreach (long? nv in values)
                    {
                        writer.Add(encode[nv.GetValueOrDefault()]);
                    }
                    writer.Finish();
                }
            }
            else if (gcd != 0 && gcd != 1)
            {
                meta.WriteByte((byte)GCD_COMPRESSED);
                meta.WriteVInt32(PackedInt32s.VERSION_CURRENT);
                data.WriteInt64(minValue);
                data.WriteInt64(gcd);
                data.WriteVInt32(BLOCK_SIZE);

                var writer = new BlockPackedWriter(data, BLOCK_SIZE);
                foreach (long? nv in values)
                {
                    writer.Add((nv.GetValueOrDefault() - minValue) / gcd);
                }
                writer.Finish();
            }
            else
            {
                meta.WriteByte((byte)DELTA_COMPRESSED); // delta-compressed

                meta.WriteVInt32(PackedInt32s.VERSION_CURRENT);
                data.WriteVInt32(BLOCK_SIZE);

                var writer = new BlockPackedWriter(data, BLOCK_SIZE);
                foreach (long? nv in values)
                {
                    writer.Add(nv.GetValueOrDefault());
                }
                writer.Finish();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                bool success = false;
                try
                {
                    if (meta != null)
                    {
                        meta.WriteVInt32(-1); // write EOF marker
                        CodecUtil.WriteFooter(meta); // write checksum
                    }
                    if (data != null)
                    {
                        CodecUtil.WriteFooter(data); // write checksum
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
                    meta = data = null;
                }
            }
        }

        public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
        {
            throw UnsupportedOperationException.Create();
        }

        public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrd)
        {
            throw UnsupportedOperationException.Create();
        }

        public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrdCount, IEnumerable<long?> ords)
        {
            throw UnsupportedOperationException.Create();
        }
    }
}