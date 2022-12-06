using J2N.Runtime.CompilerServices;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
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
    /// A <see cref="CompositeReader"/> which reads multiple, parallel indexes.  Each index added
    /// must have the same number of documents, and exactly the same hierarchical subreader structure,
    /// but typically each contains different fields. Deletions are taken from the first reader.
    /// Each document contains the union of the fields of all
    /// documents with the same document number.  When searching, matches for a
    /// query term are from the first index added that has the field.
    ///
    /// <para/>This is useful, e.g., with collections that have large fields which
    /// change rarely and small fields that change more frequently.  The smaller
    /// fields may be re-indexed in a new index and both indexes may be searched
    /// together.
    ///
    /// <para/><strong>Warning:</strong> It is up to you to make sure all indexes
    /// are created and modified the same way. For example, if you add
    /// documents to one index, you need to add the same documents in the
    /// same order to the other indexes. <em>Failure to do so will result in
    /// undefined behavior</em>.
    /// A good strategy to create suitable indexes with <see cref="IndexWriter"/> is to use
    /// <see cref="LogDocMergePolicy"/>, as this one does not reorder documents
    /// during merging (like <see cref="TieredMergePolicy"/>) and triggers merges
    /// by number of documents per segment. If you use different <see cref="MergePolicy"/>s
    /// it might happen that the segment structure of your index is no longer predictable.
    /// </summary>
    public class ParallelCompositeReader : BaseCompositeReader<IndexReader>
    {
        private readonly bool closeSubReaders;
        private readonly ISet<IndexReader> completeReaderSet = new JCG.HashSet<IndexReader>(IdentityEqualityComparer<IndexReader>.Default);

        /// <summary>
        /// Create a <see cref="ParallelCompositeReader"/> based on the provided
        /// readers; auto-disposes the given <paramref name="readers"/> on <see cref="IndexReader.Dispose()"/>.
        /// </summary>
        public ParallelCompositeReader(params CompositeReader[] readers)
            : this(true, readers)
        {
        }

        /// <summary>
        /// Create a <see cref="ParallelCompositeReader"/> based on the provided
        /// <paramref name="readers"/>.
        /// </summary>
        public ParallelCompositeReader(bool closeSubReaders, params CompositeReader[] readers)
            : this(closeSubReaders, readers, readers)
        {
        }

        /// <summary>
        /// Expert: create a <see cref="ParallelCompositeReader"/> based on the provided
        /// <paramref name="readers"/> and <paramref name="storedFieldReaders"/>; when a document is
        /// loaded, only <paramref name="storedFieldReaders"/> will be used.
        /// </summary>
        public ParallelCompositeReader(bool closeSubReaders, CompositeReader[] readers, CompositeReader[] storedFieldReaders)
            : base(PrepareSubReaders(readers, storedFieldReaders))
        {
            this.closeSubReaders = closeSubReaders;
            completeReaderSet.UnionWith(readers);
            completeReaderSet.UnionWith(storedFieldReaders);
            // update ref-counts (like MultiReader):
            if (!closeSubReaders)
            {
                foreach (IndexReader reader in completeReaderSet)
                {
                    reader.IncRef();
                }
            }
            // finally add our own synthetic readers, so we close or decRef them, too (it does not matter what we do)
            completeReaderSet.UnionWith(GetSequentialSubReaders());
        }

        private static IndexReader[] PrepareSubReaders(CompositeReader[] readers, CompositeReader[] storedFieldsReaders)
        {
            if (readers.Length == 0)
            {
                if (storedFieldsReaders.Length > 0)
                {
                    throw new ArgumentException("There must be at least one main reader if storedFieldsReaders are used.");
                }
                // LUCENENET: Optimized empty string array creation
                return Arrays.Empty<IndexReader>();
            }
            else
            {
                IList<IndexReader> firstSubReaders = readers[0].GetSequentialSubReaders();

                // check compatibility:
                int maxDoc = readers[0].MaxDoc, noSubs = firstSubReaders.Count;
                int[] childMaxDoc = new int[noSubs];
                bool[] childAtomic = new bool[noSubs];
                for (int i = 0; i < noSubs; i++)
                {
                    IndexReader r = firstSubReaders[i];
                    childMaxDoc[i] = r.MaxDoc;
                    childAtomic[i] = r is AtomicReader;
                }
                Validate(readers, maxDoc, childMaxDoc, childAtomic);
                Validate(storedFieldsReaders, maxDoc, childMaxDoc, childAtomic);

                // hierarchically build the same subreader structure as the first CompositeReader with Parallel*Readers:
                IndexReader[] subReaders = new IndexReader[noSubs];
                for (int i = 0; i < subReaders.Length; i++)
                {
                    if (firstSubReaders[i] is AtomicReader)
                    {
                        AtomicReader[] atomicSubs = new AtomicReader[readers.Length];
                        for (int j = 0; j < readers.Length; j++)
                        {
                            atomicSubs[j] = (AtomicReader)readers[j].GetSequentialSubReaders()[i];
                        }
                        AtomicReader[] storedSubs = new AtomicReader[storedFieldsReaders.Length];
                        for (int j = 0; j < storedFieldsReaders.Length; j++)
                        {
                            storedSubs[j] = (AtomicReader)storedFieldsReaders[j].GetSequentialSubReaders()[i];
                        }
                        // We pass true for closeSubs and we prevent closing of subreaders in doClose():
                        // By this the synthetic throw-away readers used here are completely invisible to ref-counting
                        subReaders[i] = new ParallelAtomicReaderAnonymousClass(atomicSubs, storedSubs);
                    }
                    else
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(firstSubReaders[i] is CompositeReader);
                        CompositeReader[] compositeSubs = new CompositeReader[readers.Length];
                        for (int j = 0; j < readers.Length; j++)
                        {
                            compositeSubs[j] = (CompositeReader)readers[j].GetSequentialSubReaders()[i];
                        }
                        CompositeReader[] storedSubs = new CompositeReader[storedFieldsReaders.Length];
                        for (int j = 0; j < storedFieldsReaders.Length; j++)
                        {
                            storedSubs[j] = (CompositeReader)storedFieldsReaders[j].GetSequentialSubReaders()[i];
                        }
                        // We pass true for closeSubs and we prevent closing of subreaders in doClose():
                        // By this the synthetic throw-away readers used here are completely invisible to ref-counting
                        subReaders[i] = new ParallelCompositeReaderAnonymousClass(compositeSubs, storedSubs);
                    }
                }
                return subReaders;
            }
        }

        private sealed class ParallelAtomicReaderAnonymousClass : ParallelAtomicReader
        {
            public ParallelAtomicReaderAnonymousClass(AtomicReader[] atomicSubs, AtomicReader[] storedSubs)
                : base(true, atomicSubs, storedSubs)
            {
            }

            protected internal override void DoClose()
            {
                // LUCENENET: Intentionally blank
            }
        }

        private sealed class ParallelCompositeReaderAnonymousClass : ParallelCompositeReader
        {
            public ParallelCompositeReaderAnonymousClass(CompositeReader[] compositeSubs, CompositeReader[] storedSubs)
                : base(true, compositeSubs, storedSubs)
            {
            }

            protected internal override void DoClose()
            {
                // LUCENENET: Intentionally blank
            }
        }

        private static void Validate(CompositeReader[] readers, int maxDoc, int[] childMaxDoc, bool[] childAtomic)
        {
            for (int i = 0; i < readers.Length; i++)
            {
                CompositeReader reader = readers[i];
                IList<IndexReader> subs = reader.GetSequentialSubReaders();
                if (reader.MaxDoc != maxDoc)
                {
                    throw new ArgumentException("All readers must have same MaxDoc: " + maxDoc + "!=" + reader.MaxDoc);
                }
                int noSubs = subs.Count;
                if (noSubs != childMaxDoc.Length)
                {
                    throw new ArgumentException("All readers must have same number of subReaders");
                }
                for (int subIDX = 0; subIDX < noSubs; subIDX++)
                {
                    IndexReader r = subs[subIDX];
                    if (r.MaxDoc != childMaxDoc[subIDX])
                    {
                        throw new ArgumentException("All readers must have same corresponding subReader maxDoc");
                    }
                    if (!(childAtomic[subIDX] ? (r is AtomicReader) : (r is CompositeReader)))
                    {
                        throw new ArgumentException("All readers must have same corresponding subReader types (atomic or composite)");
                    }
                }
            }
        }

        protected internal override void DoClose()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                Exception ioe = null; // LUCENENET: No need to cast to IOExcpetion
                foreach (IndexReader reader in completeReaderSet)
                {
                    try
                    {
                        if (closeSubReaders)
                        {
                            reader.Dispose();
                        }
                        else
                        {
                            reader.DecRef();
                        }
                    }
                    catch (Exception e) when (e.IsIOException())
                    {
                        if (ioe is null)
                        {
                            ioe = e;
                        }
                    }
                }
                // throw the first exception
                if (ioe != null)
                {
                    ExceptionDispatchInfo.Capture(ioe).Throw(); // LUCENENET: Rethrow to preserve stack details from the original throw
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }
    }
}