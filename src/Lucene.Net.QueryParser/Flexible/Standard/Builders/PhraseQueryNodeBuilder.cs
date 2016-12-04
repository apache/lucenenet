using Lucene.Net.QueryParsers.Flexible.Core.Builders;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Builders
{
    /// <summary>
    /// Builds a {@link PhraseQuery} object from a {@link TokenizedPhraseQueryNode}
    /// object.
    /// </summary>
    public class PhraseQueryNodeBuilder : IStandardQueryBuilder
    {
        public PhraseQueryNodeBuilder()
        {
            // empty constructor
        }

        public virtual Query Build(IQueryNode queryNode)
        {
            TokenizedPhraseQueryNode phraseNode = (TokenizedPhraseQueryNode)queryNode;

            PhraseQuery phraseQuery = new PhraseQuery();

            IList<IQueryNode> children = phraseNode.GetChildren();

            if (children != null)
            {
                foreach (IQueryNode child in children)
                {
                    TermQuery termQuery = (TermQuery)child
                        .GetTag(QueryTreeBuilder<Query>.QUERY_TREE_BUILDER_TAGID);
                    FieldQueryNode termNode = (FieldQueryNode)child;

                    phraseQuery.Add(termQuery.Term, termNode.PositionIncrement);
                }
            }

            return phraseQuery;
        }
    }
}
