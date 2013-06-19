using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public abstract class Terms
    {
        protected Terms()
        {
        }

        public abstract TermsEnum Iterator(TermsEnum reuse);

        private sealed class AnonymousIntersectAutomatonTermsEnum : AutomatonTermsEnum
        {
            private readonly BytesRef startTerm;

            public AnonymousIntersectAutomatonTermsEnum(TermsEnum iterator, CompiledAutomaton compiled, BytesRef startTerm)
                : base(iterator, compiled)
            {
                this.startTerm = startTerm;
            }

            protected override BytesRef NextSeekTerm(BytesRef term)
            {
                if (term == null)
                {
                    term = startTerm;
                }

                return base.NextSeekTerm(term);
            }
        }

        public virtual TermsEnum Intersect(CompiledAutomaton compiled, BytesRef startTerm)
        {
            // TODO: eventually we could support seekCeil/Exact on
            // the returned enum, instead of only being able to seek
            // at the start
            if (compiled.type != CompiledAutomaton.AUTOMATON_TYPE.NORMAL)
            {
                throw new ArgumentException("please use CompiledAutomaton.getTermsEnum instead");
            }
            if (startTerm == null)
            {
                return new AutomatonTermsEnum(Iterator(null), compiled);
            }
            else
            {
                return new AnonymousIntersectAutomatonTermsEnum(Iterator(null), compiled, startTerm);
            }
        }

        public abstract IComparer<BytesRef> Comparator { get; }

        /** Returns the number of terms for this field, or -1 if this 
         *  measure isn't stored by the codec. Note that, just like 
         *  other term measures, this measure does not take deleted 
         *  documents into account. */
        public abstract long Size { get; }

        /** Returns the sum of {@link TermsEnum#totalTermFreq} for
         *  all terms in this field, or -1 if this measure isn't
         *  stored by the codec (or if this fields omits term freq
         *  and positions).  Note that, just like other term
         *  measures, this measure does not take deleted documents
         *  into account. */
        public abstract long SumTotalTermFreq { get; }

        /** Returns the sum of {@link TermsEnum#docFreq()} for
         *  all terms in this field, or -1 if this measure isn't
         *  stored by the codec.  Note that, just like other term
         *  measures, this measure does not take deleted documents
         *  into account. */
        public abstract long SumDocFreq { get; }

        /** Returns the number of documents that have at least one
         *  term for this field, or -1 if this measure isn't
         *  stored by the codec.  Note that, just like other term
         *  measures, this measure does not take deleted documents
         *  into account. */
        public abstract int DocCount { get; }

        /** Returns true if documents in this field store offsets. */
        public abstract bool HasOffsets { get; }

        /** Returns true if documents in this field store positions. */
        public abstract bool HasPositions { get; }

        /** Returns true if documents in this field store payloads. */
        public abstract bool HasPayloads { get; }

        /** Zero-length array of {@link Terms}. */
        public static readonly Terms[] EMPTY_ARRAY = new Terms[0];
    }
}
