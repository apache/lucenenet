using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using System.Collections.Generic;
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
    /// Factory for <see cref="ISpanQueryBuilder"/>s
    /// </summary>
    public class SpanQueryBuilderFactory : ISpanQueryBuilder
    {
        private readonly IDictionary<string, ISpanQueryBuilder> builders = new Dictionary<string, ISpanQueryBuilder>();

        public virtual Query GetQuery(XmlElement e)
        {
            return GetSpanQuery(e);
        }

        public virtual void AddBuilder(string nodeName, ISpanQueryBuilder builder)
        {
            builders[nodeName] = builder;
        }

        public virtual SpanQuery GetSpanQuery(XmlElement e)
        {
            if (!builders.TryGetValue(e.Name, out ISpanQueryBuilder builder) || builder is null)
            {
                throw new ParserException("No SpanQueryObjectBuilder defined for node " + e.Name);
            }
            return builder.GetSpanQuery(e);
        }
    }
}
