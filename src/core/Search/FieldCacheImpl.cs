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
using Lucene.Net.Util.Packed;
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
    public class FieldCacheImpl : IFieldCache
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

        public class BytesFromArray : Bytes
        {
            public readonly sbyte[] values;

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

        public class ShortsFromArray : Shorts
        {
            public readonly short[] values;

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

        public class IntsFromArray : Ints
        {
            public readonly int[] values;

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
        public virtual Floats GetFloats(AtomicReader reader, string field, bool setDocsWithField)
        {
            return GetFloats(reader, field, null, setDocsWithField);
        }

        // inherit javadocs
        public virtual Floats GetFloats(AtomicReader reader, string field, FieldCache.IFloatParser parser, bool setDocsWithField)
        {
            NumericDocValues valuesIn = reader.GetNumericDocValues(field);
            if (valuesIn != null)
            {
                // Not cached here by FieldCacheImpl (cached instead
                // per-thread by SegmentReader):
                return new AnonymousFloatsFromNumericDocValues(valuesIn);
            }
            else
            {
                FieldInfo info = reader.FieldInfos.FieldInfo(field);
                if (info == null)
                {
                    return Floats.EMPTY;
                }
                else if (info.HasDocValues)
                {
                    throw new InvalidOperationException("Type mismatch: " + field + " was indexed as " + info.DocValuesTypeValue);
                }
                else if (!info.IsIndexed)
                {
                    return Floats.EMPTY;
                }
                return (Floats)caches[typeof(float)].Get(reader, new CacheKey(field, parser), setDocsWithField);
            }
        }

        private sealed class AnonymousFloatsFromNumericDocValues : Floats
        {
            private readonly NumericDocValues valuesIn;

            public AnonymousFloatsFromNumericDocValues(NumericDocValues valuesIn)
            {
                this.valuesIn = valuesIn;
            }

            public override float Get(int docID)
            {
                return Number.IntBitsToFloat((int)valuesIn.Get(docID));
            }
        }

        public class FloatsFromArray : Floats
        {
            public readonly float[] values;

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

            protected internal override object CreateValue(AtomicReader reader, CacheKey key, bool setDocsWithField)
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
                    catch (FormatException)
                    {
                        return wrapper.GetFloats(reader, key.field, FieldCache.NUMERIC_UTILS_FLOAT_PARSER, setDocsWithField);
                    }
                }

                HoldsOneThing<float[]> valuesRef = new HoldsOneThing<float[]>();

                Uninvert u = new AnonymousFloatsCacheUninvert(valuesRef, parser, reader);

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
        }

        private sealed class AnonymousFloatsCacheUninvert : Uninvert
        {
            private readonly FieldCache.IFloatParser parser;
            private readonly HoldsOneThing<float[]> valuesRef;
            private readonly AtomicReader reader;

            private float[] values;
            private float currentValue;

            public AnonymousFloatsCacheUninvert(HoldsOneThing<float[]> valuesRef, FieldCache.IFloatParser parser, AtomicReader reader)
            {
                this.valuesRef = valuesRef;
                this.parser = parser;
                this.reader = reader;
            }

            protected override void VisitTerm(BytesRef term)
            {
                currentValue = parser.ParseFloat(term);
                if (values == null)
                {
                    // Lazy alloc so for the numeric field case
                    // (which will hit a NumberFormatException
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

            protected override TermEnum TermsEnum(Terms terms)
            {
                return parser.TermsEnum(terms);
            }
        }


        // inherit javadocs
        public virtual Longs GetLongs(AtomicReader reader, string field, bool setDocsWithField)
        {
            return GetLongs(reader, field, null, setDocsWithField);
        }

        // inherit javadocs
        public virtual Longs GetLongs(AtomicReader reader, string field, FieldCache.ILongParser parser, bool setDocsWithField)
        {
            NumericDocValues valuesIn = reader.GetNumericDocValues(field);
            if (valuesIn != null)
            {
                // Not cached here by FieldCacheImpl (cached instead
                // per-thread by SegmentReader):
                return new AnonymousLongsFromNumericDocValues(valuesIn);
            }
            else
            {
                FieldInfo info = reader.FieldInfos.FieldInfo(field);
                if (info == null)
                {
                    return Longs.EMPTY;
                }
                else if (info.HasDocValues)
                {
                    throw new InvalidOperationException("Type mismatch: " + field + " was indexed as " + info.DocValuesTypeValue);
                }
                else if (!info.IsIndexed)
                {
                    return Longs.EMPTY;
                }
                return (Longs)caches[typeof(long)].Get(reader, new CacheKey(field, parser), setDocsWithField);
            }
        }

        private sealed class AnonymousLongsFromNumericDocValues : Longs
        {
            private readonly NumericDocValues valuesIn;

            public AnonymousLongsFromNumericDocValues(NumericDocValues valuesIn)
            {
                this.valuesIn = valuesIn;
            }

            public override long Get(int docID)
            {
                return valuesIn.Get(docID);
            }
        }

        public class LongsFromArray : Longs
        {
            public readonly long[] values;

            public LongsFromArray(long[] values)
            {
                this.values = values;
            }

            public override long Get(int docID)
            {
                return values[docID];
            }
        }

        internal sealed class LongCache : Cache
        {
            internal LongCache(FieldCacheImpl wrapper)
                : base(wrapper)
            {
            }

            protected internal override object CreateValue(AtomicReader reader, CacheKey key, bool setDocsWithField)
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
                    catch (FormatException)
                    {
                        return wrapper.GetLongs(reader, key.field, FieldCache.NUMERIC_UTILS_LONG_PARSER, setDocsWithField);
                    }
                }

                HoldsOneThing<long[]> valuesRef = new HoldsOneThing<long[]>();

                Uninvert u = new AnonymousLongsCacheUninvert(valuesRef, parser, reader);

                u.DoUninvert(reader, key.field, setDocsWithField);

                if (setDocsWithField)
                {
                    wrapper.SetDocsWithField(reader, key.field, u.docsWithField);
                }

                long[] values = valuesRef.Get();
                if (values == null)
                {
                    values = new long[reader.MaxDoc];
                }
                return new LongsFromArray(values);
            }
        }

        private sealed class AnonymousLongsCacheUninvert : Uninvert
        {
            private readonly FieldCache.ILongParser parser;
            private readonly HoldsOneThing<long[]> valuesRef;
            private readonly AtomicReader reader;

            private long[] values;
            private long currentValue;

            public AnonymousLongsCacheUninvert(HoldsOneThing<long[]> valuesRef, FieldCache.ILongParser parser, AtomicReader reader)
            {
                this.valuesRef = valuesRef;
                this.parser = parser;
                this.reader = reader;
            }

            protected override void VisitTerm(BytesRef term)
            {
                currentValue = parser.ParseLong(term);
                if (values == null)
                {
                    // Lazy alloc so for the numeric field case
                    // (which will hit a NumberFormatException
                    // when we first try the DEFAULT_INT_PARSER),
                    // we don't double-alloc:
                    values = new long[reader.MaxDoc];
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


        // inherit javadocs
        public virtual Doubles GetDoubles(AtomicReader reader, string field, bool setDocsWithField)
        {
            return GetDoubles(reader, field, null, setDocsWithField);
        }

        // inherit javadocs
        public virtual Doubles GetDoubles(AtomicReader reader, string field, FieldCache.IDoubleParser parser, bool setDocsWithField)
        {
            NumericDocValues valuesIn = reader.GetNumericDocValues(field);
            if (valuesIn != null)
            {
                // Not cached here by FieldCacheImpl (cached instead
                // per-thread by SegmentReader):
                return new AnonymousDoublesFromNumericDocValues(valuesIn);
            }
            else
            {
                FieldInfo info = reader.FieldInfos.FieldInfo(field);
                if (info == null)
                {
                    return Doubles.EMPTY;
                }
                else if (info.HasDocValues)
                {
                    throw new InvalidOperationException("Type mismatch: " + field + " was indexed as " + info.DocValuesTypeValue);
                }
                else if (!info.IsIndexed)
                {
                    return Doubles.EMPTY;
                }
                return (Doubles)caches[typeof(double)].Get(reader, new CacheKey(field, parser), setDocsWithField);
            }
        }

        private sealed class AnonymousDoublesFromNumericDocValues : Doubles
        {
            private readonly NumericDocValues valuesIn;

            public AnonymousDoublesFromNumericDocValues(NumericDocValues valuesIn)
            {
                this.valuesIn = valuesIn;
            }

            public override double Get(int docID)
            {
                return BitConverter.Int64BitsToDouble(valuesIn.Get(docID));
            }
        }

        public class DoublesFromArray : Doubles
        {
            public readonly double[] values;

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

            protected internal override object CreateValue(AtomicReader reader, CacheKey key, bool setDocsWithField)
            {
                FieldCache.IDoubleParser parser = (FieldCache.IDoubleParser)key.custom;
                if (parser == null)
                {
                    // Confusing: must delegate to wrapper (vs simply
                    // setting parser =
                    // DEFAULT_FLOAT_PARSER/NUMERIC_UTILS_FLOAT_PARSER) so
                    // cache key includes
                    // DEFAULT_FLOAT_PARSER/NUMERIC_UTILS_FLOAT_PARSER:
                    try
                    {
                        return wrapper.GetDoubles(reader, key.field, FieldCache.DEFAULT_DOUBLE_PARSER, setDocsWithField);
                    }
                    catch (FormatException)
                    {
                        return wrapper.GetDoubles(reader, key.field, FieldCache.NUMERIC_UTILS_DOUBLE_PARSER, setDocsWithField);
                    }
                }

                HoldsOneThing<double[]> valuesRef = new HoldsOneThing<double[]>();

                Uninvert u = new AnonymousDoublesCacheUninvert(valuesRef, parser, reader);

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
        }

        private sealed class AnonymousDoublesCacheUninvert : Uninvert
        {
            private readonly FieldCache.IDoubleParser parser;
            private readonly HoldsOneThing<double[]> valuesRef;
            private readonly AtomicReader reader;

            private double[] values;
            private double currentValue;

            public AnonymousDoublesCacheUninvert(HoldsOneThing<double[]> valuesRef, FieldCache.IDoubleParser parser, AtomicReader reader)
            {
                this.valuesRef = valuesRef;
                this.parser = parser;
                this.reader = reader;
            }

            protected override void VisitTerm(BytesRef term)
            {
                currentValue = parser.ParseDouble(term);
                if (values == null)
                {
                    // Lazy alloc so for the numeric field case
                    // (which will hit a NumberFormatException
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

            protected override TermEnum TermsEnum(Terms terms)
            {
                return parser.TermsEnum(terms);
            }
        }

        public class SortedDocValuesImpl : SortedDocValues
        {
            private readonly PagedBytes.Reader bytes;
            private readonly PackedInts.IReader termOrdToBytesOffset;
            private readonly PackedInts.IReader docToTermOrd;
            private readonly int numOrd;

            public SortedDocValuesImpl(PagedBytes.Reader bytes, PackedInts.IReader termOrdToBytesOffset,
                PackedInts.IReader docToTermOrd, int numOrd)
            {
                this.bytes = bytes;
                this.docToTermOrd = docToTermOrd;
                this.termOrdToBytesOffset = termOrdToBytesOffset;
                this.numOrd = numOrd;
            }

            public override int ValueCount
            {
                get { return numOrd; }
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
                    throw new ArgumentException("ord must be >=0 (got ord=" + ord + ")");
                }
                bytes.Fill(ret, termOrdToBytesOffset.Get(ord));
            }
        }

        public SortedDocValues GetTermsIndex(AtomicReader reader, string field)
        {
            return GetTermsIndex(reader, field, PackedInts.FAST);
        }

        public SortedDocValues GetTermsIndex(AtomicReader reader, string field, float acceptableOverheadRatio)
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
                    return FieldCache.EMPTY_TERMSINDEX;
                }
                else if (info.HasDocValues)
                {
                    // we don't try to build a sorted instance from numeric/binary doc
                    // values because dedup can be very costly
                    throw new InvalidOperationException("Type mismatch: " + field + " was indexed as " + info.DocValuesTypeValue);
                }
                else if (!info.IsIndexed)
                {
                    return FieldCache.EMPTY_TERMSINDEX;
                }
                return (SortedDocValues)caches[typeof(SortedDocValues)].Get(reader, new CacheKey(field, acceptableOverheadRatio), false);
            }
        }

        internal class SortedDocValuesCache : Cache
        {
            public SortedDocValuesCache(FieldCacheImpl wrapper)
                : base(wrapper)
            {
            }

            protected internal override object CreateValue(AtomicReader reader, CacheKey key, bool setDocsWithField)
            {
                int maxDoc = reader.MaxDoc;

                Terms terms = reader.Terms(key.field);

                float acceptableOverheadRatio = ((float)key.custom);

                PagedBytes bytes = new PagedBytes(15);

                int startBytesBPV;
                int startTermsBPV;
                int startNumUniqueTerms;

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
                    long numUniqueTerms = terms.Size;
                    if (numUniqueTerms != -1L)
                    {
                        if (numUniqueTerms > termCountHardLimit)
                        {
                            // app is misusing the API (there is more than
                            // one term per doc); in this case we make best
                            // effort to load what we can (see LUCENE-2142)
                            numUniqueTerms = termCountHardLimit;
                        }

                        startBytesBPV = PackedInts.BitsRequired(numUniqueTerms * 4);
                        startTermsBPV = PackedInts.BitsRequired(numUniqueTerms);

                        startNumUniqueTerms = (int)numUniqueTerms;
                    }
                    else
                    {
                        startBytesBPV = 1;
                        startTermsBPV = 1;
                        startNumUniqueTerms = 1;
                    }
                }
                else
                {
                    startBytesBPV = 1;
                    startTermsBPV = 1;
                    startNumUniqueTerms = 1;
                }

                GrowableWriter termOrdToBytesOffset = new GrowableWriter(startBytesBPV, 1 + startNumUniqueTerms, acceptableOverheadRatio);
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

                        if (termOrd == termOrdToBytesOffset.Size())
                        {
                            // NOTE: this code only runs if the incoming
                            // reader impl doesn't implement
                            // size (which should be uncommon)
                            termOrdToBytesOffset = termOrdToBytesOffset.Resize(ArrayUtil.Oversize(1 + termOrd, 1));
                        }
                        termOrdToBytesOffset.Set(termOrd, bytes.CopyUsingLengthPrefix(term));
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

                    if (termOrdToBytesOffset.Size() > termOrd)
                    {
                        termOrdToBytesOffset = termOrdToBytesOffset.Resize(termOrd);
                    }
                }

                // maybe an int-only impl?
                return new SortedDocValuesImpl(bytes.Freeze(true), termOrdToBytesOffset.Mutable, docToTermOrd.Mutable, termOrd);
            }
        }

        private class BinaryDocValuesImpl : BinaryDocValues
        {
            private readonly PagedBytes.Reader bytes;
            private readonly PackedInts.IReader docToOffset;

            public BinaryDocValuesImpl(PagedBytes.Reader bytes, PackedInts.IReader docToOffset)
            {
                this.bytes = bytes;
                this.docToOffset = docToOffset;
            }

            public override void Get(int docID, BytesRef ret)
            {
                int pointer = (int)docToOffset.Get(docID);
                if (pointer == 0)
                {
                    ret.bytes = MISSING;
                    ret.offset = 0;
                    ret.length = 0;
                }
                else
                {
                    bytes.Fill(ret, pointer);
                }
            }
        }

        // TODO: this if DocTermsIndex was already created, we
        // should share it...
        public BinaryDocValues GetTerms(AtomicReader reader, string field)
        {
            return GetTerms(reader, field, PackedInts.FAST);
        }

        public BinaryDocValues GetTerms(AtomicReader reader, string field, float acceptableOverheadRatio)
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
                return BinaryDocValues.EMPTY;
            }
            else if (info.HasDocValues)
            {
                throw new InvalidOperationException("Type mismatch: " + field + " was indexed as " + info.DocValuesTypeValue);
            }
            else if (!info.IsIndexed)
            {
                return BinaryDocValues.EMPTY;
            }

            return (BinaryDocValues)caches[typeof(BinaryDocValues)].Get(reader, new CacheKey(field, acceptableOverheadRatio), false);
        }

        internal sealed class BinaryDocValuesCache : Cache
        {
            public BinaryDocValuesCache(FieldCacheImpl wrapper)
                : base(wrapper)
            {
            }

            protected internal override object CreateValue(AtomicReader reader, CacheKey key, bool setDocsWithField)
            {
                // TODO: would be nice to first check if DocTermsIndex
                // was already cached for this field and then return
                // that instead, to avoid insanity

                int maxDoc = reader.MaxDoc;
                Terms terms = reader.Terms(key.field);

                float acceptableOverheadRatio = ((float)key.custom);

                int termCountHardLimit = maxDoc;

                // Holds the actual term data, expanded.
                PagedBytes bytes = new PagedBytes(15);

                int startBPV;

                if (terms != null)
                {
                    // Try for coarse estimate for number of bits; this
                    // should be an underestimate most of the time, which
                    // is fine -- GrowableWriter will reallocate as needed
                    long numUniqueTerms = terms.Size;
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

                // maybe an int-only impl?
                return new BinaryDocValuesImpl(bytes.Freeze(true), docToOffset.Mutable);
            }
        }

        // TODO: this if DocTermsIndex was already created, we
        // should share it...
        public SortedSetDocValues GetDocTermOrds(AtomicReader reader, string field)
        {
            SortedSetDocValues dv = reader.GetSortedSetDocValues(field);
            if (dv != null)
            {
                return dv;
            }

            SortedDocValues sdv = reader.GetSortedDocValues(field);
            if (sdv != null)
            {
                return new SingletonSortedSetDocValues(sdv);
            }

            FieldInfo info = reader.FieldInfos.FieldInfo(field);
            if (info == null)
            {
                return SortedSetDocValues.EMPTY;
            }
            else if (info.HasDocValues)
            {
                throw new InvalidOperationException("Type mismatch: " + field + " was indexed as " + info.DocValuesTypeValue);
            }
            else if (!info.IsIndexed)
            {
                return SortedSetDocValues.EMPTY;
            }

            DocTermOrds dto = (DocTermOrds)caches[typeof(DocTermOrds)].Get(reader, new CacheKey(field, null), false);
            return dto.Iterator(reader);
        }

        internal sealed class DocTermOrdsCache : Cache
        {
            public DocTermOrdsCache(FieldCacheImpl wrapper)
                : base(wrapper)
            {
            }

            protected internal override object CreateValue(AtomicReader reader, CacheKey key, bool setDocsWithField)
            {
                return new DocTermOrds(reader, null, key.field);
            }
        }

        private volatile StreamWriter infoStream;

        public virtual StreamWriter InfoStream
        {
            get { return infoStream; }
            set { infoStream = value; }
        }
    }
}