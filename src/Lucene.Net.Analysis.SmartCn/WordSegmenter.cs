// lucene version compatibility level: 4.8.1
using J2N.Collections.Generic.Extensions;
using Lucene.Net.Analysis.Cn.Smart.Hhmm;
using Lucene.Net.Support;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Cn.Smart
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
    /// Segment a sentence of Chinese text into words.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    internal class WordSegmenter
    {
        private readonly HHMMSegmenter hhmmSegmenter = new HHMMSegmenter(); // LUCENENET: marked readonly

        private readonly SegTokenFilter tokenFilter = new SegTokenFilter(); // LUCENENET: marked readonly

        /// <summary>
        /// Segment a sentence into words with <see cref="HHMMSegmenter"/>
        /// </summary>
        /// <param name="sentence">input sentence</param>
        /// <param name="startOffset"> start offset of sentence</param>
        /// <returns><see cref="IList{T}"/> of <see cref="SegToken"/>.</returns>
        public virtual IList<SegToken> SegmentSentence(string sentence, int startOffset)
        {

            IList<SegToken> segTokenList = hhmmSegmenter.Process(sentence);
            // tokens from sentence, excluding WordType.SENTENCE_BEGIN and WordType.SENTENCE_END
            IList<SegToken> result = Collections.EmptyList<SegToken>();

            if (segTokenList.Count > 2) // if its not an empty sentence
                result = segTokenList.GetView(1, segTokenList.Count - 2); // LUCENENET: Converted end index to length

            foreach (SegToken st in result)
            {
                ConvertSegToken(st, sentence, startOffset);
            }

            return result;
        }

        /// <summary>
        /// Process a <see cref="SegToken"/> so that it is ready for indexing.
        /// </summary>
        /// <param name="st">st input <see cref="SegToken"/></param>
        /// <param name="sentence">associated Sentence</param>
        /// <param name="sentenceStartOffset">offset into sentence</param>
        /// <returns>Lucene <see cref="SegToken"/></returns>
        public virtual SegToken ConvertSegToken(SegToken st, string sentence,
            int sentenceStartOffset)
        {

            switch (st.WordType)
            {
                case WordType.STRING:
                case WordType.NUMBER:
                case WordType.FULLWIDTH_NUMBER:
                case WordType.FULLWIDTH_STRING:
                    st.CharArray = sentence.Substring(st.StartOffset, st.EndOffset - st.StartOffset)
                        .ToCharArray();
                    break;
                default:
                    break;
            }

            st = tokenFilter.Filter(st);
            st.StartOffset += sentenceStartOffset;
            st.EndOffset += sentenceStartOffset;
            return st;
        }
    }
}
