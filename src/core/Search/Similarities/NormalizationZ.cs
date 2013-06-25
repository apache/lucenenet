using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public class NormalizationZ : Normalization
    {
        internal readonly float z;
        public float Z { get { return z; } }

        public NormalizationZ()
        {
            this(0.30F);
        }

        public NormalizationZ(float z)
        {
            this.z = z;
        }

        public override float Tfn(BasicStats stats, float tf, float len)
        {
            return (float)(tf * Math.pow(stats.avgFieldLength / len, z));
        }

        public override string ToString()
        {
            return "Z(" + z + ")";
        }
    }
}
