using System;
using Lucene.Net.Index;
using Lucene.Net.Store;

namespace Lucene.Net.Search
{
    public class SearcherManager : ReferenceManager<IndexSearcher>
    {
        private readonly SearcherFactory searcherFactory;

        public SearcherManager(IndexWriter writer, bool applyAllDeletes, SearcherFactory searcherFactory)
        {
            if (searcherFactory == null)
            {
                searcherFactory = new SearcherFactory();
            }
            this.searcherFactory = new SearcherFactory();
            current = GetSearcher(searcherFactory, DirectoryReader.Open(writer, applyAllDeletes));
        }

        public SearcherManager(Directory dir, SearcherFactory searcherFactory)
        {
            if (searcherFactory == null)
            {
                searcherFactory = new SearcherFactory();
            }
            this.searcherFactory = searcherFactory;
            current = GetSearcher(searcherFactory, DirectoryReader.Open(dir));
        }

        protected override void DecRef(IndexSearcher reference)
        {
            reference.IndexReader.DecRef();
        }

        protected override IndexSearcher RefreshIfNeeded(IndexSearcher referenceToRefresh)
        {
            var r = referenceToRefresh.IndexReader;
            var reader = r as DirectoryReader;
            if (reader == null) throw new ArgumentException("searcher's IndexReader should be a DirectoryReader, but got " + r);
            var newReader = DirectoryReader.OpenIfChanged(reader);
            return newReader == null ? null : GetSearcher(searcherFactory, newReader);
        }

        protected override bool TryIncRef(IndexSearcher reference)
        {
            return reference.IndexReader.TryIncRef();
        }

        public bool IsSearcherCurrent()
        {
            var searcher = Acquire();
            try
            {
                var r = searcher.IndexReader;
                var reader = r as DirectoryReader;
                if (reader == null) throw new InvalidOperationException("searcher's IndexReader should be a DirectoryReader, but got " + r);
                return reader.IsCurrent;
            }
            finally
            {
                Release(searcher);
            }
        }

        public static IndexSearcher GetSearcher(SearcherFactory searcherFactory, IndexReader reader)
        {
            var success = false;
            IndexSearcher searcher;
            try
            {
                searcher = searcherFactory.NewSearcher(reader);
                if (searcher.IndexReader != reader)
                {
                    throw new InvalidOperationException("SearcherFactory must wrap exactly the provided reader (got " +
                                                        searcher.IndexReader + " but expected " + reader + ")");
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
