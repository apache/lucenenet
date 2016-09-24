using Lucene.Net.Support;
using System;
using System.Collections.Generic;
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

    using BinaryDocValues = Lucene.Net.Index.BinaryDocValues;
    using Accountable = Lucene.Net.Util.Accountable;
    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using DocValuesFormat = Lucene.Net.Codecs.DocValuesFormat;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using IntsRef = Lucene.Net.Util.IntsRef;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;

    /// <summary>
    /// A per-segment cache of documents' facet ordinals. Every
    /// <seealso cref="CachedOrds"/> holds the ordinals in a raw {@code
    /// int[]}, and therefore consumes as much RAM as the total
    /// number of ordinals found in the segment, but saves the
    /// CPU cost of decoding ordinals during facet counting.
    /// 
    /// <para>
    /// <b>NOTE:</b> every <seealso cref="CachedOrds"/> is limited to 2.1B
    /// total ordinals. If that is a limitation for you then
    /// consider limiting the segment size to fewer documents, or
    /// use an alternative cache which pages through the category
    /// ordinals.
    /// 
    /// </para>
    /// <para>
    /// <b>NOTE:</b> when using this cache, it is advised to use
    /// a <seealso cref="DocValuesFormat"/> that does not cache the data in
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
    public class CachedOrdinalsReader : OrdinalsReader, Accountable
    {
        private readonly OrdinalsReader source;

        private readonly IDictionary<object, CachedOrds> ordsCache = new WeakDictionary<object, CachedOrds>();

        /// <summary>
        /// Sole constructor. </summary>
        public CachedOrdinalsReader(OrdinalsReader source)
        {
            this.source = source;
        }

        private CachedOrds GetCachedOrds(AtomicReaderContext context)
        {
            lock (this)
            {
                object cacheKey = context.Reader.CoreCacheKey;
                CachedOrds ords = ordsCache[cacheKey];
                if (ords == null)
                {
                    ords = new CachedOrds(source.GetReader(context), context.Reader.MaxDoc);
                    ordsCache[cacheKey] = ords;
                }

                return ords;
            }
        }

        public override string IndexFieldName
        {
            get
            {
                return source.IndexFieldName;
            }
        }

        public override OrdinalsSegmentReader GetReader(AtomicReaderContext context)
        {
            CachedOrds cachedOrds = GetCachedOrds(context);
            return new OrdinalsSegmentReaderAnonymousInnerClassHelper(this, cachedOrds);
        }

        private class OrdinalsSegmentReaderAnonymousInnerClassHelper : OrdinalsSegmentReader
        {
            private readonly CachedOrdinalsReader outerInstance;

            private Lucene.Net.Facet.Taxonomy.CachedOrdinalsReader.CachedOrds cachedOrds;

            public OrdinalsSegmentReaderAnonymousInnerClassHelper(CachedOrdinalsReader outerInstance, Lucene.Net.Facet.Taxonomy.CachedOrdinalsReader.CachedOrds cachedOrds)
            {
                this.outerInstance = outerInstance;
                this.cachedOrds = cachedOrds;
            }

            public override void Get(int docID, IntsRef ordinals)
            {
                ordinals.Ints = cachedOrds.ordinals;
                ordinals.Offset = cachedOrds.offsets[docID];
                ordinals.Length = cachedOrds.offsets[docID + 1] - ordinals.Offset;
            }
        }

        /// <summary>
        /// Holds the cached ordinals in two parallel {@code int[]} arrays. </summary>
        public sealed class CachedOrds : Accountable
        {
            /// <summary>
            /// Index into <seealso cref="#ordinals"/> for each document. </summary>
            public readonly int[] offsets;

            /// <summary>
            /// Holds ords for all docs. </summary>
            public readonly int[] ordinals;

            /// <summary>
            /// Creates a new <seealso cref="CachedOrds"/> from the <seealso cref="BinaryDocValues"/>.
            /// Assumes that the <seealso cref="BinaryDocValues"/> is not {@code null}.
            /// </summary>
            public CachedOrds(OrdinalsSegmentReader source, int maxDoc)
            {
                offsets = new int[maxDoc + 1];
                int[] ords = new int[maxDoc]; // let's assume one ordinal per-document as an initial size

                // this aggregator is limited to Integer.MAX_VALUE total ordinals.
                long totOrds = 0;
                IntsRef values = new IntsRef(32);
                for (int docID = 0; docID < maxDoc; docID++)
                {
                    offsets[docID] = (int)totOrds;
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
                    Array.Copy(values.Ints, 0, ords, (int)totOrds, values.Length);
                    totOrds = nextLength;
                }
                offsets[maxDoc] = (int)totOrds;

                // if ords array is bigger by more than 10% of what we really need, shrink it
                if ((double)totOrds / ords.Length < 0.9)
                {
                    this.ordinals = new int[(int)totOrds];
                    Array.Copy(ords, 0, this.ordinals, 0, (int)totOrds);
                }
                else
                {
                    this.ordinals = ords;
                }
            }

            public long RamBytesUsed()
            {
                long mem = RamUsageEstimator.ShallowSizeOf(this) + RamUsageEstimator.SizeOf(offsets);
                if (offsets != ordinals)
                {
                    mem += RamUsageEstimator.SizeOf(ordinals);
                }
                return mem;
            }
        }

        public virtual long RamBytesUsed()
        {
            lock (this)
            {
                long bytes = 0;
                foreach (CachedOrds ords in ordsCache.Values)
                {
                    bytes += ords.RamBytesUsed();
                }

                return bytes;
            }
        }
    }
}