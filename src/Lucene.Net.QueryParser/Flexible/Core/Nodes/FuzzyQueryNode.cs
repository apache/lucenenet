using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    /// <summary>
    /// A {@link FuzzyQueryNode} represents a element that contains
    /// field/text/similarity tuple
    /// </summary>
    public class FuzzyQueryNode : FieldQueryNode
    {
        private float similarity;

        private int prefixLength;

        /**
         * @param field
         *          Name of the field query will use.
         * @param termStr
         *          Term token to use for building term for the query
         */
        /**
         * @param field
         *          - Field name
         * @param term
         *          - Value
         * @param minSimilarity
         *          - similarity value
         * @param begin
         *          - position in the query string
         * @param end
         *          - position in the query string
         */
         // LUCENENET specific overload for string term
        public FuzzyQueryNode(string field, string term,
            float minSimilarity, int begin, int end)
            : this(field, term.ToCharSequence(), minSimilarity, begin, end)
        {
        }

        /**
         * @param field
         *          - Field name
         * @param term
         *          - Value
         * @param minSimilarity
         *          - similarity value
         * @param begin
         *          - position in the query string
         * @param end
         *          - position in the query string
         */
        public FuzzyQueryNode(string field, ICharSequence term,
            float minSimilarity, int begin, int end)
            : base(field, term, begin, end)
        {
            this.similarity = minSimilarity;
            SetLeaf(true);
        }

        public virtual void SetPrefixLength(int prefixLength)
        {
            this.prefixLength = prefixLength;
        }

        public virtual int GetPrefixLength()
        {
            return this.prefixLength;
        }


        public override string ToQueryString(IEscapeQuerySyntax escaper)
        {
            if (IsDefaultField(this.field))
            {
                return GetTermEscaped(escaper) + "~" + this.similarity;
            }
            else
            {
                return this.field + ":" + GetTermEscaped(escaper) + "~" + this.similarity;
            }
        }


        public override string ToString()
        {
            return "<fuzzy field='" + this.field + "' similarity='" + this.similarity
                + "' term='" + this.text + "'/>";
        }

        public virtual void SetSimilarity(float similarity)
        {
            this.similarity = similarity;
        }


        public override IQueryNode CloneTree()
        {
            FuzzyQueryNode clone = (FuzzyQueryNode)base.CloneTree();

            clone.similarity = this.similarity;

            return clone;
        }

        /**
         * @return the similarity
         */
        public virtual float GetSimilarity()
        {
            return this.similarity;
        }
    }
}
