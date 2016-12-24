using Lucene.Net.Queries;
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
    /// Builder for <see cref="BooleanFilter"/>
    /// </summary>
    public class BooleanFilterBuilder : IFilterBuilder
    {
        private readonly IFilterBuilder factory;

        public BooleanFilterBuilder(IFilterBuilder factory)
        {
            this.factory = factory;
        }

        public virtual Filter GetFilter(XmlElement e)
        {
            BooleanFilter bf = new BooleanFilter();
            XmlNodeList nl = e.ChildNodes;

            for (int i = 0; i < nl.Count; i++)
            {
                XmlNode node = nl.Item(i);
                if (node.LocalName.Equals("Clause", StringComparison.Ordinal))
                {
                    XmlElement clauseElem = (XmlElement)node;
                    Occur occurs = BooleanQueryBuilder.GetOccursValue(clauseElem);

                    XmlElement clauseFilter = DOMUtils.GetFirstChildOrFail(clauseElem);
                    Filter f = factory.GetFilter(clauseFilter);
                    bf.Add(new FilterClause(f, occurs));
                }
            }

            return bf;
        }
    }
}
