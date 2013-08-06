using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search
{
    public sealed class MaxNonCompetitiveBoostAttribute : Lucene.Net.Util.Attribute, IMaxNonCompetitiveBoostAttribute
    {
        private float maxNonCompetitiveBoost = float.NegativeInfinity;
        private BytesRef competitiveTerm = null;
        
        public float MaxNonCompetitiveBoost
        {
            get
            {
                return maxNonCompetitiveBoost;
            }
            set
            {
                this.maxNonCompetitiveBoost = value;
            }
        }

        public BytesRef CompetitiveTerm
        {
            get
            {
                return competitiveTerm;
            }
            set
            {
                this.competitiveTerm = value;
            }
        }

        public override void Clear()
        {
            maxNonCompetitiveBoost = float.NegativeInfinity;
            competitiveTerm = null;
        }

        public override void CopyTo(Util.Attribute target)
        {
            MaxNonCompetitiveBoostAttribute t = (MaxNonCompetitiveBoostAttribute)target;
            t.MaxNonCompetitiveBoost = maxNonCompetitiveBoost;
            t.CompetitiveTerm = competitiveTerm;
        }
    }
}
