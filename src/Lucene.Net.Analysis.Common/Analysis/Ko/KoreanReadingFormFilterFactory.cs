using Lucene.Net.Analysis.Util;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Ko
{
    public class KoreanReadingFormFilterFactory : TokenFilterFactory
    {
        public static readonly string NAME = "koreanReadingForm";

        public KoreanReadingFormFilterFactory(Dictionary<string, string> args)
            : base(args)
        {
            if (args.Count > 0)
            {
                throw new IllegalArgumentException("Unknown parameters: " + args);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new KoreanReadingFormFilter(input);
        }

    }
}