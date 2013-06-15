using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public abstract class TermState : ICloneable
    {
        protected TermState()
        {
        }

        public abstract void CopyFrom(TermState other);

        public virtual object Clone()
        {
            return (TermState)this.MemberwiseClone();
        }

        public override string ToString()
        {
            return "TermState";
        }
    }
}
