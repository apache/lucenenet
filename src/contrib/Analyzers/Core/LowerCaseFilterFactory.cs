using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Core
{
    public class LowerCaseFilterFactory : TokenFilterFactory, IMultiTermAwareComponent
    {
        public LowerCaseFilterFactory(IDictionary<String, String> args)
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
            return new LowerCaseFilter(luceneMatchVersion, input);
        }

        public AbstractAnalysisFactory MultiTermComponent
        {
            get { return this; }
        }
    }
}
