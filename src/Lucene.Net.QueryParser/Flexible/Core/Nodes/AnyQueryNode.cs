/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.Text;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Core.Parser;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Core.Nodes
{
	/// <summary>
	/// A
	/// <see cref="AnyQueryNode">AnyQueryNode</see>
	/// represents an ANY operator performed on a list of
	/// nodes.
	/// </summary>
	public class AnyQueryNode : AndQueryNode
	{
		private CharSequence field = null;

		private int minimumMatchingmElements = 0;

		/// <param name="clauses">- the query nodes to be or'ed</param>
		public AnyQueryNode(IList<QueryNode> clauses, CharSequence field, int minimumMatchingElements
			) : base(clauses)
		{
			this.field = field;
			this.minimumMatchingmElements = minimumMatchingElements;
			if (clauses != null)
			{
				foreach (QueryNode clause in clauses)
				{
					if (clause is FieldQueryNode)
					{
						if (clause is QueryNodeImpl)
						{
							((QueryNodeImpl)clause).toQueryStringIgnoreFields = true;
						}
						if (clause is FieldableNode)
						{
							((FieldableNode)clause).SetField(field);
						}
					}
				}
			}
		}

		public virtual int GetMinimumMatchingElements()
		{
			return this.minimumMatchingmElements;
		}

		/// <summary>returns null if the field was not specified</summary>
		/// <returns>the field</returns>
		public virtual CharSequence GetField()
		{
			return this.field;
		}

		/// <summary>returns - null if the field was not specified</summary>
		/// <returns>the field as a String</returns>
		public virtual string GetFieldAsString()
		{
			if (this.field == null)
			{
				return null;
			}
			else
			{
				return this.field.ToString();
			}
		}

		/// <param name="field">- the field to set</param>
		public virtual void SetField(CharSequence field)
		{
			this.field = field;
		}

		/// <exception cref="Sharpen.CloneNotSupportedException"></exception>
		public override QueryNode CloneTree()
		{
			Lucene.Net.Queryparser.Flexible.Core.Nodes.AnyQueryNode clone = (Lucene.Net.Queryparser.Flexible.Core.Nodes.AnyQueryNode
				)base.CloneTree();
			clone.field = this.field;
			clone.minimumMatchingmElements = this.minimumMatchingmElements;
			return clone;
		}

		public override string ToString()
		{
			if (GetChildren() == null || GetChildren().Count == 0)
			{
				return "<any field='" + this.field + "'  matchelements=" + this.minimumMatchingmElements
					 + "/>";
			}
			StringBuilder sb = new StringBuilder();
			sb.Append("<any field='" + this.field + "'  matchelements=" + this.minimumMatchingmElements
				 + ">");
			foreach (QueryNode clause in GetChildren())
			{
				sb.Append("\n");
				sb.Append(clause.ToString());
			}
			sb.Append("\n</any>");
			return sb.ToString();
		}

		public override CharSequence ToQueryString(EscapeQuerySyntax escapeSyntaxParser)
		{
			string anySTR = "ANY " + this.minimumMatchingmElements;
			StringBuilder sb = new StringBuilder();
			if (GetChildren() == null || GetChildren().Count == 0)
			{
			}
			else
			{
				// no childs case
				string filler = string.Empty;
				foreach (QueryNode clause in GetChildren())
				{
					sb.Append(filler).Append(clause.ToQueryString(escapeSyntaxParser));
					filler = " ";
				}
			}
			if (IsDefaultField(this.field))
			{
				return "( " + sb.ToString() + " ) " + anySTR;
			}
			else
			{
				return this.field + ":(( " + sb.ToString() + " ) " + anySTR + ")";
			}
		}
	}
}
