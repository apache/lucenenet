using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.Search;
using Lucene.Net.Support;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Builders
{
    /// <summary>
    /// Builds a {@link FuzzyQuery} object from a {@link FuzzyQueryNode} object.
    /// </summary>
    public class FuzzyQueryNodeBuilder : IStandardQueryBuilder
    {
        public FuzzyQueryNodeBuilder()
        {
            // empty constructor
        }

        public virtual Query Build(IQueryNode queryNode)
        {
            FuzzyQueryNode fuzzyNode = (FuzzyQueryNode)queryNode;
            string text = fuzzyNode.GetTextAsString();

            int numEdits = FuzzyQuery.FloatToEdits(fuzzyNode.Similarity,
                text.CodePointCount(0, text.Length));

            return new FuzzyQuery(new Term(fuzzyNode.GetFieldAsString(), fuzzyNode
                .GetTextAsString()), numEdits, fuzzyNode
                .PrefixLength);
        }
    }
}
