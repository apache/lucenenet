using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Index
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// <see cref="IndexReaderContext"/> for <see cref="CompositeReader"/> instance.
    /// </summary>
    public sealed class CompositeReaderContext : IndexReaderContext
    {
        private readonly IList<IndexReaderContext> children;
        private readonly IList<AtomicReaderContext> leaves;
        private readonly CompositeReader reader;

        internal static CompositeReaderContext Create(CompositeReader reader)
        {
            return (new Builder(reader)).Build();
        }

        /// <summary>
        /// Creates a <see cref="CompositeReaderContext"/> for intermediate readers that aren't
        /// not top-level readers in the current context
        /// </summary>
        internal CompositeReaderContext(CompositeReaderContext parent, CompositeReader reader, int ordInParent, int docbaseInParent, IList<IndexReaderContext> children)
            : this(parent, reader, ordInParent, docbaseInParent, children, null)
        {
        }

        /// <summary>
        /// Creates a <see cref="CompositeReaderContext"/> for top-level readers with parent set to <c>null</c>
        /// </summary>
        internal CompositeReaderContext(CompositeReader reader, IList<IndexReaderContext> children, IList<AtomicReaderContext> leaves)
            : this(null, reader, 0, 0, children, leaves)
        {
        }

        private CompositeReaderContext(CompositeReaderContext parent, CompositeReader reader, int ordInParent, int docbaseInParent, IList<IndexReaderContext> children, IList<AtomicReaderContext> leaves)
            : base(parent, ordInParent, docbaseInParent)
        {
            this.children = children.AsReadOnly();
            this.leaves = leaves;
            this.reader = reader;
        }

        public override IList<AtomicReaderContext> Leaves
        {
            get
            {
                if (!IsTopLevel)
                {
                    throw UnsupportedOperationException.Create("this is not a top-level context.");
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(leaves != null);
                return leaves;
            }
        }

        public override IList<IndexReaderContext> Children => children;

        public override IndexReader Reader => reader;

        public sealed class Builder
        {
            private readonly CompositeReader reader;
            private readonly IList<AtomicReaderContext> leaves = new JCG.List<AtomicReaderContext>();
            private int leafDocBase = 0;

            public Builder(CompositeReader reader)
            {
                this.reader = reader;
            }

            public CompositeReaderContext Build()
            {
                return (CompositeReaderContext)Build(null, reader, 0, 0);
            }

            internal IndexReaderContext Build(CompositeReaderContext parent, IndexReader reader, int ord, int docBase)
            {
                if (reader is AtomicReader ar)
                {
                    var atomic = new AtomicReaderContext(parent, ar, ord, docBase, leaves.Count, leafDocBase);
                    leaves.Add(atomic);
                    leafDocBase += reader.MaxDoc;
                    return atomic;
                }
                else
                {
                    CompositeReader cr = (CompositeReader)reader;
                    var sequentialSubReaders = cr.GetSequentialSubReaders();
                    var children = new IndexReaderContext[sequentialSubReaders.Count];
                    CompositeReaderContext newParent;
                    if (parent is null)
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
                    if (Debugging.AssertsEnabled) Debugging.Assert(newDocBase == cr.MaxDoc);
                    return newParent;
                }
            }
        }
    }
}