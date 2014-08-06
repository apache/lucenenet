using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Miscellaneous
{
    public class LimitTokenPositionFilterFactory : TokenFilterFactory 
    {
        public static readonly String MAX_TOKEN_POSITION_KEY = "maxTokenPosition";
        public static readonly String CONSUME_ALL_TOKENS_KEY = "consumeAllTokens";
        readonly int maxTokenPosition;
        readonly bool consumeAllTokens;

        public LimitTokenPositionFilterFactory(IDictionary<string, string> args) : base(args)
        {
            maxTokenPosition = RequireInt(args, MAX_TOKEN_POSITION_KEY);
            consumeAllTokens = GetBoolean(args, CONSUME_ALL_TOKENS_KEY, false);
            if (args.Any())
            {
                throw new ArgumentException("Unknown parameters: " + args);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new LimitTokenPositionFilter(input, maxTokenPosition, consumeAllTokens);
        }
    }
}
