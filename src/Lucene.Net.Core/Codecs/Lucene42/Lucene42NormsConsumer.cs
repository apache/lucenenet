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

        internal IndexOutput Data, Meta;
        internal readonly int MaxDoc;
        internal readonly float AcceptableOverheadRatio;

        internal Lucene42NormsConsumer(SegmentWriteState state, string dataCodec, string dataExtension, string metaCodec, string metaExtension, float acceptableOverheadRatio)
        {
            this.AcceptableOverheadRatio = acceptableOverheadRatio;
            MaxDoc = state.SegmentInfo.DocCount;
            bool success = false;
            try
            {
                string dataName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, dataExtension);
                Data = state.Directory.CreateOutput(dataName, state.Context);
                CodecUtil.WriteHeader(Data, dataCodec, Lucene42DocValuesProducer.VERSION_CURRENT);
                string metaName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, metaExtension);
                Meta = state.Directory.CreateOutput(metaName, state.Context);
                CodecUtil.WriteHeader(Meta, metaCodec, Lucene42DocValuesProducer.VERSION_CURRENT);
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
            Meta.WriteVInt(field.Number);
            Meta.WriteByte((byte)NUMBER);
            Meta.WriteLong(Data.FilePointer);
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
                Debug.Assert(count == MaxDoc);
            }

            if (uniqueValues != null)
            {
                // small number of unique values
                int bitsPerValue = PackedInts.BitsRequired(uniqueValues.Count - 1);
                FormatAndBits formatAndBits = PackedInts.FastestFormatAndBits(MaxDoc, bitsPerValue, AcceptableOverheadRatio);
                if (formatAndBits.bitsPerValue == 8 && minValue >= sbyte.MinValue && maxValue <= sbyte.MaxValue)
                {
                    Meta.WriteByte((byte)UNCOMPRESSED); // uncompressed
                    foreach (long? nv in values)
                    {
                        Data.WriteByte(nv == null ? (byte)0 : (byte)(sbyte)nv.Value);
                    }
                }
                else
                {
                    Meta.WriteByte((byte)TABLE_COMPRESSED); // table-compressed
                    var decode = uniqueValues.ToArray();
                    var encode = new Dictionary<long, int>();
                    Data.WriteVInt(decode.Length);
                    for (int i = 0; i < decode.Length; i++)
                    {
                        Data.WriteLong(decode[i]);
                        encode[decode[i]] = i;
                    }

                    Meta.WriteVInt(PackedInts.VERSION_CURRENT);
                    Data.WriteVInt(formatAndBits.format.id);
                    Data.WriteVInt(formatAndBits.bitsPerValue);

                    PackedInts.Writer writer = PackedInts.GetWriterNoHeader(Data, formatAndBits.format, MaxDoc, formatAndBits.bitsPerValue, PackedInts.DEFAULT_BUFFER_SIZE);
                    foreach (long? nv in values)
                    {
                        writer.Add(encode[nv == null ? 0 : nv.Value]);
                    }
                    writer.Finish();
                }
            }
            else if (gcd != 0 && gcd != 1)
            {
                Meta.WriteByte((byte)GCD_COMPRESSED);
                Meta.WriteVInt(PackedInts.VERSION_CURRENT);
                Data.WriteLong(minValue);
                Data.WriteLong(gcd);
                Data.WriteVInt(BLOCK_SIZE);

                var writer = new BlockPackedWriter(Data, BLOCK_SIZE);
                foreach (long? nv in values)
                {
                    long value = nv == null ? 0 : nv.Value;
                    writer.Add((value - minValue) / gcd);
                }
                writer.Finish();
            }
            else
            {
                Meta.WriteByte((byte)DELTA_COMPRESSED); // delta-compressed

                Meta.WriteVInt(PackedInts.VERSION_CURRENT);
                Data.WriteVInt(BLOCK_SIZE);

                var writer = new BlockPackedWriter(Data, BLOCK_SIZE);
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
                    if (Meta != null)
                    {
                        Meta.WriteVInt(-1); // write EOF marker
                        CodecUtil.WriteFooter(Meta); // write checksum
                    }
                    if (Data != null)
                    {
                        CodecUtil.WriteFooter(Data); // write checksum
                    }
                    success = true;
                }
                finally
                {
                    if (success)
                    {
                        IOUtils.Close(Data, Meta);
                    }
                    else
                    {
                        IOUtils.CloseWhileHandlingException(Data, Meta);
                    }
                    Meta = Data = null;
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