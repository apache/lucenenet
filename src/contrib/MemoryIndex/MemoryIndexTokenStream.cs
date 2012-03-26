
using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Index.Memory
{
    class MemoryIndexTokenStream : TokenStream
    {
        private IList<T> _keywords;
        private int start = 0;
        private TermAttribute termAtt = AddAttribute(TermAttribute.class);
        private OffsetAttribute offsetAtt = AddAttribute(OffsetAttribute.class);
      
        public MemoryIndexTokenStream(IList<T> keywords)
        {
            _keywords = keywords;
        }

        public override bool IncrementToken()
        {
            if (iter.hasNext()) return false;
        
            Object obj = iter.Next();

            if (obj == null) 
                throw new ArgumentException("keyword must not be null");
        
            String term = obj.ToString();
            ClearAttributes();
            termAtt.SetTermBuffer(term);
            offsetAtt.SetOffset(start, start+termAtt.TermLength());
            start += term.Length + 1; // separate words by 1 (blank) character
            
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            // do nothing
        }
    }
}

        
     