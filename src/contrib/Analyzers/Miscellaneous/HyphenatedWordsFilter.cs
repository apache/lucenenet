using System.Text;
using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Analysis.Miscellaneous
{
    public class HyphenatedWordsFilter : TokenFilter
    {
        private readonly ICharTermAttribute termAttribute;
        private readonly IOffsetAttribute offsetAttribute;

        private readonly StringBuilder hyphenated = new StringBuilder();
        private State savedState;
        private bool exhausted = false;
        private int lastEndOffset = 0;

        public HyphenatedWordsFilter(TokenStream input) : base(input)
        {
            offsetAttribute = AddAttribute<OffsetAttribute>();
            termAttribute = AddAttribute<CharTermAttribute>();
        }

        public override bool IncrementToken()
        {
            while (!exhausted && input.IncrementToken())
            {
                char[] term = termAttribute.Buffer;
                int termLength = termAttribute.Length;
                lastEndOffset = offsetAttribute.EndOffset;

                if (termLength > 0 && term[termLength - 1] == '-')
                {
                    // a hyphenated word
                    // capture the state of the first token only
                    if (savedState == null)
                    {
                        savedState = CaptureState();
                    }
                    hyphenated.Append(term, 0, termLength - 1);
                }
                else if (savedState == null)
                {
                    // not part of a hyphenated word.
                    return true;
                }
                else
                {
                    // the final portion of a hyphenated word
                    hyphenated.Append(term, 0, termLength);
                    Unhyphenate();
                    return true;
                }
            }
            exhausted = true;

            if (savedState != null)
            {
                // the final term ends with a hyphen
                // add back the hyphen, for backwards compatibility.
                hyphenated.Append('-');
                Unhyphenate();
                return true;
            }
            return false;
        }

        public override void Reset()
        {
            base.Reset();
            hyphenated.Length = 0;
            savedState = null;
            exhausted = false;
            lastEndOffset = 0;
        }

        private void Unhyphenate()
        {
            RestoreState(savedState);
            savedState = null;

            char[] term = termAttribute.Buffer;
            int length = hyphenated.Length;
            if (length > termAttribute.Length)
            {
                term = termAttribute.ResizeBuffer(length);
            }
            hyphenated.CopyTo(0, term, 0, length); //hyphenated.GetChars(0, length, term, 0);
            termAttribute.SetLength(length);
            offsetAttribute.SetOffset(offsetAttribute.StartOffset, lastEndOffset);
            hyphenated.Length = 0;
        }
    }
}
