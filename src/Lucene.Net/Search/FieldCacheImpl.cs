using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Runtime.CompilerServices;
using Lucene.Net.Support;
using Lucene.Net.Support.IO;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
#if !FEATURE_CONDITIONALWEAKTABLE_ENUMERATOR
using Prism.Events;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Search
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

    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using BinaryDocValues = Lucene.Net.Index.BinaryDocValues;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using DocTermOrds = Lucene.Net.Index.DocTermOrds;
    using DocValues = Lucene.Net.Index.DocValues;
    using FieldCacheSanityChecker = Lucene.Net.Util.FieldCacheSanityChecker;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using GrowableWriter = Lucene.Net.Util.Packed.GrowableWriter;
    using IBits = Lucene.Net.Util.IBits;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using MonotonicAppendingInt64Buffer = Lucene.Net.Util.Packed.MonotonicAppendingInt64Buffer;
    using NumericDocValues = Lucene.Net.Index.NumericDocValues;
    using PackedInt32s = Lucene.Net.Util.Packed.PackedInt32s;
    using PagedBytes = Lucene.Net.Util.PagedBytes;
    using SegmentReader = Lucene.Net.Index.SegmentReader;
    using SortedDocValues = Lucene.Net.Index.SortedDocValues;
    using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /// <summary>
    /// Expert: The default cache implementation, storing all values in memory.
    /// A WeakHashMap is used for storage.
    /// <para/>
    /// @since   lucene 1.4
    /// </summary>
    internal class FieldCacheImpl : IFieldCache
    {
        // LUCENENET specific - eliminated unnecessary Dictionary lookup by declaring each cache as a member variable
        private ByteCache caches_typeof_sbyte;
        private Int16Cache caches_typeof_short;
        private Int32Cache caches_typeof_int;
        private SingleCache caches_typeof_float;
        private Int64Cache caches_typeof_long;
        private DoubleCache caches_typeof_double;
        private BinaryDocValuesCache caches_typeof_BinaryDocValues;
        private SortedDocValuesCache caches_typeof_SortedDocValues;
        private DocTermOrdsCache caches_typeof_DocTermOrds;
        private DocsWithFieldCache caches_typeof_DocsWithFieldCache;
        internal FieldCacheImpl()
        {
            Init();

            //Have to do this here because no 'this' in class definition
            purgeCore = new CoreClosedListenerAnonymousClass(this);
            purgeReader = new ReaderClosedListenerAnonymousClass(this);
        }

        private void Init()
        {
            // LUCENENET specific - removed unnecessary lock during construction

            // LUCENENET specific - eliminated unnecessary Dictionary lookup by declaring each cache as a member variable
            caches_typeof_sbyte              = new ByteCache(this);
            caches_typeof_short              = new Int16Cache(this);
            caches_typeof_int                = new Int32Cache(this);
            caches_typeof_float              = new SingleCache(this);
            caches_typeof_long               = new Int64Cache(this);
            caches_typeof_double             = new DoubleCache(this);
            caches_typeof_BinaryDocValues    = new BinaryDocValuesCache(this);
            caches_typeof_SortedDocValues    = new SortedDocValuesCache(this);
            caches_typeof_DocTermOrds        = new DocTermOrdsCache(this);
            caches_typeof_DocsWithFieldCache = new DocsWithFieldCache(this);
        }

        public virtual void PurgeAllCaches()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                Init();
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public virtual void PurgeByCacheKey(object coreCacheKey)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                // LUCENENET specific - removed unnecessary Dictionary and loop
                caches_typeof_sbyte.PurgeByCacheKey(coreCacheKey);
                caches_typeof_short.PurgeByCacheKey(coreCacheKey);
                caches_typeof_int.PurgeByCacheKey(coreCacheKey);
                caches_typeof_float.PurgeByCacheKey(coreCacheKey);
                caches_typeof_long.PurgeByCacheKey(coreCacheKey);
                caches_typeof_double.PurgeByCacheKey(coreCacheKey);
                caches_typeof_BinaryDocValues.PurgeByCacheKey(coreCacheKey);
                caches_typeof_SortedDocValues.PurgeByCacheKey(coreCacheKey);
                caches_typeof_DocTermOrds.PurgeByCacheKey(coreCacheKey);
                caches_typeof_DocsWithFieldCache.PurgeByCacheKey(coreCacheKey);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public virtual FieldCache.CacheEntry[] GetCacheEntries()
        {
            // LUCENENET specific - instantiate/ToArray() outside of lock to improve performance
            IList<FieldCache.CacheEntry> result = new JCG.List<FieldCache.CacheEntry>(17);
            UninterruptableMonitor.Enter(this);
            try
            {
                // LUCENENET specific - refactored to use generic CacheKey to reduce casting and removed unnecessary Dictionary/loop
                AddCacheEntries(result, typeof(sbyte), caches_typeof_sbyte);
                AddCacheEntries(result, typeof(short), caches_typeof_short);
                AddCacheEntries(result, typeof(int), caches_typeof_int);
                AddCacheEntries(result, typeof(float), caches_typeof_float);
                AddCacheEntries(result, typeof(long), caches_typeof_long);
                AddCacheEntries(result, typeof(double), caches_typeof_double);
                AddCacheEntries(result, typeof(BinaryDocValues), caches_typeof_BinaryDocValues);
                AddCacheEntries(result, typeof(SortedDocValues), caches_typeof_SortedDocValues);
                AddCacheEntries(result, typeof(DocTermOrds), caches_typeof_DocTermOrds);
                AddCacheEntries(result, typeof(DocsWithFieldCache), caches_typeof_DocsWithFieldCache);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
            return result.ToArray();
        }

        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "False positive")]
        private void AddCacheEntries<TKey, TValue>(IList<FieldCache.CacheEntry> result, Type cacheType, Cache<TKey, TValue> cache) where TKey : CacheKey
        {
            UninterruptableMonitor.Enter(cache.readerCache);
            try
            {
#if FEATURE_CONDITIONALWEAKTABLE_ADDORUPDATE
                foreach (var readerCacheEntry in cache.readerCache)
                {
                    object readerKey = readerCacheEntry.Key;
                    if (readerKey is null)
                    {
                        continue;
                    }
                    IDictionary<TKey, object> innerCache = readerCacheEntry.Value;
                    foreach (KeyValuePair<TKey, object> mapEntry in innerCache)
                    {
                        TKey entry = mapEntry.Key;
                        result.Add(new FieldCache.CacheEntry(readerKey, entry.field, cacheType, entry.Custom, mapEntry.Value));
                    }
                }
#else
                // LUCENENET specific - since .NET Standard 2.0 and .NET Framework don't have a CondtionalWeakTable enumerator,
                // we use a weak event to retrieve the readerKey instances and then lookup the values in the table one by one.
                var e = new Events.GetCacheKeysEventArgs();
                eventAggregator.GetEvent<Events.GetCacheKeysEvent>().Publish(e);
                foreach (object readerKey in e.CacheKeys)
                {
                    if (cache.readerCache.TryGetValue(readerKey, out IDictionary<TKey, object> innerCache))
                    {
                        foreach (KeyValuePair<TKey, object> mapEntry in innerCache)
                        {
                            TKey entry = mapEntry.Key;
                            result.Add(new FieldCache.CacheEntry(readerKey, entry.field, cacheType, entry.Custom, mapEntry.Value));
                        }
                    }
                }
#endif
            }
            finally
            {
                UninterruptableMonitor.Exit(cache.readerCache);
            }
        }

        // per-segment fieldcaches don't purge until the shared core closes.
        internal readonly SegmentReader.ICoreDisposedListener purgeCore;

        private sealed class CoreClosedListenerAnonymousClass : SegmentReader.ICoreDisposedListener
        {
            private readonly FieldCacheImpl outerInstance;

            public CoreClosedListenerAnonymousClass(FieldCacheImpl outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public void OnDispose(object ownerCoreCacheKey)
            {
                outerInstance.PurgeByCacheKey(ownerCoreCacheKey);
            }
        }

        // composite/SlowMultiReaderWrapper fieldcaches don't purge until composite reader is closed.
        internal readonly IReaderDisposedListener purgeReader;

        private sealed class ReaderClosedListenerAnonymousClass : IReaderDisposedListener
        {
            private readonly FieldCacheImpl outerInstance;

            public ReaderClosedListenerAnonymousClass(FieldCacheImpl outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public void OnDispose(IndexReader owner)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(owner is AtomicReader);
                outerInstance.PurgeByCacheKey(((AtomicReader)owner).CoreCacheKey);
            }
        }

        private void InitReader(AtomicReader reader)
        {
            if (reader is SegmentReader segmentReader)
            {
                segmentReader.AddCoreDisposedListener(purgeCore);
            }
            else
            {
                // we have a slow reader of some sort, try to register a purge event
                // rather than relying on gc:
                object key = reader.CoreCacheKey;
                if (key is AtomicReader atomicReader)
                {
                    atomicReader.AddReaderDisposedListener(purgeReader);
                }
                else
                {
                    // last chance
                    reader.AddReaderDisposedListener(purgeReader);
                }
            }
#if !FEATURE_CONDITIONALWEAKTABLE_ENUMERATOR
            // LUCENENET specific - since .NET Standard 2.0 and .NET Framework don't have a CondtionalWeakTable enumerator,
            // we use a weak event to retrieve the readerKey instances
            reader.SubscribeToGetCacheKeysEvent(eventAggregator.GetEvent<Events.GetCacheKeysEvent>());
#endif
        }

#if !FEATURE_CONDITIONALWEAKTABLE_ENUMERATOR
        // LUCENENET specific: Add weak event handler for .NET Standard 2.0 and .NET Framework, since we don't have an enumerator to use
        private readonly IEventAggregator eventAggregator = new EventAggregator();
#endif

        /// <summary>
        /// Expert: Internal cache. </summary>
        internal abstract class Cache<TKey, TValue> where TKey : CacheKey
        {
            private protected Cache(FieldCacheImpl wrapper) // LUCENENET: Changed from internal to private protected
            {
                this.wrapper = wrapper;
            }

            internal readonly FieldCacheImpl wrapper;

            internal ConditionalWeakTable<object, IDictionary<TKey, object>> readerCache = new ConditionalWeakTable<object, IDictionary<TKey, object>>();

            protected abstract TValue CreateValue(AtomicReader reader, TKey key, bool setDocsWithField);

            /// <summary>
            /// Remove this reader from the cache, if present. </summary>
            public virtual void PurgeByCacheKey(object coreCacheKey)
            {
                UninterruptableMonitor.Enter(readerCache);
                try
                {
                    readerCache.Remove(coreCacheKey);
                }
                finally
                {
                    UninterruptableMonitor.Exit(readerCache);
                }
            }

            /// <summary>
            /// Sets the key to the value for the provided reader;
            /// if the key is already set then this doesn't change it.
            /// </summary>
            public virtual void Put(AtomicReader reader, TKey key, TValue value)
            {
                IDictionary<TKey, object> innerCache;
                object readerKey = reader.CoreCacheKey;
                UninterruptableMonitor.Enter(readerCache);
                try
                {
                    if (!readerCache.TryGetValue(readerKey, out innerCache) || innerCache is null)
                    {
                        // First time this reader is using FieldCache
                        innerCache = new Dictionary<TKey, object>
                        {
                            [key] = value
                        };
                        readerCache.AddOrUpdate(readerKey, innerCache);
                        wrapper.InitReader(reader);
                    }
                    if (!innerCache.TryGetValue(key, out object temp) || temp is null)
                        innerCache[key] = value;
                    // else if another thread beat us to it, leave the current value
                }
                finally
                {
                    UninterruptableMonitor.Exit(readerCache);
                }
            }

            public virtual TValue Get(AtomicReader reader, TKey key, bool setDocsWithField)
            {
                IDictionary<TKey, object> innerCache;
                object value = null;
                object readerKey = reader.CoreCacheKey;
                UninterruptableMonitor.Enter(readerCache);
                try
                {
                    if (!readerCache.TryGetValue(readerKey, out innerCache) || innerCache is null)
                    {
                        // First time this reader is using FieldCache
                        innerCache = new Dictionary<TKey, object>
                        {
                            [key] = value = new FieldCache.CreationPlaceholder<TValue>()
                        };
                        readerCache.AddOrUpdate(readerKey, innerCache);
                        wrapper.InitReader(reader);
                    }
                    // LUCENENET: The creation steps above will ensure the placehoder already exists by
                    // this point only in the case where the dictionary is being added.
                    // But we need to cover
                    // 1) the case where the cache already has a dictionary but no value
                    // 2) the case where the cache already has a dictionary and a null value
                    // so we diverge a little from Lucene here.
                    if (value is null)
                    {
                        if (!innerCache.TryGetValue(key, out value) || value is null)
                            innerCache[key] = value = new FieldCache.CreationPlaceholder<TValue>();
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(readerCache);
                }
                if (value is FieldCache.CreationPlaceholder<TValue> progress)
                {
                    UninterruptableMonitor.Enter(value);
                    try
                    {
                        if (progress.Value is null)
                        {
                            progress.Value = CreateValue(reader, key, setDocsWithField);
                            UninterruptableMonitor.Enter(readerCache);
                            try
                            {
                                innerCache[key] = progress.Value;
                            }
                            finally
                            {
                                UninterruptableMonitor.Exit(readerCache);
                            }
                            // Only check if key.custom (the parser) is
                            // non-null; else, we check twice for a single
                            // call to FieldCache.getXXX
                            if (!(key.Custom is null) && !(wrapper is null))
                            {
                                TextWriter infoStream = wrapper.InfoStream;
                                if (!(infoStream is null))
                                {
                                    PrintNewInsanity(infoStream, progress.Value);
                                }
                            }
                        }
                        return progress.Value;
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(value);
                    }
                }
                return (TValue)value;
            }

            private void PrintNewInsanity(TextWriter infoStream, TValue value)
            {
                FieldCacheSanityChecker.Insanity[] insanities = FieldCacheSanityChecker.CheckSanity(wrapper);
                for (int i = 0; i < insanities.Length; i++)
                {
                    FieldCacheSanityChecker.Insanity insanity = insanities[i];
                    FieldCache.CacheEntry[] entries = insanity.CacheEntries;
                    for (int j = 0; j < entries.Length; j++)
                    {
                        if (ReferenceEquals(entries[j].Value, value))
                        {
                            // OK this insanity involves our entry
                            infoStream.WriteLine("WARNING: new FieldCache insanity created\nDetails: " + insanity.ToString());
                            infoStream.WriteLine("\nStack:\n");
                            infoStream.WriteLine(new Exception().StackTrace);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Expert: Every composite-key in the internal cache is of this type. </summary>
        internal class CacheKey
        {
            internal readonly string field; // which Field
            // LUCENENET specific - moved 'custom' to generic class so we don't have to deal with casting/boxing

            /// <summary>
            /// Creates one of these objects for a custom comparer/parser. </summary>
            internal CacheKey(string field)
            {
                this.field = field;
            }

            // LUCENENET specific - Added this property to add this value to a FieldCache.CacheEntry without
            // knowing its generic closing type.
            public virtual object Custom => null;

            /// <summary>
            /// Two of these are equal if they reference the same field and type. </summary>
            public override bool Equals(object o)
            {
                if (o is CacheKey other && other.field.Equals(field, StringComparison.Ordinal))
                {
                    if (other.Custom is null)
                    {
                        return Custom is null;
                    }
                    else if (other.Custom.Equals(Custom))
                    {
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// Composes a hashcode based on the field and type. </summary>
#pragma warning disable IDE0070 // Use 'System.HashCode'
            public override int GetHashCode()
#pragma warning restore IDE0070 // Use 'System.HashCode'
            {
                return field.GetHashCode();
            }
        }

        /// <summary>
        /// Expert: Every composite-key in the internal cache is of this type. </summary>
        // LUCENENET specific - Added generic parameter to eliminate casting/boxing
        internal class CacheKey<TCustom> : CacheKey
        {
            internal readonly TCustom custom; // which custom comparer or parser 

            /// <summary>
            /// Creates one of these objects for a custom comparer/parser. </summary>
            internal CacheKey(string field, TCustom custom)
                : base(field)
            {
                this.custom = custom;
            }

            public override object Custom => custom;

            /// <summary>
            /// Two of these are equal if they reference the same field and type. </summary>
            public override bool Equals(object o)
            {
                if (o is CacheKey<TCustom> other)
                {
                    if (other.field.Equals(field, StringComparison.Ordinal))
                    {
                        if (other.custom is null)
                        {
                            return custom is null;
                        }
                        else if (other.custom.Equals(custom))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            /// <summary>
            /// Composes a hashcode based on the field and type. </summary>
            public override int GetHashCode()
            {
                return field.GetHashCode() ^ (custom is null ? 0 : custom.GetHashCode());
            }
        }

        private abstract class Uninvert
        {
            internal IBits docsWithField; // LUCENENET NOTE: Changed from public to internal, since FieldCacheImpl is internal anyway

            public virtual void DoUninvert(AtomicReader reader, string field, bool setDocsWithField)
            {
                int maxDoc = reader.MaxDoc;
                Terms terms = reader.GetTerms(field);
                if (terms != null)
                {
                    if (setDocsWithField)
                    {
                        int termsDocCount = terms.DocCount;
                        if (Debugging.AssertsEnabled) Debugging.Assert(termsDocCount <= maxDoc);
                        if (termsDocCount == maxDoc)
                        {
                            // Fast case: all docs have this field:
                            this.docsWithField = new Lucene.Net.Util.Bits.MatchAllBits(maxDoc);
                            setDocsWithField = false;
                        }
                    }

                    TermsEnum termsEnum = GetTermsEnum(terms);

                    DocsEnum docs = null;
                    FixedBitSet docsWithField = null;
                    while (termsEnum.MoveNext())
                    {
                        VisitTerm(termsEnum.Term);
                        docs = termsEnum.Docs(null, docs, DocsFlags.NONE);
                        while (true)
                        {
                            int docID = docs.NextDoc();
                            if (docID == DocIdSetIterator.NO_MORE_DOCS)
                            {
                                break;
                            }
                            VisitDoc(docID);
                            if (setDocsWithField)
                            {
                                if (docsWithField is null)
                                {
                                    // Lazy init
                                    this.docsWithField = docsWithField = new FixedBitSet(maxDoc);
                                }
                                docsWithField.Set(docID);
                            }
                        }
                    }
                }
            }

            protected abstract TermsEnum GetTermsEnum(Terms terms); // LUCENENET specific - renamed from TermsEnum()

            protected abstract void VisitTerm(BytesRef term);

            protected abstract void VisitDoc(int docID);
        }

        // null Bits means no docs matched
        internal virtual void SetDocsWithField(AtomicReader reader, string field, IBits docsWithField)
        {
            int maxDoc = reader.MaxDoc;
            IBits bits;
            if (docsWithField is null)
            {
                bits = new Lucene.Net.Util.Bits.MatchNoBits(maxDoc);
            }
            else if (docsWithField is FixedBitSet fixedBitSet)
            {
                int numSet = fixedBitSet.Cardinality;
                if (numSet >= maxDoc)
                {
                    // The cardinality of the BitSet is maxDoc if all documents have a value.
                    if (Debugging.AssertsEnabled) Debugging.Assert(numSet == maxDoc);
                    bits = new Lucene.Net.Util.Bits.MatchAllBits(maxDoc);
                }
                else
                {
                    bits = docsWithField;
                }
            }
            else
            {
                bits = docsWithField;
            }
            // LUCENENET specific - eliminated unnecessary Dictionary lookup by declaring each cache as a member variable
            caches_typeof_DocsWithFieldCache.Put(reader, new CacheKey(field), bits);
        }

        /// <summary>
        /// Checks the internal cache for an appropriate entry, and if none is
        /// found, reads the terms in <paramref name="field"/> as a single <see cref="byte"/> and returns an array
        /// of size <c>reader.MaxDoc</c> of the value each document
        /// has in the given field. </summary>
        /// <param name="reader">  Used to get field values. </param>
        /// <param name="field">   Which field contains the single <see cref="byte"/> values. </param>
        /// <param name="setDocsWithField">  If true then <see cref="GetDocsWithField(AtomicReader, string)"/> will
        ///        also be computed and stored in the <see cref="IFieldCache"/>. </param>
        /// <returns> The values in the given field for each document. </returns>
        /// <exception cref="IOException">  If any error occurs. </exception>
        [Obsolete("(4.4) Index as a numeric field using Int32Field and then use GetInt32s(AtomicReader, string, bool) instead.")]
        public virtual FieldCache.Bytes GetBytes(AtomicReader reader, string field, bool setDocsWithField)
        {
            return GetBytes(reader, field, null, setDocsWithField);
        }

#pragma warning disable 612, 618
        public virtual FieldCache.Bytes GetBytes(AtomicReader reader, string field, FieldCache.IByteParser parser, bool setDocsWithField)
#pragma warning restore 612, 618
        {
            NumericDocValues valuesIn = reader.GetNumericDocValues(field);
            if (valuesIn != null)
            {
                // Not cached here by FieldCacheImpl (cached instead
                // per-thread by SegmentReader):
                return new FieldCache_BytesAnonymousClass(valuesIn);
            }
            else
            {
                FieldInfo info = reader.FieldInfos.FieldInfo(field);
                if (info is null)
                {
                    return FieldCache.Bytes.EMPTY;
                }
                else if (info.HasDocValues)
                {
                    throw IllegalStateException.Create("Type mismatch: " + field + " was indexed as " + info.DocValuesType);
                }
                else if (!info.IsIndexed)
                {
                    return FieldCache.Bytes.EMPTY;
                }
                // LUCENENET specific - eliminated unnecessary Dictionary lookup by declaring each cache as a member variable
#pragma warning disable CS0612 // Type or member is obsolete
                return caches_typeof_sbyte.Get(reader, new CacheKey<FieldCache.IByteParser>(field, parser), setDocsWithField);
#pragma warning restore CS0612 // Type or member is obsolete
            }
        }

        private sealed class FieldCache_BytesAnonymousClass : FieldCache.Bytes
        {
            private readonly NumericDocValues valuesIn;

            public FieldCache_BytesAnonymousClass(NumericDocValues valuesIn)
            {
                this.valuesIn = valuesIn;
            }

            public override byte Get(int docID)
            {
                return (byte)valuesIn.Get(docID);
            }
        }

        internal class BytesFromArray : FieldCache.Bytes
        {
            private readonly sbyte[] values;

            public BytesFromArray(sbyte[] values)
            {
                this.values = values;
            }

            public override byte Get(int docID)
            {
                return (byte)values[docID];
            }
        }

#pragma warning disable CS0612 // Type or member is obsolete
        internal sealed class ByteCache : Cache<CacheKey<FieldCache.IByteParser>, FieldCache.Bytes>
#pragma warning restore CS0612 // Type or member is obsolete
        {
            internal ByteCache(FieldCacheImpl wrapper)
                : base(wrapper)
            {
            }

#pragma warning disable CS0612 // Type or member is obsolete
            protected override FieldCache.Bytes CreateValue(AtomicReader reader, CacheKey<FieldCache.IByteParser> key, bool setDocsWithField)
#pragma warning restore CS0612 // Type or member is obsolete
            {
                int maxDoc = reader.MaxDoc;
                sbyte[] values;
#pragma warning disable 612, 618
                FieldCache.IByteParser parser = key.custom;
#pragma warning restore 612, 618
                if (parser is null)
                {
                    // Confusing: must delegate to wrapper (vs simply
                    // setting parser = DEFAULT_INT16_PARSER) so cache
                    // key includes DEFAULT_INT16_PARSER:
#pragma warning disable 612, 618
                    return wrapper.GetBytes(reader, key.field, FieldCache.DEFAULT_BYTE_PARSER, setDocsWithField);
#pragma warning restore 612, 618
                }

                values = new sbyte[maxDoc];

                Uninvert u = new UninvertAnonymousClass(values, parser);

                u.DoUninvert(reader, key.field, setDocsWithField);

                if (setDocsWithField)
                {
                    wrapper.SetDocsWithField(reader, key.field, u.docsWithField);
                }

                return new BytesFromArray(values);
            }

            private sealed class UninvertAnonymousClass : Uninvert
            {
                private readonly sbyte[] values;
#pragma warning disable 612, 618
                private readonly FieldCache.IByteParser parser;

                public UninvertAnonymousClass(sbyte[] values, FieldCache.IByteParser parser)
#pragma warning restore 612, 618
                {
                    this.values = values;
                    this.parser = parser;
                }

                private sbyte currentValue;

                protected override void VisitTerm(BytesRef term)
                {
                    currentValue = (sbyte)parser.ParseByte(term);
                }

                protected override void VisitDoc(int docID)
                {
                    values[docID] = currentValue;
                }

                protected override TermsEnum GetTermsEnum(Terms terms)
                {
                    return parser.GetTermsEnum(terms);
                }
            }
        }

        /// <summary>
        /// Checks the internal cache for an appropriate entry, and if none is
        /// found, reads the terms in <paramref name="field"/> as <see cref="short"/>s and returns an array
        /// of size <c>reader.MaxDoc</c> of the value each document
        /// has in the given field. 
        /// <para/>
        /// NOTE: this was getShorts() in Lucene
        /// </summary>
        /// <param name="reader">  Used to get field values. </param>
        /// <param name="field">   Which field contains the <see cref="short"/>s. </param>
        /// <param name="setDocsWithField">  If true then <see cref="GetDocsWithField(AtomicReader, string)"/> will
        ///        also be computed and stored in the <see cref="IFieldCache"/>. </param>
        /// <returns> The values in the given field for each document. </returns>
        /// <exception cref="IOException">  If any error occurs. </exception>
        [Obsolete("(4.4) Index as a numeric field using Int32Field and then use GetInt32s(AtomicReader, string, bool) instead.")]
        public virtual FieldCache.Int16s GetInt16s(AtomicReader reader, string field, bool setDocsWithField)
        {
            return GetInt16s(reader, field, null, setDocsWithField);
        }

        /// <summary>
        /// Checks the internal cache for an appropriate entry, and if none is found,
        /// reads the terms in <paramref name="field"/> as shorts and returns an array of
        /// size <c>reader.MaxDoc</c> of the value each document has in the
        /// given field. 
        /// <para/>
        /// NOTE: this was getShorts() in Lucene
        /// </summary>
        /// <param name="reader">  Used to get field values. </param>
        /// <param name="field">   Which field contains the <see cref="short"/>s. </param>
        /// <param name="parser">  Computes <see cref="short"/> for string values. </param>
        /// <param name="setDocsWithField">  If true then <see cref="GetDocsWithField(AtomicReader, string)"/> will
        ///        also be computed and stored in the <see cref="IFieldCache"/>. </param>
        /// <returns> The values in the given field for each document. </returns>
        /// <exception cref="IOException">  If any error occurs. </exception>
        [Obsolete("(4.4) Index as a numeric field using Int32Field and then use GetInt32s(AtomicReader, string, bool) instead.")]
#pragma warning disable 612, 618
        public virtual FieldCache.Int16s GetInt16s(AtomicReader reader, string field, FieldCache.IInt16Parser parser, bool setDocsWithField)
#pragma warning restore 612, 618
        {
            NumericDocValues valuesIn = reader.GetNumericDocValues(field);
            if (valuesIn != null)
            {
                // Not cached here by FieldCacheImpl (cached instead
                // per-thread by SegmentReader):
                return new FieldCache_Int16sAnonymousClass(valuesIn);
            }
            else
            {
                FieldInfo info = reader.FieldInfos.FieldInfo(field);
                if (info is null)
                {
                    return FieldCache.Int16s.EMPTY;
                }
                else if (info.HasDocValues)
                {
                    throw IllegalStateException.Create("Type mismatch: " + field + " was indexed as " + info.DocValuesType);
                }
                else if (!info.IsIndexed)
                {
                    return FieldCache.Int16s.EMPTY;
                }
                // LUCENENET specific - eliminated unnecessary Dictionary lookup by declaring each cache as a member variable
                return caches_typeof_short.Get(reader, new CacheKey<FieldCache.IInt16Parser>(field, parser), setDocsWithField);
            }
        }

        private sealed class FieldCache_Int16sAnonymousClass : FieldCache.Int16s
        {
            private readonly NumericDocValues valuesIn;

            public FieldCache_Int16sAnonymousClass(NumericDocValues valuesIn)
            {
                this.valuesIn = valuesIn;
            }

            public override short Get(int docID)
            {
                return (short)valuesIn.Get(docID);
            }
        }

        /// <summary>
        /// NOTE: This was ShortsFromArray in Lucene
        /// </summary>
        internal class Int16sFromArray : FieldCache.Int16s
        {
            private readonly short[] values;

            public Int16sFromArray(short[] values)
            {
                this.values = values;
            }

            public override short Get(int docID)
            {
                return values[docID];
            }
        }

        /// <summary>
        /// NOTE: This was ShortCache in Lucene
        /// </summary>
#pragma warning disable CS0612 // Type or member is obsolete
        internal sealed class Int16Cache : Cache<CacheKey<FieldCache.IInt16Parser>, FieldCache.Int16s>
#pragma warning restore CS0612 // Type or member is obsolete
        {
            internal Int16Cache(FieldCacheImpl wrapper)
                : base(wrapper)
            {
            }

#pragma warning disable CS0612 // Type or member is obsolete
            protected override FieldCache.Int16s CreateValue(AtomicReader reader, CacheKey<FieldCache.IInt16Parser> key, bool setDocsWithField)
#pragma warning restore CS0612 // Type or member is obsolete
            {
                int maxDoc = reader.MaxDoc;
                short[] values;
#pragma warning disable 612, 618
                FieldCache.IInt16Parser parser = key.custom;
                if (parser is null)
                {
                    // Confusing: must delegate to wrapper (vs simply
                    // setting parser = DEFAULT_INT16_PARSER) so cache
                    // key includes DEFAULT_INT16_PARSER:
                    return wrapper.GetInt16s(reader, key.field, FieldCache.DEFAULT_INT16_PARSER, setDocsWithField);
                }
#pragma warning restore 612, 618

                values = new short[maxDoc];
                Uninvert u = new UninvertAnonymousClass(values, parser);

                u.DoUninvert(reader, key.field, setDocsWithField);

                if (setDocsWithField)
                {
                    wrapper.SetDocsWithField(reader, key.field, u.docsWithField);
                }
                return new Int16sFromArray(values);
            }

            private sealed class UninvertAnonymousClass : Uninvert
            {
                private readonly short[] values;
#pragma warning disable 612, 618
                private readonly FieldCache.IInt16Parser parser;

                public UninvertAnonymousClass(short[] values, FieldCache.IInt16Parser parser)
#pragma warning restore 612, 618
                {
                    this.values = values;
                    this.parser = parser;
                }

                private short currentValue;

                protected override void VisitTerm(BytesRef term)
                {
                    currentValue = parser.ParseInt16(term);
                }

                protected override void VisitDoc(int docID)
                {
                    values[docID] = currentValue;
                }

                protected override TermsEnum GetTermsEnum(Terms terms)
                {
                    return parser.GetTermsEnum(terms);
                }
            }
        }

        /// <summary>
        /// Returns an <see cref="FieldCache.Int32s"/> over the values found in documents in the given
        /// field.
        /// <para/>
        /// NOTE: this was getInts() in Lucene
        /// </summary>
        /// <seealso cref="GetInt32s(AtomicReader, string, FieldCache.IInt32Parser, bool)"/>
        public virtual FieldCache.Int32s GetInt32s(AtomicReader reader, string field, bool setDocsWithField)
        {
            return GetInt32s(reader, field, null, setDocsWithField);
        }

        /// <summary>
        /// Returns an <see cref="FieldCache.Int32s"/> over the values found in documents in the given
        /// field. If the field was indexed as <see cref="Documents.NumericDocValuesField"/>, it simply
        /// uses <see cref="AtomicReader.GetNumericDocValues(string)"/> to read the values.
        /// Otherwise, it checks the internal cache for an appropriate entry, and if
        /// none is found, reads the terms in <paramref name="field"/> as <see cref="int"/>s and returns
        /// an array of size <c>reader.MaxDoc</c> of the value each document
        /// has in the given field.
        /// <para/>
        /// NOTE: this was getInts() in Lucene
        /// </summary>
        /// <param name="reader">
        ///          Used to get field values. </param>
        /// <param name="field">
        ///          Which field contains the <see cref="int"/>s. </param>
        /// <param name="parser">
        ///          Computes <see cref="int"/> for string values. May be <c>null</c> if the
        ///          requested field was indexed as <see cref="Documents.NumericDocValuesField"/> or
        ///          <see cref="Documents.Int32Field"/>. </param>
        /// <param name="setDocsWithField">
        ///          If true then <see cref="GetDocsWithField(AtomicReader, string)"/> will also be computed and
        ///          stored in the <see cref="IFieldCache"/>. </param>
        /// <returns> The values in the given field for each document. </returns>
        /// <exception cref="IOException">
        ///           If any error occurs. </exception>
        public virtual FieldCache.Int32s GetInt32s(AtomicReader reader, string field, FieldCache.IInt32Parser parser, bool setDocsWithField)
        {
            NumericDocValues valuesIn = reader.GetNumericDocValues(field);
            if (valuesIn != null)
            {
                // Not cached here by FieldCacheImpl (cached instead
                // per-thread by SegmentReader):
                return new FieldCache_Int32sAnonymousClass(valuesIn);
            }
            else
            {
                FieldInfo info = reader.FieldInfos.FieldInfo(field);
                if (info is null)
                {
                    return FieldCache.Int32s.EMPTY;
                }
                else if (info.HasDocValues)
                {
                    throw IllegalStateException.Create("Type mismatch: " + field + " was indexed as " + info.DocValuesType);
                }
                else if (!info.IsIndexed)
                {
                    return FieldCache.Int32s.EMPTY;
                }
                // LUCENENET specific - eliminated unnecessary Dictionary lookup by declaring each cache as a member variable
                return caches_typeof_int.Get(reader, new CacheKey<FieldCache.IInt32Parser>(field, parser), setDocsWithField);
            }
        }

        private sealed class FieldCache_Int32sAnonymousClass : FieldCache.Int32s
        {
            private readonly NumericDocValues valuesIn;

            public FieldCache_Int32sAnonymousClass(NumericDocValues valuesIn)
            {
                this.valuesIn = valuesIn;
            }

            public override int Get(int docID)
            {
                return (int)valuesIn.Get(docID);
            }
        }

        /// <summary>
        /// NOTE: This was IntsFromArray in Lucene
        /// </summary>
        internal class Int32sFromArray : FieldCache.Int32s
        {
            private readonly PackedInt32s.Reader values;
            private readonly int minValue;

            public Int32sFromArray(PackedInt32s.Reader values, int minValue)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(values.BitsPerValue <= 32);
                this.values = values;
                this.minValue = minValue;
            }

            public override int Get(int docID)
            {
                long delta = values.Get(docID);
                return minValue + (int)delta;
            }
        }

        private class HoldsOneThing<T>
        {
            private T it;

            public virtual void Set(T it)
            {
                this.it = it;
            }

            public virtual T Get()
            {
                return it;
            }
        }

        private class GrowableWriterAndMinValue
        {
            internal GrowableWriterAndMinValue(GrowableWriter array, long minValue)
            {
                this.Writer = array;
                this.MinValue = minValue;
            }

            public GrowableWriter Writer { get; set; } // LUCENENET NOTE: for some reason, this was not marked readonly
            public long MinValue { get; set; } // LUCENENET NOTE: for some reason, this was not marked readonly
        }

        /// <summary>
        /// NOTE: This was IntCache in Lucene
        /// </summary>
        internal sealed class Int32Cache : Cache<CacheKey<FieldCache.IInt32Parser>, FieldCache.Int32s>
        {
            internal Int32Cache(FieldCacheImpl wrapper)
                : base(wrapper)
            {
            }

            protected override FieldCache.Int32s CreateValue(AtomicReader reader, CacheKey<FieldCache.IInt32Parser> key, bool setDocsWithField)
            {
                FieldCache.IInt32Parser parser = key.custom;
                if (parser is null)
                {
                    // Confusing: must delegate to wrapper (vs simply
                    // setting parser =
                    // DEFAULT_INT32_PARSER/NUMERIC_UTILS_INT32_PARSER) so
                    // cache key includes
                    // DEFAULT_INT32_PARSER/NUMERIC_UTILS_INT32_PARSER:
                    try
                    {
#pragma warning disable 612, 618
                        return wrapper.GetInt32s(reader, key.field, FieldCache.DEFAULT_INT32_PARSER, setDocsWithField);
#pragma warning restore 612, 618
                    }
                    catch (Exception ne) when (ne.IsNumberFormatException())
                    {
                        return wrapper.GetInt32s(reader, key.field, FieldCache.NUMERIC_UTILS_INT32_PARSER, setDocsWithField);
                    }
                }

                HoldsOneThing<GrowableWriterAndMinValue> valuesRef = new HoldsOneThing<GrowableWriterAndMinValue>();

                Uninvert u = new UninvertAnonymousClass(reader, parser, valuesRef);

                u.DoUninvert(reader, key.field, setDocsWithField);

                if (setDocsWithField)
                {
                    wrapper.SetDocsWithField(reader, key.field, u.docsWithField);
                }
                GrowableWriterAndMinValue values = valuesRef.Get();
                if (values is null)
                {
                    return new Int32sFromArray(new PackedInt32s.NullReader(reader.MaxDoc), 0);
                }
                return new Int32sFromArray(values.Writer.Mutable, (int)values.MinValue);
            }

            private sealed class UninvertAnonymousClass : Uninvert
            {
                private readonly AtomicReader reader;
                private readonly FieldCache.IInt32Parser parser;
                private readonly FieldCacheImpl.HoldsOneThing<GrowableWriterAndMinValue> valuesRef;

                public UninvertAnonymousClass(AtomicReader reader, FieldCache.IInt32Parser parser, FieldCacheImpl.HoldsOneThing<GrowableWriterAndMinValue> valuesRef)
                {
                    this.reader = reader;
                    this.parser = parser;
                    this.valuesRef = valuesRef;
                }

                private int minValue;
                private int currentValue;
                private GrowableWriter values;

                protected override void VisitTerm(BytesRef term)
                {
                    currentValue = parser.ParseInt32(term);
                    if (values is null)
                    {
                        // Lazy alloc so for the numeric field case
                        // (which will hit a FormatException
                        // when we first try the DEFAULT_INT32_PARSER),
                        // we don't double-alloc:
                        int startBitsPerValue;
                        // Make sure than missing values (0) can be stored without resizing
                        if (currentValue < 0)
                        {
                            minValue = currentValue;
                            startBitsPerValue = PackedInt32s.BitsRequired((-minValue) & 0xFFFFFFFFL);
                        }
                        else
                        {
                            minValue = 0;
                            startBitsPerValue = PackedInt32s.BitsRequired(currentValue);
                        }
                        values = new GrowableWriter(startBitsPerValue, reader.MaxDoc, PackedInt32s.FAST);
                        if (minValue != 0)
                        {
                            values.Fill(0, values.Count, (-minValue) & 0xFFFFFFFFL); // default value must be 0
                        }
                        valuesRef.Set(new GrowableWriterAndMinValue(values, minValue));
                    }
                }

                protected override void VisitDoc(int docID)
                {
                    values.Set(docID, (currentValue - minValue) & 0xFFFFFFFFL);
                }

                protected override TermsEnum GetTermsEnum(Terms terms)
                {
                    return parser.GetTermsEnum(terms);
                }
            }
        }

        public virtual IBits GetDocsWithField(AtomicReader reader, string field)
        {
            FieldInfo fieldInfo = reader.FieldInfos.FieldInfo(field);
            if (fieldInfo is null)
            {
                // field does not exist or has no value
                return new Lucene.Net.Util.Bits.MatchNoBits(reader.MaxDoc);
            }
            else if (fieldInfo.HasDocValues)
            {
                return reader.GetDocsWithField(field);
            }
            else if (!fieldInfo.IsIndexed)
            {
                return new Lucene.Net.Util.Bits.MatchNoBits(reader.MaxDoc);
            }
            // LUCENENET specific - eliminated unnecessary Dictionary lookup by declaring each cache as a member variable
            return caches_typeof_DocsWithFieldCache.Get(reader, new CacheKey(field), false);
        }

        internal sealed class DocsWithFieldCache : Cache<CacheKey, IBits>
        {
            internal DocsWithFieldCache(FieldCacheImpl wrapper)
                : base(wrapper)
            {
            }

            protected override IBits CreateValue(AtomicReader reader, CacheKey key, bool setDocsWithField) // ignored
            {
                string field = key.field;
                int maxDoc = reader.MaxDoc;

                // Visit all docs that have terms for this field
                FixedBitSet res = null;
                Terms terms = reader.GetTerms(field);
                if (terms != null)
                {
                    int termsDocCount = terms.DocCount;
                    if (Debugging.AssertsEnabled) Debugging.Assert(termsDocCount <= maxDoc);
                    if (termsDocCount == maxDoc)
                    {
                        // Fast case: all docs have this field:
                        return new Lucene.Net.Util.Bits.MatchAllBits(maxDoc);
                    }
                    TermsEnum termsEnum = terms.GetEnumerator();
                    DocsEnum docs = null;
                    while (termsEnum.MoveNext())
                    {
                        if (res is null)
                        {
                            // lazy init
                            res = new FixedBitSet(maxDoc);
                        }

                        docs = termsEnum.Docs(null, docs, DocsFlags.NONE);
                        // TODO: use bulk API
                        while (true)
                        {
                            int docID = docs.NextDoc();
                            if (docID == DocIdSetIterator.NO_MORE_DOCS)
                            {
                                break;
                            }
                            res.Set(docID);
                        }
                    }
                }
                if (res is null)
                {
                    return new Lucene.Net.Util.Bits.MatchNoBits(maxDoc);
                }
                int numSet = res.Cardinality;
                if (numSet >= maxDoc)
                {
                    // The cardinality of the BitSet is maxDoc if all documents have a value.
                    if (Debugging.AssertsEnabled) Debugging.Assert(numSet == maxDoc);
                    return new Lucene.Net.Util.Bits.MatchAllBits(maxDoc);
                }
                return res;
            }
        }

        /// <summary>
        /// NOTE: this was getFloats() in Lucene
        /// </summary>
        public virtual FieldCache.Singles GetSingles(AtomicReader reader, string field, bool setDocsWithField)
        {
            return GetSingles(reader, field, null, setDocsWithField);
        }

        /// <summary>
        /// NOTE: this was getFloats() in Lucene
        /// </summary>
        public virtual FieldCache.Singles GetSingles(AtomicReader reader, string field, FieldCache.ISingleParser parser, bool setDocsWithField)
        {
            NumericDocValues valuesIn = reader.GetNumericDocValues(field);
            if (valuesIn != null)
            {
                // Not cached here by FieldCacheImpl (cached instead
                // per-thread by SegmentReader):
                return new FieldCache_SinglesAnonymousClass(valuesIn);
            }
            else
            {
                FieldInfo info = reader.FieldInfos.FieldInfo(field);
                if (info is null)
                {
                    return FieldCache.Singles.EMPTY;
                }
                else if (info.HasDocValues)
                {
                    throw IllegalStateException.Create("Type mismatch: " + field + " was indexed as " + info.DocValuesType);
                }
                else if (!info.IsIndexed)
                {
                    return FieldCache.Singles.EMPTY;
                }
                // LUCENENET specific - eliminated unnecessary Dictionary lookup by declaring each cache as a member variable
                return caches_typeof_float.Get(reader, new CacheKey<FieldCache.ISingleParser>(field, parser), setDocsWithField);
            }
        }

        private sealed class FieldCache_SinglesAnonymousClass : FieldCache.Singles
        {
            private readonly NumericDocValues valuesIn;

            public FieldCache_SinglesAnonymousClass(NumericDocValues valuesIn)
            {
                this.valuesIn = valuesIn;
            }

            public override float Get(int docID)
            {
                return J2N.BitConversion.Int32BitsToSingle((int)valuesIn.Get(docID));
            }
        }

        /// <summary>
        /// NOTE: This was FloatsFromArray in Lucene
        /// </summary>
        internal class SinglesFromArray : FieldCache.Singles
        {
            private readonly float[] values;

            public SinglesFromArray(float[] values)
            {
                this.values = values;
            }

            public override float Get(int docID)
            {
                return values[docID];
            }
        }

        /// <summary>
        /// NOTE: This was FloatCache in Lucene
        /// </summary>
        internal sealed class SingleCache : Cache<CacheKey<FieldCache.ISingleParser>, FieldCache.Singles>
        {
            internal SingleCache(FieldCacheImpl wrapper)
                : base(wrapper)
            {
            }

            protected override FieldCache.Singles CreateValue(AtomicReader reader, CacheKey<FieldCache.ISingleParser> key, bool setDocsWithField)
            {
                FieldCache.ISingleParser parser = key.custom;
                if (parser is null)
                {
                    // Confusing: must delegate to wrapper (vs simply
                    // setting parser =
                    // DEFAULT_SINGLE_PARSER/NUMERIC_UTILS_SINGLE_PARSER) so
                    // cache key includes
                    // DEFAULT_SINGLE_PARSER/NUMERIC_UTILS_SINGLE_PARSER:
                    try
                    {
#pragma warning disable 612, 618
                        return wrapper.GetSingles(reader, key.field, FieldCache.DEFAULT_SINGLE_PARSER, setDocsWithField);
#pragma warning restore 612, 618
                    }
                    catch (Exception ne) when (ne.IsNumberFormatException())
                    {
                        return wrapper.GetSingles(reader, key.field, FieldCache.NUMERIC_UTILS_SINGLE_PARSER, setDocsWithField);
                    }
                }

                HoldsOneThing<float[]> valuesRef = new HoldsOneThing<float[]>();

                Uninvert u = new UninvertAnonymousClass(reader, parser, valuesRef);

                u.DoUninvert(reader, key.field, setDocsWithField);

                if (setDocsWithField)
                {
                    wrapper.SetDocsWithField(reader, key.field, u.docsWithField);
                }

                float[] values = valuesRef.Get();
                if (values is null)
                {
                    values = new float[reader.MaxDoc];
                }
                return new SinglesFromArray(values);
            }

            private sealed class UninvertAnonymousClass : Uninvert
            {
                private readonly AtomicReader reader;
                private readonly FieldCache.ISingleParser parser;
                private readonly FieldCacheImpl.HoldsOneThing<float[]> valuesRef;

                public UninvertAnonymousClass(AtomicReader reader, FieldCache.ISingleParser parser, FieldCacheImpl.HoldsOneThing<float[]> valuesRef)
                {
                    this.reader = reader;
                    this.parser = parser;
                    this.valuesRef = valuesRef;
                }

                private float currentValue;
                private float[] values;

                protected override void VisitTerm(BytesRef term)
                {
                    currentValue = parser.ParseSingle(term);
                    if (values is null)
                    {
                        // Lazy alloc so for the numeric field case
                        // (which will hit a FormatException
                        // when we first try the DEFAULT_INT32_PARSER),
                        // we don't double-alloc:
                        values = new float[reader.MaxDoc];
                        valuesRef.Set(values);
                    }
                }

                protected override void VisitDoc(int docID)
                {
                    values[docID] = currentValue;
                }

                protected override TermsEnum GetTermsEnum(Terms terms)
                {
                    return parser.GetTermsEnum(terms);
                }
            }
        }

        /// <summary>
        /// NOTE: this was getLongs() in Lucene
        /// </summary>
        public virtual FieldCache.Int64s GetInt64s(AtomicReader reader, string field, bool setDocsWithField)
        {
            return GetInt64s(reader, field, null, setDocsWithField);
        }

        /// <summary>
        /// NOTE: this was getLongs() in Lucene
        /// </summary>
        public virtual FieldCache.Int64s GetInt64s(AtomicReader reader, string field, FieldCache.IInt64Parser parser, bool setDocsWithField)
        {
            NumericDocValues valuesIn = reader.GetNumericDocValues(field);
            if (valuesIn != null)
            {
                // Not cached here by FieldCacheImpl (cached instead
                // per-thread by SegmentReader):
                return new FieldCache_Int64sAnonymousClass(valuesIn);
            }
            else
            {
                FieldInfo info = reader.FieldInfos.FieldInfo(field);
                if (info is null)
                {
                    return FieldCache.Int64s.EMPTY;
                }
                else if (info.HasDocValues)
                {
                    throw IllegalStateException.Create("Type mismatch: " + field + " was indexed as " + info.DocValuesType);
                }
                else if (!info.IsIndexed)
                {
                    return FieldCache.Int64s.EMPTY;
                }
                // LUCENENET specific - eliminated unnecessary Dictionary lookup by declaring each cache as a member variable
                return caches_typeof_long.Get(reader, new CacheKey<FieldCache.IInt64Parser>(field, parser), setDocsWithField);
            }
        }

        private sealed class FieldCache_Int64sAnonymousClass : FieldCache.Int64s
        {
            private readonly NumericDocValues valuesIn;

            public FieldCache_Int64sAnonymousClass(NumericDocValues valuesIn)
            {
                this.valuesIn = valuesIn;
            }

            public override long Get(int docID)
            {
                return valuesIn.Get(docID);
            }
        }

        /// <summary>
        /// NOTE: This was LongsFromArray in Lucene
        /// </summary>
        internal class Int64sFromArray : FieldCache.Int64s
        {
            private readonly PackedInt32s.Reader values;
            private readonly long minValue;

            public Int64sFromArray(PackedInt32s.Reader values, long minValue)
            {
                this.values = values;
                this.minValue = minValue;
            }

            public override long Get(int docID)
            {
                return minValue + values.Get(docID);
            }
        }

        /// <summary>
        /// NOTE: This was LongCache in Lucene
        /// </summary>
        internal sealed class Int64Cache : Cache<CacheKey<FieldCache.IInt64Parser>, FieldCache.Int64s>
        {
            internal Int64Cache(FieldCacheImpl wrapper)
                : base(wrapper)
            {
            }

            protected override FieldCache.Int64s CreateValue(AtomicReader reader, CacheKey<FieldCache.IInt64Parser> key, bool setDocsWithField)
            {
                FieldCache.IInt64Parser parser = key.custom;
                if (parser is null)
                {
                    // Confusing: must delegate to wrapper (vs simply
                    // setting parser =
                    // DEFAULT_INT64_PARSER/NUMERIC_UTILS_INT64_PARSER) so
                    // cache key includes
                    // DEFAULT_INT64_PARSER/NUMERIC_UTILS_INT64_PARSER:
                    try
                    {
#pragma warning disable 612, 618
                        return wrapper.GetInt64s(reader, key.field, FieldCache.DEFAULT_INT64_PARSER, setDocsWithField);
#pragma warning restore 612, 618
                    }
                    catch (Exception ne) when (ne.IsNumberFormatException())
                    {
                        return wrapper.GetInt64s(reader, key.field, FieldCache.NUMERIC_UTILS_INT64_PARSER, setDocsWithField);
                    }
                }

                HoldsOneThing<GrowableWriterAndMinValue> valuesRef = new HoldsOneThing<GrowableWriterAndMinValue>();

                Uninvert u = new UninvertAnonymousClass(reader, parser, valuesRef);

                u.DoUninvert(reader, key.field, setDocsWithField);

                if (setDocsWithField)
                {
                    wrapper.SetDocsWithField(reader, key.field, u.docsWithField);
                }
                GrowableWriterAndMinValue values = valuesRef.Get();
                if (values is null)
                {
                    return new Int64sFromArray(new PackedInt32s.NullReader(reader.MaxDoc), 0L);
                }
                return new Int64sFromArray(values.Writer.Mutable, values.MinValue);
            }

            private sealed class UninvertAnonymousClass : Uninvert
            {
                private readonly AtomicReader reader;
                private readonly FieldCache.IInt64Parser parser;
                private readonly FieldCacheImpl.HoldsOneThing<GrowableWriterAndMinValue> valuesRef;

                public UninvertAnonymousClass(AtomicReader reader, FieldCache.IInt64Parser parser, FieldCacheImpl.HoldsOneThing<GrowableWriterAndMinValue> valuesRef)
                {
                    this.reader = reader;
                    this.parser = parser;
                    this.valuesRef = valuesRef;
                }

                private long minValue;
                private long currentValue;
                private GrowableWriter values;

                protected override void VisitTerm(BytesRef term)
                {
                    currentValue = parser.ParseInt64(term);
                    if (values is null)
                    {
                        // Lazy alloc so for the numeric field case
                        // (which will hit a FormatException
                        // when we first try the DEFAULT_INT32_PARSER),
                        // we don't double-alloc:
                        int startBitsPerValue;
                        // Make sure than missing values (0) can be stored without resizing
                        if (currentValue < 0)
                        {
                            minValue = currentValue;
                            startBitsPerValue = minValue == long.MinValue ? 64 : PackedInt32s.BitsRequired(-minValue);
                        }
                        else
                        {
                            minValue = 0;
                            startBitsPerValue = PackedInt32s.BitsRequired(currentValue);
                        }
                        values = new GrowableWriter(startBitsPerValue, reader.MaxDoc, PackedInt32s.FAST);
                        if (minValue != 0)
                        {
                            values.Fill(0, values.Count, -minValue); // default value must be 0
                        }
                        valuesRef.Set(new GrowableWriterAndMinValue(values, minValue));
                    }
                }

                protected override void VisitDoc(int docID)
                {
                    values.Set(docID, currentValue - minValue);
                }

                protected override TermsEnum GetTermsEnum(Terms terms)
                {
                    return parser.GetTermsEnum(terms);
                }
            }
        }

        public virtual FieldCache.Doubles GetDoubles(AtomicReader reader, string field, bool setDocsWithField)
        {
            return GetDoubles(reader, field, null, setDocsWithField);
        }

        public virtual FieldCache.Doubles GetDoubles(AtomicReader reader, string field, FieldCache.IDoubleParser parser, bool setDocsWithField)
        {
            NumericDocValues valuesIn = reader.GetNumericDocValues(field);
            if (valuesIn != null)
            {
                // Not cached here by FieldCacheImpl (cached instead
                // per-thread by SegmentReader):
                return new FieldCache_DoublesAnonymousClass(valuesIn);
            }
            else
            {
                FieldInfo info = reader.FieldInfos.FieldInfo(field);
                if (info is null)
                {
                    return FieldCache.Doubles.EMPTY;
                }
                else if (info.HasDocValues)
                {
                    throw IllegalStateException.Create("Type mismatch: " + field + " was indexed as " + info.DocValuesType);
                }
                else if (!info.IsIndexed)
                {
                    return FieldCache.Doubles.EMPTY;
                }
                // LUCENENET specific - eliminated unnecessary Dictionary lookup by declaring each cache as a member variable
                return caches_typeof_double.Get(reader, new CacheKey<FieldCache.IDoubleParser>(field, parser), setDocsWithField);
            }
        }

        private sealed class FieldCache_DoublesAnonymousClass : FieldCache.Doubles
        {
            private readonly NumericDocValues valuesIn;

            public FieldCache_DoublesAnonymousClass(NumericDocValues valuesIn)
            {
                this.valuesIn = valuesIn;
            }

            public override double Get(int docID)
            {
                return J2N.BitConversion.Int64BitsToDouble(valuesIn.Get(docID));
            }
        }

        internal class DoublesFromArray : FieldCache.Doubles
        {
            private readonly double[] values;

            public DoublesFromArray(double[] values)
            {
                this.values = values;
            }

            public override double Get(int docID)
            {
                return values[docID];
            }
        }

        internal sealed class DoubleCache : Cache<CacheKey<FieldCache.IDoubleParser>, FieldCache.Doubles>
        {
            internal DoubleCache(FieldCacheImpl wrapper)
                : base(wrapper)
            {
            }

            protected override FieldCache.Doubles CreateValue(AtomicReader reader, CacheKey<FieldCache.IDoubleParser> key, bool setDocsWithField)
            {
                FieldCache.IDoubleParser parser = key.custom;
                if (parser is null)
                {
                    // Confusing: must delegate to wrapper (vs simply
                    // setting parser =
                    // DEFAULT_DOUBLE_PARSER/NUMERIC_UTILS_DOUBLE_PARSER) so
                    // cache key includes
                    // DEFAULT_DOUBLE_PARSER/NUMERIC_UTILS_DOUBLE_PARSER:
                    try
                    {
#pragma warning disable 612, 618
                        return wrapper.GetDoubles(reader, key.field, FieldCache.DEFAULT_DOUBLE_PARSER, setDocsWithField);
#pragma warning restore 612, 618
                    }
                    catch (Exception ne) when (ne.IsNumberFormatException())
                    {
                        return wrapper.GetDoubles(reader, key.field, FieldCache.NUMERIC_UTILS_DOUBLE_PARSER, setDocsWithField);
                    }
                }

                HoldsOneThing<double[]> valuesRef = new HoldsOneThing<double[]>();

                Uninvert u = new UninvertAnonymousClass(reader, parser, valuesRef);

                u.DoUninvert(reader, key.field, setDocsWithField);

                if (setDocsWithField)
                {
                    wrapper.SetDocsWithField(reader, key.field, u.docsWithField);
                }
                double[] values = valuesRef.Get();
                if (values is null)
                {
                    values = new double[reader.MaxDoc];
                }
                return new DoublesFromArray(values);
            }

            private sealed class UninvertAnonymousClass : Uninvert
            {
                private readonly AtomicReader reader;
                private readonly FieldCache.IDoubleParser parser;
                private readonly FieldCacheImpl.HoldsOneThing<double[]> valuesRef;

                public UninvertAnonymousClass(AtomicReader reader, FieldCache.IDoubleParser parser, FieldCacheImpl.HoldsOneThing<double[]> valuesRef)
                {
                    this.reader = reader;
                    this.parser = parser;
                    this.valuesRef = valuesRef;
                }

                private double currentValue;
                private double[] values;

                protected override void VisitTerm(BytesRef term)
                {
                    currentValue = parser.ParseDouble(term);
                    if (values is null)
                    {
                        // Lazy alloc so for the numeric field case
                        // (which will hit a FormatException
                        // when we first try the DEFAULT_INT32_PARSER),
                        // we don't double-alloc:
                        values = new double[reader.MaxDoc];
                        valuesRef.Set(values);
                    }
                }

                protected override void VisitDoc(int docID)
                {
                    values[docID] = currentValue;
                }

                protected override TermsEnum GetTermsEnum(Terms terms)
                {
                    return parser.GetTermsEnum(terms);
                }
            }
        }

        public class SortedDocValuesImpl : SortedDocValues
        {
            private readonly PagedBytes.Reader bytes;
            private readonly MonotonicAppendingInt64Buffer termOrdToBytesOffset;
            private readonly PackedInt32s.Reader docToTermOrd;
            private readonly int numOrd;

            public SortedDocValuesImpl(PagedBytes.Reader bytes, MonotonicAppendingInt64Buffer termOrdToBytesOffset, PackedInt32s.Reader docToTermOrd, int numOrd)
            {
                this.bytes = bytes;
                this.docToTermOrd = docToTermOrd;
                this.termOrdToBytesOffset = termOrdToBytesOffset;
                this.numOrd = numOrd;
            }

            public override int ValueCount => numOrd;

            public override int GetOrd(int docID)
            {
                // Subtract 1, matching the 1+ord we did when
                // storing, so that missing values, which are 0 in the
                // packed ints, are returned as -1 ord:
                return (int)docToTermOrd.Get(docID) - 1;
            }

            public override void LookupOrd(int ord, BytesRef ret)
            {
                if (ord < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(ord), "ord must be >=0 (got ord=" + ord + ")"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
                }
                bytes.Fill(ret, termOrdToBytesOffset.Get(ord));
            }
        }

        public virtual SortedDocValues GetTermsIndex(AtomicReader reader, string field)
        {
            return GetTermsIndex(reader, field, PackedInt32s.FAST);
        }

        public virtual SortedDocValues GetTermsIndex(AtomicReader reader, string field, float acceptableOverheadRatio)
        {
            SortedDocValues valuesIn = reader.GetSortedDocValues(field);
            if (valuesIn != null)
            {
                // Not cached here by FieldCacheImpl (cached instead
                // per-thread by SegmentReader):
                return valuesIn;
            }
            else
            {
                FieldInfo info = reader.FieldInfos.FieldInfo(field);
                if (info is null)
                {
                    return DocValues.EMPTY_SORTED;
                }
                else if (info.HasDocValues)
                {
                    // we don't try to build a sorted instance from numeric/binary doc
                    // values because dedup can be very costly
                    throw IllegalStateException.Create("Type mismatch: " + field + " was indexed as " + info.DocValuesType);
                }
                else if (!info.IsIndexed)
                {
                    return DocValues.EMPTY_SORTED;
                }
                // LUCENENET specific - eliminated unnecessary Dictionary lookup by declaring each cache as a member variable
                return caches_typeof_SortedDocValues.Get(reader, new CacheKey<FieldCache.AcceptableOverheadRatio>(field, new FieldCache.AcceptableOverheadRatio(acceptableOverheadRatio)), false);
            }
        }

        internal class SortedDocValuesCache : Cache<CacheKey<FieldCache.AcceptableOverheadRatio>, SortedDocValues>
        {
            internal SortedDocValuesCache(FieldCacheImpl wrapper)
                : base(wrapper)
            {
            }

            protected override SortedDocValues CreateValue(AtomicReader reader, CacheKey<FieldCache.AcceptableOverheadRatio> key, bool setDocsWithField) // ignored
            {
                int maxDoc = reader.MaxDoc;

                Terms terms = reader.GetTerms(key.field);

                float acceptableOverheadRatio = key.custom.Value;

                PagedBytes bytes = new PagedBytes(15);

                int startTermsBPV;

                int termCountHardLimit;
                if (maxDoc == int.MaxValue)
                {
                    termCountHardLimit = int.MaxValue;
                }
                else
                {
                    termCountHardLimit = maxDoc + 1;
                }

                // TODO: use Uninvert?
                if (terms != null)
                {
                    // Try for coarse estimate for number of bits; this
                    // should be an underestimate most of the time, which
                    // is fine -- GrowableWriter will reallocate as needed
                    long numUniqueTerms = terms.Count;
                    if (numUniqueTerms != -1L)
                    {
                        if (numUniqueTerms > termCountHardLimit)
                        {
                            // app is misusing the API (there is more than
                            // one term per doc); in this case we make best
                            // effort to load what we can (see LUCENE-2142)
                            numUniqueTerms = termCountHardLimit;
                        }

                        startTermsBPV = PackedInt32s.BitsRequired(numUniqueTerms);
                    }
                    else
                    {
                        startTermsBPV = 1;
                    }
                }
                else
                {
                    startTermsBPV = 1;
                }

                MonotonicAppendingInt64Buffer termOrdToBytesOffset = new MonotonicAppendingInt64Buffer();
                GrowableWriter docToTermOrd = new GrowableWriter(startTermsBPV, maxDoc, acceptableOverheadRatio);

                int termOrd = 0;

                // TODO: use Uninvert?

                if (terms != null)
                {
                    TermsEnum termsEnum = terms.GetEnumerator();
                    DocsEnum docs = null;

                    while (termsEnum.MoveNext())
                    {
                        if (termOrd >= termCountHardLimit)
                        {
                            break;
                        }

                        termOrdToBytesOffset.Add(bytes.CopyUsingLengthPrefix(termsEnum.Term));
                        docs = termsEnum.Docs(null, docs, DocsFlags.NONE);
                        while (true)
                        {
                            int docID = docs.NextDoc();
                            if (docID == DocIdSetIterator.NO_MORE_DOCS)
                            {
                                break;
                            }
                            // Store 1+ ord into packed bits
                            docToTermOrd.Set(docID, 1 + termOrd);
                        }
                        termOrd++;
                    }
                }
                termOrdToBytesOffset.Freeze();

                // maybe an int-only impl?
                return new SortedDocValuesImpl(bytes.Freeze(true), termOrdToBytesOffset, docToTermOrd.Mutable, termOrd);
            }
        }

        private class BinaryDocValuesImpl : BinaryDocValues
        {
            private readonly PagedBytes.Reader bytes;
            private readonly PackedInt32s.Reader docToOffset;

            public BinaryDocValuesImpl(PagedBytes.Reader bytes, PackedInt32s.Reader docToOffset)
            {
                this.bytes = bytes;
                this.docToOffset = docToOffset;
            }

            public override void Get(int docID, BytesRef ret)
            {
                int pointer = (int)docToOffset.Get(docID);
                if (pointer == 0)
                {
                    ret.Bytes = BytesRef.EMPTY_BYTES;
                    ret.Offset = 0;
                    ret.Length = 0;
                }
                else
                {
                    bytes.Fill(ret, pointer);
                }
            }
        }

        // TODO: this if DocTermsIndex was already created, we
        // should share it...
        public virtual BinaryDocValues GetTerms(AtomicReader reader, string field, bool setDocsWithField)
        {
            return GetTerms(reader, field, setDocsWithField, PackedInt32s.FAST);
        }

        public virtual BinaryDocValues GetTerms(AtomicReader reader, string field, bool setDocsWithField, float acceptableOverheadRatio)
        {
            BinaryDocValues valuesIn = reader.GetBinaryDocValues(field);
            if (valuesIn is null)
            {
                valuesIn = reader.GetSortedDocValues(field);
            }

            if (valuesIn != null)
            {
                // Not cached here by FieldCacheImpl (cached instead
                // per-thread by SegmentReader):
                return valuesIn;
            }

            FieldInfo info = reader.FieldInfos.FieldInfo(field);
            if (info is null)
            {
                return DocValues.EMPTY_BINARY;
            }
            else if (info.HasDocValues)
            {
                throw IllegalStateException.Create("Type mismatch: " + field + " was indexed as " + info.DocValuesType);
            }
            else if (!info.IsIndexed)
            {
                return DocValues.EMPTY_BINARY;
            }

            // LUCENENET specific - eliminated unnecessary Dictionary lookup by declaring each cache as a member variable
            return caches_typeof_BinaryDocValues.Get(reader, new CacheKey<FieldCache.AcceptableOverheadRatio>(field, new FieldCache.AcceptableOverheadRatio(acceptableOverheadRatio)), setDocsWithField);
        }

        internal sealed class BinaryDocValuesCache : Cache<CacheKey<FieldCache.AcceptableOverheadRatio>, BinaryDocValues>
        {
            internal BinaryDocValuesCache(FieldCacheImpl wrapper)
                : base(wrapper)
            {
            }

            protected override BinaryDocValues CreateValue(AtomicReader reader, CacheKey<FieldCache.AcceptableOverheadRatio> key, bool setDocsWithField)
            {
                // TODO: would be nice to first check if DocTermsIndex
                // was already cached for this field and then return
                // that instead, to avoid insanity

                int maxDoc = reader.MaxDoc;
                Terms terms = reader.GetTerms(key.field);

                float acceptableOverheadRatio = key.custom.Value;

                int termCountHardLimit = maxDoc;

                // Holds the actual term data, expanded.
                PagedBytes bytes = new PagedBytes(15);

                int startBPV;

                if (terms != null)
                {
                    // Try for coarse estimate for number of bits; this
                    // should be an underestimate most of the time, which
                    // is fine -- GrowableWriter will reallocate as needed
                    long numUniqueTerms = terms.Count;
                    if (numUniqueTerms != -1L)
                    {
                        if (numUniqueTerms > termCountHardLimit)
                        {
                            numUniqueTerms = termCountHardLimit;
                        }
                        startBPV = PackedInt32s.BitsRequired(numUniqueTerms * 4);
                    }
                    else
                    {
                        startBPV = 1;
                    }
                }
                else
                {
                    startBPV = 1;
                }

                GrowableWriter docToOffset = new GrowableWriter(startBPV, maxDoc, acceptableOverheadRatio);

                // pointer==0 means not set
                bytes.CopyUsingLengthPrefix(new BytesRef());

                if (terms != null)
                {
                    int termCount = 0;
                    TermsEnum termsEnum = terms.GetEnumerator();
                    DocsEnum docs = null;
                    while (true)
                    {
                        if (termCount++ == termCountHardLimit)
                        {
                            // app is misusing the API (there is more than
                            // one term per doc); in this case we make best
                            // effort to load what we can (see LUCENE-2142)
                            break;
                        }

                        if (!termsEnum.MoveNext())
                        {
                            break;
                        }
                        long pointer = bytes.CopyUsingLengthPrefix(termsEnum.Term);
                        docs = termsEnum.Docs(null, docs, DocsFlags.NONE);
                        while (true)
                        {
                            int docID = docs.NextDoc();
                            if (docID == DocIdSetIterator.NO_MORE_DOCS)
                            {
                                break;
                            }
                            docToOffset.Set(docID, pointer);
                        }
                    }
                }

                PackedInt32s.Reader offsetReader = docToOffset.Mutable;
                if (setDocsWithField)
                {
                    wrapper.SetDocsWithField(reader, key.field, new BitsAnonymousClass(maxDoc, offsetReader));
                }
                // maybe an int-only impl?
                return new BinaryDocValuesImpl(bytes.Freeze(true), offsetReader);
            }

            private sealed class BitsAnonymousClass : IBits
            {
                private readonly int maxDoc;
                private readonly PackedInt32s.Reader offsetReader;

                public BitsAnonymousClass(int maxDoc, PackedInt32s.Reader offsetReader)
                {
                    this.maxDoc = maxDoc;
                    this.offsetReader = offsetReader;
                }

                public bool Get(int index)
                {
                    return offsetReader.Get(index) != 0;
                }

                public int Length => maxDoc;
            }
        }

        // TODO: this if DocTermsIndex was already created, we
        // should share it...
        public virtual SortedSetDocValues GetDocTermOrds(AtomicReader reader, string field)
        {
            SortedSetDocValues dv = reader.GetSortedSetDocValues(field);
            if (dv != null)
            {
                return dv;
            }

            SortedDocValues sdv = reader.GetSortedDocValues(field);
            if (sdv != null)
            {
                return DocValues.Singleton(sdv);
            }

            FieldInfo info = reader.FieldInfos.FieldInfo(field);
            if (info is null)
            {
                return DocValues.EMPTY_SORTED_SET;
            }
            else if (info.HasDocValues)
            {
                throw IllegalStateException.Create("Type mismatch: " + field + " was indexed as " + info.DocValuesType);
            }
            else if (!info.IsIndexed)
            {
                return DocValues.EMPTY_SORTED_SET;
            }

            // LUCENENET specific - eliminated unnecessary Dictionary lookup by declaring each cache as a member variable
            DocTermOrds dto = caches_typeof_DocTermOrds.Get(reader, new CacheKey(field), false);
            return dto.GetIterator(reader);
        }

        internal sealed class DocTermOrdsCache : Cache<CacheKey, DocTermOrds>
        {
            internal DocTermOrdsCache(FieldCacheImpl wrapper)
                : base(wrapper)
            {
            }

            protected override DocTermOrds CreateValue(AtomicReader reader, CacheKey key, bool setDocsWithField) // ignored
            {
                return new DocTermOrds(reader, null, key.field);
            }
        }

        private volatile TextWriter infoStream;

        public virtual TextWriter InfoStream
        {
            get => infoStream;
            set =>
                // LUCENENET specific - use a SafeTextWriterWrapper to ensure that if the TextWriter
                // is disposed by the caller (using block) we don't get any exceptions if we keep using it.
                infoStream = value is null
                    ? null
                    : (value is SafeTextWriterWrapper ? value : new SafeTextWriterWrapper(value));
        }
    }
}
