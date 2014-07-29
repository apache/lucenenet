using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Index
{
    using Lucene.Net.Support;
    using System;
    using Bits = Lucene.Net.Util.Bits;

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

    using BytesRef = Lucene.Net.Util.BytesRef;

    /// <summary>
    /// Exposes <seealso cref="TermsEnum"/> API, merged from <seealso cref="TermsEnum"/> API of sub-segments.
    /// this does a merge sort, by term text, of the sub-readers.
    ///
    /// @lucene.experimental
    /// </summary>
    public sealed class MultiTermsEnum : TermsEnum
    {
        private readonly TermMergeQueue Queue;
        private readonly TermsEnumWithSlice[] Subs; // all of our subs (one per sub-reader)
        private readonly TermsEnumWithSlice[] CurrentSubs; // current subs that have at least one term for this field
        private readonly TermsEnumWithSlice[] Top;
        private readonly MultiDocsEnum.EnumWithSlice[] SubDocs;
        private readonly MultiDocsAndPositionsEnum.EnumWithSlice[] SubDocsAndPositions;

        private BytesRef LastSeek;
        private bool LastSeekExact;
        private readonly BytesRef LastSeekScratch = new BytesRef();

        private int NumTop;
        private int NumSubs;
        private BytesRef Current;
        private IComparer<BytesRef> TermComp;

        public class TermsEnumIndex
        {
            public static readonly TermsEnumIndex[] EMPTY_ARRAY = new TermsEnumIndex[0];
            internal readonly int SubIndex;
            internal readonly TermsEnum TermsEnum;

            public TermsEnumIndex(TermsEnum termsEnum, int subIndex)
            {
                this.TermsEnum = termsEnum;
                this.SubIndex = subIndex;
            }
        }

        /// <summary>
        /// Returns how many sub-reader slices contain the current </summary>
        ///  term.  <seealso cref= #getMatchArray  </seealso>
        public int MatchCount
        {
            get
            {
                return NumTop;
            }
        }

        /// <summary>
        /// Returns sub-reader slices positioned to the current term. </summary>
        public TermsEnumWithSlice[] MatchArray
        {
            get
            {
                return Top;
            }
        }

        /// <summary>
        /// Sole constructor. </summary>
        ///  <param name="slices"> Which sub-reader slices we should
        ///  merge.  </param>
        public MultiTermsEnum(ReaderSlice[] slices)
        {
            Queue = new TermMergeQueue(slices.Length);
            Top = new TermsEnumWithSlice[slices.Length];
            Subs = new TermsEnumWithSlice[slices.Length];
            SubDocs = new MultiDocsEnum.EnumWithSlice[slices.Length];
            SubDocsAndPositions = new MultiDocsAndPositionsEnum.EnumWithSlice[slices.Length];
            for (int i = 0; i < slices.Length; i++)
            {
                Subs[i] = new TermsEnumWithSlice(i, slices[i]);
                SubDocs[i] = new MultiDocsEnum.EnumWithSlice();
                SubDocs[i].Slice = slices[i];
                SubDocsAndPositions[i] = new MultiDocsAndPositionsEnum.EnumWithSlice();
                SubDocsAndPositions[i].Slice = slices[i];
            }
            CurrentSubs = new TermsEnumWithSlice[slices.Length];
        }

        public override BytesRef Term()
        {
            return Current;
        }

        public override IComparer<BytesRef> Comparator
        {
            get
            {
                return TermComp;
            }
        }

        /// <summary>
        /// The terms array must be newly created TermsEnum, ie
        ///  <seealso cref="TermsEnum#next"/> has not yet been called.
        /// </summary>
        public TermsEnum Reset(TermsEnumIndex[] termsEnumsIndex)
        {
            Debug.Assert(termsEnumsIndex.Length <= Top.Length);
            NumSubs = 0;
            NumTop = 0;
            TermComp = null;
            Queue.Clear();
            for (int i = 0; i < termsEnumsIndex.Length; i++)
            {
                TermsEnumIndex termsEnumIndex = termsEnumsIndex[i];
                Debug.Assert(termsEnumIndex != null);

                // init our term comp
                if (TermComp == null)
                {
                    Queue.TermComp = TermComp = termsEnumIndex.TermsEnum.Comparator;
                }
                else
                {
                    // We cannot merge sub-readers that have
                    // different TermComps
                    IComparer<BytesRef> subTermComp = termsEnumIndex.TermsEnum.Comparator;
                    if (subTermComp != null && !subTermComp.Equals(TermComp))
                    {
                        throw new InvalidOperationException("sub-readers have different BytesRef.Comparators: " + subTermComp + " vs " + TermComp + "; cannot merge");
                    }
                }

                BytesRef term = termsEnumIndex.TermsEnum.Next();
                if (term != null)
                {
                    TermsEnumWithSlice entry = Subs[termsEnumIndex.SubIndex];
                    entry.Reset(termsEnumIndex.TermsEnum, term);
                    Queue.Add(entry);
                    CurrentSubs[NumSubs++] = entry;
                }
                else
                {
                    // field has no terms
                }
            }

            if (Queue.Size() == 0)
            {
                return TermsEnum.EMPTY;
            }
            else
            {
                return this;
            }
        }

        public override bool SeekExact(BytesRef term)
        {
            Queue.Clear();
            NumTop = 0;

            bool seekOpt = false;
            if (LastSeek != null && TermComp.Compare(LastSeek, term) <= 0)
            {
                seekOpt = true;
            }

            LastSeek = null;
            LastSeekExact = true;

            for (int i = 0; i < NumSubs; i++)
            {
                bool status;
                // LUCENE-2130: if we had just seek'd already, prior
                // to this seek, and the new seek term is after the
                // previous one, don't try to re-seek this sub if its
                // current term is already beyond this new seek term.
                // Doing so is a waste because this sub will simply
                // seek to the same spot.
                if (seekOpt)
                {
                    BytesRef curTerm = CurrentSubs[i].Current;
                    if (curTerm != null)
                    {
                        int cmp = TermComp.Compare(term, curTerm);
                        if (cmp == 0)
                        {
                            status = true;
                        }
                        else if (cmp < 0)
                        {
                            status = false;
                        }
                        else
                        {
                            status = CurrentSubs[i].Terms.SeekExact(term);
                        }
                    }
                    else
                    {
                        status = false;
                    }
                }
                else
                {
                    status = CurrentSubs[i].Terms.SeekExact(term);
                }

                if (status)
                {
                    Top[NumTop++] = CurrentSubs[i];
                    Current = CurrentSubs[i].Current = CurrentSubs[i].Terms.Term();
                    Debug.Assert(term.Equals(CurrentSubs[i].Current));
                }
            }

            // if at least one sub had exact match to the requested
            // term then we found match
            return NumTop > 0;
        }

        public override SeekStatus SeekCeil(BytesRef term)
        {
            Queue.Clear();
            NumTop = 0;
            LastSeekExact = false;

            bool seekOpt = false;
            if (LastSeek != null && TermComp.Compare(LastSeek, term) <= 0)
            {
                seekOpt = true;
            }

            LastSeekScratch.CopyBytes(term);
            LastSeek = LastSeekScratch;

            for (int i = 0; i < NumSubs; i++)
            {
                SeekStatus status;
                // LUCENE-2130: if we had just seek'd already, prior
                // to this seek, and the new seek term is after the
                // previous one, don't try to re-seek this sub if its
                // current term is already beyond this new seek term.
                // Doing so is a waste because this sub will simply
                // seek to the same spot.
                if (seekOpt)
                {
                    BytesRef curTerm = CurrentSubs[i].Current;
                    if (curTerm != null)
                    {
                        int cmp = TermComp.Compare(term, curTerm);
                        if (cmp == 0)
                        {
                            status = SeekStatus.FOUND;
                        }
                        else if (cmp < 0)
                        {
                            status = SeekStatus.NOT_FOUND;
                        }
                        else
                        {
                            status = CurrentSubs[i].Terms.SeekCeil(term);
                        }
                    }
                    else
                    {
                        status = SeekStatus.END;
                    }
                }
                else
                {
                    status = CurrentSubs[i].Terms.SeekCeil(term);
                }

                if (status == SeekStatus.FOUND)
                {
                    Top[NumTop++] = CurrentSubs[i];
                    Current = CurrentSubs[i].Current = CurrentSubs[i].Terms.Term();
                }
                else
                {
                    if (status == SeekStatus.NOT_FOUND)
                    {
                        CurrentSubs[i].Current = CurrentSubs[i].Terms.Term();
                        Debug.Assert(CurrentSubs[i].Current != null);
                        Queue.Add(CurrentSubs[i]);
                    }
                    else
                    {
                        // enum exhausted
                        CurrentSubs[i].Current = null;
                    }
                }
            }

            if (NumTop > 0)
            {
                // at least one sub had exact match to the requested term
                return SeekStatus.FOUND;
            }
            else if (Queue.Size() > 0)
            {
                // no sub had exact match, but at least one sub found
                // a term after the requested term -- advance to that
                // next term:
                PullTop();
                return SeekStatus.NOT_FOUND;
            }
            else
            {
                return SeekStatus.END;
            }
        }

        public override void SeekExact(long ord)
        {
            throw new System.NotSupportedException();
        }

        public override long Ord()
        {
            throw new System.NotSupportedException();
        }

        private void PullTop()
        {
            // extract all subs from the queue that have the same
            // top term
            Debug.Assert(NumTop == 0);
            while (true)
            {
                Top[NumTop++] = Queue.Pop();
                if (Queue.Size() == 0 || !(Queue.Top()).Current.BytesEquals(Top[0].Current))
                {
                    break;
                }
            }
            Current = Top[0].Current;
        }

        private void PushTop()
        {
            // call next() on each top, and put back into queue
            for (int i = 0; i < NumTop; i++)
            {
                Top[i].Current = Top[i].Terms.Next();
                if (Top[i].Current != null)
                {
                    Queue.Add(Top[i]);
                }
                else
                {
                    // no more fields in this reader
                }
            }
            NumTop = 0;
        }

        public override BytesRef Next()
        {
            if (LastSeekExact)
            {
                // Must SeekCeil at this point, so those subs that
                // didn't have the term can find the following term.
                // NOTE: we could save some CPU by only SeekCeil the
                // subs that didn't match the last exact seek... but
                // most impls short-circuit if you SeekCeil to term
                // they are already on.
                SeekStatus status = SeekCeil(Current);
                Debug.Assert(status == SeekStatus.FOUND);
                LastSeekExact = false;
            }
            LastSeek = null;

            // restore queue
            PushTop();

            // gather equal top fields
            if (Queue.Size() > 0)
            {
                PullTop();
            }
            else
            {
                Current = null;
            }

            return Current;
        }

        public override int DocFreq()
        {
            int sum = 0;
            for (int i = 0; i < NumTop; i++)
            {
                sum += Top[i].Terms.DocFreq();
            }
            return sum;
        }

        public override long TotalTermFreq()
        {
            long sum = 0;
            for (int i = 0; i < NumTop; i++)
            {
                long v = Top[i].Terms.TotalTermFreq();
                if (v == -1)
                {
                    return v;
                }
                sum += v;
            }
            return sum;
        }

        public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
        {
            MultiDocsEnum docsEnum;
            // Can only reuse if incoming enum is also a MultiDocsEnum
            if (reuse != null && reuse is MultiDocsEnum)
            {
                docsEnum = (MultiDocsEnum)reuse;
                // ... and was previously created w/ this MultiTermsEnum:
                if (!docsEnum.CanReuse(this))
                {
                    docsEnum = new MultiDocsEnum(this, Subs.Length);
                }
            }
            else
            {
                docsEnum = new MultiDocsEnum(this, Subs.Length);
            }

            MultiBits multiLiveDocs;
            if (liveDocs is MultiBits)
            {
                multiLiveDocs = (MultiBits)liveDocs;
            }
            else
            {
                multiLiveDocs = null;
            }

            int upto = 0;

            for (int i = 0; i < NumTop; i++)
            {
                TermsEnumWithSlice entry = Top[i];

                Bits b;

                if (multiLiveDocs != null)
                {
                    // optimize for common case: requested skip docs is a
                    // congruent sub-slice of MultiBits: in this case, we
                    // just pull the liveDocs from the sub reader, rather
                    // than making the inefficient
                    // Slice(Multi(sub-readers)):
                    MultiBits.SubResult sub = multiLiveDocs.GetMatchingSub(entry.SubSlice);
                    if (sub.Matches)
                    {
                        b = sub.Result;
                    }
                    else
                    {
                        // custom case: requested skip docs is foreign:
                        // must slice it on every access
                        b = new BitsSlice(liveDocs, entry.SubSlice);
                    }
                }
                else if (liveDocs != null)
                {
                    b = new BitsSlice(liveDocs, entry.SubSlice);
                }
                else
                {
                    // no deletions
                    b = null;
                }

                Debug.Assert(entry.Index < docsEnum.SubDocsEnum.Length, entry.Index + " vs " + docsEnum.SubDocsEnum.Length + "; " + Subs.Length);
                DocsEnum subDocsEnum = entry.Terms.Docs(b, docsEnum.SubDocsEnum[entry.Index], flags);
                if (subDocsEnum != null)
                {
                    docsEnum.SubDocsEnum[entry.Index] = subDocsEnum;
                    SubDocs[upto].DocsEnum = subDocsEnum;
                    SubDocs[upto].Slice = entry.SubSlice;
                    upto++;
                }
                else
                {
                    // should this be an error?
                    Debug.Assert(false, "One of our subs cannot provide a docsenum");
                }
            }

            if (upto == 0)
            {
                return null;
            }
            else
            {
                return docsEnum.Reset(SubDocs, upto);
            }
        }

        public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
        {
            MultiDocsAndPositionsEnum docsAndPositionsEnum;
            // Can only reuse if incoming enum is also a MultiDocsAndPositionsEnum
            if (reuse != null && reuse is MultiDocsAndPositionsEnum)
            {
                docsAndPositionsEnum = (MultiDocsAndPositionsEnum)reuse;
                // ... and was previously created w/ this MultiTermsEnum:
                if (!docsAndPositionsEnum.CanReuse(this))
                {
                    docsAndPositionsEnum = new MultiDocsAndPositionsEnum(this, Subs.Length);
                }
            }
            else
            {
                docsAndPositionsEnum = new MultiDocsAndPositionsEnum(this, Subs.Length);
            }

            MultiBits multiLiveDocs;
            if (liveDocs is MultiBits)
            {
                multiLiveDocs = (MultiBits)liveDocs;
            }
            else
            {
                multiLiveDocs = null;
            }

            int upto = 0;

            for (int i = 0; i < NumTop; i++)
            {
                TermsEnumWithSlice entry = Top[i];

                Bits b;

                if (multiLiveDocs != null)
                {
                    // Optimize for common case: requested skip docs is a
                    // congruent sub-slice of MultiBits: in this case, we
                    // just pull the liveDocs from the sub reader, rather
                    // than making the inefficient
                    // Slice(Multi(sub-readers)):
                    MultiBits.SubResult sub = multiLiveDocs.GetMatchingSub(Top[i].SubSlice);
                    if (sub.Matches)
                    {
                        b = sub.Result;
                    }
                    else
                    {
                        // custom case: requested skip docs is foreign:
                        // must slice it on every access (very
                        // inefficient)
                        b = new BitsSlice(liveDocs, Top[i].SubSlice);
                    }
                }
                else if (liveDocs != null)
                {
                    b = new BitsSlice(liveDocs, Top[i].SubSlice);
                }
                else
                {
                    // no deletions
                    b = null;
                }

                Debug.Assert(entry.Index < docsAndPositionsEnum.SubDocsAndPositionsEnum.Length, entry.Index + " vs " + docsAndPositionsEnum.SubDocsAndPositionsEnum.Length + "; " + Subs.Length);
                DocsAndPositionsEnum subPostings = entry.Terms.DocsAndPositions(b, docsAndPositionsEnum.SubDocsAndPositionsEnum[entry.Index], flags);

                if (subPostings != null)
                {
                    docsAndPositionsEnum.SubDocsAndPositionsEnum[entry.Index] = subPostings;
                    SubDocsAndPositions[upto].DocsAndPositionsEnum = subPostings;
                    SubDocsAndPositions[upto].Slice = entry.SubSlice;
                    upto++;
                }
                else
                {
                    if (entry.Terms.Docs(b, null, DocsEnum.FLAG_NONE) != null)
                    {
                        // At least one of our subs does not store
                        // offsets or positions -- we can't correctly
                        // produce a MultiDocsAndPositions enum
                        return null;
                    }
                }
            }

            if (upto == 0)
            {
                return null;
            }
            else
            {
                return docsAndPositionsEnum.Reset(SubDocsAndPositions, upto);
            }
        }

        public sealed class TermsEnumWithSlice
        {
            internal readonly ReaderSlice SubSlice;
            internal TermsEnum Terms;
            public BytesRef Current;
            internal readonly int Index;

            public TermsEnumWithSlice(int index, ReaderSlice subSlice)
            {
                this.SubSlice = subSlice;
                this.Index = index;
                Debug.Assert(subSlice.Length >= 0, "length=" + subSlice.Length);
            }

            public void Reset(TermsEnum terms, BytesRef term)
            {
                this.Terms = terms;
                Current = term;
            }

            public override string ToString()
            {
                return SubSlice.ToString() + ":" + Terms;
            }
        }

        private sealed class TermMergeQueue : Util.PriorityQueue<TermsEnumWithSlice>
        {
            internal IComparer<BytesRef> TermComp;

            internal TermMergeQueue(int size)
                : base(size)
            {
            }

            public override bool LessThan(TermsEnumWithSlice termsA, TermsEnumWithSlice termsB)
            {
                int cmp = TermComp.Compare(termsA.Current, termsB.Current);
                if (cmp != 0)
                {
                    return cmp < 0;
                }
                else
                {
                    return termsA.SubSlice.Start < termsB.SubSlice.Start;
                }
            }
        }

        public override string ToString()
        {
            return "MultiTermsEnum(" + Arrays.ToString(Subs) + ")";
        }
    }
}