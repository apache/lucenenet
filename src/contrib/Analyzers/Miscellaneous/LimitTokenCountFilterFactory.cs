using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Miscellaneous
{
    public class LimitTokenCountFilterFactory : TokenFilterFactory
    {
        public static readonly String MAX_TOKEN_COUNT_KEY = "maxTokenCount";
        public static readonly String CONSUME_ALL_TOKENS_KEY = "consumeAllTokens";
        private readonly int maxTokenCount;
        private readonly bool consumeAllTokens;

        public LimitTokenCountFilterFactory(IDictionary<string, string> args) : base(args)
        {
            maxTokenCount = RequireInt(args, MAX_TOKEN_COUNT_KEY);
            consumeAllTokens = GetBoolean(args, CONSUME_ALL_TOKENS_KEY, false);
            if (args.Any())
            {
                throw new ArgumentException("Unknown parameters: " + args);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new LimitTokenCountFilter(input, maxTokenCount, consumeAllTokens);
        }
    }
}
