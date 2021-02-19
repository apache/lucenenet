using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Diagnostics;
using System.Collections.Generic;
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
        private readonly IList<CharFilterFactory> charFilterFactories;
        private readonly TokenizerFactory tokenizerFactory;
        private readonly IList<TokenFilterFactory> tokenFilterFactories;
        public string Name { get; set; } = null;
        public int? PositionIncrementGap { get; set; } = null;
        public int? OffsetGap { get; set; } = null;

        public AnalyzerFactory(IList<CharFilterFactory> charFilterFactories,
                               TokenizerFactory tokenizerFactory,
                               IList<TokenFilterFactory> tokenFilterFactories)
        {
            this.charFilterFactories = charFilterFactories;
            if (Debugging.AssertsEnabled) Debugging.Assert(null != tokenizerFactory);
            this.tokenizerFactory = tokenizerFactory;
            this.tokenFilterFactories = tokenFilterFactories;
        }

        // LUCENENET specific - made Name, PositionIncrementGap, and OffsetGap setters into properties (above)

        public Analyzer Create()
        {
            return new AnalyzerAnonymousClass(this);
        }

        private sealed class AnalyzerAnonymousClass : Analyzer
        {
            private readonly AnalyzerFactory outerInstance;

            public AnalyzerAnonymousClass(AnalyzerFactory outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            protected internal override TextReader InitReader(string fieldName, TextReader reader)
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

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
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
                return outerInstance.PositionIncrementGap ?? base.GetPositionIncrementGap(fieldName);
            }

            public override int GetOffsetGap(string fieldName)
            {
                return outerInstance.OffsetGap ?? base.GetOffsetGap(fieldName);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("AnalyzerFactory(");
            if (null != Name)
            {
                sb.Append("name:");
                sb.Append(Name);
                sb.Append(", ");
            }
            if (null != PositionIncrementGap)
            {
                sb.Append("positionIncrementGap:");
                sb.Append(PositionIncrementGap);
                sb.Append(", ");
            }
            if (null != OffsetGap)
            {
                sb.Append("offsetGap:");
                sb.Append(OffsetGap);
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
