using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Core
{
    public class LowerCaseTokenizerFactory : TokenizerFactory, IMultiTermAwareComponent
    {
        public LowerCaseTokenizerFactory(IDictionary<String, String> args)
            : base(args)
        {
            AssureMatchVersion();
            if (args.Count > 0)
            {
                throw new ArgumentException("Unknown parameters: " + args);
            }
        }

        public override Tokenizer Create(Net.Util.AttributeSource.AttributeFactory factory, System.IO.TextReader input)
        {
            return new LowerCaseTokenizer(luceneMatchVersion, factory, input);
        }
        
        public AbstractAnalysisFactory MultiTermComponent
        {
            get { return new LowerCaseFilterFactory(new HashMap<String, String>(OriginalArgs)); }
        }
    }
}
