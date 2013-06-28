using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene3x
{
    [Obsolete]
    internal sealed class TermInfosReader : IDisposable
    {
        private readonly Directory directory;
        private readonly String segment;
        private readonly FieldInfos fieldInfos;

        private readonly CloseableThreadLocal<ThreadResources> threadResources = new CloseableThreadLocal<ThreadResources>();
        private readonly SegmentTermEnum origEnum;
        private readonly long size;

        private readonly TermInfosReaderIndex index;
        private readonly int indexLength;

        private readonly int totalIndexInterval;

        private const int DEFAULT_CACHE_SIZE = 1024;

        // Just adds term's ord to TermInfo
        private sealed class TermInfoAndOrd : TermInfo
        {
            internal long termOrd;

            public TermInfoAndOrd(TermInfo ti, long termOrd)
                : base(ti)
            {
                //assert termOrd >= 0;
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

            public override DoubleBarrelLRUCache.CloneableKey Clone()
            {
                return new CloneableTerm(term);
            }
        }

        private readonly DoubleBarrelLRUCache<CloneableTerm, TermInfoAndOrd> termsCache = new DoubleBarrelLRUCache<CloneableTerm, TermInfoAndOrd>(DEFAULT_CACHE_SIZE);

        private sealed class ThreadResources
        {
            internal SegmentTermEnum termEnum;
        }

        internal TermInfosReader(Directory dir, String seg, FieldInfos fis, IOContext context, int indexDivisor)
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

                origEnum = new SegmentTermEnum(directory.OpenInput(IndexFileNames.SegmentFileName(segment, "", Lucene3xPostingsFormat.TERMS_EXTENSION),
                                                                   context), fieldInfos, false);
                size = origEnum.size;


                if (indexDivisor != -1)
                {
                    // Load terms index
                    totalIndexInterval = origEnum.indexInterval * indexDivisor;

                    String indexFileName = IndexFileNames.SegmentFileName(segment, "", Lucene3xPostingsFormat.TERMS_INDEX_EXTENSION);
                    SegmentTermEnum indexEnum = new SegmentTermEnum(directory.OpenInput(indexFileName,
                                                                                               context), fieldInfos, true);

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

        public int SkipInterval
        {
            get { return origEnum.skipInterval; }
        }

        public int MaxSkipLevels
        {
            get { return origEnum.maxSkipLevels; }
        }

        public void Dispose()
        {
            IOUtils.Close(origEnum, threadResources);
        }

        internal long Size
        {
            get
            {
                return size;
            }
        }

        private ThreadResources GetThreadResources()
        {
            ThreadResources resources = threadResources.Get();
            if (resources == null)
            {
                resources = new ThreadResources();
                resources.termEnum = Terms();
                threadResources.Set(resources);
            }
            return resources;
        }

        private static readonly IComparer<BytesRef> legacyComparator = BytesRef.UTF8SortedAsUTF16Comparer;

        private int CompareAsUTF16(Term term1, Term term2)
        {
            if (term1.Field.Equals(term2.Field))
            {
                return legacyComparator.Compare(term1.Bytes, term2.Bytes);
            }
            else
            {
                return term1.Field.CompareTo(term2.Field);
            }
        }

        internal TermInfo Get(Term term)
        {
            return Get(term, false);
        }

        private TermInfo Get(Term term, bool mustSeekEnum)
        {
            if (size == 0) return null;

            EnsureIndexIsRead();
            TermInfoAndOrd tiOrd = termsCache[new CloneableTerm(term)];
            ThreadResources resources = this.GetThreadResources();

            if (!mustSeekEnum && tiOrd != null)
            {
                return tiOrd;
            }

            return SeekEnum(resources.termEnum, term, tiOrd, true);
        }

        public void CacheCurrentTerm(SegmentTermEnum enumerator)
        {
            termsCache[new CloneableTerm(enumerator.Term)] = new TermInfoAndOrd(enumerator.termInfo, enumerator.position);
        }

        internal static Term DeepCopyOf(Term other)
        {
            return new Term(other.Field, BytesRef.DeepCopyOf(other.Bytes));
        }

        internal TermInfo SeekEnum(SegmentTermEnum enumerator, Term term, bool useCache)
        {
            if (useCache)
            {
                return SeekEnum(enumerator, term,
                                termsCache[new CloneableTerm(DeepCopyOf(term))],
                                useCache);
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
            if (enumerator.Term != null                 // term is at or past current
          && ((enumerator.Prev != null && CompareAsUTF16(term, enumerator.Prev) > 0)
              || CompareAsUTF16(term, enumerator.Term) >= 0))
            {
                int enumOffset = (int)(enumerator.position / totalIndexInterval) + 1;
                if (indexLength == enumOffset    // but before end of block
              || index.CompareTo(term, enumOffset) < 0)
                {
                    // no need to seek

                    TermInfo ti;
                    int numScans = enumerator.ScanTo(term);
                    if (enumerator.Term != null && CompareAsUTF16(term, enumerator.Term) == 0)
                    {
                        ti = enumerator.termInfo;
                        if (numScans > 1)
                        {
                            // we only  want to put this TermInfo into the cache if
                            // scanEnum skipped more than one dictionary entry.
                            // This prevents RangeQueries or WildcardQueries to 
                            // wipe out the cache when they iterate over a large numbers
                            // of terms in order
                            if (tiOrd == null)
                            {
                                if (useCache)
                                {
                                    termsCache[new CloneableTerm(DeepCopyOf(term))] = new TermInfoAndOrd(ti, enumerator.position);
                                }
                            }
                            else
                            {
                                //assert sameTermInfo(ti, tiOrd, enumerator);
                                //assert (int) enumerator.position == tiOrd.termOrd;
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
            TermInfo ti2;

            if (enumerator.Term != null && CompareAsUTF16(term, enumerator.Term) == 0)
            {
                ti2 = enumerator.termInfo;
                if (tiOrd == null)
                {
                    if (useCache)
                    {
                        termsCache[new CloneableTerm(DeepCopyOf(term))] = new TermInfoAndOrd(ti2, enumerator.position);
                    }
                }
                else
                {
                    //assert sameTermInfo(ti, tiOrd, enumerator);
                    //assert enumerator.position == tiOrd.termOrd;
                }
            }
            else
            {
                ti2 = null;
            }
            return ti2;
        }

        // called only from asserts
        private bool SameTermInfo(TermInfo ti1, TermInfo ti2, SegmentTermEnum enumerator)
        {
            if (ti1.docFreq != ti2.docFreq)
            {
                return false;
            }
            if (ti1.freqPointer != ti2.freqPointer)
            {
                return false;
            }
            if (ti1.proxPointer != ti2.proxPointer)
            {
                return false;
            }
            // skipOffset is only valid when docFreq >= skipInterval:
            if (ti1.docFreq >= enumerator.skipInterval &&
                ti1.skipOffset != ti2.skipOffset)
            {
                return false;
            }
            return true;
        }

        private void EnsureIndexIsRead()
        {
            if (index == null)
            {
                throw new InvalidOperationException("terms index was not loaded when this reader was created");
            }
        }

        internal long GetPosition(Term term)
        {
            if (size == 0) return -1;

            EnsureIndexIsRead();
            int indexOffset = index.GetIndexOffset(term);

            SegmentTermEnum enumerator = GetThreadResources().termEnum;
            index.SeekEnum(enumerator, indexOffset);

            while (CompareAsUTF16(term, enumerator.Term) > 0 && enumerator.Next()) { }

            if (CompareAsUTF16(term, enumerator.Term) == 0)
                return enumerator.position;
            else
                return -1;
        }

        public SegmentTermEnum Terms()
        {
            return (SegmentTermEnum)origEnum.Clone();         
        }

        public SegmentTermEnum Terms(Term term)
        {
            Get(term, true);
            return (SegmentTermEnum)GetThreadResources().termEnum.Clone();
        }
    }
}
