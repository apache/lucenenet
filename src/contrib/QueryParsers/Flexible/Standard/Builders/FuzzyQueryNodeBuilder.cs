using Lucene.Net.Index;
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
    public class FuzzyQueryNodeBuilder : IStandardQueryBuilder
    {
        public FuzzyQueryNodeBuilder()
        {
            // empty constructor
        }

        public Query Build(IQueryNode queryNode)
        {
            FuzzyQueryNode fuzzyNode = (FuzzyQueryNode)queryNode;
            String text = fuzzyNode.TextAsString;

            int numEdits = FuzzyQuery.FloatToEdits(fuzzyNode.Similarity, text.Length);

            return new FuzzyQuery(new Term(fuzzyNode.FieldAsString, fuzzyNode.TextAsString), numEdits, fuzzyNode.PrefixLength);
        }

        object IQueryBuilder.Build(IQueryNode queryNode)
        {
            return Build(queryNode);
        }
    }
}
