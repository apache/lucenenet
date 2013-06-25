using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Index
{
    public sealed class MultiTermsEnum : TermsEnum
    {
        private readonly TermMergeQueue queue;
        private readonly TermsEnumWithSlice[] subs;        // all of our subs (one per sub-reader)
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
            public readonly static TermsEnumIndex[] EMPTY_ARRAY = new TermsEnumIndex[0];
            internal readonly int subIndex;
            internal readonly TermsEnum termsEnum;

            public TermsEnumIndex(TermsEnum termsEnum, int subIndex)
            {
                this.termsEnum = termsEnum;
                this.subIndex = subIndex;
            }
        }

        public int MatchCount
        {
            get { return numTop; }
        }

        public TermsEnumWithSlice[] MatchArray
        {
            get { return top; }
        }

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
                subDocs[i].slice = slices[i];
                subDocsAndPositions[i] = new MultiDocsAndPositionsEnum.EnumWithSlice();
                subDocsAndPositions[i].slice = slices[i];
            }
            currentSubs = new TermsEnumWithSlice[slices.Length];
        }

        public override BytesRef Term
        {
            get { return current; }
        }

        public override IComparer<BytesRef> Comparator
        {
            get { return termComp; }
        }

        public TermsEnum Reset(TermsEnumIndex[] termsEnumsIndex)
        {
            //assert termsEnumsIndex.length <= top.length;
            numSubs = 0;
            numTop = 0;
            termComp = null;
            queue.Clear();
            for (int i = 0; i < termsEnumsIndex.Length; i++)
            {

                TermsEnumIndex termsEnumIndex = termsEnumsIndex[i];
                //assert termsEnumIndex != null;

                // init our term comp
                if (termComp == null)
                {
                    queue.termComp = termComp = termsEnumIndex.termsEnum.Comparator;
                }
                else
                {
                    // We cannot merge sub-readers that have
                    // different TermComps
                    IComparer<BytesRef> subTermComp = termsEnumIndex.termsEnum.Comparator;
                    if (subTermComp != null && !subTermComp.Equals(termComp))
                    {
                        throw new InvalidOperationException("sub-readers have different BytesRef.Comparators: " + subTermComp + " vs " + termComp + "; cannot merge");
                    }
                }

                BytesRef term = termsEnumIndex.termsEnum.Next();
                if (term != null)
                {
                    TermsEnumWithSlice entry = subs[termsEnumIndex.subIndex];
                    entry.Reset(termsEnumIndex.termsEnum, term);
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

        public override bool SeekExact(BytesRef term, bool useCache)
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
                    BytesRef curTerm = currentSubs[i].current;
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
                            status = currentSubs[i].terms.SeekExact(term, useCache);
                        }
                    }
                    else
                    {
                        status = false;
                    }
                }
                else
                {
                    status = currentSubs[i].terms.SeekExact(term, useCache);
                }

                if (status)
                {
                    top[numTop++] = currentSubs[i];
                    current = currentSubs[i].current = currentSubs[i].terms.Term;
                    //assert term.equals(currentSubs[i].current);
                }
            }

            // if at least one sub had exact match to the requested
            // term then we found match
            return numTop > 0;
        }

        public override SeekStatus SeekCeil(BytesRef term, bool useCache)
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
                    BytesRef curTerm = currentSubs[i].current;
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
                            status = currentSubs[i].terms.SeekCeil(term, useCache);
                        }
                    }
                    else
                    {
                        status = SeekStatus.END;
                    }
                }
                else
                {
                    status = currentSubs[i].terms.SeekCeil(term, useCache);
                }

                if (status == SeekStatus.FOUND)
                {
                    top[numTop++] = currentSubs[i];
                    current = currentSubs[i].current = currentSubs[i].terms.Term;
                }
                else
                {
                    if (status == SeekStatus.NOT_FOUND)
                    {
                        currentSubs[i].current = currentSubs[i].terms.Term;
                        //assert currentSubs[i].current != null;
                        queue.Add(currentSubs[i]);
                    }
                    else
                    {
                        // enum exhausted
                        currentSubs[i].current = null;
                    }
                }
            }

            if (numTop > 0)
            {
                // at least one sub had exact match to the requested term
                return SeekStatus.FOUND;
            }
            else if (queue.Size > 0)
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
            throw new NotSupportedException();
        }

        public override long Ord
        {
            get { throw new NotSupportedException(); }
        }

        private void PullTop()
        {
            // extract all subs from the queue that have the same
            // top term
            //assert numTop == 0;
            while (true)
            {
                top[numTop++] = queue.Pop();
                if (queue.Size == 0 || !(queue.Top()).current.BytesEquals(top[0].current))
                {
                    break;
                }
            }
            current = top[0].current;
        }

        private void PushTop()
        {
            // call next() on each top, and put back into queue
            for (int i = 0; i < numTop; i++)
            {
                top[i].current = top[i].terms.Next();
                if (top[i].current != null)
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
                // Must seekCeil at this point, so those subs that
                // didn't have the term can find the following term.
                // NOTE: we could save some CPU by only seekCeil the
                // subs that didn't match the last exact seek... but
                // most impls short-circuit if you seekCeil to term
                // they are already on.
                SeekStatus status = SeekCeil(current);
                //assert status == SeekStatus.FOUND;
                lastSeekExact = false;
            }
            lastSeek = null;

            // restore queue
            PushTop();

            // gather equal top fields
            if (queue.Size > 0)
            {
                PullTop();
            }
            else
            {
                current = null;
            }

            return current;
        }

        public override int DocFreq
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < numTop; i++)
                {
                    sum += top[i].terms.DocFreq;
                }
                return sum;
            }
        }

        public override long TotalTermFreq
        {
            get
            {
                long sum = 0;
                for (int i = 0; i < numTop; i++)
                {
                    long v = top[i].terms.TotalTermFreq;
                    if (v == -1)
                    {
                        return v;
                    }
                    sum += v;
                }
                return sum;
            }
        }

        public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, int flags)
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

                IBits b;

                if (multiLiveDocs != null)
                {
                    // optimize for common case: requested skip docs is a
                    // congruent sub-slice of MultiBits: in this case, we
                    // just pull the liveDocs from the sub reader, rather
                    // than making the inefficient
                    // Slice(Multi(sub-readers)):
                    MultiBits.SubResult sub = multiLiveDocs.GetMatchingSub(entry.subSlice);
                    if (sub.matches)
                    {
                        b = sub.result;
                    }
                    else
                    {
                        // custom case: requested skip docs is foreign:
                        // must slice it on every access
                        b = new BitsSlice(liveDocs, entry.subSlice);
                    }
                }
                else if (liveDocs != null)
                {
                    b = new BitsSlice(liveDocs, entry.subSlice);
                }
                else
                {
                    // no deletions
                    b = null;
                }

                //assert entry.index < docsEnum.subDocsEnum.length: entry.index + " vs " + docsEnum.subDocsEnum.length + "; " + subs.length;
                DocsEnum subDocsEnum = entry.terms.Docs(b, docsEnum.subDocsEnum[entry.index], flags);
                if (subDocsEnum != null)
                {
                    docsEnum.subDocsEnum[entry.index] = subDocsEnum;
                    subDocs[upto].docsEnum = subDocsEnum;
                    subDocs[upto].slice = entry.subSlice;
                    upto++;
                }
                else
                {
                    // should this be an error?
                    //assert false : "One of our subs cannot provide a docsenum";
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

        public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, int flags)
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

                IBits b;

                if (multiLiveDocs != null)
                {
                    // Optimize for common case: requested skip docs is a
                    // congruent sub-slice of MultiBits: in this case, we
                    // just pull the liveDocs from the sub reader, rather
                    // than making the inefficient
                    // Slice(Multi(sub-readers)):
                    MultiBits.SubResult sub = multiLiveDocs.GetMatchingSub(top[i].subSlice);
                    if (sub.matches)
                    {
                        b = sub.result;
                    }
                    else
                    {
                        // custom case: requested skip docs is foreign:
                        // must slice it on every access (very
                        // inefficient)
                        b = new BitsSlice(liveDocs, top[i].subSlice);
                    }
                }
                else if (liveDocs != null)
                {
                    b = new BitsSlice(liveDocs, top[i].subSlice);
                }
                else
                {
                    // no deletions
                    b = null;
                }

                //assert entry.index < docsAndPositionsEnum.subDocsAndPositionsEnum.length: entry.index + " vs " + docsAndPositionsEnum.subDocsAndPositionsEnum.length + "; " + subs.length;
                DocsAndPositionsEnum subPostings = entry.terms.DocsAndPositions(b, docsAndPositionsEnum.subDocsAndPositionsEnum[entry.index], flags);

                if (subPostings != null)
                {
                    docsAndPositionsEnum.subDocsAndPositionsEnum[entry.index] = subPostings;
                    subDocsAndPositions[upto].docsAndPositionsEnum = subPostings;
                    subDocsAndPositions[upto].slice = entry.subSlice;
                    upto++;
                }
                else
                {
                    if (entry.terms.Docs(b, null, DocsEnum.FLAG_NONE) != null)
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
            internal readonly ReaderSlice subSlice;
            internal TermsEnum terms;
            public BytesRef current;
            internal readonly int index;

            public TermsEnumWithSlice(int index, ReaderSlice subSlice)
            {
                this.subSlice = subSlice;
                this.index = index;
                //assert subSlice.length >= 0: "length=" + subSlice.length;
            }

            public void Reset(TermsEnum terms, BytesRef term)
            {
                this.terms = terms;
                current = term;
            }

            public override string ToString()
            {
                return subSlice.ToString() + ":" + terms;
            }
        }

        private sealed class TermMergeQueue : Lucene.Net.Util.PriorityQueue<TermsEnumWithSlice>
        {
            internal IComparer<BytesRef> termComp;

            internal TermMergeQueue(int size)
                : base(size)
            {
            }

            public override bool LessThan(TermsEnumWithSlice termsA, TermsEnumWithSlice termsB)
            {
                int cmp = termComp.Compare(termsA.current, termsB.current);
                if (cmp != 0)
                {
                    return cmp < 0;
                }
                else
                {
                    return termsA.subSlice.start < termsB.subSlice.start;
                }
            }
        }

        public override string ToString()
        {
            return "MultiTermsEnum(" + string.Join(", ", (IEnumerable<TermsEnumWithSlice>)subs) + ")";
        }
    }
}
