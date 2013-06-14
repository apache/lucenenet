using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public sealed class CompositeReaderContext : IndexReaderContext
    {
        private readonly List<IndexReaderContext> children;
        private readonly List<AtomicReaderContext> leaves;
        private readonly CompositeReader reader;

        internal static CompositeReaderContext Create(CompositeReader reader)
        {
            return new Builder(reader).build();
        }

        /**
         * Creates a {@link CompositeReaderContext} for intermediate readers that aren't
         * not top-level readers in the current context
         */
        internal CompositeReaderContext(CompositeReaderContext parent, CompositeReader reader, int ordInParent,
            int docbaseInParent, List<IndexReaderContext> children)
            : base(parent, reader, ordInParent, docbaseInParent, children, null)
        {
        }

        /**
         * Creates a {@link CompositeReaderContext} for top-level readers with parent set to <code>null</code>
         */
        internal CompositeReaderContext(CompositeReader reader, List<IndexReaderContext> children, List<AtomicReaderContext> leaves)
            : base(null, reader, 0, 0, children, leaves)
        {
        }

        private CompositeReaderContext(CompositeReaderContext parent, CompositeReader reader,
            int ordInParent, int docbaseInParent, List<IndexReaderContext> children, List<AtomicReaderContext> leaves)
            : base(parent, ordInParent, docbaseInParent)
        {
            this.children = Collections.unmodifiableList(children);
            this.leaves = leaves == null ? null : Collections.unmodifiableList(leaves);
            this.reader = reader;
        }


        public override List<AtomicReaderContext> Leaves()
        {
            if (!isTopLevel)
                throw new NotSupportedException("This is not a top-level context.");
            //assert leaves != null;
            return leaves;
        }



        public override List<IndexReaderContext> Children()
        {
            return children;
        }


        public override CompositeReader Reader()
        {
            return reader;
        }

        private class Builder
        {
            private readonly CompositeReader reader;
            private readonly List<AtomicReaderContext> leaves = new List<AtomicReaderContext>();
            private int leafDocBase = 0;

            public Builder(CompositeReader reader)
            {
                this.reader = reader;
            }

            public CompositeReaderContext build()
            {
                return (CompositeReaderContext)build(null, reader, 0, 0);
            }

            private IndexReaderContext build(CompositeReaderContext parent, IndexReader reader, int ord, int docBase)
            {
                if (reader.GetType() == typeof(AtomicReader))
                {
                    AtomicReader ar = (AtomicReader)reader;
                    AtomicReaderContext atomic = new AtomicReaderContext(parent, ar, ord, docBase, leaves.size(), leafDocBase);
                    leaves.Add(atomic);
                    leafDocBase += reader.MaxDoc;
                    return atomic;
                }
                else
                {
                    CompositeReader cr = (CompositeReader)reader;
                    var sequentialSubReaders = cr.GetSequentialSubReaders();
                    List<IndexReaderContext> children = new IndexReaderContext[sequentialSubReaders.Length].ToList();
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
                    for (int i = 0, c = sequentialSubReaders.Length; i < c; i++)
                    {
                        IndexReader r = sequentialSubReaders[i];
                        children.set(i, build(newParent, r, i, newDocBase));
                        newDocBase += r.MaxDoc;
                    }
                    //assert newDocBase == cr.maxDoc();
                    return newParent;
                }
            }
        }

    }
}
