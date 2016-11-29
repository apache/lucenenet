using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.QueryParsers.Flexible.Core.Util;
using Lucene.Net.QueryParsers.Flexible.Messages;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    /// <summary>
    /// A {@link QueryNodeImpl} is the default implementation of the interface
    /// {@link QueryNode}
    /// </summary>
    public abstract class QueryNodeImpl : IQueryNode, ICloneable
    {
        /* index default field */
        // TODO remove PLAINTEXT_FIELD_NAME replacing it with configuration APIs
        public static readonly string PLAINTEXT_FIELD_NAME = "_plain";

        private bool isLeaf = true;

        private Dictionary<string, object> tags = new Dictionary<string, object>();

        private List<IQueryNode> clauses = null;

        protected virtual void Allocate()
        {

            if (this.clauses == null)
            {
                this.clauses = new List<IQueryNode>();

            }
            else
            {
                this.clauses.Clear();
            }

        }

        public void Add(IQueryNode child)
        {

            if (IsLeaf() || this.clauses == null || child == null)
            {
                throw new ArgumentException(NLS
                    .GetLocalizedMessage(QueryParserMessages.NODE_ACTION_NOT_SUPPORTED));
            }

            this.clauses.Add(child);
            ((QueryNodeImpl)child).SetParent(this);

        }


        public void Add(IList<IQueryNode> children)
        {

            if (IsLeaf() || this.clauses == null)
            {
                throw new ArgumentException(NLS
                    .GetLocalizedMessage(QueryParserMessages.NODE_ACTION_NOT_SUPPORTED));
            }

            foreach (IQueryNode child in children)
            {
                Add(child);
            }

        }


        public virtual bool IsLeaf()
        {
            return this.isLeaf;
        }


        public void Set(IList<IQueryNode> children)
        {

            if (IsLeaf() || this.clauses == null)
            {
                //ResourceBundle bundle = ResourceBundle
                //    .getBundle("org.apache.lucene.queryParser.messages.QueryParserMessages");
                //String message = bundle.getObject("Q0008E.NODE_ACTION_NOT_SUPPORTED")
                //    .toString();
                string message = Lucene.Net.QueryParsers.Properties.Resources.NODE_ACTION_NOT_SUPPORTED;

                throw new ArgumentException(message);

            }

            // reset parent value
            foreach (IQueryNode child in children)
            {
                child.RemoveFromParent();
            }

            List<IQueryNode> existingChildren = new List<IQueryNode>(GetChildren());
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
            QueryNodeImpl clone = (QueryNodeImpl)this.MemberwiseClone();
            clone.isLeaf = this.isLeaf;

            // Reset all tags
            clone.tags = new Dictionary<string, object>();

            // copy children
            if (this.clauses != null)
            {
                List<IQueryNode> localClauses = new List<IQueryNode>();
                foreach (IQueryNode clause in this.clauses)
                {
                    localClauses.Add(clause.CloneTree());
                }
                clone.clauses = localClauses;
            }

            return clone;
        }


        public virtual /*IQueryNode*/ object Clone()
        {
            return CloneTree();
        }

        protected virtual void SetLeaf(bool isLeaf)
        {
            this.isLeaf = isLeaf;
        }

        /**
         * @return a List for QueryNode object. Returns null, for nodes that do not
         *         contain children. All leaf Nodes return null.
         */

        public IList<IQueryNode> GetChildren()
        {
            if (IsLeaf() || this.clauses == null)
            {
                return null;
            }
            return new List<IQueryNode>(this.clauses);
        }


        public virtual void SetTag(string tagName, object value)
        {
            this.tags[tagName.ToLower(CultureInfo.InvariantCulture)] = value;
        }


        public virtual void UnsetTag(string tagName)
        {
            this.tags.Remove(tagName.ToLower(CultureInfo.InvariantCulture));
        }

        /** verify if a node contains a tag */
        public virtual bool ContainsTag(string tagName)
        {
            return this.tags.ContainsKey(tagName.ToLower(CultureInfo.InvariantCulture));
        }

        public virtual object GetTag(string tagName)
        {
            return this.tags[tagName.ToLower(CultureInfo.InvariantCulture)];
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


        public virtual IQueryNode GetParent()
        {
            return this.parent;
        }

        protected virtual bool IsRoot()
        {
            return GetParent() == null;
        }

        /**
         * If set to true the the method toQueryString will not write field names
         */
        protected internal bool toQueryStringIgnoreFields = false;

        /**
         * This method is use toQueryString to detect if fld is the default field
         * 
         * @param fld - field name
         * @return true if fld is the default field
         */
        // TODO: remove this method, it's commonly used by {@link
        // #toQueryString(org.apache.lucene.queryParser.core.parser.EscapeQuerySyntax)}
        // to figure out what is the default field, however, {@link
        // #toQueryString(org.apache.lucene.queryParser.core.parser.EscapeQuerySyntax)}
        // should receive the default field value directly by parameter
        protected virtual bool IsDefaultField(string fld)
        {
            if (this.toQueryStringIgnoreFields)
                return true;
            if (fld == null)
                return true;
            if (QueryNodeImpl.PLAINTEXT_FIELD_NAME.Equals(StringUtils.ToString(fld)))
                return true;
            return false;
        }

        /**
         * Every implementation of this class should return pseudo xml like this:
         * 
         * For FieldQueryNode: &lt;field start='1' end='2' field='subject' text='foo'/&gt;
         * 
         * @see org.apache.lucene.queryparser.flexible.core.nodes.QueryNode#toString()
         */

        public override string ToString()
        {
            return base.ToString();
        }

        /**
         * Returns a map containing all tags attached to this query node.
         * 
         * @return a map containing all tags attached to this query node
         */
        public virtual IDictionary<string, object> GetTagMap()
        {
            return new Dictionary<string, object>(this.tags);
        }

        public virtual void RemoveFromParent()
        {
            if (this.parent != null)
            {
                IList<IQueryNode> parentChildren = this.parent.GetChildren();
                //IEnumerator<IQueryNode> it = parentChildren.GetEnumerator();

                //while (it.MoveNext())
                //{
                //    if (it.Current == this)
                //    {
                //        it.Remove();
                //    }
                //}

                // LUCENENET NOTE: Loop in reverse so we can remove items
                // without screwing up our iterator.
                for (int i = parentChildren.Count - 1; i >= 0; i--)
                {
                    if (parentChildren[i] == this)
                    {
                        parentChildren.RemoveAt(i);
                    }
                }

                this.parent = null;
            }
        }

        // LUCENENET specific - class must implement all members of IQueryNode
        public abstract string ToQueryString(IEscapeQuerySyntax escapeSyntaxParser);
    }
}
