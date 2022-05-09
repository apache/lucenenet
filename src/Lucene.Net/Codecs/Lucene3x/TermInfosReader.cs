using J2N.Text;
using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BytesRef = Lucene.Net.Util.BytesRef;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Codecs.Lucene3x
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

    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// This stores a monotonically increasing set of <c>Term, TermInfo</c> pairs in a
    /// Directory.  Pairs are accessed either by <see cref="Term"/> or by ordinal position the
    /// set.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    [Obsolete("(4.0) this class has been replaced by FormatPostingsTermsDictReader, except for reading old segments.")]
    internal sealed class TermInfosReader : IDisposable
    {
        private readonly Directory directory;
        private readonly string segment;
        private readonly FieldInfos fieldInfos;

#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly DisposableThreadLocal<ThreadResources> threadResources = new DisposableThreadLocal<ThreadResources>();
        private readonly SegmentTermEnum origEnum;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private readonly long size;

        private readonly TermInfosReaderIndex index;
        private readonly int indexLength;

        private readonly int totalIndexInterval;

        private const int DEFAULT_CACHE_SIZE = 1024;

        // Just adds term's ord to TermInfo
        public sealed class TermInfoAndOrd : TermInfo
        {
            internal readonly long termOrd;

            public TermInfoAndOrd(TermInfo ti, long termOrd)
                : base(ti)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(termOrd >= 0);
                this.termOrd = termOrd;
            }
        }

        private class CloneableTerm : DoubleBarrelLRUCache.CloneableKey
        {
            internal Term term;

            public CloneableTerm(Term t)
            {
                this.term = t;
            }

            public override bool Equals(object other)
            {
                CloneableTerm t = (CloneableTerm)other;
                return this.term.Equals(t.term);
            }

            public override int GetHashCode()
            {
                return term.GetHashCode();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override object Clone()
            {
                return new CloneableTerm(term);
            }
        }

        private readonly DoubleBarrelLRUCache<CloneableTerm, TermInfoAndOrd> termsCache = new DoubleBarrelLRUCache<CloneableTerm, TermInfoAndOrd>(DEFAULT_CACHE_SIZE);

        /// <summary>
        /// Per-thread resources managed by ThreadLocal.
        /// </summary>
        private sealed class ThreadResources
        {
            internal SegmentTermEnum termEnum;
        }

        internal TermInfosReader(Directory dir, string seg, FieldInfos fis, IOContext context, int indexDivisor)
        {
            bool success = false;

            if (indexDivisor < 1 && indexDivisor != -1)
            {
                throw new ArgumentException("indexDivisor must be -1 (don't load terms index) or greater than 0: got " + indexDivisor);
            }

            try
            {
                directory = dir;
                segment = seg;
                fieldInfos = fis;

                origEnum = new SegmentTermEnum(directory.OpenInput(IndexFileNames.SegmentFileName(segment, "", Lucene3xPostingsFormat.TERMS_EXTENSION), context), fieldInfos, false);
                size = origEnum.size;

                if (indexDivisor != -1)
                {
                    // Load terms index
                    totalIndexInterval = origEnum.indexInterval * indexDivisor;

                    string indexFileName = IndexFileNames.SegmentFileName(segment, "", Lucene3xPostingsFormat.TERMS_INDEX_EXTENSION);
                    SegmentTermEnum indexEnum = new SegmentTermEnum(directory.OpenInput(indexFileName, context), fieldInfos, true);

                    try
                    {
                        index = new TermInfosReaderIndex(indexEnum, indexDivisor, dir.FileLength(indexFileName), totalIndexInterval);
                        indexLength = index.Length;
                    }
                    finally
                    {
                        indexEnum.Dispose();
                    }
                }
                else
                {
                    // Do not load terms index:
                    totalIndexInterval = -1;
                    index = null;
                    indexLength = -1;
                }
                success = true;
            }
            finally
            {
                // With lock-less commits, it's entirely possible (and
                // fine) to hit a FileNotFound exception above. In
                // this case, we want to explicitly close any subset
                // of things that were opened so that we don't have to
                // wait for a GC to do so.
                if (!success)
                {
                    Dispose();
                }
            }
        }

        public int SkipInterval => origEnum.skipInterval;

        public int MaxSkipLevels => origEnum.maxSkipLevels;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            IOUtils.Dispose(origEnum, threadResources);
        }

        /// <summary>
        /// Returns the number of term/value pairs in the set.
        /// <para/>
        /// NOTE: This was size() in Lucene.
        /// </summary>
        internal long Count => size;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ThreadResources GetThreadResources()
        {
            ThreadResources resources = threadResources.Value;
            if (resources is null)
            {
                resources = new ThreadResources();
                resources.termEnum = Terms();
                threadResources.Value = resources;
            }
            return resources;
        }

        private static readonly IComparer<BytesRef> legacyComparer = BytesRef.UTF8SortedAsUTF16Comparer;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CompareAsUTF16(Term term1, Term term2) // LUCENENET: CA1822: Mark members as static
        {
            if (term1.Field.Equals(term2.Field, StringComparison.Ordinal))
            {
                return legacyComparer.Compare(term1.Bytes, term2.Bytes);
            }
            else
            {
                return term1.Field.CompareToOrdinal(term2.Field);
            }
        }

        /// <summary>
        /// Returns the <see cref="TermInfo"/> for a <see cref="Term"/> in the set, or <c>null</c>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal TermInfo Get(Term term)
        {
            return Get(term, false);
        }

        /// <summary>
        /// Returns the <see cref="TermInfo"/> for a <see cref="Term"/> in the set, or <c>null</c>. </summary>
        private TermInfo Get(Term term, bool mustSeekEnum)
        {
            if (size == 0)
            {
                return null;
            }

            EnsureIndexIsRead();
            TermInfoAndOrd tiOrd = termsCache.Get(new CloneableTerm(term));
            ThreadResources resources = GetThreadResources();

            if (!mustSeekEnum && tiOrd != null)
            {
                return tiOrd;
            }

            return SeekEnum(resources.termEnum, term, tiOrd, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CacheCurrentTerm(SegmentTermEnum enumerator)
        {
            termsCache.Put(new CloneableTerm(enumerator.Term()), new TermInfoAndOrd(enumerator.termInfo, enumerator.position));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Term DeepCopyOf(Term other)
        {
            return new Term(other.Field, BytesRef.DeepCopyOf(other.Bytes));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal TermInfo SeekEnum(SegmentTermEnum enumerator, Term term, bool useCache)
        {
            if (useCache)
            {
                return SeekEnum(enumerator, term, termsCache.Get(new CloneableTerm(DeepCopyOf(term))), useCache);
            }
            else
            {
                return SeekEnum(enumerator, term, null, useCache);
            }
        }

        internal TermInfo SeekEnum(SegmentTermEnum enumerator, Term term, TermInfoAndOrd tiOrd, bool useCache)
        {
            if (size == 0)
            {
                return null;
            }

            // optimize sequential access: first try scanning cached enum w/o seeking
            if (enumerator.Term() != null && ((enumerator.Prev() != null && CompareAsUTF16(term, enumerator.Prev()) > 0) || CompareAsUTF16(term, enumerator.Term()) >= 0)) // term is at or past current
            {
                int enumOffset = (int)(enumerator.position / totalIndexInterval) + 1;
                if (indexLength == enumOffset || index.CompareTo(term, enumOffset) < 0) // but before end of block
                {
                    // no need to seek

                    TermInfo ti;
                    int numScans = enumerator.ScanTo(term);
                    if (enumerator.Term() != null && CompareAsUTF16(term, enumerator.Term()) == 0)
                    {
                        ti = enumerator.termInfo;
                        if (numScans > 1)
                        {
                            // we only  want to put this TermInfo into the cache if
                            // scanEnum skipped more than one dictionary entry.
                            // this prevents RangeQueries or WildcardQueries to
                            // wipe out the cache when they iterate over a large numbers
                            // of terms in order
                            if (tiOrd is null)
                            {
                                if (useCache)
                                {
                                    termsCache.Put(new CloneableTerm(DeepCopyOf(term)), new TermInfoAndOrd(ti, enumerator.position));
                                }
                            }
                            else if (Debugging.AssertsEnabled)
                            {
                                Debugging.Assert(SameTermInfo(ti, tiOrd, enumerator));
                                Debugging.Assert((int)enumerator.position == tiOrd.termOrd);
                            }
                        }
                    }
                    else
                    {
                        ti = null;
                    }

                    return ti;
                }
            }

            // random-access: must seek
            int indexPos;
            if (tiOrd != null)
            {
                indexPos = (int)(tiOrd.termOrd / totalIndexInterval);
            }
            else
            {
                // Must do binary search:
                indexPos = index.GetIndexOffset(term);
            }

            index.SeekEnum(enumerator, indexPos);
            enumerator.ScanTo(term);
            TermInfo ti_;

            if (enumerator.Term() != null && CompareAsUTF16(term, enumerator.Term()) == 0)
            {
                ti_ = enumerator.termInfo;
                if (tiOrd is null)
                {
                    if (useCache)
                    {
                        termsCache.Put(new CloneableTerm(DeepCopyOf(term)), new TermInfoAndOrd(ti_, enumerator.position));
                    }
                }
                else if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(SameTermInfo(ti_, tiOrd, enumerator));
                    Debugging.Assert(enumerator.position == tiOrd.termOrd);
                }
            }
            else
            {
                ti_ = null;
            }
            return ti_;
        }

        // called only from asserts
        private static bool SameTermInfo(TermInfo ti1, TermInfo ti2, SegmentTermEnum enumerator) // LUCENENET: CA1822: Mark members as static
        {
            if (ti1.DocFreq != ti2.DocFreq)
            {
                return false;
            }
            if (ti1.FreqPointer != ti2.FreqPointer)
            {
                return false;
            }
            if (ti1.ProxPointer != ti2.ProxPointer)
            {
                return false;
            }
            // skipOffset is only valid when docFreq >= skipInterval:
            if (ti1.DocFreq >= enumerator.skipInterval && ti1.SkipOffset != ti2.SkipOffset)
            {
                return false;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureIndexIsRead()
        {
            if (index is null)
            {
                throw IllegalStateException.Create("terms index was not loaded when this reader was created");
            }
        }

        /// <summary>
        /// Returns the position of a <see cref="Term"/> in the set or -1. </summary>
        internal long GetPosition(Term term)
        {
            if (size == 0)
            {
                return -1;
            }

            EnsureIndexIsRead();
            int indexOffset = index.GetIndexOffset(term);

            SegmentTermEnum enumerator = GetThreadResources().termEnum;
            index.SeekEnum(enumerator, indexOffset);

            while (CompareAsUTF16(term, enumerator.Term()) > 0 && enumerator.Next())
            {
            }

            if (CompareAsUTF16(term, enumerator.Term()) == 0)
            {
                return enumerator.position;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Returns an enumeration of all the <see cref="Term"/>s and <see cref="TermInfo"/>s in the set. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SegmentTermEnum Terms()
        {
            return (SegmentTermEnum)origEnum.Clone();
        }

        /// <summary>
        /// Returns an enumeration of terms starting at or after the named term. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SegmentTermEnum Terms(Term term)
        {
            Get(term, true);
            return (SegmentTermEnum)GetThreadResources().termEnum.Clone();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal long RamBytesUsed()
        {
            return index is null ? 0 : index.RamBytesUsed();
        }
    }
}