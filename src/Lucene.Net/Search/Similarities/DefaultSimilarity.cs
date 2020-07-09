using System;

namespace Lucene.Net.Search.Similarities
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using FieldInvertState = Lucene.Net.Index.FieldInvertState;
    using SmallSingle = Lucene.Net.Util.SmallSingle;

    /// <summary>
    /// Expert: Default scoring implementation which encodes (<see cref="EncodeNormValue(float)"/>)
    /// norm values as a single byte before being stored. At search time,
    /// the norm byte value is read from the index
    /// <see cref="Lucene.Net.Store.Directory"/> and
    /// decoded (<see cref="DecodeNormValue(long)"/>) back to a float <i>norm</i> value.
    /// this encoding/decoding, while reducing index size, comes with the price of
    /// precision loss - it is not guaranteed that <i>Decode(Encode(x)) = x</i>. For
    /// instance, <i>Decode(Encode(0.89)) = 0.75</i>.
    /// <para/>
    /// Compression of norm values to a single byte saves memory at search time,
    /// because once a field is referenced at search time, its norms - for all
    /// documents - are maintained in memory.
    /// <para/>
    /// The rationale supporting such lossy compression of norm values is that given
    /// the difficulty (and inaccuracy) of users to express their true information
    /// need by a query, only big differences matter. 
    /// <para/>
    /// Last, note that search time is too late to modify this <i>norm</i> part of
    /// scoring, e.g. by using a different <see cref="Similarity"/> for search.
    /// </summary>
    public class DefaultSimilarity : TFIDFSimilarity
    {
        /// <summary>
        /// Cache of decoded bytes. </summary>
        private static readonly float[] NORM_TABLE = LoadNormTable();

        private static float[] LoadNormTable() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
            float[] normTable = new float[256];
            for (int i = 0; i < 256; i++)
            {
                normTable[i] = SmallSingle.SByte315ToSingle((sbyte)i);
            }
            return normTable;
        }

        /// <summary>
        /// Sole constructor: parameter-free </summary>
        public DefaultSimilarity()
        {
        }

        /// <summary>
        /// Implemented as <c>overlap / maxOverlap</c>. </summary>
        public override float Coord(int overlap, int maxOverlap)
        {
            return overlap / (float)maxOverlap;
        }

        /// <summary>
        /// Implemented as <c>1/sqrt(sumOfSquaredWeights)</c>. </summary>
        public override float QueryNorm(float sumOfSquaredWeights)
        {
            return (float)(1.0 / Math.Sqrt(sumOfSquaredWeights));
        }

        /// <summary>
        /// Encodes a normalization factor for storage in an index.
        /// <para/>
        /// The encoding uses a three-bit mantissa, a five-bit exponent, and the
        /// zero-exponent point at 15, thus representing values from around 7x10^9 to
        /// 2x10^-9 with about one significant decimal digit of accuracy. Zero is also
        /// represented. Negative numbers are rounded up to zero. Values too large to
        /// represent are rounded down to the largest representable value. Positive
        /// values too small to represent are rounded up to the smallest positive
        /// representable value.
        /// </summary>
        /// <seealso cref="Lucene.Net.Documents.Field.Boost"/>
        /// <seealso cref="Lucene.Net.Util.SmallSingle"/>
        public override sealed long EncodeNormValue(float f)
        {
            return SmallSingle.SingleToSByte315(f);
        }

        /// <summary>
        /// Decodes the norm value, assuming it is a single byte.
        /// </summary>
        /// <seealso cref="EncodeNormValue(float)"/>
        public override sealed float DecodeNormValue(long norm)
        {
            return NORM_TABLE[(int)(norm & 0xFF)]; // & 0xFF maps negative bytes to positive above 127
        }

        /// <summary>
        /// Implemented as
        /// <c>state.Boost * LengthNorm(numTerms)</c>, where
        /// <c>numTerms</c> is <see cref="FieldInvertState.Length"/> if 
        /// <see cref="DiscountOverlaps"/> is <c>false</c>, else it's 
        /// <see cref="FieldInvertState.Length"/> - 
        /// <see cref="FieldInvertState.NumOverlap"/>.
        ///
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public override float LengthNorm(FieldInvertState state)
        {
            int numTerms;
            if (m_discountOverlaps)
            {
                numTerms = state.Length - state.NumOverlap;
            }
            else
            {
                numTerms = state.Length;
            }
            return state.Boost * ((float)(1.0 / Math.Sqrt(numTerms)));
        }

        /// <summary>
        /// Implemented as <c>Math.Sqrt(freq)</c>. </summary>
        public override float Tf(float freq)
        {
            return (float)Math.Sqrt(freq);
        }

        /// <summary>
        /// Implemented as <c>1 / (distance + 1)</c>. </summary>
        public override float SloppyFreq(int distance)
        {
            return 1.0f / (distance + 1);
        }

        /// <summary>
        /// The default implementation returns <c>1</c> </summary>
        public override float ScorePayload(int doc, int start, int end, BytesRef payload)
        {
            return 1;
        }

        /// <summary>
        /// Implemented as <c>log(numDocs/(docFreq+1)) + 1</c>. </summary>
        public override float Idf(long docFreq, long numDocs)
        {
            return (float)(Math.Log(numDocs / (double)(docFreq + 1)) + 1.0);
        }

        /// <summary>
        /// <c>True</c> if overlap tokens (tokens with a position of increment of zero) are
        /// discounted from the document's length.
        /// </summary>
        protected bool m_discountOverlaps = true;

        /// <summary>
        /// Determines whether overlap tokens (Tokens with
        /// 0 position increment) are ignored when computing
        /// norm.  By default this is true, meaning overlap
        /// tokens do not count when computing norms.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        /// <seealso cref="TFIDFSimilarity.ComputeNorm(FieldInvertState)"/>
        public virtual bool DiscountOverlaps
        {
            get => m_discountOverlaps;
            set => m_discountOverlaps = value;
        }

        public override string ToString()
        {
            return "DefaultSimilarity";
        }
    }
}