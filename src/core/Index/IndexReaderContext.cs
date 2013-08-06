using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public abstract class IndexReaderContext
    {
        public readonly CompositeReaderContext parent;
        public readonly bool isTopLevel;
        public readonly int docBaseInParent;
        public readonly int ordInParent;

        public IndexReaderContext(CompositeReaderContext parent, int ordInParent, int docBaseInParent)
        {
            if (!(this is CompositeReaderContext || this is AtomicReaderContext))
                throw new Exception("This class should never be extended by custom code!");

            //if (!(this instanceof CompositeReaderContext || this instanceof AtomicReaderContext))
            //    throw new Exception("This class should never be extended by custom code!");
            this.parent = parent;
            this.docBaseInParent = docBaseInParent;
            this.ordInParent = ordInParent;
            this.isTopLevel = parent == null;
        }

        public abstract IndexReader Reader { get; }

        /**
         * Returns the context's leaves if this context is a top-level context.
         * For convenience, if this is an {@link AtomicReaderContext} this
         * returns itself as the only leaf.
         * <p>Note: this is convenience method since leaves can always be obtained by
         * walking the context tree using {@link #children()}.
         * @throws UnsupportedOperationException if this is not a top-level context.
         * @see #children()
         */
        public abstract IList<AtomicReaderContext> Leaves { get; }

        /**
         * Returns the context's children iff this context is a composite context
         * otherwise <code>null</code>.
         */
        public abstract IList<IndexReaderContext> Children { get; }
    }
}
