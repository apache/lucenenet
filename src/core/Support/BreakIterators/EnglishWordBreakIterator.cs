using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Support.BreakIterators
{
    // HACK: someone please improve this!
    public class EnglishWordBreakIterator : EnglishBreakIteratorBase
    {
        public override bool IsBoundary(int offset)
        {
            char c = Peek(offset);
            
            if (char.IsLetterOrDigit(c))
                return false;

            if (char.IsWhiteSpace(c))
                return true;

            char cplus = Peek(offset + 1);

            if (cplus != ENDINPUT && char.IsLetterOrDigit(cplus))
                return false;

            return true;
        }
    }
}
