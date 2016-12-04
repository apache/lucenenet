using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using System.Collections.Generic;

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
    /// A {@link QueryNode} is a interface implemented by all nodes on a QueryNode
    /// tree.
    /// </summary>
    public interface IQueryNode
    {
        /** convert to a query string understood by the query parser */
        // TODO: this interface might be changed in the future
        string ToQueryString(IEscapeQuerySyntax escapeSyntaxParser);

        /** for printing */
        string ToString();

        /** get Children nodes */
        IList<IQueryNode> GetChildren();

        /** verify if a node is a Leaf node */
        bool IsLeaf { get; }

        /** verify if a node contains a tag */
        bool ContainsTag(string tagName);

        /**
         * Returns object stored under that tag name
         */
        object GetTag(string tagName);

        IQueryNode Parent { get; }

        /**
         * Recursive clone the QueryNode tree The tags are not copied to the new tree
         * when you call the cloneTree() method
         * 
         * @return the cloned tree
         */
        IQueryNode CloneTree();

        // Below are the methods that can change state of a QueryNode
        // Write Operations (not Thread Safe)

        // add a new child to a non Leaf node
        void Add(IQueryNode child);

        void Add(IList<IQueryNode> children);

        // reset the children of a node
        void Set(IList<IQueryNode> children);

        /**
         * Associate the specified value with the specified tagName. If the tagName
         * already exists, the old value is replaced. The tagName and value cannot be
         * null. tagName will be converted to lowercase.
         */
        void SetTag(string tagName, object value);

        /**
         * Unset a tag. tagName will be converted to lowercase.
         */
        void UnsetTag(string tagName);

        /**
         * Returns a map containing all tags attached to this query node. 
         * 
         * @return a map containing all tags attached to this query node
         */
        IDictionary<string, object> TagMap { get; }

        /**
         * Removes this query node from its parent.
         */
        void RemoveFromParent();
    }
}
