using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Params
{
    public class PerDimensionOrdinalPolicy : CategoryListParams
    {
        private readonly IDictionary<String, OrdinalPolicy?> policies;
        private readonly OrdinalPolicy defaultOP;

        public PerDimensionOrdinalPolicy(IDictionary<String, OrdinalPolicy?> policies)
            : this(policies, DEFAULT_ORDINAL_POLICY)
        {
        }

        public PerDimensionOrdinalPolicy(IDictionary<String, OrdinalPolicy?> policies, OrdinalPolicy defaultOP)
        {
            this.defaultOP = defaultOP;
            this.policies = policies;
        }

        public override OrdinalPolicy GetOrdinalPolicy(string dimension)
        {
            OrdinalPolicy? op = policies[dimension];
            return op == null ? defaultOP : op.Value;
        }

        public override string ToString()
        {
            return base.ToString() + @" policies=" + policies;
        }
    }
}
