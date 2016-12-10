using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    /// FieldFragList has a list of "frag info" that is used by FragmentsBuilder class
    /// to create fragments (snippets).
    /// </summary>
    public abstract class FieldFragList
    {
        private List<WeightedFragInfo> fragInfos = new List<WeightedFragInfo>();

        /**
         * a constructor.
         * 
         * @param fragCharSize the length (number of chars) of a fragment
         */
        public FieldFragList(int fragCharSize)
        {
        }

        /**
         * convert the list of WeightedPhraseInfo to WeightedFragInfo, then add it to the fragInfos
         * 
         * @param startOffset start offset of the fragment
         * @param endOffset end offset of the fragment
         * @param phraseInfoList list of WeightedPhraseInfo objects
         */
        public abstract void Add(int startOffset, int endOffset, IList<FieldPhraseList.WeightedPhraseInfo> phraseInfoList);

        /**
         * return the list of WeightedFragInfos.
         * 
         * @return fragInfos.
         */
        public IList<WeightedFragInfo> FragInfos
        {
            get { return fragInfos; }
        }

        /**
         * List of term offsets + weight for a frag info
         */
        public class WeightedFragInfo
        {

            private List<SubInfo> subInfos;
            private float totalBoost;
            private int startOffset;
            private int endOffset;

            public WeightedFragInfo(int startOffset, int endOffset, List<SubInfo> subInfos, float totalBoost)
            {
                this.startOffset = startOffset;
                this.endOffset = endOffset;
                this.totalBoost = totalBoost;
                this.subInfos = subInfos;
            }

            public List<SubInfo> SubInfos
            {
                get { return subInfos; }
            }

            public float TotalBoost
            {
                get { return totalBoost; }
            }

            public int StartOffset
            {
                get { return startOffset; }
            }

            public int EndOffset
            {
                get { return endOffset; }
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("subInfos=(");
                foreach (SubInfo si in subInfos)
                    sb.Append(si.ToString());
                sb.Append(")/").Append(Number.ToString(totalBoost)).Append('(').Append(startOffset).Append(',').Append(endOffset).Append(')');
                return sb.ToString();
            }

            /**
             * Represents the list of term offsets for some text
             */
            public class SubInfo
            {
                private readonly string text;  // unnecessary member, just exists for debugging purpose
                private readonly IList<FieldPhraseList.WeightedPhraseInfo.Toffs> termsOffsets;   // usually termsOffsets.size() == 1,
                                                             // but if position-gap > 1 and slop > 0 then size() could be greater than 1
                private readonly int seqnum;
                private readonly float boost; // used for scoring split WeightedPhraseInfos.

                public SubInfo(string text, IList<FieldPhraseList.WeightedPhraseInfo.Toffs> termsOffsets, int seqnum, float boost)
                {
                    this.text = text;
                    this.termsOffsets = termsOffsets;
                    this.seqnum = seqnum;
                    this.boost = boost;
                }

                public IList<FieldPhraseList.WeightedPhraseInfo.Toffs> TermsOffsets
                {
                    get { return termsOffsets; }
                }

                public int Seqnum
                {
                    get { return seqnum; }
                }

                public string Text
                {
                    get { return text; }
                }

                public float Boost
                {
                    get { return boost; }
                }


                public override string ToString()
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(text).Append('(');
                    foreach (FieldPhraseList.WeightedPhraseInfo.Toffs to in termsOffsets)
                        sb.Append(to.ToString());
                    sb.Append(')');
                    return sb.ToString();
                }
            }
        }
    }
}
