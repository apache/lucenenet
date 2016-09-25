using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

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

    using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using LockObtainFailedException = Lucene.Net.Store.LockObtainFailedException; // javadocs
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Cl2oTaxonomyWriterCache = Lucene.Net.Facet.Taxonomy.WriterCache.Cl2oTaxonomyWriterCache;
    using Directory = Lucene.Net.Store.Directory;
    using CorruptIndexException = Lucene.Net.Index.CorruptIndexException; // javadocs
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using Document = Lucene.Net.Documents.Document;
    using Field = Lucene.Net.Documents.Field;
    using FieldType = Lucene.Net.Documents.FieldType;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using LogByteSizeMergePolicy = Lucene.Net.Index.LogByteSizeMergePolicy;
    using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
    using ReaderManager = Lucene.Net.Index.ReaderManager;
    using SegmentInfos = Lucene.Net.Index.SegmentInfos;
    using StringField = Lucene.Net.Documents.StringField;
    using ITaxonomyWriterCache = Lucene.Net.Facet.Taxonomy.WriterCache.ITaxonomyWriterCache;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TextField = Lucene.Net.Documents.TextField;
    using TieredMergePolicy = Lucene.Net.Index.TieredMergePolicy;
    using TokenStream = Lucene.Net.Analysis.TokenStream;

    /// <summary>
    /// <seealso cref="TaxonomyWriter"/> which uses a <seealso cref="Directory"/> to store the taxonomy
    /// information on disk, and keeps an additional in-memory cache of some or all
    /// categories.
    /// <para>
    /// In addition to the permanently-stored information in the <seealso cref="Directory"/>,
    /// efficiency dictates that we also keep an in-memory cache of <B>recently
    /// seen</B> or <B>all</B> categories, so that we do not need to go back to disk
    /// for every category addition to see which ordinal this category already has,
    /// if any. A <seealso cref="TaxonomyWriterCache"/> object determines the specific caching
    /// algorithm used.
    /// </para>
    /// <para>
    /// This class offers some hooks for extending classes to control the
    /// <seealso cref="IndexWriter"/> instance that is used. See <seealso cref="#openIndexWriter"/>.
    /// 
    /// @lucene.experimental
    /// </para>
    /// </summary>
    public class DirectoryTaxonomyWriter : ITaxonomyWriter
    {
        /// <summary>
        /// Property name of user commit data that contains the index epoch. The epoch
        /// changes whenever the taxonomy is recreated (i.e. opened with
        /// <seealso cref="OpenMode#CREATE"/>.
        /// <para>
        /// Applications should not use this property in their commit data because it
        /// will be overridden by this taxonomy writer.
        /// </para>
        /// </summary>
        public const string INDEX_EPOCH = "index.epoch";

        private readonly Directory dir;
        private readonly IndexWriter indexWriter;
        private readonly ITaxonomyWriterCache cache;
        private readonly AtomicInteger cacheMisses = new AtomicInteger(0);

        // Records the taxonomy index epoch, updated on replaceTaxonomy as well.
        private long indexEpoch;

        private SinglePositionTokenStream parentStream = new SinglePositionTokenStream(Consts.PAYLOAD_PARENT);
        private Field parentStreamField;
        private Field fullPathField;
        private int cacheMissesUntilFill = 11;
        private bool shouldFillCache = true;

        // even though lazily initialized, not volatile so that access to it is
        // faster. we keep a volatile boolean init instead.
        private ReaderManager readerManager;
        private volatile bool initializedReaderManager = false;
        private volatile bool shouldRefreshReaderManager;

        /// <summary>
        /// We call the cache "complete" if we know that every category in our
        /// taxonomy is in the cache. When the cache is <B>not</B> complete, and
        /// we can't find a category in the cache, we still need to look for it
        /// in the on-disk index; Therefore when the cache is not complete, we
        /// need to open a "reader" to the taxonomy index.
        /// The cache becomes incomplete if it was never filled with the existing
        /// categories, or if a put() to the cache ever returned true (meaning
        /// that some of the cached data was cleared).
        /// </summary>
        private volatile bool cacheIsComplete;
        private volatile bool isClosed = false;
        private volatile TaxonomyIndexArrays taxoArrays;
        private volatile int nextID;

        /// <summary>
        /// Reads the commit data from a Directory. </summary>
        private static IDictionary<string, string> ReadCommitData(Directory dir)
        {
            SegmentInfos infos = new SegmentInfos();
            infos.Read(dir);
            return infos.UserData;
        }

        /// <summary>
        /// Forcibly unlocks the taxonomy in the named directory.
        /// <P>
        /// Caution: this should only be used by failure recovery code, when it is
        /// known that no other process nor thread is in fact currently accessing
        /// this taxonomy.
        /// <P>
        /// This method is unnecessary if your <seealso cref="Directory"/> uses a
        /// <seealso cref="NativeFSLockFactory"/> instead of the default
        /// <seealso cref="SimpleFSLockFactory"/>. When the "native" lock is used, a lock
        /// does not stay behind forever when the process using it dies. 
        /// </summary>
        public static void Unlock(Directory directory)
        {
            IndexWriter.Unlock(directory);
        }

        /// <summary>
        /// Construct a Taxonomy writer.
        /// </summary>
        /// <param name="directory">
        ///    The <seealso cref="Directory"/> in which to store the taxonomy. Note that
        ///    the taxonomy is written directly to that directory (not to a
        ///    subdirectory of it). </param>
        /// <param name="openMode">
        ///    Specifies how to open a taxonomy for writing: <code>APPEND</code>
        ///    means open an existing index for append (failing if the index does
        ///    not yet exist). <code>CREATE</code> means create a new index (first
        ///    deleting the old one if it already existed).
        ///    <code>APPEND_OR_CREATE</code> appends to an existing index if there
        ///    is one, otherwise it creates a new index. </param>
        /// <param name="cache">
        ///    A <seealso cref="TaxonomyWriterCache"/> implementation which determines
        ///    the in-memory caching policy. See for example
        ///    <seealso cref="LruTaxonomyWriterCache"/> and <seealso cref="Cl2oTaxonomyWriterCache"/>.
        ///    If null or missing, <seealso cref="#defaultTaxonomyWriterCache()"/> is used. </param>
        /// <exception cref="CorruptIndexException">
        ///     if the taxonomy is corrupted. </exception>
        /// <exception cref="LockObtainFailedException">
        ///     if the taxonomy is locked by another writer. If it is known
        ///     that no other concurrent writer is active, the lock might
        ///     have been left around by an old dead process, and should be
        ///     removed using <seealso cref="#unlock(Directory)"/>. </exception>
        /// <exception cref="IOException">
        ///     if another error occurred. </exception>
        public DirectoryTaxonomyWriter(Directory directory, OpenMode openMode, 
            ITaxonomyWriterCache cache)
        {
            dir = directory;
            IndexWriterConfig config = CreateIndexWriterConfig(openMode);
            indexWriter = OpenIndexWriter(dir, config);

            // verify (to some extent) that merge policy in effect would preserve category docids 
            if (indexWriter != null)
            {
                Debug.Assert(!(indexWriter.Config.MergePolicy is TieredMergePolicy), "for preserving category docids, merging none-adjacent segments is not allowed");
            }

            // after we opened the writer, and the index is locked, it's safe to check
            // the commit data and read the index epoch
            openMode = config.OpenMode.HasValue ? config.OpenMode.Value : OpenMode.CREATE_OR_APPEND;
            if (!DirectoryReader.IndexExists(directory))
            {
                indexEpoch = 1;
            }
            else
            {
                string epochStr = null;
                IDictionary<string, string> commitData = ReadCommitData(directory);
                if (commitData != null && commitData.ContainsKey(INDEX_EPOCH))
                {
                    epochStr = commitData[INDEX_EPOCH];
                }
                // no commit data, or no epoch in it means an old taxonomy, so set its epoch to 1, for lack
                // of a better value.
                indexEpoch = epochStr == null ? 1 : Convert.ToInt64(epochStr, 16);
            }

            if (openMode == OpenMode.CREATE)
            {
                ++indexEpoch;
            }

            FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
            ft.OmitNorms = true;
            parentStreamField = new Field(Consts.FIELD_PAYLOADS, parentStream, ft);
            fullPathField = new StringField(Consts.FULL, "", Field.Store.YES);

            if (indexWriter == null)
                return;

            nextID = indexWriter.MaxDoc;

            if (cache == null)
            {
                cache = DefaultTaxonomyWriterCache();
            }
            this.cache = cache;

            if (nextID == 0)
            {
                cacheIsComplete = true;
                // Make sure that the taxonomy always contain the root category
                // with category id 0.
                AddCategory(new FacetLabel());
            }
            else
            {
                // There are some categories on the disk, which we have not yet
                // read into the cache, and therefore the cache is incomplete.
                // We choose not to read all the categories into the cache now,
                // to avoid terrible performance when a taxonomy index is opened
                // to add just a single category. We will do it later, after we
                // notice a few cache misses.
                cacheIsComplete = false;
            }
        }

        /// <summary>
        /// Open internal index writer, which contains the taxonomy data.
        /// <para>
        /// Extensions may provide their own <seealso cref="IndexWriter"/> implementation or instance. 
        /// <br><b>NOTE:</b> the instance this method returns will be closed upon calling
        /// to <seealso cref="#close()"/>.
        /// <br><b>NOTE:</b> the merge policy in effect must not merge none adjacent segments. See
        /// comment in <seealso cref="#createIndexWriterConfig(IndexWriterConfig.OpenMode)"/> for the logic behind this.
        ///  
        /// </para>
        /// </summary>
        /// <seealso cref= #createIndexWriterConfig(IndexWriterConfig.OpenMode)
        /// </seealso>
        /// <param name="directory">
        ///          the <seealso cref="Directory"/> on top of which an <seealso cref="IndexWriter"/>
        ///          should be opened. </param>
        /// <param name="config">
        ///          configuration for the internal index writer. </param>
        protected virtual IndexWriter OpenIndexWriter(Directory directory, IndexWriterConfig config)
        {
            return new IndexWriter(directory, config);
        }

        /// <summary>
        /// Create the <seealso cref="IndexWriterConfig"/> that would be used for opening the internal index writer.
        /// <br>Extensions can configure the <seealso cref="IndexWriter"/> as they see fit,
        /// including setting a <seealso cref="Lucene.Net.index.MergeScheduler merge-scheduler"/>, or
        /// <seealso cref="Lucene.Net.index.IndexDeletionPolicy deletion-policy"/>, different RAM size
        /// etc.<br>
        /// <br><b>NOTE:</b> internal docids of the configured index must not be altered.
        /// For that, categories are never deleted from the taxonomy index.
        /// In addition, merge policy in effect must not merge none adjacent segments.
        /// </summary>
        /// <seealso cref= #openIndexWriter(Directory, IndexWriterConfig)
        /// </seealso>
        /// <param name="openMode"> see <seealso cref="OpenMode"/> </param>
        protected virtual IndexWriterConfig CreateIndexWriterConfig(OpenMode openMode)
        {
            // TODO: should we use a more optimized Codec, e.g. Pulsing (or write custom)?
            // The taxonomy has a unique structure, where each term is associated with one document

            // :Post-Release-Update-Version.LUCENE_XY:
            // Make sure we use a MergePolicy which always merges adjacent segments and thus
            // keeps the doc IDs ordered as well (this is crucial for the taxonomy index).
            return (new IndexWriterConfig(LuceneVersion.LUCENE_48, null)).SetOpenMode(openMode).SetMergePolicy(new LogByteSizeMergePolicy());
        }

        /// <summary>
        /// Opens a <seealso cref="ReaderManager"/> from the internal <seealso cref="IndexWriter"/>. 
        /// </summary>
        private void InitReaderManager()
        {
            if (!initializedReaderManager)
            {
                lock (this)
                {
                    // verify that the taxo-writer hasn't been closed on us.
                    EnsureOpen();
                    if (!initializedReaderManager)
                    {
                        readerManager = new ReaderManager(indexWriter, false);
                        shouldRefreshReaderManager = false;
                        initializedReaderManager = true;
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new instance with a default cache as defined by
        /// <seealso cref="#defaultTaxonomyWriterCache()"/>.
        /// </summary>
        public DirectoryTaxonomyWriter(Directory directory, OpenMode openMode = OpenMode.CREATE_OR_APPEND)
            : this(directory, openMode, DefaultTaxonomyWriterCache())
        {
        }

        /// <summary>
        /// Defines the default <seealso cref="TaxonomyWriterCache"/> to use in constructors
        /// which do not specify one.
        /// <P>  
        /// The current default is <seealso cref="Cl2oTaxonomyWriterCache"/> constructed
        /// with the parameters (1024, 0.15f, 3), i.e., the entire taxonomy is
        /// cached in memory while building it.
        /// </summary>
        public static ITaxonomyWriterCache DefaultTaxonomyWriterCache()
        {
            return new Cl2oTaxonomyWriterCache(1024, 0.15f, 3);
        }

        /// <summary>
        /// Frees used resources as well as closes the underlying <seealso cref="IndexWriter"/>,
        /// which commits whatever changes made to it to the underlying
        /// <seealso cref="Directory"/>.
        /// </summary>
        public void Dispose()
        {
            lock (this)
            {
                if (!isClosed)
                {
                    Commit();
                    DoClose();
                }
            }
        }

        private void DoClose()
        {
            indexWriter.Dispose();
            isClosed = true;
            CloseResources();
        }

        /// <summary>
        /// A hook for extending classes to close additional resources that were used.
        /// The default implementation closes the <seealso cref="IndexReader"/> as well as the
        /// <seealso cref="TaxonomyWriterCache"/> instances that were used. <br>
        /// <b>NOTE:</b> if you override this method, you should include a
        /// <code>super.closeResources()</code> call in your implementation.
        /// </summary>
        protected virtual void CloseResources()
        {
            lock (this)
            {
                if (initializedReaderManager)
                {
                    readerManager.Dispose();
                    readerManager = null;
                    initializedReaderManager = false;
                }
                if (cache != null)
                {
                    cache.Close();
                }
            }
        }

        /// <summary>
        /// Look up the given category in the cache and/or the on-disk storage,
        /// returning the category's ordinal, or a negative number in case the
        /// category does not yet exist in the taxonomy.
        /// </summary>
        protected virtual int FindCategory(FacetLabel categoryPath)
        {
            lock (this)
            {
                // If we can find the category in the cache, or we know the cache is
                // complete, we can return the response directly from it
                int res = cache.Get(categoryPath);
                if (res >= 0 || cacheIsComplete)
                {
                    return res;
                }

                cacheMisses.IncrementAndGet();
                // After a few cache misses, it makes sense to read all the categories
                // from disk and into the cache. The reason not to do this on the first
                // cache miss (or even when opening the writer) is that it will
                // significantly slow down the case when a taxonomy is opened just to
                // add one category. The idea only spending a long time on reading
                // after enough time was spent on cache misses is known as an "online
                // algorithm".
                PerhapsFillCache();
                res = cache.Get(categoryPath);
                if (res >= 0 || cacheIsComplete)
                {
                    // if after filling the cache from the info on disk, the category is in it
                    // or the cache is complete, return whatever cache.get returned.
                    return res;
                }

                // if we get here, it means the category is not in the cache, and it is not
                // complete, and therefore we must look for the category on disk.

                // We need to get an answer from the on-disk index.
                InitReaderManager();

                int doc = -1;
                DirectoryReader reader = readerManager.Acquire();
                try
                {
                    BytesRef catTerm = new BytesRef(FacetsConfig.PathToString(categoryPath.Components, categoryPath.Length));
                    TermsEnum termsEnum = null; // reuse
                    DocsEnum docs = null; // reuse
                    foreach (AtomicReaderContext ctx in reader.Leaves)
                    {
                        Terms terms = ctx.AtomicReader.Terms(Consts.FULL);
                        if (terms != null)
                        {
                            termsEnum = terms.Iterator(termsEnum);
                            if (termsEnum.SeekExact(catTerm))
                            {
                                // liveDocs=null because the taxonomy has no deletes
                                docs = termsEnum.Docs(null, docs, 0); // freqs not required
                                // if the term was found, we know it has exactly one document.
                                doc = docs.NextDoc() + ctx.DocBase;
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    readerManager.Release(reader);
                }
                if (doc > 0)
                {
                    AddToCache(categoryPath, doc);
                }
                return doc;
            }
        }

        public virtual int AddCategory(FacetLabel categoryPath)
        {
            EnsureOpen();
            // check the cache outside the synchronized block. this results in better
            // concurrency when categories are there.
            int res = cache.Get(categoryPath);
            if (res < 0)
            {
                // the category is not in the cache - following code cannot be executed in parallel.
                lock (this)
                {
                    res = FindCategory(categoryPath);
                    if (res < 0)
                    {
                        // This is a new category, and we need to insert it into the index
                        // (and the cache). Actually, we might also need to add some of
                        // the category's ancestors before we can add the category itself
                        // (while keeping the invariant that a parent is always added to
                        // the taxonomy before its child). internalAddCategory() does all
                        // this recursively
                        res = InternalAddCategory(categoryPath);
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// Add a new category into the index (and the cache), and return its new
        /// ordinal.
        /// <para>
        /// Actually, we might also need to add some of the category's ancestors
        /// before we can add the category itself (while keeping the invariant that a
        /// parent is always added to the taxonomy before its child). We do this by
        /// recursion.
        /// </para>
        /// </summary>
        private int InternalAddCategory(FacetLabel cp)
        {
            // Find our parent's ordinal (recursively adding the parent category
            // to the taxonomy if it's not already there). Then add the parent
            // ordinal as payloads (rather than a stored field; payloads can be
            // more efficiently read into memory in bulk by LuceneTaxonomyReader)
            int parent;
            if (cp.Length > 1)
            {
                FacetLabel parentPath = cp.Subpath(cp.Length - 1);
                parent = FindCategory(parentPath);
                if (parent < 0)
                {
                    parent = InternalAddCategory(parentPath);
                }
            }
            else if (cp.Length == 1)
            {
                parent = TaxonomyReader.ROOT_ORDINAL;
            }
            else
            {
                parent = TaxonomyReader.INVALID_ORDINAL;
            }
            int id = AddCategoryDocument(cp, parent);

            return id;
        }

        /// <summary>
        /// Verifies that this instance wasn't closed, or throws
        /// <seealso cref="AlreadyClosedException"/> if it is.
        /// </summary>
        protected internal void EnsureOpen()
        {
            if (isClosed)
            {
                throw new AlreadyClosedException("The taxonomy writer has already been closed");
            }
        }

        /// <summary>
        /// Note that the methods calling addCategoryDocument() are synchornized, so
        /// this method is effectively synchronized as well.
        /// </summary>
        private int AddCategoryDocument(FacetLabel categoryPath, int parent)
        {
            // Before Lucene 2.9, position increments >=0 were supported, so we
            // added 1 to parent to allow the parent -1 (the parent of the root).
            // Unfortunately, starting with Lucene 2.9, after LUCENE-1542, this is
            // no longer enough, since 0 is not encoded consistently either (see
            // comment in SinglePositionTokenStream). But because we must be
            // backward-compatible with existing indexes, we can't just fix what
            // we write here (e.g., to write parent+2), and need to do a workaround
            // in the reader (which knows that anyway only category 0 has a parent
            // -1).    
            parentStream.Set(Math.Max(parent + 1, 1));
            Document d = new Document();
            d.Add(parentStreamField);

            fullPathField.StringValue = FacetsConfig.PathToString(categoryPath.Components, categoryPath.Length);
            d.Add(fullPathField);

            // Note that we do no pass an Analyzer here because the fields that are
            // added to the Document are untokenized or contains their own TokenStream.
            // Therefore the IndexWriter's Analyzer has no effect.
            indexWriter.AddDocument(d);
            int id = nextID++;

            // added a category document, mark that ReaderManager is not up-to-date
            shouldRefreshReaderManager = true;

            // also add to the parent array
            taxoArrays = TaxoArrays.Add(id, parent);

            // NOTE: this line must be executed last, or else the cache gets updated
            // before the parents array (LUCENE-4596)
            AddToCache(categoryPath, id);

            return id;
        }

        private class SinglePositionTokenStream : TokenStream
        {
            private ICharTermAttribute termAtt;
            private IPositionIncrementAttribute posIncrAtt;
            private bool returned;
            private int val;
            private readonly string word;

            public SinglePositionTokenStream(string word)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
                posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
                this.word = word;
                returned = true;
            }

            /// <summary>
            /// Set the value we want to keep, as the position increment.
            /// Note that when TermPositions.nextPosition() is later used to
            /// retrieve this value, val-1 will be returned, not val.
            /// <P>
            /// IMPORTANT NOTE: Before Lucene 2.9, val>=0 were safe (for val==0,
            /// the retrieved position would be -1). But starting with Lucene 2.9,
            /// this unfortunately changed, and only val>0 are safe. val=0 can
            /// still be used, but don't count on the value you retrieve later
            /// (it could be 0 or -1, depending on circumstances or versions).
            /// This change is described in Lucene's JIRA: LUCENE-1542. 
            /// </summary>
            public virtual void Set(int val)
            {
                this.val = val;
                returned = false;
            }

            public sealed override bool IncrementToken()
            {
                if (returned)
                {
                    return false;
                }
                ClearAttributes();
                posIncrAtt.PositionIncrement = val;
                termAtt.SetEmpty();
                termAtt.Append(word);
                returned = true;
                return true;
            }
        }

        private void AddToCache(FacetLabel categoryPath, int id)
        {
            if (cache.Put(categoryPath, id))
            {
                // If cache.put() returned true, it means the cache was limited in
                // size, became full, and parts of it had to be evicted. It is
                // possible that a relatively-new category that isn't yet visible
                // to our 'reader' was evicted, and therefore we must now refresh 
                // the reader.
                RefreshReaderManager();
                cacheIsComplete = false;
            }
        }

        private void RefreshReaderManager()
        {
            lock (this)
            {
                // this method is synchronized since it cannot happen concurrently with
                // addCategoryDocument -- when this method returns, we must know that the
                // reader manager's state is current. also, it sets shouldRefresh to false, 
                // and this cannot overlap with addCatDoc too.
                // NOTE: since this method is sync'ed, it can call maybeRefresh, instead of
                // maybeRefreshBlocking. If ever this is changed, make sure to change the
                // call too.
                if (shouldRefreshReaderManager && initializedReaderManager)
                {
                    readerManager.MaybeRefresh();
                    shouldRefreshReaderManager = false;
                }
            }
        }

        public virtual void Commit()
        {
            lock (this)
            {
                EnsureOpen();
                // LUCENE-4972: if we always call setCommitData, we create empty commits
                string epochStr = null;
                indexWriter.CommitData.TryGetValue(INDEX_EPOCH, out epochStr);
                if (epochStr == null || Convert.ToInt64(epochStr, 16) != indexEpoch)
                {
                    indexWriter.CommitData = CombinedCommitData(indexWriter.CommitData);
                }
                indexWriter.Commit();
            }
        }

        /// <summary>
        /// Combine original user data with the taxonomy epoch. </summary>
        private IDictionary<string, string> CombinedCommitData(IDictionary<string, string> commitData)
        {
            IDictionary<string, string> m = new Dictionary<string, string>();
            if (commitData != null)
            {
                m.PutAll(commitData);
            }
            m[INDEX_EPOCH] = Convert.ToString(indexEpoch, 16);
            return m;
        }

        public virtual IDictionary<string, string> CommitData
        {
            set
            {
                indexWriter.CommitData = CombinedCommitData(value);
            }
            get
            {
                return CombinedCommitData(indexWriter.CommitData);
            }
        }


        /// <summary>
        /// prepare most of the work needed for a two-phase commit.
        /// See <seealso cref="IndexWriter#prepareCommit"/>.
        /// </summary>
        public virtual void PrepareCommit()
        {
            lock (this)
            {
                EnsureOpen();
                // LUCENE-4972: if we always call setCommitData, we create empty commits
                string epochStr = indexWriter.CommitData[INDEX_EPOCH];
                if (epochStr == null || Convert.ToInt64(epochStr, 16) != indexEpoch)
                {
                    indexWriter.CommitData = CombinedCommitData(indexWriter.CommitData);
                }
                indexWriter.PrepareCommit();
            }
        }

        public virtual int Size
        {
            get
            {
                EnsureOpen();
                return nextID;
            }
        }

        /// <summary>
        /// Set the number of cache misses before an attempt is made to read the entire
        /// taxonomy into the in-memory cache.
        /// <para>
        /// This taxonomy writer holds an in-memory cache of recently seen categories
        /// to speed up operation. On each cache-miss, the on-disk index needs to be
        /// consulted. When an existing taxonomy is opened, a lot of slow disk reads
        /// like that are needed until the cache is filled, so it is more efficient to
        /// read the entire taxonomy into memory at once. We do this complete read
        /// after a certain number (defined by this method) of cache misses.
        /// </para>
        /// <para>
        /// If the number is set to {@code 0}, the entire taxonomy is read into the
        /// cache on first use, without fetching individual categories first.
        /// </para>
        /// <para>
        /// NOTE: it is assumed that this method is called immediately after the
        /// taxonomy writer has been created.
        /// </para>
        /// </summary>
        public virtual int CacheMissesUntilFill
        {
            set
            {
                EnsureOpen();
                cacheMissesUntilFill = value;
            }
        }

        // we need to guarantee that if several threads call this concurrently, only
        // one executes it, and after it returns, the cache is updated and is either
        // complete or not.
        private void PerhapsFillCache()
        {
            lock (this)
            {
                if (cacheMisses.Get() < cacheMissesUntilFill)
                {
                    return;
                }

                if (!shouldFillCache)
                {
                    // we already filled the cache once, there's no need to re-fill it
                    return;
                }
                shouldFillCache = false;

                InitReaderManager();

                bool aborted = false;
                DirectoryReader reader = readerManager.Acquire();
                try
                {
                    TermsEnum termsEnum = null;
                    DocsEnum docsEnum = null;
                    foreach (AtomicReaderContext ctx in reader.Leaves)
                    {
                        Terms terms = ctx.AtomicReader.Terms(Consts.FULL);
                        if (terms != null) // cannot really happen, but be on the safe side
                        {
                            termsEnum = terms.Iterator(termsEnum);
                            while (termsEnum.Next() != null)
                            {
                                if (!cache.Full)
                                {
                                    BytesRef t = termsEnum.Term();
                                    // Since we guarantee uniqueness of categories, each term has exactly
                                    // one document. Also, since we do not allow removing categories (and
                                    // hence documents), there are no deletions in the index. Therefore, it
                                    // is sufficient to call next(), and then doc(), exactly once with no
                                    // 'validation' checks.
                                    FacetLabel cp = new FacetLabel(FacetsConfig.StringToPath(t.Utf8ToString()));
                                    docsEnum = termsEnum.Docs(null, docsEnum, DocsEnum.FLAG_NONE);
                                    bool res = cache.Put(cp, docsEnum.NextDoc() + ctx.DocBase);
                                    Debug.Assert(!res, "entries should not have been evicted from the cache");
                                }
                                else
                                {
                                    // the cache is full and the next put() will evict entries from it, therefore abort the iteration.
                                    aborted = true;
                                    break;
                                }
                            }
                        }
                        if (aborted)
                        {
                            break;
                        }
                    }
                }
                finally
                {
                    readerManager.Release(reader);
                }

                cacheIsComplete = !aborted;
                if (cacheIsComplete)
                {
                    lock (this)
                    {
                        // everything is in the cache, so no need to keep readerManager open.
                        // this block is executed in a sync block so that it works well with
                        // initReaderManager called in parallel.
                        readerManager.Dispose();
                        readerManager = null;
                        initializedReaderManager = false;
                    }
                }
            }
        }

        private TaxonomyIndexArrays TaxoArrays
        {
            get
            {
                if (taxoArrays == null)
                {
                    lock (this)
                    {
                        if (taxoArrays == null)
                        {
                            InitReaderManager();
                            DirectoryReader reader = readerManager.Acquire();
                            try
                            {
                                // according to Java Concurrency, this might perform better on some
                                // JVMs, since the object initialization doesn't happen on the
                                // volatile member.
                                TaxonomyIndexArrays tmpArrays = new TaxonomyIndexArrays(reader);
                                taxoArrays = tmpArrays;
                            }
                            finally
                            {
                                readerManager.Release(reader);
                            }
                        }
                    }
                }
                return taxoArrays;
            }
        }

        public virtual int GetParent(int ordinal)
        {
            EnsureOpen();
            // Note: the following if() just enforces that a user can never ask
            // for the parent of a nonexistant category - even if the parent array
            // was allocated bigger than it really needs to be.
            if (ordinal >= nextID)
            {
                throw new System.IndexOutOfRangeException("requested ordinal is bigger than the largest ordinal in the taxonomy");
            }

            int[] parents = TaxoArrays.Parents();
            Debug.Assert(ordinal < parents.Length, "requested ordinal (" + ordinal + "); parents.length (" + parents.Length + ") !");
            return parents[ordinal];
        }

        /// <summary>
        /// Takes the categories from the given taxonomy directory, and adds the
        /// missing ones to this taxonomy. Additionally, it fills the given
        /// <seealso cref="OrdinalMap"/> with a mapping from the original ordinal to the new
        /// ordinal.
        /// </summary>
        public virtual void AddTaxonomy(Directory taxoDir, IOrdinalMap map)
        {
            EnsureOpen();
            DirectoryReader r = DirectoryReader.Open(taxoDir);
            try
            {
                int size = r.NumDocs;
                IOrdinalMap ordinalMap = map;
                ordinalMap.Size = size;
                int @base = 0;
                TermsEnum te = null;
                DocsEnum docs = null;
                foreach (AtomicReaderContext ctx in r.Leaves)
                {
                    AtomicReader ar = ctx.AtomicReader;
                    Terms terms = ar.Terms(Consts.FULL);
                    te = terms.Iterator(te);
                    while (te.Next() != null)
                    {
                        FacetLabel cp = new FacetLabel(FacetsConfig.StringToPath(te.Term().Utf8ToString()));
                        int ordinal = AddCategory(cp);
                        docs = te.Docs(null, docs, DocsEnum.FLAG_NONE);
                        ordinalMap.AddMapping(docs.NextDoc() + @base, ordinal);
                    }
                    @base += ar.MaxDoc; // no deletions, so we're ok
                }
                ordinalMap.AddDone();
            }
            finally
            {
                r.Dispose();
            }
        }

        /// <summary>
        /// Mapping from old ordinal to new ordinals, used when merging indexes 
        /// wit separate taxonomies.
        /// <para> 
        /// addToTaxonomies() merges one or more taxonomies into the given taxonomy
        /// (this). An IOrdinalMap is filled for each of the added taxonomies,
        /// containing the new ordinal (in the merged taxonomy) of each of the
        /// categories in the old taxonomy.
        /// <P>  
        /// There exist two implementations of IOrdinalMap: MemoryOrdinalMap and
        /// DiskOrdinalMap. As their names suggest, the former keeps the map in
        /// memory and the latter in a temporary disk file. Because these maps will
        /// later be needed one by one (to remap the counting lists), not all at the
        /// same time, it is recommended to put the first taxonomy's map in memory,
        /// and all the rest on disk (later to be automatically read into memory one
        /// by one, when needed).
        /// </para>
        /// </summary>
        public interface IOrdinalMap
        {
            /// <summary>
            /// Set the size of the map. This MUST be called before addMapping().
            /// It is assumed (but not verified) that addMapping() will then be
            /// called exactly 'size' times, with different origOrdinals between 0
            /// and size-1.  
            /// </summary>
            int Size { set; }

            /// <summary>
            /// Record a mapping. </summary>
            void AddMapping(int origOrdinal, int newOrdinal);

            /// <summary>
            /// Call addDone() to say that all addMapping() have been done.
            /// In some implementations this might free some resources. 
            /// </summary>
            void AddDone();

            /// <summary>
            /// Return the map from the taxonomy's original (consecutive) ordinals
            /// to the new taxonomy's ordinals. If the map has to be read from disk
            /// and ordered appropriately, it is done when getMap() is called.
            /// getMap() should only be called once, and only when the map is actually
            /// needed. Calling it will also free all resources that the map might
            /// be holding (such as temporary disk space), other than the returned int[].
            /// </summary>
            int[] Map { get; }
        }

        /// <summary>
        /// <seealso cref="OrdinalMap"/> maintained in memory
        /// </summary>
        public sealed class MemoryOrdinalMap : IOrdinalMap
        {
            internal int[] map;

            /// <summary>
            /// Sole constructor. 
            /// </summary>
            public MemoryOrdinalMap()
            {
                map = new int[] { };
            }

            public int Size
            {
                set { map = new int[value]; }
            }

            public void AddMapping(int origOrdinal, int newOrdinal)
            {
                if (map.Length - 1 >= origOrdinal)
                {
                    map[origOrdinal] = newOrdinal;
                }
                else
                {
                    Array.Resize(ref map, origOrdinal + 1);
                    map[origOrdinal] = newOrdinal;
                }


            }

            public void AddDone() // nothing to do
            {
            }
            public int[] Map
            {
                get
                {
                    return map;
                }
            }
        }

        /// <summary>
        /// <seealso cref="OrdinalMap"/> maintained on file system
        /// </summary>
        public sealed class DiskOrdinalMap : IOrdinalMap
        {
            internal string tmpfile;
            internal OutputStreamDataOutput @out;

            /// <summary>
            /// Sole constructor. 
            /// </summary>
            public DiskOrdinalMap(string tmpfile)
            {
                this.tmpfile = tmpfile;
                var outfs = new FileStream(tmpfile, FileMode.OpenOrCreate, FileAccess.Write);
                @out = new OutputStreamDataOutput(outfs);
            }

            public void AddMapping(int origOrdinal, int newOrdinal)
            {
                @out.WriteInt(origOrdinal);
                @out.WriteInt(newOrdinal);
            }

            public int Size
            {
                set
                {
                    @out.WriteInt(value);
                }
            }

            public void AddDone()
            {
                if (@out != null)
                {
                    @out.Dispose();
                    @out = null;
                }
            }

            int[] map = null;

            public int[] Map
            {
                get
                {
                    if (map != null)
                    {
                        return map;
                    }
                    AddDone(); // in case this wasn't previously called

                    var ifs = new FileStream(tmpfile, FileMode.OpenOrCreate, FileAccess.Read);
                    var @in = new InputStreamDataInput(ifs);
                    map = new int[@in.ReadInt()];
                    // NOTE: The current code assumes here that the map is complete,
                    // i.e., every ordinal gets one and exactly one value. Otherwise,
                    // we may run into an EOF here, or vice versa, not read everything.
                    for (int i = 0; i < map.Length; i++)
                    {
                        int origordinal = @in.ReadInt();
                        int newordinal = @in.ReadInt();
                        map[origordinal] = newordinal;
                    }
                    @in.Dispose();

                    // Delete the temporary file, which is no longer needed.
                    if (File.Exists(tmpfile))
                    {
                        File.Delete(tmpfile);
                    }
                    return map;
                }
            }
        }

        /// <summary>
        /// Rollback changes to the taxonomy writer and closes the instance. Following
        /// this method the instance becomes unusable (calling any of its API methods
        /// will yield an <seealso cref="AlreadyClosedException"/>).
        /// </summary>
        public virtual void Rollback()
        {
            lock (this)
            {
                EnsureOpen();
                indexWriter.Rollback();
                DoClose();
            }
        }

        /// <summary>
        /// Replaces the current taxonomy with the given one. This method should
        /// generally be called in conjunction with
        /// <seealso cref="IndexWriter#addIndexes(Directory...)"/> to replace both the taxonomy
        /// as well as the search index content.
        /// </summary>
        public virtual void ReplaceTaxonomy(Directory taxoDir)
        {
            lock (this)
            {
                // replace the taxonomy by doing IW optimized operations
                indexWriter.DeleteAll();
                indexWriter.AddIndexes(taxoDir);
                shouldRefreshReaderManager = true;
                InitReaderManager(); // ensure that it's initialized
                RefreshReaderManager();
                nextID = indexWriter.MaxDoc;
                taxoArrays = null; // must nullify so that it's re-computed next time it's needed

                // need to clear the cache, so that addCategory won't accidentally return
                // old categories that are in the cache.
                cache.Clear();
                cacheIsComplete = false;
                shouldFillCache = true;
                cacheMisses.Set(0);

                // update indexEpoch as a taxonomy replace is just like it has be recreated
                ++indexEpoch;
            }
        }

        /// <summary>
        /// Returns the <seealso cref="Directory"/> of this taxonomy writer. </summary>
        public virtual Directory Directory
        {
            get
            {
                return dir;
            }
        }

        /// <summary>
        /// Used by <seealso cref="DirectoryTaxonomyReader"/> to support NRT.
        /// <para>
        /// <b>NOTE:</b> you should not use the obtained <seealso cref="IndexWriter"/> in any
        /// way, other than opening an IndexReader on it, or otherwise, the taxonomy
        /// index may become corrupt!
        /// </para>
        /// </summary>
        internal IndexWriter InternalIndexWriter
        {
            get
            {
                return indexWriter;
            }
        }

        /// <summary>
        /// Expert: returns current index epoch, if this is a
        /// near-real-time reader.  Used by {@link
        /// DirectoryTaxonomyReader} to support NRT. 
        /// 
        /// @lucene.internal 
        /// </summary>
        public long TaxonomyEpoch
        {
            get
            {
                return indexEpoch;
            }
        }
    }
}