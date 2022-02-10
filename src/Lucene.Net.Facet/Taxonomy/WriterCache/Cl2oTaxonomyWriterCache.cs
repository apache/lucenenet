// Lucene version compatibility level 4.8.1
using Lucene.Net.Support.Threading;
using System;
using System.Threading;

namespace Lucene.Net.Facet.Taxonomy.WriterCache
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

    /// <summary>
    /// <see cref="ITaxonomyWriterCache"/> using <see cref="CompactLabelToOrdinal"/>. Although
    /// called cache, it maintains in memory all the mappings from category to
    /// ordinal, relying on that <see cref="CompactLabelToOrdinal"/> is an efficient
    /// mapping for this purpose.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class Cl2oTaxonomyWriterCache : ITaxonomyWriterCache
    {
        private readonly int initialCapcity, numHashArrays;
        private readonly float loadFactor;

        private volatile CompactLabelToOrdinal cache;

        // LUCENENET specific - use ReaderWriterLockSlim for better throughput
        private readonly ReaderWriterLockSlim syncLock = new ReaderWriterLockSlim();
        private readonly object disposalLock = new object();
        private bool isDisposed = false;

        /// <summary>
        /// Sole constructor.
        /// </summary>
        public Cl2oTaxonomyWriterCache(int initialCapcity, float loadFactor, int numHashArrays)
        {
            this.cache = new CompactLabelToOrdinal(initialCapcity, loadFactor, numHashArrays);
            this.initialCapcity = initialCapcity;
            this.numHashArrays = numHashArrays;
            this.loadFactor = loadFactor;
        }

        public virtual void Clear()
        {
            syncLock.EnterWriteLock();
            try
            {
                cache = new CompactLabelToOrdinal(initialCapcity, loadFactor, numHashArrays);
            }
            finally
            {
                syncLock.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) // LUCENENET specific - use proper dispose pattern
        {
            
            if (disposing)
            {
                if (isDisposed) return;

                // LUCENENET: Use additional lock to ensure our ReaderWriterLockSlim only gets
                // disposed by the first caller.
                UninterruptableMonitor.Enter(disposalLock);
                try
                {
                    if (isDisposed) return;
                    syncLock.EnterWriteLock();
                    try
                    {
                        cache = null;
                    }
                    finally
                    {
                        syncLock.ExitWriteLock();
                        isDisposed = true;
                        syncLock.Dispose();
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(disposalLock);
                }
            }
        }

        public virtual bool IsFull =>
            // This cache is never full
            false;

        public virtual int Get(FacetLabel categoryPath)
        {
            syncLock.EnterReadLock();
            try
            {
                return cache.GetOrdinal(categoryPath);
            }
            finally
            {
                syncLock.ExitReadLock();
            }
        }

        public virtual bool Put(FacetLabel categoryPath, int ordinal)
        {
            syncLock.EnterWriteLock();
            try
            {
                cache.AddLabel(categoryPath, ordinal);
                // Tell the caller we didn't clear part of the cache, so it doesn't
                // have to flush its on-disk index now
                return false;
            }
            finally
            {
                syncLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Returns the number of bytes in memory used by this object.
        /// </summary>
        public virtual int GetMemoryUsage()
        {
            return cache is null ? 0 : cache.GetMemoryUsage();
        }
    }
}