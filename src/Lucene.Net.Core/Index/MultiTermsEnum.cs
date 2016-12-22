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
        private readonly TermMergeQueue queue;
        private readonly TermsEnumWithSlice[] subs; // all of our subs (one per sub-reader)
        private readonly TermsEnumWithSlice[] currentSubs; // current subs that have at least one term for this field
        private readonly TermsEnumWithSlice[] top;
        private readonly MultiDocsEnum.EnumWithSlice[] subDocs;
        private readonly MultiDocsAndPositionsEnum.EnumWithSlice[] subDocsAndPositions;

        private BytesRef lastSeek;
        private bool lastSeekExact;
        private readonly BytesRef lastSeekScratch = new BytesRef();

        private int numTop;
        private int numSubs;
        private BytesRef current;
        private IComparer<BytesRef> termComp;

        public class TermsEnumIndex
        {
            public static readonly TermsEnumIndex[] EMPTY_ARRAY = new TermsEnumIndex[0];
            internal int SubIndex { get; private set; }
            internal TermsEnum TermsEnum { get; private set; }

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
                return numTop;
            }
        }

        /// <summary>
        /// Returns sub-reader slices positioned to the current term. </summary>
        public TermsEnumWithSlice[] GetMatchArray() // LUCENENET NOTE: per MSDN properties shouldn't return array
        {
            return top;
        }

        /// <summary>
        /// Sole constructor. </summary>
        ///  <param name="slices"> Which sub-reader slices we should
        ///  merge.  </param>
        public MultiTermsEnum(ReaderSlice[] slices)
        {
            queue = new TermMergeQueue(slices.Length);
            top = new TermsEnumWithSlice[slices.Length];
            subs = new TermsEnumWithSlice[slices.Length];
            subDocs = new MultiDocsEnum.EnumWithSlice[slices.Length];
            subDocsAndPositions = new MultiDocsAndPositionsEnum.EnumWithSlice[slices.Length];
            for (int i = 0; i < slices.Length; i++)
            {
                subs[i] = new TermsEnumWithSlice(i, slices[i]);
                subDocs[i] = new MultiDocsEnum.EnumWithSlice();
                subDocs[i].Slice = slices[i];
                subDocsAndPositions[i] = new MultiDocsAndPositionsEnum.EnumWithSlice();
                subDocsAndPositions[i].Slice = slices[i];
            }
            currentSubs = new TermsEnumWithSlice[slices.Length];
        }

        public override BytesRef Term
        {
            get { return current; }
        }

        public override IComparer<BytesRef> Comparator
        {
            get
            {
                return termComp;
            }
        }

        /// <summary>
        /// The terms array must be newly created TermsEnum, ie
        ///  <seealso cref="TermsEnum#next"/> has not yet been called.
        /// </summary>
        public TermsEnum Reset(TermsEnumIndex[] termsEnumsIndex)
        {
            Debug.Assert(termsEnumsIndex.Length <= top.Length);
            numSubs = 0;
            numTop = 0;
            termComp = null;
            queue.Clear();
            for (int i = 0; i < termsEnumsIndex.Length; i++)
            {
                TermsEnumIndex termsEnumIndex = termsEnumsIndex[i];
                Debug.Assert(termsEnumIndex != null);

                // init our term comp
                if (termComp == null)
                {
                    queue.termComp = termComp = termsEnumIndex.TermsEnum.Comparator;
                }
                else
                {
                    // We cannot merge sub-readers that have
                    // different TermComps
                    IComparer<BytesRef> subTermComp = termsEnumIndex.TermsEnum.Comparator;
                    if (subTermComp != null && !subTermComp.Equals(termComp))
                    {
                        throw new InvalidOperationException("sub-readers have different BytesRef.Comparators: " + subTermComp + " vs " + termComp + "; cannot merge");
                    }
                }

                BytesRef term = termsEnumIndex.TermsEnum.Next();
                if (term != null)
                {
                    TermsEnumWithSlice entry = subs[termsEnumIndex.SubIndex];
                    entry.Reset(termsEnumIndex.TermsEnum, term);
                    queue.Add(entry);
                    currentSubs[numSubs++] = entry;
                }
                else
                {
                    // field has no terms
                }
            }

            if (queue.Size() == 0)
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
            queue.Clear();
            numTop = 0;

            bool seekOpt = false;
            if (lastSeek != null && termComp.Compare(lastSeek, term) <= 0)
            {
                seekOpt = true;
            }

            lastSeek = null;
            lastSeekExact = true;

            for (int i = 0; i < numSubs; i++)
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
                    BytesRef curTerm = currentSubs[i].Current;
                    if (curTerm != null)
                    {
                        int cmp = termComp.Compare(term, curTerm);
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
                            status = currentSubs[i].Terms.SeekExact(term);
                        }
                    }
                    else
                    {
                        status = false;
                    }
                }
                else
                {
                    status = currentSubs[i].Terms.SeekExact(term);
                }

                if (status)
                {
                    top[numTop++] = currentSubs[i];
                    current = currentSubs[i].Current = currentSubs[i].Terms.Term;
                    Debug.Assert(term.Equals(currentSubs[i].Current));
                }
            }

            // if at least one sub had exact match to the requested
            // term then we found match
            return numTop > 0;
        }

        public override SeekStatus SeekCeil(BytesRef term)
        {
            queue.Clear();
            numTop = 0;
            lastSeekExact = false;

            bool seekOpt = false;
            if (lastSeek != null && termComp.Compare(lastSeek, term) <= 0)
            {
                seekOpt = true;
            }

            lastSeekScratch.CopyBytes(term);
            lastSeek = lastSeekScratch;

            for (int i = 0; i < numSubs; i++)
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
                    BytesRef curTerm = currentSubs[i].Current;
                    if (curTerm != null)
                    {
                        int cmp = termComp.Compare(term, curTerm);
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
                            status = currentSubs[i].Terms.SeekCeil(term);
                        }
                    }
                    else
                    {
                        status = SeekStatus.END;
                    }
                }
                else
                {
                    status = currentSubs[i].Terms.SeekCeil(term);
                }

                if (status == SeekStatus.FOUND)
                {
                    top[numTop++] = currentSubs[i];
                    current = currentSubs[i].Current = currentSubs[i].Terms.Term;
                }
                else
                {
                    if (status == SeekStatus.NOT_FOUND)
                    {
                        currentSubs[i].Current = currentSubs[i].Terms.Term;
                        Debug.Assert(currentSubs[i].Current != null);
                        queue.Add(currentSubs[i]);
                    }
                    else
                    {
                        // enum exhausted
                        currentSubs[i].Current = null;
                    }
                }
            }

            if (numTop > 0)
            {
                // at least one sub had exact match to the requested term
                return SeekStatus.FOUND;
            }
            else if (queue.Size() > 0)
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
            Debug.Assert(numTop == 0);
            while (true)
            {
                top[numTop++] = queue.Pop();
                if (queue.Size() == 0 || !(queue.Top()).Current.BytesEquals(top[0].Current))
                {
                    break;
                }
            }
            current = top[0].Current;
        }

        private void PushTop()
        {
            // call next() on each top, and put back into queue
            for (int i = 0; i < numTop; i++)
            {
                top[i].Current = top[i].Terms.Next();
                if (top[i].Current != null)
                {
                    queue.Add(top[i]);
                }
                else
                {
                    // no more fields in this reader
                }
            }
            numTop = 0;
        }

        public override BytesRef Next()
        {
            if (lastSeekExact)
            {
                // Must SeekCeil at this point, so those subs that
                // didn't have the term can find the following term.
                // NOTE: we could save some CPU by only SeekCeil the
                // subs that didn't match the last exact seek... but
                // most impls short-circuit if you SeekCeil to term
                // they are already on.
                SeekStatus status = SeekCeil(current);
                Debug.Assert(status == SeekStatus.FOUND);
                lastSeekExact = false;
            }
            lastSeek = null;

            // restore queue
            PushTop();

            // gather equal top fields
            if (queue.Size() > 0)
            {
                PullTop();
            }
            else
            {
                current = null;
            }

            return current;
        }

        public override int DocFreq()
        {
            int sum = 0;
            for (int i = 0; i < numTop; i++)
            {
                sum += top[i].Terms.DocFreq();
            }
            return sum;
        }

        public override long TotalTermFreq()
        {
            long sum = 0;
            for (int i = 0; i < numTop; i++)
            {
                long v = top[i].Terms.TotalTermFreq();
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
                    docsEnum = new MultiDocsEnum(this, subs.Length);
                }
            }
            else
            {
                docsEnum = new MultiDocsEnum(this, subs.Length);
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

            for (int i = 0; i < numTop; i++)
            {
                TermsEnumWithSlice entry = top[i];

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

                Debug.Assert(entry.Index < docsEnum.subDocsEnum.Length, entry.Index + " vs " + docsEnum.subDocsEnum.Length + "; " + subs.Length);
                DocsEnum subDocsEnum = entry.Terms.Docs(b, docsEnum.subDocsEnum[entry.Index], flags);
                if (subDocsEnum != null)
                {
                    docsEnum.subDocsEnum[entry.Index] = subDocsEnum;
                    subDocs[upto].DocsEnum = subDocsEnum;
                    subDocs[upto].Slice = entry.SubSlice;
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
                return docsEnum.Reset(subDocs, upto);
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
                    docsAndPositionsEnum = new MultiDocsAndPositionsEnum(this, subs.Length);
                }
            }
            else
            {
                docsAndPositionsEnum = new MultiDocsAndPositionsEnum(this, subs.Length);
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

            for (int i = 0; i < numTop; i++)
            {
                TermsEnumWithSlice entry = top[i];

                Bits b;

                if (multiLiveDocs != null)
                {
                    // Optimize for common case: requested skip docs is a
                    // congruent sub-slice of MultiBits: in this case, we
                    // just pull the liveDocs from the sub reader, rather
                    // than making the inefficient
                    // Slice(Multi(sub-readers)):
                    MultiBits.SubResult sub = multiLiveDocs.GetMatchingSub(top[i].SubSlice);
                    if (sub.Matches)
                    {
                        b = sub.Result;
                    }
                    else
                    {
                        // custom case: requested skip docs is foreign:
                        // must slice it on every access (very
                        // inefficient)
                        b = new BitsSlice(liveDocs, top[i].SubSlice);
                    }
                }
                else if (liveDocs != null)
                {
                    b = new BitsSlice(liveDocs, top[i].SubSlice);
                }
                else
                {
                    // no deletions
                    b = null;
                }

                Debug.Assert(entry.Index < docsAndPositionsEnum.subDocsAndPositionsEnum.Length, entry.Index + " vs " + docsAndPositionsEnum.subDocsAndPositionsEnum.Length + "; " + subs.Length);
                DocsAndPositionsEnum subPostings = entry.Terms.DocsAndPositions(b, docsAndPositionsEnum.subDocsAndPositionsEnum[entry.Index], flags);

                if (subPostings != null)
                {
                    docsAndPositionsEnum.subDocsAndPositionsEnum[entry.Index] = subPostings;
                    subDocsAndPositions[upto].DocsAndPositionsEnum = subPostings;
                    subDocsAndPositions[upto].Slice = entry.SubSlice;
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
                return docsAndPositionsEnum.Reset(subDocsAndPositions, upto);
            }
        }

        public sealed class TermsEnumWithSlice
        {
            internal ReaderSlice SubSlice { get; private set; }
            internal TermsEnum Terms { get; set; }
            public BytesRef Current { get; set; }
            internal int Index { get; private set; }

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
            internal IComparer<BytesRef> termComp;

            internal TermMergeQueue(int size)
                : base(size)
            {
            }

            public override bool LessThan(TermsEnumWithSlice termsA, TermsEnumWithSlice termsB)
            {
                int cmp = termComp.Compare(termsA.Current, termsB.Current);
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
            return "MultiTermsEnum(" + Arrays.ToString(subs) + ")";
        }
    }
}