using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Flexible.Core.Builders;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.Search;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Builders
{
    /// <summary>
    /// Builds a {@link MultiPhraseQuery} object from a {@link MultiPhraseQueryNode}
    /// object.
    /// </summary>
    public class MultiPhraseQueryNodeBuilder : IStandardQueryBuilder
    {
        public MultiPhraseQueryNodeBuilder()
        {
            // empty constructor
        }


        public virtual Query Build(IQueryNode queryNode)
        {
            MultiPhraseQueryNode phraseNode = (MultiPhraseQueryNode)queryNode;

            MultiPhraseQuery phraseQuery = new MultiPhraseQuery();

            IList<IQueryNode> children = phraseNode.GetChildren();

            if (children != null)
            {
                //TreeDictionary<int?, List<Term>> positionTermMap = new TreeDictionary<int?, List<Term>>();
                IDictionary<int?, List<Term>> positionTermMap = new SortedDictionary<int?, List<Term>>();

                foreach (IQueryNode child in children)
                {
                    FieldQueryNode termNode = (FieldQueryNode)child;
                    TermQuery termQuery = (TermQuery)termNode
                        .GetTag(QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID);

                    //List<Term> termList = positionTermMap[termNode.GetPositionIncrement()];
                    //if (termList == null)
                    List<Term> termList;
                    if (!positionTermMap.TryGetValue(termNode.GetPositionIncrement(), out termList) || termList == null)
                    {
                        termList = new List<Term>();
                        positionTermMap[termNode.GetPositionIncrement()] = termList;

                    }

                    termList.Add(termQuery.Term);

                }

                foreach (int positionIncrement in positionTermMap.Keys)
                {
                    List<Term> termList = positionTermMap[positionIncrement];

                    phraseQuery.Add(termList.ToArray(/*new Term[termList.size()]*/),
                                positionIncrement);

                }

            }

            return phraseQuery;

        }

        /// <summary>
        /// LUCENENET specific overload for supporting IQueryBuilder
        /// </summary>
        object IQueryBuilder.Build(IQueryNode queryNode)
        {
            return Build(queryNode);
        }
    }
}
