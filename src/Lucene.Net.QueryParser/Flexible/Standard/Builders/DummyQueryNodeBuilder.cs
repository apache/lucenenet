using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.Search;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Builders
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
    /// This builder does nothing. Commonly used for <see cref="IQueryNode"/> objects that
    /// are built by its parent's builder.
    /// </summary>
    /// <seealso cref="IStandardQueryBuilder"/>
    /// <seealso cref="Core.Builders.QueryTreeBuilder{TQuery}"/>
    public class DummyQueryNodeBuilder : IStandardQueryBuilder
    {
        /// <summary>
        /// Constructs a <see cref="DummyQueryNodeBuilder"/> object.
        /// </summary>
        public DummyQueryNodeBuilder()
        {
            // empty constructor
        }

        /// <summary>
        /// Always return <c>null</c>.
        /// </summary>
        /// <param name="queryNode"></param>
        /// <returns><c>null</c></returns>
        public virtual Query Build(IQueryNode queryNode)
        {
            return null;
        }
    }
}
