using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public class SearcherTaxonomyManager : ReferenceManager<SearcherTaxonomyManager.SearcherAndTaxonomy>
    {
        public class SearcherAndTaxonomy
        {
            public readonly IndexSearcher searcher;
            public readonly DirectoryTaxonomyReader taxonomyReader;
            
            internal SearcherAndTaxonomy(IndexSearcher searcher, DirectoryTaxonomyReader taxonomyReader)
            {
                this.searcher = searcher;
                this.taxonomyReader = taxonomyReader;
            }
        }

        private readonly SearcherFactory searcherFactory;
        private readonly long taxoEpoch;
        private readonly DirectoryTaxonomyWriter taxoWriter;

        public SearcherTaxonomyManager(IndexWriter writer, bool applyAllDeletes, SearcherFactory searcherFactory, DirectoryTaxonomyWriter taxoWriter)
        {
            if (searcherFactory == null)
            {
                searcherFactory = new SearcherFactory();
            }

            this.searcherFactory = searcherFactory;
            this.taxoWriter = taxoWriter;
            DirectoryTaxonomyReader taxoReader = new DirectoryTaxonomyReader(taxoWriter);
            current = new SearcherAndTaxonomy(SearcherManager.GetSearcher(searcherFactory, DirectoryReader.Open(writer, applyAllDeletes)), taxoReader);
            taxoEpoch = taxoWriter.TaxonomyEpoch;
        }

        protected override void DecRef(SearcherAndTaxonomy ref_renamed)
        {
            ref_renamed.searcher.IndexReader.DecRef();
            ref_renamed.taxonomyReader.DecRef();
        }

        protected override bool TryIncRef(SearcherAndTaxonomy ref_renamed)
        {
            if (ref_renamed.searcher.IndexReader.TryIncRef())
            {
                if (ref_renamed.taxonomyReader.TryIncRef())
                {
                    return true;
                }
                else
                {
                    ref_renamed.searcher.IndexReader.DecRef();
                }
            }

            return false;
        }

        protected override SearcherAndTaxonomy RefreshIfNeeded(SearcherAndTaxonomy ref_renamed)
        {
            IndexReader r = ref_renamed.searcher.IndexReader;
            IndexReader newReader = DirectoryReader.OpenIfChanged((DirectoryReader)r);
            if (newReader == null)
            {
                return null;
            }
            else
            {
                DirectoryTaxonomyReader tr = TaxonomyReader.OpenIfChanged(ref_renamed.taxonomyReader);
                if (tr == null)
                {
                    ref_renamed.taxonomyReader.IncRef();
                    tr = ref_renamed.taxonomyReader;
                }
                else if (taxoWriter.TaxonomyEpoch != taxoEpoch)
                {
                    IOUtils.Close(newReader, tr);
                    throw new InvalidOperationException(@"DirectoryTaxonomyWriter.replaceTaxonomy was called, which is not allowed when using SearcherTaxonomyManager");
                }

                return new SearcherAndTaxonomy(SearcherManager.GetSearcher(searcherFactory, newReader), tr);
            }
        }
    }
}
