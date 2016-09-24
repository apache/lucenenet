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
    /// Manages near-real-time reopen of both an IndexSearcher
    /// and a TaxonomyReader.
    /// 
    /// <para><b>NOTE</b>: If you call {@link
    /// DirectoryTaxonomyWriter#replaceTaxonomy} then you must
    /// open a new {@code SearcherTaxonomyManager} afterwards.
    /// </para>
    /// </summary>
    public class SearcherTaxonomyManager : ReferenceManager<SearcherTaxonomyManager.SearcherAndTaxonomy>
    {
        /// <summary>
        /// Holds a matched pair of <seealso cref="IndexSearcher"/> and
        ///  <seealso cref="TaxonomyReader"/> 
        /// </summary>
        public class SearcherAndTaxonomy
        {
            /// <summary>
            /// Point-in-time <seealso cref="IndexSearcher"/>. </summary>
            public readonly IndexSearcher searcher;

            /// <summary>
            /// Matching point-in-time <seealso cref="DirectoryTaxonomyReader"/>. </summary>
            public readonly DirectoryTaxonomyReader taxonomyReader;

            /// <summary>
            /// Create a SearcherAndTaxonomy </summary>
            public SearcherAndTaxonomy(IndexSearcher searcher, DirectoryTaxonomyReader taxonomyReader)
            {
                this.searcher = searcher;
                this.taxonomyReader = taxonomyReader;
            }
        }

        private readonly SearcherFactory searcherFactory;
        private readonly long taxoEpoch;
        private readonly DirectoryTaxonomyWriter taxoWriter;

        /// <summary>
        /// Creates near-real-time searcher and taxonomy reader
        ///  from the corresponding writers. 
        /// </summary>
        public SearcherTaxonomyManager(IndexWriter writer, bool applyAllDeletes, 
            SearcherFactory searcherFactory, DirectoryTaxonomyWriter taxoWriter)
        {
            if (searcherFactory == null)
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
        /// <seealso cref="#maybeRefresh()"/> in the same thread. Otherwise it could lead to an
        /// unsync'd <seealso cref="IndexSearcher"/> and <seealso cref="TaxonomyReader"/> pair.
        /// </para>
        /// </summary>
        public SearcherTaxonomyManager(Store.Directory indexDir, Store.Directory taxoDir, SearcherFactory searcherFactory)
        {
            if (searcherFactory == null)
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
            @ref.searcher.IndexReader.DecRef();

            // This decRef can fail, and then in theory we should
            // tryIncRef the searcher to put back the ref count
            // ... but 1) the below decRef should only fail because
            // it decRef'd to 0 and closed and hit some IOException
            // during close, in which case 2) very likely the
            // searcher was also just closed by the above decRef and
            // a tryIncRef would fail:
            @ref.taxonomyReader.DecRef();
        }

        protected override bool TryIncRef(SearcherAndTaxonomy @ref)
        {
            if (@ref.searcher.IndexReader.TryIncRef())
            {
                if (@ref.taxonomyReader.TryIncRef())
                {
                    return true;
                }
                else
                {
                    @ref.searcher.IndexReader.DecRef();
                }
            }
            return false;
        }

        protected override SearcherAndTaxonomy RefreshIfNeeded(SearcherAndTaxonomy @ref)
        {
            // Must re-open searcher first, otherwise we may get a
            // new reader that references ords not yet known to the
            // taxonomy reader:
            IndexReader r = @ref.searcher.IndexReader;
            IndexReader newReader = DirectoryReader.OpenIfChanged((DirectoryReader)r);
            if (newReader == null)
            {
                return null;
            }
            else
            {
                var tr = TaxonomyReader.OpenIfChanged(@ref.taxonomyReader);
                if (tr == null)
                {
                    @ref.taxonomyReader.IncRef();
                    tr = @ref.taxonomyReader;
                }
                else if (taxoWriter != null && taxoWriter.TaxonomyEpoch != taxoEpoch)
                {
                    IOUtils.Close(newReader, tr);
                    throw new InvalidOperationException("DirectoryTaxonomyWriter.replaceTaxonomy was called, which is not allowed when using SearcherTaxonomyManager");
                }

                return new SearcherAndTaxonomy(SearcherManager.GetSearcher(searcherFactory, newReader), tr);
            }
        }

        protected override int GetRefCount(SearcherAndTaxonomy reference)
        {
            return reference.searcher.IndexReader.RefCount;
        }
    }
}