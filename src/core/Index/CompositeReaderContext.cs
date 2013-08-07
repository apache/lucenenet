using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public sealed class CompositeReaderContext : IndexReaderContext
    {
        private readonly IList<IndexReaderContext> children;
        private readonly IList<AtomicReaderContext> leaves;
        private readonly CompositeReader reader;

        internal static CompositeReaderContext Create(CompositeReader reader)
        {
            return new Builder(reader).Build();
        }

        /**
         * Creates a {@link CompositeReaderContext} for intermediate readers that aren't
         * not top-level readers in the current context
         */
        internal CompositeReaderContext(CompositeReaderContext parent, CompositeReader reader, int ordInParent,
            int docbaseInParent, IList<IndexReaderContext> children)
            : this(parent, reader, ordInParent, docbaseInParent, children, null)
        {
        }

        /**
         * Creates a {@link CompositeReaderContext} for top-level readers with parent set to <code>null</code>
         */
        internal CompositeReaderContext(CompositeReader reader, IList<IndexReaderContext> children, IList<AtomicReaderContext> leaves)
            : this(null, reader, 0, 0, children, leaves)
        {
        }

        private CompositeReaderContext(CompositeReaderContext parent, CompositeReader reader,
            int ordInParent, int docbaseInParent, IList<IndexReaderContext> children, IList<AtomicReaderContext> leaves)
            : base(parent, ordInParent, docbaseInParent)
        {
            this.children = children.ToArray();
            this.leaves = leaves == null ? null : leaves;
            this.reader = reader;
        }
        
        public override IList<AtomicReaderContext> Leaves
        {
            get
            {
                if (!isTopLevel)
                    throw new NotSupportedException("This is not a top-level context.");
                //assert leaves != null;
                return leaves;
            }
        }

        public override IList<IndexReaderContext> Children
        {
            get
            {
                return children;
            }
        }

        public override IndexReader Reader
        {
            get
            {
                return reader;
            }
        }

        private class Builder
        {
            private readonly CompositeReader reader;
            private readonly IList<AtomicReaderContext> leaves = new List<AtomicReaderContext>();
            private int leafDocBase = 0;

            public Builder(CompositeReader reader)
            {
                this.reader = reader;
            }

            public CompositeReaderContext Build()
            {
                return (CompositeReaderContext)Build(null, reader, 0, 0);
            }

            private IndexReaderContext Build(CompositeReaderContext parent, IndexReader reader, int ord, int docBase)
            {
                if (reader is AtomicReader)
                {
                    AtomicReader ar = (AtomicReader)reader;
                    AtomicReaderContext atomic = new AtomicReaderContext(parent, ar, ord, docBase, leaves.Count, leafDocBase);
                    leaves.Add(atomic);
                    leafDocBase += reader.MaxDoc;
                    return atomic;
                }
                else
                {
                    CompositeReader cr = (CompositeReader)reader;
                    var sequentialSubReaders = cr.GetSequentialSubReaders();
                    List<IndexReaderContext> children = new IndexReaderContext[sequentialSubReaders.Count].ToList();
                    CompositeReaderContext newParent;
                    if (parent == null)
                    {
                        newParent = new CompositeReaderContext(cr, children, leaves);
                    }
                    else
                    {
                        newParent = new CompositeReaderContext(parent, cr, ord, docBase, children);
                    }
                    int newDocBase = 0;
                    for (int i = 0, c = sequentialSubReaders.Count; i < c; i++)
                    {
                        IndexReader r = sequentialSubReaders[i];
                        children[i] = Build(newParent, r, i, newDocBase);
                        newDocBase += r.MaxDoc;
                    }
                    //assert newDocBase == cr.maxDoc();
                    return newParent;
                }
            }
        }

    }
}
