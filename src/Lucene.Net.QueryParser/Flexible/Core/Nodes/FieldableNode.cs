namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
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
    /// A query node implements <see cref="IFieldableNode"/> interface to indicate that its
    /// children and itself are associated to a specific field.
    /// 
    /// If it has any children which also implements this interface, it must ensure
    /// the children are associated to the same field.
    /// </summary>
    public interface IFieldableNode : IQueryNode
    {
        /// <summary>
        /// Gets or Sets the field name associated to the node and every node under it.
        /// </summary>
        string Field { get; set; }
    }
}
