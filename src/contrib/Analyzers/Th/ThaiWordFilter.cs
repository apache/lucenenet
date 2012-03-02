using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Analysis.Th
{
    /**
     * {@link TokenFilter} that use {@link java.text.BreakIterator} to break each 
     * Token that is Thai into separate Token(s) for each Thai word.
     * <p>WARNING: this filter may not work correctly with all JREs.
     * It is known to work with Sun/Oracle and Harmony JREs.
     */
    public sealed class ThaiWordFilter : TokenFilter
    {
        //private BreakIterator breaker = null;

        private TermAttribute termAtt;
        private OffsetAttribute offsetAtt;

        private State thaiState = null;
        // I'm sure this is far slower than if we just created a simple UnicodeBlock class
        // considering this is used on a single char, we have to create a new string for it,
        // via ToString(), so we can then run a costly(?) regex on it.  Yikes.
        private Regex _isThaiRegex = new Regex(@"\p{IsThai}", RegexOptions.Compiled);

        public ThaiWordFilter(TokenStream input)
            : base(input)
        {
            throw new NotSupportedException("PORT ISSUES");
            //breaker = BreakIterator.getWordInstance(new Locale("th"));
            //termAtt = AddAttribute<TermAttribute>();
            //offsetAtt = AddAttribute<OffsetAttribute>();
        }

        public sealed override bool IncrementToken()
        {
            //int end;
            //if (thaiState != null)
            //{
            //    int start = breaker.Current();
            //    end = breaker.next();
            //    if (end != BreakIterator.DONE)
            //    {
            //        RestoreState(thaiState);
            //        termAtt.SetTermBuffer(termAtt.TermBuffer(), start, end - start);
            //        offsetAtt.SetOffset(offsetAtt.StartOffset() + start, offsetAtt.StartOffset() + end);
            //        return true;
            //    }
            //    thaiState = null;
            //}

            //if (input.IncrementToken() == false || termAtt.TermLength() == 0)
            //    return false;

            //String text = termAtt.Term();
            //if (!_isThaiRegex.Match(new string(new[]{text[0]})).Success)
            //{
            //    termAtt.SetTermBuffer(text.ToLower());
            //    return true;
            //}

            //thaiState = CaptureState();

            //breaker.SetText(text);
            //end = breaker.next();
            //if (end != BreakIterator.DONE)
            //{
            //    termAtt.SetTermBuffer(text, 0, end);
            //    offsetAtt.SetOffset(offsetAtt.StartOffset(), offsetAtt.StartOffset() + end);
            //    return true;
            //}
            return false;
        }

        public override void Reset()
        {
            base.Reset();
            thaiState = null;
        }
    }
}