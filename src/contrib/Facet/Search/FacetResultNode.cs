using Lucene.Net.Facet.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public class FacetResultNode
    {
        public static readonly IList<FacetResultNode> EMPTY_SUB_RESULTS = new List<FacetResultNode>();
        public int ordinal;
        public CategoryPath label;
        public double value;
        public IList<FacetResultNode> subResults = EMPTY_SUB_RESULTS;

        public FacetResultNode(int ordinal, double value)
        {
            this.ordinal = ordinal;
            this.value = value;
        }

        public override string ToString()
        {
            return ToString("");
        }

        public virtual string ToString(string prefix)
        {
            StringBuilder sb = new StringBuilder(prefix);
            if (label == null)
            {
                sb.Append("not labeled (ordinal=").Append(ordinal).Append(")");
            }
            else
            {
                sb.Append(label.ToString());
            }

            sb.Append(" (").Append(value.ToString()).Append(")");
            foreach (FacetResultNode sub in subResults)
            {
                sb.Append("\n").Append(prefix).Append(sub.ToString(prefix + "  "));
            }

            return sb.ToString();
        }
    }
}
