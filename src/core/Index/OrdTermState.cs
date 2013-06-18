using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public class OrdTermState : TermState
    {
        public long ord;

        public OrdTermState()
        {
        }

        public override void CopyFrom(TermState other)
        {
            //assert other instanceof OrdTermState : "can not copy from " + other.getClass().getName();
            this.ord = ((OrdTermState)other).ord;
        }

        public override string ToString()
        {
            return "OrdTermState ord=" + ord;
        }
    }
}
