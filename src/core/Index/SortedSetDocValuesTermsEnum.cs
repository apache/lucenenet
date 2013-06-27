using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    internal class SortedSetDocValuesTermsEnum : TermsEnum
    {
        private readonly SortedSetDocValues values;
        private long currentOrd = -1;
        private readonly BytesRef term = new BytesRef();

        public SortedSetDocValuesTermsEnum(SortedSetDocValues values)
        {
            this.values = values;
        }

        public override SeekStatus SeekCeil(BytesRef text, bool useCache)
        {
            long ord = values.LookupTerm(text);
            if (ord >= 0)
            {
                currentOrd = ord;
                term.offset = 0;
                // TODO: is there a cleaner way?
                // term.bytes may be pointing to codec-private byte[]
                // storage, so we must force new byte[] allocation:
                term.bytes = new sbyte[text.length];
                term.CopyBytes(text);
                return SeekStatus.FOUND;
            }
            else
            {
                currentOrd = -ord - 1;
                if (currentOrd == values.ValueCount)
                {
                    return SeekStatus.END;
                }
                else
                {
                    // TODO: hmm can we avoid this "extra" lookup?:
                    values.LookupOrd(currentOrd, term);
                    return SeekStatus.NOT_FOUND;
                }
            }
        }

        public override bool SeekExact(BytesRef text, bool useCache)
        {
            long ord = values.LookupTerm(text);
            if (ord >= 0)
            {
                term.offset = 0;
                // TODO: is there a cleaner way?
                // term.bytes may be pointing to codec-private byte[]
                // storage, so we must force new byte[] allocation:
                term.bytes = new sbyte[text.length];
                term.CopyBytes(text);
                currentOrd = ord;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void SeekExact(long ord)
        {
            //assert ord >= 0 && ord < values.getValueCount();
            currentOrd = (int)ord;
            values.LookupOrd(currentOrd, term);
        }

        public override BytesRef Next()
        {
            currentOrd++;
            if (currentOrd >= values.ValueCount)
            {
                return null;
            }
            values.LookupOrd(currentOrd, term);
            return term;
        }

        public override BytesRef Term
        {
            get { return term; }
        }

        public override long Ord
        {
            get { return currentOrd; }
        }

        public override int DocFreq
        {
            get { throw new NotSupportedException(); }
        }

        public override long TotalTermFreq
        {
            get { return -1; }
        }

        public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, int flags)
        {
            throw new NotSupportedException();
        }

        public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, int flags)
        {
            throw new NotSupportedException();
        }

        public override IComparer<BytesRef> Comparator
        {
            get { return BytesRef.UTF8SortedAsUnicodeComparer; }
        }

        public override void SeekExact(BytesRef term, TermState state)
        {
            //assert state != null && state instanceof OrdTermState;
            this.SeekExact(((OrdTermState)state).ord);
        }

        public override TermState TermState
        {
            get
            {
                OrdTermState state = new OrdTermState();
                state.ord = currentOrd;
                return state;
            }
        }
    }
}
