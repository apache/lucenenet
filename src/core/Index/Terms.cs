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

        public abstract TermsEnum iterator(TermsEnum reuse);
        
  public TermsEnum Intersect(CompiledAutomaton compiled, BytesRef startTerm) {
    // TODO: eventually we could support seekCeil/Exact on
    // the returned enum, instead of only being able to seek
    // at the start
    if (compiled.type != CompiledAutomaton.AUTOMATON_TYPE.NORMAL) {
      throw new ArgumentException("please use CompiledAutomaton.getTermsEnum instead");
    }
    if (startTerm == null) {
      return new AutomatonTermsEnum(iterator(null), compiled);
    } else {
      return new AutomatonTermsEnum(iterator(null), compiled) {
        
        protected override BytesRef nextSeekTerm(BytesRef term){
          if (term == null) {
            term = startTerm;
          }
          return super.nextSeekTerm(term);
        }
      };
    }
}
    public abstract IComparer<BytesRef> getComparator();

  /** Returns the number of terms for this field, or -1 if this 
   *  measure isn't stored by the codec. Note that, just like 
   *  other term measures, this measure does not take deleted 
   *  documents into account. */
  public abstract long Size();
  
  /** Returns the sum of {@link TermsEnum#totalTermFreq} for
   *  all terms in this field, or -1 if this measure isn't
   *  stored by the codec (or if this fields omits term freq
   *  and positions).  Note that, just like other term
   *  measures, this measure does not take deleted documents
   *  into account. */
  public abstract long GetSumTotalTermFreq();

  /** Returns the sum of {@link TermsEnum#docFreq()} for
   *  all terms in this field, or -1 if this measure isn't
   *  stored by the codec.  Note that, just like other term
   *  measures, this measure does not take deleted documents
   *  into account. */
  public abstract long GetSumDocFreq();

  /** Returns the number of documents that have at least one
   *  term for this field, or -1 if this measure isn't
   *  stored by the codec.  Note that, just like other term
   *  measures, this measure does not take deleted documents
   *  into account. */
  public abstract int GetDocCount();
  
  /** Returns true if documents in this field store offsets. */
  public abstract bool HasOffsets();
  
  /** Returns true if documents in this field store positions. */
  public abstract bool HasPositions();
  
  /** Returns true if documents in this field store payloads. */
  public abstract bool HasPayloads();

  /** Zero-length array of {@link Terms}. */
  public static readonly Terms[] EMPTY_ARRAY = new Terms[0];
    }
}
