using Lucene.Net.Search;
using System.Collections.Generic;
using System.Xml;

namespace Lucene.Net.QueryParsers.Xml
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
    /// Factory for <see cref="IFilterBuilder"/>
    /// </summary>
    public class FilterBuilderFactory : IFilterBuilder
    {
        IDictionary<string, IFilterBuilder> builders = new Dictionary<string, IFilterBuilder>();

        public virtual Filter GetFilter(XmlElement n)
        {
            IFilterBuilder builder;
            if (!builders.TryGetValue(n.Name, out builder) || builder == null)
            {
                throw new ParserException("No FilterBuilder defined for node " + n.Name);
            }
            return builder.GetFilter(n);
        }

        public virtual void AddBuilder(string nodeName, IFilterBuilder builder)
        {
            builders[nodeName] = builder;
        }

        public virtual IFilterBuilder GetFilterBuilder(string nodeName)
        {
            IFilterBuilder result;
            builders.TryGetValue(nodeName, out result);
            return result;
        }
    }
}
