using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Standard
{
    public class ClassicFilterFactory : TokenFilterFactory
    {
        public ClassicFilterFactory(IDictionary<String, String> args)
            : base(args)
        {
            if (args.Count > 0)
            {
                throw new ArgumentException("Unknown parameters: " + args);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new ClassicFilter(input);
        }
    }
}
