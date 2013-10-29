using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Support.BreakIterators
{
    // HACK: someone please improve this!
    public class EnglishLineBreakIterator : EnglishBreakIteratorBase
    {
        public override bool IsBoundary(int offset)
        {
            if (offset == _text.Length - 1)
                return true;

            char c = Peek(offset);

            return IsValidLineDelim(c);
        }
    }
}
