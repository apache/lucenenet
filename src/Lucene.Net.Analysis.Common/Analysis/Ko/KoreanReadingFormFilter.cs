using Lucene.Net.Analysis.Ko.TokenAttributes;
using Lucene.Net.Analysis.TokenAttributes;

namespace Lucene.Net.Analysis.Ko
{
    public sealed class KoreanReadingFormFilter : TokenFilter
    {
        private readonly ICharTermAttribute termAttr;
        private readonly IReadingAttribute readingAttr;

        public KoreanReadingFormFilter(TokenStream input, bool useRomaji)
            : base(input)
        {
            this.termAttr = AddAttribute<ICharTermAttribute>();
            this.readingAttr = AddAttribute<IReadingAttribute>();
        }

        public KoreanReadingFormFilter(TokenStream input)
            : this(input, false)
        {
        }

        public override bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                string reading = readingAttr.GetReading();

                if (reading is null)
                {
                    // if its an OOV term, just try the term text
                    termAttr.SetEmpty().
                        Append(reading);
                }
                else
                {
                    termAttr.SetEmpty().
                        Append(reading);
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}