using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public sealed class AtomicReaderContext : IndexReaderContext
    {
        public readonly int ord;
        /** The readers absolute doc base */
        public readonly int docBase;

        private readonly AtomicReader reader;
        private readonly IList<AtomicReaderContext> leaves;

        public AtomicReaderContext(CompositeReaderContext parent, AtomicReader reader, int ord, int docBase, int leafOrd, int leafDocBase)
            : base(parent, ord, docBase)
        {
            this.ord = leafOrd;
            this.docBase = leafDocBase;
            this.reader = reader;
            this.leaves = isTopLevel ? new[] { this } : null;
        }

        public AtomicReaderContext(AtomicReader atomicReader)
            : this(null, atomicReader, 0, 0, 0, 0)
        {
        }

        public override IList<AtomicReaderContext> Leaves
        {
            get
            {
                if (!isTopLevel)
                {
                    throw new NotSupportedException("This is not a top-level context.");
                }
                //assert leaves != null;
                return leaves;
            }
        }

        public override IList<IndexReaderContext> Children
        {
            get
            {
                return null;
            }
        }

        public override IndexReader Reader
        {
            get
            {
                return reader;
            }
        }
    }
}
