using Lucene.Net.Search;
using System;
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
    /// Builder for <see cref="BooleanQuery"/>
    /// </summary>
    public class BooleanQueryBuilder : IQueryBuilder
    {
        private readonly IQueryBuilder factory;

        public BooleanQueryBuilder(IQueryBuilder factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// (non-Javadoc)
        /// @see org.apache.lucene.xmlparser.QueryObjectBuilder#process(org.w3c.dom.Element)
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public virtual Query GetQuery(XmlElement e)
        {
            BooleanQuery bq = new BooleanQuery(DOMUtils.GetAttribute(e, "disableCoord", false));
            bq.MinimumNumberShouldMatch = DOMUtils.GetAttribute(e, "minimumNumberShouldMatch", 0);
            bq.Boost = DOMUtils.GetAttribute(e, "boost", 1.0f);

            XmlNodeList nl = e.ChildNodes;
            for (int i = 0; i < nl.Count; i++)
            {
                XmlNode node = nl.Item(i);
                if (node.LocalName.Equals("Clause", StringComparison.Ordinal))
                {
                    XmlElement clauseElem = (XmlElement)node;
                    Occur occurs = GetOccursValue(clauseElem);

                    XmlElement clauseQuery = DOMUtils.GetFirstChildOrFail(clauseElem);
                    Query q = factory.GetQuery(clauseQuery);
                    bq.Add(new BooleanClause(q, occurs));
                }
            }

            return bq;
        }

        internal static Occur GetOccursValue(XmlElement clauseElem)
        {
            string occs = clauseElem.GetAttribute("occurs");
            Occur occurs = Occur.SHOULD;
            if ("must".Equals(occs, StringComparison.OrdinalIgnoreCase))
            {
                occurs = Occur.MUST;
            }
            else
            {
                if ("mustNot".Equals(occs, StringComparison.OrdinalIgnoreCase))
                {
                    occurs = Occur.MUST_NOT;
                }
                else
                {
                    if (("should".Equals(occs, StringComparison.OrdinalIgnoreCase)) || ("".Equals(occs, StringComparison.Ordinal)))
                    {
                        occurs = Occur.SHOULD;
                    }
                    else
                    {
                        if (occs != null)
                        {
                            throw new ParserException("Invalid value for \"occurs\" attribute of clause:" + occs);
                        }
                    }
                }
            }
            return occurs;
        }
    }
}
