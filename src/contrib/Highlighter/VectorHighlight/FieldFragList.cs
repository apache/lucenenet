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

using System;
using System.Collections.Generic;
using System.Text;

using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Index;

using Toffs = Lucene.Net.Search.VectorHighlight.FieldPhraseList.WeightedPhraseInfo.Toffs;
using WeightedPhraseInfo = Lucene.Net.Search.VectorHighlight.FieldPhraseList.WeightedPhraseInfo;


namespace Lucene.Net.Search.VectorHighlight
{
    ///<summary>
    /// FieldFragList has a list of "frag info" that is used by FragmentsBuilder class
    /// to create fragments (snippets).
    ///</summary>
    public abstract class FieldFragList
    {
        private IList<WeightedFragInfo> fragInfos = new List<WeightedFragInfo>();
                
        /// <summary>
        /// a constructor.
        /// </summary>
        /// <param name="fragCharSize">the length (number of chars) of a fragment</param>
        public FieldFragList(int fragCharSize)
        {
        }
                
        /// <summary>
        /// convert the list of WeightedPhraseInfo to WeightedFragInfo, then add it to the fragInfos 
        /// </summary>
        /// <param name="startOffset">start offset of the fragment</param>
        /// <param name="endOffset">end offset of the fragment</param>
        /// <param name="phraseInfoList">list of WeightedPhraseInfo objects</param>
        public abstract void Add(int startOffset, int endOffset, IList<WeightedPhraseInfo> phraseInfoList);

        public IList<WeightedFragInfo> FragInfos
        {
            get { return fragInfos; }
        }

        public class WeightedFragInfo
        {
            private IList<SubInfo> subInfos;
            private float totalBoost;
            private int startOffset;
            private int endOffset;

            public WeightedFragInfo(int startOffset, int endOffset, IList<SubInfo> subInfos, float totalBoost)
            {
                this.startOffset = startOffset;
                this.endOffset = endOffset;
                this.totalBoost = totalBoost;
                this.subInfos = subInfos;
            }

            public IList<SubInfo> SubInfos
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
                sb.Append(")/").Append(totalBoost.ToString(".0").Replace(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator,".")).Append('(').Append(startOffset).Append(',').Append(endOffset).Append(')');
                return sb.ToString();
            }

            public class SubInfo
            {
                private readonly String text;  // unnecessary member, just exists for debugging purpose
                private readonly IList<Toffs> termsOffsets;   // usually termsOffsets.size() == 1,
                // but if position-gap > 1 and slop > 0 then size() could be greater than 1
                private int seqnum;

                public SubInfo(String text, IList<Toffs> termsOffsets, int seqnum)
                {
                    this.text = text;
                    this.termsOffsets = termsOffsets;
                    this.seqnum = seqnum;
                }

                public IList<Toffs> TermsOffsets
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

                public override string ToString()
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(text).Append('(');
                    foreach (Toffs to in termsOffsets)
                        sb.Append(to.ToString());
                    sb.Append(')');
                    return sb.ToString();
                }
            }
        }
    }
}
