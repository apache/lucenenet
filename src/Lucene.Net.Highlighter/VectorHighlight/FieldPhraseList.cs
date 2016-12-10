using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        /**
   * List of non-overlapping WeightedPhraseInfo objects.
   */
        List<WeightedPhraseInfo> phraseList = new List<WeightedPhraseInfo>();

        /**
         * create a FieldPhraseList that has no limit on the number of phrases to analyze
         * 
         * @param fieldTermStack FieldTermStack object
         * @param fieldQuery FieldQuery object
         */
        public FieldPhraseList(FieldTermStack fieldTermStack, FieldQuery fieldQuery)
            : this(fieldTermStack, fieldQuery, int.MaxValue)
        {
        }

        /**
         * return the list of WeightedPhraseInfo.
         * 
         * @return phraseList.
         */
        public IList<WeightedPhraseInfo> PhraseList
        {
            get { return phraseList; }
        }

        /**
         * a constructor.
         * 
         * @param fieldTermStack FieldTermStack object
         * @param fieldQuery FieldQuery object
         * @param phraseLimit maximum size of phraseList
         */
        public FieldPhraseList(FieldTermStack fieldTermStack, FieldQuery fieldQuery, int phraseLimit)
        {
            string field = fieldTermStack.FieldName;

            //LinkedList<TermInfo> phraseCandidate = new LinkedList<TermInfo>();
            List<TermInfo> phraseCandidate = new List<TermInfo>();
            QueryPhraseMap currMap = null;
            QueryPhraseMap nextMap = null;
            while (!fieldTermStack.IsEmpty && (phraseList.Count < phraseLimit))
            {
                phraseCandidate.Clear();

                TermInfo ti = null;
                TermInfo first = null;

                first = ti = fieldTermStack.Pop();
                currMap = fieldQuery.GetFieldTermMap(field, ti.Text);
                while (currMap == null && ti.Next != first)
                {
                    ti = ti.Next;
                    currMap = fieldQuery.GetFieldTermMap(field, ti.Text);
                }

                // if not found, discard top TermInfo from stack, then try next element
                if (currMap == null) continue;

                // if found, search the longest phrase
                phraseCandidate.Add(ti);
                while (true)
                {
                    first = ti = fieldTermStack.Pop();
                    nextMap = null;
                    if (ti != null)
                    {
                        nextMap = currMap.GetTermMap(ti.Text);
                        while (nextMap == null && ti.Next != first)
                        {
                            ti = ti.Next;
                            nextMap = currMap.GetTermMap(ti.Text);
                        }
                    }
                    if (ti == null || nextMap == null)
                    {
                        if (ti != null)
                            fieldTermStack.Push(ti);
                        if (currMap.IsValidTermOrPhrase(phraseCandidate))
                        {
                            AddIfNoOverlap(new WeightedPhraseInfo(phraseCandidate, currMap.Boost, currMap.TermOrPhraseNumber));
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
                                    AddIfNoOverlap(new WeightedPhraseInfo(phraseCandidate, currMap.Boost, currMap.TermOrPhraseNumber));
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

        /**
         * Merging constructor.
         *
         * @param toMerge FieldPhraseLists to merge to build this one
         */
        public FieldPhraseList(FieldPhraseList[] toMerge)
        {
            // Merge all overlapping WeightedPhraseInfos
            // Step 1.  Sort by startOffset, endOffset, and boost, in that order.

            IEnumerator<WeightedPhraseInfo>[] allInfos = new IEnumerator<WeightedPhraseInfo>[toMerge.Length];
            int index = 0;
            foreach (FieldPhraseList fplToMerge in toMerge)
            {
                allInfos[index++] = fplToMerge.phraseList.GetEnumerator();
            }
            MergedIterator<WeightedPhraseInfo> itr = new MergedIterator<WeightedPhraseInfo>(false, allInfos);
            // Step 2.  Walk the sorted list merging infos that overlap
            phraseList = new List<WeightedPhraseInfo>();
            if (!itr.MoveNext())
            {
                return;
            }
            List<WeightedPhraseInfo> work = new List<WeightedPhraseInfo>();
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

        public void AddIfNoOverlap(WeightedPhraseInfo wpi)
        {
            foreach (WeightedPhraseInfo existWpi in PhraseList)
            {
                if (existWpi.IsOffsetOverlap(wpi))
                {
                    // WeightedPhraseInfo.addIfNoOverlap() dumps the second part of, for example, hyphenated words (social-economics). 
                    // The result is that all informations in TermInfo are lost and not available for further operations. 
                    existWpi.TermsInfos.AddAll(wpi.TermsInfos);
                    return;
                }
            }
            PhraseList.Add(wpi);
        }

        /**
         * Represents the list of term offsets and boost for some text
         */
        public class WeightedPhraseInfo : IComparable<WeightedPhraseInfo>
        {
            private List<Toffs> termsOffsets;   // usually termsOffsets.size() == 1,
                                                // but if position-gap > 1 and slop > 0 then size() could be greater than 1
            private float boost;  // query boost
            private int seqnum;

            private List<TermInfo> termsInfos;

            /**
             * Text of the match, calculated on the fly.  Use for debugging only.
             * @return the text
             */
            public string GetText()
            {
                StringBuilder text = new StringBuilder();
                foreach (TermInfo ti in termsInfos)
                {
                    text.Append(ti.Text);
                }
                return text.ToString();
            }

            /**
             * @return the termsOffsets
             */
            public IList<Toffs> TermsOffsets
            {
                get { return termsOffsets; }
            }

            /**
             * @return the boost
             */
            public float Boost
            {
                get { return boost; }
            }

            /**
             * @return the termInfos 
             */
            public IList<TermInfo> TermsInfos
            {
                get { return termsInfos; }
            }

            public WeightedPhraseInfo(IList<TermInfo> terms, float boost)
                    : this(terms, boost, 0)
            {

            }

            public WeightedPhraseInfo(IList<TermInfo> terms, float boost, int seqnum)
            {
                this.boost = boost;
                this.seqnum = seqnum;

                // We keep TermInfos for further operations
                termsInfos = new List<TermInfo>(terms);

                termsOffsets = new List<Toffs>(terms.Count);
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

            /**
             * Merging constructor.  Note that this just grabs seqnum from the first info.
             */
            public WeightedPhraseInfo(ICollection<WeightedPhraseInfo> toMerge)
            {
                // Pretty much the same idea as merging FieldPhraseLists:
                // Step 1.  Sort by startOffset, endOffset
                //          While we are here merge the boosts and termInfos
                IEnumerator<WeightedPhraseInfo> toMergeItr = toMerge.GetEnumerator();
                if (!toMergeItr.MoveNext())
                {
                    throw new ArgumentException("toMerge must contain at least one WeightedPhraseInfo.");
                }
                WeightedPhraseInfo first = toMergeItr.Current;
                IEnumerator<Toffs>[] allToffs = new IEnumerator<Toffs>[toMerge.Count];
                termsInfos = new List<TermInfo>();
                seqnum = first.seqnum;
                boost = first.boost;
                allToffs[0] = first.termsOffsets.GetEnumerator();
                int index = 1;
                while (toMergeItr.MoveNext())
                {
                    WeightedPhraseInfo info = toMergeItr.Current;
                    boost += info.boost;
                    termsInfos.AddAll(info.termsInfos);
                    allToffs[index++] = info.termsOffsets.GetEnumerator();
                }
                // Step 2.  Walk the sorted list merging overlaps
                MergedIterator<Toffs> itr = new MergedIterator<Toffs>(false, allToffs);
                termsOffsets = new List<Toffs>();
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

            public int StartOffset
            {
                get { return termsOffsets[0].StartOffset; }
            }

            public int EndOffset
            {
                get { return termsOffsets[termsOffsets.Count - 1].EndOffset; }
            }

            public bool IsOffsetOverlap(WeightedPhraseInfo other)
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
                StringBuilder sb = new StringBuilder();
                sb.Append(GetText()).Append('(').Append(Number.ToString(boost)).Append(")(");
                foreach (Toffs to in termsOffsets)
                {
                    sb.Append(to);
                }
                sb.Append(')');
                return sb.ToString();
            }

            /**
             * @return the seqnum
             */
            public int Seqnum
            {
                get { return seqnum; }
            }

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
                long b = Number.DoubleToLongBits(Boost);
                result = prime * result + (int)(b ^ TripleShift(b, 32));
                return result;
            }

            // LUCENENET NOTE: For some reason the standard way of correcting the >>>
            // operator (uint)b >> 32) didn't work here. Got this solution from http://stackoverflow.com/a/6625912
            // and it works just like in Java.
            private static long TripleShift(long n, int s)
            {
                if (n >= 0)
                    return n >> s;
                return (n >> s) + (2 << ~s);
            }

            public override bool Equals(object obj)
            {
                if (this == obj)
                {
                    return true;
                }
                if (obj == null)
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

            /**
             * Term offsets (start + end)
             */
            public class Toffs : IComparable<Toffs>
            {
                private int startOffset;
                private int endOffset;
                public Toffs(int startOffset, int endOffset)
                {
                    this.startOffset = startOffset;
                    this.endOffset = endOffset;
                }

                public virtual int StartOffset
                {
                    get { return startOffset; }
                }
                public virtual int EndOffset
                {
                    get { return endOffset; }
                    set { endOffset = value; }
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
                    if (obj == null)
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
