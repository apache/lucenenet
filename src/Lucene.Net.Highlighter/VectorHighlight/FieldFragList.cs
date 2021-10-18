using System;
using System.Collections.Generic;
using System.Text;
using Float = J2N.Numerics.Single;
using JCG = J2N.Collections.Generic;
using Toffs = Lucene.Net.Search.VectorHighlight.FieldPhraseList.WeightedPhraseInfo.Toffs;
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
    /// FieldFragList has a list of "frag info" that is used by <see cref="IFragmentsBuilder"/> class
    /// to create fragments (snippets).
    /// </summary>
    public abstract class FieldFragList
    {
        private readonly IList<WeightedFragInfo> fragInfos = new JCG.List<WeightedFragInfo>();

        /// <summary>
        /// a constructor.
        /// </summary>
        /// <param name="fragCharSize">the length (number of chars) of a fragment</param>
#pragma warning disable IDE0060 // Remove unused parameter
        protected FieldFragList(int fragCharSize) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
#pragma warning restore IDE0060 // Remove unused parameter
        {
        }

        /// <summary>
        /// convert the list of <see cref="WeightedPhraseInfo"/> to <see cref="WeightedFragInfo"/>, then add it to the fragInfos
        /// </summary>
        /// <param name="startOffset">start offset of the fragment</param>
        /// <param name="endOffset">end offset of the fragment</param>
        /// <param name="phraseInfoList">list of <see cref="WeightedPhraseInfo"/> objects</param>
        public abstract void Add(int startOffset, int endOffset, IList<WeightedPhraseInfo> phraseInfoList);

        /// <summary>
        /// return the list of <see cref="WeightedFragInfo"/>s.
        /// </summary>
        public virtual IList<WeightedFragInfo> FragInfos => fragInfos;

        /// <summary>
        /// List of term offsets + weight for a frag info
        /// </summary>
        public class WeightedFragInfo : IFormattable // LUCENENET specific - implemented IFormattable for floating point representations
        {
            private readonly IList<SubInfo> subInfos; // LUCENENET: marked readonly
            private readonly float totalBoost; // LUCENENET: marked readonly
            private readonly int startOffset; // LUCENENET: marked readonly
            private readonly int endOffset; // LUCENENET: marked readonly

            public WeightedFragInfo(int startOffset, int endOffset, IList<SubInfo> subInfos, float totalBoost)
            {
                this.startOffset = startOffset;
                this.endOffset = endOffset;
                this.totalBoost = totalBoost;
                this.subInfos = subInfos;
            }

            public IList<SubInfo> SubInfos => subInfos;

            public float TotalBoost => totalBoost;

            public int StartOffset => startOffset;

            public int EndOffset => endOffset;

            public override string ToString()
            {
                return ToString(null);
            }

            // LUCENENET specific: allow formatting the boost in the current culture.

            public virtual string ToString(IFormatProvider provider)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("subInfos=(");
                foreach (SubInfo si in subInfos)
                    sb.Append(si.ToString());
                // LUCENENET: allow formatting in the current culture
                sb.Append(")/").Append(Float.ToString(totalBoost, provider)).Append('(').Append(startOffset).Append(',').Append(endOffset).Append(')');
                return sb.ToString();
            }

            string IFormattable.ToString(string format, IFormatProvider provider) => ToString(provider);

            /// <summary>
            /// Represents the list of term offsets for some text
            /// </summary>
            public class SubInfo
            {
                private readonly string text;  // unnecessary member, just exists for debugging purpose
                private readonly IList<Toffs> termsOffsets;   // usually termsOffsets.size() == 1,
                                                             // but if position-gap > 1 and slop > 0 then size() could be greater than 1
                private readonly int seqnum;
                private readonly float boost; // used for scoring split WeightedPhraseInfos.

                public SubInfo(string text, IList<Toffs> termsOffsets, int seqnum, float boost)
                {
                    this.text = text;
                    this.termsOffsets = termsOffsets;
                    this.seqnum = seqnum;
                    this.boost = boost;
                }

                public virtual IList<Toffs> TermsOffsets => termsOffsets;

                public virtual int Seqnum => seqnum;

                public virtual string Text => text;

                public virtual float Boost => boost;

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
