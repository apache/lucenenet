using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
using SubInfo = Lucene.Net.Search.VectorHighlight.FieldFragList.WeightedFragInfo.SubInfo;
using TermInfo = Lucene.Net.Search.VectorHighlight.FieldTermStack.TermInfo;
using WeightedPhraseInfo = Lucene.Net.Search.VectorHighlight.FieldPhraseList.WeightedPhraseInfo;

namespace Lucene.Net.Search.VectorHighlight
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
    /// A weighted implementation of <see cref="FieldFragList"/>.
    /// </summary>
    public class WeightedFieldFragList : FieldFragList
    {
        /// <summary>
        /// a constructor.
        /// </summary>
        /// <param name="fragCharSize">the length (number of chars) of a fragment</param>
        public WeightedFieldFragList(int fragCharSize)
            : base(fragCharSize)
        {
        }

        /// <summary>
        /// <seealso cref="FieldFragList.Add(int, int, IList{WeightedPhraseInfo})"/>.
        /// </summary>
        public override void Add(int startOffset, int endOffset, IList<WeightedPhraseInfo> phraseInfoList)
        {
            IList<SubInfo> tempSubInfos = new JCG.List<SubInfo>();
            IList<SubInfo> realSubInfos = new JCG.List<SubInfo>();
            ISet<string> distinctTerms = new JCG.HashSet<string>();
            int length = 0;

            foreach (WeightedPhraseInfo phraseInfo in phraseInfoList)
            {
                float phraseTotalBoost = 0;
                foreach (TermInfo ti in phraseInfo.TermsInfos)
                {
                    if (distinctTerms.Add(ti.Text))
                        phraseTotalBoost += ti.Weight * phraseInfo.Boost;
                    length++;
                }
                tempSubInfos.Add(new SubInfo(phraseInfo.GetText(), phraseInfo.TermsOffsets,
                    phraseInfo.Seqnum, phraseTotalBoost));
            }

            // We want that terms per fragment (length) is included into the weight. Otherwise a one-word-query
            // would cause an equal weight for all fragments regardless of how much words they contain.  
            // To avoid that fragments containing a high number of words possibly "outrank" more relevant fragments
            // we "bend" the length with a standard-normalization a little bit.
            float norm = length * (1 / (float)Math.Sqrt(length));

            float totalBoost = 0;
            foreach (SubInfo tempSubInfo in tempSubInfos)
            {
                float subInfoBoost = tempSubInfo.Boost * norm;
                realSubInfos.Add(new SubInfo(tempSubInfo.Text, tempSubInfo.TermsOffsets,
                  tempSubInfo.Seqnum, subInfoBoost));
                totalBoost += subInfoBoost;
            }

            FragInfos.Add(new WeightedFragInfo(startOffset, endOffset, realSubInfos, totalBoost));
        }
    }
}
