using Lucene.Net.Documents;
using Lucene.Net.Facet.Collections;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Taxonomy.Directory
{
    public class DirectoryTaxonomyReader : TaxonomyReader
    {
        private static readonly int DEFAULT_CACHE_VALUE = 4000;
        private readonly DirectoryTaxonomyWriter taxoWriter;
        private readonly long taxoEpoch;
        private readonly DirectoryReader indexReader;
        private LRUHashMap<CategoryPath, int?> ordinalCache;
        private LRUHashMap<int, CategoryPath> categoryCache;
        private volatile TaxonomyIndexArrays taxoArrays;

        private char delimiter = Consts.DEFAULT_DELIMITER;

        internal DirectoryTaxonomyReader(DirectoryReader indexReader, 
            DirectoryTaxonomyWriter taxoWriter, 
            LRUHashMap<CategoryPath, int?> ordinalCache, 
            LRUHashMap<int, CategoryPath> categoryCache, 
            TaxonomyIndexArrays taxoArrays)
        {
            this.indexReader = indexReader;
            this.taxoWriter = taxoWriter;
            this.taxoEpoch = taxoWriter == null ? -1 : taxoWriter.TaxonomyEpoch;
            this.ordinalCache = ordinalCache == null ? new LRUHashMap<CategoryPath, int?>(DEFAULT_CACHE_VALUE) : ordinalCache;
            this.categoryCache = categoryCache == null ? new LRUHashMap<int, CategoryPath>(DEFAULT_CACHE_VALUE) : categoryCache;
            this.taxoArrays = taxoArrays != null ? new TaxonomyIndexArrays(indexReader, taxoArrays) : null;
        }

        public DirectoryTaxonomyReader(Lucene.Net.Store.Directory directory)
        {
            indexReader = OpenIndexReader(directory);
            taxoWriter = null;
            taxoEpoch = -1;
            ordinalCache = new LRUHashMap<CategoryPath, int?>(DEFAULT_CACHE_VALUE);
            categoryCache = new LRUHashMap<int, CategoryPath>(DEFAULT_CACHE_VALUE);
        }

        public DirectoryTaxonomyReader(DirectoryTaxonomyWriter taxoWriter)
        {
            this.taxoWriter = taxoWriter;
            taxoEpoch = taxoWriter.TaxonomyEpoch;
            indexReader = OpenIndexReader(taxoWriter.InternalIndexWriter);
            ordinalCache = new LRUHashMap<CategoryPath, int?>(DEFAULT_CACHE_VALUE);
            categoryCache = new LRUHashMap<int, CategoryPath>(DEFAULT_CACHE_VALUE);
        }

        private void InitTaxoArrays()
        {
            lock (this)
            {
                if (taxoArrays == null)
                {
                    TaxonomyIndexArrays tmpArrays = new TaxonomyIndexArrays(indexReader);
                    taxoArrays = tmpArrays;
                }
            }
        }

        protected override void DoClose()
        {
            indexReader.Dispose();
            taxoArrays = null;
            ordinalCache = null;
            categoryCache = null;
        }

        protected override TaxonomyReader DoOpenIfChanged()
        {
            EnsureOpen();
            DirectoryReader r2 = DirectoryReader.OpenIfChanged(indexReader);
            if (r2 == null)
            {
                return null;
            }

            bool success = false;
            try
            {
                bool recreated = false;
                if (taxoWriter == null)
                {
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
                        recreated = true;
                    }
                }
                else
                {
                    if (taxoEpoch != taxoWriter.TaxonomyEpoch)
                    {
                        recreated = true;
                    }
                }

                DirectoryTaxonomyReader newtr;
                if (recreated)
                {
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
                    IOUtils.CloseWhileHandlingException((IDisposable)r2);
                }
            }
        }

        protected virtual DirectoryReader OpenIndexReader(Lucene.Net.Store.Directory directory)
        {
            return DirectoryReader.Open(directory);
        }

        protected virtual DirectoryReader OpenIndexReader(IndexWriter writer)
        {
            return DirectoryReader.Open(writer, false);
        }

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

        public override IDictionary<String, String> CommitUserData
        {
            get
            {
                EnsureOpen();
                return indexReader.IndexCommit.UserData;
            }
        }

        public override int GetOrdinal(CategoryPath cp)
        {
            EnsureOpen();
            if (cp.length == 0)
            {
                return ROOT_ORDINAL;
            }

            lock (ordinalCache)
            {
                int? res = ordinalCache[cp];
                if (res != null)
                {
                    if (res.Value < indexReader.MaxDoc)
                    {
                        return res.Value;
                    }
                    else
                    {
                        return TaxonomyReader.INVALID_ORDINAL;
                    }
                }
            }

            int ret = TaxonomyReader.INVALID_ORDINAL;
            DocsEnum docs = MultiFields.GetTermDocsEnum(indexReader, null, Consts.FULL, new BytesRef(cp.ToString(delimiter)), 0);
            if (docs != null && docs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
            {
                ret = docs.DocID;
                lock (ordinalCache)
                {
                    ordinalCache[cp] = ret;
                }
            }

            return ret;
        }

        public override CategoryPath GetPath(int ordinal)
        {
            EnsureOpen();
            if (ordinal < 0 || ordinal >= indexReader.MaxDoc)
            {
                return null;
            }

            int catIDInteger = ordinal;
            lock (categoryCache)
            {
                CategoryPath res = categoryCache[catIDInteger];
                if (res != null)
                {
                    return res;
                }
            }

            Document doc = indexReader.Document(ordinal);
            CategoryPath ret = new CategoryPath(doc.Get(Consts.FULL), delimiter);
            lock (categoryCache)
            {
                categoryCache[catIDInteger] = ret;
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

        public virtual void SetCacheSize(int size)
        {
            EnsureOpen();
            lock (categoryCache)
            {
                categoryCache.Capacity = size;
            }

            lock (ordinalCache)
            {
                ordinalCache.Capacity = size;
            }
        }

        public virtual void SetDelimiter(char delimiter)
        {
            EnsureOpen();
            this.delimiter = delimiter;
        }

        public virtual string ToString(int max)
        {
            EnsureOpen();
            StringBuilder sb = new StringBuilder();
            int upperl = Math.Min(max, indexReader.MaxDoc);
            for (int i = 0; i < upperl; i++)
            {
                try
                {
                    CategoryPath category = this.GetPath(i);
                    if (category == null)
                    {
                        sb.Append(i + @": NULL!! \n");
                        continue;
                    }

                    if (category.length == 0)
                    {
                        sb.Append(i + @": EMPTY STRING!! \n");
                        continue;
                    }

                    sb.Append(i + @": " + category.ToString() + @"\n");
                }
                catch (System.IO.IOException e)
                {
                    Debug.WriteLine(e.Message);
                }
            }

            return sb.ToString();
        }
    }
}
