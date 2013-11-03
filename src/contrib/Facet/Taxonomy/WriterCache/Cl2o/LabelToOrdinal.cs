using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Taxonomy.WriterCache.Cl2o
{
    public abstract class LabelToOrdinal
    {
        protected int counter;
        public const int INVALID_ORDINAL = -2;

        public virtual int MaxOrdinal
        {
            get
            {
                return this.counter;
            }
        }

        public virtual int NextOrdinal
        {
            get
            {
                return this.counter++;
            }
        }

        public abstract void AddLabel(CategoryPath label, int ordinal);

        public abstract int GetOrdinal(CategoryPath label);
    }
}
