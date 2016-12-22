using Lucene.Net.Support;
using System.Collections.Generic;
using System.Diagnostics;

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
    /// <seealso cref="IndexReaderContext"/> for <seealso cref="CompositeReader"/> instance.
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
        /// Creates a <seealso cref="CompositeReaderContext"/> for intermediate readers that aren't
        /// not top-level readers in the current context
        /// </summary>
        internal CompositeReaderContext(CompositeReaderContext parent, CompositeReader reader, int ordInParent, int docbaseInParent, List<IndexReaderContext> children)
            : this(parent, reader, ordInParent, docbaseInParent, children, null)
        {
        }

        /// <summary>
        /// Creates a <seealso cref="CompositeReaderContext"/> for top-level readers with parent set to <code>null</code>
        /// </summary>
        internal CompositeReaderContext(CompositeReader reader, List<IndexReaderContext> children, IList<AtomicReaderContext> leaves)
            : this(null, reader, 0, 0, children, leaves)
        {
        }

        private CompositeReaderContext(CompositeReaderContext parent, CompositeReader reader, int ordInParent, int docbaseInParent, List<IndexReaderContext> children, IList<AtomicReaderContext> leaves)
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
                    throw new System.NotSupportedException("this is not a top-level context.");
                }
                Debug.Assert(leaves != null);
                return leaves;
            }
        }

        public override IList<IndexReaderContext> Children
        {
            get { return children; }
        }

        public override IndexReader Reader
        {
            get { return reader; }
        }

        public sealed class Builder
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

            internal IndexReaderContext Build(CompositeReaderContext parent, IndexReader reader, int ord, int docBase)
            {
                var ar = reader as AtomicReader;
                if (ar != null)
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
                    var children = Arrays.AsList(new IndexReaderContext[sequentialSubReaders.Count]);
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
                    Debug.Assert(newDocBase == cr.MaxDoc);
                    return newParent;
                }
            }
        }
    }
}