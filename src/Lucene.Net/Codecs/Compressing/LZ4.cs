using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Codecs.Compressing
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

    using DataInput = Lucene.Net.Store.DataInput;
    using DataOutput = Lucene.Net.Store.DataOutput;
    using PackedInt32s = Lucene.Net.Util.Packed.PackedInt32s;

    /// <summary>
    /// LZ4 compression and decompression routines.
    /// <para/>
    /// http://code.google.com/p/lz4/
    /// http://fastcompression.blogspot.fr/p/lz4.html
    /// </summary>
    public static class LZ4 // LUCENENET specific - made static
    {
        internal const int MEMORY_USAGE = 14;
        internal const int MIN_MATCH = 4; // minimum length of a match
        internal const int MAX_DISTANCE = 1 << 16; // maximum distance of a reference
        internal const int LAST_LITERALS = 5; // the last 5 bytes must be encoded as literals
        internal const int HASH_LOG_HC = 15; // log size of the dictionary for compressHC
        internal const int HASH_TABLE_SIZE_HC = 1 << HASH_LOG_HC;
        internal const int OPTIMAL_ML = 0x0F + 4 - 1; // match length that doesn't require an additional byte

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Hash(int i, int hashBits)
        {
            return (i * -1640531535).TripleShift(32 - hashBits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int HashHC(int i)
        {
            return Hash(i, HASH_LOG_HC);
        }

        /// <summary>
        /// NOTE: This was readInt() in Lucene.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ReadInt32(byte[] buf, int i)
        {
            return ((((sbyte)buf[i]) & 0xFF) << 24) | ((((sbyte)buf[i + 1]) & 0xFF) << 16) | ((((sbyte)buf[i + 2]) & 0xFF) << 8) |
                (((sbyte)buf[i + 3]) & 0xFF);
        }

        /// <summary>
        /// NOTE: This was readIntEquals() in Lucene.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ReadInt32Equals(byte[] buf, int i, int j)
        {
            return ReadInt32(buf, i) == ReadInt32(buf, j);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CommonBytes(byte[] b, int o1, int o2, int limit)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(o1 < o2);
            int count = 0;
            while (o2 < limit && b[o1++] == b[o2++])
            {
                ++count;
            }
            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        /// Decompress at least <paramref name="decompressedLen"/> bytes into
        /// <c>dest[dOff]</c>. Please note that <paramref name="dest"/> must be large
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
                int literalLen = token.TripleShift(4);

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
                if (Debugging.AssertsEnabled) Debugging.Assert(matchDec > 0);

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
                    Arrays.Copy(dest, dOff - matchDec, dest, dOff, fastLen);
                    dOff += matchLen;
                }
            } while (dOff < decompressedLen);

            return dOff;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EncodeLen(int l, DataOutput @out)
        {
            while (l >= 0xFF)
            {
                @out.WriteByte(/*(byte)*/0xFF); // LUCENENET: Removed unnecessary cast
                l -= 0xFF;
            }
            @out.WriteByte((byte)l);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EncodeLiterals(byte[] bytes, int token, int anchor, int literalLen, DataOutput @out)
        {
            @out.WriteByte((byte)token);

            // encode literal length
            if (literalLen >= 0x0F)
            {
                EncodeLen(literalLen - 0x0F, @out);
            }

            // encode literals
            @out.WriteBytes(bytes, anchor, literalLen);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EncodeLastLiterals(byte[] bytes, int anchor, int literalLen, DataOutput @out)
        {
            int token = Math.Min(literalLen, 0x0F) << 4;
            EncodeLiterals(bytes, token, anchor, literalLen, @out);
        }

        private static void EncodeSequence(byte[] bytes, int anchor, int matchRef, int matchOff, int matchLen, DataOutput @out)
        {
            int literalLen = matchOff - anchor;
            if (Debugging.AssertsEnabled) Debugging.Assert(matchLen >= 4);
            // encode token
            int token = (Math.Min(literalLen, 0x0F) << 4) | Math.Min(matchLen - 4, 0x0F);
            EncodeLiterals(bytes, token, anchor, literalLen, @out);

            // encode match dec
            int matchDec = matchOff - matchRef;
            if (Debugging.AssertsEnabled) Debugging.Assert(matchDec > 0 && matchDec < 1 << 16);
            @out.WriteByte((byte)matchDec);
            @out.WriteByte((byte)matchDec.TripleShift(8));

            // encode match len
            if (matchLen >= MIN_MATCH + 0x0F)
            {
                EncodeLen(matchLen - 0x0F - MIN_MATCH, @out);
            }
        }

        public sealed class HashTable
        {
            internal int hashLog;
            internal PackedInt32s.Mutable hashTable;

            internal void Reset(int len)
            {
                int bitsPerOffset = PackedInt32s.BitsRequired(len - LAST_LITERALS);
                int bitsPerOffsetLog = 32 - (bitsPerOffset - 1).LeadingZeroCount();
                hashLog = MEMORY_USAGE + 3 - bitsPerOffsetLog;
                if (hashTable is null || hashTable.Count < 1 << hashLog || hashTable.BitsPerValue < bitsPerOffset)
                {
                    hashTable = PackedInt32s.GetMutable(1 << hashLog, bitsPerOffset, PackedInt32s.DEFAULT);
                }
                else
                {
                    hashTable.Clear();
                }
            }
        }

        /// <summary>
        /// Compress <c>bytes[off:off+len]</c> into <paramref name="out"/> using
        /// at most 16KB of memory. <paramref name="ht"/> shouldn't be shared across threads
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
                int hashLog = ht.hashLog;
                PackedInt32s.Mutable hashTable = ht.hashTable;

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
                        int v = ReadInt32(bytes, off);
                        int h = Hash(v, hashLog);
                        @ref = @base + (int)hashTable.Get(h);
                        if (Debugging.AssertsEnabled) Debugging.Assert(PackedInt32s.BitsRequired(off - @base) <= hashTable.BitsPerValue);
                        hashTable.Set(h, off - @base);
                        if (off - @ref < MAX_DISTANCE && ReadInt32(bytes, @ref) == v)
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
                //mainContinue: ; // LUCENENET NOTE: Not Referenced
                }
            mainBreak: {/* LUCENENET: intentionally blank */}
            }

            // last literals
            int literalLen = end - anchor;
            if (Debugging.AssertsEnabled) Debugging.Assert(literalLen >= LAST_LITERALS || literalLen == len);
            EncodeLastLiterals(bytes, anchor, end - anchor, @out);
        }

        public class Match
        {
            internal int start, @ref, len;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal virtual void Fix(int correction)
            {
                start += correction;
                @ref += correction;
                len -= correction;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal virtual int End()
            {
                return start + len;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CopyTo(Match m1, Match m2)
        {
            m2.len = m1.len;
            m2.start = m1.start;
            m2.@ref = m1.@ref;
        }

        public sealed class HCHashTable
        {
            internal const int MAX_ATTEMPTS = 256;
            internal const int MASK = MAX_DISTANCE - 1;
            internal int nextToUpdate;
            private int @base;
            private readonly int[] hashTable;
            private readonly short[] chainTable;

            internal HCHashTable()
            {
                hashTable = new int[HASH_TABLE_SIZE_HC];
                chainTable = new short[MAX_DISTANCE];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void Reset(int @base)
            {
                this.@base = @base;
                nextToUpdate = @base;
                Arrays.Fill(hashTable, -1);
                Arrays.Fill(chainTable, (short)0);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private int HashPointer(byte[] bytes, int off)
            {
                int v = ReadInt32(bytes, off);
                int h = HashHC(v);
                return hashTable[h];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private int Next(int off)
            {
                return off - (chainTable[off & MASK] & 0xFFFF);
            }

            private void AddHash(byte[] bytes, int off)
            {
                int v = ReadInt32(bytes, off);
                int h = HashHC(v);
                int delta = off - hashTable[h];
                if (Debugging.AssertsEnabled) Debugging.Assert(delta > 0, delta.ToString());
                if (delta >= MAX_DISTANCE)
                {
                    delta = MAX_DISTANCE - 1;
                }
                chainTable[off & MASK] = (short)delta;
                hashTable[h] = off;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void Insert(int off, byte[] bytes)
            {
                for (; nextToUpdate < off; ++nextToUpdate)
                {
                    AddHash(bytes, nextToUpdate);
                }
            }

            internal bool InsertAndFindBestMatch(byte[] buf, int off, int matchLimit, Match match)
            {
                match.start = off;
                match.len = 0;
                int delta = 0;
                int repl = 0;

                Insert(off, buf);

                int @ref = HashPointer(buf, off);

                if (@ref >= off - 4 && @ref <= off && @ref >= @base) // potential repetition
                {
                    if (ReadInt32Equals(buf, @ref, off)) // confirmed
                    {
                        delta = off - @ref;
                        repl = match.len = MIN_MATCH + CommonBytes(buf, @ref + MIN_MATCH, off + MIN_MATCH, matchLimit);
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
                    if (buf[@ref + match.len] == buf[off + match.len] && ReadInt32Equals(buf, @ref, off))
                    {
                        int matchLen = MIN_MATCH + CommonBytes(buf, @ref + MIN_MATCH, off + MIN_MATCH, matchLimit);
                        if (matchLen > match.len)
                        {
                            match.@ref = @ref;
                            match.len = matchLen;
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
                        chainTable[ptr & MASK] = (short)delta; // pre load
                        ++ptr;
                    }
                    do
                    {
                        chainTable[ptr & MASK] = (short)delta;
                        hashTable[HashHC(ReadInt32(buf, ptr))] = ptr;
                        ++ptr;
                    } while (ptr < end);
                    nextToUpdate = end;
                }

                return match.len != 0;
            }

            internal bool InsertAndFindWiderMatch(byte[] buf, int off, int startLimit, int matchLimit, int minLen, Match match)
            {
                match.len = minLen;

                Insert(off, buf);

                int delta = off - startLimit;
                int @ref = HashPointer(buf, off);
                for (int i = 0; i < MAX_ATTEMPTS; ++i)
                {
                    if (@ref < Math.Max(@base, off - MAX_DISTANCE + 1) || @ref > off)
                    {
                        break;
                    }
                    if (buf[@ref - delta + match.len] == buf[startLimit + match.len] && ReadInt32Equals(buf, @ref, off))
                    {
                        int matchLenForward = MIN_MATCH + CommonBytes(buf, @ref + MIN_MATCH, off + MIN_MATCH, matchLimit);
                        int matchLenBackward = CommonBytesBackward(buf, @ref, off, @base, startLimit);
                        int matchLen = matchLenBackward + matchLenForward;
                        if (matchLen > match.len)
                        {
                            match.len = matchLen;
                            match.@ref = @ref - matchLenBackward;
                            match.start = off - matchLenBackward;
                        }
                    }
                    @ref = Next(@ref);
                }

                return match.len > minLen;
            }
        }

        /// <summary>
        /// Compress <c>bytes[off:off+len]</c> into <paramref name="out"/>. Compared to
        /// <see cref="LZ4.Compress(byte[], int, int, DataOutput, HashTable)"/>, this method
        /// is slower and uses more memory (~ 256KB per thread) but should provide
        /// better compression ratios (especially on large inputs) because it chooses
        /// the best match among up to 256 candidates and then performs trade-offs to
        /// fix overlapping matches. <paramref name="ht"/> shouldn't be shared across threads
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
                    if (Debugging.AssertsEnabled) Debugging.Assert(match1.start >= anchor);
                    if (match1.End() >= mfLimit || !ht.InsertAndFindWiderMatch(src, match1.End() - 2, match1.start + 1, matchLimit, match1.len, match2))
                    {
                        // no better match
                        EncodeSequence(src, anchor, match1.@ref, match1.start, match1.len, @out);
                        anchor = sOff = match1.End();
                        goto mainContinue;
                    }

                    if (match0.start < match1.start)
                    {
                        if (match2.start < match1.start + match0.len) // empirical
                        {
                            CopyTo(match0, match1);
                        }
                    }
                    if (Debugging.AssertsEnabled) Debugging.Assert(match2.start > match1.start);

                    if (match2.start - match1.start < 3) // First Match too small : removed
                    {
                        CopyTo(match2, match1);
                        goto search2Continue;
                    }

                    while (true)
                    {
                        if (match2.start - match1.start < OPTIMAL_ML)
                        {
                            int newMatchLen = match1.len;
                            if (newMatchLen > OPTIMAL_ML)
                            {
                                newMatchLen = OPTIMAL_ML;
                            }
                            if (match1.start + newMatchLen > match2.End() - MIN_MATCH)
                            {
                                newMatchLen = match2.start - match1.start + match2.len - MIN_MATCH;
                            }
                            int correction = newMatchLen - (match2.start - match1.start);
                            if (correction > 0)
                            {
                                match2.Fix(correction);
                            }
                        }

                        if (match2.start + match2.len >= mfLimit || !ht.InsertAndFindWiderMatch(src, match2.End() - 3, match2.start, matchLimit, match2.len, match3))
                        {
                            // no better match -> 2 sequences to encode
                            if (match2.start < match1.End())
                            {
                                match1.len = match2.start - match1.start;
                            }
                            // encode seq 1
                            EncodeSequence(src, anchor, match1.@ref, match1.start, match1.len, @out);
                            anchor = /*sOff =*/ match1.End(); // LUCENENET: IDE0059: Remove unnecessary value assignment
                            // encode seq 2
                            EncodeSequence(src, anchor, match2.@ref, match2.start, match2.len, @out);
                            anchor = sOff = match2.End();
                            goto mainContinue;
                        }

                        if (match3.start < match1.End() + 3) // Not enough space for match 2 : remove it
                        {
                            if (match3.start >= match1.End()) // // can write Seq1 immediately ==> Seq2 is removed, so Seq3 becomes Seq1
                            {
                                if (match2.start < match1.End())
                                {
                                    int correction = match1.End() - match2.start;
                                    match2.Fix(correction);
                                    if (match2.len < MIN_MATCH)
                                    {
                                        CopyTo(match3, match2);
                                    }
                                }

                                EncodeSequence(src, anchor, match1.@ref, match1.start, match1.len, @out);
                                anchor = /*sOff =*/ match1.End(); // LUCENENET: IDE0059: Remove unnecessary value assignment

                                CopyTo(match3, match1);
                                CopyTo(match2, match0);

                                goto search2Continue;
                            }

                            CopyTo(match3, match2);
                            goto search3Continue;
                        }

                        // OK, now we have 3 ascending matches; let's write at least the first one
                        if (match2.start < match1.End())
                        {
                            if (match2.start - match1.start < 0x0F)
                            {
                                if (match1.len > OPTIMAL_ML)
                                {
                                    match1.len = OPTIMAL_ML;
                                }
                                if (match1.End() > match2.End() - MIN_MATCH)
                                {
                                    match1.len = match2.End() - match1.start - MIN_MATCH;
                                }
                                int correction = match1.End() - match2.start;
                                match2.Fix(correction);
                            }
                            else
                            {
                                match1.len = match2.start - match1.start;
                            }
                        }

                        EncodeSequence(src, anchor, match1.@ref, match1.start, match1.len, @out);
                        anchor = /*sOff =*/ match1.End(); // LUCENENET: IDE0059: Remove unnecessary value assignment

                        CopyTo(match2, match1);
                        CopyTo(match3, match2);

                        // goto search3Continue; // LUCENENET: Removed redundant jump statements. https://rules.sonarsource.com/csharp/RSPEC-3626
                    search3Continue: {/* LUCENENET: intentionally blank */}
                    }
                //search3Break: ; // LUCENENET NOTE: Unreachable

                search2Continue: {/* LUCENENET: intentionally blank */}
                }
            //search2Break: ; // LUCENENET NOTE: Not referenced

            mainContinue: {/* LUCENENET: intentionally blank */}
            }
        //mainBreak: // LUCENENET NOTE: Not referenced

            EncodeLastLiterals(src, anchor, srcEnd - anchor, @out);
        }
    }
}