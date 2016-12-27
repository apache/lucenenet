using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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

    using ByteArrayDataInput = Lucene.Net.Store.ByteArrayDataInput;
    using DocIdSet = Lucene.Net.Search.DocIdSet;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using MonotonicAppendingLongBuffer = Lucene.Net.Util.Packed.MonotonicAppendingLongBuffer;
    using PackedInts = Lucene.Net.Util.Packed.PackedInts;

    /// <summary>
    /// <seealso cref="DocIdSet"/> implementation based on word-aligned hybrid encoding on
    /// words of 8 bits.
    /// <p>this implementation doesn't support random-access but has a fast
    /// <seealso cref="DocIdSetIterator"/> which can advance in logarithmic time thanks to
    /// an index.</p>
    /// <p>The compression scheme is simplistic and should work well with sparse and
    /// very dense doc id sets while being only slightly larger than a
    /// <seealso cref="FixedBitSet"/> for incompressible sets (overhead&lt;2% in the worst
    /// case) in spite of the index.</p>
    /// <p><b>Format</b>: The format is byte-aligned. An 8-bits word is either clean,
    /// meaning composed only of zeros or ones, or dirty, meaning that it contains
    /// between 1 and 7 bits set. The idea is to encode sequences of clean words
    /// using run-length encoding and to leave sequences of dirty words as-is.</p>
    /// <table>
    ///   <tr><th>Token</th><th>Clean length+</th><th>Dirty length+</th><th>Dirty words</th></tr>
    ///   <tr><td>1 byte</td><td>0-n bytes</td><td>0-n bytes</td><td>0-n bytes</td></tr>
    /// </table>
    /// <ul>
    ///   <li><b>Token</b> encodes whether clean means full of zeros or ones in the
    /// first bit, the number of clean words minus 2 on the next 3 bits and the
    /// number of dirty words on the last 4 bits. The higher-order bit is a
    /// continuation bit, meaning that the number is incomplete and needs additional
    /// bytes to be read.</li>
    ///   <li><b>Clean length+</b>: If clean length has its higher-order bit set,
    /// you need to read a <seealso cref="DataInput#readVInt() vint"/>, shift it by 3 bits on
    /// the left side and add it to the 3 bits which have been read in the token.</li>
    ///   <li><b>Dirty length+</b> works the same way as <b>Clean length+</b> but
    /// on 4 bits and for the length of dirty words.</li>
    ///   <li><b>Dirty words</b> are the dirty words, there are <b>Dirty length</b>
    /// of them.</li>
    /// </ul>
    /// <p>this format cannot encode sequences of less than 2 clean words and 0 dirty
    /// word. The reason is that if you find a single clean word, you should rather
    /// encode it as a dirty word. this takes the same space as starting a new
    /// sequence (since you need one byte for the token) but will be lighter to
    /// decode. There is however an exception for the first sequence. Since the first
    /// sequence may start directly with a dirty word, the clean length is encoded
    /// directly, without subtracting 2.</p>
    /// <p>There is an additional restriction on the format: the sequence of dirty
    /// words is not allowed to contain two consecutive clean words. this restriction
    /// exists to make sure no space is wasted and to make sure iterators can read
    /// the next doc ID by reading at most 2 dirty words.</p>
    /// @lucene.experimental
    /// </summary>
    public sealed class WAH8DocIdSet : DocIdSet
    {
        // Minimum index interval, intervals below this value can't guarantee anymore
        // that this set implementation won't be significantly larger than a FixedBitSet
        // The reason is that a single sequence saves at least one byte and an index
        // entry requires at most 8 bytes (2 ints) so there shouldn't be more than one
        // index entry every 8 sequences
        private const int MIN_INDEX_INTERVAL = 8;

        /// <summary>
        /// Default index interval. </summary>
        public const int DEFAULT_INDEX_INTERVAL = 24;

        private static readonly MonotonicAppendingLongBuffer SINGLE_ZERO_BUFFER = new MonotonicAppendingLongBuffer(1, 64, PackedInts.COMPACT);
        private static WAH8DocIdSet EMPTY = new WAH8DocIdSet(new byte[0], 0, 1, SINGLE_ZERO_BUFFER, SINGLE_ZERO_BUFFER);

        static WAH8DocIdSet()
        {
            SINGLE_ZERO_BUFFER.Add(0L);
            SINGLE_ZERO_BUFFER.Freeze();
        }

        private static readonly IComparer<Iterator> SERIALIZED_LENGTH_COMPARATOR = new ComparatorAnonymousInnerClassHelper();

        private class ComparatorAnonymousInnerClassHelper : IComparer<Iterator>
        {
            public ComparatorAnonymousInnerClassHelper()
            {
            }

            public virtual int Compare(Iterator wi1, Iterator wi2)
            {
                return wi1.@in.Length - wi2.@in.Length;
            }
        }

        /// <summary>
        /// Same as <seealso cref="#intersect(Collection, int)"/> with the default index interval. </summary>
        public static WAH8DocIdSet Intersect(ICollection<WAH8DocIdSet> docIdSets)
        {
            return Intersect(docIdSets, DEFAULT_INDEX_INTERVAL);
        }

        /// <summary>
        /// Compute the intersection of the provided sets. this method is much faster than
        /// computing the intersection manually since it operates directly at the byte level.
        /// </summary>
        public static WAH8DocIdSet Intersect(ICollection<WAH8DocIdSet> docIdSets, int indexInterval)
        {
            switch (docIdSets.Count)
            {
                case 0:
                    throw new System.ArgumentException("There must be at least one set to intersect");
                case 1:
                    var iter = docIdSets.GetEnumerator();
                    iter.MoveNext();
                    return iter.Current;
            }
            // The logic below is similar to ConjunctionScorer
            int numSets = docIdSets.Count;
            var iterators = new Iterator[numSets];
            int i = 0;
            foreach (WAH8DocIdSet set in docIdSets)
            {
                var it = (Iterator)set.GetIterator();
                iterators[i++] = it;
            }
            Array.Sort(iterators, SERIALIZED_LENGTH_COMPARATOR);
            WordBuilder builder = (WordBuilder)(new WordBuilder()).SetIndexInterval(indexInterval);
            int wordNum = 0;
            while (true)
            {
                // Advance the least costly iterator first
                iterators[0].AdvanceWord(wordNum);
                wordNum = iterators[0].WordNum;
                if (wordNum == DocIdSetIterator.NO_MORE_DOCS)
                {
                    break;
                }
                byte word = iterators[0].Word;
                for (i = 1; i < numSets; ++i)
                {
                    if (iterators[i].WordNum < wordNum)
                    {
                        iterators[i].AdvanceWord(wordNum);
                    }
                    if (iterators[i].WordNum > wordNum)
                    {
                        wordNum = iterators[i].WordNum;
                        goto mainContinue;
                    }
                    Debug.Assert(iterators[i].WordNum == wordNum);
                    word &= iterators[i].Word;
                    if (word == 0)
                    {
                        // There are common words, but they don't share any bit
                        ++wordNum;
                        goto mainContinue;
                    }
                }
                // Found a common word
                Debug.Assert(word != 0);
                builder.AddWord(wordNum, word);
                ++wordNum;
            mainContinue: ;
            }
            //mainBreak:
            return builder.Build();
        }

        /// <summary>
        /// Same as <seealso cref="#union(Collection, int)"/> with the default index interval. </summary>
        public static WAH8DocIdSet Union(ICollection<WAH8DocIdSet> docIdSets)
        {
            return Union(docIdSets, DEFAULT_INDEX_INTERVAL);
        }

        /// <summary>
        /// Compute the union of the provided sets. this method is much faster than
        /// computing the union manually since it operates directly at the byte level.
        /// </summary>
        public static WAH8DocIdSet Union(ICollection<WAH8DocIdSet> docIdSets, int indexInterval)
        {
            switch (docIdSets.Count)
            {
                case 0:
                    return EMPTY;

                case 1:
                    var iter = docIdSets.GetEnumerator();
                    iter.MoveNext();
                    return iter.Current;
            }
            // The logic below is very similar to DisjunctionScorer
            int numSets = docIdSets.Count;
            PriorityQueue<Iterator> iterators = new PriorityQueueAnonymousInnerClassHelper(numSets);
            foreach (WAH8DocIdSet set in docIdSets)
            {
                Iterator iterator = (Iterator)set.GetIterator();
                iterator.NextWord();
                iterators.Add(iterator);
            }

            Iterator top = iterators.Top;
            if (top.WordNum == int.MaxValue)
            {
                return EMPTY;
            }
            int wordNum = top.WordNum;
            byte word = top.Word;
            WordBuilder builder = (WordBuilder)(new WordBuilder()).SetIndexInterval(indexInterval);
            while (true)
            {
                top.NextWord();
                iterators.UpdateTop();
                top = iterators.Top;
                if (top.WordNum == wordNum)
                {
                    word |= top.Word;
                }
                else
                {
                    builder.AddWord(wordNum, word);
                    if (top.WordNum == int.MaxValue)
                    {
                        break;
                    }
                    wordNum = top.WordNum;
                    word = top.Word;
                }
            }
            return builder.Build();
        }

        private class PriorityQueueAnonymousInnerClassHelper : PriorityQueue<WAH8DocIdSet.Iterator>
        {
            public PriorityQueueAnonymousInnerClassHelper(int numSets)
                : base(numSets)
            {
            }

            protected internal override bool LessThan(Iterator a, Iterator b)
            {
                return a.WordNum < b.WordNum;
            }
        }

        internal static int WordNum(int docID)
        {
            Debug.Assert(docID >= 0);
            return (int)((uint)docID >> 3);
        }

        /// <summary>
        /// Word-based builder. </summary>
        public class WordBuilder
        {
            internal readonly GrowableByteArrayDataOutput @out;
            internal readonly GrowableByteArrayDataOutput DirtyWords;
            internal int Clean;
            internal int LastWordNum;
            internal int NumSequences;
            internal int IndexInterval_Renamed;
            internal int Cardinality;
            internal bool Reverse;

            internal WordBuilder()
            {
                @out = new GrowableByteArrayDataOutput(1024);
                DirtyWords = new GrowableByteArrayDataOutput(128);
                Clean = 0;
                LastWordNum = -1;
                NumSequences = 0;
                IndexInterval_Renamed = DEFAULT_INDEX_INTERVAL;
                Cardinality = 0;
            }

            /// <summary>
            /// Set the index interval. Smaller index intervals improve performance of
            ///  <seealso cref="DocIdSetIterator#advance(int)"/> but make the <seealso cref="DocIdSet"/>
            ///  larger. An index interval <code>i</code> makes the index add an overhead
            ///  which is at most <code>4/i</code>, but likely much less.The default index
            ///  interval is <code>8</code>, meaning the index has an overhead of at most
            ///  50%. To disable indexing, you can pass <seealso cref="Integer#MAX_VALUE"/> as an
            ///  index interval.
            /// </summary>
            public virtual object SetIndexInterval(int indexInterval)
            {
                if (indexInterval < MIN_INDEX_INTERVAL)
                {
                    throw new System.ArgumentException("indexInterval must be >= " + MIN_INDEX_INTERVAL);
                }
                this.IndexInterval_Renamed = indexInterval;
                return this;
            }

            internal virtual void WriteHeader(bool reverse, int cleanLength, int dirtyLength)
            {
                int cleanLengthMinus2 = cleanLength - 2;
                Debug.Assert(cleanLengthMinus2 >= 0);
                Debug.Assert(dirtyLength >= 0);
                int token = ((cleanLengthMinus2 & 0x03) << 4) | (dirtyLength & 0x07);
                if (reverse)
                {
                    token |= 1 << 7;
                }
                if (cleanLengthMinus2 > 0x03)
                {
                    token |= 1 << 6;
                }
                if (dirtyLength > 0x07)
                {
                    token |= 1 << 3;
                }
                @out.WriteByte((byte)(sbyte)token);
                if (cleanLengthMinus2 > 0x03)
                {
                    @out.WriteVInt((int)((uint)cleanLengthMinus2 >> 2));
                }
                if (dirtyLength > 0x07)
                {
                    @out.WriteVInt((int)((uint)dirtyLength >> 3));
                }
            }

            private bool SequenceIsConsistent()
            {
                for (int i = 1; i < DirtyWords.Length; ++i)
                {
                    Debug.Assert(DirtyWords.Bytes[i - 1] != 0 || DirtyWords.Bytes[i] != 0);
                    Debug.Assert((byte)DirtyWords.Bytes[i - 1] != 0xFF || (byte)DirtyWords.Bytes[i] != 0xFF);
                }
                return true;
            }

            internal virtual void WriteSequence()
            {
                Debug.Assert(SequenceIsConsistent());
                try
                {
                    WriteHeader(Reverse, Clean, DirtyWords.Length);
                }
                catch (System.IO.IOException cannotHappen)
                {
                    throw new InvalidOperationException(cannotHappen.ToString(), cannotHappen);
                }
                @out.WriteBytes(DirtyWords.Bytes, 0, DirtyWords.Length);
                DirtyWords.Length = 0;
                ++NumSequences;
            }

            internal virtual void AddWord(int wordNum, byte word)
            {
                Debug.Assert(wordNum > LastWordNum);
                Debug.Assert(word != 0);

                if (!Reverse)
                {
                    if (LastWordNum == -1)
                    {
                        Clean = 2 + wordNum; // special case for the 1st sequence
                        DirtyWords.WriteByte(word);
                    }
                    else
                    {
                        switch (wordNum - LastWordNum)
                        {
                            case 1:
                                if (word == 0xFF && (byte)DirtyWords.Bytes[DirtyWords.Length - 1] == 0xFF)
                                {
                                    --DirtyWords.Length;
                                    WriteSequence();
                                    Reverse = true;
                                    Clean = 2;
                                }
                                else
                                {
                                    DirtyWords.WriteByte(word);
                                }
                                break;

                            case 2:
                                DirtyWords.WriteByte(0);
                                DirtyWords.WriteByte(word);
                                break;

                            default:
                                WriteSequence();
                                Clean = wordNum - LastWordNum - 1;
                                DirtyWords.WriteByte(word);
                                break;
                        }
                    }
                }
                else
                {
                    Debug.Assert(LastWordNum >= 0);
                    switch (wordNum - LastWordNum)
                    {
                        case 1:
                            if (word == 0xFF)
                            {
                                if (DirtyWords.Length == 0)
                                {
                                    ++Clean;
                                }
                                else if ((byte)DirtyWords.Bytes[DirtyWords.Length - 1] == 0xFF)
                                {
                                    --DirtyWords.Length;
                                    WriteSequence();
                                    Clean = 2;
                                }
                                else
                                {
                                    DirtyWords.WriteByte(word);
                                }
                            }
                            else
                            {
                                DirtyWords.WriteByte(word);
                            }
                            break;

                        case 2:
                            DirtyWords.WriteByte(0);
                            DirtyWords.WriteByte(word);
                            break;

                        default:
                            WriteSequence();
                            Reverse = false;
                            Clean = wordNum - LastWordNum - 1;
                            DirtyWords.WriteByte(word);
                            break;
                    }
                }
                LastWordNum = wordNum;
                Cardinality += BitUtil.BitCount(word);
            }

            /// <summary>
            /// Build a new <seealso cref="WAH8DocIdSet"/>. </summary>
            public virtual WAH8DocIdSet Build()
            {
                if (Cardinality == 0)
                {
                    Debug.Assert(LastWordNum == -1);
                    return EMPTY;
                }
                WriteSequence();
                byte[] data = Arrays.CopyOf((byte[])(Array)@out.Bytes, @out.Length);

                // Now build the index
                int valueCount = (NumSequences - 1) / IndexInterval_Renamed + 1;
                MonotonicAppendingLongBuffer indexPositions, indexWordNums;
                if (valueCount <= 1)
                {
                    indexPositions = indexWordNums = SINGLE_ZERO_BUFFER;
                }
                else
                {
                    const int pageSize = 128;
                    int initialPageCount = (valueCount + pageSize - 1) / pageSize;
                    MonotonicAppendingLongBuffer positions = new MonotonicAppendingLongBuffer(initialPageCount, pageSize, PackedInts.COMPACT);
                    MonotonicAppendingLongBuffer wordNums = new MonotonicAppendingLongBuffer(initialPageCount, pageSize, PackedInts.COMPACT);

                    positions.Add(0L);
                    wordNums.Add(0L);
                    Iterator it = new Iterator(data, Cardinality, int.MaxValue, SINGLE_ZERO_BUFFER, SINGLE_ZERO_BUFFER);
                    Debug.Assert(it.@in.Position == 0);
                    Debug.Assert(it.WordNum == -1);
                    for (int i = 1; i < valueCount; ++i)
                    {
                        // skip indexInterval sequences
                        for (int j = 0; j < IndexInterval_Renamed; ++j)
                        {
                            bool readSequence = it.ReadSequence();
                            Debug.Assert(readSequence);
                            it.SkipDirtyBytes();
                        }
                        int position = it.@in.Position;
                        int wordNum = it.WordNum;
                        positions.Add(position);
                        wordNums.Add(wordNum + 1);
                    }
                    positions.Freeze();
                    wordNums.Freeze();
                    indexPositions = positions;
                    indexWordNums = wordNums;
                }

                return new WAH8DocIdSet(data, Cardinality, IndexInterval_Renamed, indexPositions, indexWordNums);
            }
        }

        /// <summary>
        /// A builder for <seealso cref="WAH8DocIdSet"/>s. </summary>
        public sealed class Builder : WordBuilder
        {
            private int LastDocID;
            private int WordNum, Word;

            /// <summary>
            /// Sole constructor </summary>
            public Builder()
                : base()
            {
                LastDocID = -1;
                WordNum = -1;
                Word = 0;
            }

            /// <summary>
            /// Add a document to this builder. Documents must be added in order. </summary>
            public Builder Add(int docID)
            {
                if (docID <= LastDocID)
                {
                    throw new System.ArgumentException("Doc ids must be added in-order, got " + docID + " which is <= lastDocID=" + LastDocID);
                }
                int wordNum = WordNum(docID);
                if (this.WordNum == -1)
                {
                    this.WordNum = wordNum;
                    Word = 1 << (docID & 0x07);
                }
                else if (wordNum == this.WordNum)
                {
                    Word |= 1 << (docID & 0x07);
                }
                else
                {
                    AddWord(this.WordNum, (byte)Word);
                    this.WordNum = wordNum;
                    Word = 1 << (docID & 0x07);
                }
                LastDocID = docID;
                return this;
            }

            /// <summary>
            /// Add the content of the provided <seealso cref="DocIdSetIterator"/>. </summary>
            public Builder Add(DocIdSetIterator disi)
            {
                for (int doc = disi.NextDoc(); doc != DocIdSetIterator.NO_MORE_DOCS; doc = disi.NextDoc())
                {
                    Add(doc);
                }
                return this;
            }

            public override object SetIndexInterval(int indexInterval)
            {
                return (Builder)base.SetIndexInterval(indexInterval);
            }

            public override WAH8DocIdSet Build()
            {
                if (this.WordNum != -1)
                {
                    AddWord(WordNum, (byte)Word);
                }
                return base.Build();
            }
        }

        // where the doc IDs are stored
        private readonly byte[] Data;

        private readonly int Cardinality_Renamed;
        private readonly int IndexInterval;

        // index for advance(int)
        private readonly MonotonicAppendingLongBuffer Positions, WordNums; // wordNums[i] starts at the sequence at positions[i]

        internal WAH8DocIdSet(byte[] data, int cardinality, int indexInterval, MonotonicAppendingLongBuffer positions, MonotonicAppendingLongBuffer wordNums)
        {
            this.Data = data;
            this.Cardinality_Renamed = cardinality;
            this.IndexInterval = indexInterval;
            this.Positions = positions;
            this.WordNums = wordNums;
        }

        public override bool IsCacheable
        {
            get
            {
                return true;
            }
        }

        public override DocIdSetIterator GetIterator()
        {
            return new Iterator(Data, Cardinality_Renamed, IndexInterval, Positions, WordNums);
        }

        internal static int ReadCleanLength(ByteArrayDataInput @in, int token)
        {
            int len = ((int)((uint)token >> 4)) & 0x07;
            int startPosition = @in.Position;
            if ((len & 0x04) != 0)
            {
                len = (len & 0x03) | (@in.ReadVInt() << 2);
            }
            if (startPosition != 1)
            {
                len += 2;
            }
            return len;
        }

        internal static int ReadDirtyLength(ByteArrayDataInput @in, int token)
        {
            int len = token & 0x0F;
            if ((len & 0x08) != 0)
            {
                len = (len & 0x07) | (@in.ReadVInt() << 3);
            }
            return len;
        }

        internal class Iterator : DocIdSetIterator
        {
            /* Using the index can be costly for close targets. */

            internal static int IndexThreshold(int cardinality, int indexInterval)
            {
                // Short sequences encode for 3 words (2 clean words and 1 dirty byte),
                // don't advance if we are going to read less than 3 x indexInterval
                // sequences
                long indexThreshold = 3L * 3 * indexInterval;
                return (int)Math.Min(int.MaxValue, indexThreshold);
            }

            internal readonly ByteArrayDataInput @in;
            internal readonly int Cardinality;
            internal readonly int IndexInterval;
            internal readonly MonotonicAppendingLongBuffer Positions, WordNums;
            internal readonly int IndexThreshold_Renamed;
            internal int AllOnesLength;
            internal int DirtyLength;

            internal int WordNum; // byte offset
            internal byte Word; // current word
            internal int BitList; // list of bits set in the current word
            internal int SequenceNum; // in which sequence are we?

            internal int DocID_Renamed;

            internal Iterator(byte[] data, int cardinality, int indexInterval, MonotonicAppendingLongBuffer positions, MonotonicAppendingLongBuffer wordNums)
            {
                this.@in = new ByteArrayDataInput(data);
                this.Cardinality = cardinality;
                this.IndexInterval = indexInterval;
                this.Positions = positions;
                this.WordNums = wordNums;
                WordNum = -1;
                Word = 0;
                BitList = 0;
                SequenceNum = -1;
                DocID_Renamed = -1;
                IndexThreshold_Renamed = IndexThreshold(cardinality, indexInterval);
            }

            internal virtual bool ReadSequence()
            {
                if (@in.Eof)
                {
                    WordNum = int.MaxValue;
                    return false;
                }
                int token = @in.ReadByte() & 0xFF;
                if ((token & (1 << 7)) == 0)
                {
                    int cleanLength = ReadCleanLength(@in, token);
                    WordNum += cleanLength;
                }
                else
                {
                    AllOnesLength = ReadCleanLength(@in, token);
                }
                DirtyLength = ReadDirtyLength(@in, token);
                Debug.Assert(@in.Length - @in.Position >= DirtyLength, @in.Position + " " + @in.Length + " " + DirtyLength);
                ++SequenceNum;
                return true;
            }

            internal virtual void SkipDirtyBytes(int count)
            {
                Debug.Assert(count >= 0);
                Debug.Assert(count <= AllOnesLength + DirtyLength);
                WordNum += count;
                if (count <= AllOnesLength)
                {
                    AllOnesLength -= count;
                }
                else
                {
                    count -= AllOnesLength;
                    AllOnesLength = 0;
                    @in.SkipBytes(count);
                    DirtyLength -= count;
                }
            }

            internal virtual void SkipDirtyBytes()
            {
                WordNum += AllOnesLength + DirtyLength;
                @in.SkipBytes(DirtyLength);
                AllOnesLength = 0;
                DirtyLength = 0;
            }

            internal virtual void NextWord()
            {
                if (AllOnesLength > 0)
                {
                    Word = 0xFF;
                    ++WordNum;
                    --AllOnesLength;
                    return;
                }
                if (DirtyLength > 0)
                {
                    Word = @in.ReadByte();
                    ++WordNum;
                    --DirtyLength;
                    if (Word != 0)
                    {
                        return;
                    }
                    if (DirtyLength > 0)
                    {
                        Word = @in.ReadByte();
                        ++WordNum;
                        --DirtyLength;
                        Debug.Assert(Word != 0); // never more than one consecutive 0
                        return;
                    }
                }
                if (ReadSequence())
                {
                    NextWord();
                }
            }

            internal virtual int ForwardBinarySearch(int targetWordNum)
            {
                // advance forward and double the window at each step
                int indexSize = (int)WordNums.Size();
                int lo = SequenceNum / IndexInterval, hi = lo + 1;
                Debug.Assert(SequenceNum == -1 || WordNums.Get(lo) <= WordNum);
                Debug.Assert(lo + 1 == WordNums.Size() || WordNums.Get(lo + 1) > WordNum);
                while (true)
                {
                    if (hi >= indexSize)
                    {
                        hi = indexSize - 1;
                        break;
                    }
                    else if (WordNums.Get(hi) >= targetWordNum)
                    {
                        break;
                    }
                    int newLo = hi;
                    hi += (hi - lo) << 1;
                    lo = newLo;
                }

                // we found a window containing our target, let's binary search now
                while (lo <= hi)
                {
                    int mid = (int)((uint)(lo + hi) >> 1);
                    int midWordNum = (int)WordNums.Get(mid);
                    if (midWordNum <= targetWordNum)
                    {
                        lo = mid + 1;
                    }
                    else
                    {
                        hi = mid - 1;
                    }
                }
                Debug.Assert(WordNums.Get(hi) <= targetWordNum);
                Debug.Assert(hi + 1 == WordNums.Size() || WordNums.Get(hi + 1) > targetWordNum);
                return hi;
            }

            internal virtual void AdvanceWord(int targetWordNum)
            {
                Debug.Assert(targetWordNum > WordNum);
                int delta = targetWordNum - WordNum;
                if (delta <= AllOnesLength + DirtyLength + 1)
                {
                    SkipDirtyBytes(delta - 1);
                }
                else
                {
                    SkipDirtyBytes();
                    Debug.Assert(DirtyLength == 0);
                    if (delta > IndexThreshold_Renamed)
                    {
                        // use the index
                        int i = ForwardBinarySearch(targetWordNum);
                        int position = (int)Positions.Get(i);
                        if (position > @in.Position) // if the binary search returned a backward offset, don't move
                        {
                            WordNum = (int)WordNums.Get(i) - 1;
                            @in.Position = position;
                            SequenceNum = i * IndexInterval - 1;
                        }
                    }

                    while (true)
                    {
                        if (!ReadSequence())
                        {
                            return;
                        }
                        delta = targetWordNum - WordNum;
                        if (delta <= AllOnesLength + DirtyLength + 1)
                        {
                            if (delta > 1)
                            {
                                SkipDirtyBytes(delta - 1);
                            }
                            break;
                        }
                        SkipDirtyBytes();
                    }
                }

                NextWord();
            }

            public override int DocID
            {
                get { return DocID_Renamed; }
            }

            public override int NextDoc()
            {
                if (BitList != 0) // there are remaining bits in the current word
                {
                    DocID_Renamed = (WordNum << 3) | ((BitList & 0x0F) - 1);
                    BitList = (int)((uint)BitList >> 4);
                    return DocID_Renamed;
                }
                NextWord();
                if (WordNum == int.MaxValue)
                {
                    return DocID_Renamed = NO_MORE_DOCS;
                }
                BitList = BitUtil.BitList(Word);
                Debug.Assert(BitList != 0);
                DocID_Renamed = (WordNum << 3) | ((BitList & 0x0F) - 1);
                BitList = (int)((uint)BitList >> 4);
                return DocID_Renamed;
            }

            public override int Advance(int target)
            {
                Debug.Assert(target > DocID_Renamed);
                int targetWordNum = WordNum(target);
                if (targetWordNum > this.WordNum)
                {
                    AdvanceWord(targetWordNum);
                    BitList = BitUtil.BitList(Word);
                }
                return SlowAdvance(target);
            }

            public override long Cost()
            {
                return Cardinality;
            }
        }

        /// <summary>
        /// Return the number of documents in this <seealso cref="DocIdSet"/> in constant time. </summary>
        public int Cardinality()
        {
            return Cardinality_Renamed;
        }

        /// <summary>
        /// Return the memory usage of this class in bytes. </summary>
        public long RamBytesUsed()
        {
            return RamUsageEstimator.AlignObjectSize(3 * RamUsageEstimator.NUM_BYTES_OBJECT_REF + 2 * RamUsageEstimator.NUM_BYTES_INT) 
                + RamUsageEstimator.SizeOf(Data) 
                + Positions.RamBytesUsed() 
                + WordNums.RamBytesUsed();
        }
    }
}