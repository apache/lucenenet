using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Miscellaneous
{
    public class ASCIIFoldingFilterFactory : TokenFilterFactory, IMultiTermAwareComponent 
    {
        public ASCIIFoldingFilterFactory(IDictionary<string, string> args) : base(args)
        {
            if (args.Any())
            {
                throw new ArgumentException("Unknown parameters: " + args);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new ASCIIFoldingFilter(input);
        }

        public AbstractAnalysisFactory MultiTermComponent { get { return this; } }
    }
}
