using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public abstract class TermsEnum : IBytesRefIterator
    {
        private AttributeSource atts = null;

        protected TermsEnum()
        {
        }

        public virtual AttributeSource Attributes
        {
            get
            {
                if (atts == null) atts = new AttributeSource();
                return atts;
            }
        }

        /** Represents returned result from {@link #seekCeil}. */
        public enum SeekStatus
        {
            /** The term was not found, and the end of iteration was hit. */
            END,
            /** The precise term was found. */
            FOUND,
            /** A different term was found after the requested term */
            NOT_FOUND
        }

        public virtual bool SeekExact(BytesRef text, bool useCache)
        {
            return SeekCeil(text, useCache) == SeekStatus.FOUND;
        }

        public abstract SeekStatus SeekCeil(BytesRef text, bool useCache);

        public SeekStatus SeekCeil(BytesRef text)
        {
            return SeekCeil(text, true);
        }

        public abstract void SeekExact(long ord);

        public virtual void SeekExact(BytesRef term, TermState state)
        {
            if (!SeekExact(term, true))
            {
                throw new ArgumentException("term=" + term + " does not exist");
            }
        }

        public abstract BytesRef Term { get; }

        public abstract long Ord { get; }

        public abstract int DocFreq { get; }

        public abstract long TotalTermFreq { get; }

        public DocsEnum Docs(IBits liveDocs, DocsEnum reuse)
        {
            return Docs(liveDocs, reuse, DocsEnum.FLAG_FREQS);
        }

        public abstract DocsEnum Docs(IBits liveDocs, DocsEnum reuse, int flags);

        public DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse)
        {
            return DocsAndPositions(liveDocs, reuse, DocsAndPositionsEnum.FLAG_OFFSETS | DocsAndPositionsEnum.FLAG_PAYLOADS);
        }

        public abstract DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, int flags);

        private sealed class AnonymousTermState : TermState
        {
            public override void CopyFrom(TermState other)
            {
                throw new NotSupportedException();
            }
        }

        public virtual TermState TermState
        {
            get { return new AnonymousTermState(); }
        }

        private sealed class AnonymousEmptyTermsEnum : TermsEnum
        {
            public override SeekStatus SeekCeil(BytesRef text, bool useCache)
            {
                return SeekStatus.END;
            }

            public override void SeekExact(long ord)
            {
            }

            public override BytesRef Term
            {
                get { throw new InvalidOperationException("this property should never be called."); }
            }

            public override IComparer<BytesRef> Comparator
            {
                get { return null; }
            }

            public override int DocFreq
            {
                get { throw new InvalidOperationException("this property should never be called."); }
            }

            public override long TotalTermFreq
            {
                get { throw new InvalidOperationException("this property should never be called."); }
            }

            public override long Ord
            {
                get { throw new InvalidOperationException("this property should never be called."); }
            }

            public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, int flags)
            {
                throw new InvalidOperationException("this method should never be called.");
            }

            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, int flags)
            {
                throw new InvalidOperationException("this method should never be called.");
            }

            public override BytesRef Next()
            {
                return null;
            }

            // make it synchronized here, to prevent double lazy init
            public override AttributeSource Attributes
            {
                get
                {
                    lock (this)
                    {
                        return base.Attributes;
                    }
                }
            }

            public override TermState TermState
            {
                get
                {
                    throw new InvalidOperationException("this property should never be called.");
                }
            }

            public override bool SeekExact(BytesRef text, bool useCache)
            {
                throw new InvalidOperationException("this method should never be called.");
            }
        }

        public static readonly TermsEnum EMPTY = new AnonymousEmptyTermsEnum();

        public abstract BytesRef Next();

        public abstract IComparer<BytesRef> Comparator { get; }
    }
}
