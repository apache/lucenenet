/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Config;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Processors;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Config;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors
{
	/// <summary>
	/// This processor verifies if
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.PHRASE_SLOP
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.PHRASE_SLOP
	/// 	</see>
	/// is defined in the
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler"
	/// 	>Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler</see>
	/// . If it is, it looks for every
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.TokenizedPhraseQueryNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.TokenizedPhraseQueryNode</see>
	/// and
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.MultiPhraseQueryNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.MultiPhraseQueryNode</see>
	/// that does
	/// not have any
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.SlopQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.SlopQueryNode
	/// 	</see>
	/// applied to it and creates an
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.SlopQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.SlopQueryNode
	/// 	</see>
	/// and apply to it. The new
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.SlopQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.SlopQueryNode
	/// 	</see>
	/// has the
	/// same slop value defined in the configuration. <br/>
	/// </summary>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.SlopQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.SlopQueryNode
	/// 	</seealso>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.PHRASE_SLOP
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.PHRASE_SLOP
	/// 	</seealso>
	public class DefaultPhraseSlopQueryNodeProcessor : QueryNodeProcessorImpl
	{
		private bool processChildren = true;

		private int defaultPhraseSlop;

		public DefaultPhraseSlopQueryNodeProcessor()
		{
		}

		// empty constructor
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
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

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PostProcessNode(QueryNode node)
		{
			if (node is TokenizedPhraseQueryNode || node is MultiPhraseQueryNode)
			{
				return new SlopQueryNode(node, this.defaultPhraseSlop);
			}
			return node;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PreProcessNode(QueryNode node)
		{
			if (node is SlopQueryNode)
			{
				this.processChildren = false;
			}
			return node;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
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

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override IList<QueryNode> SetChildrenOrder(IList<QueryNode> children
			)
		{
			return children;
		}
	}
}
