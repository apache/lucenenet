using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.QueryParsers.Flexible.Core.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using JCG = J2N.Collections.Generic;

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
    /// A <see cref="QueryNode"/> is the default implementation of the interface
    /// <see cref="IQueryNode"/>
    /// </summary>
    public abstract class QueryNode : IQueryNode // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        /// <summary>
        /// index default field
        /// </summary>
        // TODO remove PLAINTEXT_FIELD_NAME replacing it with configuration APIs
        public static readonly string PLAINTEXT_FIELD_NAME = "_plain";

        private bool isLeaf = true;

        private Dictionary<string, object> tags = new Dictionary<string, object>();

        private JCG.List<IQueryNode> clauses = null;

        protected virtual void Allocate()
        {
            if (this.clauses is null)
            {
                this.clauses = new JCG.List<IQueryNode>();
            }
            else
            {
                this.clauses.Clear();
            }
        }

        public void Add(IQueryNode child)
        {
            if (IsLeaf || this.clauses is null || child is null)
            {
                // LUCENENET: Factored out NLS/Message/IMessage so end users can optionally utilize the built-in .NET localization.
                throw new ArgumentException(
                    QueryParserMessages.NODE_ACTION_NOT_SUPPORTED);
            }

            this.clauses.Add(child);
            ((QueryNode)child).SetParent(this);
        }

        public void Add(IList<IQueryNode> children)
        {
            if (IsLeaf || this.clauses is null)
            {
                // LUCENENET: Factored out NLS/Message/IMessage so end users can optionally utilize the built-in .NET localization.
                throw new ArgumentException(
                    QueryParserMessages.NODE_ACTION_NOT_SUPPORTED);
            }

            foreach (IQueryNode child in children)
            {
                Add(child);
            }
        }

        public virtual bool IsLeaf
        {
            get => this.isLeaf;
            protected set => this.isLeaf = value;
        }

        public void Set(IList<IQueryNode> children)
        {
            if (IsLeaf || this.clauses is null)
            {
                // LUCENENET: Factored out NLS/Message/IMessage so end users can optionally utilize the built-in .NET localization.
                throw new ArgumentException(QueryParserMessages.NODE_ACTION_NOT_SUPPORTED);
            }

            // reset parent value
            foreach (IQueryNode child in children)
            {
                child.RemoveFromParent();
            }

            // LUCENENET specific: GetChildren already creates a new list, there is
            // no need to do it again here and have another O(n) operation
            IList<IQueryNode> existingChildren = GetChildren();
            foreach (IQueryNode existingChild in existingChildren)
            {
                existingChild.RemoveFromParent();
            }

            // allocate new children list
            Allocate();

            // add new children and set parent
            Add(children);
        }

        public virtual IQueryNode CloneTree()
        {
            QueryNode clone = (QueryNode)this.MemberwiseClone();
            clone.isLeaf = this.isLeaf;

            // Reset all tags
            clone.tags = new Dictionary<string, object>();

            // copy children
            if (this.clauses != null)
            {
                JCG.List<IQueryNode> localClauses = new JCG.List<IQueryNode>();
                foreach (IQueryNode clause in this.clauses)
                {
                    localClauses.Add(clause.CloneTree());
                }
                clone.clauses = localClauses;
            }

            return clone;
        }

        public virtual object Clone()
        {
            return CloneTree();
        }

        /// <summary>
        /// a List for QueryNode object. Returns null, for nodes that do not
        /// contain children. All leaf Nodes return null.
        /// </summary>
        public IList<IQueryNode> GetChildren()
        {
            if (IsLeaf || this.clauses is null)
            {
                return null;
            }
            return new JCG.List<IQueryNode>(this.clauses);
        }

        public virtual void SetTag(string tagName, object value)
        {
            this.tags[CultureInfo.InvariantCulture.TextInfo.ToLower(tagName)] = value;
        }

        public virtual void UnsetTag(string tagName)
        {
            this.tags.Remove(CultureInfo.InvariantCulture.TextInfo.ToLower(tagName));
        }

        /// <summary>
        /// verify if a node contains a tag
        /// </summary>
        public virtual bool ContainsTag(string tagName)
        {
            return this.tags.ContainsKey(CultureInfo.InvariantCulture.TextInfo.ToLower(tagName));
        }

        public virtual object GetTag(string tagName)
        {
            return this.tags[CultureInfo.InvariantCulture.TextInfo.ToLower(tagName)];
        }

        /// <inheritdoc/>
        public virtual bool TryGetTag(string tagName, out object tag)
        {
            return this.tags.TryGetValue(tagName, out tag);
        }

        private IQueryNode parent = null;

        private void SetParent(IQueryNode parent)
        {
            if (this.parent != parent)
            {
                this.RemoveFromParent();
                this.parent = parent;
            }
        }

        public virtual IQueryNode Parent => this.parent;

        protected virtual bool IsRoot => Parent is null;

        /// <summary>
        /// If set to true the the method toQueryString will not write field names
        /// </summary>
        protected internal bool m_toQueryStringIgnoreFields = false;

        /// <summary>
        /// This method is use toQueryString to detect if fld is the default field
        /// </summary>
        /// <param name="fld">field name</param>
        /// <returns>true if fld is the default field</returns>
        // TODO: remove this method, it's commonly used by 
        // <see cref="ToQueryString(IEscapeQuerySyntax)"/>
        // to figure out what is the default field, however, 
        // <see cref="ToQueryString(IEscapeQuerySyntax)"/>
        // should receive the default field value directly by parameter
        protected virtual bool IsDefaultField(string fld)
        {
            if (this.m_toQueryStringIgnoreFields)
                return true;
            if (fld is null)
                return true;
            if (QueryNode.PLAINTEXT_FIELD_NAME.Equals(StringUtils.ToString(fld), StringComparison.Ordinal))
                return true;
            return false;
        }

        /// <summary>
        /// Every implementation of this class should return pseudo xml like this:
        /// 
        /// For FieldQueryNode: &lt;field start='1' end='2' field='subject' text='foo'/&gt;
        /// </summary>
        /// <seealso cref="IQueryNode.ToString()"/>
        public override string ToString()
        {
            return base.ToString();
        }

        /// <summary>
        /// Gets a map containing all tags attached to this query node.
        /// </summary>
        public virtual IDictionary<string, object> TagMap => new Dictionary<string, object>(this.tags);

        // LUCENENET NOTE: There was a bug in 4.8.1 here because parent.GetChildren() returns a copy of the
        // children, so removing items from it is pointless. We therefore diverge to the version 8.8.1 source of Lucene
        // from this point. Not sure when this patch was applied.

        public virtual void RemoveChildren(IQueryNode childNode)
        {
            // LUCENENET: Use RemoveAll() method for optimal performance.
            clauses.RemoveAll((value) => value == childNode);
            childNode.RemoveFromParent();
        }

        public virtual void RemoveFromParent()
        {
            if (this.parent != null)
            {
                IQueryNode parent = this.parent;
                this.parent = null;
                parent.RemoveChildren(this);
            }
        }

        // LUCENENET specific - class must implement all members of IQueryNode
        public abstract string ToQueryString(IEscapeQuerySyntax escapeSyntaxParser);
    }
}
