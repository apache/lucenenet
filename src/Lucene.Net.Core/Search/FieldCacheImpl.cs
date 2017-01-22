using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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
    using IBits = Lucene.Net.Util.IBits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using DocTermOrds = Lucene.Net.Index.DocTermOrds;
    using DocValues = Lucene.Net.Index.DocValues;
    using FieldCacheSanityChecker = Lucene.Net.Util.FieldCacheSanityChecker;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using GrowableWriter = Lucene.Net.Util.Packed.GrowableWriter;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using MonotonicAppendingLongBuffer = Lucene.Net.Util.Packed.MonotonicAppendingLongBuffer;
    using NumericDocValues = Lucene.Net.Index.NumericDocValues;
    using PackedInts = Lucene.Net.Util.Packed.PackedInts;
    using PagedBytes = Lucene.Net.Util.PagedBytes;
    using SegmentReader = Lucene.Net.Index.SegmentReader;
    using SortedDocValues = Lucene.Net.Index.SortedDocValues;
    using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /// <summary>
    /// Expert: The default cache implementation, storing all values in memory.
    /// A WeakHashMap is used for storage.
    ///
    /// @since   lucene 1.4
    /// </summary>
    internal class FieldCacheImpl : IFieldCache
    {
        private IDictionary<Type, Cache> caches;

        internal FieldCacheImpl()
        {
            Init();

            //Have to do this here because no 'this' in class definition
            purgeCore = new CoreClosedListenerAnonymousInnerClassHelper(this);
            purgeReader = new ReaderClosedListenerAnonymousInnerClassHelper(this);
        }

        private void Init()
        {
            lock (this)
            {
                caches = new Dictionary<Type, Cache>(9);
                caches[typeof(sbyte)] = new ByteCache(this);
                caches[typeof(short)] = new ShortCache(this);
                caches[typeof(int)] = new IntCache(this);
                caches[typeof(float)] = new FloatCache(this);
                caches[typeof(long)] = new LongCache(this);
                caches[typeof(double)] = new DoubleCache(this);
                caches[typeof(BinaryDocValues)] = new BinaryDocValuesCache(this);
                caches[typeof(SortedDocValues)] = new SortedDocValuesCache(this);
                caches[typeof(DocTermOrds)] = new DocTermOrdsCache(this);
                caches[typeof(DocsWithFieldCache)] = new DocsWithFieldCache(this);
            }
        }

        public virtual void PurgeAllCaches()
        {
            lock (this)
            {
                Init();
            }
        }

        public virtual void PurgeByCacheKey(object coreCacheKey)
        {
            lock (this)
            {
                foreach (Cache c in caches.Values)
                {
                    c.PurgeByCacheKey(coreCacheKey);
                }
            }
        }

        public virtual FieldCache.CacheEntry[] GetCacheEntries()
        {
            lock (this)
            {
                IList<FieldCache.CacheEntry> result = new List<FieldCache.CacheEntry>(17);
                foreach (KeyValuePair<Type, Cache> cacheEntry in caches)
                {
                    Cache cache = cacheEntry.Value;
                    Type cacheType = cacheEntry.Key;
                    lock (cache.readerCache)
                    {
                        foreach (KeyValuePair<object, IDictionary<CacheKey, object>> readerCacheEntry in cache.readerCache)
                        {
                            object readerKey = readerCacheEntry.Key;
                            if (readerKey == null)
                            {
                                continue;
                            }
                            IDictionary<CacheKey, object> innerCache = readerCacheEntry.Value;
                            foreach (KeyValuePair<CacheKey, object> mapEntry in innerCache)
                            {
                                CacheKey entry = mapEntry.Key;
                                result.Add(new FieldCache.CacheEntry(readerKey, entry.field, cacheType, entry.custom, mapEntry.Value));
                            }
                        }
                    }
                }
                return result.ToArray();
            }
        }

        // per-segment fieldcaches don't purge until the shared core closes.
        internal readonly SegmentReader.ICoreClosedListener purgeCore;

        private class CoreClosedListenerAnonymousInnerClassHelper : SegmentReader.ICoreClosedListener
        {
            private FieldCacheImpl outerInstance;

            public CoreClosedListenerAnonymousInnerClassHelper(FieldCacheImpl outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public void OnClose(object ownerCoreCacheKey)
            {
                outerInstance.PurgeByCacheKey(ownerCoreCacheKey);
            }
        }

        // composite/SlowMultiReaderWrapper fieldcaches don't purge until composite reader is closed.
        internal readonly IndexReader.IReaderClosedListener purgeReader;

        private class ReaderClosedListenerAnonymousInnerClassHelper : IndexReader.IReaderClosedListener
        {
            private FieldCacheImpl outerInstance;

            public ReaderClosedListenerAnonymousInnerClassHelper(FieldCacheImpl outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public void OnClose(IndexReader owner)
            {
                Debug.Assert(owner is AtomicReader);
                outerInstance.PurgeByCacheKey(((AtomicReader)owner).CoreCacheKey);
            }
        }

        private void InitReader(AtomicReader reader)
        {
            if (reader is SegmentReader)
            {
                ((SegmentReader)reader).AddCoreClosedListener(purgeCore);
            }
            else
            {
                // we have a slow reader of some sort, try to register a purge event
                // rather than relying on gc:
                object key = reader.CoreCacheKey;
                if (key is AtomicReader)
                {
                    ((AtomicReader)key).AddReaderClosedListener(purgeReader);
                }
                else
                {
                    // last chance
                    reader.AddReaderClosedListener(purgeReader);
                }
            }
        }

        /// <summary>
        /// Expert: Internal cache. </summary>
        internal abstract class Cache
        {
            internal Cache(FieldCacheImpl wrapper)
            {
                this.wrapper = wrapper;
            }

            internal readonly FieldCacheImpl wrapper;

            internal IDictionary<object, IDictionary<CacheKey, object>> readerCache = new WeakDictionary<object, IDictionary<CacheKey, object>>();

            protected abstract object CreateValue(AtomicReader reader, CacheKey key, bool setDocsWithField);

            /// <summary>
            /// Remove this reader from the cache, if present. </summary>
            public virtual void PurgeByCacheKey(object coreCacheKey)
            {
                lock (readerCache)
                {
                    readerCache.Remove(coreCacheKey);
                }
            }

            /// <summary>
            /// Sets the key to the value for the provided reader;
            ///  if the key is already set then this doesn't change it.
            /// </summary>
            public virtual void Put(AtomicReader reader, CacheKey key, object value)
            {
                object readerKey = reader.CoreCacheKey;
                lock (readerCache)
                {
                    IDictionary<CacheKey, object> innerCache = readerCache[readerKey];
                    if (innerCache == null)
                    {
                        // First time this reader is using FieldCache
                        innerCache = new Dictionary<CacheKey, object>();
                        readerCache[readerKey] = innerCache;
                        wrapper.InitReader(reader);
                    }
                    // LUCENENET NOTE: We declare a temp variable here so we 
                    // don't overwrite value variable with the null
                    // that will result when this if block succeeds; otherwise
                    // we won't have a value to put in the cache.
                    object temp;
                    if (!innerCache.TryGetValue(key, out temp))
                    {
                        innerCache[key] = value;
                    }
                    else
                    {
                        // Another thread beat us to it; leave the current
                        // value
                    }
                }
            }

            public virtual object Get(AtomicReader reader, CacheKey key, bool setDocsWithField)
            {
                IDictionary<CacheKey, object> innerCache;
                object value;
                object readerKey = reader.CoreCacheKey;
                lock (readerCache)
                {
                    innerCache = readerCache[readerKey];
                    if (innerCache == null)
                    {
                        // First time this reader is using FieldCache
                        innerCache = new Dictionary<CacheKey, object>();
                        readerCache[readerKey] = innerCache;
                        wrapper.InitReader(reader);
                        value = null;
                    }
                    else
                    {
                        innerCache.TryGetValue(key, out value);
                    }
                    if (value == null)
                    {
                        value = new FieldCache.CreationPlaceholder();
                        innerCache[key] = value;
                    }
                }
                if (value is FieldCache.CreationPlaceholder)
                {
                    lock (value)
                    {
                        FieldCache.CreationPlaceholder progress = (FieldCache.CreationPlaceholder)value;
                        if (progress.Value == null)
                        {
                            progress.Value = CreateValue(reader, key, setDocsWithField);
                            lock (readerCache)
                            {
                                innerCache[key] = progress.Value;
                            }

                            // Only check if key.custom (the parser) is
                            // non-null; else, we check twice for a single
                            // call to FieldCache.getXXX
                            if (key.custom != null && wrapper != null)
                            {
                                TextWriter infoStream = wrapper.InfoStream;
                                if (infoStream != null)
                                {
                                    PrintNewInsanity(infoStream, progress.Value);
                                }
                            }
                        }
                        return progress.Value;
                    }
                }
                return value;
            }

            private void PrintNewInsanity(TextWriter infoStream, object value)
            {
                FieldCacheSanityChecker.Insanity[] insanities = FieldCacheSanityChecker.CheckSanity(wrapper);
                for (int i = 0; i < insanities.Length; i++)
                {
                    FieldCacheSanityChecker.Insanity insanity = insanities[i];
                    FieldCache.CacheEntry[] entries = insanity.GetCacheEntries();
                    for (int j = 0; j < entries.Length; j++)
                    {
                        if (entries[j].Value == value)
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
            internal readonly object custom; // which custom comparer or parser 

            /// <summary>
            /// Creates one of these objects for a custom comparer/parser. </summary>
            internal CacheKey(string field, object custom)
            {
                this.field = field;
                this.custom = custom;
            }

            /// <summary>
            /// Two of these are equal iff they reference the same field and type. </summary>
            public override bool Equals(object o)
            {
                if (o is CacheKey)
                {
                    CacheKey other = (CacheKey)o;
                    if (other.field.Equals(field))
                    {
                        if (other.custom == null)
                        {
                            if (custom == null)
                            {
                                return true;
                            }
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
                return field.GetHashCode() ^ (custom == null ? 0 : custom.GetHashCode());
            }
        }

        private abstract class Uninvert
        {
            internal IBits docsWithField; // LUCENENET NOTE: Changed from public to internal, since FieldCacheImpl is internal anyway

            public virtual void DoUninvert(AtomicReader reader, string field, bool setDocsWithField)
            {
                int maxDoc = reader.MaxDoc;
                Terms terms = reader.Terms(field);
                if (terms != null)
                {
                    if (setDocsWithField)
                    {
                        int termsDocCount = terms.DocCount;
                        Debug.Assert(termsDocCount <= maxDoc);
                        if (termsDocCount == maxDoc)
                        {
                            // Fast case: all docs have this field:
                            this.docsWithField = new Lucene.Net.Util.Bits.MatchAllBits(maxDoc);
                            setDocsWithField = false;
                        }
                    }

                    TermsEnum termsEnum = TermsEnum(terms);

                    DocsEnum docs = null;
                    FixedBitSet docsWithField = null;
                    while (true)
                    {
                        BytesRef term = termsEnum.Next();
                        if (term == null)
                        {
                            break;
                        }
                        VisitTerm(term);
                        docs = termsEnum.Docs(null, docs, DocsEnum.FLAG_NONE);
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
                                if (docsWithField == null)
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

            protected abstract TermsEnum TermsEnum(Terms terms);

            protected abstract void VisitTerm(BytesRef term);

            protected abstract void VisitDoc(int docID);
        }

        // null Bits means no docs matched
        internal virtual void SetDocsWithField(AtomicReader reader, string field, IBits docsWithField)
        {
            int maxDoc = reader.MaxDoc;
            IBits bits;
            if (docsWithField == null)
            {
                bits = new Lucene.Net.Util.Bits.MatchNoBits(maxDoc);
            }
            else if (docsWithField is FixedBitSet)
            {
                int numSet = ((FixedBitSet)docsWithField).Cardinality();
                if (numSet >= maxDoc)
                {
                    // The cardinality of the BitSet is maxDoc if all documents have a value.
                    Debug.Assert(numSet == maxDoc);
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
            caches[typeof(DocsWithFieldCache)].Put(reader, new CacheKey(field, null), bits);
        }

        // inherit javadocs
        public virtual FieldCache.Bytes GetBytes(AtomicReader reader, string field, bool setDocsWithField)
        {
            return GetBytes(reader, field, null, setDocsWithField);
        }

        public virtual FieldCache.Bytes GetBytes(AtomicReader reader, string field, FieldCache.IByteParser parser, bool setDocsWithField)
        {
            NumericDocValues valuesIn = reader.GetNumericDocValues(field);
            if (valuesIn != null)
            {
                // Not cached here by FieldCacheImpl (cached instead
                // per-thread by SegmentReader):
                return new FieldCache_BytesAnonymousInnerClassHelper(this, valuesIn);
            }
            else
            {
                FieldInfo info = reader.FieldInfos.FieldInfo(field);
                if (info == null)
                {
                    return FieldCache.Bytes.EMPTY;
                }
                else if (info.HasDocValues)
                {
                    throw new InvalidOperationException("Type mismatch: " + field + " was indexed as " + info.DocValuesType);
                }
                else if (!info.IsIndexed)
                {
                    return FieldCache.Bytes.EMPTY;
                }
                return (FieldCache.Bytes)caches[typeof(sbyte)].Get(reader, new CacheKey(field, parser), setDocsWithField);
            }
        }

        private class FieldCache_BytesAnonymousInnerClassHelper : FieldCache.Bytes
        {
            private readonly FieldCacheImpl outerInstance;

            private NumericDocValues valuesIn;

            public FieldCache_BytesAnonymousInnerClassHelper(FieldCacheImpl outerInstance, NumericDocValues valuesIn)
            {
                this.outerInstance = outerInstance;
                this.valuesIn = valuesIn;
            }

            public override sbyte Get(int docID)
            {
                return (sbyte)valuesIn.Get(docID);
            }
        }

        internal class BytesFromArray : FieldCache.Bytes
        {
            private readonly sbyte[] values;

            public BytesFromArray(sbyte[] values)
            {
                this.values = values;
            }

            public override sbyte Get(int docID)
            {
                return values[docID];
            }
        }

        internal sealed class ByteCache : Cache
        {
            internal ByteCache(FieldCacheImpl wrapper)
                : base(wrapper)
            {
            }

            protected override object CreateValue(AtomicReader reader, CacheKey key, bool setDocsWithField)
            {
                int maxDoc = reader.MaxDoc;
                sbyte[] values;
                FieldCache.IByteParser parser = (FieldCache.IByteParser)key.custom;
                if (parser == null)
                {
                    // Confusing: must delegate to wrapper (vs simply
                    // setting parser = DEFAULT_SHORT_PARSER) so cache
                    // key includes DEFAULT_SHORT_PARSER:
                    return wrapper.GetBytes(reader, key.field, FieldCache.DEFAULT_BYTE_PARSER, setDocsWithField);
                }

                values = new sbyte[maxDoc];

                Uninvert u = new UninvertAnonymousInnerClassHelper(values, parser);

                u.DoUninvert(reader, key.field, setDocsWithField);

                if (setDocsWithField)
                {
                    wrapper.SetDocsWithField(reader, key.field, u.docsWithField);
                }

                return new BytesFromArray(values);
            }

            private class UninvertAnonymousInnerClassHelper : Uninvert
            {
                private readonly sbyte[] values;
                private readonly FieldCache.IByteParser parser;

                public UninvertAnonymousInnerClassHelper(sbyte[] values, FieldCache.IByteParser parser)
                {
                    this.values = values;
                    this.parser = parser;
                }

                private sbyte currentValue;

                protected override void VisitTerm(BytesRef term)
                {
                    currentValue = parser.ParseByte(term);
                }

                protected override void VisitDoc(int docID)
                {
                    values[docID] = currentValue;
                }

                protected override TermsEnum TermsEnum(Terms terms)
                {
                    return parser.TermsEnum(terms);
                }
            }
        }

        // inherit javadocs
        public virtual FieldCache.Shorts GetShorts(AtomicReader reader, string field, bool setDocsWithField)
        {
            return GetShorts(reader, field, null, setDocsWithField);
        }

        // inherit javadocs
        public virtual FieldCache.Shorts GetShorts(AtomicReader reader, string field, FieldCache.IShortParser parser, bool setDocsWithField)
        {
            NumericDocValues valuesIn = reader.GetNumericDocValues(field);
            if (valuesIn != null)
            {
                // Not cached here by FieldCacheImpl (cached instead
                // per-thread by SegmentReader):
                return new FieldCache_ShortsAnonymousInnerClassHelper(this, valuesIn);
            }
            else
            {
                FieldInfo info = reader.FieldInfos.FieldInfo(field);
                if (info == null)
                {
                    return FieldCache.Shorts.EMPTY;
                }
                else if (info.HasDocValues)
                {
                    throw new InvalidOperationException("Type mismatch: " + field + " was indexed as " + info.DocValuesType);
                }
                else if (!info.IsIndexed)
                {
                    return FieldCache.Shorts.EMPTY;
                }
                return (FieldCache.Shorts)caches[typeof(short)].Get(reader, new CacheKey(field, parser), setDocsWithField);
            }
        }

        private class FieldCache_ShortsAnonymousInnerClassHelper : FieldCache.Shorts
        {
            private readonly FieldCacheImpl outerInstance;

            private NumericDocValues valuesIn;

            public FieldCache_ShortsAnonymousInnerClassHelper(FieldCacheImpl outerInstance, NumericDocValues valuesIn)
            {
                this.outerInstance = outerInstance;
                this.valuesIn = valuesIn;
            }

            public override short Get(int docID)
            {
                return (short)valuesIn.Get(docID);
            }
        }

        internal class ShortsFromArray : FieldCache.Shorts
        {
            private readonly short[] values;

            public ShortsFromArray(short[] values)
            {
                this.values = values;
            }

            public override short Get(int docID)
            {
                return values[docID];
            }
        }

        internal sealed class ShortCache : Cache
        {
            internal ShortCache(FieldCacheImpl wrapper)
                : base(wrapper)
            {
            }

            protected override object CreateValue(AtomicReader reader, CacheKey key, bool setDocsWithField)
            {
                int maxDoc = reader.MaxDoc;
                short[] values;
                FieldCache.IShortParser parser = (FieldCache.IShortParser)key.custom;
                if (parser == null)
                {
                    // Confusing: must delegate to wrapper (vs simply
                    // setting parser = DEFAULT_SHORT_PARSER) so cache
                    // key includes DEFAULT_SHORT_PARSER:
                    return wrapper.GetShorts(reader, key.field, FieldCache.DEFAULT_SHORT_PARSER, setDocsWithField);
                }

                values = new short[maxDoc];
                Uninvert u = new UninvertAnonymousInnerClassHelper(this, values, parser);

                u.DoUninvert(reader, key.field, setDocsWithField);

                if (setDocsWithField)
                {
                    wrapper.SetDocsWithField(reader, key.field, u.docsWithField);
                }
                return new ShortsFromArray(values);
            }

            private class UninvertAnonymousInnerClassHelper : Uninvert
            {
                private readonly ShortCache outerInstance;

                private short[] values;
                private FieldCache.IShortParser parser;

                public UninvertAnonymousInnerClassHelper(ShortCache outerInstance, short[] values, FieldCache.IShortParser parser)
                {
                    this.outerInstance = outerInstance;
                    this.values = values;
                    this.parser = parser;
                }

                private short currentValue;

                protected override void VisitTerm(BytesRef term)
                {
                    currentValue = parser.ParseShort(term);
                }

                protected override void VisitDoc(int docID)
                {
                    values[docID] = currentValue;
                }

                protected override TermsEnum TermsEnum(Terms terms)
                {
                    return parser.TermsEnum(terms);
                }
            }
        }

        // inherit javadocs
        public virtual FieldCache.Ints GetInts(AtomicReader reader, string field, bool setDocsWithField)
        {
            return GetInts(reader, field, null, setDocsWithField);
        }

        public virtual FieldCache.Ints GetInts(AtomicReader reader, string field, FieldCache.IIntParser parser, bool setDocsWithField)
        {
            NumericDocValues valuesIn = reader.GetNumericDocValues(field);
            if (valuesIn != null)
            {
                // Not cached here by FieldCacheImpl (cached instead
                // per-thread by SegmentReader):
                return new FieldCache_IntsAnonymousInnerClassHelper(this, valuesIn);
            }
            else
            {
                FieldInfo info = reader.FieldInfos.FieldInfo(field);
                if (info == null)
                {
                    return FieldCache.Ints.EMPTY;
                }
                else if (info.HasDocValues)
                {
                    throw new InvalidOperationException("Type mismatch: " + field + " was indexed as " + info.DocValuesType);
                }
                else if (!info.IsIndexed)
                {
                    return FieldCache.Ints.EMPTY;
                }
                return (FieldCache.Ints)caches[typeof(int)].Get(reader, new CacheKey(field, parser), setDocsWithField);
            }
        }

        private class FieldCache_IntsAnonymousInnerClassHelper : FieldCache.Ints
        {
            private readonly FieldCacheImpl outerInstance;

            private NumericDocValues valuesIn;

            public FieldCache_IntsAnonymousInnerClassHelper(FieldCacheImpl outerInstance, NumericDocValues valuesIn)
            {
                this.outerInstance = outerInstance;
                this.valuesIn = valuesIn;
            }

            public override int Get(int docID)
            {
                return (int)valuesIn.Get(docID);
            }
        }

        internal class IntsFromArray : FieldCache.Ints
        {
            private readonly PackedInts.Reader values;
            private readonly int minValue;

            public IntsFromArray(PackedInts.Reader values, int minValue)
            {
                Debug.Assert(values.BitsPerValue <= 32);
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

        internal sealed class IntCache : Cache
        {
            internal IntCache(FieldCacheImpl wrapper)
                : base(wrapper)
            {
            }

            protected override object CreateValue(AtomicReader reader, CacheKey key, bool setDocsWithField)
            {
                FieldCache.IIntParser parser = (FieldCache.IIntParser)key.custom;
                if (parser == null)
                {
                    // Confusing: must delegate to wrapper (vs simply
                    // setting parser =
                    // DEFAULT_INT_PARSER/NUMERIC_UTILS_INT_PARSER) so
                    // cache key includes
                    // DEFAULT_INT_PARSER/NUMERIC_UTILS_INT_PARSER:
                    try
                    {
                        return wrapper.GetInts(reader, key.field, FieldCache.DEFAULT_INT_PARSER, setDocsWithField);
                    }
                    catch (System.FormatException)
                    {
                        return wrapper.GetInts(reader, key.field, FieldCache.NUMERIC_UTILS_INT_PARSER, setDocsWithField);
                    }
                }

                HoldsOneThing<GrowableWriterAndMinValue> valuesRef = new HoldsOneThing<GrowableWriterAndMinValue>();

                Uninvert u = new UninvertAnonymousInnerClassHelper(this, reader, parser, valuesRef);

                u.DoUninvert(reader, key.field, setDocsWithField);

                if (setDocsWithField)
                {
                    wrapper.SetDocsWithField(reader, key.field, u.docsWithField);
                }
                GrowableWriterAndMinValue values = valuesRef.Get();
                if (values == null)
                {
                    return new IntsFromArray(new PackedInts.NullReader(reader.MaxDoc), 0);
                }
                return new IntsFromArray(values.Writer.Mutable, (int)values.MinValue);
            }

            private class UninvertAnonymousInnerClassHelper : Uninvert
            {
                private readonly IntCache outerInstance;

                private AtomicReader reader;
                private FieldCache.IIntParser parser;
                private FieldCacheImpl.HoldsOneThing<GrowableWriterAndMinValue> valuesRef;

                public UninvertAnonymousInnerClassHelper(IntCache outerInstance, AtomicReader reader, FieldCache.IIntParser parser, FieldCacheImpl.HoldsOneThing<GrowableWriterAndMinValue> valuesRef)
                {
                    this.outerInstance = outerInstance;
                    this.reader = reader;
                    this.parser = parser;
                    this.valuesRef = valuesRef;
                }

                private int minValue;
                private int currentValue;
                private GrowableWriter values;

                protected override void VisitTerm(BytesRef term)
                {
                    currentValue = parser.ParseInt(term);
                    if (values == null)
                    {
                        // Lazy alloc so for the numeric field case
                        // (which will hit a System.FormatException
                        // when we first try the DEFAULT_INT_PARSER),
                        // we don't double-alloc:
                        int startBitsPerValue;
                        // Make sure than missing values (0) can be stored without resizing
                        if (currentValue < 0)
                        {
                            minValue = currentValue;
                            startBitsPerValue = PackedInts.BitsRequired((-minValue) & 0xFFFFFFFFL);
                        }
                        else
                        {
                            minValue = 0;
                            startBitsPerValue = PackedInts.BitsRequired(currentValue);
                        }
                        values = new GrowableWriter(startBitsPerValue, reader.MaxDoc, PackedInts.FAST);
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

                protected override TermsEnum TermsEnum(Terms terms)
                {
                    return parser.TermsEnum(terms);
                }
            }
        }

        public virtual IBits GetDocsWithField(AtomicReader reader, string field)
        {
            FieldInfo fieldInfo = reader.FieldInfos.FieldInfo(field);
            if (fieldInfo == null)
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
            return (IBits)caches[typeof(DocsWithFieldCache)].Get(reader, new CacheKey(field, null), false);
        }

        internal sealed class DocsWithFieldCache : Cache
        {
            internal DocsWithFieldCache(FieldCacheImpl wrapper)
                : base(wrapper)
            {
            }

            protected override object CreateValue(AtomicReader reader, CacheKey key, bool setDocsWithField) // ignored
            {
                string field = key.field;
                int maxDoc = reader.MaxDoc;

                // Visit all docs that have terms for this field
                FixedBitSet res = null;
                Terms terms = reader.Terms(field);
                if (terms != null)
                {
                    int termsDocCount = terms.DocCount;
                    Debug.Assert(termsDocCount <= maxDoc);
                    if (termsDocCount == maxDoc)
                    {
                        // Fast case: all docs have this field:
                        return new Lucene.Net.Util.Bits.MatchAllBits(maxDoc);
                    }
                    TermsEnum termsEnum = terms.Iterator(null);
                    DocsEnum docs = null;
                    while (true)
                    {
                        BytesRef term = termsEnum.Next();
                        if (term == null)
                        {
                            break;
                        }
                        if (res == null)
                        {
                            // lazy init
                            res = new FixedBitSet(maxDoc);
                        }

                        docs = termsEnum.Docs(null, docs, DocsEnum.FLAG_NONE);
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
                if (res == null)
                {
                    return new Lucene.Net.Util.Bits.MatchNoBits(maxDoc);
                }
                int numSet = res.Cardinality();
                if (numSet >= maxDoc)
                {
                    // The cardinality of the BitSet is maxDoc if all documents have a value.
                    Debug.Assert(numSet == maxDoc);
                    return new Lucene.Net.Util.Bits.MatchAllBits(maxDoc);
                }
                return res;
            }
        }

        public virtual FieldCache.Floats GetFloats(AtomicReader reader, string field, bool setDocsWithField)
        {
            return GetFloats(reader, field, null, setDocsWithField);
        }

        public virtual FieldCache.Floats GetFloats(AtomicReader reader, string field, FieldCache.IFloatParser parser, bool setDocsWithField)
        {
            NumericDocValues valuesIn = reader.GetNumericDocValues(field);
            if (valuesIn != null)
            {
                // Not cached here by FieldCacheImpl (cached instead
                // per-thread by SegmentReader):
                return new FieldCache_FloatsAnonymousInnerClassHelper(this, valuesIn);
            }
            else
            {
                FieldInfo info = reader.FieldInfos.FieldInfo(field);
                if (info == null)
                {
                    return FieldCache.Floats.EMPTY;
                }
                else if (info.HasDocValues)
                {
                    throw new InvalidOperationException("Type mismatch: " + field + " was indexed as " + info.DocValuesType);
                }
                else if (!info.IsIndexed)
                {
                    return FieldCache.Floats.EMPTY;
                }
                return (FieldCache.Floats)caches[typeof(float)].Get(reader, new CacheKey(field, parser), setDocsWithField);
            }
        }

        private class FieldCache_FloatsAnonymousInnerClassHelper : FieldCache.Floats
        {
            private readonly FieldCacheImpl outerInstance;

            private NumericDocValues valuesIn;

            public FieldCache_FloatsAnonymousInnerClassHelper(FieldCacheImpl outerInstance, NumericDocValues valuesIn)
            {
                this.outerInstance = outerInstance;
                this.valuesIn = valuesIn;
            }

            public override float Get(int docID)
            {
                return Number.IntBitsToFloat((int)valuesIn.Get(docID));
            }
        }

        internal class FloatsFromArray : FieldCache.Floats
        {
            private readonly float[] values;

            public FloatsFromArray(float[] values)
            {
                this.values = values;
            }

            public override float Get(int docID)
            {
                return values[docID];
            }
        }

        internal sealed class FloatCache : Cache
        {
            internal FloatCache(FieldCacheImpl wrapper)
                : base(wrapper)
            {
            }

            protected override object CreateValue(AtomicReader reader, CacheKey key, bool setDocsWithField)
            {
                FieldCache.IFloatParser parser = (FieldCache.IFloatParser)key.custom;
                if (parser == null)
                {
                    // Confusing: must delegate to wrapper (vs simply
                    // setting parser =
                    // DEFAULT_FLOAT_PARSER/NUMERIC_UTILS_FLOAT_PARSER) so
                    // cache key includes
                    // DEFAULT_FLOAT_PARSER/NUMERIC_UTILS_FLOAT_PARSER:
                    try
                    {
                        return wrapper.GetFloats(reader, key.field, FieldCache.DEFAULT_FLOAT_PARSER, setDocsWithField);
                    }
                    catch (System.FormatException)
                    {
                        return wrapper.GetFloats(reader, key.field, FieldCache.NUMERIC_UTILS_FLOAT_PARSER, setDocsWithField);
                    }
                }

                HoldsOneThing<float[]> valuesRef = new HoldsOneThing<float[]>();

                Uninvert u = new UninvertAnonymousInnerClassHelper(this, reader, parser, valuesRef);

                u.DoUninvert(reader, key.field, setDocsWithField);

                if (setDocsWithField)
                {
                    wrapper.SetDocsWithField(reader, key.field, u.docsWithField);
                }

                float[] values = valuesRef.Get();
                if (values == null)
                {
                    values = new float[reader.MaxDoc];
                }
                return new FloatsFromArray(values);
            }

            private class UninvertAnonymousInnerClassHelper : Uninvert
            {
                private readonly FloatCache outerInstance;

                private AtomicReader reader;
                private FieldCache.IFloatParser parser;
                private FieldCacheImpl.HoldsOneThing<float[]> valuesRef;

                public UninvertAnonymousInnerClassHelper(FloatCache outerInstance, AtomicReader reader, FieldCache.IFloatParser parser, FieldCacheImpl.HoldsOneThing<float[]> valuesRef)
                {
                    this.outerInstance = outerInstance;
                    this.reader = reader;
                    this.parser = parser;
                    this.valuesRef = valuesRef;
                }

                private float currentValue;
                private float[] values;

                protected override void VisitTerm(BytesRef term)
                {
                    currentValue = parser.ParseFloat(term);
                    if (values == null)
                    {
                        // Lazy alloc so for the numeric field case
                        // (which will hit a System.FormatException
                        // when we first try the DEFAULT_INT_PARSER),
                        // we don't double-alloc:
                        values = new float[reader.MaxDoc];
                        valuesRef.Set(values);
                    }
                }

                protected override void VisitDoc(int docID)
                {
                    values[docID] = currentValue;
                }

                protected override TermsEnum TermsEnum(Terms terms)
                {
                    return parser.TermsEnum(terms);
                }
            }
        }

        public virtual FieldCache.Longs GetLongs(AtomicReader reader, string field, bool setDocsWithField)
        {
            return GetLongs(reader, field, null, setDocsWithField);
        }

        public virtual FieldCache.Longs GetLongs(AtomicReader reader, string field, FieldCache.ILongParser parser, bool setDocsWithField)
        {
            NumericDocValues valuesIn = reader.GetNumericDocValues(field);
            if (valuesIn != null)
            {
                // Not cached here by FieldCacheImpl (cached instead
                // per-thread by SegmentReader):
                return new FieldCache_LongsAnonymousInnerClassHelper(this, valuesIn);
            }
            else
            {
                FieldInfo info = reader.FieldInfos.FieldInfo(field);
                if (info == null)
                {
                    return FieldCache.Longs.EMPTY;
                }
                else if (info.HasDocValues)
                {
                    throw new InvalidOperationException("Type mismatch: " + field + " was indexed as " + info.DocValuesType);
                }
                else if (!info.IsIndexed)
                {
                    return FieldCache.Longs.EMPTY;
                }
                return (FieldCache.Longs)caches[typeof(long)].Get(reader, new CacheKey(field, parser), setDocsWithField);
            }
        }

        private class FieldCache_LongsAnonymousInnerClassHelper : FieldCache.Longs
        {
            private readonly FieldCacheImpl outerInstance;

            private NumericDocValues valuesIn;

            public FieldCache_LongsAnonymousInnerClassHelper(FieldCacheImpl outerInstance, NumericDocValues valuesIn)
            {
                this.outerInstance = outerInstance;
                this.valuesIn = valuesIn;
            }

            public override long Get(int docID)
            {
                return valuesIn.Get(docID);
            }
        }

        internal class LongsFromArray : FieldCache.Longs
        {
            private readonly PackedInts.Reader values;
            private readonly long minValue;

            public LongsFromArray(PackedInts.Reader values, long minValue)
            {
                this.values = values;
                this.minValue = minValue;
            }

            public override long Get(int docID)
            {
                return minValue + values.Get(docID);
            }
        }

        internal sealed class LongCache : Cache
        {
            internal LongCache(FieldCacheImpl wrapper)
                : base(wrapper)
            {
            }

            protected override object CreateValue(AtomicReader reader, CacheKey key, bool setDocsWithField)
            {
                FieldCache.ILongParser parser = (FieldCache.ILongParser)key.custom;
                if (parser == null)
                {
                    // Confusing: must delegate to wrapper (vs simply
                    // setting parser =
                    // DEFAULT_LONG_PARSER/NUMERIC_UTILS_LONG_PARSER) so
                    // cache key includes
                    // DEFAULT_LONG_PARSER/NUMERIC_UTILS_LONG_PARSER:
                    try
                    {
                        return wrapper.GetLongs(reader, key.field, FieldCache.DEFAULT_LONG_PARSER, setDocsWithField);
                    }
                    catch (System.FormatException)
                    {
                        return wrapper.GetLongs(reader, key.field, FieldCache.NUMERIC_UTILS_LONG_PARSER, setDocsWithField);
                    }
                }

                HoldsOneThing<GrowableWriterAndMinValue> valuesRef = new HoldsOneThing<GrowableWriterAndMinValue>();

                Uninvert u = new UninvertAnonymousInnerClassHelper(this, reader, parser, valuesRef);

                u.DoUninvert(reader, key.field, setDocsWithField);

                if (setDocsWithField)
                {
                    wrapper.SetDocsWithField(reader, key.field, u.docsWithField);
                }
                GrowableWriterAndMinValue values = valuesRef.Get();
                if (values == null)
                {
                    return new LongsFromArray(new PackedInts.NullReader(reader.MaxDoc), 0L);
                }
                return new LongsFromArray(values.Writer.Mutable, values.MinValue);
            }

            private class UninvertAnonymousInnerClassHelper : Uninvert
            {
                private readonly LongCache outerInstance;

                private AtomicReader reader;
                private FieldCache.ILongParser parser;
                private FieldCacheImpl.HoldsOneThing<GrowableWriterAndMinValue> valuesRef;

                public UninvertAnonymousInnerClassHelper(LongCache outerInstance, AtomicReader reader, FieldCache.ILongParser parser, FieldCacheImpl.HoldsOneThing<GrowableWriterAndMinValue> valuesRef)
                {
                    this.outerInstance = outerInstance;
                    this.reader = reader;
                    this.parser = parser;
                    this.valuesRef = valuesRef;
                }

                private long minValue;
                private long currentValue;
                private GrowableWriter values;

                protected override void VisitTerm(BytesRef term)
                {
                    currentValue = parser.ParseLong(term);
                    if (values == null)
                    {
                        // Lazy alloc so for the numeric field case
                        // (which will hit a System.FormatException
                        // when we first try the DEFAULT_INT_PARSER),
                        // we don't double-alloc:
                        int startBitsPerValue;
                        // Make sure than missing values (0) can be stored without resizing
                        if (currentValue < 0)
                        {
                            minValue = currentValue;
                            startBitsPerValue = minValue == long.MinValue ? 64 : PackedInts.BitsRequired(-minValue);
                        }
                        else
                        {
                            minValue = 0;
                            startBitsPerValue = PackedInts.BitsRequired(currentValue);
                        }
                        values = new GrowableWriter(startBitsPerValue, reader.MaxDoc, PackedInts.FAST);
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

                protected override TermsEnum TermsEnum(Terms terms)
                {
                    return parser.TermsEnum(terms);
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
                return new FieldCache_DoublesAnonymousInnerClassHelper(this, valuesIn);
            }
            else
            {
                FieldInfo info = reader.FieldInfos.FieldInfo(field);
                if (info == null)
                {
                    return FieldCache.Doubles.EMPTY;
                }
                else if (info.HasDocValues)
                {
                    throw new InvalidOperationException("Type mismatch: " + field + " was indexed as " + info.DocValuesType);
                }
                else if (!info.IsIndexed)
                {
                    return FieldCache.Doubles.EMPTY;
                }
                return (FieldCache.Doubles)caches[typeof(double)].Get(reader, new CacheKey(field, parser), setDocsWithField);
            }
        }

        private class FieldCache_DoublesAnonymousInnerClassHelper : FieldCache.Doubles
        {
            private readonly FieldCacheImpl outerInstance;

            private NumericDocValues valuesIn;

            public FieldCache_DoublesAnonymousInnerClassHelper(FieldCacheImpl outerInstance, NumericDocValues valuesIn)
            {
                this.outerInstance = outerInstance;
                this.valuesIn = valuesIn;
            }

            public override double Get(int docID)
            {
                return BitConverter.Int64BitsToDouble(valuesIn.Get(docID));
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

        internal sealed class DoubleCache : Cache
        {
            internal DoubleCache(FieldCacheImpl wrapper)
                : base(wrapper)
            {
            }

            protected override object CreateValue(AtomicReader reader, CacheKey key, bool setDocsWithField)
            {
                FieldCache.IDoubleParser parser = (FieldCache.IDoubleParser)key.custom;
                if (parser == null)
                {
                    // Confusing: must delegate to wrapper (vs simply
                    // setting parser =
                    // DEFAULT_DOUBLE_PARSER/NUMERIC_UTILS_DOUBLE_PARSER) so
                    // cache key includes
                    // DEFAULT_DOUBLE_PARSER/NUMERIC_UTILS_DOUBLE_PARSER:
                    try
                    {
                        return wrapper.GetDoubles(reader, key.field, FieldCache.DEFAULT_DOUBLE_PARSER, setDocsWithField);
                    }
                    catch (System.FormatException)
                    {
                        return wrapper.GetDoubles(reader, key.field, FieldCache.NUMERIC_UTILS_DOUBLE_PARSER, setDocsWithField);
                    }
                }

                HoldsOneThing<double[]> valuesRef = new HoldsOneThing<double[]>();

                Uninvert u = new UninvertAnonymousInnerClassHelper(this, reader, parser, valuesRef);

                u.DoUninvert(reader, key.field, setDocsWithField);

                if (setDocsWithField)
                {
                    wrapper.SetDocsWithField(reader, key.field, u.docsWithField);
                }
                double[] values = valuesRef.Get();
                if (values == null)
                {
                    values = new double[reader.MaxDoc];
                }
                return new DoublesFromArray(values);
            }

            private class UninvertAnonymousInnerClassHelper : Uninvert
            {
                private readonly DoubleCache outerInstance;

                private AtomicReader reader;
                private FieldCache.IDoubleParser parser;
                private FieldCacheImpl.HoldsOneThing<double[]> valuesRef;

                public UninvertAnonymousInnerClassHelper(DoubleCache outerInstance, AtomicReader reader, FieldCache.IDoubleParser parser, FieldCacheImpl.HoldsOneThing<double[]> valuesRef)
                {
                    this.outerInstance = outerInstance;
                    this.reader = reader;
                    this.parser = parser;
                    this.valuesRef = valuesRef;
                }

                private double currentValue;
                private double[] values;

                protected override void VisitTerm(BytesRef term)
                {
                    currentValue = parser.ParseDouble(term);
                    if (values == null)
                    {
                        // Lazy alloc so for the numeric field case
                        // (which will hit a System.FormatException
                        // when we first try the DEFAULT_INT_PARSER),
                        // we don't double-alloc:
                        values = new double[reader.MaxDoc];
                        valuesRef.Set(values);
                    }
                }

                protected override void VisitDoc(int docID)
                {
                    values[docID] = currentValue;
                }

                protected override TermsEnum TermsEnum(Terms terms)
                {
                    return parser.TermsEnum(terms);
                }
            }
        }

        public class SortedDocValuesImpl : SortedDocValues
        {
            private readonly PagedBytes.Reader bytes;
            private readonly MonotonicAppendingLongBuffer termOrdToBytesOffset;
            private readonly PackedInts.Reader docToTermOrd;
            private readonly int numOrd;

            public SortedDocValuesImpl(PagedBytes.Reader bytes, MonotonicAppendingLongBuffer termOrdToBytesOffset, PackedInts.Reader docToTermOrd, int numOrd)
            {
                this.bytes = bytes;
                this.docToTermOrd = docToTermOrd;
                this.termOrdToBytesOffset = termOrdToBytesOffset;
                this.numOrd = numOrd;
            }

            public override int ValueCount
            {
                get
                {
                    return numOrd;
                }
            }

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
                    throw new System.ArgumentException("ord must be >=0 (got ord=" + ord + ")");
                }
                bytes.Fill(ret, termOrdToBytesOffset.Get(ord));
            }
        }

        public virtual SortedDocValues GetTermsIndex(AtomicReader reader, string field)
        {
            return GetTermsIndex(reader, field, PackedInts.FAST);
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
                if (info == null)
                {
                    return DocValues.EMPTY_SORTED;
                }
                else if (info.HasDocValues)
                {
                    // we don't try to build a sorted instance from numeric/binary doc
                    // values because dedup can be very costly
                    throw new InvalidOperationException("Type mismatch: " + field + " was indexed as " + info.DocValuesType);
                }
                else if (!info.IsIndexed)
                {
                    return DocValues.EMPTY_SORTED;
                }
                return (SortedDocValues)caches[typeof(SortedDocValues)].Get(reader, new CacheKey(field, acceptableOverheadRatio), false);
            }
        }

        internal class SortedDocValuesCache : Cache
        {
            internal SortedDocValuesCache(FieldCacheImpl wrapper)
                : base(wrapper)
            {
            }

            protected override object CreateValue(AtomicReader reader, CacheKey key, bool setDocsWithField) // ignored
            {
                int maxDoc = reader.MaxDoc;

                Terms terms = reader.Terms(key.field);

                float acceptableOverheadRatio = (float)((float?)key.custom);

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

                        startTermsBPV = PackedInts.BitsRequired(numUniqueTerms);
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

                MonotonicAppendingLongBuffer termOrdToBytesOffset = new MonotonicAppendingLongBuffer();
                GrowableWriter docToTermOrd = new GrowableWriter(startTermsBPV, maxDoc, acceptableOverheadRatio);

                int termOrd = 0;

                // TODO: use Uninvert?

                if (terms != null)
                {
                    TermsEnum termsEnum = terms.Iterator(null);
                    DocsEnum docs = null;

                    while (true)
                    {
                        BytesRef term = termsEnum.Next();
                        if (term == null)
                        {
                            break;
                        }
                        if (termOrd >= termCountHardLimit)
                        {
                            break;
                        }

                        termOrdToBytesOffset.Add(bytes.CopyUsingLengthPrefix(term));
                        docs = termsEnum.Docs(null, docs, DocsEnum.FLAG_NONE);
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
            private readonly PackedInts.Reader docToOffset;

            public BinaryDocValuesImpl(PagedBytes.Reader bytes, PackedInts.Reader docToOffset)
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
            return GetTerms(reader, field, setDocsWithField, PackedInts.FAST);
        }

        public virtual BinaryDocValues GetTerms(AtomicReader reader, string field, bool setDocsWithField, float acceptableOverheadRatio)
        {
            BinaryDocValues valuesIn = reader.GetBinaryDocValues(field);
            if (valuesIn == null)
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
            if (info == null)
            {
                return DocValues.EMPTY_BINARY;
            }
            else if (info.HasDocValues)
            {
                throw new InvalidOperationException("Type mismatch: " + field + " was indexed as " + info.DocValuesType);
            }
            else if (!info.IsIndexed)
            {
                return DocValues.EMPTY_BINARY;
            }

            return (BinaryDocValues)caches[typeof(BinaryDocValues)].Get(reader, new CacheKey(field, acceptableOverheadRatio), setDocsWithField);
        }

        internal sealed class BinaryDocValuesCache : Cache
        {
            internal BinaryDocValuesCache(FieldCacheImpl wrapper)
                : base(wrapper)
            {
            }

            protected override object CreateValue(AtomicReader reader, CacheKey key, bool setDocsWithField)
            {
                // TODO: would be nice to first check if DocTermsIndex
                // was already cached for this field and then return
                // that instead, to avoid insanity

                int maxDoc = reader.MaxDoc;
                Terms terms = reader.Terms(key.field);

                float acceptableOverheadRatio = (float)((float?)key.custom);

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
                        startBPV = PackedInts.BitsRequired(numUniqueTerms * 4);
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
                    TermsEnum termsEnum = terms.Iterator(null);
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

                        BytesRef term = termsEnum.Next();
                        if (term == null)
                        {
                            break;
                        }
                        long pointer = bytes.CopyUsingLengthPrefix(term);
                        docs = termsEnum.Docs(null, docs, DocsEnum.FLAG_NONE);
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

                PackedInts.Reader offsetReader = docToOffset.Mutable;
                if (setDocsWithField)
                {
                    wrapper.SetDocsWithField(reader, key.field, new BitsAnonymousInnerClassHelper(this, maxDoc, offsetReader));
                }
                // maybe an int-only impl?
                return new BinaryDocValuesImpl(bytes.Freeze(true), offsetReader);
            }

            private class BitsAnonymousInnerClassHelper : IBits
            {
                private readonly BinaryDocValuesCache outerInstance;

                private int maxDoc;
                private PackedInts.Reader offsetReader;

                public BitsAnonymousInnerClassHelper(BinaryDocValuesCache outerInstance, int maxDoc, PackedInts.Reader offsetReader)
                {
                    this.outerInstance = outerInstance;
                    this.maxDoc = maxDoc;
                    this.offsetReader = offsetReader;
                }

                public virtual bool Get(int index)
                {
                    return offsetReader.Get(index) != 0;
                }

                public virtual int Length
                {
                    get { return maxDoc; }
                }
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
            if (info == null)
            {
                return DocValues.EMPTY_SORTED_SET;
            }
            else if (info.HasDocValues)
            {
                throw new InvalidOperationException("Type mismatch: " + field + " was indexed as " + info.DocValuesType);
            }
            else if (!info.IsIndexed)
            {
                return DocValues.EMPTY_SORTED_SET;
            }

            DocTermOrds dto = (DocTermOrds)caches[typeof(DocTermOrds)].Get(reader, new CacheKey(field, null), false);
            return dto.GetIterator(reader);
        }

        internal sealed class DocTermOrdsCache : Cache
        {
            internal DocTermOrdsCache(FieldCacheImpl wrapper)
                : base(wrapper)
            {
            }

            protected override object CreateValue(AtomicReader reader, CacheKey key, bool setDocsWithField) // ignored
            {
                return new DocTermOrds(reader, null, key.field);
            }
        }

        private volatile TextWriter infoStream;

        public virtual TextWriter InfoStream
        {
            set
            {
                // LUCENENET specific - use a SafeTextWriterWrapper to ensure that if the TextWriter
                // is disposed by the caller (using block) we don't get any exceptions if we keep using it.
                infoStream = value == null
                    ? null
                    : (value is SafeTextWriterWrapper ? value : new SafeTextWriterWrapper(value));
            }
            get
            {
                return infoStream;
            }
        }
    }
}