using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lucene.Net.Store;

namespace Lucene.Net.Facet.Taxonomy.Directory
{
    using Document = Lucene.Net.Documents.Document;
    using Lucene.Net.Facet.Taxonomy;
    using CorruptIndexException = Lucene.Net.Index.CorruptIndexException; // javadocs
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using MultiFields = Lucene.Net.Index.MultiFields;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Directory = Lucene.Net.Store.Directory;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using IOUtils = Lucene.Net.Util.IOUtils;

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
    /// A <seealso cref="TaxonomyReader"/> which retrieves stored taxonomy information from a
    /// <seealso cref="Directory"/>.
    /// <P>
    /// Reading from the on-disk index on every method call is too slow, so this
    /// implementation employs caching: Some methods cache recent requests and their
    /// results, while other methods prefetch all the data into memory and then
    /// provide answers directly from in-memory tables. See the documentation of
    /// individual methods for comments on their performance.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class DirectoryTaxonomyReader : TaxonomyReader, IDisposable
    {

        public class IntClass
        {
            public int? IntItem { get; set; }
        }
        private const int DEFAULT_CACHE_VALUE = 4000;

        private readonly DirectoryTaxonomyWriter taxoWriter;
        private readonly long taxoEpoch; // used in doOpenIfChanged
        private readonly DirectoryReader indexReader;

        // TODO: test DoubleBarrelLRUCache and consider using it instead
        private LRUHashMap<FacetLabel, IntClass> ordinalCache;
        private LRUHashMap<int, FacetLabel> categoryCache;

        private volatile TaxonomyIndexArrays taxoArrays;

        /// <summary>
        /// Called only from <seealso cref="#doOpenIfChanged()"/>. If the taxonomy has been
        /// recreated, you should pass {@code null} as the caches and parent/children
        /// arrays.
        /// </summary>
        internal DirectoryTaxonomyReader(DirectoryReader indexReader, DirectoryTaxonomyWriter taxoWriter, LRUHashMap<FacetLabel, IntClass> ordinalCache, LRUHashMap<int, FacetLabel> categoryCache, TaxonomyIndexArrays taxoArrays)
        {
            this.indexReader = indexReader;
            this.taxoWriter = taxoWriter;
            this.taxoEpoch = taxoWriter == null ? -1 : taxoWriter.TaxonomyEpoch;

            // use the same instance of the cache, note the protective code in getOrdinal and getPath
            this.ordinalCache = ordinalCache == null ? new LRUHashMap<FacetLabel, IntClass>(DEFAULT_CACHE_VALUE) : ordinalCache;
            this.categoryCache = categoryCache == null ? new LRUHashMap<int, FacetLabel>(DEFAULT_CACHE_VALUE) : categoryCache;

            this.taxoArrays = taxoArrays != null ? new TaxonomyIndexArrays(indexReader, taxoArrays) : null;
        }

        /// <summary>
        /// Open for reading a taxonomy stored in a given <seealso cref="Directory"/>.
        /// </summary>
        /// <param name="directory">
        ///          The <seealso cref="Directory"/> in which the taxonomy resides. </param>
        /// <exception cref="CorruptIndexException">
        ///           if the Taxonomy is corrupt. </exception>
        /// <exception cref="IOException">
        ///           if another error occurred. </exception>
        public DirectoryTaxonomyReader(Directory directory)
        {
            indexReader = OpenIndexReader(directory);
            taxoWriter = null;
            taxoEpoch = -1;

            // These are the default cache sizes; they can be configured after
            // construction with the cache's setMaxSize() method

            ordinalCache = new LRUHashMap<FacetLabel, IntClass>(DEFAULT_CACHE_VALUE);
            categoryCache = new LRUHashMap<int, FacetLabel>(DEFAULT_CACHE_VALUE);
        }

        /// <summary>
        /// Opens a <seealso cref="DirectoryTaxonomyReader"/> over the given
        /// <seealso cref="DirectoryTaxonomyWriter"/> (for NRT).
        /// </summary>
        /// <param name="taxoWriter">
        ///          The <seealso cref="DirectoryTaxonomyWriter"/> from which to obtain newly
        ///          added categories, in real-time. </param>
        public DirectoryTaxonomyReader(DirectoryTaxonomyWriter taxoWriter)
        {
            this.taxoWriter = taxoWriter;
            taxoEpoch = taxoWriter.TaxonomyEpoch;
            indexReader = OpenIndexReader(taxoWriter.InternalIndexWriter);

            // These are the default cache sizes; they can be configured after
            // construction with the cache's setMaxSize() method

            ordinalCache = new LRUHashMap<FacetLabel, IntClass>(DEFAULT_CACHE_VALUE);
            categoryCache = new LRUHashMap<int, FacetLabel>(DEFAULT_CACHE_VALUE);
        }

        private void InitTaxoArrays()
        {
            lock (this)
            {
                if (taxoArrays == null)
                {
                    // according to Java Concurrency in Practice, this might perform better on
                    // some JVMs, because the array initialization doesn't happen on the
                    // volatile member.
                    TaxonomyIndexArrays tmpArrays = new TaxonomyIndexArrays(indexReader);
                    taxoArrays = tmpArrays;
                }
            }
        }

        protected internal override void DoClose()
        {
            indexReader.Dispose();
            taxoArrays = null;
            // do not clear() the caches, as they may be used by other DTR instances.
            ordinalCache = null;
            categoryCache = null;
        }

        /// <summary>
        /// Implements the opening of a new <seealso cref="DirectoryTaxonomyReader"/> instance if
        /// the taxonomy has changed.
        /// 
        /// <para>
        /// <b>NOTE:</b> the returned <seealso cref="DirectoryTaxonomyReader"/> shares the
        /// ordinal and category caches with this reader. This is not expected to cause
        /// any issues, unless the two instances continue to live. The reader
        /// guarantees that the two instances cannot affect each other in terms of
        /// correctness of the caches, however if the size of the cache is changed
        /// through <seealso cref="#setCacheSize(int)"/>, it will affect both reader instances.
        /// </para>
        /// </summary>
        protected override TaxonomyReader DoOpenIfChanged()
        {
            EnsureOpen();

            // This works for both NRT and non-NRT readers (i.e. an NRT reader remains NRT).
            var r2 = DirectoryReader.OpenIfChanged(indexReader);
            if (r2 == null)
            {
                return null; // no changes, nothing to do
            }

            // check if the taxonomy was recreated
            bool success = false;
            try
            {
                bool recreated = false;
                if (taxoWriter == null)
                {
                    // not NRT, check epoch from commit data
                    string t1 = indexReader.IndexCommit.UserData[DirectoryTaxonomyWriter.INDEX_EPOCH];
                    string t2 = r2.IndexCommit.UserData[DirectoryTaxonomyWriter.INDEX_EPOCH];
                    if (t1 == null)
                    {
                        if (t2 != null)
                        {
                            recreated = true;
                        }
                    }
                    else if (!t1.Equals(t2))
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
                    IOUtils.CloseWhileHandlingException(r2);
                }
            }
        }

        /// <summary>
        /// Open the <seealso cref="DirectoryReader"/> from this {@link
        ///  Directory}. 
        /// </summary>
        protected virtual DirectoryReader OpenIndexReader(Directory directory)
        {
            return DirectoryReader.Open(directory);
        }

        /// <summary>
        /// Open the <seealso cref="DirectoryReader"/> from this {@link
        ///  IndexWriter}. 
        /// </summary>
        protected virtual DirectoryReader OpenIndexReader(IndexWriter writer)
        {
            return DirectoryReader.Open(writer, false);
        }

        /// <summary>
        /// Expert: returns the underlying <seealso cref="DirectoryReader"/> instance that is
        /// used by this <seealso cref="TaxonomyReader"/>.
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
                if (taxoArrays == null)
                {
                    InitTaxoArrays();
                }
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
            lock (ordinalCache)
            {
                IntClass res = ordinalCache.Get(cp);
                if (res != null && res.IntItem != null)
                {
                    if ((int)res.IntItem.Value < indexReader.MaxDoc)
                    {
                        // Since the cache is shared with DTR instances allocated from
                        // doOpenIfChanged, we need to ensure that the ordinal is one that
                        // this DTR instance recognizes.
                        return (int)res.IntItem.Value;
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

            // If we're still here, we have a cache miss. We need to fetch the
            // value from disk, and then also put it in the cache:
            int ret = TaxonomyReader.INVALID_ORDINAL;
            DocsEnum docs = MultiFields.GetTermDocsEnum(indexReader, null, Consts.FULL, new BytesRef(FacetsConfig.PathToString(cp.Components, cp.Length)), 0);
            if (docs != null && docs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
            {
                ret = docs.DocID();

                // we only store the fact that a category exists, not its inexistence.
                // This is required because the caches are shared with new DTR instances
                // that are allocated from doOpenIfChanged. Therefore, if we only store
                // information about found categories, we cannot accidently tell a new
                // generation of DTR that a category does not exist.
                lock (ordinalCache)
                {
                    ordinalCache.Put(cp, new IntClass { IntItem = Convert.ToInt32(ret) });
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
            int catIDInteger = Convert.ToInt32(ordinal);
            lock (categoryCache)
            {
                var res = categoryCache.Get(catIDInteger,false);
                if (res != null)
                {
                    return res;
                }
            }

            Document doc = indexReader.Document(ordinal);
            FacetLabel ret = new FacetLabel(FacetsConfig.StringToPath(doc.Get(Consts.FULL)));
            lock (categoryCache)
            {
                categoryCache.Put(catIDInteger, ret);
            }

            return ret;
        }

        public override int Size
        {
            get
            {
                EnsureOpen();
                return indexReader.NumDocs;
            }
        }

        /// <summary>
        /// setCacheSize controls the maximum allowed size of each of the caches
        /// used by <seealso cref="#getPath(int)"/> and <seealso cref="#getOrdinal(FacetLabel)"/>.
        /// <P>
        /// Currently, if the given size is smaller than the current size of
        /// a cache, it will not shrink, and rather we be limited to its current
        /// size. </summary>
        /// <param name="size"> the new maximum cache size, in number of entries. </param>
        public virtual int CacheSize
        {
            set
            {
                EnsureOpen();
                lock (categoryCache)
                {
                    categoryCache.MaxSize = value;
                }
                lock (ordinalCache)
                {
                    ordinalCache.MaxSize = value;
                }
            }
        }

        /// <summary>
        /// Returns ordinal -> label mapping, up to the provided
        ///  max ordinal or number of ordinals, whichever is
        ///  smaller. 
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
                    if (category == null)
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
                catch (IOException)
                {
                    throw;
                }
            }
            return sb.ToString();
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }

}