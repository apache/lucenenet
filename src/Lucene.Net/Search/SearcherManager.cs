using Lucene.Net.Diagnostics;
using System;
using System.IO;

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
    /// Utility class to safely share <see cref="IndexSearcher"/> instances across multiple
    /// threads, while periodically reopening. This class ensures each searcher is
    /// disposed only once all threads have finished using it.
    ///
    /// <para/>
    /// Use <see cref="ReferenceManager{G}.Acquire()"/> to obtain the current searcher, and <see cref="ReferenceManager{G}.Release(G)"/> to
    /// release it, like this:
    ///
    /// <code>
    /// IndexSearcher s = manager.Acquire();
    /// try 
    /// {
    ///     // Do searching, doc retrieval, etc. with s
    /// } 
    /// finally 
    /// {
    ///     manager.Release(s);
    ///     // Do not use s after this!
    ///     s = null;
    /// }
    /// </code>
    ///
    /// <para/>
    /// In addition you should periodically call <see cref="ReferenceManager{G}.MaybeRefresh()"/>. While it's
    /// possible to call this just before running each query, this is discouraged
    /// since it penalizes the unlucky queries that do the reopen. It's better to use
    /// a separate background thread, that periodically calls <see cref="ReferenceManager{G}.MaybeRefresh()"/>. Finally,
    /// be sure to call <see cref="ReferenceManager{G}.Dispose()"/> once you are done.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="SearcherFactory"/>
    public sealed class SearcherManager : ReferenceManager<IndexSearcher>
    {
        private readonly SearcherFactory searcherFactory;

        /// <summary>
        /// Creates and returns a new <see cref="SearcherManager"/> from the given
        /// <see cref="IndexWriter"/>.
        /// </summary>
        /// <param name="writer">
        ///          The <see cref="IndexWriter"/> to open the <see cref="IndexReader"/> from. </param>
        /// <param name="applyAllDeletes">
        ///          If <c>true</c>, all buffered deletes will be applied (made
        ///          visible) in the <see cref="IndexSearcher"/> / <see cref="DirectoryReader"/>.
        ///          If <c>false</c>, the deletes may or may not be applied, but
        ///          remain buffered (in <see cref="IndexWriter"/>) so that they will be applied in
        ///          the future. Applying deletes can be costly, so if your app can
        ///          tolerate deleted documents being returned you might gain some
        ///          performance by passing <c>false</c>. See
        ///          <see cref="DirectoryReader.OpenIfChanged(DirectoryReader, IndexWriter, bool)"/>. </param>
        /// <param name="searcherFactory">
        ///          An optional <see cref="SearcherFactory"/>. Pass <c>null</c> if you
        ///          don't require the searcher to be warmed before going live or other
        ///          custom behavior.
        /// </param>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public SearcherManager(IndexWriter writer, bool applyAllDeletes, SearcherFactory searcherFactory)
        {
            if (searcherFactory is null)
            {
                searcherFactory = new SearcherFactory();
            }
            this.searcherFactory = searcherFactory;
            Current = GetSearcher(searcherFactory, DirectoryReader.Open(writer, applyAllDeletes));
        }

        /// <summary>
        /// Creates and returns a new <see cref="SearcherManager"/> from the given <see cref="Directory"/>. </summary>
        /// <param name="dir"> The directory to open the <see cref="DirectoryReader"/> on. </param>
        /// <param name="searcherFactory"> An optional <see cref="SearcherFactory"/>. Pass
        ///        <c>null</c> if you don't require the searcher to be warmed
        ///        before going live or other custom behavior.
        /// </param>
        /// <exception cref="IOException"> If there is a low-level I/O error </exception>
        public SearcherManager(Directory dir, SearcherFactory searcherFactory)
        {
            if (searcherFactory is null)
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
            if (Debugging.AssertsEnabled) Debugging.Assert(r is DirectoryReader,"searcher's IndexReader should be a DirectoryReader, but got {0}", r);
            IndexReader newReader = DirectoryReader.OpenIfChanged((DirectoryReader)r);
            if (newReader is null)
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
        /// Returns <c>true</c> if no changes have occured since this searcher
        /// ie. reader was opened, otherwise <c>false</c>. </summary>
        /// <seealso cref="DirectoryReader.IsCurrent()"/>
        public bool IsSearcherCurrent()
        {
            IndexSearcher searcher = Acquire();
            try
            {
                IndexReader r = searcher.IndexReader;
                if (Debugging.AssertsEnabled) Debugging.Assert(r is DirectoryReader,"searcher's IndexReader should be a DirectoryReader, but got {0}", r);
                return ((DirectoryReader)r).IsCurrent();
            }
            finally
            {
                Release(searcher);
            }
        }

        /// <summary>
        /// Expert: creates a searcher from the provided 
        /// <see cref="IndexReader"/> using the provided 
        /// <see cref="SearcherFactory"/>.  NOTE: this decRefs incoming reader
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
                    throw IllegalStateException.Create("SearcherFactory must wrap exactly the provided reader (got " + searcher.IndexReader + " but expected " + reader + ")");
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