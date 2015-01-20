/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.Text;
using Org.Apache.Lucene.Queryparser.Flexible.Core;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Messages;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Parser;
using Org.Apache.Lucene.Queryparser.Flexible.Messages;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes
{
	/// <summary>
	/// A
	/// <see cref="ProximityQueryNode">ProximityQueryNode</see>
	/// represents a query where the terms should meet
	/// specific distance conditions. (a b c) WITHIN [SENTENCE|PARAGRAPH|NUMBER]
	/// [INORDER] ("a" "b" "c") WITHIN [SENTENCE|PARAGRAPH|NUMBER] [INORDER]
	/// TODO: Add this to the future standard Lucene parser/processor/builder
	/// </summary>
	public class ProximityQueryNode : BooleanQueryNode
	{
		/// <summary>Distance condition: PARAGRAPH, SENTENCE, or NUMBER</summary>
		public enum Type
		{
			PARAGRAPH,
			SENTENCE,
			NUMBER
		}

		internal class EnumTypeHelper
		{
			//HM:revisit refactored to EnumTypeHelper
			//abstract CharSequence toQueryString();
			public static CharSequence ToQueryString(ProximityQueryNode.Type type)
			{
				switch (type)
				{
					case ProximityQueryNode.Type.PARAGRAPH:
					{
						return "WITHIN PARAGRAPH";
					}

					case ProximityQueryNode.Type.SENTENCE:
					{
						return "WITHIN SENTENCE";
					}

					case ProximityQueryNode.Type.NUMBER:
					{
						return "WITHIN";
					}

					default:
					{
						return null;
						break;
					}
				}
			}
		}

		/// <summary>utility class containing the distance condition and number</summary>
		public class ProximityType
		{
			internal int pDistance = 0;

			internal ProximityQueryNode.Type pType = null;

			public ProximityType(ProximityQueryNode.Type type) : this(type, 0)
			{
			}

			public ProximityType(ProximityQueryNode.Type type, int distance)
			{
				this.pType = type;
				this.pDistance = distance;
			}
		}

		private ProximityQueryNode.Type proximityType = ProximityQueryNode.Type.SENTENCE;

		private int distance = -1;

		private bool inorder = false;

		private CharSequence field = null;

		/// <param name="clauses">- QueryNode children</param>
		/// <param name="field">- field name</param>
		/// <param name="type">- type of proximity query</param>
		/// <param name="distance">- positive integer that specifies the distance</param>
		/// <param name="inorder">
		/// - true, if the tokens should be matched in the order of the
		/// clauses
		/// </param>
		public ProximityQueryNode(IList<QueryNode> clauses, CharSequence field, ProximityQueryNode.Type
			 type, int distance, bool inorder) : base(clauses)
		{
			SetLeaf(false);
			this.proximityType = type;
			this.inorder = inorder;
			this.field = field;
			if (type == ProximityQueryNode.Type.NUMBER)
			{
				if (distance <= 0)
				{
					throw new QueryNodeError(new MessageImpl(QueryParserMessages.PARAMETER_VALUE_NOT_SUPPORTED
						, "distance", distance));
				}
				else
				{
					this.distance = distance;
				}
			}
			ClearFields(clauses, field);
		}

		/// <param name="clauses">- QueryNode children</param>
		/// <param name="field">- field name</param>
		/// <param name="type">- type of proximity query</param>
		/// <param name="inorder">
		/// - true, if the tokens should be matched in the order of the
		/// clauses
		/// </param>
		public ProximityQueryNode(IList<QueryNode> clauses, CharSequence field, ProximityQueryNode.Type
			 type, bool inorder) : this(clauses, field, type, -1, inorder)
		{
		}

		private static void ClearFields(IList<QueryNode> nodes, CharSequence field)
		{
			if (nodes == null || nodes.Count == 0)
			{
				return;
			}
			foreach (QueryNode clause in nodes)
			{
				if (clause is FieldQueryNode)
				{
					((FieldQueryNode)clause).toQueryStringIgnoreFields = true;
					((FieldQueryNode)clause).SetField(field);
				}
			}
		}

		public virtual ProximityQueryNode.Type GetProximityType()
		{
			return this.proximityType;
		}

		public override string ToString()
		{
			string distanceSTR = ((this.distance == -1) ? (string.Empty) : (" distance='" + this
				.distance) + "'");
			if (GetChildren() == null || GetChildren().Count == 0)
			{
				return "<proximity field='" + this.field + "' inorder='" + this.inorder + "' type='"
					 + this.proximityType.ToString() + "'" + distanceSTR + "/>";
			}
			StringBuilder sb = new StringBuilder();
			sb.Append("<proximity field='" + this.field + "' inorder='" + this.inorder + "' type='"
				 + this.proximityType.ToString() + "'" + distanceSTR + ">");
			foreach (QueryNode child in GetChildren())
			{
				sb.Append("\n");
				sb.Append(child.ToString());
			}
			sb.Append("\n</proximity>");
			return sb.ToString();
		}

		public override CharSequence ToQueryString(EscapeQuerySyntax escapeSyntaxParser)
		{
			string withinSTR = ProximityQueryNode.EnumTypeHelper.ToQueryString(this.proximityType
				) + ((this.distance == -1) ? (string.Empty) : (" " + this.distance)) + ((this.inorder
				) ? (" INORDER") : (string.Empty));
			StringBuilder sb = new StringBuilder();
			if (GetChildren() == null || GetChildren().Count == 0)
			{
			}
			else
			{
				// no children case
				string filler = string.Empty;
				foreach (QueryNode child in GetChildren())
				{
					sb.Append(filler).Append(child.ToQueryString(escapeSyntaxParser));
					filler = " ";
				}
			}
			if (IsDefaultField(this.field))
			{
				return "( " + sb.ToString() + " ) " + withinSTR;
			}
			else
			{
				return this.field + ":(( " + sb.ToString() + " ) " + withinSTR + ")";
			}
		}

		/// <exception cref="Sharpen.CloneNotSupportedException"></exception>
		public override QueryNode CloneTree()
		{
			ProximityQueryNode clone = (ProximityQueryNode)base.CloneTree();
			clone.proximityType = this.proximityType;
			clone.distance = this.distance;
			clone.field = this.field;
			return clone;
		}

		/// <returns>the distance</returns>
		public virtual int GetDistance()
		{
			return this.distance;
		}

		/// <summary>returns null if the field was not specified in the query string</summary>
		/// <returns>the field</returns>
		public virtual CharSequence GetField()
		{
			return this.field;
		}

		/// <summary>returns null if the field was not specified in the query string</summary>
		/// <returns>the field</returns>
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

		/// <param name="field">the field to set</param>
		public virtual void SetField(CharSequence field)
		{
			this.field = field;
		}

		/// <returns>terms must be matched in the specified order</returns>
		public virtual bool IsInOrder()
		{
			return this.inorder;
		}
	}
}
