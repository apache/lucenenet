/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Queryparser.Flexible.Core.Config;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Core.Processors;
using Lucene.Net.Queryparser.Flexible.Core.Util;
using Lucene.Net.Queryparser.Flexible.Standard.Config;
using Lucene.Net.Queryparser.Flexible.Standard.Processors;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Standard.Processors
{
	/// <summary>
	/// This processor iterates the query node tree looking for every
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldableNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldableNode
	/// 	</see>
	/// that has
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.BOOST
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.BOOST
	/// 	</see>
	/// in its
	/// config. If there is, the boost is applied to that
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldableNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldableNode
	/// 	</see>
	/// . <br/>
	/// </summary>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.BOOST
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.BOOST
	/// 	</seealso>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler
	/// 	">Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler</seealso>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldableNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldableNode
	/// 	</seealso>
	public class BoostQueryNodeProcessor : QueryNodeProcessorImpl
	{
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
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

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PreProcessNode(QueryNode node)
		{
			return node;
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override IList<QueryNode> SetChildrenOrder(IList<QueryNode> children
			)
		{
			return children;
		}
	}
}
