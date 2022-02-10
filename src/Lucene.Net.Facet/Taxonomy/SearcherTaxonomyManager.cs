// Lucene version compatibility level 4.8.1
using Lucene.Net.Search;
using System;

namespace Lucene.Net.Facet.Taxonomy
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
    using DirectoryTaxonomyReader = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyReader;
    using DirectoryTaxonomyWriter = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using SearcherFactory = Lucene.Net.Search.SearcherFactory;
    using SearcherManager = Lucene.Net.Search.SearcherManager;

    /// <summary>
    /// Manages near-real-time reopen of both an <see cref="IndexSearcher"/>
    /// and a <see cref="TaxonomyReader"/>.
    /// 
    /// <para>
    /// <b>NOTE</b>: If you call <see cref="DirectoryTaxonomyWriter.ReplaceTaxonomy"/>
    /// then you must open a new <see cref="SearcherTaxonomyManager"/> afterwards.
    /// </para>
    /// </summary>
    public class SearcherTaxonomyManager : ReferenceManager<SearcherTaxonomyManager.SearcherAndTaxonomy>
    {
        /// <summary>
        /// Holds a matched pair of <see cref="IndexSearcher"/> and
        /// <see cref="Taxonomy.TaxonomyReader"/> 
        /// </summary>
        public class SearcherAndTaxonomy
        {
            /// <summary>
            /// Point-in-time <see cref="IndexSearcher"/>.
            /// </summary>
            public IndexSearcher Searcher { get; private set; }

            /// <summary>
            /// Matching point-in-time <see cref="DirectoryTaxonomyReader"/>.
            /// </summary>
            public DirectoryTaxonomyReader TaxonomyReader { get; private set; }

            /// <summary>
            /// Create a <see cref="SearcherAndTaxonomy"/>
            /// </summary>
            public SearcherAndTaxonomy(IndexSearcher searcher, DirectoryTaxonomyReader taxonomyReader)
            {
                this.Searcher = searcher;
                this.TaxonomyReader = taxonomyReader;
            }
        }

        private readonly SearcherFactory searcherFactory;
        private readonly long taxoEpoch;
        private readonly DirectoryTaxonomyWriter taxoWriter;

        /// <summary>
        /// Creates near-real-time searcher and taxonomy reader
        /// from the corresponding writers. 
        /// </summary>
        public SearcherTaxonomyManager(IndexWriter writer, bool applyAllDeletes, 
            SearcherFactory searcherFactory, DirectoryTaxonomyWriter taxoWriter)
        {
            if (searcherFactory is null)
            {
                searcherFactory = new SearcherFactory();
            }
            this.searcherFactory = searcherFactory;
            this.taxoWriter = taxoWriter;
            var taxoReader = new DirectoryTaxonomyReader(taxoWriter);
            Current = new SearcherAndTaxonomy(SearcherManager.GetSearcher(
                searcherFactory, DirectoryReader.Open(writer, applyAllDeletes)), taxoReader);
            this.taxoEpoch = taxoWriter.TaxonomyEpoch;
        }

        /// <summary>
        /// Creates search and taxonomy readers over the corresponding directories.
        /// 
        /// <para>
        /// <b>NOTE:</b> you should only use this constructor if you commit and call
        /// <see cref="Search.ReferenceManager{G}.MaybeRefresh()"/> (on the <see cref="Index.ReaderManager"/>) in the same thread. Otherwise it could lead to an
        /// unsync'd <see cref="IndexSearcher"/> and <see cref="TaxonomyReader"/> pair.
        /// </para>
        /// </summary>
        public SearcherTaxonomyManager(Store.Directory indexDir, Store.Directory taxoDir, SearcherFactory searcherFactory)
        {
            if (searcherFactory is null)
            {
                searcherFactory = new SearcherFactory();
            }
            this.searcherFactory = searcherFactory;
            var taxoReader = new DirectoryTaxonomyReader(taxoDir);
            Current = new SearcherAndTaxonomy(SearcherManager.GetSearcher(
                searcherFactory, DirectoryReader.Open(indexDir)), taxoReader);
            this.taxoWriter = null;
            taxoEpoch = -1;
        }

        protected override void DecRef(SearcherAndTaxonomy @ref)
        {
            @ref.Searcher.IndexReader.DecRef();

            // This decRef can fail, and then in theory we should
            // tryIncRef the searcher to put back the ref count
            // ... but 1) the below decRef should only fail because
            // it decRef'd to 0 and closed and hit some IOException
            // during close, in which case 2) very likely the
            // searcher was also just closed by the above decRef and
            // a tryIncRef would fail:
            @ref.TaxonomyReader.DecRef();
        }

        protected override bool TryIncRef(SearcherAndTaxonomy @ref)
        {
            if (@ref.Searcher.IndexReader.TryIncRef())
            {
                if (@ref.TaxonomyReader.TryIncRef())
                {
                    return true;
                }
                else
                {
                    @ref.Searcher.IndexReader.DecRef();
                }
            }
            return false;
        }

        protected override SearcherAndTaxonomy RefreshIfNeeded(SearcherAndTaxonomy @ref)
        {
            // Must re-open searcher first, otherwise we may get a
            // new reader that references ords not yet known to the
            // taxonomy reader:
            IndexReader r = @ref.Searcher.IndexReader;
            IndexReader newReader = DirectoryReader.OpenIfChanged((DirectoryReader)r);
            if (newReader is null)
            {
                return null;
            }
            else
            {
                var tr = TaxonomyReader.OpenIfChanged(@ref.TaxonomyReader);
                if (tr is null)
                {
                    @ref.TaxonomyReader.IncRef();
                    tr = @ref.TaxonomyReader;
                }
                else if (taxoWriter != null && taxoWriter.TaxonomyEpoch != taxoEpoch)
                {
                    IOUtils.Dispose(newReader, tr);
                    throw IllegalStateException.Create("DirectoryTaxonomyWriter.ReplaceTaxonomy() was called, which is not allowed when using SearcherTaxonomyManager");
                }

                return new SearcherAndTaxonomy(SearcherManager.GetSearcher(searcherFactory, newReader), tr);
            }
        }

        protected override int GetRefCount(SearcherAndTaxonomy reference)
        {
            return reference.Searcher.IndexReader.RefCount;
        }
    }
}