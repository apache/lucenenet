using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;

namespace Lucene.Net.Codecs
{
    public class BlockTermState : OrdTermState
    {
        /** how many docs have this term */
        public int docFreq;
        /** total number of occurrences of this term */
        public long totalTermFreq;

        /** the term's ord in the current block */
        public int termBlockOrd;
        /** fp into the terms dict primary file (_X.tim) that holds this term */
        public long blockFilePointer;

        /** Sole constructor. (For invocation by subclass 
         *  constructors, typically implicit.) */
        protected BlockTermState()
        {
        }

        public override void CopyFrom(TermState _other)
        {
            //assert _other instanceof BlockTermState : "can not copy from " + _other.getClass().getName();
            BlockTermState other = (BlockTermState)_other;
            base.CopyFrom(_other);
            docFreq = other.docFreq;
            totalTermFreq = other.totalTermFreq;
            termBlockOrd = other.termBlockOrd;
            blockFilePointer = other.blockFilePointer;

            // NOTE: don't copy blockTermCount;
            // it's "transient": used only by the "primary"
            // termState, and regenerated on seek by TermState
        }

        public override string ToString()
        {
            return "docFreq=" + docFreq + " totalTermFreq=" + totalTermFreq + " termBlockOrd=" + termBlockOrd + " blockFP=" + blockFilePointer;
        }
    }
}
