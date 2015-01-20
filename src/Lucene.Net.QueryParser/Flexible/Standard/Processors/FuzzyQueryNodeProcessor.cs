/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Queryparser.Flexible.Core.Config;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Core.Processors;
using Lucene.Net.Queryparser.Flexible.Standard.Config;
using Lucene.Net.Queryparser.Flexible.Standard.Processors;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Standard.Processors
{
	/// <summary>
	/// This processor iterates the query node tree looking for every
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.FuzzyQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.FuzzyQueryNode
	/// 	</see>
	/// , when this kind of node is found, it checks on the
	/// query configuration for
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.FUZZY_CONFIG
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.FUZZY_CONFIG
	/// 	</see>
	/// , gets the
	/// fuzzy prefix length and default similarity from it and set to the fuzzy node.
	/// For more information about fuzzy prefix length check:
	/// <see cref="Lucene.Net.Search.FuzzyQuery">Lucene.Net.Search.FuzzyQuery
	/// 	</see>
	/// . <br/>
	/// </summary>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.FUZZY_CONFIG
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.FUZZY_CONFIG
	/// 	</seealso>
	/// <seealso cref="Lucene.Net.Search.FuzzyQuery">Lucene.Net.Search.FuzzyQuery
	/// 	</seealso>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.FuzzyQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.FuzzyQueryNode
	/// 	</seealso>
	public class FuzzyQueryNodeProcessor : QueryNodeProcessorImpl
	{
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PostProcessNode(QueryNode node)
		{
			return node;
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PreProcessNode(QueryNode node)
		{
			if (node is FuzzyQueryNode)
			{
				FuzzyQueryNode fuzzyNode = (FuzzyQueryNode)node;
				QueryConfigHandler config = GetQueryConfigHandler();
				FuzzyConfig fuzzyConfig = null;
				if (config != null && (fuzzyConfig = config.Get(StandardQueryConfigHandler.ConfigurationKeys
					.FUZZY_CONFIG)) != null)
				{
					fuzzyNode.SetPrefixLength(fuzzyConfig.GetPrefixLength());
					if (fuzzyNode.GetSimilarity() < 0)
					{
						fuzzyNode.SetSimilarity(fuzzyConfig.GetMinSimilarity());
					}
				}
				else
				{
					if (fuzzyNode.GetSimilarity() < 0)
					{
						throw new ArgumentException("No FUZZY_CONFIG set in the config");
					}
				}
			}
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
