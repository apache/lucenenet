using Lucene.Net.Search.Spans;
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
    /// Builder for <see cref="SpanOrQuery"/>
    /// </summary>
    public class SpanOrBuilder : SpanBuilderBase
    {
        private readonly ISpanQueryBuilder factory;

        public SpanOrBuilder(ISpanQueryBuilder factory)
        {
            this.factory = factory;
        }

        public override SpanQuery GetSpanQuery(XmlElement e)
        {
            JCG.List<SpanQuery> clausesList = new JCG.List<SpanQuery>();
            for (XmlNode kid = e.FirstChild; kid != null; kid = kid.NextSibling)
            {
                if (kid.NodeType == XmlNodeType.Element)
                {
                    SpanQuery clause = factory.GetSpanQuery((XmlElement)kid);
                    clausesList.Add(clause);
                }
            }
            SpanQuery[] clauses = clausesList.ToArray(/*new SpanQuery[clausesList.size()]*/);
            SpanOrQuery soq = new SpanOrQuery(clauses);
            soq.Boost = DOMUtils.GetAttribute(e, "boost", 1.0f);
            return soq;
        }
    }
}
