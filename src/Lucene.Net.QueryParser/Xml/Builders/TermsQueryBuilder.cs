using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Xml;

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
    /// Builds a <see cref="BooleanQuery"/> from all of the terms found in the XML element using the choice of analyzer
    /// </summary>
    public class TermsQueryBuilder : IQueryBuilder
    {
        private readonly Analyzer analyzer;

        public TermsQueryBuilder(Analyzer analyzer)
        {
            this.analyzer = analyzer;
        }

        public virtual Query GetQuery(XmlElement e)
        {
            string fieldName = DOMUtils.GetAttributeWithInheritanceOrFail(e, "fieldName");
            string text = DOMUtils.GetNonBlankTextOrFail(e);

            BooleanQuery bq = new BooleanQuery(DOMUtils.GetAttribute(e, "disableCoord", false));
            bq.MinimumNumberShouldMatch = DOMUtils.GetAttribute(e, "minimumNumberShouldMatch", 0);
            TokenStream ts = null;
            try
            {
                ts = analyzer.GetTokenStream(fieldName, text);
                ITermToBytesRefAttribute termAtt = ts.AddAttribute<ITermToBytesRefAttribute>();
                Term term = null;
                BytesRef bytes = termAtt.BytesRef;
                ts.Reset();
                while (ts.IncrementToken())
                {
                    termAtt.FillBytesRef();
                    term = new Term(fieldName, BytesRef.DeepCopyOf(bytes));
                    bq.Add(new BooleanClause(new TermQuery(term), Occur.SHOULD));
                }
                ts.End();
            }
            catch (Exception ioe) when (ioe.IsIOException())
            {
                throw RuntimeException.Create("Error constructing terms from index:" + ioe, ioe);
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(ts);
            }

            bq.Boost = DOMUtils.GetAttribute(e, "boost", 1.0f);
            return bq;
        }
    }
}
