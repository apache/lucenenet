using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lucene.Net.Facet.Taxonomy.WriterCache.Cl2o
{
    public class Cl2oTaxonomyWriterCache : ITaxonomyWriterCache
    {
        private readonly ReaderWriterLockSlim lockobj = new ReaderWriterLockSlim();
        private readonly int initialCapcity, numHashArrays;
        private readonly float loadFactor;
        private volatile CompactLabelToOrdinal cache;

        public Cl2oTaxonomyWriterCache(int initialCapcity, float loadFactor, int numHashArrays)
        {
            this.cache = new CompactLabelToOrdinal(initialCapcity, loadFactor, numHashArrays);
            this.initialCapcity = initialCapcity;
            this.numHashArrays = numHashArrays;
            this.loadFactor = loadFactor;
        }

        public void Clear()
        {
            lockobj.EnterWriteLock();
            try
            {
                cache = new CompactLabelToOrdinal(initialCapcity, loadFactor, numHashArrays);
            }
            finally
            {
                lockobj.ExitWriteLock();
            }
        }

        public void Close()
        {
            lock (this)
            {
                cache = null;
            }
        }

        public bool IsFull
        {
            get
            {
                return false;
            }
        }

        public int? Get(CategoryPath categoryPath)
        {
            lockobj.EnterReadLock();
            try
            {
                return cache.GetOrdinal(categoryPath);
            }
            finally
            {
                lockobj.ExitReadLock();
            }
        }

        public bool Put(CategoryPath categoryPath, int? ordinal)
        {
            lockobj.EnterWriteLock();
            try
            {
                cache.AddLabel(categoryPath, ordinal.GetValueOrDefault());
                return false;
            }
            finally
            {
                lockobj.ExitWriteLock();
            }
        }

        public virtual int MemoryUsage
        {
            get
            {
                return cache == null ? 0 : cache.MemoryUsage;
            }
        }
    }
}
