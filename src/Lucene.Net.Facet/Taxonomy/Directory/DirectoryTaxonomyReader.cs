// Lucene version compatibility level 4.8.1
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Lucene.Net.Facet.Taxonomy.Directory
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using Document = Lucene.Net.Documents.Document;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using MultiFields = Lucene.Net.Index.MultiFields;

    /// <summary>
    /// A <see cref="TaxonomyReader"/> which retrieves stored taxonomy information from a
    /// <see cref="Directory"/>.
    /// <para/>
    /// Reading from the on-disk index on every method call is too slow, so this
    /// implementation employs caching: Some methods cache recent requests and their
    /// results, while other methods prefetch all the data into memory and then
    /// provide answers directly from in-memory tables. See the documentation of
    /// individual methods for comments on their performance.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class DirectoryTaxonomyReader : TaxonomyReader
    {
        /// <summary>
        /// LUCENENET specific class to make an <see cref="int"/> type into a reference type.
        /// </summary>
        private class Int32Class
        {
            /// <summary>
            /// NOTE: This was intItem (field) in Lucene
            /// </summary>
            public int Value { get; private set; }

            public Int32Class(int value)
            {
                Value = value;
            }

            public static implicit operator int(Int32Class integer) => integer.Value;
            public static implicit operator Int32Class(int integer) => new Int32Class(integer);
        }
        private const int DEFAULT_CACHE_VALUE = 4000;

        private readonly DirectoryTaxonomyWriter taxoWriter;
        private readonly long taxoEpoch; // used in doOpenIfChanged
        private readonly DirectoryReader indexReader;

        // TODO: test DoubleBarrelLRUCache and consider using it instead
        private LruDictionary<FacetLabel, Int32Class> ordinalCache;
        private LruDictionary<int, FacetLabel> categoryCache;
        private readonly ReaderWriterLockSlim ordinalCacheLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private readonly ReaderWriterLockSlim categoryCacheLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        private /*volatile*/ TaxonomyIndexArrays taxoArrays; // LUCENENET specific: LazyInitalizer negates the need for volatile
        private bool isDisposed = false;

        /// <summary>
        /// Called only from <see cref="DoOpenIfChanged()"/>. If the taxonomy has been
        /// recreated, you should pass <c>null</c> as the caches and parent/children
        /// arrays.
        /// </summary>
        private DirectoryTaxonomyReader(DirectoryReader indexReader, DirectoryTaxonomyWriter taxoWriter, 
            LruDictionary<FacetLabel, Int32Class> ordinalCache, LruDictionary<int, FacetLabel> categoryCache, 
            TaxonomyIndexArrays taxoArrays)
        {
            this.indexReader = indexReader;
            this.taxoWriter = taxoWriter;
            this.taxoEpoch = taxoWriter is null ? -1 : taxoWriter.TaxonomyEpoch;

            // use the same instance of the cache, note the protective code in getOrdinal and getPath
            this.ordinalCache = ordinalCache ?? new LruDictionary<FacetLabel, Int32Class>(DEFAULT_CACHE_VALUE);
            this.categoryCache = categoryCache ?? new LruDictionary<int, FacetLabel>(DEFAULT_CACHE_VALUE);

            this.taxoArrays = taxoArrays != null ? new TaxonomyIndexArrays(indexReader, taxoArrays) : null;
        }

        /// <summary>
        /// Open for reading a taxonomy stored in a given <see cref="Directory"/>. Uses <see cref="DirectoryTaxonomyIndexReaderFactory.Default"/> as
        /// for <see cref="DirectoryTaxonomyIndexReaderFactory"/>.
        /// </summary>
        /// <param name="directory"> The <see cref="Directory"/> in which the taxonomy resides. </param>
        /// <exception cref="Index.CorruptIndexException"> if the Taxonomy is corrupt. </exception>
        /// <exception cref="IOException"> if another error occurred. </exception>
        public DirectoryTaxonomyReader(Directory directory)
            : this(DirectoryTaxonomyIndexReaderFactory.Default, directory)
        {
        }

        /// <summary>
        /// Open for reading a taxonomy stored in a given <see cref="Directory"/>.
        /// </summary>
        /// <param name="indexReaderFactory"> The <see cref="DirectoryTaxonomyIndexReaderFactory"/> to use to open the index reader. </param>
        /// <param name="directory"> The <see cref="Directory"/> in which the taxonomy resides. </param>
        /// <exception cref="Index.CorruptIndexException"> if the Taxonomy is corrupt. </exception>
        /// <exception cref="IOException"> if another error occurred. </exception>
        public DirectoryTaxonomyReader(DirectoryTaxonomyIndexReaderFactory indexReaderFactory, Directory directory)
        {
            // LUCENENET specific - uses indexReaderFactory to open the index reader instead of
            // calling virtual method
            if (indexReaderFactory is null) throw new ArgumentNullException(nameof(indexReaderFactory));
            indexReader = indexReaderFactory.OpenIndexReader(directory);
            taxoWriter = null;
            taxoEpoch = -1;

            // These are the default cache sizes; they can be configured after
            // construction with the cache's setMaxSize() method

            ordinalCache = new LruDictionary<FacetLabel, Int32Class>(DEFAULT_CACHE_VALUE);
            categoryCache = new LruDictionary<int, FacetLabel>(DEFAULT_CACHE_VALUE);
        }

        /// <summary>
        /// Opens a <see cref="DirectoryTaxonomyReader"/> over the given
        /// <see cref="DirectoryTaxonomyWriter"/> (for NRT). Uses <see cref="DirectoryTaxonomyIndexReaderFactory.Default"/>
        /// for <see cref="DirectoryTaxonomyIndexReaderFactory"/>.
        /// </summary>
        /// <param name="taxoWriter">
        ///          The <see cref="DirectoryTaxonomyWriter"/> from which to obtain newly
        ///          added categories, in real-time. </param>
        public DirectoryTaxonomyReader(DirectoryTaxonomyWriter taxoWriter)
            : this(DirectoryTaxonomyIndexReaderFactory.Default, taxoWriter)
        {
        }

        /// <summary>
        /// Opens a <see cref="DirectoryTaxonomyReader"/> over the given
        /// <see cref="DirectoryTaxonomyWriter"/> (for NRT).
        /// </summary>
        /// <param name="indexReaderFactory"> The <see cref="DirectoryTaxonomyIndexReaderFactory"/> to use to open the index reader. </param>
        /// <param name="taxoWriter">
        ///          The <see cref="DirectoryTaxonomyWriter"/> from which to obtain newly
        ///          added categories, in real-time. </param>
        /// <exception cref="ArgumentNullException"> if <paramref name="indexReaderFactory"/> or <paramref name="taxoWriter"/> is <c>null</c>. </exception>
        public DirectoryTaxonomyReader(DirectoryTaxonomyIndexReaderFactory indexReaderFactory, DirectoryTaxonomyWriter taxoWriter)
        {
            // LUCENENET added null checks
            if (taxoWriter is null) throw new ArgumentNullException(nameof(taxoWriter));
            this.taxoWriter = taxoWriter;
            taxoEpoch = taxoWriter.TaxonomyEpoch;
            
            // LUCENENET specific - uses indexReaderFactory to open the index reader instead of
            // calling virtual method
            if (indexReaderFactory is null) throw new ArgumentNullException(nameof(indexReaderFactory));
            indexReader = indexReaderFactory.OpenIndexReader(taxoWriter.InternalIndexWriter);

            // These are the default cache sizes; they can be configured after
            // construction with the cache's setMaxSize() method

            ordinalCache = new LruDictionary<FacetLabel, Int32Class>(DEFAULT_CACHE_VALUE);
            categoryCache = new LruDictionary<int, FacetLabel>(DEFAULT_CACHE_VALUE);
        }

        // LUCENENET specific - eliminated the InitTaxoArrays() method in favor of LazyInitializer

        protected override void Dispose(bool disposing) // LUCENENET specific - changed from DoClose()
        {
            if (disposing && !isDisposed)
            {
                indexReader.Dispose();
                taxoArrays = null;
                // do not clear() the caches, as they may be used by other DTR instances.
                ordinalCache = null;
                categoryCache = null;
                ordinalCacheLock.Dispose(); // LUCENENET specific - cleanup ReaderWriterLockSlim instances
                categoryCacheLock.Dispose();
                isDisposed = true;
            }
        }

        /// <summary>
        /// Implements the opening of a new <see cref="DirectoryTaxonomyReader"/> instance if
        /// the taxonomy has changed.
        /// 
        /// <para>
        /// <b>NOTE:</b> the returned <see cref="DirectoryTaxonomyReader"/> shares the
        /// ordinal and category caches with this reader. This is not expected to cause
        /// any issues, unless the two instances continue to live. The reader
        /// guarantees that the two instances cannot affect each other in terms of
        /// correctness of the caches, however if the size of the cache is changed
        /// through <see cref="SetCacheSize(int)"/>, it will affect both reader instances.
        /// </para>
        /// </summary>
        protected override TaxonomyReader DoOpenIfChanged()
        {
            EnsureOpen();

            // This works for both NRT and non-NRT readers (i.e. an NRT reader remains NRT).
            var r2 = DirectoryReader.OpenIfChanged(indexReader);
            if (r2 is null)
            {
                return null; // no changes, nothing to do
            }

            // check if the taxonomy was recreated
            bool success = false;
            try
            {
                bool recreated = false;
                if (taxoWriter is null)
                {
                    // not NRT, check epoch from commit data
                    string t1 = indexReader.IndexCommit.UserData[DirectoryTaxonomyWriter.INDEX_EPOCH];
                    string t2 = r2.IndexCommit.UserData[DirectoryTaxonomyWriter.INDEX_EPOCH];
                    if (t1 is null)
                    {
                        if (t2 != null)
                        {
                            recreated = true;
                        }
                    }
                    else if (!t1.Equals(t2, StringComparison.Ordinal))
                    {
                        // t1 != null and t2 cannot be null b/c DirTaxoWriter always puts the commit data.
                        // it's ok to use String.equals because we require the two epoch values to be the same.
                        recreated = true;
                    }
                }
                else
                {
                    // NRT, compare current taxoWriter.epoch() vs the one that was given at construction
                    if (taxoEpoch != taxoWriter.TaxonomyEpoch)
                    {
                        recreated = true;
                    }
                }

                DirectoryTaxonomyReader newtr;
                if (recreated)
                {
                    // if recreated, do not reuse anything from this instace. the information
                    // will be lazily computed by the new instance when needed.
                    newtr = new DirectoryTaxonomyReader(r2, taxoWriter, null, null, null);
                }
                else
                {
                    newtr = new DirectoryTaxonomyReader(r2, taxoWriter, ordinalCache, categoryCache, taxoArrays);
                }

                success = true;
                return newtr;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(r2);
                }
            }
        }

        /// LUCENENET specific - OpenIndexReader(Directory directory) and
        /// OpenIndexReader(IndexWriter writer) were
        /// moved to <see cref="DirectoryTaxonomyIndexReaderFactory"/> to allow extended classes
        /// to customize writer behavior. This is a breaking change from Lucene, and required
        /// in order to offer the same functionality in .NET as Lucene offers in Java. These virtual methods
        /// were being called from the constructors and have different initialization sequence in .NET
        /// so a factory approach was used instead.

        /// <summary>
        /// Expert: returns the underlying <see cref="DirectoryReader"/> instance that is
        /// used by this <see cref="TaxonomyReader"/>.
        /// </summary>
        internal virtual DirectoryReader InternalIndexReader
        {
            get
            {
                EnsureOpen();
                return indexReader;
            }
        }

        public override ParallelTaxonomyArrays ParallelTaxonomyArrays
        {
            get
            {
                EnsureOpen();

                // LUCENENET specific - eliminated the InitTaxoArrays() method in favor of LazyInitializer
                if (null == taxoArrays)
                    return LazyInitializer.EnsureInitialized(ref taxoArrays, () => new TaxonomyIndexArrays(indexReader));

                return taxoArrays;
            }
        }

        public override IDictionary<string, string> CommitUserData
        {
            get
            {
                EnsureOpen();
                return indexReader.IndexCommit.UserData;
            }
        }

        public override int GetOrdinal(FacetLabel cp)
        {
            EnsureOpen();
            if (cp.Length == 0)
            {
                return ROOT_ORDINAL;
            }

            // First try to find the answer in the LRU cache:

            // LUCENENET: Despite LRUHashMap being thread-safe, we get much better performance
            // if reads are separated from writes.
            ordinalCacheLock.EnterReadLock();
            try
            {
                if (ordinalCache.TryGetValue(cp, out Int32Class res))
                {
                    if (res < indexReader.MaxDoc)
                    {
                        // Since the cache is shared with DTR instances allocated from
                        // doOpenIfChanged, we need to ensure that the ordinal is one that
                        // this DTR instance recognizes.
                        return res;
                    }
                    else
                    {
                        // if we get here, it means that the category was found in the cache,
                        // but is not recognized by this TR instance. Therefore there's no
                        // need to continue search for the path on disk, because we won't find
                        // it there too.
                        return TaxonomyReader.INVALID_ORDINAL;
                    }
                }
            }
            finally
            {
                ordinalCacheLock.ExitReadLock();
            }

            // If we're still here, we have a cache miss. We need to fetch the
            // value from disk, and then also put it in the cache:
            int ret = TaxonomyReader.INVALID_ORDINAL;
            DocsEnum docs = MultiFields.GetTermDocsEnum(indexReader, null, Consts.FULL, new BytesRef(FacetsConfig.PathToString(cp.Components, cp.Length)), 0);
            if (docs != null && docs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
            {
                ret = docs.DocID;

                // we only store the fact that a category exists, not its inexistence.
                // This is required because the caches are shared with new DTR instances
                // that are allocated from doOpenIfChanged. Therefore, if we only store
                // information about found categories, we cannot accidently tell a new
                // generation of DTR that a category does not exist.

                ordinalCacheLock.EnterWriteLock();
                try
                {
                    ordinalCache[cp] = ret;
                }
                finally
                {
                    ordinalCacheLock.ExitWriteLock();
                }
            }

            return ret;
        }

        public override FacetLabel GetPath(int ordinal)
        {
            EnsureOpen();

            // Since the cache is shared with DTR instances allocated from
            // doOpenIfChanged, we need to ensure that the ordinal is one that this DTR
            // instance recognizes. Therefore we do this check up front, before we hit
            // the cache.
            if (ordinal < 0 || ordinal >= indexReader.MaxDoc)
            {
                return null;
            }

            // TODO: can we use an int-based hash impl, such as IntToObjectMap,
            // wrapped as LRU?

            // LUCENENET NOTE: We don't need to convert ordinal from int to int here as was done in Java.
            // LUCENENET: Despite LRUHashMap being thread-safe, we get much better performance
            // if reads are separated from writes.
            categoryCacheLock.EnterReadLock();
            try
            {
                if (categoryCache.TryGetValue(ordinal, out FacetLabel res))
                    return res;
            }
            finally
            {
                categoryCacheLock.ExitReadLock();
            }

            Document doc = indexReader.Document(ordinal);
            var result = new FacetLabel(FacetsConfig.StringToPath(doc.Get(Consts.FULL)));
            categoryCacheLock.EnterWriteLock();
            try
            {
                categoryCache[ordinal] = result;
            }
            finally
            {
                categoryCacheLock.ExitWriteLock();
            }

            return result;
        }

        public override int Count
        {
            get
            {
                EnsureOpen();
                return indexReader.NumDocs;
            }
        }

        /// <summary>
        /// <see cref="SetCacheSize"/> controls the maximum allowed size of each of the caches
        /// used by <see cref="GetPath(int)"/> and <see cref="GetOrdinal(FacetLabel)"/>.
        /// <para/>
        /// Currently, if the given size is smaller than the current size of
        /// a cache, it will not shrink, and rather we be limited to its current
        /// size. </summary>
        /// <param name="size"> The new maximum cache size, in number of entries. </param>
        public virtual void SetCacheSize(int size)
        {
            EnsureOpen();
            categoryCacheLock.EnterWriteLock();
            try
            {
                categoryCache.Limit = size;
            }
            finally
            {
                categoryCacheLock.ExitWriteLock();
            }
            ordinalCacheLock.EnterWriteLock();
            try
            {
                ordinalCache.Limit = size;
            }
            finally
            {
                ordinalCacheLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Returns ordinal -> label mapping, up to the provided
        /// max ordinal or number of ordinals, whichever is
        /// smaller. 
        /// </summary>
        public virtual string ToString(int max)
        {
            EnsureOpen();
            StringBuilder sb = new StringBuilder();
            int upperl = Math.Min(max, indexReader.MaxDoc);
            for (int i = 0; i < upperl; i++)
            {
                try
                {
                    FacetLabel category = this.GetPath(i);
                    if (category is null)
                    {
                        sb.Append(i + ": NULL!! \n");
                        continue;
                    }
                    if (category.Length == 0)
                    {
                        sb.Append(i + ": EMPTY STRING!! \n");
                        continue;
                    }
                    sb.Append(i + ": " + category.ToString() + "\n");
                }
                catch (Exception e) when (e.IsIOException())
                {
                    // LUCENENET TODO: Should we use a 3rd party logging library?

                    // LUCENENET specific - using System.Diagnostics.Trace rather than using a logging library as a workaround.
                    System.Diagnostics.Trace.WriteLine(e.ToString(), "FINEST");
                }
            }
            return sb.ToString();
        }
    }
}