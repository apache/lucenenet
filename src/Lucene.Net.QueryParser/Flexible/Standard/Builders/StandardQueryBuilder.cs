using Lucene.Net.QueryParsers.Flexible.Core.Builders;
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
    /// This interface should be implemented by every class that wants to build
    /// <see cref="Query"/> objects from <see cref="Core.Nodes.IQueryNode"/> objects. 
    /// </summary>
    /// <seealso cref="IQueryBuilder{TQuery}"/>
    /// <seealso cref="QueryTreeBuilder"/>
    public interface IStandardQueryBuilder : IQueryBuilder<Query>
    {
        // LUCENENET specific - we don't need to redeclare Build here because
        // it already exists in the now generic IQueryBuilder
        //Query Build(IQueryNode queryNode);
    }
}
