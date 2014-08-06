using System;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analysis.Util;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.Miscellaneous
{
    public sealed class RemoveDuplicatesTokenFilter : TokenFilter
    {
        private readonly ICharTermAttribute termAttribute;
        private readonly IPositionIncrementAttribute posIncAttribute;

        // use a fixed version, as we don't care about case sensitivity.
        private readonly CharArraySet previous = new CharArraySet(Version.LUCENE_33, 8, false);

        public RemoveDuplicatesTokenFilter(TokenStream input) : base(input)
        {
            posIncAttribute = AddAttribute<PositionIncrementAttribute>();
            termAttribute = AddAttribute<CharTermAttribute>();
        }

        public override bool IncrementToken()
        {
            while (input.IncrementToken())
            {
                var term = termAttribute.Buffer;
                var length = termAttribute.Length;
                var posIncrement = posIncAttribute.PositionIncrement;

                if (posIncrement > 0)
                {
                    previous.Clear();
                }

                var duplicate = (posIncrement == 0 && previous.Contains(term, 0, length));

                // clone the term, and add to the set of seen terms.
                var saved = new char[length];
                Array.Copy(term, 0, saved, 0, length);
                previous.Add(saved);

                if (!duplicate)
                {
                    return true;
                }
            }
            return false;
        }

        public override void Reset()
        {
            base.Reset();
            previous.Clear();
        }
    }
}