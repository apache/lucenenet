using Lucene.Net.Diagnostics;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
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
    /// Split an index based on a <see cref="Filter"/>.
    /// </summary>
    public class PKIndexSplitter
    {
        private readonly Filter docsInFirstIndex;
        private readonly Directory input;
        private readonly Directory dir1;
        private readonly Directory dir2;
        private readonly IndexWriterConfig config1;
        private readonly IndexWriterConfig config2;

        /// <summary>
        /// Split an index based on a <see cref="Filter"/>. All documents that match the filter
        /// are sent to dir1, remaining ones to dir2.
        /// </summary>
        public PKIndexSplitter(LuceneVersion version, Directory input, Directory dir1, Directory dir2, Filter docsInFirstIndex)
              : this(input, dir1, dir2, docsInFirstIndex, NewDefaultConfig(version), NewDefaultConfig(version))
        {
        }

        private static IndexWriterConfig NewDefaultConfig(LuceneVersion version)
        {
            return (new IndexWriterConfig(version, null) { OpenMode = OpenMode.CREATE });
        }

        public PKIndexSplitter(Directory input, Directory dir1, Directory dir2, Filter docsInFirstIndex, IndexWriterConfig config1, IndexWriterConfig config2)
        {
            this.input = input;
            this.dir1 = dir1;
            this.dir2 = dir2;
            this.docsInFirstIndex = docsInFirstIndex;
            this.config1 = config1;
            this.config2 = config2;
        }

        /// <summary>
        /// Split an index based on a  given primary key term 
        /// and a 'middle' term.  If the middle term is present, it's
        /// sent to dir2.
        /// </summary>
        public PKIndexSplitter(LuceneVersion version, Directory input, Directory dir1, Directory dir2, Term midTerm)
              : this(version, input, dir1, dir2, new TermRangeFilter(midTerm.Field, null, midTerm.Bytes, true, false))
        {
        }

        public PKIndexSplitter(Directory input, Directory dir1, Directory dir2, Term midTerm, IndexWriterConfig config1, IndexWriterConfig config2)
              : this(input, dir1, dir2, new TermRangeFilter(midTerm.Field, null, midTerm.Bytes, true, false), config1, config2)
        {
        }

        public virtual void Split()
        {
            bool success = false;
            DirectoryReader reader = DirectoryReader.Open(input);
            try
            {
                // pass an individual config in here since one config can not be reused!
                CreateIndex(config1, dir1, reader, docsInFirstIndex, false);
                CreateIndex(config2, dir2, reader, docsInFirstIndex, true);
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Dispose(reader);
                }
                else
                {
                    IOUtils.DisposeWhileHandlingException(reader);
                }
            }
        }

        private static void CreateIndex(IndexWriterConfig config, Directory target, IndexReader reader, Filter preserveFilter, bool negateFilter) // LUCENENET: CA1822: Mark members as static
        {
            bool success = false;
            IndexWriter w = new IndexWriter(target, config);
            try
            {
                IList<AtomicReaderContext> leaves = reader.Leaves;
                IndexReader[] subReaders = new IndexReader[leaves.Count];
                int i = 0;
                foreach (AtomicReaderContext ctx in leaves)
                {
                    subReaders[i++] = new DocumentFilteredAtomicIndexReader(ctx, preserveFilter, negateFilter);
                }
                w.AddIndexes(subReaders);
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Dispose(w);
                }
                else
                {
                    IOUtils.DisposeWhileHandlingException(w);
                }
            }
        }

        private class DocumentFilteredAtomicIndexReader : FilterAtomicReader
        {
            internal readonly IBits liveDocs;
            internal readonly int numDocs;

            public DocumentFilteredAtomicIndexReader(AtomicReaderContext context, Filter preserveFilter, bool negateFilter)
                    : base(context.AtomicReader)
            {
                int maxDoc = m_input.MaxDoc;
                FixedBitSet bits = new FixedBitSet(maxDoc);
                // ignore livedocs here, as we filter them later:
                DocIdSet docs = preserveFilter.GetDocIdSet(context, null);
                if (docs != null)
                {
                    DocIdSetIterator it = docs.GetIterator();
                    if (it != null)
                    {
                        bits.Or(it);
                    }
                }
                if (negateFilter)
                {
                    bits.Flip(0, maxDoc);
                }

                if (m_input.HasDeletions)
                {
                    IBits oldLiveDocs = m_input.LiveDocs;
                    if (Debugging.AssertsEnabled) Debugging.Assert(oldLiveDocs != null);
                    DocIdSetIterator it = bits.GetIterator();
                    for (int i = it.NextDoc(); i < maxDoc; i = it.NextDoc())
                    {
                        if (!oldLiveDocs.Get(i))
                        {
                            // we can safely modify the current bit, as the iterator already stepped over it:
                            bits.Clear(i);
                        }
                    }
                }

                this.liveDocs = bits;
                this.numDocs = bits.Cardinality;
            }

            public override int NumDocs => numDocs;

            public override IBits LiveDocs => liveDocs;
        }
    }
}