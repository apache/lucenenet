using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public class DistributionLL : Distribution
    {
        public DistributionLL() { }

        public override sealed float Score(BasicStats stats, float tfn, float lambda)
        {
            return (float)-Math.log(lambda / (tfn + lambda));
        }

        public override string ToString()
        {
            return "LL";
        }
    }
}
