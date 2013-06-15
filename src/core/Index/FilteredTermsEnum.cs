using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public abstract class FilteredTermsEnum : TermsEnum
    {
        private BytesRef initialSeekTerm = null;
        private bool doSeek;
        private BytesRef actualTerm = null;

        private readonly TermsEnum tenum;

        protected enum AcceptStatus
        {
            /** Accept the term and position the enum at the next term. */
            YES,
            /** Accept the term and advance ({@link FilteredTermsEnum#nextSeekTerm(BytesRef)})
             * to the next term. */
            YES_AND_SEEK,
            /** Reject the term and position the enum at the next term. */
            NO,
            /** Reject the term and advance ({@link FilteredTermsEnum#nextSeekTerm(BytesRef)})
             * to the next term. */
            NO_AND_SEEK,
            /** Reject the term and stop enumerating. */
            END
        }

        protected abstract AcceptStatus Accept(BytesRef term);

        public FilteredTermsEnum(TermsEnum tenum)
            : this(tenum, true)
        {
        }

        public FilteredTermsEnum(TermsEnum tenum, bool startWithSeek)
        {
            //assert tenum != null;
            this.tenum = tenum;
            doSeek = startWithSeek;
        }

        protected BytesRef InitialSeekTerm
        {
            get { return this.initialSeekTerm; }
            set { this.initialSeekTerm = value; }
        }

        protected virtual BytesRef NextSeekTerm(BytesRef currentTerm)
        {
            BytesRef t = initialSeekTerm;
            initialSeekTerm = null;
            return t;
        }

        public override AttributeSource Attributes
        {
            get { return tenum.Attributes; }
        }

        public override BytesRef Term
        {
            get { return tenum.Term; }
        }

        public override IComparer<BytesRef> Comparator
        {
            get { return tenum.Comparator; }
        }

        public override int DocFreq
        {
            get { return tenum.DocFreq; }
        }

        public override long TotalTermFreq
        {
            get { return tenum.TotalTermFreq; }
        }

        public override bool SeekExact(BytesRef text, bool useCache)
        {
            throw new NotSupportedException(GetType().Name + " does not support seeking");
        }

        public override SeekStatus SeekCeil(BytesRef text, bool useCache)
        {
            throw new NotSupportedException(GetType().Name + " does not support seeking");
        }

        public override void SeekExact(long ord)
        {
            throw new NotSupportedException(GetType().Name + " does not support seeking");
        }

        public override long Ord
        {
            get { return tenum.Ord; }
        }

        public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, int flags)
        {
            return tenum.Docs(liveDocs, reuse, flags);
        }

        public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, int flags)
        {
            return tenum.DocsAndPositions(liveDocs, reuse, flags);
        }

        public override void SeekExact(BytesRef term, TermState state)
        {
            throw new NotSupportedException(GetType().Name + " does not support seeking");
        }

        public override TermState TermState
        {
            get
            {
                //assert tenum != null;
                return tenum.TermState;
            }
        }

        public override BytesRef Next()
        {
            //System.out.println("FTE.next doSeek=" + doSeek);
            //new Throwable().printStackTrace(System.out);
            for (; ; )
            {
                // Seek or forward the iterator
                if (doSeek)
                {
                    doSeek = false;
                    BytesRef t = NextSeekTerm(actualTerm);
                    //System.out.println("  seek to t=" + (t == null ? "null" : t.utf8ToString()) + " tenum=" + tenum);
                    // Make sure we always seek forward:
                    //assert actualTerm == null || t == null || getComparator().compare(t, actualTerm) > 0: "curTerm=" + actualTerm + " seekTerm=" + t;
                    if (t == null || tenum.SeekCeil(t, false) == SeekStatus.END)
                    {
                        // no more terms to seek to or enum exhausted
                        //System.out.println("  return null");
                        return null;
                    }
                    actualTerm = tenum.Term;
                    //System.out.println("  got term=" + actualTerm.utf8ToString());
                }
                else
                {
                    actualTerm = tenum.Next();
                    if (actualTerm == null)
                    {
                        // enum exhausted
                        return null;
                    }
                }

                // check if term is accepted
                switch (Accept(actualTerm))
                {
                    case AcceptStatus.YES_AND_SEEK:
                        doSeek = true;
                        // term accepted, but we need to seek so fall-through
                        return actualTerm; // .NET port: using return statement from case AcceptStatus.YES to go around fall-through
                    case AcceptStatus.YES:
                        // term accepted
                        return actualTerm;
                    case AcceptStatus.NO_AND_SEEK:
                        // invalid term, seek next time
                        doSeek = true;
                        break;
                    case AcceptStatus.END:
                        // we are supposed to end the enum
                        return null;
                }
            }
        }
    }
}
