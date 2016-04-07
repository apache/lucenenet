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
    /// <seealso cref="TaxonomyWriterCache"/> using <seealso cref="CompactLabelToOrdinal"/>. Although
    /// called cache, it maintains in memory all the mappings from category to
    /// ordinal, relying on that <seealso cref="CompactLabelToOrdinal"/> is an efficient
    /// mapping for this purpose.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class Cl2oTaxonomyWriterCache : TaxonomyWriterCache
    {
        private const int LockTimeOut = 1000;
        private readonly ReaderWriterLockSlim @lock = new ReaderWriterLockSlim();
        private readonly int initialCapcity, numHashArrays;
        private readonly float loadFactor;

        private volatile CompactLabelToOrdinal cache;

        /// <summary>
        /// Sole constructor. </summary>
        public Cl2oTaxonomyWriterCache(int initialCapcity, float loadFactor, int numHashArrays)
        {
            this.cache = new CompactLabelToOrdinal(initialCapcity, loadFactor, numHashArrays);
            this.initialCapcity = initialCapcity;
            this.numHashArrays = numHashArrays;
            this.loadFactor = loadFactor;
        }

        public virtual void Clear()
        {
            if (@lock.TryEnterWriteLock(LockTimeOut))
            {
                try
                {
                    cache = new CompactLabelToOrdinal(initialCapcity, loadFactor, numHashArrays);
                }
                finally
                {
                    @lock.ExitWriteLock();
                }
            }
            else {
                //Throwing ArguementException to maintain behavoir with ReaderWriterLock.AquireWriteLock.
                throw new ArgumentException();
            }
        }

        public virtual void Close()
        {
            lock (this)
            {
                cache = null;
            }
        }

        public virtual bool Full
        {
            get
            {
                // This cache is never full
                return false;
            }
        }

        public virtual int Get(FacetLabel categoryPath)
        {
            if (@lock.TryEnterReadLock(LockTimeOut))
            {
                try
                {
                    return cache.GetOrdinal(categoryPath);
                }
                finally
                {
                    @lock.ExitReadLock();
                }
            }
            else
            {
                //Throwing ArguementException to maintain behavoir with ReaderWriterLock.AquireWriteLock.
                throw new ArgumentException();
            }
        }

        public virtual bool Put(FacetLabel categoryPath, int ordinal)
        {
            if (@lock.TryEnterWriteLock(LockTimeOut))
            {
                try
                {
                    cache.AddLabel(categoryPath, ordinal);
                    // Tell the caller we didn't clear part of the cache, so it doesn't
                    // have to flush its on-disk index now
                    return false;
                }
                finally
                {
                    @lock.ExitWriteLock();
                }
            }
            else
            {
                //Throwing ArguementException to maintain behavoir with ReaderWriterLock.AquireWriteLock.
                throw new ArgumentException();
            }
        }

        /// <summary>
        /// Returns the number of bytes in memory used by this object. </summary>
        public virtual int MemoryUsage
        {
            get
            {
                return cache == null ? 0 : cache.MemoryUsage;
            }
        }

    }

}