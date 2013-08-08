using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Standard
{
    public class StandardTokenizerFactory : TokenizerFactory
    {
        private readonly int maxTokenLength;

        public StandardTokenizerFactory(IDictionary<String, String> args)
            : base(args)
        {
            AssureMatchVersion();
            maxTokenLength = GetInt(args, "maxTokenLength", StandardAnalyzer.DEFAULT_MAX_TOKEN_LENGTH);
            if (args.Count > 0)
            {
                throw new ArgumentException("Unknown parameters: " + args);
            }
        }

        public override Tokenizer Create(Net.Util.AttributeSource.AttributeFactory factory, System.IO.TextReader input)
        {
            StandardTokenizer tokenizer = new StandardTokenizer(luceneMatchVersion, factory, input);
            tokenizer.MaxTokenLength = maxTokenLength;
            return tokenizer;
        }
    }
}
