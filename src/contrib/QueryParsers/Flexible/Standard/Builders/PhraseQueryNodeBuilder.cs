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
    public class PhraseQueryNodeBuilder : IStandardQueryBuilder
    {
        public PhraseQueryNodeBuilder()
        {
            // empty constructor
        }

        public Query Build(IQueryNode queryNode)
        {
            TokenizedPhraseQueryNode phraseNode = (TokenizedPhraseQueryNode)queryNode;

            PhraseQuery phraseQuery = new PhraseQuery();

            IList<IQueryNode> children = phraseNode.Children;

            if (children != null)
            {
                foreach (IQueryNode child in children)
                {
                    TermQuery termQuery = (TermQuery)child
                        .GetTag(QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID);
                    FieldQueryNode termNode = (FieldQueryNode)child;

                    phraseQuery.Add(termQuery.Term, termNode.PositionIncrement);
                }
            }

            return phraseQuery;
        }

        object IQueryBuilder.Build(IQueryNode queryNode)
        {
            return Build(queryNode);
        }
    }
}
