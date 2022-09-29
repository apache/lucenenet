using Lucene.Net.Analysis.Util;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Ko
{
    public class KoreanNumberFilterFactory : TokenFilterFactory
    {
        public static readonly string NAME = "koreanNumber";

        public KoreanNumberFilterFactory(Dictionary<string, string> args)
            : base(args)
        {
            if (args.Count > 0)
            {
                throw new IllegalArgumentException("Unknown parameters: " + args);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new KoreanNumberFilter(input);
        }
    }
}