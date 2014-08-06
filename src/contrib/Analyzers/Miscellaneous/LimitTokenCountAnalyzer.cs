using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Miscellaneous
{
    public sealed class LimitTokenCountAnalyzer : AnalyzerWrapper
    {
        private readonly Analyzer delegateAnalyzer;
        private readonly int maxTokenCount;
        private readonly bool consumeAllTokens;

        public LimitTokenCountAnalyzer(Analyzer delegateAnalyzer, int maxTokenCount)
            : this(delegateAnalyzer, maxTokenCount, false)
        {
        }

        public LimitTokenCountAnalyzer(Analyzer delegateAnalyzer, int maxTokenCount, bool consumeAllTokens)
        {
            this.delegateAnalyzer = delegateAnalyzer;
            this.maxTokenCount = maxTokenCount;
            this.consumeAllTokens = consumeAllTokens;
        }



        protected override Analyzer GetWrappedAnalyzer(string fieldName)
        {
            return delegateAnalyzer;
        }

        protected override TokenStreamComponents WrapComponents(string fieldName, TokenStreamComponents components)
        {
            return new TokenStreamComponents(components.Tokenizer, new LimitTokenCountFilter(components.TokenStream, maxTokenCount, consumeAllTokens));
        }

        public override string ToString()
        {
            return string.Format("LimitTokenCountAnalyzer({0}, maxTokenCount={1}, consumeAllTokens={2})", delegateAnalyzer, maxTokenCount, consumeAllTokens);
        }
    }
}
