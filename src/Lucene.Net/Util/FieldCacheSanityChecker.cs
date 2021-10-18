using Lucene.Net.Search;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Util
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

    using IndexReader = Lucene.Net.Index.IndexReader;

    /// <summary>
    /// <para>
    /// Provides methods for sanity checking that entries in the FieldCache
    /// are not wasteful or inconsistent.
    /// </para>
    /// <para>
    /// Lucene 2.9 Introduced numerous enhancements into how the FieldCache
    /// is used by the low levels of Lucene searching (for Sorting and
    /// ValueSourceQueries) to improve both the speed for Sorting, as well
    /// as reopening of IndexReaders.  But these changes have shifted the
    /// usage of FieldCache from "top level" IndexReaders (frequently a
    /// MultiReader or DirectoryReader) down to the leaf level SegmentReaders.
    /// As a result, existing applications that directly access the FieldCache
    /// may find RAM usage increase significantly when upgrading to 2.9 or
    /// Later.  This class provides an API for these applications (or their
    /// Unit tests) to check at run time if the FieldCache contains "insane"
    /// usages of the FieldCache.
    /// </para>
    /// @lucene.experimental 
    /// </summary>
    /// <seealso cref="IFieldCache"/>
    /// <seealso cref="FieldCacheSanityChecker.Insanity"/>
    /// <seealso cref="FieldCacheSanityChecker.InsanityType"/>
    public sealed class FieldCacheSanityChecker
    {
        private readonly bool estimateRam;

        public FieldCacheSanityChecker()
        {
            /* NOOP */
        }

        /// <param name="estimateRam">If set, estimate size for all <see cref="FieldCache.CacheEntry"/> objects will be calculated.</param>
        // LUCENENET specific - added this constructor overload so we wouldn't need a (ridiculous) SetRamUsageEstimator() method
        public FieldCacheSanityChecker(bool estimateRam)
        {
            this.estimateRam = estimateRam;
        }

        // LUCENENET specific - using constructor overload to replace this method
        ///// <summary>
        ///// If set, estimate size for all CacheEntry objects will be calculateed.
        ///// </summary>
        //public bool SetRamUsageEstimator(bool flag)
        //{
        //    estimateRam = flag;
        //}

        /// <summary>
        /// Quick and dirty convenience method </summary>
        /// <seealso cref="Check(FieldCache.CacheEntry[])"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Insanity[] CheckSanity(IFieldCache cache)
        {
            return CheckSanity(cache.GetCacheEntries());
        }

        /// <summary>
        /// Quick and dirty convenience method that instantiates an instance with
        /// "good defaults" and uses it to test the <see cref="FieldCache.CacheEntry"/>s </summary>
        /// <seealso cref="Check(FieldCache.CacheEntry[])"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Insanity[] CheckSanity(params FieldCache.CacheEntry[] cacheEntries)
        {
            FieldCacheSanityChecker sanityChecker = new FieldCacheSanityChecker(estimateRam: true);
            return sanityChecker.Check(cacheEntries);
        }

        /// <summary>
        /// Tests a CacheEntry[] for indication of "insane" cache usage.
        /// <para>
        /// <b>NOTE:</b>FieldCache CreationPlaceholder objects are ignored.
        /// (:TODO: is this a bad idea? are we masking a real problem?)
        /// </para>
        /// </summary>
        public Insanity[] Check(params FieldCache.CacheEntry[] cacheEntries)
        {
            if (null == cacheEntries || 0 == cacheEntries.Length)
            {
                return Arrays.Empty<Insanity>();
            }

            if (estimateRam)
            {
                for (int i = 0; i < cacheEntries.Length; i++)
                {
                    cacheEntries[i].EstimateSize();
                }
            }

            // the indirect mapping lets MapOfSet dedup identical valIds for us
            // maps the (valId) identityhashCode of cache values to
            // sets of CacheEntry instances
            MapOfSets<int, FieldCache.CacheEntry> valIdToItems = new MapOfSets<int, FieldCache.CacheEntry>(new Dictionary<int, ISet<FieldCache.CacheEntry>>(17));
            // maps ReaderField keys to Sets of ValueIds
            MapOfSets<ReaderField, int> readerFieldToValIds = new MapOfSets<ReaderField, int>(new Dictionary<ReaderField, ISet<int>>(17));

            // any keys that we know result in more then one valId
            ISet<ReaderField> valMismatchKeys = new JCG.HashSet<ReaderField>();

            // iterate over all the cacheEntries to get the mappings we'll need
            for (int i = 0; i < cacheEntries.Length; i++)
            {
                FieldCache.CacheEntry item = cacheEntries[i];
                object val = item.Value;

                // It's OK to have dup entries, where one is eg
                // float[] and the other is the Bits (from
                // getDocWithField())
                if (val is IBits)
                {
                    continue;
                }

                if (val is FieldCache.ICreationPlaceholder)
                {
                    continue;
                }

                ReaderField rf = new ReaderField(item.ReaderKey, item.FieldName);

                int valId = RuntimeHelpers.GetHashCode(val);

                // indirect mapping, so the MapOfSet will dedup identical valIds for us
                valIdToItems.Put(valId, item);
                if (1 < readerFieldToValIds.Put(rf, valId))
                {
                    valMismatchKeys.Add(rf);
                }
            }

            JCG.List<Insanity> insanity = new JCG.List<Insanity>(valMismatchKeys.Count * 3);

            insanity.AddRange(CheckValueMismatch(valIdToItems, readerFieldToValIds, valMismatchKeys));
            insanity.AddRange(CheckSubreaders(valIdToItems, readerFieldToValIds));

            return insanity.ToArray();
        }

        /// <summary>
        /// Internal helper method used by check that iterates over
        /// <paramref name="valMismatchKeys"/> and generates a <see cref="ICollection{T}"/> of <see cref="Insanity"/>
        /// instances accordingly.  The <see cref="MapOfSets{TKey, TValue}"/> are used to populate
        /// the <see cref="Insanity"/> objects. </summary>
        /// <seealso cref="InsanityType.VALUEMISMATCH"/>
        private static ICollection<Insanity> CheckValueMismatch( // LUCENENET: CA1822: Mark members as static
            MapOfSets<int, FieldCache.CacheEntry> valIdToItems,
            MapOfSets<ReaderField, int> readerFieldToValIds,
            ISet<ReaderField> valMismatchKeys)
        {
            JCG.List<Insanity> insanity = new JCG.List<Insanity>(valMismatchKeys.Count * 3);

            if (valMismatchKeys.Count != 0)
            {
                // we have multiple values for some ReaderFields

                IDictionary<ReaderField, ISet<int>> rfMap = readerFieldToValIds.Map;
                IDictionary<int, ISet<FieldCache.CacheEntry>> valMap = valIdToItems.Map;
                foreach (ReaderField rf in valMismatchKeys)
                {
                    IList<FieldCache.CacheEntry> badEntries = new JCG.List<FieldCache.CacheEntry>(valMismatchKeys.Count * 2);
                    foreach (int value in rfMap[rf])
                    {
                        foreach (FieldCache.CacheEntry cacheEntry in valMap[value])
                        {
                            badEntries.Add(cacheEntry);
                        }
                    }

                    FieldCache.CacheEntry[] badness = new FieldCache.CacheEntry[badEntries.Count];
                    badEntries.CopyTo(badness, 0);

                    insanity.Add(new Insanity(InsanityType.VALUEMISMATCH, "Multiple distinct value objects for " + rf.ToString(), badness));
                }
            }
            return insanity;
        }

        /// <summary>
        /// Internal helper method used by check that iterates over
        /// the keys of <paramref name="readerFieldToValIds"/> and generates a <see cref="ICollection{T}"/>
        /// of <see cref="Insanity"/> instances whenever two (or more) <see cref="ReaderField"/> instances are
        /// found that have an ancestry relationships.
        /// </summary>
        /// <seealso cref="InsanityType.SUBREADER"/>
        private static ICollection<Insanity> CheckSubreaders(MapOfSets<int, FieldCache.CacheEntry> valIdToItems, MapOfSets<ReaderField, int> readerFieldToValIds) // LUCENENET: CA1822: Mark members as static
        {
            JCG.List<Insanity> insanity = new JCG.List<Insanity>(23);

            Dictionary<ReaderField, ISet<ReaderField>> badChildren = new Dictionary<ReaderField, ISet<ReaderField>>(17);
            MapOfSets<ReaderField, ReaderField> badKids = new MapOfSets<ReaderField, ReaderField>(badChildren); // wrapper

            IDictionary<int, ISet<FieldCache.CacheEntry>> viToItemSets = valIdToItems.Map;
            IDictionary<ReaderField, ISet<int>> rfToValIdSets = readerFieldToValIds.Map;

            HashSet<ReaderField> seen = new HashSet<ReaderField>();

            //IDictionary<ReaderField, ISet<int>>.KeyCollection readerFields = rfToValIdSets.Keys;
            foreach (ReaderField rf in rfToValIdSets.Keys)
            {
                if (seen.Contains(rf))
                {
                    continue;
                }

                IList<object> kids = GetAllDescendantReaderKeys(rf.ReaderKey);
                foreach (object kidKey in kids)
                {
                    ReaderField kid = new ReaderField(kidKey, rf.FieldName);

                    // LUCENENET: Eliminated extra lookup by using TryGetValue instead of ContainsKey
                    if (badChildren.TryGetValue(kid, out ISet<ReaderField> badKid))
                    {
                        // we've already process this kid as RF and found other problems
                        // track those problems as our own
                        badKids.Put(rf, kid);
                        badKids.PutAll(rf, badKid);
                        badChildren.Remove(kid);
                    }
                    else if (rfToValIdSets.ContainsKey(kid))
                    {
                        // we have cache entries for the kid
                        badKids.Put(rf, kid);
                    }
                    seen.Add(kid);
                }
                seen.Add(rf);
            }

            // every mapping in badKids represents an Insanity
            foreach (ReaderField parent in badChildren.Keys)
            {
                ISet<ReaderField> kids = badChildren[parent];

                JCG.List<FieldCache.CacheEntry> badEntries = new JCG.List<FieldCache.CacheEntry>(kids.Count * 2);

                // put parent entr(ies) in first
                {
                    foreach (int value in rfToValIdSets[parent])
                    {
                        badEntries.AddRange(viToItemSets[value]);
                    }
                }

                // now the entries for the descendants
                foreach (ReaderField kid in kids)
                {
                    foreach (int value in rfToValIdSets[kid])
                    {
                        badEntries.AddRange(viToItemSets[value]);
                    }
                }

                FieldCache.CacheEntry[] badness = badEntries.ToArray();

                insanity.Add(new Insanity(InsanityType.SUBREADER, "Found caches for descendants of " + parent.ToString(), badness));
            }

            return insanity;
        }

        /// <summary>
        /// Checks if the <paramref name="seed"/> is an <see cref="IndexReader"/>, and if so will walk
        /// the hierarchy of subReaders building up a list of the objects
        /// returned by <c>seed.CoreCacheKey</c>
        /// </summary>
        private static IList<object> GetAllDescendantReaderKeys(object seed) // LUCENENET: CA1822: Mark members as static
        {
            var all = new JCG.List<object>(17) { seed }; // will grow as we iter
            for (var i = 0; i < all.Count; i++)
            {
                var obj = all[i];
                // TODO: We don't check closed readers here (as getTopReaderContext
                // throws ObjectDisposedException), what should we do? Reflection?
                if (obj is IndexReader reader)
                {
                    try
                    {
                        var childs = reader.Context.Children;
                        if (childs != null) // it is composite reader
                        {
                            foreach (var ctx in childs)
                            {
                                all.Add(ctx.Reader.CoreCacheKey);
                            }
                        }
                    }
                    catch (Exception ace) when (ace.IsAlreadyClosedException())
                    {
                        // ignore this reader
                    }
                }
            }
            // need to skip the first, because it was the seed
            return all.GetView(1, all.Count - 1); // LUCENENET: Converted end index to length
        }

        /// <summary>
        /// Simple pair object for using "readerKey + fieldName" a Map key
        /// </summary>
        private sealed class ReaderField
        {
            public object ReaderKey => readerKey;
            private readonly object readerKey;
            public string FieldName { get; private set; }

            public ReaderField(object readerKey, string fieldName)
            {
                this.readerKey = readerKey;
                this.FieldName = fieldName;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode()
            {
                return RuntimeHelpers.GetHashCode(readerKey) * FieldName.GetHashCode();
            }

            public override bool Equals(object that)
            {
                if (!(that is ReaderField))
                {
                    return false;
                }

                ReaderField other = (ReaderField)that;
                return (object.ReferenceEquals(this.readerKey, other.readerKey) 
                    && this.FieldName.Equals(other.FieldName, StringComparison.Ordinal));
            }

            public override string ToString()
            {
                return readerKey.ToString() + "+" + FieldName;
            }
        }

        /// <summary>
        /// Simple container for a collection of related <see cref="FieldCache.CacheEntry"/> objects that
        /// in conjunction with each other represent some "insane" usage of the
        /// <see cref="IFieldCache"/>.
        /// </summary>
        public sealed class Insanity
        {
            private readonly InsanityType type;
            private readonly string msg;
            private readonly FieldCache.CacheEntry[] entries;

            public Insanity(InsanityType type, string msg, params FieldCache.CacheEntry[] entries)
            {
                // LUCENENET specific - rearranged order to take advantage of throw expressions and
                // changed from IllegalArgumentException to ArgumentNullException (.NET convention)
                this.type = type ?? throw new ArgumentNullException(nameof(type), "Insanity requires non-null InsanityType");
                this.entries = entries ?? throw new ArgumentNullException(nameof(entries), "Insanity requires non-null CacheEntry[]");
                if (0 == entries.Length)
                    throw new ArgumentException("Insanity requires non-empty CacheEntry[]");
                this.msg = msg;
            }

            /// <summary>
            /// Type of insane behavior this object represents
            /// </summary>
            public InsanityType Type => type;

            /// <summary>
            /// Description of the insane behavior
            /// </summary>
            public string Msg => msg;

            /// <summary>
            /// <see cref="FieldCache.CacheEntry"/> objects which suggest a problem
            /// </summary>
            [WritableArray]
            [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
            public FieldCache.CacheEntry[] CacheEntries => entries;

            /// <summary>
            /// Multi-Line representation of this <see cref="Insanity"/> object, starting with
            /// the Type and Msg, followed by each CacheEntry.ToString() on it's
            /// own line prefaced by a tab character
            /// </summary>
            public override string ToString()
            {
                StringBuilder buf = new StringBuilder();
                buf.Append(Type).Append(": ");

                string m = Msg;
                if (null != m)
                {
                    buf.Append(m);
                }

                buf.Append('\n');

                FieldCache.CacheEntry[] ce = CacheEntries;
                for (int i = 0; i < ce.Length; i++)
                {
                    buf.Append('\t').Append(ce[i].ToString()).Append('\n');
                }

                return buf.ToString();
            }
        }

        /// <summary>
        /// An Enumeration of the different types of "insane" behavior that
        /// may be detected in a <see cref="IFieldCache"/>.
        /// </summary>
        /// <seealso cref="InsanityType.SUBREADER"/>
        /// <seealso cref="InsanityType.VALUEMISMATCH"/>
        /// <seealso cref="InsanityType.EXPECTED"/>
        public sealed class InsanityType
        {
            private readonly string label;

            private InsanityType(string label)
            {
                this.label = label;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override string ToString()
            {
                return label;
            }

            /// <summary>
            /// Indicates an overlap in cache usage on a given field
            /// in sub/super readers.
            /// </summary>
            public static readonly InsanityType SUBREADER = new InsanityType("SUBREADER");

            /// <summary>
            /// <para>
            /// Indicates entries have the same reader+fieldname but
            /// different cached values.  This can happen if different datatypes,
            /// or parsers are used -- and while it's not necessarily a bug
            /// it's typically an indication of a possible problem.
            /// </para>
            /// <para>
            /// <b>NOTE:</b> Only the reader, fieldname, and cached value are actually
            /// tested -- if two cache entries have different parsers or datatypes but
            /// the cached values are the same Object (== not just Equal()) this method
            /// does not consider that a red flag.  This allows for subtle variations
            /// in the way a Parser is specified (null vs DEFAULT_INT64_PARSER, etc...)
            /// </para>
            /// </summary>
            public static readonly InsanityType VALUEMISMATCH = new InsanityType("VALUEMISMATCH");

            /// <summary>
            /// Indicates an expected bit of "insanity".  This may be useful for
            /// clients that wish to preserve/log information about insane usage
            /// but indicate that it was expected.
            /// </summary>
            public static readonly InsanityType EXPECTED = new InsanityType("EXPECTED");
        }
    }
}