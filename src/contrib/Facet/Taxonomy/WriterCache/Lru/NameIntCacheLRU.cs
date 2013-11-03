using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Taxonomy.WriterCache.Lru
{
    public class NameIntCacheLRU
    {
        private HashMap<Object, int?> cache;
        long nMisses = 0;
        long nHits = 0;
        private int maxCacheSize;

        internal NameIntCacheLRU(int maxCacheSize)
        {
            this.maxCacheSize = maxCacheSize;
            CreateCache(maxCacheSize);
        }

        public virtual int MaxSize
        {
            get
            {
                return maxCacheSize;
            }
        }

        public virtual int Size
        {
            get
            {
                return cache.Count;
            }
        }

        private void CreateCache(int maxSize)
        {
            cache = new HashMap<object, int?>(1000);
            //if (maxSize < int.MaxValue)
            //{
            //    cache = new HashMap<Object, int?>(1000, (float)0.7, true);
            //}
            //else
            //{
            //    cache = new HashMap<Object, int?>(1000, (float)0.7);
            //}
        }

        internal virtual int? Get(CategoryPath name)
        {
            int? res = cache[Key(name)];
            if (res == null)
            {
                nMisses++;
            }
            else
            {
                nHits++;
            }

            return res;
        }

        internal virtual Object Key(CategoryPath name)
        {
            return name;
        }

        internal virtual Object Key(CategoryPath name, int prefixLen)
        {
            return name.Subpath(prefixLen);
        }

        internal virtual bool Put(CategoryPath name, int? val)
        {
            cache[Key(name)] = val;
            return IsCacheFull;
        }

        internal virtual bool Put(CategoryPath name, int prefixLen, int? val)
        {
            cache[Key(name, prefixLen)] = val;
            return IsCacheFull;
        }

        private bool IsCacheFull
        {
            get
            {
                return cache.Count > maxCacheSize;
            }
        }

        internal virtual void Clear()
        {
            cache.Clear();
        }

        internal virtual string Stats()
        {
            return @"#miss=" + nMisses + @" #hit=" + nHits;
        }

        internal virtual bool MakeRoomLRU()
        {
            if (!IsCacheFull)
            {
                return false;
            }

            int n = cache.Count - (2 * maxCacheSize) / 3;
            if (n <= 0)
            {
                return false;
            }

            IEnumerator<Object> it = new HashSet<object>(cache.Keys).GetEnumerator();
            int i = 0;
            while (i < n && it.MoveNext())
            {
                //it.Current;
                cache.Remove(it.Current);
                i++;
            }

            return true;
        }
    }
}
