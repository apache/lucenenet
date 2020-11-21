using Lucene.Net.Analysis;
using Lucene.Net.Sandbox.Queries;
using Lucene.Net.Search;
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
    /// Builder for <see cref="FuzzyLikeThisQuery"/>
    /// </summary>
    public class FuzzyLikeThisQueryBuilder : IQueryBuilder
    {
        private const int DEFAULT_MAX_NUM_TERMS = 50;
#pragma warning disable 612, 618
        private const float DEFAULT_MIN_SIMILARITY = SlowFuzzyQuery.defaultMinSimilarity;
#pragma warning restore 612, 618
        private const int DEFAULT_PREFIX_LENGTH = 1;
        private const bool DEFAULT_IGNORE_TF = false;

        private readonly Analyzer analyzer;

        public FuzzyLikeThisQueryBuilder(Analyzer analyzer)
        {
            this.analyzer = analyzer;
        }

        public virtual Query GetQuery(XmlElement e)
        {
            XmlNodeList nl = e.GetElementsByTagName("Field");
            int maxNumTerms = DOMUtils.GetAttribute(e, "maxNumTerms", DEFAULT_MAX_NUM_TERMS);
            FuzzyLikeThisQuery fbq = new FuzzyLikeThisQuery(maxNumTerms, analyzer);
            fbq.IgnoreTF = DOMUtils.GetAttribute(e, "ignoreTF", DEFAULT_IGNORE_TF);

            for (int i = 0; i < nl.Count; i++)
            {
                XmlElement fieldElem = (XmlElement)nl.Item(i);
                float minSimilarity = DOMUtils.GetAttribute(fieldElem, "minSimilarity", DEFAULT_MIN_SIMILARITY);
                int prefixLength = DOMUtils.GetAttribute(fieldElem, "prefixLength", DEFAULT_PREFIX_LENGTH);
                string fieldName = DOMUtils.GetAttributeWithInheritance(fieldElem, "fieldName");

                string value = DOMUtils.GetText(fieldElem);
                fbq.AddTerms(value, fieldName, minSimilarity, prefixLength);
            }

            fbq.Boost = DOMUtils.GetAttribute(e, "boost", 1.0f);
            return fbq;
        }
    }
}
