using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Util;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Lucene.Net.Benchmarks.ByTask.Utils
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
    /// A factory to create an analyzer.
    /// </summary>
    /// <seealso cref="Tasks.AnalyzerFactoryTask"/>
    public sealed class AnalyzerFactory
    {
        private IList<CharFilterFactory> charFilterFactories;
        private TokenizerFactory tokenizerFactory;
        private IList<TokenFilterFactory> tokenFilterFactories;
        private string name = null;
        private int? positionIncrementGap = null;
        private int? offsetGap = null;

        public AnalyzerFactory(IList<CharFilterFactory> charFilterFactories,
                               TokenizerFactory tokenizerFactory,
                               IList<TokenFilterFactory> tokenFilterFactories)
        {
            this.charFilterFactories = charFilterFactories;
            Debug.Assert(null != tokenizerFactory);
            this.tokenizerFactory = tokenizerFactory;
            this.tokenFilterFactories = tokenFilterFactories;
        }

        // LUCENENET TODO: Properties ?
        public void SetName(string name)
        {
            this.name = name;
        }

        public void SetPositionIncrementGap(int positionIncrementGap) // LUCENENET TODO: Nullable?
        {
            this.positionIncrementGap = positionIncrementGap;
        }

        public void SetOffsetGap(int offsetGap) // LUCENENET TODO: Nullable?
        {
            this.offsetGap = offsetGap;
        }

        public Analyzer Create()
        {
            return new AnalyzerAnonymousHelper(this);
        }

        private class AnalyzerAnonymousHelper : Analyzer
        {
            private readonly AnalyzerFactory outerInstance;

            public AnalyzerAnonymousHelper(AnalyzerFactory outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            protected override TextReader InitReader(string fieldName, TextReader reader)
            {
                if (outerInstance.charFilterFactories != null && outerInstance.charFilterFactories.Count > 0)
                {
                    TextReader wrappedReader = reader;
                    foreach (CharFilterFactory charFilterFactory in outerInstance.charFilterFactories)
                    {
                        wrappedReader = charFilterFactory.Create(wrappedReader);
                    }
                    reader = wrappedReader;
                }
                return reader;
            }

            protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = outerInstance.tokenizerFactory.Create(reader);
                TokenStream tokenStream = tokenizer;
                foreach (TokenFilterFactory filterFactory in outerInstance.tokenFilterFactories)
                {
                    tokenStream = filterFactory.Create(tokenStream);
                }
                return new TokenStreamComponents(tokenizer, tokenStream);
            }

            public override int GetPositionIncrementGap(string fieldName)
            {
                return outerInstance.positionIncrementGap.HasValue
                    ? outerInstance.positionIncrementGap.Value
                    : base.GetPositionIncrementGap(fieldName);
            }

            public override int GetOffsetGap(string fieldName)
            {
                return outerInstance.offsetGap.HasValue
                    ? outerInstance.offsetGap.Value
                    : base.GetOffsetGap(fieldName);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("AnalyzerFactory(");
            if (null != name)
            {
                sb.Append("name:");
                sb.Append(name);
                sb.Append(", ");
            }
            if (null != positionIncrementGap)
            {
                sb.Append("positionIncrementGap:");
                sb.Append(positionIncrementGap);
                sb.Append(", ");
            }
            if (null != offsetGap)
            {
                sb.Append("offsetGap:");
                sb.Append(offsetGap);
                sb.Append(", ");
            }
            foreach (CharFilterFactory charFilterFactory in charFilterFactories)
            {
                sb.Append(charFilterFactory);
                sb.Append(", ");
            }
            sb.Append(tokenizerFactory);
            foreach (TokenFilterFactory tokenFilterFactory in tokenFilterFactories)
            {
                sb.Append(", ");
                sb.Append(tokenFilterFactory);
            }
            sb.Append(')');
            return sb.ToString();
        }
    }
}
