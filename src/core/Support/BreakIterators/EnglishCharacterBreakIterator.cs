using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Support.BreakIterators
{
    // HACK: someone please improve this!
    public class EnglishCharacterBreakIterator : EnglishBreakIteratorBase
    {
        public override bool IsBoundary(int offset)
        {
            return true;
        }
    }
}
