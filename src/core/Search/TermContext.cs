using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search
{
    public sealed class TermContext
    {
        public readonly IndexReaderContext topReaderContext;
        private readonly TermState[] states;
        private int docFreq;
        private long totalTermFreq;

        public TermContext(IndexReaderContext context)
        {
            //assert context != null && context.isTopLevel;
            topReaderContext = context;
            docFreq = 0;
            int len;
            if (context.Leaves == null)
            {
                len = 1;
            }
            else
            {
                len = context.Leaves.Count;
            }
            states = new TermState[len];
        }

        public TermContext(IndexReaderContext context, TermState state, int ord, int docFreq, long totalTermFreq)
            : this(context)
        {
            Register(state, ord, docFreq, totalTermFreq);
        }

        public static TermContext Build(IndexReaderContext context, Term term, bool cache)
        {
            //assert context != null && context.isTopLevel;
            String field = term.Field;
            BytesRef bytes = term.Bytes;
            TermContext perReaderTermState = new TermContext(context);
            //if (DEBUG) System.out.println("prts.build term=" + term);
            foreach (AtomicReaderContext ctx in context.Leaves)
            {
                //if (DEBUG) System.out.println("  r=" + leaves[i].reader);
                Fields fields = ctx.Reader.Fields;
                if (fields != null)
                {
                    Terms terms = fields.Terms(field);
                    if (terms != null)
                    {
                        TermsEnum termsEnum = terms.Iterator(null);
                        if (termsEnum.SeekExact(bytes, cache))
                        {
                            TermState termState = termsEnum.TermState;
                            //if (DEBUG) System.out.println("    found");
                            perReaderTermState.Register(termState, ctx.ord, termsEnum.DocFreq, termsEnum.TotalTermFreq);
                        }
                    }
                }
            }
            return perReaderTermState;
        }

        public void Clear()
        {
            docFreq = 0;
            Arrays.Fill(states, null);
        }


        public void Register(TermState state, int ord, int docFreq, long totalTermFreq)
        {
            //assert state != null : "state must not be null";
            //assert ord >= 0 && ord < states.length;
            //assert states[ord] == null : "state for ord: " + ord
            //    + " already registered";
            this.docFreq += docFreq;
            if (this.totalTermFreq >= 0 && totalTermFreq >= 0)
                this.totalTermFreq += totalTermFreq;
            else
                this.totalTermFreq = -1;
            states[ord] = state;
        }

        public TermState Get(int ord)
        {
            //assert ord >= 0 && ord < states.length;
            return states[ord];
        }

        public int DocFreq
        {
            get { return docFreq; }
            set { docFreq = value; }
        }

        public long TotalTermFreq
        {
            get { return totalTermFreq; }
        }
    }
}
