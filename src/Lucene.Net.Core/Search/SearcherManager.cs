using System;
using System.Diagnostics;

namespace Lucene.Net.Search
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

    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;

    /// <summary>
    /// Utility class to safely share <seealso cref="IndexSearcher"/> instances across multiple
    /// threads, while periodically reopening. this class ensures each searcher is
    /// closed only once all threads have finished using it.
    ///
    /// <p>
    /// Use <seealso cref="#acquire"/> to obtain the current searcher, and <seealso cref="#release"/> to
    /// release it, like this:
    ///
    /// <pre class="prettyprint">
    /// IndexSearcher s = manager.acquire();
    /// try {
    ///   // Do searching, doc retrieval, etc. with s
    /// } finally {
    ///   manager.release(s);
    /// }
    /// // Do not use s after this!
    /// s = null;
    /// </pre>
    ///
    /// <p>
    /// In addition you should periodically call <seealso cref="#maybeRefresh"/>. While it's
    /// possible to call this just before running each query, this is discouraged
    /// since it penalizes the unlucky queries that do the reopen. It's better to use
    /// a separate background thread, that periodically calls maybeReopen. Finally,
    /// be sure to call <seealso cref="#close"/> once you are done.
    /// </summary>
    /// <seealso cref="SearcherFactory"/>
    ///
    /// @lucene.experimental </seealso>
    public sealed class SearcherManager : ReferenceManager<IndexSearcher>
    {
        private readonly SearcherFactory searcherFactory;

        /// <summary>
        /// Creates and returns a new SearcherManager from the given
        /// <seealso cref="IndexWriter"/>.
        /// </summary>
        /// <param name="writer">
        ///          the IndexWriter to open the IndexReader from. </param>
        /// <param name="applyAllDeletes">
        ///          If <code>true</code>, all buffered deletes will be applied (made
        ///          visible) in the <seealso cref="IndexSearcher"/> / <seealso cref="DirectoryReader"/>.
        ///          If <code>false</code>, the deletes may or may not be applied, but
        ///          remain buffered (in IndexWriter) so that they will be applied in
        ///          the future. Applying deletes can be costly, so if your app can
        ///          tolerate deleted documents being returned you might gain some
        ///          performance by passing <code>false</code>. See
        ///          <seealso cref="DirectoryReader#openIfChanged(DirectoryReader, IndexWriter, boolean)"/>. </param>
        /// <param name="searcherFactory">
        ///          An optional <see cref="SearcherFactory"/>. Pass <code>null</code> if you
        ///          don't require the searcher to be warmed before going live or other
        ///          custom behavior.
        /// </param>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public SearcherManager(IndexWriter writer, bool applyAllDeletes, SearcherFactory searcherFactory = null)
        {
            if (searcherFactory == null)
            {
                searcherFactory = new SearcherFactory();
            }
            this.searcherFactory = searcherFactory;
            Current = GetSearcher(searcherFactory, DirectoryReader.Open(writer, applyAllDeletes));
        }

        /// <summary>
        /// Creates and returns a new SearcherManager from the given <seealso cref="Directory"/>. </summary>
        /// <param name="dir"> the directory to open the DirectoryReader on. </param>
        /// <param name="searcherFactory"> An optional <see cref="SearcherFactory"/>. Pass
        ///        <code>null</code> if you don't require the searcher to be warmed
        ///        before going live or other custom behavior.
        /// </param>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public SearcherManager(Directory dir, SearcherFactory searcherFactory)
        {
            if (searcherFactory == null)
            {
                searcherFactory = new SearcherFactory();
            }
            this.searcherFactory = searcherFactory;
            Current = GetSearcher(searcherFactory, DirectoryReader.Open(dir));
        }

        protected override void DecRef(IndexSearcher reference)
        {
            reference.IndexReader.DecRef();
        }

        protected override IndexSearcher RefreshIfNeeded(IndexSearcher referenceToRefresh)
        {
            IndexReader r = referenceToRefresh.IndexReader;
            Debug.Assert(r is DirectoryReader, "searcher's IndexReader should be a DirectoryReader, but got " + r);
            IndexReader newReader = DirectoryReader.OpenIfChanged((DirectoryReader)r);
            if (newReader == null)
            {
                return null;
            }
            else
            {
                return GetSearcher(searcherFactory, newReader);
            }
        }

        protected override bool TryIncRef(IndexSearcher reference)
        {
            return reference.IndexReader.TryIncRef();
        }

        protected override int GetRefCount(IndexSearcher reference)
        {
            return reference.IndexReader.RefCount;
        }

        /// <summary>
        /// Returns <code>true</code> if no changes have occured since this searcher
        /// ie. reader was opened, otherwise <code>false</code>. </summary>
        /// <seealso cref= DirectoryReader#isCurrent()  </seealso>
        public bool IsSearcherCurrent()
        {
            IndexSearcher searcher = Acquire();
            try
            {
                IndexReader r = searcher.IndexReader;
                Debug.Assert(r is DirectoryReader, "searcher's IndexReader should be a DirectoryReader, but got " + r);
                return ((DirectoryReader)r).IsCurrent;
            }
            finally
            {
                Release(searcher);
            }
        }

        /// <summary>
        /// Expert: creates a searcher from the provided {@link
        ///  IndexReader} using the provided {@link
        ///  SearcherFactory}.  NOTE: this decRefs incoming reader
        /// on throwing an exception.
        /// </summary>
        public static IndexSearcher GetSearcher(SearcherFactory searcherFactory, IndexReader reader)
        {
            bool success = false;
            IndexSearcher searcher;
            try
            {
                searcher = searcherFactory.NewSearcher(reader);
                if (searcher.IndexReader != reader)
                {
                    throw new InvalidOperationException("SearcherFactory must wrap exactly the provided reader (got " + searcher.IndexReader + " but expected " + reader + ")");
                }
                success = true;
            }
            finally
            {
                if (!success)
                {
                    reader.DecRef();
                }
            }
            return searcher;
        }
    }
}