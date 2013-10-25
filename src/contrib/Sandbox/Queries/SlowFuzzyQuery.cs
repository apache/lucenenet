using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Sandbox.Queries
{
    public class SlowFuzzyQuery : MultiTermQuery
    {
        public static readonly float defaultMinSimilarity = LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE;
        public static readonly int defaultPrefixLength = 0;
        public static readonly int defaultMaxExpansions = 50;
        private float minimumSimilarity;
        private int prefixLength;
        private bool termLongEnough = false;
        protected Term term;

        public SlowFuzzyQuery(Term term, float minimumSimilarity, int prefixLength, int maxExpansions)
            : base(term.Field)
        {
            this.term = term;
            if (minimumSimilarity >= 1.0f && minimumSimilarity != (int)minimumSimilarity)
                throw new ArgumentException(@"fractional edit distances are not allowed");
            if (minimumSimilarity < 0.0f)
                throw new ArgumentException(@"minimumSimilarity < 0");
            if (prefixLength < 0)
                throw new ArgumentException(@"prefixLength < 0");
            if (maxExpansions < 0)
                throw new ArgumentException(@"maxExpansions < 0");
            SetRewriteMethod(new TopTermsScoringBooleanQueryRewrite(maxExpansions));
            string text = term.Text;
            int len = text.Length;
            if (len > 0 && (minimumSimilarity >= 1f || len > 1.0f / (1.0f - minimumSimilarity)))
            {
                this.termLongEnough = true;
            }

            this.minimumSimilarity = minimumSimilarity;
            this.prefixLength = prefixLength;
        }

        public SlowFuzzyQuery(Term term, float minimumSimilarity, int prefixLength)
            : this(term, minimumSimilarity, prefixLength, defaultMaxExpansions)
        {
        }

        public SlowFuzzyQuery(Term term, float minimumSimilarity)
            : this(term, minimumSimilarity, defaultPrefixLength, defaultMaxExpansions)
        {
        }

        public SlowFuzzyQuery(Term term)
            : this(term, defaultMinSimilarity, defaultPrefixLength, defaultMaxExpansions)
        {
        }

        public virtual float GetMinSimilarity()
        {
            return minimumSimilarity;
        }

        public virtual int GetPrefixLength()
        {
            return prefixLength;
        }

        protected override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
        {
            if (!termLongEnough)
            {
                return new SingleTermsEnum(terms.Iterator(null), term.Bytes);
            }

            return new SlowFuzzyTermsEnum(terms, atts, GetTerm(), minimumSimilarity, prefixLength);
        }

        public virtual Term GetTerm()
        {
            return term;
        }

        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            if (!term.Field.Equals(field))
            {
                buffer.Append(term.Field);
                buffer.Append(@":");
            }

            buffer.Append(term.Text);
            buffer.Append('~');
            buffer.Append(minimumSimilarity);
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + Number.FloatToIntBits(minimumSimilarity);
            result = prime * result + prefixLength;
            result = prime * result + ((term == null) ? 0 : term.GetHashCode());
            return result;
        }

        public override bool Equals(Object obj)
        {
            if (this == obj)
                return true;
            if (!base.Equals(obj))
                return false;
            if (GetType() != obj.GetType())
                return false;
            SlowFuzzyQuery other = (SlowFuzzyQuery)obj;
            if (Number.FloatToIntBits(minimumSimilarity) != Number.FloatToIntBits(other.minimumSimilarity))
                return false;
            if (prefixLength != other.prefixLength)
                return false;
            if (term == null)
            {
                if (other.term != null)
                    return false;
            }
            else if (!term.Equals(other.term))
                return false;
            return true;
        }
    }
}
