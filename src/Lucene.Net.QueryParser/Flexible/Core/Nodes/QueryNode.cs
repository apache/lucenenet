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
    /// A <see cref="IQueryNode"/> is a interface implemented by all nodes on a <see cref="IQueryNode"/>
    /// tree.
    /// </summary>
    public interface IQueryNode
    {
        /// <summary>
        /// convert to a query string understood by the query parser
        /// </summary>
        // TODO: this interface might be changed in the future
        string ToQueryString(IEscapeQuerySyntax escapeSyntaxParser);

        /// <summary>
        /// for printing
        /// </summary>
        string ToString();

        /// <summary>
        /// get Children nodes
        /// </summary>
        IList<IQueryNode> GetChildren();

        /// <summary>
        /// verify if a node is a Leaf node
        /// </summary>
        bool IsLeaf { get; }

        /// <summary>
        /// verify if a node contains a tag
        /// </summary>
        bool ContainsTag(string tagName);

        /// <summary>
        /// Returns object stored under that tag name
        /// </summary>
        object GetTag(string tagName);

        /// <summary>
        /// Gets the tag associated with the specified tagName.
        /// </summary>
        bool TryGetTag(string tagName, out object tag);

        IQueryNode Parent { get; }

        /// <summary>
        /// Recursive clone the <see cref="IQueryNode"/> tree. The tags are not copied to the new tree
        /// when you call the <see cref="CloneTree()"/> method.
        /// </summary>
        /// <returns>the cloned tree</returns>
        IQueryNode CloneTree();

        // Below are the methods that can change state of a QueryNode
        // Write Operations (not Thread Safe)

        /// <summary>
        /// add a new child to a non Leaf node
        /// </summary>
        void Add(IQueryNode child);

        void Add(IList<IQueryNode> children);

        /// <summary>
        /// reset the children of a node
        /// </summary>
        void Set(IList<IQueryNode> children);

        /// <summary>
        /// Associate the specified value with the specified <paramref name="tagName"/>. If the <paramref name="tagName"/>
        /// already exists, the old value is replaced. The <paramref name="tagName"/> and <paramref name="value"/> cannot be
        /// null. <paramref name="tagName"/> will be converted to lowercase.
        /// </summary>
        void SetTag(string tagName, object value);

        /// <summary>
        /// Unset a tag. <paramref name="tagName"/> will be converted to lowercase.
        /// </summary>
        void UnsetTag(string tagName);

        /// <summary>
        /// Gets a map containing all tags attached to this query node. 
        /// </summary>
        IDictionary<string, object> TagMap { get; }

        /// <summary>
        /// Removes this query node from its parent.
        /// </summary>
        void RemoveFromParent();

        // LUCENENET: From Lucene 8.8.1, patch to broken RemoveFromParent() behavior
        /// <summary>
        /// Remove a child node.
        /// </summary>
        /// <param name="childNode">Which child to remove.</param>
        void RemoveChildren(IQueryNode childNode);
    }
}
