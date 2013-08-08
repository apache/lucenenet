using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Standard
{
    public class StandardFilterFactory : TokenFilterFactory
    {
        public StandardFilterFactory(IDictionary<String, String> args)
            : base(args)
        {            
            AssureMatchVersion();
            if (args.Count > 0)
            {
                throw new ArgumentException("Unknown parameters: " + args);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new StandardFilter(luceneMatchVersion, input);
        }
    }
}
