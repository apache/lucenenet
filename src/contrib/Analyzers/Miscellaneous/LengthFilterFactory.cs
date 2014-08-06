using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Miscellaneous
{
    public class LengthFilterFactory : TokenFilterFactory
    {
        private readonly int min;
        private readonly int max;
        private readonly bool enablePositionIncrements;
        public static readonly String MIN_KEY = "min";
        public static readonly String MAX_KEY = "max";

        public LengthFilterFactory(IDictionary<string, string> args) : base(args)
        {
            min = RequireInt(args, MIN_KEY);
            max = RequireInt(args, MAX_KEY);
            enablePositionIncrements = GetBoolean(args, "enablePositionIncrements", false);
            if (args.Any())
            {
                throw new ArgumentException("Unknown parameters: " + args);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new LengthFilter(enablePositionIncrements, input, min, max);
        }
    }
}
