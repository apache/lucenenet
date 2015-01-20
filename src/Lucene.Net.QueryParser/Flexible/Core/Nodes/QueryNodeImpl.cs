/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using Lucene.Net.Queryparser.Flexible.Core.Messages;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Core.Parser;
using Lucene.Net.Queryparser.Flexible.Core.Util;
using Lucene.Net.Queryparser.Flexible.Messages;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Core.Nodes
{
	/// <summary>
	/// A
	/// <see cref="QueryNodeImpl">QueryNodeImpl</see>
	/// is the default implementation of the interface
	/// <see cref="QueryNode">QueryNode</see>
	/// </summary>
	public abstract class QueryNodeImpl : QueryNode, ICloneable
	{
		public static readonly string PLAINTEXT_FIELD_NAME = "_plain";

		private bool isLeaf = true;

		private Hashtable<string, object> tags = new Hashtable<string, object>();

		private IList<QueryNode> clauses = null;

		// TODO remove PLAINTEXT_FIELD_NAME replacing it with configuration APIs
		protected internal virtual void Allocate()
		{
			if (this.clauses == null)
			{
				this.clauses = new AList<QueryNode>();
			}
			else
			{
				this.clauses.Clear();
			}
		}

		public void Add(QueryNode child)
		{
			if (IsLeaf() || this.clauses == null || child == null)
			{
				throw new ArgumentException(NLS.GetLocalizedMessage(QueryParserMessages.NODE_ACTION_NOT_SUPPORTED
					));
			}
			this.clauses.AddItem(child);
			((QueryNodeImpl)child).SetParent(this);
		}

		public void Add(IList<QueryNode> children)
		{
			if (IsLeaf() || this.clauses == null)
			{
				throw new ArgumentException(NLS.GetLocalizedMessage(QueryParserMessages.NODE_ACTION_NOT_SUPPORTED
					));
			}
			foreach (QueryNode child in children)
			{
				Add(child);
			}
		}

		public virtual bool IsLeaf()
		{
			return this.isLeaf;
		}

		public void Set(IList<QueryNode> children)
		{
			if (IsLeaf() || this.clauses == null)
			{
				ResourceBundle bundle = ResourceBundle.GetBundle("Lucene.Net.queryParser.messages.QueryParserMessages"
					);
				string message = bundle.GetObject("Q0008E.NODE_ACTION_NOT_SUPPORTED").ToString();
				throw new ArgumentException(message);
			}
			// reset parent value
			foreach (QueryNode child in children)
			{
				child.RemoveFromParent();
			}
			AList<QueryNode> existingChildren = new AList<QueryNode>(GetChildren());
			foreach (QueryNode existingChild in existingChildren)
			{
				existingChild.RemoveFromParent();
			}
			// allocate new children list
			Allocate();
			// add new children and set parent
			Add(children);
		}

		/// <exception cref="Sharpen.CloneNotSupportedException"></exception>
		public virtual QueryNode CloneTree()
		{
			QueryNodeImpl clone = (QueryNodeImpl)base.Clone();
			clone.isLeaf = this.isLeaf;
			// Reset all tags
			clone.tags = new Hashtable<string, object>();
			// copy children
			if (this.clauses != null)
			{
				IList<QueryNode> localClauses = new AList<QueryNode>();
				foreach (QueryNode clause in this.clauses)
				{
					localClauses.AddItem(clause.CloneTree());
				}
				clone.clauses = localClauses;
			}
			return clone;
		}

		/// <exception cref="Sharpen.CloneNotSupportedException"></exception>
		public virtual QueryNode Clone()
		{
			return CloneTree();
		}

		protected internal virtual void SetLeaf(bool isLeaf)
		{
			this.isLeaf = isLeaf;
		}

		/// <returns>
		/// a List for QueryNode object. Returns null, for nodes that do not
		/// contain children. All leaf Nodes return null.
		/// </returns>
		public IList<QueryNode> GetChildren()
		{
			if (IsLeaf() || this.clauses == null)
			{
				return null;
			}
			return new AList<QueryNode>(this.clauses);
		}

		public virtual void SetTag(string tagName, object value)
		{
			this.tags.Put(tagName.ToLower(CultureInfo.ROOT), value);
		}

		public virtual void UnsetTag(string tagName)
		{
			this.tags.Remove(tagName.ToLower(CultureInfo.ROOT));
		}

		/// <summary>verify if a node contains a tag</summary>
		public virtual bool ContainsTag(string tagName)
		{
			return this.tags.ContainsKey(tagName.ToLower(CultureInfo.ROOT));
		}

		public virtual object GetTag(string tagName)
		{
			return this.tags[tagName.ToLower(CultureInfo.ROOT)];
		}

		private QueryNode parent = null;

		private void SetParent(QueryNode parent)
		{
			if (this.parent != parent)
			{
				this.RemoveFromParent();
				this.parent = parent;
			}
		}

		public virtual QueryNode GetParent()
		{
			return this.parent;
		}

		protected internal virtual bool IsRoot()
		{
			return GetParent() == null;
		}

		/// <summary>If set to true the the method toQueryString will not write field names</summary>
		protected internal bool toQueryStringIgnoreFields = false;

		/// <summary>This method is use toQueryString to detect if fld is the default field</summary>
		/// <param name="fld">- field name</param>
		/// <returns>true if fld is the default field</returns>
		protected internal virtual bool IsDefaultField(CharSequence fld)
		{
			// TODO: remove this method, it's commonly used by {@link
			// #toQueryString(Lucene.Net.queryParser.core.parser.EscapeQuerySyntax)}
			// to figure out what is the default field, however, {@link
			// #toQueryString(Lucene.Net.queryParser.core.parser.EscapeQuerySyntax)}
			// should receive the default field value directly by parameter
			if (this.toQueryStringIgnoreFields)
			{
				return true;
			}
			if (fld == null)
			{
				return true;
			}
			if (QueryNodeImpl.PLAINTEXT_FIELD_NAME.Equals(StringUtils.ToString(fld)))
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// Every implementation of this class should return pseudo xml like this:
		/// For FieldQueryNode: &lt;field start='1' end='2' field='subject' text='foo'/&gt;
		/// </summary>
		/// <seealso cref="QueryNode.ToString()">QueryNode.ToString()</seealso>
		public override string ToString()
		{
			return base.ToString();
		}

		/// <summary>Returns a map containing all tags attached to this query node.</summary>
		/// <remarks>Returns a map containing all tags attached to this query node.</remarks>
		/// <returns>a map containing all tags attached to this query node</returns>
		public virtual IDictionary<string, object> GetTagMap()
		{
			return (IDictionary<string, object>)this.tags.Clone();
		}

		public virtual void RemoveFromParent()
		{
			if (this.parent != null)
			{
				IList<QueryNode> parentChildren = this.parent.GetChildren();
				Iterator<QueryNode> it = parentChildren.Iterator();
				while (it.HasNext())
				{
					if (it.Next() == this)
					{
						it.Remove();
					}
				}
				this.parent = null;
			}
		}

		public abstract CharSequence ToQueryString(EscapeQuerySyntax arg1);
	}
}
