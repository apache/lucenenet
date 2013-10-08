using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.BR
{
    public class BrazilianStemFilterFactory : TokenFilterFactory 
    {
        public BrazilianStemFilterFactory(IDictionary<string, string> args) : base(args)
        {
            if (args.Any())
            {
                throw new ArgumentException("Unknown parameters: " + args);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new BrazilianStemFilter(input);
        }
    }
}
