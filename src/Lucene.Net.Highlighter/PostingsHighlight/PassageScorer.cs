#if FEATURE_BREAKITERATOR
using System;

namespace Lucene.Net.Search.PostingsHighlight
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
    /// Ranks passages found by <see cref="ICUPostingsHighlighter"/>.
    /// <para/>
    /// Each passage is scored as a miniature document within the document.
    /// The final score is computed as <c>norm</c> * ∑ (<c>weight</c> * <c>tf</c>).
    /// The default implementation is <c>norm</c> * BM25.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class PassageScorer
    {
        // TODO: this formula is completely made up. It might not provide relevant snippets!

        /// <summary>BM25 k1 parameter, controls term frequency normalization</summary>
        internal readonly float k1;
        /// <summary>BM25 b parameter, controls length normalization.</summary>
        internal readonly float b;
        /// <summary>A pivot used for length normalization.</summary>
        internal readonly float pivot;

        /// <summary>
        /// Creates <see cref="PassageScorer"/> with these default values:
        /// <list type="bullet">
        ///     <item><description><c>k1 = 1.2</c></description></item>
        ///     <item><description><c>b = 0.75</c></description></item>
        ///     <item><description><c>pivot = 87</c></description></item>
        /// </list>
        /// </summary>
        public PassageScorer()
            // 1.2 and 0.75 are well-known bm25 defaults (but maybe not the best here) ?
            // 87 is typical average english sentence length.
            : this(1.2f, 0.75f, 87f)
        {
        }

        /// <summary>
        /// Creates <see cref="PassageScorer"/> with specified scoring parameters
        /// </summary>
        /// <param name="k1">Controls non-linear term frequency normalization (saturation).</param>
        /// <param name="b">Controls to what degree passage length normalizes tf values.</param>
        /// <param name="pivot">Pivot value for length normalization (some rough idea of average sentence length in characters).</param>
        public PassageScorer(float k1, float b, float pivot)
        {
            this.k1 = k1;
            this.b = b;
            this.pivot = pivot;
        }

        /// <summary>
        /// Computes term importance, given its in-document statistics.
        /// </summary>
        /// <param name="contentLength">length of document in characters</param>
        /// <param name="totalTermFreq">number of time term occurs in document</param>
        /// <returns>term importance</returns>
        public virtual float Weight(int contentLength, int totalTermFreq)
        {
            // approximate #docs from content length
            float numDocs = 1 + contentLength / pivot;
            // numDocs not numDocs - docFreq (ala DFR), since we approximate numDocs
            return (k1 + 1) * (float)Math.Log(1 + (numDocs + 0.5D) / (totalTermFreq + 0.5D));
        }

        /// <summary>
        /// Computes term weight, given the frequency within the passage
        /// and the passage's length.
        /// </summary>
        /// <param name="freq">number of occurrences of within this passage</param>
        /// <param name="passageLen">length of the passage in characters.</param>
        /// <returns>term weight</returns>
        public virtual float Tf(int freq, int passageLen)
        {
            float norm = k1 * ((1 - b) + b * (passageLen / pivot));
            return freq / (freq + norm);
        }

        /// <summary>
        /// Normalize a passage according to its position in the document.
        /// <para/>
        /// Typically passages towards the beginning of the document are 
        /// more useful for summarizing the contents.
        /// <para/>
        /// The default implementation is <c>1 + 1/log(pivot + passageStart)</c>
        /// </summary>
        /// <param name="passageStart">start offset of the passage</param>
        /// <returns>a boost value multiplied into the passage's core.</returns>
        public virtual float Norm(int passageStart)
        {
            return 1 + 1 / (float)Math.Log(pivot + passageStart);
        }
    }
}
#endif