using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    public class FuzzyQueryNode : FieldQueryNode
    {
        private float similarity;

        private int prefixLength;

        public FuzzyQueryNode(ICharSequence field, ICharSequence term, float minSimilarity, int begin, int end)
            : base(field, term, begin, end)
        {
            this.similarity = minSimilarity;
            SetLeaf(true);
        }

        public int PrefixLength
        {
            get { return prefixLength; }
            set { prefixLength = value; }
        }

        public override ICharSequence ToQueryString(IEscapeQuerySyntax escaper)
        {
            if (IsDefaultField(this.field))
            {
                return new StringCharSequenceWrapper(GetTermEscaped(escaper) + "~" + this.similarity);
            }
            else
            {
                return new StringCharSequenceWrapper(this.field + ":" + GetTermEscaped(escaper) + "~" + this.similarity);
            }
        }

        public override string ToString()
        {
            return "<fuzzy field='" + this.field + "' similarity='" + this.similarity + "' term='" + this.text + "'/>";
        }

        public float Similarity
        {
            get { return similarity; }
            set { similarity = value; }
        }

        public override IQueryNode CloneTree()
        {
            FuzzyQueryNode clone = (FuzzyQueryNode)base.CloneTree();

            clone.similarity = this.similarity;

            return clone;
        }
    }
}
