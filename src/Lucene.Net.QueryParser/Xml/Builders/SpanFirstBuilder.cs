using Lucene.Net.Search.Spans;
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
    /// Builder for <see cref="SpanFirstQuery"/>
    /// </summary>
    public class SpanFirstBuilder : SpanBuilderBase
    {
        private readonly ISpanQueryBuilder factory;

        public SpanFirstBuilder(ISpanQueryBuilder factory)
        {
            this.factory = factory;
        }

        public override SpanQuery GetSpanQuery(XmlElement e)
        {
            int end = DOMUtils.GetAttribute(e, "end", 1);
            XmlElement child = DOMUtils.GetFirstChildElement(e);
            SpanQuery q = factory.GetSpanQuery(child);

            SpanFirstQuery sfq = new SpanFirstQuery(q, end);

            sfq.Boost = DOMUtils.GetAttribute(e, "boost", 1.0f);
            return sfq;
        }
    }
}
