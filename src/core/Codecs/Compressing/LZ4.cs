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

/**
 * LZ4 compression and decompression routines.
 *
 * http://code.google.com/p/lz4/
 * http://fastcompression.blogspot.fr/p/lz4.html
 */

using Lucene.Net.Store;
using Lucene.Net.Util.Packed;
using System;
namespace Lucene.Net.Codecs.Compressing
{

    internal sealed class LZ4
    {

        private LZ4() { }

        static internal int MEMORY_USAGE = 14;
        static internal int MIN_MATCH = 4; // minimum length of a match
        static internal int MAX_DISTANCE = 1 << 16; // maximum distance of a reference
        static internal int LAST_LITERALS = 5; // the last 5 bytes must be encoded as literals
        static internal int HASH_LOG_HC = 15; // log size of the dictionary for compressHC
        static internal int HASH_TABLE_SIZE_HC = 1 << HASH_LOG_HC;
        static internal int OPTIMAL_ML = 0x0F + 4 - 1; // match length that doesn't require an additional byte


        private static int Hash(int i, int hashBits)
        {
            return Lucene.Net.Support.Number.URShift((i * -1640531535), (32 - hashBits));
        }

        private static int HashHC(int i)
        {
            return Hash(i, HASH_LOG_HC);
        }

        private static int ReadInt(byte[] buf, int i)
        {
            return ((buf[i] & 0xFF) << 24) | ((buf[i + 1] & 0xFF) << 16) | ((buf[i + 2] & 0xFF) << 8) | (buf[i + 3] & 0xFF);
        }

        private static bool ReadIntEquals(byte[] buf, int i, int j)
        {
            return ReadInt(buf, i) == ReadInt(buf, j);
        }

        private static int CommonBytes(byte[] b, int o1, int o2, int limit)
        {
            //assert o1 < o2;
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

        /**
         * Decompress at least <code>decompressedLen</code> bytes into
         * <code>dest[dOff:]</code>. Please note that <code>dest</code> must be large
         * enough to be able to hold <b>all</b> decompressed data (meaning that you
         * need to know the total decompressed length).
         */
        public static int Decompress(DataInput compressed, int decompressedLen, byte[] dest, int dOff)
        {
            int destEnd = dest.Length;

            do
            {
                // literals
                int token = compressed.ReadByte() & 0xFF;
                //hackmp - this was an usigned shift operator '>>>'..i've gone with '>>' as the int being
                //referenced is not unsigned.
                int literalLen = Lucene.Net.Support.Number.URShift(token, 4);

                if (literalLen != 0)
                {
                    if (literalLen == 0x0F)
                    {
                        byte len;
                        while ((len = compressed.ReadByte()) == (byte)0xFF)
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
                int matchDec = (compressed.ReadByte() & 0xFF) | ((compressed.ReadByte() & 0xFF) << 8);
                //assert matchDec > 0;

                int matchLen = token & 0x0F;
                if (matchLen == 0x0F)
                {
                    int len;
                    while ((len = compressed.ReadByte()) == (byte)0xFF)
                    {
                        matchLen += 0xFF;
                    }
                    matchLen += len & 0xFF;
                }
                matchLen += MIN_MATCH;

                // copying a multiple of 8 bytes can make decompression from 5% to 10% faster
                long fastLen = (matchLen + 7) & 0xFFFFFFF8;
                if (matchDec < matchLen || dOff + fastLen > destEnd)
                {
                    // overlap -> naive incremental copy
                    for (int r = dOff - matchDec, end = dOff + matchLen; dOff < end; ++r, ++dOff)
                    {
                        dest[dOff] = dest[r];
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

        private static void EncodeLen(int l, DataOutput output)
        {
            while (l >= 0xFF)
            {
                output.WriteByte((byte)0xFF);
                l -= 0xFF;
            }

            output.WriteByte((byte)l);
        }

        private static void EncodeLiterals(byte[] bytes, int token, int anchor, int literalLen, DataOutput output)
        {
            output.WriteByte((byte)token);

            // encode literal length
            if (literalLen >= 0x0F)
            {
                EncodeLen(literalLen - 0x0F, output);
            }

            // encode literals
            output.WriteBytes(bytes, anchor, literalLen);
        }

        private static void EncodeLastLiterals(byte[] bytes, int anchor, int literalLen, DataOutput output)
        {
            int token = Math.Min(literalLen, 0x0F) << 4;
            EncodeLiterals(bytes, token, anchor, literalLen, output);
        }

        private static void EncodeSequence(byte[] bytes, int anchor, int matchRef, int matchOff, int matchLen, DataOutput output)
        {
            int literalLen = matchOff - anchor;
            //assert matchLen >= 4;
            // encode token
            int token = (Math.Min(literalLen, 0x0F) << 4) | Math.Min(matchLen - 4, 0x0F);
            EncodeLiterals(bytes, token, anchor, literalLen, output);

            // encode match dec
            int matchDec = matchOff - matchRef;
            //assert matchDec > 0 && matchDec < 1 << 16;
            output.WriteByte((byte)matchDec);
            //hackmp - was an unsigned shift '>>>'..
            output.WriteByte((byte)Lucene.Net.Support.Number.URShift(matchDec, 8));

            // encode match len
            if (matchLen >= MIN_MATCH + 0x0F)
            {
                EncodeLen(matchLen - 0x0F - MIN_MATCH, output);
            }
        }

        internal sealed class HashTable
        {
            internal int hashLog;
            internal PackedInts.Mutable hashTable;

            void Reset(int len)
            {
                int bitsPerOffset = PackedInts.bitsRequired(len - LAST_LITERALS);
                int bitsPerOffsetLog = 32 - Lucene.Net.Support.Number.NumberOfLeadingZeros(bitsPerOffset - 1);
                hashLog = MEMORY_USAGE + 3 - bitsPerOffsetLog;

                if (hashTable == null || hashTable.size() < 1 << hashLog || hashTable.getBitsPerValue() < bitsPerOffset)
                {
                    hashTable = PackedInts.getMutable(1 << hashLog, bitsPerOffset, PackedInts.DEFAULT);
                }
                else
                {
                    hashTable.clear();
                }
            }

        }

        /**
         * Compress <code>bytes[off:off+len]</code> into <code>out</code> using
         * at most 16KB of memory. <code>ht</code> shouldn't be shared across threads
         * but can safely be reused.
         */
        public static void Compress(byte[] bytes, int off, int len, DataOutput output, HashTable ht) 
      {
		int bse = off;
		int end = off + len;

		int anchor = off++;

		if (len > LAST_LITERALS + MIN_MATCH) {

		  int limit = end - LAST_LITERALS;
		  int matchLimit = limit - MIN_MATCH;
		  ht.Reset(len);
		  int hashLog = ht.hashLog;
		  PackedInts.Mutable hashTable = ht.hashTable;

		  main:
		  while (off < limit) 
          {
			// find a match
			int r;
			while (true) 
            {
			  if (off >= matchLimit) 
              {
				break main;
			  }

			  int v = ReadInt(bytes, off);
			  int h = Hash(v, hashLog);
			  r = bse + (int) hashTable.get(h);
			  //assert PackedInts.bitsRequired(off - bse) <= hashTable.getBitsPerValue();
			  hashTable.set(h, off - bse);
			  if (off - r < MAX_DISTANCE && ReadInt(bytes, r) == v) {
				break;
			  }
			  ++off;
			}

			// compute match length
			int matchLen = MIN_MATCH + CommonBytes(bytes, r + MIN_MATCH, off + MIN_MATCH, limit);

			EncodeSequence(bytes, anchor, r, off, matchLen, output);
			off += matchLen;
			anchor = off;
		  }
		}

		// last literals
		int literalLen = end - anchor;
		//assert literalLen >= LAST_LITERALS || literalLen == len;
		EncodeLastLiterals(bytes, anchor, end - anchor, output);
	  }

        internal class Match
        {
            internal int start, r, len;

            internal void fix(int correction)
            {
                start += correction;
                r += correction;
                len -= correction;
            }

            internal int end()
            {
                return start + len;
            }
        }

        private static void CopyTo(Match m1, Match m2)
        {
            m2.len = m1.len;
            m2.start = m1.start;
            m2.r = m1.r;
        }

        internal sealed class HCHashTable
        {
            static int MAX_ATTEMPTS = 256;
            static int MASK = MAX_DISTANCE - 1;
            int nextToUpdate;
            private int bse;
            private int[] hashTable;
            private short[] chainTable;

            HCHashTable()
            {
                hashTable = new int[HASH_TABLE_SIZE_HC];
                chainTable = new short[MAX_DISTANCE];
            }

            internal void Reset(int bse)
            {
                this.bse = bse;
                nextToUpdate = bse;
                Lucene.Net.Support.Arrays.Fill(hashTable, -1);
                Lucene.Net.Support.Arrays.Fill(chainTable, (short)0);
            }

            internal int HashPointer(byte[] bytes, int off)
            {
                int v = ReadInt(bytes, off);
                int h = HashHC(v);
                return bse + hashTable[h];
            }

            internal int Next(int off)
            {
                return bse + off - (chainTable[off & MASK] & 0xFFFF);
            }

            internal void AddHash(byte[] bytes, int off)
            {
                int v = ReadInt(bytes, off);
                int h = HashHC(v);
                int delta = off - hashTable[h];
                if (delta >= MAX_DISTANCE)
                {
                    delta = MAX_DISTANCE - 1;
                }
                chainTable[off & MASK] = (short)delta;
                hashTable[h] = off - bse;
            }

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

                Insert(off, buf);

                int r = HashPointer(buf, off);
                for (int i = 0; i < MAX_ATTEMPTS; ++i)
                {
                    if (r < Math.Max(bse, off - MAX_DISTANCE + 1))
                    {
                        break;
                    }
                    if (buf[r + match.len] == buf[off + match.len] && ReadIntEquals(buf, r, off))
                    {
                        int matchLen = MIN_MATCH + CommonBytes(buf, r + MIN_MATCH, off + MIN_MATCH, matchLimit);
                        if (matchLen > match.len)
                        {
                            match.r = r;
                            match.len = matchLen;
                        }
                    }
                    r = Next(r);
                }

                return match.len != 0;
            }

            internal bool InsertAndFindWiderMatch(byte[] buf, int off, int startLimit, int matchLimit, int minLen, Match match)
            {
                match.len = minLen;

                Insert(off, buf);

                int delta = off - startLimit;
                int r = HashPointer(buf, off);
                for (int i = 0; i < MAX_ATTEMPTS; ++i)
                {
                    if (r < Math.Max(bse, off - MAX_DISTANCE + 1))
                    {
                        break;
                    }
                    if (buf[r - delta + match.len] == buf[startLimit + match.len]
                        && ReadIntEquals(buf, r, off))
                    {
                        int matchLenForward = MIN_MATCH + CommonBytes(buf, r + MIN_MATCH, off + MIN_MATCH, matchLimit);
                        int matchLenBackward = CommonBytesBackward(buf, r, off, bse, startLimit);
                        int matchLen = matchLenBackward + matchLenForward;
                        if (matchLen > match.len)
                        {
                            match.len = matchLen;
                            match.r = r - matchLenBackward;
                            match.start = off - matchLenBackward;
                        }
                    }
                    r = Next(r);
                }

                return match.len > minLen;
            }

        }

        /**
         * Compress <code>bytes[off:off+len]</code> into <code>out</code>. Compared to
         * {@link LZ4#compress(byte[], int, int, DataOutput, HashTable)}, this method
         * is slower and uses more memory (~ 256KB per thread) but should provide
         * better compression ratios (especially on large inputs) because it chooses
         * the best match among up to 256 candidates and then performs trade-offs to
         * fix overlapping matches. <code>ht</code> shouldn't be shared across threads
         * but can safely be reused.
         */
        public static void CompressHC(byte[] src, int srcOff, int srcLen, DataOutput output, HCHashTable ht)
	  {

		int srcEnd = srcOff + srcLen;
		int matchLimit = srcEnd - LAST_LITERALS;

		int sOff = srcOff;
		int anchor = sOff++;

		ht.Reset(srcOff);
		Match match0 = new Match();
		Match match1 = new Match();
		Match match2 = new Match();
		Match match3 = new Match();

		main:
		while (sOff < matchLimit) 
        {
		  if (!ht.InsertAndFindBestMatch(src, sOff, matchLimit, match1)) {
			++sOff;
			continue;
		  }

		  // saved, in case we would skip too much
		  CopyTo(match1, match0);

		  search2:
		  while (true) {
			//assert match1.start >= anchor;
			if (match1.end() >= matchLimit
				|| !ht.InsertAndFindWiderMatch(src, match1.end() - 2, match1.start + 1, matchLimit, match1.len, match2)) {
			  // no better match
			  EncodeSequence(src, anchor, match1.r, match1.start, match1.len, output);
			  anchor = sOff = match1.end();
			  goto main;
			}

			if (match0.start < match1.start) {
			  if (match2.start < match1.start + match0.len) { // empirical
				CopyTo(match0, match1);
			  }
			}
			//assert match2.start > match1.start;

			if (match2.start - match1.start < 3) { // First Match too small : removed
			  CopyTo(match2, match1);
			  goto search2;
			}

			search3:
			while (true) {
			  if (match2.start - match1.start < OPTIMAL_ML) {
				int newMatchLen = match1.len;
				if (newMatchLen > OPTIMAL_ML) {
				  newMatchLen = OPTIMAL_ML;
				}
				if (match1.start + newMatchLen > match2.end() - MIN_MATCH) {
				  newMatchLen = match2.start - match1.start + match2.len - MIN_MATCH;
				}
				int correction = newMatchLen - (match2.start - match1.start);
				if (correction > 0) {
				  match2.fix(correction);
				}
			  }

			  if (match2.start + match2.len >= matchLimit
				  || !ht.InsertAndFindWiderMatch(src, match2.end() - 3, match2.start, matchLimit, match2.len, match3)) {
				// no better match -> 2 sequences to encode
				if (match2.start < match1.end()) {
				  if (match2.start - match1.start < OPTIMAL_ML) {
					if (match1.len > OPTIMAL_ML) {
					  match1.len = OPTIMAL_ML;
					}
					if (match1.end() > match2.end() - MIN_MATCH) {
					  match1.len = match2.end() - match1.start - MIN_MATCH;
					}
					int correction = match1.len - (match2.start - match1.start);
					if (correction > 0) {
					  match2.fix(correction);
					}
				  } else {
					match1.len = match2.start - match1.start;
				  }
				}
				// encode seq 1
				EncodeSequence(src, anchor, match1.r, match1.start, match1.len, output);
				anchor = sOff = match1.end();
				// encode seq 2
				EncodeSequence(src, anchor, match2.r, match2.start, match2.len, output);
				anchor = sOff = match2.end();
				goto main;
			  }

			  if (match3.start < match1.end() + 3) { // Not enough space for match 2 : remove it
				if (match3.start >= match1.end()) { // // can write Seq1 immediately ==> Seq2 is removed, so Seq3 becomes Seq1
				  if (match2.start < match1.end()) {
					int correction = match1.end() - match2.start;
					match2.fix(correction);
					if (match2.len < MIN_MATCH) {
					  CopyTo(match3, match2);
					}
				  }

				  EncodeSequence(src, anchor, match1.r, match1.start, match1.len, output);
				  anchor = sOff = match1.end();

				  CopyTo(match3, match1);
				  CopyTo(match2, match0);

				  goto search2;
				}

				CopyTo(match3, match2);
				goto search3;
			  }

			  // OK, now we have 3 ascending matches; let's write at least the first one
			  if (match2.start < match1.end()) {
				if (match2.start - match1.start < 0x0F) {
				  if (match1.len > OPTIMAL_ML) {
					match1.len = OPTIMAL_ML;
				  }
				  if (match1.end() > match2.end() - MIN_MATCH) {
					match1.len = match2.end() - match1.start - MIN_MATCH;
				  }
				  int correction = match1.end() - match2.start;
				  match2.fix(correction);
				} else {
				  match1.len = match2.start - match1.start;
				}
			  }

			  EncodeSequence(src, anchor, match1.r, match1.start, match1.len, output);
			  anchor = sOff = match1.end();

			  CopyTo(match2, match1);
			  CopyTo(match3, match2);

			  goto search3;
			}

		  }

		}

		EncodeLastLiterals(src, anchor, srcEnd - anchor, output);
	  }
    }
}