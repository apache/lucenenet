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
    /// Builder for <see cref="FilteredQuery"/>
    /// </summary>
    public class FilteredQueryBuilder : IQueryBuilder
    {
        private readonly IFilterBuilder filterFactory;
        private readonly IQueryBuilder queryFactory;

        public FilteredQueryBuilder(IFilterBuilder filterFactory, IQueryBuilder queryFactory)
        {
            this.filterFactory = filterFactory;
            this.queryFactory = queryFactory;

        }

        /// <summary>
        /// (non-Javadoc)
        /// @see org.apache.lucene.xmlparser.QueryObjectBuilder#process(org.w3c.dom.Element)
        /// </summary>
        public virtual Query GetQuery(XmlElement e)
        {
            XmlElement filterElement = DOMUtils.GetChildByTagOrFail(e, "Filter");
            filterElement = DOMUtils.GetFirstChildOrFail(filterElement);
            Filter f = filterFactory.GetFilter(filterElement);

            XmlElement queryElement = DOMUtils.GetChildByTagOrFail(e, "Query");
            queryElement = DOMUtils.GetFirstChildOrFail(queryElement);
            Query q = queryFactory.GetQuery(queryElement);

            FilteredQuery fq = new FilteredQuery(q, f);
            fq.Boost = DOMUtils.GetAttribute(e, "boost", 1.0f);
            return fq;
        }
    }
}
