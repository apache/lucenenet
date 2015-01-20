/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Queryparser.Flexible.Core.Config;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Core.Processors;
using Lucene.Net.Queryparser.Flexible.Standard.Config;
using Lucene.Net.Queryparser.Flexible.Standard.Nodes;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Standard.Processors
{
	/// <summary>
	/// This processor verifies if
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.PHRASE_SLOP
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.PHRASE_SLOP
	/// 	</see>
	/// is defined in the
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler"
	/// 	>Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler</see>
	/// . If it is, it looks for every
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.TokenizedPhraseQueryNode
	/// 	">Lucene.Net.Queryparser.Flexible.Core.Nodes.TokenizedPhraseQueryNode</see>
	/// and
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Nodes.MultiPhraseQueryNode
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Nodes.MultiPhraseQueryNode</see>
	/// that does
	/// not have any
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.SlopQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.SlopQueryNode
	/// 	</see>
	/// applied to it and creates an
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.SlopQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.SlopQueryNode
	/// 	</see>
	/// and apply to it. The new
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.SlopQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.SlopQueryNode
	/// 	</see>
	/// has the
	/// same slop value defined in the configuration. <br/>
	/// </summary>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.SlopQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.SlopQueryNode
	/// 	</seealso>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.PHRASE_SLOP
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.PHRASE_SLOP
	/// 	</seealso>
	public class DefaultPhraseSlopQueryNodeProcessor : QueryNodeProcessorImpl
	{
		private bool processChildren = true;

		private int defaultPhraseSlop;

		public DefaultPhraseSlopQueryNodeProcessor()
		{
		}

		// empty constructor
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public override QueryNode Process(QueryNode queryTree)
		{
			QueryConfigHandler queryConfig = GetQueryConfigHandler();
			if (queryConfig != null)
			{
				int defaultPhraseSlop = queryConfig.Get(StandardQueryConfigHandler.ConfigurationKeys
					.PHRASE_SLOP);
				if (defaultPhraseSlop != null)
				{
					this.defaultPhraseSlop = defaultPhraseSlop;
					return base.Process(queryTree);
				}
			}
			return queryTree;
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PostProcessNode(QueryNode node)
		{
			if (node is TokenizedPhraseQueryNode || node is MultiPhraseQueryNode)
			{
				return new SlopQueryNode(node, this.defaultPhraseSlop);
			}
			return node;
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PreProcessNode(QueryNode node)
		{
			if (node is SlopQueryNode)
			{
				this.processChildren = false;
			}
			return node;
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override void ProcessChildren(QueryNode queryTree)
		{
			if (this.processChildren)
			{
				base.ProcessChildren(queryTree);
			}
			else
			{
				this.processChildren = true;
			}
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
