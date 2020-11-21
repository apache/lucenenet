using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using System.Collections.Generic;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Nodes
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
    /// A <see cref="StandardBooleanQueryNode"/> has the same behavior as
    /// <see cref="BooleanQueryNode"/>. It only indicates if the coord should be enabled or
    /// not for this boolean query. 
    /// </summary>
    /// <seealso cref="Search.Similarities.Similarity.Coord(int, int)"/>
    /// <seealso cref="Search.BooleanQuery"/>
    public class StandardBooleanQueryNode : BooleanQueryNode
    {
        private readonly bool disableCoord; // LUCENENET: marked readonly

        public StandardBooleanQueryNode(IList<IQueryNode> clauses, bool disableCoord)
            : base(clauses)
        {
            this.disableCoord = disableCoord;
        }

        public virtual bool DisableCoord => this.disableCoord;
    }
}
