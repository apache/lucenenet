using Lucene.Net.Queries;
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
    /// Builder for <see cref="BoostingQuery"/>
    /// </summary>
    public class BoostingQueryBuilder : IQueryBuilder
    {
        private const float DEFAULT_BOOST = 0.01f;

        private readonly IQueryBuilder factory;

        public BoostingQueryBuilder(IQueryBuilder factory)
        {
            this.factory = factory;
        }

        public virtual Query GetQuery(XmlElement e)
        {
            XmlElement mainQueryElem = DOMUtils.GetChildByTagOrFail(e, "Query");
            mainQueryElem = DOMUtils.GetFirstChildOrFail(mainQueryElem);
            Query mainQuery = factory.GetQuery(mainQueryElem);

            XmlElement boostQueryElem = DOMUtils.GetChildByTagOrFail(e, "BoostQuery");
            float boost = DOMUtils.GetAttribute(boostQueryElem, "boost", DEFAULT_BOOST);
            boostQueryElem = DOMUtils.GetFirstChildOrFail(boostQueryElem);
            Query boostQuery = factory.GetQuery(boostQueryElem);

            BoostingQuery bq = new BoostingQuery(mainQuery, boostQuery, boost);

            bq.Boost = DOMUtils.GetAttribute(e, "boost", 1.0f);
            return bq;
        }
    }
}
