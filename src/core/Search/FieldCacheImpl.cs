/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bytes = Lucene.Net.Search.FieldCache.Bytes;
using CacheEntry = Lucene.Net.Search.FieldCache.CacheEntry;
using CreationPlaceholder = Lucene.Net.Search.FieldCache.CreationPlaceholder;
using Doubles = Lucene.Net.Search.FieldCache.Doubles;
using FieldCacheSanityChecker = Lucene.Net.Util.FieldCacheSanityChecker;
using Floats = Lucene.Net.Search.FieldCache.Floats;
using IndexReader = Lucene.Net.Index.IndexReader;
using Ints = Lucene.Net.Search.FieldCache.Ints;
using Longs = Lucene.Net.Search.FieldCache.Longs;
using Shorts = Lucene.Net.Search.FieldCache.Shorts;
using Single = Lucene.Net.Support.Single;
using StringHelper = Lucene.Net.Util.StringHelper;
using Term = Lucene.Net.Index.Term;
using TermEnum = Lucene.Net.Index.TermsEnum;

namespace Lucene.Net.Search
{

    /// <summary> Expert: The default cache implementation, storing all values in memory.
    /// A WeakDictionary is used for storage.
    /// 
    /// <p/>Created: May 19, 2004 4:40:36 PM
    /// 
    /// </summary>
    /// <since>   lucene 1.4
    /// </since>
    class FieldCacheImpl : IFieldCache
    {
        private IDictionary<Type, Cache> caches;

        internal FieldCacheImpl()
        {
            Init();

            // .NET Port: we must do this here instead of inline, due to use of "this"
            purgeCore = new AnonymousPurgeCoreClosedListener(this);
            purgeReader = new AnonymousPurgeReaderClosedListener(this);
        }

        private void Init()
        {
            lock (this)
            {
                caches = new HashMap<Type, Cache>(7);
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

        // lucene.net: java version 3.0.3 with patch in rev. 912330 applied:
        // uschindler 21/02/2010 12:16:42 LUCENE-2273: Fixed bug in FieldCacheImpl.getCacheEntries() that used 
        //                     WeakHashMap incorrectly and lead to ConcurrentModificationException
        public virtual void PurgeAllCaches()
        {
            lock (this)
            {
                Init();
            }
        }

        // lucene.net: java version 3.0.3 with patch in rev. 912330 applied:
        // uschindler 21/02/2010 12:16:42 LUCENE-2273: Fixed bug in FieldCacheImpl.getCacheEntries() that used 
        //                     WeakHashMap incorrectly and lead to ConcurrentModificationException
        public void Purge(AtomicReader r)
        {
            lock (this)
            {
                foreach (Cache c in caches.Values)
                {
                    c.Purge(r);
                }
            }
        }

        // lucene.net: java version 3.0.3 with patch in rev. 912330 applied:
        // uschindler 21/02/2010 12:16:42 LUCENE-2273: Fixed bug in FieldCacheImpl.getCacheEntries() that used 
        //                     WeakHashMap incorrectly and lead to ConcurrentModificationException
        public virtual CacheEntry[] GetCacheEntries()
        {
            lock (this)
            {
                IList<CacheEntry> result = new List<CacheEntry>(17);
                foreach (var cacheEntry in caches)
                {
                    var cache = cacheEntry.Value;
                    var cacheType = cacheEntry.Key;
                    lock (cache.readerCache)
                    {
                        foreach (var readerCacheEntry in cache.readerCache)
                        {
                            var readerKey = readerCacheEntry.Key;
                            if (readerKey == null) continue;
                            var innerCache = readerCacheEntry.Value;
                            foreach (var mapEntry in innerCache)
                            {
                                CacheKey entry = mapEntry.Key;
                                result.Add(new CacheEntry(readerKey, entry.field, cacheType, entry.custom, mapEntry.Value));
                            }
                        }
                    }
                }
                return result.ToArray();
            }
        }

        // per-segment fieldcaches don't purge until the shared core closes.
        internal readonly SegmentReader.ICoreClosedListener purgeCore; // = new AnonymousPurgeCoreClosedListener(this);

        private sealed class AnonymousPurgeCoreClosedListener : SegmentReader.ICoreClosedListener
        {
            private readonly FieldCacheImpl parent;

            public AnonymousPurgeCoreClosedListener(FieldCacheImpl parent)
            {
                this.parent = parent;
            }

            public void OnClose(SegmentReader owner)
            {
                parent.Purge(owner);
            }
        }

        // composite/SlowMultiReaderWrapper fieldcaches don't purge until composite reader is closed.
        internal readonly IndexReader.IReaderClosedListener purgeReader; // = new AnonymousPurgeReaderClosedListener(this);

        private sealed class AnonymousPurgeReaderClosedListener : IndexReader.IReaderClosedListener
        {
            private readonly FieldCacheImpl parent;

            public AnonymousPurgeReaderClosedListener(FieldCacheImpl parent)
            {
                this.parent = parent;
            }

            public void OnClose(IndexReader owner)
            {
                if (!(owner is AtomicReader))
                    throw new ArgumentException("owner is not of type AtomicReader");

                parent.Purge((AtomicReader)owner);
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

        /// <summary>Expert: Internal cache. </summary>
        internal abstract class Cache
        {
            internal Cache(FieldCacheImpl wrapper)
            {
                this.wrapper = wrapper;
            }

            internal readonly FieldCacheImpl wrapper;

            internal IDictionary<object, IDictionary<CacheKey, object>> readerCache = new WeakDictionary<object, IDictionary<CacheKey, object>>();

            protected internal abstract object CreateValue(AtomicReader reader, CacheKey key, bool setDocsWithField);

            /* Remove this reader from the cache, if present. */
            public void Purge(AtomicReader r)
            {
                object readerKey = r.CoreCacheKey;
                lock (readerCache)
                {
                    readerCache.Remove(readerKey);
                }
            }

            public void Put(AtomicReader reader, CacheKey key, object value)
            {
                object readerKey = reader.CoreCacheKey;
                lock (readerCache)
                {
                    IDictionary<CacheKey, Object> innerCache = readerCache[readerKey];
                    if (innerCache == null)
                    {
                        // First time this reader is using FieldCache
                        innerCache = new HashMap<CacheKey, Object>();
                        readerCache[readerKey] = innerCache;
                        wrapper.InitReader(reader);
                    }
                    if (innerCache[key] == null)
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
                        innerCache = new HashMap<CacheKey, object>();
                        readerCache[readerKey] = innerCache;
                        wrapper.InitReader(reader);
                        value = null;
                    }
                    else
                    {
                        value = innerCache[key];
                    }
                    if (value == null)
                    {
                        value = new CreationPlaceholder();
                        innerCache[key] = value;
                    }
                }
                if (value is CreationPlaceholder)
                {
                    lock (value)
                    {
                        CreationPlaceholder progress = (CreationPlaceholder)value;
                        if (progress.value == null)
                        {
                            progress.value = CreateValue(reader, key, setDocsWithField);
                            lock (readerCache)
                            {
                                innerCache[key] = progress.value;
                            }

                            // Only check if key.custom (the parser) is
                            // non-null; else, we check twice for a single
                            // call to FieldCache.getXXX
                            if (key.custom != null && wrapper != null)
                            {
                                StreamWriter infoStream = wrapper.InfoStream;
                                if (infoStream != null)
                                {
                                    PrintNewInsanity(infoStream, progress.value);
                                }
                            }
                        }
                        return progress.value;
                    }
                }
                return value;
            }

            private void PrintNewInsanity(StreamWriter infoStream, object value)
            {
                FieldCacheSanityChecker.Insanity[] insanities = FieldCacheSanityChecker.CheckSanity(wrapper);
                for (int i = 0; i < insanities.Length; i++)
                {
                    FieldCacheSanityChecker.Insanity insanity = insanities[i];
                    CacheEntry[] entries = insanity.GetCacheEntries();
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

        /// <summary>Expert: Every composite-key in the internal cache is of this type. </summary>
        protected internal class CacheKey
        {
            internal readonly string field; // which Fieldable
            internal readonly object custom; // which custom comparator or parser

            /// <summary>Creates one of these objects for a custom comparator/parser. </summary>
            internal CacheKey(string field, object custom)
            {
                this.field = string.Intern(field);
                this.custom = custom;
            }

            /// <summary>Two of these are equal iff they reference the same field and type. </summary>
            public override bool Equals(object o)
            {
                if (o is CacheKey)
                {
                    CacheKey other = (CacheKey)o;
                    if (other.field == field)
                    {
                        if (other.custom == null)
                        {
                            if (custom == null)
                                return true;
                        }
                        else if (other.custom.Equals(custom))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            /// <summary>Composes a hashcode based on the field and type. </summary>
            public override int GetHashCode()
            {
                return field.GetHashCode() ^ (custom == null ? 0 : custom.GetHashCode());
            }
        }

        private abstract class Uninvert
        {
            public IBits docsWithField;

            public void DoUninvert(AtomicReader reader, string field, bool setDocsWithField)
            {
                // .NET Port: Renamed to DoUninvert to avoid conflict with type name.

                int maxDoc = reader.MaxDoc;
                Terms terms = reader.Terms(field);
                if (terms != null)
                {
                    if (setDocsWithField)
                    {
                        int termsDocCount = terms.DocCount;
                        //assert termsDocCount <= maxDoc;
                        if (termsDocCount == maxDoc)
                        {
                            // Fast case: all docs have this field:
                            docsWithField = new Bits.MatchAllBits(maxDoc);
                            setDocsWithField = false;
                        }
                    }

                    TermsEnum termsEnum = TermsEnum(terms);

                    DocsEnum docs = null;
                    FixedBitSet docsWithField2 = null;
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
                                if (docsWithField2 == null)
                                {
                                    // Lazy init
                                    this.docsWithField = docsWithField2 = new FixedBitSet(maxDoc);
                                }
                                docsWithField2.Set(docID);
                            }
                        }
                    }
                }
            }

            protected abstract TermsEnum TermsEnum(Terms terms);
            protected abstract void VisitTerm(BytesRef term);
            protected abstract void VisitDoc(int docID);
        }

        internal void SetDocsWithField(AtomicReader reader, string field, IBits docsWithField)
        {
            int maxDoc = reader.MaxDoc;
            IBits bits;
            if (docsWithField == null)
            {
                bits = new Bits.MatchNoBits(maxDoc);
            }
            else if (docsWithField is FixedBitSet)
            {
                int numSet = ((FixedBitSet)docsWithField).Cardinality();
                if (numSet >= maxDoc)
                {
                    // The cardinality of the BitSet is maxDoc if all documents have a value.
                    //assert numSet == maxDoc;
                    bits = new Bits.MatchAllBits(maxDoc);
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
        public virtual Bytes GetBytes(AtomicReader reader, string field, bool setDocsWithField)
        {
            return GetBytes(reader, field, null, setDocsWithField);
        }

        // inherit javadocs
        public virtual Bytes GetBytes(AtomicReader reader, string field, FieldCache.IByteParser parser, bool setDocsWithField)
        {
            NumericDocValues valuesIn = reader.GetNumericDocValues(field);
            if (valuesIn != null)
            {
                // Not cached here by FieldCacheImpl (cached instead
                // per-thread by SegmentReader):
                return new AnonymousBytesFromNumericDocValues(valuesIn);
            }
            else
            {
                FieldInfo info = reader.FieldInfos.FieldInfo(field);
                if (info == null)
                {
                    return Bytes.EMPTY;
                }
                else if (info.HasDocValues)
                {
                    throw new InvalidOperationException("Type mismatch: " + field + " was indexed as " + info.DocValuesTypeValue);
                }
                else if (!info.IsIndexed)
                {
                    return Bytes.EMPTY;
                }
                return (Bytes)caches[typeof(sbyte)].Get(reader, new CacheKey(field, parser), setDocsWithField);
            }
        }

        private sealed class AnonymousBytesFromNumericDocValues : Bytes
        {
            private readonly NumericDocValues valuesIn;

            public AnonymousBytesFromNumericDocValues(NumericDocValues valuesIn)
            {
                this.valuesIn = valuesIn;
            }

            public override sbyte Get(int docID)
            {
                return (sbyte)valuesIn.Get(docID);
            }
        }

        internal class BytesFromArray : Bytes
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

            protected internal override object CreateValue(AtomicReader reader, CacheKey key, bool setDocsWithField)
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

                Uninvert u = new AnonymousBytesCacheUninvert(values, parser);

                u.DoUninvert(reader, key.field, setDocsWithField);

                if (setDocsWithField)
                {
                    wrapper.SetDocsWithField(reader, key.field, u.docsWithField);
                }

                return new BytesFromArray(values);
            }
        }

        private sealed class AnonymousBytesCacheUninvert : Uninvert
        {
            private readonly sbyte[] values;
            private readonly FieldCache.IByteParser parser;
            private sbyte currentValue;

            public AnonymousBytesCacheUninvert(sbyte[] values, FieldCache.IByteParser parser)
            {
                this.values = values;
                this.parser = parser;
            }

            protected override void VisitTerm(BytesRef term)
            {
                currentValue = parser.ParseByte(term);
            }

            protected override void VisitDoc(int docID)
            {
                values[docID] = currentValue;
            }

            protected override TermEnum TermsEnum(Terms terms)
            {
                return parser.TermsEnum(terms);
            }
        }

        // inherit javadocs
        public virtual Shorts GetShorts(AtomicReader reader, string field, bool setDocsWithField)
        {
            return GetShorts(reader, field, null, setDocsWithField);
        }

        // inherit javadocs
        public virtual Shorts GetShorts(AtomicReader reader, string field, FieldCache.IShortParser parser, bool setDocsWithField)
        {
            NumericDocValues valuesIn = reader.GetNumericDocValues(field);
            if (valuesIn != null)
            {
                // Not cached here by FieldCacheImpl (cached instead
                // per-thread by SegmentReader):
                return new AnonymousShortsFromNumericDocValues(valuesIn);
            }
            else
            {
                FieldInfo info = reader.FieldInfos.FieldInfo(field);
                if (info == null)
                {
                    return Shorts.EMPTY;
                }
                else if (info.HasDocValues)
                {
                    throw new InvalidOperationException("Type mismatch: " + field + " was indexed as " + info.DocValuesTypeValue);
                }
                else if (!info.IsIndexed)
                {
                    return Shorts.EMPTY;
                }
                return (Shorts)caches[typeof(short)].Get(reader, new CacheKey(field, parser), setDocsWithField);
            }
        }

        private sealed class AnonymousShortsFromNumericDocValues : Shorts
        {
            private readonly NumericDocValues valuesIn;

            public AnonymousShortsFromNumericDocValues(NumericDocValues valuesIn)
            {
                this.valuesIn = valuesIn;
            }

            public override short Get(int docID)
            {
                return (short)valuesIn.Get(docID);
            }
        }

        internal class ShortsFromArray : Shorts
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

            protected internal override object CreateValue(AtomicReader reader, CacheKey key, bool setDocsWithField)
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

                Uninvert u = new AnonymousShortsCacheUninvert(values, parser);

                u.DoUninvert(reader, key.field, setDocsWithField);

                if (setDocsWithField)
                {
                    wrapper.SetDocsWithField(reader, key.field, u.docsWithField);
                }

                return new ShortsFromArray(values);
            }
        }

        private sealed class AnonymousShortsCacheUninvert : Uninvert
        {
            private readonly short[] values;
            private readonly FieldCache.IShortParser parser;
            private short currentValue;

            public AnonymousShortsCacheUninvert(short[] values, FieldCache.IShortParser parser)
            {
                this.values = values;
                this.parser = parser;
            }

            protected override void VisitTerm(BytesRef term)
            {
                currentValue = parser.ParseShort(term);
            }

            protected override void VisitDoc(int docID)
            {
                values[docID] = currentValue;
            }

            protected override TermEnum TermsEnum(Terms terms)
            {
                return parser.TermsEnum(terms);
            }
        }

        // inherit javadocs
        public virtual Ints GetInts(AtomicReader reader, string field, bool setDocsWithField)
        {
            return GetInts(reader, field, null, setDocsWithField);
        }

        // inherit javadocs
        public virtual Ints GetInts(AtomicReader reader, string field, FieldCache.IIntParser parser, bool setDocsWithField)
        {
            NumericDocValues valuesIn = reader.GetNumericDocValues(field);
            if (valuesIn != null)
            {
                // Not cached here by FieldCacheImpl (cached instead
                // per-thread by SegmentReader):
                return new AnonymousIntsFromNumericDocValues(valuesIn);
            }
            else
            {
                FieldInfo info = reader.FieldInfos.FieldInfo(field);
                if (info == null)
                {
                    return Ints.EMPTY;
                }
                else if (info.HasDocValues)
                {
                    throw new InvalidOperationException("Type mismatch: " + field + " was indexed as " + info.DocValuesTypeValue);
                }
                else if (!info.IsIndexed)
                {
                    return Ints.EMPTY;
                }
                return (Ints)caches[typeof(int)].Get(reader, new CacheKey(field, parser), setDocsWithField);
            }
        }

        private sealed class AnonymousIntsFromNumericDocValues : Ints
        {
            private readonly NumericDocValues valuesIn;

            public AnonymousIntsFromNumericDocValues(NumericDocValues valuesIn)
            {
                this.valuesIn = valuesIn;
            }

            public override int Get(int docID)
            {
                return (int)valuesIn.Get(docID);
            }
        }

        internal class IntsFromArray : Ints
        {
            private readonly int[] values;

            public IntsFromArray(int[] values)
            {
                this.values = values;
            }

            public override int Get(int docID)
            {
                return values[docID];
            }
        }

        private class HoldsOneThing<T>
        {
            private T it;

            public void Set(T it)
            {
                this.it = it;
            }

            public T Get()
            {
                return it;
            }
        }

        internal sealed class IntCache : Cache
        {
            internal IntCache(FieldCacheImpl wrapper)
                : base(wrapper)
            {
            }

            protected internal override object CreateValue(AtomicReader reader, CacheKey key, bool setDocsWithField)
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
                    catch (FormatException)
                    {
                        return wrapper.GetInts(reader, key.field, FieldCache.NUMERIC_UTILS_INT_PARSER, setDocsWithField);
                    }
                }

                HoldsOneThing<int[]> valuesRef = new HoldsOneThing<int[]>();

                Uninvert u = new AnonymousIntsCacheUninvert(valuesRef, parser, reader);

                u.DoUninvert(reader, key.field, setDocsWithField);

                if (setDocsWithField)
                {
                    wrapper.SetDocsWithField(reader, key.field, u.docsWithField);
                }

                int[] values = valuesRef.Get();
                if (values == null)
                {
                    values = new int[reader.MaxDoc];
                }
                return new IntsFromArray(values);
            }
        }

        private sealed class AnonymousIntsCacheUninvert : Uninvert
        {
            private readonly FieldCache.IIntParser parser;
            private readonly HoldsOneThing<int[]> valuesRef;
            private readonly AtomicReader reader;

            private int[] values;
            private int currentValue;

            public AnonymousIntsCacheUninvert(HoldsOneThing<int[]> valuesRef, FieldCache.IIntParser parser, AtomicReader reader)
            {
                this.valuesRef = valuesRef;
                this.parser = parser;
                this.reader = reader;
            }

            protected override void VisitTerm(BytesRef term)
            {
                currentValue = parser.ParseInt(term);
                if (values == null)
                {
                    // Lazy alloc so for the numeric field case
                    // (which will hit a NumberFormatException
                    // when we first try the DEFAULT_INT_PARSER),
                    // we don't double-alloc:
                    values = new int[reader.MaxDoc];
                    valuesRef.Set(values);
                }
            }

            protected override void VisitDoc(int docID)
            {
                values[docID] = currentValue;
            }

            protected override TermEnum TermsEnum(Terms terms)
            {
                return parser.TermsEnum(terms);
            }
        }

        public IBits GetDocsWithField(AtomicReader reader, string field)
        {
            FieldInfo fieldInfo = reader.FieldInfos.FieldInfo(field);
            if (fieldInfo == null)
            {
                // field does not exist or has no value
                return new Bits.MatchNoBits(reader.MaxDoc);
            }
            else if (fieldInfo.HasDocValues)
            {
                // doc values are dense
                return new Bits.MatchAllBits(reader.MaxDoc);
            }
            else if (!fieldInfo.IsIndexed)
            {
                return new Bits.MatchNoBits(reader.MaxDoc);
            }
            return (IBits)caches[typeof(DocsWithFieldCache)].Get(reader, new CacheKey(field, null), false);
        }

        internal sealed class DocsWithFieldCache : Cache
        {
            internal DocsWithFieldCache(FieldCacheImpl wrapper)
                : base(wrapper)
            {
            }

            protected internal override object CreateValue(AtomicReader reader, CacheKey key, bool setDocsWithField)
            {
                string field = key.field;
                int maxDoc = reader.MaxDoc;

                // Visit all docs that have terms for this field
                FixedBitSet res = null;
                Terms terms = reader.Terms(field);
                if (terms != null)
                {
                    int termsDocCount = terms.DocCount;
                    //assert termsDocCount <= maxDoc;
                    if (termsDocCount == maxDoc)
                    {
                        // Fast case: all docs have this field:
                        return new Bits.MatchAllBits(maxDoc);
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
                    return new Bits.MatchNoBits(maxDoc);
                }
                int numSet = res.Cardinality();
                if (numSet >= maxDoc)
                {
                    // The cardinality of the BitSet is maxDoc if all documents have a value.
                    //assert numSet == maxDoc;
                    return new Bits.MatchAllBits(maxDoc);
                }
                return res;
            }
        }

        // inherit javadocs
        public virtual float[] GetFloats(IndexReader reader, string field)
        {
            return GetFloats(reader, field, null);
        }

        // inherit javadocs
        public virtual float[] GetFloats(IndexReader reader, string field, FloatParser parser)
        {

            return (float[])caches[typeof(float)].Get(reader, new Entry(field, parser));
        }

        internal sealed class FloatCache : Cache
        {
            internal FloatCache(FieldCache wrapper)
                : base(wrapper)
            {
            }

            protected internal override System.Object CreateValue(IndexReader reader, Entry entryKey)
            {
                Entry entry = entryKey;
                string field = entry.field;
                FloatParser parser = (FloatParser)entry.custom;
                if (parser == null)
                {
                    try
                    {
                        return wrapper.GetFloats(reader, field, Lucene.Net.Search.FieldCache_Fields.DEFAULT_FLOAT_PARSER);
                    }
                    catch (System.FormatException)
                    {
                        return wrapper.GetFloats(reader, field, Lucene.Net.Search.FieldCache_Fields.NUMERIC_UTILS_FLOAT_PARSER);
                    }
                }
                float[] retArray = null;
                TermDocs termDocs = reader.TermDocs();
                TermsEnum termEnum = reader.Terms(new Term(field));
                try
                {
                    do
                    {
                        Term term = termEnum.Term;
                        if (term == null || (System.Object)term.Field != (System.Object)field)
                            break;
                        float termval = parser.ParseFloat(term.Text);
                        if (retArray == null)
                            // late init
                            retArray = new float[reader.MaxDoc];
                        termDocs.Seek(termEnum);
                        while (termDocs.Next())
                        {
                            retArray[termDocs.Doc] = termval;
                        }
                    }
                    while (termEnum.Next());
                }
                catch (StopFillCacheException)
                {
                }
                finally
                {
                    termDocs.Close();
                    termEnum.Close();
                }
                if (retArray == null)
                    // no values
                    retArray = new float[reader.MaxDoc];
                return retArray;
            }
        }



        public virtual long[] GetLongs(IndexReader reader, string field)
        {
            return GetLongs(reader, field, null);
        }

        // inherit javadocs
        public virtual long[] GetLongs(IndexReader reader, string field, Lucene.Net.Search.LongParser parser)
        {
            return (long[])caches[typeof(long)].Get(reader, new Entry(field, parser));
        }

        internal sealed class LongCache : Cache
        {
            internal LongCache(FieldCache wrapper)
                : base(wrapper)
            {
            }

            protected internal override System.Object CreateValue(IndexReader reader, Entry entryKey)
            {
                Entry entry = entryKey;
                string field = entry.field;
                Lucene.Net.Search.LongParser parser = (Lucene.Net.Search.LongParser)entry.custom;
                if (parser == null)
                {
                    try
                    {
                        return wrapper.GetLongs(reader, field, Lucene.Net.Search.FieldCache_Fields.DEFAULT_LONG_PARSER);
                    }
                    catch (System.FormatException)
                    {
                        return wrapper.GetLongs(reader, field, Lucene.Net.Search.FieldCache_Fields.NUMERIC_UTILS_LONG_PARSER);
                    }
                }
                long[] retArray = null;
                TermDocs termDocs = reader.TermDocs();
                TermsEnum termEnum = reader.Terms(new Term(field));
                try
                {
                    do
                    {
                        Term term = termEnum.Term;
                        if (term == null || (System.Object)term.Field != (System.Object)field)
                            break;
                        long termval = parser.ParseLong(term.Text);
                        if (retArray == null)
                            // late init
                            retArray = new long[reader.MaxDoc];
                        termDocs.Seek(termEnum);
                        while (termDocs.Next())
                        {
                            retArray[termDocs.Doc] = termval;
                        }
                    }
                    while (termEnum.Next());
                }
                catch (StopFillCacheException)
                {
                }
                finally
                {
                    termDocs.Close();
                    termEnum.Close();
                }
                if (retArray == null)
                    // no values
                    retArray = new long[reader.MaxDoc];
                return retArray;
            }
        }


        // inherit javadocs
        public virtual double[] GetDoubles(IndexReader reader, string field)
        {
            return GetDoubles(reader, field, null);
        }

        // inherit javadocs
        public virtual double[] GetDoubles(IndexReader reader, string field, Lucene.Net.Search.DoubleParser parser)
        {
            return (double[])caches[typeof(double)].Get(reader, new Entry(field, parser));
        }

        internal sealed class DoubleCache : Cache
        {
            internal DoubleCache(FieldCache wrapper)
                : base(wrapper)
            {
            }

            protected internal override System.Object CreateValue(IndexReader reader, Entry entryKey)
            {
                Entry entry = entryKey;
                string field = entry.field;
                Lucene.Net.Search.DoubleParser parser = (Lucene.Net.Search.DoubleParser)entry.custom;
                if (parser == null)
                {
                    try
                    {
                        return wrapper.GetDoubles(reader, field, Lucene.Net.Search.FieldCache_Fields.DEFAULT_DOUBLE_PARSER);
                    }
                    catch (System.FormatException)
                    {
                        return wrapper.GetDoubles(reader, field, Lucene.Net.Search.FieldCache_Fields.NUMERIC_UTILS_DOUBLE_PARSER);
                    }
                }
                double[] retArray = null;
                TermDocs termDocs = reader.TermDocs();
                TermsEnum termEnum = reader.Terms(new Term(field));
                try
                {
                    do
                    {
                        Term term = termEnum.Term;
                        if (term == null || (System.Object)term.Field != (System.Object)field)
                            break;
                        double termval = parser.ParseDouble(term.Text);
                        if (retArray == null)
                            // late init
                            retArray = new double[reader.MaxDoc];
                        termDocs.Seek(termEnum);
                        while (termDocs.Next())
                        {
                            retArray[termDocs.Doc] = termval;
                        }
                    }
                    while (termEnum.Next());
                }
                catch (StopFillCacheException)
                {
                }
                finally
                {
                    termDocs.Close();
                    termEnum.Close();
                }
                if (retArray == null)
                    // no values
                    retArray = new double[reader.MaxDoc];
                return retArray;
            }
        }


        // inherit javadocs
        public virtual string[] GetStrings(IndexReader reader, string field)
        {
            return (string[])caches[typeof(string)].Get(reader, new Entry(field, (Parser)null));
        }

        internal sealed class StringCache : Cache
        {
            internal StringCache(FieldCache wrapper)
                : base(wrapper)
            {
            }

            protected internal override System.Object CreateValue(IndexReader reader, Entry entryKey)
            {
                string field = StringHelper.Intern(entryKey.field);
                string[] retArray = new string[reader.MaxDoc];
                TermDocs termDocs = reader.TermDocs();
                TermsEnum termEnum = reader.Terms(new Term(field));
                try
                {
                    do
                    {
                        Term term = termEnum.Term;
                        if (term == null || (System.Object)term.Field != (System.Object)field)
                            break;
                        string termval = term.Text;
                        termDocs.Seek(termEnum);
                        while (termDocs.Next())
                        {
                            retArray[termDocs.Doc] = termval;
                        }
                    }
                    while (termEnum.Next());
                }
                finally
                {
                    termDocs.Close();
                    termEnum.Close();
                }
                return retArray;
            }
        }


        // inherit javadocs
        public virtual StringIndex GetStringIndex(IndexReader reader, string field)
        {
            return (StringIndex)caches[typeof(StringIndex)].Get(reader, new Entry(field, (Parser)null));
        }

        internal sealed class StringIndexCache : Cache
        {
            internal StringIndexCache(FieldCache wrapper)
                : base(wrapper)
            {
            }

            protected internal override System.Object CreateValue(IndexReader reader, Entry entryKey)
            {
                string field = StringHelper.Intern(entryKey.field);
                int[] retArray = new int[reader.MaxDoc];
                string[] mterms = new string[reader.MaxDoc + 1];
                TermDocs termDocs = reader.TermDocs();
                TermsEnum termEnum = reader.Terms(new Term(field));
                int t = 0; // current term number

                // an entry for documents that have no terms in this field
                // should a document with no terms be at top or bottom?
                // this puts them at the top - if it is changed, FieldDocSortedHitQueue
                // needs to change as well.
                mterms[t++] = null;

                try
                {
                    do
                    {
                        Term term = termEnum.Term;
                        if (term == null || term.Field != field || t >= mterms.Length) break;

                        // store term text
                        mterms[t] = term.Text;

                        termDocs.Seek(termEnum);
                        while (termDocs.Next())
                        {
                            retArray[termDocs.Doc] = t;
                        }

                        t++;
                    }
                    while (termEnum.Next());
                }
                finally
                {
                    termDocs.Close();
                    termEnum.Close();
                }

                if (t == 0)
                {
                    // if there are no terms, make the term array
                    // have a single null entry
                    mterms = new string[1];
                }
                else if (t < mterms.Length)
                {
                    // if there are less terms than documents,
                    // trim off the dead array space
                    string[] terms = new string[t];
                    Array.Copy(mterms, 0, terms, 0, t);
                    mterms = terms;
                }

                StringIndex value_Renamed = new StringIndex(retArray, mterms);
                return value_Renamed;
            }
        }

        private volatile System.IO.StreamWriter infoStream;

        public virtual StreamWriter InfoStream
        {
            get { return infoStream; }
            set { infoStream = value; }
        }
    }
}