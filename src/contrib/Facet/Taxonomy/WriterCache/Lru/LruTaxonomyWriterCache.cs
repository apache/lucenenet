using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Taxonomy.WriterCache.Lru
{
    public class LruTaxonomyWriterCache : ITaxonomyWriterCache
    {
        public enum LRUType
        {
            LRU_HASHED,
            LRU_STRING
        }

        private NameIntCacheLRU cache;

        public LruTaxonomyWriterCache(int cacheSize)
            : this(cacheSize, LRUType.LRU_HASHED)
        {
        }

        public LruTaxonomyWriterCache(int cacheSize, LRUType lruType)
        {
            if (lruType == LRUType.LRU_HASHED)
            {
                this.cache = new NameHashIntCacheLRU(cacheSize);
            }
            else
            {
                this.cache = new NameIntCacheLRU(cacheSize);
            }
        }

        public bool IsFull
        {
            get
            {
                lock (this)
                {
                    return cache.Size == cache.MaxSize;
                }
            }
        }

        public void Clear()
        {
            lock (this)
            {
                cache.Clear();
            }
        }

        public void Close()
        {
            lock (this)
            {
                cache.Clear();
                cache = null;
            }
        }

        public int? Get(CategoryPath categoryPath)
        {
            lock (this)
            {
                int? res = cache.Get(categoryPath);
                if (res == null)
                {
                    return -1;
                }

                return res.Value;
            }
        }

        public bool Put(CategoryPath categoryPath, int? ordinal)
        {
            lock (this)
            {
                bool ret = cache.Put(categoryPath, ordinal);
                if (ret)
                {
                    cache.MakeRoomLRU();
                }

                return ret;
            }
        }
    }
}
