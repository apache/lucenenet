using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public class Lambda
    {
        public Lambda() { }

        public abstract float Lambda(BasicStats stats);
        public abstract Explanation Explain(BasicStats stats);

        public override abstract string ToString();
    }
}
