using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
    using FormatAndBits = Lucene.Net.Util.Packed.PackedInts.FormatAndBits;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using MathUtil = Lucene.Net.Util.MathUtil;
    using PackedInts = Lucene.Net.Util.Packed.PackedInts;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;

    /// <summary>
    /// Writer for <seealso cref="Lucene42NormsFormat"/>
    /// </summary>
    internal class Lucene42NormsConsumer : DocValuesConsumer
    {
        internal const sbyte NUMBER = 0;

        internal const int BLOCK_SIZE = 4096;

        internal const sbyte DELTA_COMPRESSED = 0;
        internal const sbyte TABLE_COMPRESSED = 1;
        internal const sbyte UNCOMPRESSED = 2;
        internal const sbyte GCD_COMPRESSED = 3;

        internal IndexOutput data, meta;
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
                    IOUtils.CloseWhileHandlingException(this);
                }
            }
        }

        public override void AddNumericField(FieldInfo field, IEnumerable<long?> values)
        {
            meta.WriteVInt(field.Number);
            meta.WriteByte((byte)NUMBER);
            meta.WriteLong(data.FilePointer);
            long minValue = long.MaxValue;
            long maxValue = long.MinValue;
            long gcd = 0;
            // TODO: more efficient?
            HashSet<long> uniqueValues = null;
            if (true)
            {
                uniqueValues = new HashSet<long>();

                long count = 0;
                foreach (long? nv in values)
                {
                    Debug.Assert(nv != null);
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
                Debug.Assert(count == maxDoc);
            }

            if (uniqueValues != null)
            {
                // small number of unique values
                int bitsPerValue = PackedInts.BitsRequired(uniqueValues.Count - 1);
                FormatAndBits formatAndBits = PackedInts.FastestFormatAndBits(maxDoc, bitsPerValue, acceptableOverheadRatio);
                if (formatAndBits.BitsPerValue == 8 && minValue >= sbyte.MinValue && maxValue <= sbyte.MaxValue)
                {
                    meta.WriteByte((byte)UNCOMPRESSED); // uncompressed
                    foreach (long? nv in values)
                    {
                        data.WriteByte(nv == null ? (byte)0 : (byte)(sbyte)nv.Value);
                    }
                }
                else
                {
                    meta.WriteByte((byte)TABLE_COMPRESSED); // table-compressed
                    var decode = uniqueValues.ToArray();
                    var encode = new Dictionary<long, int>();
                    data.WriteVInt(decode.Length);
                    for (int i = 0; i < decode.Length; i++)
                    {
                        data.WriteLong(decode[i]);
                        encode[decode[i]] = i;
                    }

                    meta.WriteVInt(PackedInts.VERSION_CURRENT);
                    data.WriteVInt(formatAndBits.Format.Id);
                    data.WriteVInt(formatAndBits.BitsPerValue);

                    PackedInts.Writer writer = PackedInts.GetWriterNoHeader(data, formatAndBits.Format, maxDoc, formatAndBits.BitsPerValue, PackedInts.DEFAULT_BUFFER_SIZE);
                    foreach (long? nv in values)
                    {
                        writer.Add(encode[nv == null ? 0 : nv.Value]);
                    }
                    writer.Finish();
                }
            }
            else if (gcd != 0 && gcd != 1)
            {
                meta.WriteByte((byte)GCD_COMPRESSED);
                meta.WriteVInt(PackedInts.VERSION_CURRENT);
                data.WriteLong(minValue);
                data.WriteLong(gcd);
                data.WriteVInt(BLOCK_SIZE);

                var writer = new BlockPackedWriter(data, BLOCK_SIZE);
                foreach (long? nv in values)
                {
                    long value = nv == null ? 0 : nv.Value;
                    writer.Add((value - minValue) / gcd);
                }
                writer.Finish();
            }
            else
            {
                meta.WriteByte((byte)DELTA_COMPRESSED); // delta-compressed

                meta.WriteVInt(PackedInts.VERSION_CURRENT);
                data.WriteVInt(BLOCK_SIZE);

                var writer = new BlockPackedWriter(data, BLOCK_SIZE);
                foreach (long? nv in values)
                {
                    writer.Add(nv == null ? 0 : nv.Value);
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
                        meta.WriteVInt(-1); // write EOF marker
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
                        IOUtils.Close(data, meta);
                    }
                    else
                    {
                        IOUtils.CloseWhileHandlingException(data, meta);
                    }
                    meta = data = null;
                }
            }
        }

        public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
        {
            throw new NotSupportedException();
        }

        public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrd)
        {
            throw new NotSupportedException();
        }

        public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrdCount, IEnumerable<long?> ords)
        {
            throw new NotSupportedException();
        }
    }
}