using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.QueryParsers.Flexible.Core.Util;
using Lucene.Net.QueryParsers.Flexible.Messages;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    public abstract class QueryNode : IQueryNode, ICloneable
    {
        /* index default field */
        // TODO remove PLAINTEXT_FIELD_NAME replacing it with configuration APIs
        public const string PLAINTEXT_FIELD_NAME = "_plain";

        private bool isLeaf = true;

        private HashMap<String, Object> tags = new HashMap<String, Object>();

        private IList<IQueryNode> clauses = null;

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
            if (this.IsLeaf || this.clauses == null || child == null)
            {
                throw new ArgumentException(NLS.GetLocalizedMessage(QueryParserMessages.NODE_ACTION_NOT_SUPPORTED));
            }

            this.clauses.Add(child);
            ((QueryNode)child).Parent = this;
        }

        public void Add(IList<IQueryNode> children)
        {
            if (this.IsLeaf || this.clauses == null)
            {
                throw new ArgumentException(NLS.GetLocalizedMessage(QueryParserMessages.NODE_ACTION_NOT_SUPPORTED));
            }

            foreach (IQueryNode child in children)
            {
                Add(child);
            }
        }

        public bool IsLeaf
        {
            get
            {
                return this.isLeaf;
            }
        }

        public void Set(IList<IQueryNode> children)
        {
            if (this.IsLeaf || this.clauses == null)
            {
                var bundle = new ResourceManager(typeof(QueryParserMessages));
                string message = bundle.GetObject("Q0008E.NODE_ACTION_NOT_SUPPORTED").ToString();

                throw new ArgumentException(message);
            }

            // reset parent value
            foreach (IQueryNode child in children)
            {
                ((QueryNode)child).Parent = null;
            }

            // allocate new children list
            Allocate();

            // add new children and set parent
            foreach (IQueryNode child in children)
            {
                Add(child);
            }
        }

        public virtual IQueryNode CloneTree()
        {
            QueryNode clone = (QueryNode)base.MemberwiseClone();
            clone.isLeaf = this.isLeaf;

            // Reset all tags
            clone.tags = new HashMap<String, Object>();

            // copy children
            if (this.clauses != null)
            {
                IList<IQueryNode> localClauses = new List<IQueryNode>();
                foreach (QueryNode clause in this.clauses)
                {
                    localClauses.Add(clause.CloneTree());
                }
                clone.clauses = localClauses;
            }

            return clone;
        }

        public object Clone()
        {
            return CloneTree();
        }

        protected void SetLeaf(bool isLeaf)
        {
            this.isLeaf = isLeaf;
        }

        public IList<IQueryNode> Children
        {
            get
            {
                if (IsLeaf || this.clauses == null)
                {
                    return null;
                }
                return this.clauses;
            }
        }

        public void SetTag(string tagName, object value)
        {
            this.tags[tagName.ToLowerInvariant()] = value;
        }

        public void UnsetTag(string tagName)
        {
            this.tags.Remove(tagName.ToLowerInvariant());
        }

        public bool ContainsTag(string tagName)
        {
            return this.tags.ContainsKey(tagName.ToLowerInvariant());
        }

        public object GetTag(string tagName)
        {
            return this.tags[tagName.ToLowerInvariant()];
        }

        private IQueryNode parent = null;
        
        public IQueryNode Parent
        {
            get { return this.parent; }
            private set
            {
                this.parent = value;
            }
        }

        protected bool IsRoot
        {
            get { return this.Parent == null; }
        }

        protected internal bool toQueryStringIgnoreFields = false;

        protected bool IsDefaultField(ICharSequence fld)
        {
            if (this.toQueryStringIgnoreFields)
                return true;
            if (fld == null)
                return true;
            if (fld.Equals(QueryNode.PLAINTEXT_FIELD_NAME))
                return true;
            return false;
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public abstract ICharSequence ToQueryString(IEscapeQuerySyntax escapeSyntaxParser);
        
        public IDictionary<string, object> TagMap
        {
            get { return new HashMap<string, object>(this.tags); }
        }
    }
}
