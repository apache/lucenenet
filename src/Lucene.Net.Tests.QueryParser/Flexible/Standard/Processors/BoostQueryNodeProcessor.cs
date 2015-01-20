/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Config;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Processors;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Util;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Config;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors
{
	/// <summary>
	/// This processor iterates the query node tree looking for every
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.FieldableNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.FieldableNode
	/// 	</see>
	/// that has
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.BOOST
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.BOOST
	/// 	</see>
	/// in its
	/// config. If there is, the boost is applied to that
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.FieldableNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.FieldableNode
	/// 	</see>
	/// . <br/>
	/// </summary>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.BOOST
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.BOOST
	/// 	</seealso>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler</seealso>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.FieldableNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.FieldableNode
	/// 	</seealso>
	public class BoostQueryNodeProcessor : QueryNodeProcessorImpl
	{
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PostProcessNode(QueryNode node)
		{
			if (node is FieldableNode && (node.GetParent() == null || !(node.GetParent() is FieldableNode
				)))
			{
				FieldableNode fieldNode = (FieldableNode)node;
				QueryConfigHandler config = GetQueryConfigHandler();
				if (config != null)
				{
					CharSequence field = fieldNode.GetField();
					FieldConfig fieldConfig = config.GetFieldConfig(StringUtils.ToString(field));
					if (fieldConfig != null)
					{
						float boost = fieldConfig.Get(StandardQueryConfigHandler.ConfigurationKeys.BOOST);
						if (boost != null)
						{
							return new BoostQueryNode(node, boost);
						}
					}
				}
			}
			return node;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PreProcessNode(QueryNode node)
		{
			return node;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override IList<QueryNode> SetChildrenOrder(IList<QueryNode> children
			)
		{
			return children;
		}
	}
}
