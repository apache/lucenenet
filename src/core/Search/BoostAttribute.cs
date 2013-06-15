using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Util;


namespace Lucene.Net.Search
{
    public sealed class BoostAttribute : Lucene.Net.Util.Attribute, IBoostAttribute
    {
        private float boost = 1.0f;

        public float Boost
        {
            get { return boost; }
            set { boost = value; }
        }

        public override void Clear()
        {
            boost = 1.0f;
        }
        
        public override void CopyTo(Lucene.Net.Util.Attribute target)
        {
            ((BoostAttribute)target).Boost = boost;
        }
    }
}
