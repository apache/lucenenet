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
            char cplus = Peek(offset + 1);

            if (char.IsLetterOrDigit(c))
                return false;

            if (cplus != ENDINPUT && char.IsLetterOrDigit(cplus))
                return false;

            return true;
        }
    }
}
