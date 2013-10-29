using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Support.BreakIterators
{
    // HACK: someone please improve this!
    public class EnglishSentenceBreakIterator : EnglishBreakIteratorBase
    {
        public override bool IsBoundary(int offset)
        {
            if (offset == _text.Length - 1)
                return true;

            char c = Peek(offset);

            if (!IsSentenceDelim(c))
                return false;

            return char.IsWhiteSpace(Peek(offset + 1));
        }
    }
}
