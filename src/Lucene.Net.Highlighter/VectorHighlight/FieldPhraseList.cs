using J2N.Numerics;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Text;
using Float = J2N.Numerics.Single;
using JCG = J2N.Collections.Generic;
using QueryPhraseMap = Lucene.Net.Search.VectorHighlight.FieldQuery.QueryPhraseMap;
using TermInfo = Lucene.Net.Search.VectorHighlight.FieldTermStack.TermInfo;

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
    /// FieldPhraseList has a list of WeightedPhraseInfo that is used by FragListBuilder
    /// to create a FieldFragList object.
    /// </summary>
    public class FieldPhraseList
    {
        /// <summary>
        /// List of non-overlapping <see cref="WeightedPhraseInfo"/> objects.
        /// </summary>
        internal IList<WeightedPhraseInfo> phraseList = new JCG.List<WeightedPhraseInfo>();

        /// <summary>
        /// create a <see cref="FieldPhraseList"/> that has no limit on the number of phrases to analyze
        /// </summary>
        /// <param name="fieldTermStack"><see cref="FieldTermStack"/> object</param>
        /// <param name="fieldQuery"><see cref="FieldQuery"/> object</param>
        public FieldPhraseList(FieldTermStack fieldTermStack, FieldQuery fieldQuery)
            : this(fieldTermStack, fieldQuery, int.MaxValue)
        {
        }

        /// <summary>
        /// return the list of <see cref="WeightedPhraseInfo"/>.
        /// </summary>
        public virtual IList<WeightedPhraseInfo> PhraseList => phraseList;

        /// <summary>
        /// a constructor.
        /// </summary>
        /// <param name="fieldTermStack"><see cref="FieldTermStack"/> object</param>
        /// <param name="fieldQuery"><see cref="FieldQuery"/> object</param>
        /// <param name="phraseLimit">maximum size of phraseList</param>
        public FieldPhraseList(FieldTermStack fieldTermStack, FieldQuery fieldQuery, int phraseLimit)
        {
            string field = fieldTermStack.FieldName;

            IList<TermInfo> phraseCandidate = new JCG.List<TermInfo>();
            QueryPhraseMap currMap; // LUCENENET: IDE0059: Remove unnecessary value assignment
            QueryPhraseMap nextMap; // LUCENENET: IDE0059: Remove unnecessary value assignment
            while (!fieldTermStack.IsEmpty && (phraseList.Count < phraseLimit))
            {
                phraseCandidate.Clear();

                TermInfo ti; // LUCENENET: IDE0059: Remove unnecessary value assignment
                TermInfo first; // LUCENENET: IDE0059: Remove unnecessary value assignment

                first = ti = fieldTermStack.Pop();
                currMap = fieldQuery.GetFieldTermMap(field, ti.Text);
                while (currMap is null && ti.Next != first)
                {
                    ti = ti.Next;
                    currMap = fieldQuery.GetFieldTermMap(field, ti.Text);
                }

                // if not found, discard top TermInfo from stack, then try next element
                if (currMap is null) continue;

                // if found, search the longest phrase
                phraseCandidate.Add(ti);
                while (true)
                {
                    first = ti = fieldTermStack.Pop();
                    nextMap = null;
                    if (ti != null)
                    {
                        nextMap = currMap.GetTermMap(ti.Text);
                        while (nextMap is null && ti.Next != first)
                        {
                            ti = ti.Next;
                            nextMap = currMap.GetTermMap(ti.Text);
                        }
                    }
                    if (ti is null || nextMap is null)
                    {
                        if (ti != null)
                            fieldTermStack.Push(ti);
                        if (currMap.IsValidTermOrPhrase(phraseCandidate))
                        {
                            AddIfNoOverlapInternal(new WeightedPhraseInfo(phraseCandidate, currMap.Boost, currMap.TermOrPhraseNumber));
                        }
                        else
                        {
                            while (phraseCandidate.Count > 1)
                            {

                                //fieldTermStack.Push(phraseCandidate.Last.Value);
                                //phraseCandidate.RemoveLast();

                                TermInfo last = phraseCandidate[phraseCandidate.Count - 1];
                                phraseCandidate.Remove(last);
                                fieldTermStack.Push(last);

                                currMap = fieldQuery.SearchPhrase(field, phraseCandidate);
                                if (currMap != null)
                                {
                                    AddIfNoOverlapInternal(new WeightedPhraseInfo(phraseCandidate, currMap.Boost, currMap.TermOrPhraseNumber));
                                    break;
                                }
                            }
                        }
                        break;
                    }
                    else
                    {
                        phraseCandidate.Add(ti);
                        currMap = nextMap;
                    }
                }
            }
        }

        /// <summary>
        /// Merging constructor.
        /// </summary>
        /// <param name="toMerge"><see cref="FieldPhraseList"/>s to merge to build this one</param>
        public FieldPhraseList(FieldPhraseList[] toMerge)
        {
            // Merge all overlapping WeightedPhraseInfos
            // Step 1.  Sort by startOffset, endOffset, and boost, in that order.

            IEnumerator<WeightedPhraseInfo>[] allInfos = new IEnumerator<WeightedPhraseInfo>[toMerge.Length];
            try
            {
                int index = 0;
                foreach (FieldPhraseList fplToMerge in toMerge)
                {
                    allInfos[index++] = fplToMerge.phraseList.GetEnumerator();
                }
                using MergedEnumerator<WeightedPhraseInfo> itr = new MergedEnumerator<WeightedPhraseInfo>(false, allInfos);
                // Step 2.  Walk the sorted list merging infos that overlap
                phraseList = new JCG.List<WeightedPhraseInfo>();
                if (!itr.MoveNext())
                {
                    return;
                }
                IList<WeightedPhraseInfo> work = new JCG.List<WeightedPhraseInfo>();
                WeightedPhraseInfo first = itr.Current;
                work.Add(first);
                int workEndOffset = first.EndOffset;
                while (itr.MoveNext())
                {
                    WeightedPhraseInfo current = itr.Current;
                    if (current.StartOffset <= workEndOffset)
                    {
                        workEndOffset = Math.Max(workEndOffset, current.EndOffset);
                        work.Add(current);
                    }
                    else
                    {
                        if (work.Count == 1)
                        {
                            phraseList.Add(work[0]);
                            work[0] = current;
                        }
                        else
                        {
                            phraseList.Add(new WeightedPhraseInfo(work));
                            work.Clear();
                            work.Add(current);
                        }
                        workEndOffset = current.EndOffset;
                    }
                }
                if (work.Count == 1)
                {
                    phraseList.Add(work[0]);
                }
                else
                {
                    phraseList.Add(new WeightedPhraseInfo(work));
                    work.Clear();
                }
            }
            finally
            {
                IOUtils.Dispose(allInfos);
            }
        }

        /// <summary>
        ///
        /// NOTE: When overriding this method, be aware that the constructor of this class calls 
        /// a private method and not this virtual method. So if you need to override
        /// the behavior during the initialization, call your own private method from the constructor
        /// with whatever custom behavior you need.
        /// </summary>
        public virtual void AddIfNoOverlap(WeightedPhraseInfo wpi) =>
            AddIfNoOverlapInternal(wpi);

        // LUCENENET specific - created a private method that can be called from AddIfNoOverlap
        // in order to avoid calling virtual methods from the constructor.
        private void AddIfNoOverlapInternal(WeightedPhraseInfo wpi)
        {
            foreach (WeightedPhraseInfo existWpi in PhraseList)
            {
                if (existWpi.IsOffsetOverlap(wpi))
                {
                    // WeightedPhraseInfo.addIfNoOverlap() dumps the second part of, for example, hyphenated words (social-economics). 
                    // The result is that all informations in TermInfo are lost and not available for further operations. 
                    existWpi.TermsInfos.AddRange(wpi.TermsInfos);
                    return;
                }
            }
            PhraseList.Add(wpi);
        }

        /// <summary>
        /// Represents the list of term offsets and boost for some text
        /// </summary>
        public class WeightedPhraseInfo : IComparable<WeightedPhraseInfo>, IFormattable // LUCENENET specific - implemented IFormattable for floating point representations
        {
            private readonly IList<Toffs> termsOffsets;   // usually termsOffsets.size() == 1, // LUCENENET: marked readonly
                                                         // but if position-gap > 1 and slop > 0 then size() could be greater than 1
            private readonly float boost;  // query boost // LUCENENET: marked readonly
            private readonly int seqnum; // LUCENENET: marked readonly

            private readonly JCG.List<TermInfo> termsInfos; // LUCENENET: marked readonly

            /// <summary>
            /// Text of the match, calculated on the fly.  Use for debugging only.
            /// </summary>
            /// <returns>the text</returns>
            public virtual string GetText()
            {
                StringBuilder text = new StringBuilder();
                foreach (TermInfo ti in termsInfos)
                {
                    text.Append(ti.Text);
                }
                return text.ToString();
            }

            /// <summary>
            /// the termsOffsets
            /// </summary>
            public virtual IList<Toffs> TermsOffsets => termsOffsets;

            /// <summary>
            /// the boost
            /// </summary>
            public virtual float Boost => boost;

            /// <summary>
            /// the termInfos 
            /// </summary>
            public virtual IList<TermInfo> TermsInfos => termsInfos;

            public WeightedPhraseInfo(IList<TermInfo> terms, float boost)
                    : this(terms, boost, 0)
            {
            }

            public WeightedPhraseInfo(IList<TermInfo> terms, float boost, int seqnum)
            {
                this.boost = boost;
                this.seqnum = seqnum;

                // We keep TermInfos for further operations
                termsInfos = new JCG.List<TermInfo>(terms);

                termsOffsets = new JCG.List<Toffs>(terms.Count);
                TermInfo ti = terms[0];
                termsOffsets.Add(new Toffs(ti.StartOffset, ti.EndOffset));
                if (terms.Count == 1)
                {
                    return;
                }
                int pos = ti.Position;
                for (int i = 1; i < terms.Count; i++)
                {
                    ti = terms[i];
                    if (ti.Position - pos == 1)
                    {
                        Toffs to = termsOffsets[termsOffsets.Count - 1];
                        to.EndOffset = ti.EndOffset;
                    }
                    else
                    {
                        termsOffsets.Add(new Toffs(ti.StartOffset, ti.EndOffset));
                    }
                    pos = ti.Position;
                }
            }

            /// <summary>
            /// Merging constructor.  Note that this just grabs seqnum from the first info.
            /// </summary>
            public WeightedPhraseInfo(ICollection<WeightedPhraseInfo> toMerge)
            {
                IEnumerator<Toffs>[] allToffs = new IEnumerator<Toffs>[toMerge.Count];
                try
                {

                    // Pretty much the same idea as merging FieldPhraseLists:
                    // Step 1.  Sort by startOffset, endOffset
                    //          While we are here merge the boosts and termInfos
                    using IEnumerator<WeightedPhraseInfo> toMergeItr = toMerge.GetEnumerator();
                    if (!toMergeItr.MoveNext())
                    {
                        throw new ArgumentException("toMerge must contain at least one WeightedPhraseInfo.");
                    }
                    WeightedPhraseInfo first = toMergeItr.Current;

                    termsInfos = new JCG.List<TermInfo>();
                    seqnum = first.seqnum;
                    boost = first.boost;
                    allToffs[0] = first.termsOffsets.GetEnumerator();
                    int index = 1;
                    while (toMergeItr.MoveNext())
                    {
                        WeightedPhraseInfo info = toMergeItr.Current;
                        boost += info.boost;
                        termsInfos.AddRange(info.termsInfos);
                        allToffs[index++] = info.termsOffsets.GetEnumerator();
                    }

                    // Step 2.  Walk the sorted list merging overlaps
                    using MergedEnumerator<Toffs> itr = new MergedEnumerator<Toffs>(false, allToffs);
                    termsOffsets = new JCG.List<Toffs>();
                    if (!itr.MoveNext())
                    {
                        return;
                    }
                    Toffs work = itr.Current;
                    while (itr.MoveNext())
                    {
                        Toffs current = itr.Current;
                        if (current.StartOffset <= work.EndOffset)
                        {
                            work.EndOffset = Math.Max(work.EndOffset, current.EndOffset);
                        }
                        else
                        {
                            termsOffsets.Add(work);
                            work = current;
                        }
                    }
                    termsOffsets.Add(work);
                }
                finally
                {
                    IOUtils.Dispose(allToffs);
                }
            }

            public virtual int StartOffset => termsOffsets[0].StartOffset;

            public virtual int EndOffset => termsOffsets[termsOffsets.Count - 1].EndOffset;

            public virtual bool IsOffsetOverlap(WeightedPhraseInfo other)
            {
                int so = StartOffset;
                int eo = EndOffset;
                int oso = other.StartOffset;
                int oeo = other.EndOffset;
                if (so <= oso && oso < eo) return true;
                if (so < oeo && oeo <= eo) return true;
                if (oso <= so && so < oeo) return true;
                if (oso < eo && eo <= oeo) return true;
                return false;
            }

            public override string ToString()
            {
                return ToString(null);
            }

            public virtual string ToString(IFormatProvider provider)
            {
                StringBuilder sb = new StringBuilder();
                // LUCENENET: allow passing culture
                sb.Append(GetText()).Append('(').Append(Float.ToString(boost, provider)).Append(")(");
                foreach (Toffs to in termsOffsets)
                {
                    sb.Append(to);
                }
                sb.Append(')');
                return sb.ToString();
            }

            string IFormattable.ToString(string format, IFormatProvider provider) => ToString(provider);

            /// <summary>
            /// the seqnum
            /// </summary>
            public virtual int Seqnum => seqnum;

            public virtual int CompareTo(WeightedPhraseInfo other)
            {
                int diff = StartOffset - other.StartOffset;
                if (diff != 0)
                {
                    return diff;
                }
                diff = EndOffset - other.EndOffset;
                if (diff != 0)
                {
                    return diff;
                }
                return (int)Math.Sign(Boost - other.Boost);
            }


            public override int GetHashCode()
            {
                int prime = 31;
                int result = 1;
                result = prime * result + StartOffset;
                result = prime * result + EndOffset;
                long b = J2N.BitConversion.DoubleToInt64Bits(Boost);
                result = prime * result + (int)(b ^ b.TripleShift(32));
                return result;
            }

            public override bool Equals(object obj)
            {
                if (this == obj)
                {
                    return true;
                }
                if (obj is null)
                {
                    return false;
                }
                if (GetType() != obj.GetType())
                {
                    return false;
                }
                WeightedPhraseInfo other = (WeightedPhraseInfo)obj;
                if (StartOffset != other.StartOffset)
                {
                    return false;
                }
                if (EndOffset != other.EndOffset)
                {
                    return false;
                }
                if (Boost != other.Boost)
                {
                    return false;
                }
                return true;
            }

            /// <summary>
            /// Term offsets (start + end)
            /// </summary>
            public class Toffs : IComparable<Toffs>
            {
                private readonly int startOffset; // LUCENENET: marked readonly
                private int endOffset;
                public Toffs(int startOffset, int endOffset)
                {
                    this.startOffset = startOffset;
                    this.endOffset = endOffset;
                }

                public virtual int StartOffset => startOffset;

                public virtual int EndOffset
                {
                    get => endOffset;
                    set => endOffset = value;
                }

                public virtual int CompareTo(Toffs other)
                {
                    int diff = StartOffset - other.StartOffset;
                    if (diff != 0)
                    {
                        return diff;
                    }
                    return EndOffset - other.EndOffset;
                }

                public override int GetHashCode()
                {
                    int prime = 31;
                    int result = 1;
                    result = prime * result + StartOffset;
                    result = prime * result + EndOffset;
                    return result;
                }

                public override bool Equals(object obj)
                {
                    if (this == obj)
                    {
                        return true;
                    }
                    if (obj is null)
                    {
                        return false;
                    }
                    if (GetType() != obj.GetType())
                    {
                        return false;
                    }
                    Toffs other = (Toffs)obj;
                    if (StartOffset != other.StartOffset)
                    {
                        return false;
                    }
                    if (EndOffset != other.EndOffset)
                    {
                        return false;
                    }
                    return true;
                }
                public override string ToString()
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append('(').Append(startOffset).Append(',').Append(endOffset).Append(')');
                    return sb.ToString();
                }
            }
        }
    }
}
