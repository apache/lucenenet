using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Queries;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
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
    /// Builder for <see cref="TermsFilter"/>
    /// </summary>
    public class TermsFilterBuilder : IFilterBuilder
    {
        private readonly Analyzer analyzer;

        public TermsFilterBuilder(Analyzer analyzer)
        {
            this.analyzer = analyzer;
        }

        /// <summary>
        /// (non-Javadoc)
        /// @see org.apache.lucene.xmlparser.FilterBuilder#process(org.w3c.dom.Element)
        /// </summary>
        public virtual Filter GetFilter(XmlElement e)
        {
            IList<BytesRef> terms = new JCG.List<BytesRef>();
            string text = DOMUtils.GetNonBlankTextOrFail(e);
            string fieldName = DOMUtils.GetAttributeWithInheritanceOrFail(e, "fieldName");

            TokenStream ts = null;
            try
            {
                ts = analyzer.GetTokenStream(fieldName, text);
                ITermToBytesRefAttribute termAtt = ts.AddAttribute<ITermToBytesRefAttribute>();
                BytesRef bytes = termAtt.BytesRef;
                ts.Reset();
                while (ts.IncrementToken())
                {
                    termAtt.FillBytesRef();
                    terms.Add(BytesRef.DeepCopyOf(bytes));
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
            return new TermsFilter(fieldName, terms);
        }
    }
}
