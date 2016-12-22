using System;

namespace Lucene.Net.Search.Similarities
{
    using BytesRef = Lucene.Net.Util.BytesRef;

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

    using FieldInvertState = Lucene.Net.Index.FieldInvertState;
    using SmallFloat = Lucene.Net.Util.SmallFloat;

    /// <summary>
    /// Expert: Default scoring implementation which {@link #encodeNormValue(float)
    /// encodes} norm values as a single byte before being stored. At search time,
    /// the norm byte value is read from the index
    /// <seealso cref="Lucene.Net.Store.Directory directory"/> and
    /// <seealso cref="#decodeNormValue(long) decoded"/> back to a float <i>norm</i> value.
    /// this encoding/decoding, while reducing index size, comes with the price of
    /// precision loss - it is not guaranteed that <i>decode(encode(x)) = x</i>. For
    /// instance, <i>decode(encode(0.89)) = 0.75</i>.
    /// <p>
    /// Compression of norm values to a single byte saves memory at search time,
    /// because once a field is referenced at search time, its norms - for all
    /// documents - are maintained in memory.
    /// <p>
    /// The rationale supporting such lossy compression of norm values is that given
    /// the difficulty (and inaccuracy) of users to express their true information
    /// need by a query, only big differences matter. <br>
    /// &nbsp;<br>
    /// Last, note that search time is too late to modify this <i>norm</i> part of
    /// scoring, e.g. by using a different <seealso cref="Similarity"/> for search.
    /// </summary>
    public class DefaultSimilarity : TFIDFSimilarity
    {
        /// <summary>
        /// Cache of decoded bytes. </summary>
        private static readonly float[] NORM_TABLE = new float[256];

        static DefaultSimilarity()
        {
            for (int i = 0; i < 256; i++)
            {
                NORM_TABLE[i] = SmallFloat.Byte315ToFloat((sbyte)i);
            }
        }

        /// <summary>
        /// Sole constructor: parameter-free </summary>
        public DefaultSimilarity()
        {
        }

        /// <summary>
        /// Implemented as <code>overlap / maxOverlap</code>. </summary>
        public override float Coord(int overlap, int maxOverlap)
        {
            return overlap / (float)maxOverlap;
        }

        /// <summary>
        /// Implemented as <code>1/sqrt(sumOfSquaredWeights)</code>. </summary>
        public override float QueryNorm(float sumOfSquaredWeights)
        {
            return (float)(1.0 / Math.Sqrt(sumOfSquaredWeights));
        }

        /// <summary>
        /// Encodes a normalization factor for storage in an index.
        /// <p>
        /// The encoding uses a three-bit mantissa, a five-bit exponent, and the
        /// zero-exponent point at 15, thus representing values from around 7x10^9 to
        /// 2x10^-9 with about one significant decimal digit of accuracy. Zero is also
        /// represented. Negative numbers are rounded up to zero. Values too large to
        /// represent are rounded down to the largest representable value. Positive
        /// values too small to represent are rounded up to the smallest positive
        /// representable value.
        /// </summary>
        /// <seealso cref= Lucene.Net.Document.Field#setBoost(float) </seealso>
        /// <seealso cref= Lucene.Net.Util.SmallFloat </seealso>
        public override sealed long EncodeNormValue(float f)
        {
            return SmallFloat.FloatToByte315(f);
        }

        /// <summary>
        /// Decodes the norm value, assuming it is a single byte.
        /// </summary>
        /// <seealso cref= #encodeNormValue(float) </seealso>
        public override sealed float DecodeNormValue(long norm)
        {
            return NORM_TABLE[(int)(norm & 0xFF)]; // & 0xFF maps negative bytes to positive above 127
        }

        /// <summary>
        /// Implemented as
        ///  <code>state.getBoost()*lengthNorm(numTerms)</code>, where
        ///  <code>numTerms</code> is <seealso cref="FieldInvertState#getLength()"/> if {@link
        ///  #setDiscountOverlaps} is false, else it's {@link
        ///  FieldInvertState#getLength()} - {@link
        ///  FieldInvertState#getNumOverlap()}.
        ///
        ///  @lucene.experimental
        /// </summary>
        public override float LengthNorm(FieldInvertState state)
        {
            int numTerms;
            if (DiscountOverlaps_Renamed)
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
        /// Implemented as <code>sqrt(freq)</code>. </summary>
        public override float Tf(float freq)
        {
            return (float)Math.Sqrt(freq);
        }

        /// <summary>
        /// Implemented as <code>1 / (distance + 1)</code>. </summary>
        public override float SloppyFreq(int distance)
        {
            return 1.0f / (distance + 1);
        }

        /// <summary>
        /// The default implementation returns <code>1</code> </summary>
        public override float ScorePayload(int doc, int start, int end, BytesRef payload)
        {
            return 1;
        }

        /// <summary>
        /// Implemented as <code>log(numDocs/(docFreq+1)) + 1</code>. </summary>
        public override float Idf(long docFreq, long numDocs)
        {
            return (float)(Math.Log(numDocs / (double)(docFreq + 1)) + 1.0);
        }

        /// <summary>
        /// True if overlap tokens (tokens with a position of increment of zero) are
        /// discounted from the document's length.
        /// </summary>
        protected bool DiscountOverlaps_Renamed = true; // LUCENENET TODO: rename

        /// <summary>
        /// Determines whether overlap tokens (Tokens with
        ///  0 position increment) are ignored when computing
        ///  norm.  By default this is true, meaning overlap
        ///  tokens do not count when computing norms.
        ///
        ///  @lucene.experimental
        /// </summary>
        ///  <seealso cref= #computeNorm </seealso>
        public virtual bool DiscountOverlaps
        {
            set
            {
                DiscountOverlaps_Renamed = value;
            }
            get
            {
                return DiscountOverlaps_Renamed;
            }
        }

        public override string ToString()
        {
            return "DefaultSimilarity";
        }
    }
}