using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Index;
using Lucene.Net.Search.Spans;
using Lucene.Net.Util;
using System;
using System.Xml;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.QueryParsers.Xml.Builders
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
    /// Builder that analyzes the text into a <see cref="SpanOrQuery"/>
    /// </summary>
    public class SpanOrTermsBuilder : SpanBuilderBase
    {
        private readonly Analyzer analyzer;

        public SpanOrTermsBuilder(Analyzer analyzer)
        {
            this.analyzer = analyzer;
        }

        public override SpanQuery GetSpanQuery(XmlElement e)
        {
            string fieldName = DOMUtils.GetAttributeWithInheritanceOrFail(e, "fieldName");
            string value = DOMUtils.GetNonBlankTextOrFail(e);

            JCG.List<SpanQuery> clausesList = new JCG.List<SpanQuery>();

            TokenStream ts = null;
            try
            {
                ts = analyzer.GetTokenStream(fieldName, value);
                ITermToBytesRefAttribute termAtt = ts.AddAttribute<ITermToBytesRefAttribute>();
                BytesRef bytes = termAtt.BytesRef;
                ts.Reset();
                while (ts.IncrementToken())
                {
                    termAtt.FillBytesRef();
                    SpanTermQuery stq = new SpanTermQuery(new Term(fieldName, BytesRef.DeepCopyOf(bytes)));
                    clausesList.Add(stq);
                }
                ts.End();
                SpanOrQuery soq = new SpanOrQuery(clausesList.ToArray(/*new SpanQuery[clausesList.size()]*/));
                soq.Boost = DOMUtils.GetAttribute(e, "boost", 1.0f);
                return soq;
            }
            catch (Exception ioe) when (ioe.IsIOException())
            {
                throw new ParserException("IOException parsing value:" + value, ioe);
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(ts);
            }
        }
    }
}
