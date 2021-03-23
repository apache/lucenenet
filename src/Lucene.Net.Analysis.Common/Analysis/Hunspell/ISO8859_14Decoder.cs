// Lucene version compatibility level 4.10.4
using Lucene.Net.Support;
using System;
using System.Text;

namespace Lucene.Net.Analysis.Hunspell
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

    // LUCENENET NOTE: This class was refactored from its Java counterpart.

    // many hunspell dictionaries use this encoding, yet java/.NET does not have it?!?!
    [ExceptionToClassNameConvention]
    internal sealed class ISO8859_14Encoding : Encoding
    {
        private static readonly Decoder decoder = new ISO8859_14Decoder();
        public override Decoder GetDecoder()
        {
            return new ISO8859_14Decoder();
        }

        public override string EncodingName => "iso-8859-14";

        public override int CodePage => 28604;

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return decoder.GetCharCount(bytes, index, count);
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            return decoder.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
        }

        public override int GetMaxCharCount(int byteCount)
        {
            return byteCount;
        }


        #region Encoding Not Implemented
        public override int GetByteCount(char[] chars, int index, int count)
        {
            throw new NotImplementedException();
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            throw new NotImplementedException();
        }

        public override int GetMaxByteCount(int charCount)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    [ExceptionToClassNameConvention]
    internal sealed class ISO8859_14Decoder : Decoder
    {
        internal static readonly char[] TABLE = new char[]
        {
            (char)0x00A0, (char)0x1E02, (char)0x1E03, (char)0x00A3, (char)0x010A, (char)0x010B, (char)0x1E0A, (char)0x00A7,
            (char)0x1E80, (char)0x00A9, (char)0x1E82, (char)0x1E0B, (char)0x1EF2, (char)0x00AD, (char)0x00AE, (char)0x0178,
            (char)0x1E1E, (char)0x1E1F, (char)0x0120, (char)0x0121, (char)0x1E40, (char)0x1E41, (char)0x00B6, (char)0x1E56,
            (char)0x1E81, (char)0x1E57, (char)0x1E83, (char)0x1E60, (char)0x1EF3, (char)0x1E84, (char)0x1E85, (char)0x1E61,
            (char)0x00C0, (char)0x00C1, (char)0x00C2, (char)0x00C3, (char)0x00C4, (char)0x00C5, (char)0x00C6, (char)0x00C7,
            (char)0x00C8, (char)0x00C9, (char)0x00CA, (char)0x00CB, (char)0x00CC, (char)0x00CD, (char)0x00CE, (char)0x00CF,
            (char)0x0174, (char)0x00D1, (char)0x00D2, (char)0x00D3, (char)0x00D4, (char)0x00D5, (char)0x00D6, (char)0x1E6A,
            (char)0x00D8, (char)0x00D9, (char)0x00DA, (char)0x00DB, (char)0x00DC, (char)0x00DD, (char)0x0176, (char)0x00DF,
            (char)0x00E0, (char)0x00E1, (char)0x00E2, (char)0x00E3, (char)0x00E4, (char)0x00E5, (char)0x00E6, (char)0x00E7,
            (char)0x00E8, (char)0x00E9, (char)0x00EA, (char)0x00EB, (char)0x00EC, (char)0x00ED, (char)0x00EE, (char)0x00EF,
            (char)0x0175, (char)0x00F1, (char)0x00F2, (char)0x00F3, (char)0x00F4, (char)0x00F5, (char)0x00F6, (char)0x1E6B,
            (char)0x00F8, (char)0x00F9, (char)0x00FA, (char)0x00FB, (char)0x00FC, (char)0x00FD, (char)0x0177, (char)0x00FF
        };

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return count;
        }

        public override int GetChars(byte[] bytesIn, int byteIndex, int byteCount, char[] charsOut, int charIndex)
        {
            int writeCount = 0;
            int charPointer = charIndex;

            for (int i = byteIndex; i < (byteIndex + byteCount); i++)
            {
                // Decode the value
                char ch = (char)(bytesIn[i] & 0xff);
                if (ch >= 0xA0)
                {
                    ch = TABLE[ch - 0xA0];
                }
                // write the value to the correct buffer slot
                charsOut[charPointer] = ch;
                writeCount++;
                charPointer++;
            }

            return writeCount;
        }
    }
}