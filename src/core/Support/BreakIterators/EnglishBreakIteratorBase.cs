using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Support.BreakIterators
{
    // HACK: someone please improve this!
    public abstract class EnglishBreakIteratorBase : BreakIteratorBase
    {
        private static ISet<char> _sentenceDelims = new HashSet<char>() { '.', '!', '?' };
        private static ISet<char> _validLineDelims = new HashSet<char>() { ' ', '\t', '\r', '\n', '-' };

        protected static bool IsSentenceDelim(char c)
        {
            return _sentenceDelims.Contains(c);
        }

        protected static bool IsValidLineDelim(char c)
        {
            return _validLineDelims.Contains(c);
        }

        public abstract override bool IsBoundary(int offset);
    }
}
