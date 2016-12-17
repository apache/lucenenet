using System;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Compressing
{
    using Lucene.Net.Support;

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

    using DataInput = Lucene.Net.Store.DataInput;
    using DataOutput = Lucene.Net.Store.DataOutput;
    using PackedInts = Lucene.Net.Util.Packed.PackedInts;

    /// <summary>
    /// LZ4 compression and decompression routines.
    ///
    /// http://code.google.com/p/lz4/
    /// http://fastcompression.blogspot.fr/p/lz4.html
    /// </summary>
    public sealed class LZ4
    {
        private LZ4()
        {
        }

        internal const int MEMORY_USAGE = 14;
        internal const int MIN_MATCH = 4; // minimum length of a match
        internal static readonly int MAX_DISTANCE = 1 << 16; // maximum distance of a reference
        internal const int LAST_LITERALS = 5; // the last 5 bytes must be encoded as literals
        internal const int HASH_LOG_HC = 15; // log size of the dictionary for compressHC
        internal static readonly int HASH_TABLE_SIZE_HC = 1 << HASH_LOG_HC;
        internal static readonly int OPTIMAL_ML = 0x0F + 4 - 1; // match length that doesn't require an additional byte

        private static int Hash(int i, int hashBits)
        {
            return Number.URShift((i * -1640531535), (32 - hashBits));
        }

        private static int HashHC(int i)
        {
            return Hash(i, HASH_LOG_HC);
        }

        private static int ReadInt(byte[] buf, int i)
        {
            return ((((sbyte)buf[i]) & 0xFF) << 24) | ((((sbyte)buf[i + 1]) & 0xFF) << 16) | ((((sbyte)buf[i + 2]) & 0xFF) << 8) |
                (((sbyte)buf[i + 3]) & 0xFF);
        }

        private static bool ReadIntEquals(byte[] buf, int i, int j)
        {
            return ReadInt(buf, i) == ReadInt(buf, j);
        }

        private static int CommonBytes(byte[] b, int o1, int o2, int limit)
        {
            Debug.Assert(o1 < o2);
            int count = 0;
            while (o2 < limit && b[o1++] == b[o2++])
            {
                ++count;
            }
            return count;
        }

        private static int CommonBytesBackward(byte[] b, int o1, int o2, int l1, int l2)
        {
            int count = 0;
            while (o1 > l1 && o2 > l2 && b[--o1] == b[--o2])
            {
                ++count;
            }
            return count;
        }

        /// <summary>
        /// Decompress at least <code>decompressedLen</code> bytes into
        /// <code>dest[dOff:]</code>. Please note that <code>dest</code> must be large
        /// enough to be able to hold <b>all</b> decompressed data (meaning that you
        /// need to know the total decompressed length).
        /// </summary>
        public static int Decompress(DataInput compressed, int decompressedLen, byte[] dest, int dOff)
        {
            int destEnd = dest.Length;

            do
            {
                // literals
                int token = compressed.ReadByte() & 0xFF;
                int literalLen = (int)(((uint)token) >> 4);

                if (literalLen != 0)
                {
                    if (literalLen == 0x0F)
                    {
                        byte len;
                        while ((len = compressed.ReadByte()) == 0xFF)
                        {
                            literalLen += 0xFF;
                        }
                        literalLen += len & 0xFF;
                    }
                    compressed.ReadBytes(dest, dOff, literalLen);
                    dOff += literalLen;
                }

                if (dOff >= decompressedLen)
                {
                    break;
                }

                // matchs
                var byte1 = compressed.ReadByte();
                var byte2 = compressed.ReadByte();
                int matchDec = (byte1 & 0xFF) | ((byte2 & 0xFF) << 8);
                Debug.Assert(matchDec > 0);

                int matchLen = token & 0x0F;
                if (matchLen == 0x0F)
                {
                    int len;
                    while ((len = compressed.ReadByte()) == 0xFF)
                    {
                        matchLen += 0xFF;
                    }
                    matchLen += len & 0xFF;
                }
                matchLen += MIN_MATCH;

                // copying a multiple of 8 bytes can make decompression from 5% to 10% faster
                int fastLen = (int)((matchLen + 7) & 0xFFFFFFF8);
                if (matchDec < matchLen || dOff + fastLen > destEnd)
                {
                    // overlap -> naive incremental copy
                    for (int @ref = dOff - matchDec, end = dOff + matchLen; dOff < end; ++@ref, ++dOff)
                    {
                        dest[dOff] = dest[@ref];
                    }
                }
                else
                {
                    // no overlap -> arraycopy
                    Array.Copy(dest, dOff - matchDec, dest, dOff, fastLen);
                    dOff += matchLen;
                }
            } while (dOff < decompressedLen);

            return dOff;
        }

        private static void EncodeLen(int l, DataOutput @out)
        {
            while (l >= 0xFF)
            {
                @out.WriteByte(unchecked((byte)(sbyte)0xFF));
                l -= 0xFF;
            }
            @out.WriteByte((byte)(sbyte)l);
        }

        private static void EncodeLiterals(byte[] bytes, int token, int anchor, int literalLen, DataOutput @out)
        {
            @out.WriteByte((byte)(sbyte)token);

            // encode literal length
            if (literalLen >= 0x0F)
            {
                EncodeLen(literalLen - 0x0F, @out);
            }

            // encode literals
            @out.WriteBytes(bytes, anchor, literalLen);
        }

        private static void EncodeLastLiterals(byte[] bytes, int anchor, int literalLen, DataOutput @out)
        {
            int token = Math.Min(literalLen, 0x0F) << 4;
            EncodeLiterals(bytes, token, anchor, literalLen, @out);
        }

        private static void EncodeSequence(byte[] bytes, int anchor, int matchRef, int matchOff, int matchLen, DataOutput @out)
        {
            int literalLen = matchOff - anchor;
            Debug.Assert(matchLen >= 4);
            // encode token
            int token = (Math.Min(literalLen, 0x0F) << 4) | Math.Min(matchLen - 4, 0x0F);
            EncodeLiterals(bytes, token, anchor, literalLen, @out);

            // encode match dec
            int matchDec = matchOff - matchRef;
            Debug.Assert(matchDec > 0 && matchDec < 1 << 16);
            @out.WriteByte((byte)(sbyte)matchDec);
            @out.WriteByte((byte)(sbyte)((int)((uint)matchDec >> 8)));

            // encode match len
            if (matchLen >= MIN_MATCH + 0x0F)
            {
                EncodeLen(matchLen - 0x0F - MIN_MATCH, @out);
            }
        }

        public sealed class HashTable
        {
            internal int HashLog;
            internal PackedInts.Mutable hashTable;

            internal void Reset(int len)
            {
                int bitsPerOffset = PackedInts.BitsRequired(len - LAST_LITERALS);
                int bitsPerOffsetLog = 32 - Number.NumberOfLeadingZeros(bitsPerOffset - 1);
                HashLog = MEMORY_USAGE + 3 - bitsPerOffsetLog;
                if (hashTable == null || hashTable.Size() < 1 << HashLog || hashTable.BitsPerValue < bitsPerOffset)
                {
                    hashTable = PackedInts.GetMutable(1 << HashLog, bitsPerOffset, PackedInts.DEFAULT);
                }
                else
                {
                    hashTable.Clear();
                }
            }
        }

        /// <summary>
        /// Compress <code>bytes[off:off+len]</code> into <code>out</code> using
        /// at most 16KB of memory. <code>ht</code> shouldn't be shared across threads
        /// but can safely be reused.
        /// </summary>
        public static void Compress(byte[] bytes, int off, int len, DataOutput @out, HashTable ht)
        {
            int @base = off;
            int end = off + len;

            int anchor = off++;

            if (len > LAST_LITERALS + MIN_MATCH)
            {
                int limit = end - LAST_LITERALS;
                int matchLimit = limit - MIN_MATCH;
                ht.Reset(len);
                int hashLog = ht.HashLog;
                PackedInts.Mutable hashTable = ht.hashTable;

                while (off <= limit)
                {
                    // find a match
                    int @ref;
                    while (true)
                    {
                        if (off >= matchLimit)
                        {
                            goto mainBreak;
                        }
                        int v = ReadInt(bytes, off);
                        int h = Hash(v, hashLog);
                        @ref = @base + (int)hashTable.Get(h);
                        Debug.Assert(PackedInts.BitsRequired(off - @base) <= hashTable.BitsPerValue);
                        hashTable.Set(h, off - @base);
                        if (off - @ref < MAX_DISTANCE && ReadInt(bytes, @ref) == v)
                        {
                            break;
                        }
                        ++off;
                    }

                    // compute match length
                    int matchLen = MIN_MATCH + CommonBytes(bytes, @ref + MIN_MATCH, off + MIN_MATCH, limit);

                    EncodeSequence(bytes, anchor, @ref, off, matchLen, @out);
                    off += matchLen;
                    anchor = off;
                mainContinue: ;
                }
            mainBreak: ;
            }

            // last literals
            int literalLen = end - anchor;
            Debug.Assert(literalLen >= LAST_LITERALS || literalLen == len);
            EncodeLastLiterals(bytes, anchor, end - anchor, @out);
        }

        public class Match
        {
            internal int Start, @ref, Len;

            internal virtual void Fix(int correction)
            {
                Start += correction;
                @ref += correction;
                Len -= correction;
            }

            internal virtual int End()
            {
                return Start + Len;
            }
        }

        private static void CopyTo(Match m1, Match m2)
        {
            m2.Len = m1.Len;
            m2.Start = m1.Start;
            m2.@ref = m1.@ref;
        }

        public sealed class HCHashTable
        {
            internal const int MAX_ATTEMPTS = 256;
            internal static readonly int MASK = MAX_DISTANCE - 1;
            internal int NextToUpdate;
            private int @base;
            private readonly int[] HashTable;
            private readonly short[] ChainTable;

            internal HCHashTable()
            {
                HashTable = new int[HASH_TABLE_SIZE_HC];
                ChainTable = new short[MAX_DISTANCE];
            }

            internal void Reset(int @base)
            {
                this.@base = @base;
                NextToUpdate = @base;
                CollectionsHelper.Fill(HashTable, -1);
                CollectionsHelper.Fill(ChainTable, (short)0);
            }

            private int HashPointer(byte[] bytes, int off)
            {
                int v = ReadInt(bytes, off);
                int h = HashHC(v);
                return HashTable[h];
            }

            private int Next(int off)
            {
                return off - (ChainTable[off & MASK] & 0xFFFF);
            }

            private void AddHash(byte[] bytes, int off)
            {
                int v = ReadInt(bytes, off);
                int h = HashHC(v);
                int delta = off - HashTable[h];
                Debug.Assert(delta > 0, delta.ToString());
                if (delta >= MAX_DISTANCE)
                {
                    delta = MAX_DISTANCE - 1;
                }
                ChainTable[off & MASK] = (short)delta;
                HashTable[h] = off;
            }

            internal void Insert(int off, byte[] bytes)
            {
                for (; NextToUpdate < off; ++NextToUpdate)
                {
                    AddHash(bytes, NextToUpdate);
                }
            }

            internal bool InsertAndFindBestMatch(byte[] buf, int off, int matchLimit, Match match)
            {
                match.Start = off;
                match.Len = 0;
                int delta = 0;
                int repl = 0;

                Insert(off, buf);

                int @ref = HashPointer(buf, off);

                if (@ref >= off - 4 && @ref <= off && @ref >= @base) // potential repetition
                {
                    if (ReadIntEquals(buf, @ref, off)) // confirmed
                    {
                        delta = off - @ref;
                        repl = match.Len = MIN_MATCH + CommonBytes(buf, @ref + MIN_MATCH, off + MIN_MATCH, matchLimit);
                        match.@ref = @ref;
                    }
                    @ref = Next(@ref);
                }

                for (int i = 0; i < MAX_ATTEMPTS; ++i)
                {
                    if (@ref < Math.Max(@base, off - MAX_DISTANCE + 1) || @ref > off)
                    {
                        break;
                    }
                    if (buf[@ref + match.Len] == buf[off + match.Len] && ReadIntEquals(buf, @ref, off))
                    {
                        int matchLen = MIN_MATCH + CommonBytes(buf, @ref + MIN_MATCH, off + MIN_MATCH, matchLimit);
                        if (matchLen > match.Len)
                        {
                            match.@ref = @ref;
                            match.Len = matchLen;
                        }
                    }
                    @ref = Next(@ref);
                }

                if (repl != 0)
                {
                    int ptr = off;
                    int end = off + repl - (MIN_MATCH - 1);
                    while (ptr < end - delta)
                    {
                        ChainTable[ptr & MASK] = (short)delta; // pre load
                        ++ptr;
                    }
                    do
                    {
                        ChainTable[ptr & MASK] = (short)delta;
                        HashTable[HashHC(ReadInt(buf, ptr))] = ptr;
                        ++ptr;
                    } while (ptr < end);
                    NextToUpdate = end;
                }

                return match.Len != 0;
            }

            internal bool InsertAndFindWiderMatch(byte[] buf, int off, int startLimit, int matchLimit, int minLen, Match match)
            {
                match.Len = minLen;

                Insert(off, buf);

                int delta = off - startLimit;
                int @ref = HashPointer(buf, off);
                for (int i = 0; i < MAX_ATTEMPTS; ++i)
                {
                    if (@ref < Math.Max(@base, off - MAX_DISTANCE + 1) || @ref > off)
                    {
                        break;
                    }
                    if (buf[@ref - delta + match.Len] == buf[startLimit + match.Len] && ReadIntEquals(buf, @ref, off))
                    {
                        int matchLenForward = MIN_MATCH + CommonBytes(buf, @ref + MIN_MATCH, off + MIN_MATCH, matchLimit);
                        int matchLenBackward = CommonBytesBackward(buf, @ref, off, @base, startLimit);
                        int matchLen = matchLenBackward + matchLenForward;
                        if (matchLen > match.Len)
                        {
                            match.Len = matchLen;
                            match.@ref = @ref - matchLenBackward;
                            match.Start = off - matchLenBackward;
                        }
                    }
                    @ref = Next(@ref);
                }

                return match.Len > minLen;
            }
        }

        /// <summary>
        /// Compress <code>bytes[off:off+len]</code> into <code>out</code>. Compared to
        /// <seealso cref="LZ4#compress(byte[], int, int, DataOutput, HashTable)"/>, this method
        /// is slower and uses more memory (~ 256KB per thread) but should provide
        /// better compression ratios (especially on large inputs) because it chooses
        /// the best match among up to 256 candidates and then performs trade-offs to
        /// fix overlapping matches. <code>ht</code> shouldn't be shared across threads
        /// but can safely be reused.
        /// </summary>
        public static void CompressHC(byte[] src, int srcOff, int srcLen, DataOutput @out, HCHashTable ht)
        {
            int srcEnd = srcOff + srcLen;
            int matchLimit = srcEnd - LAST_LITERALS;
            int mfLimit = matchLimit - MIN_MATCH;

            int sOff = srcOff;
            int anchor = sOff++;

            ht.Reset(srcOff);
            Match match0 = new Match();
            Match match1 = new Match();
            Match match2 = new Match();
            Match match3 = new Match();

            while (sOff <= mfLimit)
            {
                if (!ht.InsertAndFindBestMatch(src, sOff, matchLimit, match1))
                {
                    ++sOff;
                    continue;
                }

                // saved, in case we would skip too much
                CopyTo(match1, match0);

                while (true)
                {
                    Debug.Assert(match1.Start >= anchor);
                    if (match1.End() >= mfLimit || !ht.InsertAndFindWiderMatch(src, match1.End() - 2, match1.Start + 1, matchLimit, match1.Len, match2))
                    {
                        // no better match
                        EncodeSequence(src, anchor, match1.@ref, match1.Start, match1.Len, @out);
                        anchor = sOff = match1.End();
                        goto mainContinue;
                    }

                    if (match0.Start < match1.Start)
                    {
                        if (match2.Start < match1.Start + match0.Len) // empirical
                        {
                            CopyTo(match0, match1);
                        }
                    }
                    Debug.Assert(match2.Start > match1.Start);

                    if (match2.Start - match1.Start < 3) // First Match too small : removed
                    {
                        CopyTo(match2, match1);
                        goto search2Continue;
                    }

                    while (true)
                    {
                        if (match2.Start - match1.Start < OPTIMAL_ML)
                        {
                            int newMatchLen = match1.Len;
                            if (newMatchLen > OPTIMAL_ML)
                            {
                                newMatchLen = OPTIMAL_ML;
                            }
                            if (match1.Start + newMatchLen > match2.End() - MIN_MATCH)
                            {
                                newMatchLen = match2.Start - match1.Start + match2.Len - MIN_MATCH;
                            }
                            int correction = newMatchLen - (match2.Start - match1.Start);
                            if (correction > 0)
                            {
                                match2.Fix(correction);
                            }
                        }

                        if (match2.Start + match2.Len >= mfLimit || !ht.InsertAndFindWiderMatch(src, match2.End() - 3, match2.Start, matchLimit, match2.Len, match3))
                        {
                            // no better match -> 2 sequences to encode
                            if (match2.Start < match1.End())
                            {
                                match1.Len = match2.Start - match1.Start;
                            }
                            // encode seq 1
                            EncodeSequence(src, anchor, match1.@ref, match1.Start, match1.Len, @out);
                            anchor = sOff = match1.End();
                            // encode seq 2
                            EncodeSequence(src, anchor, match2.@ref, match2.Start, match2.Len, @out);
                            anchor = sOff = match2.End();
                            goto mainContinue;
                        }

                        if (match3.Start < match1.End() + 3) // Not enough space for match 2 : remove it
                        {
                            if (match3.Start >= match1.End()) // // can write Seq1 immediately ==> Seq2 is removed, so Seq3 becomes Seq1
                            {
                                if (match2.Start < match1.End())
                                {
                                    int correction = match1.End() - match2.Start;
                                    match2.Fix(correction);
                                    if (match2.Len < MIN_MATCH)
                                    {
                                        CopyTo(match3, match2);
                                    }
                                }

                                EncodeSequence(src, anchor, match1.@ref, match1.Start, match1.Len, @out);
                                anchor = sOff = match1.End();

                                CopyTo(match3, match1);
                                CopyTo(match2, match0);

                                goto search2Continue;
                            }

                            CopyTo(match3, match2);
                            goto search3Continue;
                        }

                        // OK, now we have 3 ascending matches; let's write at least the first one
                        if (match2.Start < match1.End())
                        {
                            if (match2.Start - match1.Start < 0x0F)
                            {
                                if (match1.Len > OPTIMAL_ML)
                                {
                                    match1.Len = OPTIMAL_ML;
                                }
                                if (match1.End() > match2.End() - MIN_MATCH)
                                {
                                    match1.Len = match2.End() - match1.Start - MIN_MATCH;
                                }
                                int correction = match1.End() - match2.Start;
                                match2.Fix(correction);
                            }
                            else
                            {
                                match1.Len = match2.Start - match1.Start;
                            }
                        }

                        EncodeSequence(src, anchor, match1.@ref, match1.Start, match1.Len, @out);
                        anchor = sOff = match1.End();

                        CopyTo(match2, match1);
                        CopyTo(match3, match2);

                        goto search3Continue;
                    search3Continue: ;
                    }
                search3Break: ;

                search2Continue: ;
                }
            search2Break: ;

            mainContinue: ;
            }
        mainBreak:

            EncodeLastLiterals(src, anchor, srcEnd - anchor, @out);
        }
    }
}