// Lucene version compatibility level 4.8.1
using Lucene.Net.Support;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

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

    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using BinaryDocValues = Lucene.Net.Index.BinaryDocValues;
    using DocValuesFormat = Lucene.Net.Codecs.DocValuesFormat;
    using IAccountable = Lucene.Net.Util.IAccountable;
    using Int32sRef = Lucene.Net.Util.Int32sRef;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;

    /// <summary>
    /// A per-segment cache of documents' facet ordinals. Every
    /// <see cref="CachedOrds"/> holds the ordinals in a raw <see cref="T:int[]"/>, 
    /// and therefore consumes as much RAM as the total
    /// number of ordinals found in the segment, but saves the
    /// CPU cost of decoding ordinals during facet counting.
    /// 
    /// <para>
    /// <b>NOTE:</b> every <see cref="CachedOrds"/> is limited to 2.1B
    /// total ordinals. If that is a limitation for you then
    /// consider limiting the segment size to fewer documents, or
    /// use an alternative cache which pages through the category
    /// ordinals.
    /// 
    /// </para>
    /// <para>
    /// <b>NOTE:</b> when using this cache, it is advised to use
    /// a <see cref="DocValuesFormat"/> that does not cache the data in
    /// memory, at least for the category lists fields, or
    /// otherwise you'll be doing double-caching.
    /// 
    /// </para>
    /// <para>
    /// <b>NOTE:</b> create one instance of this and re-use it
    /// for all facet implementations (the cache is per-instance,
    /// not static).
    /// </para>
    /// </summary>
    public class CachedOrdinalsReader : OrdinalsReader, IAccountable
    {
        private readonly OrdinalsReader source;

#if FEATURE_CONDITIONALWEAKTABLE_ENUMERATOR
        private readonly ConditionalWeakTable<object, CachedOrds> ordsCache = new ConditionalWeakTable<object, CachedOrds>();
        private readonly object ordsCacheLock = new object();
#else
        private readonly WeakDictionary<object, CachedOrds> ordsCache = new WeakDictionary<object, CachedOrds>();
        private readonly ReaderWriterLockSlim syncLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
#endif

        /// <summary>
        /// Sole constructor. </summary>
        public CachedOrdinalsReader(OrdinalsReader source)
        {
            this.source = source;
        }

        private CachedOrds GetCachedOrds(AtomicReaderContext context)
        {
            object cacheKey = context.Reader.CoreCacheKey;
#if FEATURE_CONDITIONALWEAKTABLE_ENUMERATOR
            return ordsCache.GetValue(cacheKey, (cacheKey) => new CachedOrds(source.GetReader(context), context.Reader.MaxDoc));
#else
            CachedOrds ords;
            syncLock.EnterReadLock();
            try
            {
                if (ordsCache.TryGetValue(cacheKey, out ords))
                    return ords;
            }
            finally
            {
                syncLock.ExitReadLock();
            }

            ords = new CachedOrds(source.GetReader(context), context.Reader.MaxDoc);
            syncLock.EnterWriteLock();
            try
            {
                ordsCache[cacheKey] = ords;
            }
            finally
            {
                syncLock.ExitWriteLock();
            }

            return ords;
#endif
        }

        public override string IndexFieldName => source.IndexFieldName;

        public override OrdinalsSegmentReader GetReader(AtomicReaderContext context)
        {
            CachedOrds cachedOrds = GetCachedOrds(context);
            return new OrdinalsSegmentReaderAnonymousClass(cachedOrds);
        }

        private class OrdinalsSegmentReaderAnonymousClass : OrdinalsSegmentReader
        {
            private readonly CachedOrds cachedOrds;

            public OrdinalsSegmentReaderAnonymousClass(CachedOrds cachedOrds)
            {
                this.cachedOrds = cachedOrds;
            }

            public override void Get(int docID, Int32sRef ordinals)
            {
                ordinals.Int32s = cachedOrds.Ordinals;
                ordinals.Offset = cachedOrds.Offsets[docID];
                ordinals.Length = cachedOrds.Offsets[docID + 1] - ordinals.Offset;
            }
        }

        /// <summary>
        /// Holds the cached ordinals in two parallel <see cref="T:int[]"/> arrays.
        /// </summary>
        public sealed class CachedOrds : IAccountable
        {
            /// <summary>
            /// Index into <see cref="Ordinals"/> for each document.
            /// </summary>
            [WritableArray]
            [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
            public int[] Offsets { get; private set; }

            /// <summary>
            /// Holds ords for all docs.
            /// </summary>
            [WritableArray]
            [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
            public int[] Ordinals { get; private set; }

            /// <summary>
            /// Creates a new <see cref="CachedOrds"/> from the <see cref="BinaryDocValues"/>.
            /// Assumes that the <see cref="BinaryDocValues"/> is not <c>null</c>.
            /// </summary>
            public CachedOrds(OrdinalsSegmentReader source, int maxDoc)
            {
                Offsets = new int[maxDoc + 1];
                int[] ords = new int[maxDoc]; // let's assume one ordinal per-document as an initial size

                // this aggregator is limited to Integer.MAX_VALUE total ordinals.
                long totOrds = 0;
                Int32sRef values = new Int32sRef(32);
                for (int docID = 0; docID < maxDoc; docID++)
                {
                    Offsets[docID] = (int)totOrds;
                    source.Get(docID, values);
                    long nextLength = totOrds + values.Length;
                    if (nextLength > ords.Length)
                    {
                        if (nextLength > ArrayUtil.MAX_ARRAY_LENGTH)
                        {
                            throw new ThreadStateException("too many ordinals (>= " + nextLength + ") to cache");
                        }
                        ords = ArrayUtil.Grow(ords, (int)nextLength);
                    }
                    Array.Copy(values.Int32s, 0, ords, (int)totOrds, values.Length);
                    totOrds = nextLength;
                }
                Offsets[maxDoc] = (int)totOrds;

                // if ords array is bigger by more than 10% of what we really need, shrink it
                if ((double)totOrds / ords.Length < 0.9)
                {
                    this.Ordinals = new int[(int)totOrds];
                    Array.Copy(ords, 0, this.Ordinals, 0, (int)totOrds);
                }
                else
                {
                    this.Ordinals = ords;
                }
            }

            public long RamBytesUsed()
            {
                long mem = RamUsageEstimator.ShallowSizeOf(this) + RamUsageEstimator.SizeOf(Offsets);
                if (Offsets != Ordinals)
                {
                    mem += RamUsageEstimator.SizeOf(Ordinals);
                }
                return mem;
            }
        }

        public virtual long RamBytesUsed()
        {
#if FEATURE_CONDITIONALWEAKTABLE_ENUMERATOR
            lock (ordsCacheLock)
#else
            syncLock.EnterReadLock();
            try
#endif
            {
                long bytes = 0;
                foreach (var pair in ordsCache)
                {
                    bytes += pair.Value.RamBytesUsed();
                }

                return bytes;
            }
#if !FEATURE_CONDITIONALWEAKTABLE_ENUMERATOR
            finally
            {
                syncLock.ExitReadLock();
            }
#endif
        }
    }
}