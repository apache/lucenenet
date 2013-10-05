using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Flexible.Core.Builders;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Builders
{
    public class MultiPhraseQueryNodeBuilder : IStandardQueryBuilder
    {
        public MultiPhraseQueryNodeBuilder()
        {
            // empty constructor
        }

        public Query Build(IQueryNode queryNode)
        {
            MultiPhraseQueryNode phraseNode = (MultiPhraseQueryNode)queryNode;

            MultiPhraseQuery phraseQuery = new MultiPhraseQuery();

            IList<IQueryNode> children = phraseNode.Children;

            if (children != null)
            {
                SortedDictionary<int, IList<Term>> positionTermMap = new SortedDictionary<int, IList<Term>>();

                foreach (QueryNode child in children)
                {
                    FieldQueryNode termNode = (FieldQueryNode)child;
                    TermQuery termQuery = (TermQuery)termNode
                        .GetTag(QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID);
                    IList<Term> termList = positionTermMap[termNode.PositionIncrement];

                    if (termList == null)
                    {
                        termList = new List<Term>();
                        positionTermMap[termNode.PositionIncrement] = termList;

                    }

                    termList.Add(termQuery.Term);
                }

                foreach (int positionIncrement in positionTermMap.Keys)
                {
                    IList<Term> termList = positionTermMap[positionIncrement];

                    phraseQuery.Add(termList.ToArray(), positionIncrement);
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
