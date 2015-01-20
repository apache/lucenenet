/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Queryparser.Flexible.Core;
using Lucene.Net.Queryparser.Flexible.Core.Builders;
using Lucene.Net.Queryparser.Flexible.Core.Messages;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Messages;
using Lucene.Net.Queryparser.Flexible.Standard.Parser;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Core.Builders
{
	/// <summary>This class should be used when there is a builder for each type of node.
	/// 	</summary>
	/// <remarks>
	/// This class should be used when there is a builder for each type of node.
	/// The type of node may be defined in 2 different ways: - by the field name,
	/// when the node implements the
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldableNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldableNode
	/// 	</see>
	/// interface - by its class,
	/// it keeps checking the class and all the interfaces and classes this class
	/// implements/extends until it finds a builder for that class/interface
	/// This class always check if there is a builder for the field name before it
	/// checks for the node class. So, field name builders have precedence over class
	/// builders.
	/// When a builder is found for a node, it's called and the node is passed to the
	/// builder. If the returned built object is not <code>null</code>, it's tagged
	/// on the node using the tag
	/// <see cref="QUERY_TREE_BUILDER_TAGID">QUERY_TREE_BUILDER_TAGID</see>
	/// .
	/// The children are usually built before the parent node. However, if a builder
	/// associated to a node is an instance of
	/// <see cref="QueryTreeBuilder">QueryTreeBuilder</see>
	/// , the node is
	/// delegated to this builder and it's responsible to build the node and its
	/// children.
	/// </remarks>
	/// <seealso cref="QueryBuilder">QueryBuilder</seealso>
	public class QueryTreeBuilder : QueryBuilder
	{
		/// <summary>
		/// This tag is used to tag the nodes in a query tree with the built objects
		/// produced from their own associated builder.
		/// </summary>
		/// <remarks>
		/// This tag is used to tag the nodes in a query tree with the built objects
		/// produced from their own associated builder.
		/// </remarks>
		public static readonly string QUERY_TREE_BUILDER_TAGID = typeof(Lucene.Net.Queryparser.Flexible.Core.Builders.QueryTreeBuilder
			).FullName;

		private Dictionary<Type, QueryBuilder> queryNodeBuilders;

		private Dictionary<string, QueryBuilder> fieldNameBuilders;

		/// <summary>
		/// <see cref="QueryTreeBuilder">QueryTreeBuilder</see>
		/// constructor.
		/// </summary>
		public QueryTreeBuilder()
		{
		}

		// empty constructor
		/// <summary>Associates a field name with a builder.</summary>
		/// <remarks>Associates a field name with a builder.</remarks>
		/// <param name="fieldName">the field name</param>
		/// <param name="builder">the builder to be associated</param>
		public virtual void SetBuilder(CharSequence fieldName, QueryBuilder builder)
		{
			if (this.fieldNameBuilders == null)
			{
				this.fieldNameBuilders = new Dictionary<string, QueryBuilder>();
			}
			this.fieldNameBuilders.Put(fieldName.ToString(), builder);
		}

		/// <summary>Associates a class with a builder</summary>
		/// <param name="queryNodeClass">the class</param>
		/// <param name="builder">the builder to be associated</param>
		public virtual void SetBuilder<_T0>(Type<_T0> queryNodeClass, QueryBuilder builder
			) where _T0:QueryNode
		{
			if (this.queryNodeBuilders == null)
			{
				this.queryNodeBuilders = new Dictionary<Type, QueryBuilder>();
			}
			this.queryNodeBuilders.Put(queryNodeClass, builder);
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		private void Process(QueryNode node)
		{
			if (node != null)
			{
				QueryBuilder builder = GetBuilder(node);
				if (!(builder is Lucene.Net.Queryparser.Flexible.Core.Builders.QueryTreeBuilder
					))
				{
					IList<QueryNode> children = node.GetChildren();
					if (children != null)
					{
						foreach (QueryNode child in children)
						{
							Process(child);
						}
					}
				}
				ProcessNode(node, builder);
			}
		}

		private QueryBuilder GetBuilder(QueryNode node)
		{
			QueryBuilder builder = null;
			if (this.fieldNameBuilders != null && node is FieldableNode)
			{
				CharSequence field = ((FieldableNode)node).GetField();
				if (field != null)
				{
					field = field.ToString();
				}
				builder = this.fieldNameBuilders.Get(field);
			}
			if (builder == null && this.queryNodeBuilders != null)
			{
				Type clazz = node.GetType();
				do
				{
					builder = GetQueryBuilder(clazz);
					if (builder == null)
					{
						Type[] classes = clazz.GetInterfaces();
						foreach (Type actualClass in classes)
						{
							builder = GetQueryBuilder(actualClass);
							if (builder != null)
							{
								break;
							}
						}
					}
				}
				while (builder == null && (clazz = clazz.BaseType) != null);
			}
			return builder;
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		private void ProcessNode(QueryNode node, QueryBuilder builder)
		{
			if (builder == null)
			{
				throw new QueryNodeException(new MessageImpl(QueryParserMessages.LUCENE_QUERY_CONVERSION_ERROR
					, node.ToQueryString(new EscapeQuerySyntaxImpl()), node.GetType().FullName));
			}
			object obj = builder.Build(node);
			if (obj != null)
			{
				node.SetTag(QUERY_TREE_BUILDER_TAGID, obj);
			}
		}

		private QueryBuilder GetQueryBuilder<_T0>(Type<_T0> clazz)
		{
			if (typeof(QueryNode).IsAssignableFrom(clazz))
			{
				return this.queryNodeBuilders.Get(clazz);
			}
			return null;
		}

		/// <summary>Builds some kind of object from a query tree.</summary>
		/// <remarks>
		/// Builds some kind of object from a query tree. Each node in the query tree
		/// is built using an specific builder associated to it.
		/// </remarks>
		/// <param name="queryNode">the query tree root node</param>
		/// <returns>the built object</returns>
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// if some node builder throws a
		/// <see cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">Lucene.Net.Queryparser.Flexible.Core.QueryNodeException
		/// 	</see>
		/// or if there is a node which had no
		/// builder associated to it
		/// </exception>
		public virtual object Build(QueryNode queryNode)
		{
			Process(queryNode);
			return queryNode.GetTag(QUERY_TREE_BUILDER_TAGID);
		}
	}
}
