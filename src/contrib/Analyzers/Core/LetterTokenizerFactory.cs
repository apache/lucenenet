using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Core
{
    public class LetterTokenizerFactory : TokenizerFactory
    {
        public LetterTokenizerFactory(IDictionary<String, String> args)
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
            return new LetterTokenizer(luceneMatchVersion, factory, input);
        }
    }
}
