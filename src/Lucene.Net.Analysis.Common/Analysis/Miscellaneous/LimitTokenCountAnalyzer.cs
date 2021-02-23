// Lucene version compatibility level 4.8.1
namespace Lucene.Net.Analysis.Miscellaneous
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// This <see cref="Analyzer"/> limits the number of tokens while indexing. It is
    /// a replacement for the maximum field length setting inside <see cref="Index.IndexWriter"/>. </summary>
    /// <seealso cref="LimitTokenCountFilter"/>
    public sealed class LimitTokenCountAnalyzer : AnalyzerWrapper
    {
        private readonly Analyzer @delegate;
        private readonly int maxTokenCount;
        private readonly bool consumeAllTokens;

        /// <summary>
        /// Build an analyzer that limits the maximum number of tokens per field.
        /// This analyzer will not consume any tokens beyond the maxTokenCount limit
        /// </summary>
        /// <seealso cref="LimitTokenCountAnalyzer(Analyzer,int,bool)"/>
        public LimitTokenCountAnalyzer(Analyzer @delegate, int maxTokenCount)
            : this(@delegate, maxTokenCount, false)
        {
        }

        /// <summary>
        /// Build an analyzer that limits the maximum number of tokens per field. </summary>
        /// <param name="delegate"> the analyzer to wrap </param>
        /// <param name="maxTokenCount"> max number of tokens to produce </param>
        /// <param name="consumeAllTokens"> whether all tokens from the delegate should be consumed even if maxTokenCount is reached. </param>
        public LimitTokenCountAnalyzer(Analyzer @delegate, int maxTokenCount, bool consumeAllTokens)
            : base(@delegate.Strategy)
        {
            this.@delegate = @delegate;
            this.maxTokenCount = maxTokenCount;
            this.consumeAllTokens = consumeAllTokens;
        }

        protected override Analyzer GetWrappedAnalyzer(string fieldName)
        {
            return @delegate;
        }

        protected override TokenStreamComponents WrapComponents(string fieldName, TokenStreamComponents components)
        {
            return new TokenStreamComponents(components.Tokenizer, new LimitTokenCountFilter(components.TokenStream, maxTokenCount, consumeAllTokens));
        }

        public override string ToString()
        {
            return "LimitTokenCountAnalyzer(" + @delegate.ToString() + ", maxTokenCount=" + maxTokenCount + ", consumeAllTokens=" + consumeAllTokens + ")";
        }
    }
}