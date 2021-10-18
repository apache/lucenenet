using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
using SubInfo = Lucene.Net.Search.VectorHighlight.FieldFragList.WeightedFragInfo.SubInfo;
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
    /// A simple implementation of <see cref="FieldFragList"/>.
    /// </summary>
    public class SimpleFieldFragList : FieldFragList
    {
        /// <summary>
        /// a constructor.
        /// </summary>
        /// <param name="fragCharSize">the length (number of chars) of a fragment</param>
        public SimpleFieldFragList(int fragCharSize)
            : base(fragCharSize)
        {
        }

        /// <summary>
        /// <seealso cref="FieldFragList.Add(int, int, IList{WeightedPhraseInfo})"/>
        /// </summary>
        public override void Add(int startOffset, int endOffset, IList<WeightedPhraseInfo> phraseInfoList)
        {
            float totalBoost = 0;
            IList<SubInfo> subInfos = new JCG.List<SubInfo>();
            foreach (WeightedPhraseInfo phraseInfo in phraseInfoList)
            {
                subInfos.Add(new SubInfo(phraseInfo.GetText(), phraseInfo.TermsOffsets, phraseInfo.Seqnum, phraseInfo.Boost));
                totalBoost += phraseInfo.Boost;
            }
            FragInfos.Add(new WeightedFragInfo(startOffset, endOffset, subInfos, totalBoost));
        }
    }
}
