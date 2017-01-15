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

        private static readonly IComparer<Iterator> SERIALIZED_LENGTH_COMPARER = new ComparerAnonymousInnerClassHelper();

        private class ComparerAnonymousInnerClassHelper : IComparer<Iterator>
        {
            public ComparerAnonymousInnerClassHelper()
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
            Array.Sort(iterators, SERIALIZED_LENGTH_COMPARER);
            WordBuilder builder = (WordBuilder)(new WordBuilder()).SetIndexInterval(indexInterval);
            int wordNum = 0;
            while (true)
            {
                // Advance the least costly iterator first
                iterators[0].AdvanceWord(wordNum);
                wordNum = iterators[0].wordNum;
                if (wordNum == DocIdSetIterator.NO_MORE_DOCS)
                {
                    break;
                }
                byte word = iterators[0].word;
                for (i = 1; i < numSets; ++i)
                {
                    if (iterators[i].wordNum < wordNum)
                    {
                        iterators[i].AdvanceWord(wordNum);
                    }
                    if (iterators[i].wordNum > wordNum)
                    {
                        wordNum = iterators[i].wordNum;
                        goto mainContinue;
                    }
                    Debug.Assert(iterators[i].wordNum == wordNum);
                    word &= iterators[i].word;
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
            if (top.wordNum == int.MaxValue)
            {
                return EMPTY;
            }
            int wordNum = top.wordNum;
            byte word = top.word;
            WordBuilder builder = (WordBuilder)(new WordBuilder()).SetIndexInterval(indexInterval);
            while (true)
            {
                top.NextWord();
                iterators.UpdateTop();
                top = iterators.Top;
                if (top.wordNum == wordNum)
                {
                    word |= top.word;
                }
                else
                {
                    builder.AddWord(wordNum, word);
                    if (top.wordNum == int.MaxValue)
                    {
                        break;
                    }
                    wordNum = top.wordNum;
                    word = top.word;
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
                return a.wordNum < b.wordNum;
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
            internal readonly GrowableByteArrayDataOutput dirtyWords;
            internal int clean;
            internal int lastWordNum;
            internal int numSequences;
            internal int indexInterval;
            internal int cardinality;
            internal bool reverse;

            internal WordBuilder()
            {
                @out = new GrowableByteArrayDataOutput(1024);
                dirtyWords = new GrowableByteArrayDataOutput(128);
                clean = 0;
                lastWordNum = -1;
                numSequences = 0;
                indexInterval = DEFAULT_INDEX_INTERVAL;
                cardinality = 0;
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
                this.indexInterval = indexInterval;
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
                for (int i = 1; i < dirtyWords.Length; ++i)
                {
                    Debug.Assert(dirtyWords.Bytes[i - 1] != 0 || dirtyWords.Bytes[i] != 0);
                    Debug.Assert((byte)dirtyWords.Bytes[i - 1] != 0xFF || (byte)dirtyWords.Bytes[i] != 0xFF);
                }
                return true;
            }

            internal virtual void WriteSequence()
            {
                Debug.Assert(SequenceIsConsistent());
                try
                {
                    WriteHeader(reverse, clean, dirtyWords.Length);
                }
                catch (System.IO.IOException cannotHappen)
                {
                    throw new InvalidOperationException(cannotHappen.ToString(), cannotHappen);
                }
                @out.WriteBytes(dirtyWords.Bytes, 0, dirtyWords.Length);
                dirtyWords.Length = 0;
                ++numSequences;
            }

            internal virtual void AddWord(int wordNum, byte word)
            {
                Debug.Assert(wordNum > lastWordNum);
                Debug.Assert(word != 0);

                if (!reverse)
                {
                    if (lastWordNum == -1)
                    {
                        clean = 2 + wordNum; // special case for the 1st sequence
                        dirtyWords.WriteByte(word);
                    }
                    else
                    {
                        switch (wordNum - lastWordNum)
                        {
                            case 1:
                                if (word == 0xFF && (byte)dirtyWords.Bytes[dirtyWords.Length - 1] == 0xFF)
                                {
                                    --dirtyWords.Length;
                                    WriteSequence();
                                    reverse = true;
                                    clean = 2;
                                }
                                else
                                {
                                    dirtyWords.WriteByte(word);
                                }
                                break;

                            case 2:
                                dirtyWords.WriteByte(0);
                                dirtyWords.WriteByte(word);
                                break;

                            default:
                                WriteSequence();
                                clean = wordNum - lastWordNum - 1;
                                dirtyWords.WriteByte(word);
                                break;
                        }
                    }
                }
                else
                {
                    Debug.Assert(lastWordNum >= 0);
                    switch (wordNum - lastWordNum)
                    {
                        case 1:
                            if (word == 0xFF)
                            {
                                if (dirtyWords.Length == 0)
                                {
                                    ++clean;
                                }
                                else if ((byte)dirtyWords.Bytes[dirtyWords.Length - 1] == 0xFF)
                                {
                                    --dirtyWords.Length;
                                    WriteSequence();
                                    clean = 2;
                                }
                                else
                                {
                                    dirtyWords.WriteByte(word);
                                }
                            }
                            else
                            {
                                dirtyWords.WriteByte(word);
                            }
                            break;

                        case 2:
                            dirtyWords.WriteByte(0);
                            dirtyWords.WriteByte(word);
                            break;

                        default:
                            WriteSequence();
                            reverse = false;
                            clean = wordNum - lastWordNum - 1;
                            dirtyWords.WriteByte(word);
                            break;
                    }
                }
                lastWordNum = wordNum;
                cardinality += BitUtil.BitCount(word);
            }

            /// <summary>
            /// Build a new <seealso cref="WAH8DocIdSet"/>. </summary>
            public virtual WAH8DocIdSet Build()
            {
                if (cardinality == 0)
                {
                    Debug.Assert(lastWordNum == -1);
                    return EMPTY;
                }
                WriteSequence();
                byte[] data = Arrays.CopyOf((byte[])(Array)@out.Bytes, @out.Length);

                // Now build the index
                int valueCount = (numSequences - 1) / indexInterval + 1;
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
                    Iterator it = new Iterator(data, cardinality, int.MaxValue, SINGLE_ZERO_BUFFER, SINGLE_ZERO_BUFFER);
                    Debug.Assert(it.@in.Position == 0);
                    Debug.Assert(it.wordNum == -1);
                    for (int i = 1; i < valueCount; ++i)
                    {
                        // skip indexInterval sequences
                        for (int j = 0; j < indexInterval; ++j)
                        {
                            bool readSequence = it.ReadSequence();
                            Debug.Assert(readSequence);
                            it.SkipDirtyBytes();
                        }
                        int position = it.@in.Position;
                        int wordNum = it.wordNum;
                        positions.Add(position);
                        wordNums.Add(wordNum + 1);
                    }
                    positions.Freeze();
                    wordNums.Freeze();
                    indexPositions = positions;
                    indexWordNums = wordNums;
                }

                return new WAH8DocIdSet(data, cardinality, indexInterval, indexPositions, indexWordNums);
            }
        }

        /// <summary>
        /// A builder for <seealso cref="WAH8DocIdSet"/>s. </summary>
        public sealed class Builder : WordBuilder
        {
            private int lastDocID;
            private int wordNum, word;

            /// <summary>
            /// Sole constructor </summary>
            public Builder()
                : base()
            {
                lastDocID = -1;
                wordNum = -1;
                word = 0;
            }

            /// <summary>
            /// Add a document to this builder. Documents must be added in order. </summary>
            public Builder Add(int docID)
            {
                if (docID <= lastDocID)
                {
                    throw new System.ArgumentException("Doc ids must be added in-order, got " + docID + " which is <= lastDocID=" + lastDocID);
                }
                int wordNum = WordNum(docID);
                if (this.wordNum == -1)
                {
                    this.wordNum = wordNum;
                    word = 1 << (docID & 0x07);
                }
                else if (wordNum == this.wordNum)
                {
                    word |= 1 << (docID & 0x07);
                }
                else
                {
                    AddWord(this.wordNum, (byte)word);
                    this.wordNum = wordNum;
                    word = 1 << (docID & 0x07);
                }
                lastDocID = docID;
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
                if (this.wordNum != -1)
                {
                    AddWord(wordNum, (byte)word);
                }
                return base.Build();
            }
        }

        // where the doc IDs are stored
        private readonly byte[] data;

        private readonly int cardinality;
        private readonly int indexInterval;

        // index for advance(int)
        private readonly MonotonicAppendingLongBuffer positions, wordNums; // wordNums[i] starts at the sequence at positions[i]

        internal WAH8DocIdSet(byte[] data, int cardinality, int indexInterval, MonotonicAppendingLongBuffer positions, MonotonicAppendingLongBuffer wordNums)
        {
            this.data = data;
            this.cardinality = cardinality;
            this.indexInterval = indexInterval;
            this.positions = positions;
            this.wordNums = wordNums;
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
            return new Iterator(data, cardinality, indexInterval, positions, wordNums);
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
            internal readonly int cardinality;
            internal readonly int indexInterval;
            internal readonly MonotonicAppendingLongBuffer positions, wordNums;
            internal readonly int indexThreshold;
            internal int allOnesLength;
            internal int dirtyLength;

            internal int wordNum; // byte offset
            internal byte word; // current word
            internal int bitList; // list of bits set in the current word
            internal int sequenceNum; // in which sequence are we?

            internal int docID;

            internal Iterator(byte[] data, int cardinality, int indexInterval, MonotonicAppendingLongBuffer positions, MonotonicAppendingLongBuffer wordNums)
            {
                this.@in = new ByteArrayDataInput(data);
                this.cardinality = cardinality;
                this.indexInterval = indexInterval;
                this.positions = positions;
                this.wordNums = wordNums;
                wordNum = -1;
                word = 0;
                bitList = 0;
                sequenceNum = -1;
                docID = -1;
                indexThreshold = IndexThreshold(cardinality, indexInterval);
            }

            internal virtual bool ReadSequence()
            {
                if (@in.Eof)
                {
                    wordNum = int.MaxValue;
                    return false;
                }
                int token = @in.ReadByte() & 0xFF;
                if ((token & (1 << 7)) == 0)
                {
                    int cleanLength = ReadCleanLength(@in, token);
                    wordNum += cleanLength;
                }
                else
                {
                    allOnesLength = ReadCleanLength(@in, token);
                }
                dirtyLength = ReadDirtyLength(@in, token);
                Debug.Assert(@in.Length - @in.Position >= dirtyLength, @in.Position + " " + @in.Length + " " + dirtyLength);
                ++sequenceNum;
                return true;
            }

            internal virtual void SkipDirtyBytes(int count)
            {
                Debug.Assert(count >= 0);
                Debug.Assert(count <= allOnesLength + dirtyLength);
                wordNum += count;
                if (count <= allOnesLength)
                {
                    allOnesLength -= count;
                }
                else
                {
                    count -= allOnesLength;
                    allOnesLength = 0;
                    @in.SkipBytes(count);
                    dirtyLength -= count;
                }
            }

            internal virtual void SkipDirtyBytes()
            {
                wordNum += allOnesLength + dirtyLength;
                @in.SkipBytes(dirtyLength);
                allOnesLength = 0;
                dirtyLength = 0;
            }

            internal virtual void NextWord()
            {
                if (allOnesLength > 0)
                {
                    word = 0xFF;
                    ++wordNum;
                    --allOnesLength;
                    return;
                }
                if (dirtyLength > 0)
                {
                    word = @in.ReadByte();
                    ++wordNum;
                    --dirtyLength;
                    if (word != 0)
                    {
                        return;
                    }
                    if (dirtyLength > 0)
                    {
                        word = @in.ReadByte();
                        ++wordNum;
                        --dirtyLength;
                        Debug.Assert(word != 0); // never more than one consecutive 0
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
                int indexSize = (int)wordNums.Count;
                int lo = sequenceNum / indexInterval, hi = lo + 1;
                Debug.Assert(sequenceNum == -1 || wordNums.Get(lo) <= wordNum);
                Debug.Assert(lo + 1 == wordNums.Count || wordNums.Get(lo + 1) > wordNum);
                while (true)
                {
                    if (hi >= indexSize)
                    {
                        hi = indexSize - 1;
                        break;
                    }
                    else if (wordNums.Get(hi) >= targetWordNum)
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
                    int midWordNum = (int)wordNums.Get(mid);
                    if (midWordNum <= targetWordNum)
                    {
                        lo = mid + 1;
                    }
                    else
                    {
                        hi = mid - 1;
                    }
                }
                Debug.Assert(wordNums.Get(hi) <= targetWordNum);
                Debug.Assert(hi + 1 == wordNums.Count || wordNums.Get(hi + 1) > targetWordNum);
                return hi;
            }

            internal virtual void AdvanceWord(int targetWordNum)
            {
                Debug.Assert(targetWordNum > wordNum);
                int delta = targetWordNum - wordNum;
                if (delta <= allOnesLength + dirtyLength + 1)
                {
                    SkipDirtyBytes(delta - 1);
                }
                else
                {
                    SkipDirtyBytes();
                    Debug.Assert(dirtyLength == 0);
                    if (delta > indexThreshold)
                    {
                        // use the index
                        int i = ForwardBinarySearch(targetWordNum);
                        int position = (int)positions.Get(i);
                        if (position > @in.Position) // if the binary search returned a backward offset, don't move
                        {
                            wordNum = (int)wordNums.Get(i) - 1;
                            @in.Position = position;
                            sequenceNum = i * indexInterval - 1;
                        }
                    }

                    while (true)
                    {
                        if (!ReadSequence())
                        {
                            return;
                        }
                        delta = targetWordNum - wordNum;
                        if (delta <= allOnesLength + dirtyLength + 1)
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
                get { return docID; }
            }

            public override int NextDoc()
            {
                if (bitList != 0) // there are remaining bits in the current word
                {
                    docID = (wordNum << 3) | ((bitList & 0x0F) - 1);
                    bitList = (int)((uint)bitList >> 4);
                    return docID;
                }
                NextWord();
                if (wordNum == int.MaxValue)
                {
                    return docID = NO_MORE_DOCS;
                }
                bitList = BitUtil.BitList(word);
                Debug.Assert(bitList != 0);
                docID = (wordNum << 3) | ((bitList & 0x0F) - 1);
                bitList = (int)((uint)bitList >> 4);
                return docID;
            }

            public override int Advance(int target)
            {
                Debug.Assert(target > docID);
                int targetWordNum = WordNum(target);
                if (targetWordNum > this.wordNum)
                {
                    AdvanceWord(targetWordNum);
                    bitList = BitUtil.BitList(word);
                }
                return SlowAdvance(target);
            }

            public override long Cost()
            {
                return cardinality;
            }
        }

        /// <summary>
        /// Return the number of documents in this <seealso cref="DocIdSet"/> in constant time. </summary>
        public int Cardinality()
        {
            return cardinality;
        }

        /// <summary>
        /// Return the memory usage of this class in bytes. </summary>
        public long RamBytesUsed()
        {
            return RamUsageEstimator.AlignObjectSize(3 * RamUsageEstimator.NUM_BYTES_OBJECT_REF + 2 * RamUsageEstimator.NUM_BYTES_INT) 
                + RamUsageEstimator.SizeOf(data) 
                + positions.RamBytesUsed() 
                + wordNums.RamBytesUsed();
        }
    }
}