using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Util
{
    public abstract class TokenizerFactory : AbstractAnalysisFactory
    {
        private static readonly AnalysisSPILoader<TokenizerFactory> loader =
            new AnalysisSPILoader<TokenizerFactory>(typeof(TokenizerFactory));

        public static TokenizerFactory ForName(String name, IDictionary<String, String> args)
        {
            return loader.NewInstance(name, args);
        }

        public static Type LookupClass(String name)
        {
            return loader.LookupClass(name);
        }

        public static ICollection<String> AvailableTokenizers
        {
            get
            {
                return loader.AvailableServices;
            }
        }

        public static void ReloadTokenizers()
        {
            loader.Reload();
        }

        protected TokenizerFactory(IDictionary<String, String> args)
            : base(args)
        {
        }

        public Tokenizer Create(TextReader input)
        {
            return Create(Lucene.Net.Util.AttributeSource.AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, input);
        }

        public abstract Tokenizer Create(Lucene.Net.Util.AttributeSource.AttributeFactory factory, TextReader input);
    }
}
