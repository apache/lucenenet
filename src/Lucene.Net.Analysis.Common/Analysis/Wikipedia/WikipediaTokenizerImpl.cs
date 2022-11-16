// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Support;
using System;
using System.IO;
using System.Text;

namespace Lucene.Net.Analysis.Wikipedia
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

    /// <summary>
    /// JFlex-generated tokenizer that is aware of Wikipedia syntax.
    /// </summary>
    internal class WikipediaTokenizerImpl
    {
        /// <summary>This character denotes the end of file</summary>
        public static readonly int YYEOF = -1;

        /// <summary>initial size of the lookahead buffer</summary>
        private static readonly int ZZ_BUFFERSIZE = 4096;

        /// <summary>lexical states</summary>
        public static readonly int YYINITIAL = 0;
        public static readonly int CATEGORY_STATE = 2;
        public static readonly int INTERNAL_LINK_STATE = 4;
        public static readonly int EXTERNAL_LINK_STATE = 6;
        public static readonly int TWO_SINGLE_QUOTES_STATE = 8;
        public static readonly int THREE_SINGLE_QUOTES_STATE = 10;
        public static readonly int FIVE_SINGLE_QUOTES_STATE = 12;
        public static readonly int DOUBLE_EQUALS_STATE = 14;
        public static readonly int DOUBLE_BRACE_STATE = 16;
        public static readonly int STRING = 18;

        /// <summary>
        /// ZZ_LEXSTATE[l] is the state in the DFA for the lexical state l
        /// ZZ_LEXSTATE[l+1] is the state in the DFA for the lexical state l
        ///     at the beginning of a line
        /// l is of the form l = 2*k, k a non negative integer
        /// </summary>
        private static readonly int[] ZZ_LEXSTATE = {
            0,  0,  1,  1,  2,  2,  3,  3,  4,  4,  5,  5,  6,  6,  7,  7,
            8,  8,  9, 9
        };

        /// <summary>
        /// Translates characters to character classes
        /// </summary>
        private const string ZZ_CMAP_PACKED =
            "\x0009\x0000\x0001\x0014\x0001\x0013\x0001\x0000\x0001\x0014\x0001\x0012\x0012\x0000\x0001\x0014\x0001\x0000\x0001\x000A" +
            "\x0001\x002B\x0002\x0000\x0001\x0003\x0001\x0001\x0004\x0000\x0001\x000C\x0001\x0005\x0001\x0002\x0001\x0008\x000A\x000E" +
            "\x0001\x0017\x0001\x0000\x0001\x0007\x0001\x0009\x0001\x000B\x0001\x002B\x0001\x0004\x0002\x000D\x0001\x0018\x0005\x000D" +
            "\x0001\x0021\x0011\x000D\x0001\x0015\x0001\x0000\x0001\x0016\x0001\x0000\x0001\x0006\x0001\x0000\x0001\x0019\x0001\x0023" +
            "\x0002\x000D\x0001\x001B\x0001\x0020\x0001\x001C\x0001\x0028\x0001\x0021\x0004\x000D\x0001\x0022\x0001\x001D\x0001\x0029" +
            "\x0001\x000D\x0001\x001E\x0001\x002A\x0001\x001A\x0003\x000D\x0001\x0024\x0001\x001F\x0001\x000D\x0001\x0025\x0001\x0027" +
            "\x0001\x0026\x0042\x0000\x0017\x000D\x0001\x0000\x001F\x000D\x0001\x0000\u0568\x000D\x000A\x000F\x0086\x000D\x000A\x000F" +
            "\u026c\x000D\x000A\x000F\x0076\x000D\x000A\x000F\x0076\x000D\x000A\x000F\x0076\x000D\x000A\x000F\x0076\x000D\x000A\x000F" +
            "\x0077\x000D\x0009\x000F\x0076\x000D\x000A\x000F\x0076\x000D\x000A\x000F\x0076\x000D\x000A\x000F\x00E0\x000D\x000A\x000F" +
            "\x0076\x000D\x000A\x000F\u0166\x000D\x000A\x000F\x00B6\x000D\u0100\x000D\u0e00\x000D\u1040\x0000\u0150\x0011\x0060\x0000" +
            "\x0010\x0011\u0100\x0000\x0080\x0011\x0080\x0000\u19c0\x0011\x0040\x0000\u5200\x0011\u0c00\x0000\u2bb0\x0010\u2150\x0000" +
            "\u0200\x0011\u0465\x0000\x003B\x0011\x003D\x000D\x0023\x0000";

        /// <summary>
        /// Translates characters to character classes
        /// </summary>
        private static readonly char[] ZZ_CMAP = ZzUnpackCMap(ZZ_CMAP_PACKED);

        /// <summary>
        /// Translates DFA states to action switch labels.
        /// </summary>
        private static readonly int[] ZZ_ACTION = ZzUnpackAction();

        private const string ZZ_ACTION_PACKED_0 =
            "\x000A\x0000\x0004\x0001\x0004\x0002\x0001\x0003\x0001\x0004\x0001\x0001\x0002\x0005\x0001\x0006" +
            "\x0001\x0005\x0001\x0007\x0001\x0005\x0002\x0008\x0001\x0009\x0001\x0005\x0001\x000A\x0001\x0009" +
            "\x0001\x000B\x0001\x000C\x0001\x000D\x0001\x000E\x0001\x000D\x0001\x000F\x0001\x0010\x0001\x0008" +
            "\x0001\x0011\x0001\x0008\x0004\x0012\x0001\x0013\x0001\x0014\x0001\x0015\x0001\x0016\x0003\x0000" +
            "\x0001\x0017\x000C\x0000\x0001\x0018\x0001\x0019\x0001\x001A\x0001\x001B\x0001\x0009\x0001\x0000" +
            "\x0001\x001C\x0001\x001D\x0001\x001E\x0001\x0000\x0001\x001F\x0001\x0000\x0001\x0020\x0003\x0000" +
            "\x0001\x0021\x0001\x0022\x0002\x0023\x0001\x0022\x0002\x0024\x0002\x0000\x0001\x0023\x0001\x0000" +
            "\x000C\x0023\x0001\x0022\x0003\x0000\x0001\x0009\x0001\x0025\x0003\x0000\x0001\x0026\x0001\x0027" +
            "\x0005\x0000\x0001\x0028\x0004\x0000\x0001\x0028\x0002\x0000\x0002\x0028\x0002\x0000\x0001\x0009" +
            "\x0005\x0000\x0001\x0019\x0001\x0022\x0001\x0023\x0001\x0029\x0003\x0000\x0001\x0009\x0002\x0000" +
            "\x0001\x002A\x0018\x0000\x0001\x002B\x0002\x0000\x0001\x002C\x0001\x002D\x0001\x002E";

        private static int[] ZzUnpackAction()
        {
            int[] result = new int[181];
            int offset = 0;
            offset = ZzUnpackAction(ZZ_ACTION_PACKED_0, offset, result);
            return result;
        }

        private static int ZzUnpackAction(string packed, int offset, int[] result)
        {
            int i = 0;       /* index in packed string  */
            int j = offset;  /* index in unpacked array */
            int l = packed.Length;
            while (i < l)
            {
                int count = packed[i++];
                int value = packed[i++];
                do result[j++] = value; while (--count > 0);
            }
            return j;
        }


        /// <summary>
        /// Translates a state to a row index in the transition table
        /// </summary>
        private static readonly int[] ZZ_ROWMAP = ZzUnpackRowMap();

        private const string ZZ_ROWMAP_PACKED_0 =
            "\x0000\x0000\x0000\x002C\x0000\x0058\x0000\x0084\x0000\x00B0\x0000\x00DC\x0000\u0108\x0000\u0134" +
            "\x0000\u0160\x0000\u018c\x0000\u01b8\x0000\u01e4\x0000\u0210\x0000\u023c\x0000\u0268\x0000\u0294" +
            "\x0000\u02c0\x0000\u02ec\x0000\u01b8\x0000\u0318\x0000\u0344\x0000\u01b8\x0000\u0370\x0000\u039c" +
            "\x0000\u03c8\x0000\u03f4\x0000\u0420\x0000\u01b8\x0000\u0370\x0000\u044c\x0000\u0478\x0000\u01b8" +
            "\x0000\u04a4\x0000\u04d0\x0000\u04fc\x0000\u0528\x0000\u0554\x0000\u0580\x0000\u05ac\x0000\u05d8" +
            "\x0000\u0604\x0000\u0630\x0000\u065c\x0000\u01b8\x0000\u0688\x0000\u0370\x0000\u06b4\x0000\u06e0" +
            "\x0000\u070c\x0000\u01b8\x0000\u01b8\x0000\u0738\x0000\u0764\x0000\u0790\x0000\u01b8\x0000\u07bc" +
            "\x0000\u07e8\x0000\u0814\x0000\u0840\x0000\u086c\x0000\u0898\x0000\u08c4\x0000\u08f0\x0000\u091c" +
            "\x0000\u0948\x0000\u0974\x0000\u09a0\x0000\u09cc\x0000\u09f8\x0000\u01b8\x0000\u01b8\x0000\u0a24" +
            "\x0000\u0a50\x0000\u0a7c\x0000\u0a7c\x0000\u01b8\x0000\u0aa8\x0000\u0ad4\x0000\u0b00\x0000\u0b2c" +
            "\x0000\u0b58\x0000\u0b84\x0000\u0bb0\x0000\u0bdc\x0000\u0c08\x0000\u0c34\x0000\u0c60\x0000\u0c8c" +
            "\x0000\u0814\x0000\u0cb8\x0000\u0ce4\x0000\u0d10\x0000\u0d3c\x0000\u0d68\x0000\u0d94\x0000\u0dc0" +
            "\x0000\u0dec\x0000\u0e18\x0000\u0e44\x0000\u0e70\x0000\u0e9c\x0000\u0ec8\x0000\u0ef4\x0000\u0f20" +
            "\x0000\u0f4c\x0000\u0f78\x0000\u0fa4\x0000\u0fd0\x0000\u0ffc\x0000\u1028\x0000\u1054\x0000\u01b8" +
            "\x0000\u1080\x0000\u10ac\x0000\u10d8\x0000\u1104\x0000\u01b8\x0000\u1130\x0000\u115c\x0000\u1188" +
            "\x0000\u11b4\x0000\u11e0\x0000\u120c\x0000\u1238\x0000\u1264\x0000\u1290\x0000\u12bc\x0000\u12e8" +
            "\x0000\u1314\x0000\u1340\x0000\u07e8\x0000\u0974\x0000\u136c\x0000\u1398\x0000\u13c4\x0000\u13f0" +
            "\x0000\u141c\x0000\u1448\x0000\u1474\x0000\u14a0\x0000\u01b8\x0000\u14cc\x0000\u14f8\x0000\u1524" +
            "\x0000\u1550\x0000\u157c\x0000\u15a8\x0000\u15d4\x0000\u1600\x0000\u162c\x0000\u01b8\x0000\u1658" +
            "\x0000\u1684\x0000\u16b0\x0000\u16dc\x0000\u1708\x0000\u1734\x0000\u1760\x0000\u178c\x0000\u17b8" +
            "\x0000\u17e4\x0000\u1810\x0000\u183c\x0000\u1868\x0000\u1894\x0000\u18c0\x0000\u18ec\x0000\u1918" +
            "\x0000\u1944\x0000\u1970\x0000\u199c\x0000\u19c8\x0000\u19f4\x0000\u1a20\x0000\u1a4c\x0000\u1a78" +
            "\x0000\u1aa4\x0000\u1ad0\x0000\u01b8\x0000\u01b8\x0000\u01b8";

        private static int[] ZzUnpackRowMap()
        {
            int[] result = new int[181];
            int offset = 0;
            offset = ZzUnpackRowMap(ZZ_ROWMAP_PACKED_0, offset, result);
            return result;
        }

        private static int ZzUnpackRowMap(string packed, int offset, int[] result)
        {
            int i = 0;  /* index in packed string  */
            int j = offset;  /* index in unpacked array */
            int l = packed.Length;
            while (i < l)
            {
                int high = packed[i++] << 16;
                result[j++] = high | packed[i++];
            }
            return j;
        }

        /// <summary>
        /// The transition table of the DFA
        /// </summary>
        private static readonly int[] ZZ_TRANS = ZzUnpackTrans();

        private const string ZZ_TRANS_PACKED_0 =
            "\x0001\x000B\x0001\x000C\x0005\x000B\x0001\x000D\x0001\x000B\x0001\x000E\x0003\x000B\x0001\x000F" +
            "\x0001\x0010\x0001\x0011\x0001\x0012\x0001\x0013\x0003\x000B\x0001\x0014\x0002\x000B\x000D\x000F" +
            "\x0001\x0015\x0002\x000B\x0003\x000F\x0001\x000B\x0007\x0016\x0001\x0017\x0005\x0016\x0004\x0018" +
            "\x0005\x0016\x0001\x0019\x0001\x0016\x000D\x0018\x0003\x0016\x0003\x0018\x0008\x0016\x0001\x0017" +
            "\x0005\x0016\x0004\x001A\x0005\x0016\x0001\x001B\x0001\x0016\x000D\x001A\x0003\x0016\x0003\x001A" +
            "\x0001\x0016\x0007\x001C\x0001\x001D\x0005\x001C\x0004\x001E\x0001\x001C\x0001\x001F\x0002\x0016" +
            "\x0001\x001C\x0001\x0020\x0001\x001C\x000D\x001E\x0003\x001C\x0001\x0021\x0002\x001E\x0002\x001C" +
            "\x0001\x0022\x0005\x001C\x0001\x001D\x0005\x001C\x0004\x0023\x0004\x001C\x0001\x0024\x0002\x001C" +
            "\x000D\x0023\x0003\x001C\x0003\x0023\x0008\x001C\x0001\x001D\x0005\x001C\x0004\x0025\x0004\x001C" +
            "\x0001\x0024\x0002\x001C\x000D\x0025\x0003\x001C\x0003\x0025\x0008\x001C\x0001\x001D\x0005\x001C" +
            "\x0004\x0025\x0004\x001C\x0001\x0026\x0002\x001C\x000D\x0025\x0003\x001C\x0003\x0025\x0008\x001C" +
            "\x0001\x001D\x0001\x001C\x0001\x0027\x0003\x001C\x0004\x0028\x0007\x001C\x000D\x0028\x0003\x001C" +
            "\x0003\x0028\x0008\x001C\x0001\x0029\x0005\x001C\x0004\x002A\x0007\x001C\x000D\x002A\x0001\x001C" +
            "\x0001\x002B\x0001\x001C\x0003\x002A\x0001\x001C\x0001\x002C\x0001\x002D\x0005\x002C\x0001\x002E" +
            "\x0001\x002C\x0001\x002F\x0003\x002C\x0004\x0030\x0004\x002C\x0001\x0031\x0002\x002C\x000D\x0030" +
            "\x0002\x002C\x0001\x0032\x0003\x0030\x0001\x002C\x002D\x0000\x0001\x0033\x0032\x0000\x0001\x0034" +
            "\x0004\x0000\x0004\x0035\x0007\x0000\x0006\x0035\x0001\x0036\x0006\x0035\x0003\x0000\x0003\x0035" +
            "\x000A\x0000\x0001\x0037\x0023\x0000\x0001\x0038\x0001\x0039\x0001\x003A\x0001\x003B\x0002\x003C" +
            "\x0001\x0000\x0001\x003D\x0003\x0000\x0001\x003D\x0001\x000F\x0001\x0010\x0001\x0011\x0001\x0012" +
            "\x0007\x0000\x000D\x000F\x0003\x0000\x0003\x000F\x0003\x0000\x0001\x003E\x0001\x0000\x0001\x003F" +
            "\x0002\x0040\x0001\x0000\x0001\x0041\x0003\x0000\x0001\x0041\x0003\x0010\x0001\x0012\x0007\x0000" +
            "\x000D\x0010\x0003\x0000\x0003\x0010\x0002\x0000\x0001\x0038\x0001\x0042\x0001\x003A\x0001\x003B" +
            "\x0002\x0040\x0001\x0000\x0001\x0041\x0003\x0000\x0001\x0041\x0001\x0011\x0001\x0010\x0001\x0011" +
            "\x0001\x0012\x0007\x0000\x000D\x0011\x0003\x0000\x0003\x0011\x0003\x0000\x0001\x0043\x0001\x0000" +
            "\x0001\x003F\x0002\x003C\x0001\x0000\x0001\x003D\x0003\x0000\x0001\x003D\x0004\x0012\x0007\x0000" +
            "\x000D\x0012\x0003\x0000\x0003\x0012\x0016\x0000\x0001\x0044\x003B\x0000\x0001\x0045\x000E\x0000" +
            "\x0001\x0034\x0004\x0000\x0004\x0035\x0007\x0000\x000D\x0035\x0003\x0000\x0003\x0035\x000E\x0000" +
            "\x0004\x0018\x0007\x0000\x000D\x0018\x0003\x0000\x0003\x0018\x0017\x0000\x0001\x0046\x0022\x0000" +
            "\x0004\x001A\x0007\x0000\x000D\x001A\x0003\x0000\x0003\x001A\x0017\x0000\x0001\x0047\x0022\x0000" +
            "\x0004\x001E\x0007\x0000\x000D\x001E\x0003\x0000\x0003\x001E\x0014\x0000\x0001\x0016\x0025\x0000" +
            "\x0004\x001E\x0007\x0000\x0002\x001E\x0001\x0048\x000A\x001E\x0003\x0000\x0003\x001E\x0002\x0000" +
            "\x0001\x0049\x0037\x0000\x0004\x0023\x0007\x0000\x000D\x0023\x0003\x0000\x0003\x0023\x0016\x0000" +
            "\x0001\x004A\x0023\x0000\x0004\x0025\x0007\x0000\x000D\x0025\x0003\x0000\x0003\x0025\x0016\x0000" +
            "\x0001\x004B\x001F\x0000\x0001\x004C\x002F\x0000\x0004\x0028\x0007\x0000\x000D\x0028\x0003\x0000" +
            "\x0003\x0028\x0009\x0000\x0001\x004D\x0004\x0000\x0004\x0035\x0007\x0000\x000D\x0035\x0003\x0000" +
            "\x0003\x0035\x000E\x0000\x0004\x002A\x0007\x0000\x000D\x002A\x0003\x0000\x0003\x002A\x0027\x0000" +
            "\x0001\x004C\x0006\x0000\x0001\x004E\x0033\x0000\x0001\x004F\x002F\x0000\x0004\x0030\x0007\x0000" +
            "\x000D\x0030\x0003\x0000\x0003\x0030\x0016\x0000\x0001\x0050\x0023\x0000\x0004\x0035\x0007\x0000" +
            "\x000D\x0035\x0003\x0000\x0003\x0035\x000C\x0000\x0001\x001C\x0001\x0000\x0004\x0051\x0001\x0000" +
            "\x0003\x0052\x0003\x0000\x000D\x0051\x0003\x0000\x0003\x0051\x000C\x0000\x0001\x001C\x0001\x0000" +
            "\x0004\x0051\x0001\x0000\x0003\x0052\x0003\x0000\x0003\x0051\x0001\x0053\x0009\x0051\x0003\x0000" +
            "\x0003\x0051\x000E\x0000\x0001\x0054\x0001\x0000\x0001\x0054\x0008\x0000\x000D\x0054\x0003\x0000" +
            "\x0003\x0054\x000E\x0000\x0001\x0055\x0001\x0056\x0001\x0057\x0001\x0058\x0007\x0000\x000D\x0055" +
            "\x0003\x0000\x0003\x0055\x000E\x0000\x0001\x0059\x0001\x0000\x0001\x0059\x0008\x0000\x000D\x0059" +
            "\x0003\x0000\x0003\x0059\x000E\x0000\x0001\x005A\x0001\x005B\x0001\x005A\x0001\x005B\x0007\x0000" +
            "\x000D\x005A\x0003\x0000\x0003\x005A\x000E\x0000\x0001\x005C\x0002\x005D\x0001\x005E\x0007\x0000" +
            "\x000D\x005C\x0003\x0000\x0003\x005C\x000E\x0000\x0001\x003D\x0002\x005F\x0008\x0000\x000D\x003D" +
            "\x0003\x0000\x0003\x003D\x000E\x0000\x0001\x0060\x0002\x0061\x0001\x0062\x0007\x0000\x000D\x0060" +
            "\x0003\x0000\x0003\x0060\x000E\x0000\x0004\x005B\x0007\x0000\x000D\x005B\x0003\x0000\x0003\x005B" +
            "\x000E\x0000\x0001\x0063\x0002\x0064\x0001\x0065\x0007\x0000\x000D\x0063\x0003\x0000\x0003\x0063" +
            "\x000E\x0000\x0001\x0066\x0002\x0067\x0001\x0068\x0007\x0000\x000D\x0066\x0003\x0000\x0003\x0066" +
            "\x000E\x0000\x0001\x0069\x0001\x0061\x0001\x006A\x0001\x0062\x0007\x0000\x000D\x0069\x0003\x0000" +
            "\x0003\x0069\x000E\x0000\x0001\x006B\x0002\x0056\x0001\x0058\x0007\x0000\x000D\x006B\x0003\x0000" +
            "\x0003\x006B\x0018\x0000\x0001\x006C\x0001\x006D\x0034\x0000\x0001\x006E\x0017\x0000\x0004\x001E" +
            "\x0007\x0000\x0002\x001E\x0001\x006F\x000A\x001E\x0003\x0000\x0003\x001E\x0002\x0000\x0001\x0070" +
            "\x0041\x0000\x0001\x0071\x0001\x0072\x0020\x0000\x0004\x0035\x0007\x0000\x0006\x0035\x0001\x0073" +
            "\x0006\x0035\x0003\x0000\x0003\x0035\x0002\x0000\x0001\x0074\x0033\x0000\x0001\x0075\x0039\x0000" +
            "\x0001\x0076\x0001\x0077\x001C\x0000\x0001\x0078\x0001\x0000\x0001\x001C\x0001\x0000\x0004\x0051" +
            "\x0001\x0000\x0003\x0052\x0003\x0000\x000D\x0051\x0003\x0000\x0003\x0051\x000E\x0000\x0004\x0079" +
            "\x0001\x0000\x0003\x0052\x0003\x0000\x000D\x0079\x0003\x0000\x0003\x0079\x000A\x0000\x0001\x0078" +
            "\x0001\x0000\x0001\x001C\x0001\x0000\x0004\x0051\x0001\x0000\x0003\x0052\x0003\x0000\x0008\x0051" +
            "\x0001\x007A\x0004\x0051\x0003\x0000\x0003\x0051\x0002\x0000\x0001\x0038\x000B\x0000\x0001\x0054" +
            "\x0001\x0000\x0001\x0054\x0008\x0000\x000D\x0054\x0003\x0000\x0003\x0054\x0003\x0000\x0001\x007B" +
            "\x0001\x0000\x0001\x003F\x0002\x007C\x0006\x0000\x0001\x0055\x0001\x0056\x0001\x0057\x0001\x0058" +
            "\x0007\x0000\x000D\x0055\x0003\x0000\x0003\x0055\x0003\x0000\x0001\x007D\x0001\x0000\x0001\x003F" +
            "\x0002\x007E\x0001\x0000\x0001\x007F\x0003\x0000\x0001\x007F\x0003\x0056\x0001\x0058\x0007\x0000" +
            "\x000D\x0056\x0003\x0000\x0003\x0056\x0003\x0000\x0001\x0080\x0001\x0000\x0001\x003F\x0002\x007E" +
            "\x0001\x0000\x0001\x007F\x0003\x0000\x0001\x007F\x0001\x0057\x0001\x0056\x0001\x0057\x0001\x0058" +
            "\x0007\x0000\x000D\x0057\x0003\x0000\x0003\x0057\x0003\x0000\x0001\x0081\x0001\x0000\x0001\x003F" +
            "\x0002\x007C\x0006\x0000\x0004\x0058\x0007\x0000\x000D\x0058\x0003\x0000\x0003\x0058\x0003\x0000" +
            "\x0001\x0082\x0002\x0000\x0001\x0082\x0007\x0000\x0001\x005A\x0001\x005B\x0001\x005A\x0001\x005B" +
            "\x0007\x0000\x000D\x005A\x0003\x0000\x0003\x005A\x0003\x0000\x0001\x0082\x0002\x0000\x0001\x0082" +
            "\x0007\x0000\x0004\x005B\x0007\x0000\x000D\x005B\x0003\x0000\x0003\x005B\x0003\x0000\x0001\x007C" +
            "\x0001\x0000\x0001\x003F\x0002\x007C\x0006\x0000\x0001\x005C\x0002\x005D\x0001\x005E\x0007\x0000" +
            "\x000D\x005C\x0003\x0000\x0003\x005C\x0003\x0000\x0001\x007E\x0001\x0000\x0001\x003F\x0002\x007E" +
            "\x0001\x0000\x0001\x007F\x0003\x0000\x0001\x007F\x0003\x005D\x0001\x005E\x0007\x0000\x000D\x005D" +
            "\x0003\x0000\x0003\x005D\x0003\x0000\x0001\x007C\x0001\x0000\x0001\x003F\x0002\x007C\x0006\x0000" +
            "\x0004\x005E\x0007\x0000\x000D\x005E\x0003\x0000\x0003\x005E\x0003\x0000\x0001\x007F\x0002\x0000" +
            "\x0002\x007F\x0001\x0000\x0001\x007F\x0003\x0000\x0001\x007F\x0003\x005F\x0008\x0000\x000D\x005F" +
            "\x0003\x0000\x0003\x005F\x0003\x0000\x0001\x0043\x0001\x0000\x0001\x003F\x0002\x003C\x0001\x0000" +
            "\x0001\x003D\x0003\x0000\x0001\x003D\x0001\x0060\x0002\x0061\x0001\x0062\x0007\x0000\x000D\x0060" +
            "\x0003\x0000\x0003\x0060\x0003\x0000\x0001\x003E\x0001\x0000\x0001\x003F\x0002\x0040\x0001\x0000" +
            "\x0001\x0041\x0003\x0000\x0001\x0041\x0003\x0061\x0001\x0062\x0007\x0000\x000D\x0061\x0003\x0000" +
            "\x0003\x0061\x0003\x0000\x0001\x0043\x0001\x0000\x0001\x003F\x0002\x003C\x0001\x0000\x0001\x003D" +
            "\x0003\x0000\x0001\x003D\x0004\x0062\x0007\x0000\x000D\x0062\x0003\x0000\x0003\x0062\x0003\x0000" +
            "\x0001\x003C\x0001\x0000\x0001\x003F\x0002\x003C\x0001\x0000\x0001\x003D\x0003\x0000\x0001\x003D" +
            "\x0001\x0063\x0002\x0064\x0001\x0065\x0007\x0000\x000D\x0063\x0003\x0000\x0003\x0063\x0003\x0000" +
            "\x0001\x0040\x0001\x0000\x0001\x003F\x0002\x0040\x0001\x0000\x0001\x0041\x0003\x0000\x0001\x0041" +
            "\x0003\x0064\x0001\x0065\x0007\x0000\x000D\x0064\x0003\x0000\x0003\x0064\x0003\x0000\x0001\x003C" +
            "\x0001\x0000\x0001\x003F\x0002\x003C\x0001\x0000\x0001\x003D\x0003\x0000\x0001\x003D\x0004\x0065" +
            "\x0007\x0000\x000D\x0065\x0003\x0000\x0003\x0065\x0003\x0000\x0001\x003D\x0002\x0000\x0002\x003D" +
            "\x0001\x0000\x0001\x003D\x0003\x0000\x0001\x003D\x0001\x0066\x0002\x0067\x0001\x0068\x0007\x0000" +
            "\x000D\x0066\x0003\x0000\x0003\x0066\x0003\x0000\x0001\x0041\x0002\x0000\x0002\x0041\x0001\x0000" +
            "\x0001\x0041\x0003\x0000\x0001\x0041\x0003\x0067\x0001\x0068\x0007\x0000\x000D\x0067\x0003\x0000" +
            "\x0003\x0067\x0003\x0000\x0001\x003D\x0002\x0000\x0002\x003D\x0001\x0000\x0001\x003D\x0003\x0000" +
            "\x0001\x003D\x0004\x0068\x0007\x0000\x000D\x0068\x0003\x0000\x0003\x0068\x0003\x0000\x0001\x0083" +
            "\x0001\x0000\x0001\x003F\x0002\x003C\x0001\x0000\x0001\x003D\x0003\x0000\x0001\x003D\x0001\x0069" +
            "\x0001\x0061\x0001\x006A\x0001\x0062\x0007\x0000\x000D\x0069\x0003\x0000\x0003\x0069\x0003\x0000" +
            "\x0001\x0084\x0001\x0000\x0001\x003F\x0002\x0040\x0001\x0000\x0001\x0041\x0003\x0000\x0001\x0041" +
            "\x0001\x006A\x0001\x0061\x0001\x006A\x0001\x0062\x0007\x0000\x000D\x006A\x0003\x0000\x0003\x006A" +
            "\x0003\x0000\x0001\x0081\x0001\x0000\x0001\x003F\x0002\x007C\x0006\x0000\x0001\x006B\x0002\x0056" +
            "\x0001\x0058\x0007\x0000\x000D\x006B\x0003\x0000\x0003\x006B\x0019\x0000\x0001\x006D\x002C\x0000" +
            "\x0001\x0085\x0034\x0000\x0001\x0086\x0016\x0000\x0004\x001E\x0007\x0000\x000D\x001E\x0003\x0000" +
            "\x0001\x001E\x0001\x0087\x0001\x001E\x0019\x0000\x0001\x0072\x002C\x0000\x0001\x0088\x001D\x0000" +
            "\x0001\x001C\x0001\x0000\x0004\x0051\x0001\x0000\x0003\x0052\x0003\x0000\x0003\x0051\x0001\x0089" +
            "\x0009\x0051\x0003\x0000\x0003\x0051\x0002\x0000\x0001\x008A\x0042\x0000\x0001\x0077\x002C\x0000" +
            "\x0001\x008B\x001C\x0000\x0001\x008C\x002A\x0000\x0001\x0078\x0003\x0000\x0004\x0079\x0007\x0000" +
            "\x000D\x0079\x0003\x0000\x0003\x0079\x000A\x0000\x0001\x0078\x0001\x0000\x0001\x008D\x0001\x0000" +
            "\x0004\x0051\x0001\x0000\x0003\x0052\x0003\x0000\x000D\x0051\x0003\x0000\x0003\x0051\x000E\x0000" +
            "\x0001\x008E\x0001\x0058\x0001\x008E\x0001\x0058\x0007\x0000\x000D\x008E\x0003\x0000\x0003\x008E" +
            "\x000E\x0000\x0004\x005E\x0007\x0000\x000D\x005E\x0003\x0000\x0003\x005E\x000E\x0000\x0004\x0062" +
            "\x0007\x0000\x000D\x0062\x0003\x0000\x0003\x0062\x000E\x0000\x0004\x0065\x0007\x0000\x000D\x0065" +
            "\x0003\x0000\x0003\x0065\x000E\x0000\x0004\x0068\x0007\x0000\x000D\x0068\x0003\x0000\x0003\x0068" +
            "\x000E\x0000\x0001\x008F\x0001\x0062\x0001\x008F\x0001\x0062\x0007\x0000\x000D\x008F\x0003\x0000" +
            "\x0003\x008F\x000E\x0000\x0004\x0058\x0007\x0000\x000D\x0058\x0003\x0000\x0003\x0058\x000E\x0000" +
            "\x0004\x0090\x0007\x0000\x000D\x0090\x0003\x0000\x0003\x0090\x001B\x0000\x0001\x0091\x0031\x0000" +
            "\x0001\x0092\x0018\x0000\x0004\x001E\x0006\x0000\x0001\x0093\x000D\x001E\x0003\x0000\x0002\x001E" +
            "\x0001\x0094\x001B\x0000\x0001\x0095\x001A\x0000\x0001\x0078\x0001\x0000\x0001\x001C\x0001\x0000" +
            "\x0004\x0051\x0001\x0000\x0003\x0052\x0003\x0000\x0008\x0051\x0001\x0096\x0004\x0051\x0003\x0000" +
            "\x0003\x0051\x0002\x0000\x0001\x0097\x0044\x0000\x0001\x0098\x001E\x0000\x0004\x0099\x0007\x0000" +
            "\x000D\x0099\x0003\x0000\x0003\x0099\x0003\x0000\x0001\x007B\x0001\x0000\x0001\x003F\x0002\x007C" +
            "\x0006\x0000\x0001\x008E\x0001\x0058\x0001\x008E\x0001\x0058\x0007\x0000\x000D\x008E\x0003\x0000" +
            "\x0003\x008E\x0003\x0000\x0001\x0083\x0001\x0000\x0001\x003F\x0002\x003C\x0001\x0000\x0001\x003D" +
            "\x0003\x0000\x0001\x003D\x0001\x008F\x0001\x0062\x0001\x008F\x0001\x0062\x0007\x0000\x000D\x008F" +
            "\x0003\x0000\x0003\x008F\x0003\x0000\x0001\x0082\x0002\x0000\x0001\x0082\x0007\x0000\x0004\x0090" +
            "\x0007\x0000\x000D\x0090\x0003\x0000\x0003\x0090\x001C\x0000\x0001\x009A\x002D\x0000\x0001\x009B" +
            "\x0016\x0000\x0001\x009C\x0030\x0000\x0004\x001E\x0006\x0000\x0001\x0093\x000D\x001E\x0003\x0000" +
            "\x0003\x001E\x001C\x0000\x0001\x009D\x0019\x0000\x0001\x0078\x0001\x0000\x0001\x004C\x0001\x0000" +
            "\x0004\x0051\x0001\x0000\x0003\x0052\x0003\x0000\x000D\x0051\x0003\x0000\x0003\x0051\x001C\x0000" +
            "\x0001\x009E\x001A\x0000\x0001\x009F\x0002\x0000\x0004\x0099\x0007\x0000\x000D\x0099\x0003\x0000" +
            "\x0003\x0099\x001D\x0000\x0001\x00A0\x0032\x0000\x0001\x00A1\x0010\x0000\x0001\x00A2\x003F\x0000" +
            "\x0001\x00A3\x002B\x0000\x0001\x00A4\x001A\x0000\x0001\x001C\x0001\x0000\x0004\x0079\x0001\x0000" +
            "\x0003\x0052\x0003\x0000\x000D\x0079\x0003\x0000\x0003\x0079\x001E\x0000\x0001\x00A5\x002B\x0000" +
            "\x0001\x00A6\x001B\x0000\x0004\x00A7\x0007\x0000\x000D\x00A7\x0003\x0000\x0003\x00A7\x001E\x0000" +
            "\x0001\x00A8\x002B\x0000\x0001\x00A9\x002C\x0000\x0001\x00AA\x0031\x0000\x0001\x00AB\x0009\x0000" +
            "\x0001\x00AC\x000A\x0000\x0004\x00A7\x0007\x0000\x000D\x00A7\x0003\x0000\x0003\x00A7\x001F\x0000" +
            "\x0001\x00AD\x002B\x0000\x0001\x00AE\x002C\x0000\x0001\x00AF\x0012\x0000\x0001\x000B\x0032\x0000" +
            "\x0004\x00B0\x0007\x0000\x000D\x00B0\x0003\x0000\x0003\x00B0\x0020\x0000\x0001\x00B1\x002B\x0000" +
            "\x0001\x00B2\x0023\x0000\x0001\x00B3\x0016\x0000\x0002\x00B0\x0001\x0000\x0002\x00B0\x0001\x0000" +
            "\x0002\x00B0\x0002\x0000\x0005\x00B0\x0007\x0000\x000D\x00B0\x0003\x0000\x0004\x00B0\x0017\x0000" +
            "\x0001\x00B4\x002B\x0000\x0001\x00B5\x0014\x0000";

        private static int[] ZzUnpackTrans()
        {
            int[] result = new int[6908];
            int offset = 0;
            offset = ZzUnpackTrans(ZZ_TRANS_PACKED_0, offset, result);
            return result;
        }

        private static int ZzUnpackTrans(string packed, int offset, int[] result)
        {
            int i = 0;       /* index in packed string  */
            int j = offset;  /* index in unpacked array */
            int l = packed.Length;
            while (i < l)
            {
                int count = packed[i++];
                int value = packed[i++];
                value--;
                do result[j++] = value; while (--count > 0);
            }
            return j;
        }


        /* error codes */
        private static readonly int ZZ_UNKNOWN_ERROR = 0;
        private static readonly int ZZ_NO_MATCH = 1;
        private static readonly int ZZ_PUSHBACK_2BIG = 2;

        /* error messages for the codes above */
        private static readonly string[] ZZ_ERROR_MSG = {
            "Unkown internal scanner error",
            "Error: could not match input",
            "Error: pushback value was too large"
        };

        /// <summary>
        /// ZZ_ATTRIBUTE[aState] contains the attributes of state <c>aState</c>
        /// </summary>
        private static readonly int[] ZZ_ATTRIBUTE = ZzUnpackAttribute();

        private const string ZZ_ATTRIBUTE_PACKED_0 =
            "\x000A\x0000\x0001\x0009\x0007\x0001\x0001\x0009\x0002\x0001\x0001\x0009\x0005\x0001\x0001\x0009" +
            "\x0003\x0001\x0001\x0009\x000B\x0001\x0001\x0009\x0005\x0001\x0002\x0009\x0003\x0000\x0001\x0009" +
            "\x000C\x0000\x0002\x0001\x0002\x0009\x0001\x0001\x0001\x0000\x0002\x0001\x0001\x0009\x0001\x0000" +
            "\x0001\x0001\x0001\x0000\x0001\x0001\x0003\x0000\x0007\x0001\x0002\x0000\x0001\x0001\x0001\x0000" +
            "\x000D\x0001\x0003\x0000\x0001\x0001\x0001\x0009\x0003\x0000\x0001\x0001\x0001\x0009\x0005\x0000" +
            "\x0001\x0001\x0004\x0000\x0001\x0001\x0002\x0000\x0002\x0001\x0002\x0000\x0001\x0001\x0005\x0000" +
            "\x0001\x0009\x0003\x0001\x0003\x0000\x0001\x0001\x0002\x0000\x0001\x0009\x0018\x0000\x0001\x0001" +
            "\x0002\x0000\x0003\x0009";

        private static int[] ZzUnpackAttribute()
        {
            int[] result = new int[181];
            int offset = 0;
            offset = ZzUnpackAttribute(ZZ_ATTRIBUTE_PACKED_0, offset, result);
            return result;
        }

        private static int ZzUnpackAttribute(string packed, int offset, int[] result)
        {
            int i = 0;       /* index in packed string  */
            int j = offset;  /* index in unpacked array */
            int l = packed.Length;
            while (i < l)
            {
                int count = packed[i++];
                int value = packed[i++];
                do result[j++] = value; while (--count > 0);
            }
            return j;
        }

        /// <summary>the input device</summary>
        private TextReader zzReader;

        /// <summary>the current state of the DFA</summary>
        private int zzState;

        /// <summary>the current lexical state</summary>
        private int zzLexicalState = YYINITIAL;

        /// <summary>
        /// this buffer contains the current text to be matched and is
        /// the source of the YyText string 
        /// </summary>
        private char[] zzBuffer = new char[ZZ_BUFFERSIZE];

        /// <summary>the textposition at the last accepting state</summary>
        private int zzMarkedPos;

        /// <summary>the current text position in the buffer</summary>
        private int zzCurrentPos;

        /// <summary>startRead marks the beginning of the YyText string in the buffer</summary>
        private int zzStartRead;

        /// <summary>
        /// endRead marks the last character in the buffer, that has been read
        /// from input
        /// </summary>
        private int zzEndRead;

        ///// <summary>number of newlines encountered up to the start of the matched text</summary>
        //private int yyline;

        /// <summary>the number of characters up to the start of the matched text</summary>
        private int yychar;

        ///// <summary>
        ///// the number of characters from the last newline up to the start of the
        ///// matched text
        ///// </summary>
        //private int yycolumn; // LUCENENET: Never read

        ///// <summary>
        ///// zzAtBOL == true &lt;=&gt; the scanner is currently at the beginning of a line
        ///// </summary>
        //private bool zzAtBOL = true; // LUCENENET: Never read

        /// <summary>zzAtEOF == true &lt;=&gt; the scanner is at the EOF</summary>
        private bool zzAtEOF;

        ///// <summary>denotes if the user-EOF-code has already been executed</summary>
        //private bool zzEOFDone; // LUCENENET: Never read


        /* user code: */

        public static readonly int ALPHANUM = WikipediaTokenizer.ALPHANUM_ID;
        public static readonly int APOSTROPHE = WikipediaTokenizer.APOSTROPHE_ID;
        public static readonly int ACRONYM = WikipediaTokenizer.ACRONYM_ID;
        public static readonly int COMPANY = WikipediaTokenizer.COMPANY_ID;
        public static readonly int EMAIL = WikipediaTokenizer.EMAIL_ID;
        public static readonly int HOST = WikipediaTokenizer.HOST_ID;
        public static readonly int NUM = WikipediaTokenizer.NUM_ID;
        public static readonly int CJ = WikipediaTokenizer.CJ_ID;
        public static readonly int INTERNAL_LINK = WikipediaTokenizer.INTERNAL_LINK_ID;
        public static readonly int EXTERNAL_LINK = WikipediaTokenizer.EXTERNAL_LINK_ID;
        public static readonly int CITATION = WikipediaTokenizer.CITATION_ID;
        public static readonly int CATEGORY = WikipediaTokenizer.CATEGORY_ID;
        public static readonly int BOLD = WikipediaTokenizer.BOLD_ID;
        public static readonly int ITALICS = WikipediaTokenizer.ITALICS_ID;
        public static readonly int BOLD_ITALICS = WikipediaTokenizer.BOLD_ITALICS_ID;
        public static readonly int HEADING = WikipediaTokenizer.HEADING_ID;
        public static readonly int SUB_HEADING = WikipediaTokenizer.SUB_HEADING_ID;
        public static readonly int EXTERNAL_LINK_URL = WikipediaTokenizer.EXTERNAL_LINK_URL_ID;


        private int currentTokType;
        private int numBalanced = 0;
        private int positionInc = 1;
        private int numLinkToks = 0;
        //Anytime we start a new on a Wiki reserved token (category, link, etc.) this value will be 0, otherwise it will be the number of tokens seen
        //this can be useful for detecting when a new reserved token is encountered
        //see https://issues.apache.org/jira/browse/LUCENE-1133
        private int numWikiTokensSeen = 0;

        public static readonly string[] TOKEN_TYPES = WikipediaTokenizer.TOKEN_TYPES;

        /// <summary>
        /// Returns the number of tokens seen inside a category or link, etc.
        /// </summary>
        /// <returns>the number of tokens seen inside the context of wiki syntax.</returns>
        public int NumWikiTokensSeen => numWikiTokensSeen;

        public int YyChar => yychar;

        public int PositionIncrement => positionInc;

        /// <summary>
        /// Fills Lucene token with the current token text.
        /// </summary>
        internal void GetText(ICharTermAttribute t)
        {
            t.CopyBuffer(zzBuffer, zzStartRead, zzMarkedPos - zzStartRead);
        }

        internal int SetText(StringBuilder buffer)
        {
            int length = zzMarkedPos - zzStartRead;
            buffer.Append(zzBuffer, zzStartRead, length);
            return length;
        }

        internal void Reset()
        {
            currentTokType = 0;
            numBalanced = 0;
            positionInc = 1;
            numLinkToks = 0;
            numWikiTokensSeen = 0;
        }

        /// <summary>
        /// Creates a new scanner
        /// </summary>
        /// <param name="in">the TextReader to read input from.</param>
        internal WikipediaTokenizerImpl(TextReader @in)
        {
            this.zzReader = @in;
        }


        /// <summary>
        /// Unpacks the compressed character translation table.
        /// </summary>
        /// <param name="packed">the packed character translation table</param>
        /// <returns>the unpacked character translation table</returns>
        private static char[] ZzUnpackCMap(string packed)
        {
            char[] map = new char[0x10000];
            int i = 0;  /* index in packed string  */
            int j = 0;  /* index in unpacked array */
            while (i < 230)
            {
                int count = packed[i++];
                char value = packed[i++];
                do map[j++] = value; while (--count > 0);
            }
            return map;
        }


        /// <summary>
        /// Refills the input buffer.
        /// </summary>
        /// <returns><c>false</c>, iff there was new input.</returns>
        /// <exception cref="IOException">if any I/O-Error occurs</exception>
        private bool ZzRefill()
        {

            /* first: make room (if you can) */
            if (zzStartRead > 0)
            {
                Arrays.Copy(zzBuffer, zzStartRead,
                                 zzBuffer, 0,
                                 zzEndRead - zzStartRead);

                /* translate stored positions */
                zzEndRead -= zzStartRead;
                zzCurrentPos -= zzStartRead;
                zzMarkedPos -= zzStartRead;
                zzStartRead = 0;
            }

            /* is the buffer big enough? */
            if (zzCurrentPos >= zzBuffer.Length)
            {
                /* if not: blow it up */
                char[] newBuffer = new char[zzCurrentPos * 2];
                Arrays.Copy(zzBuffer, 0, newBuffer, 0, zzBuffer.Length);
                zzBuffer = newBuffer;
            }

            /* readonlyly: fill the buffer with new input */
            int numRead = zzReader.Read(zzBuffer, zzEndRead,
                                                    zzBuffer.Length - zzEndRead);

            if (numRead > 0)
            {
                zzEndRead += numRead;
                return false;
            }
            // unlikely but not impossible: read 0 characters, but not at end of stream    
            if (numRead == 0)
            {
                int c = zzReader.Read();
                if (c == -1)
                {
                    return true;
                }
                else
                {
                    zzBuffer[zzEndRead++] = (char)c;
                    return false;
                }
            }

            // numRead < 0
            return true;
        }


        /// <summary>
        /// Disposes the input stream.
        /// </summary>
        public void YyClose()
        {
            zzAtEOF = true;            /* indicate end of file */
            zzEndRead = zzStartRead;  /* invalidate buffer    */

            if (zzReader != null)
            {
                zzReader.Dispose();
            }
        }


        /// <summary>
        /// Resets the scanner to read from a new input stream.
        /// Does not close the old reader.
        /// <para/>
        /// All internal variables are reset, the old input stream 
        /// <b>cannot</b> be reused (internal buffer is discarded and lost).
        /// Lexical state is set to <see cref="YYINITIAL"/>.
        /// <para/>
        /// Internal scan buffer is resized down to its initial length, if it has grown.
        /// </summary>
        /// <param name="reader">the new input stream </param>
        public void YyReset(TextReader reader)
        {
            zzReader = reader;
            //zzAtBOL = true; // LUCENENET: Never read
            zzAtEOF = false;
            //zzEOFDone = false; // LUCENENET: Never read
            zzEndRead = zzStartRead = 0;
            zzCurrentPos = zzMarkedPos = 0;
            //yyline = yychar = yycolumn = 0; // LUCENENET: Never read
            yychar = 0;
            zzLexicalState = YYINITIAL;
            if (zzBuffer.Length > ZZ_BUFFERSIZE)
                zzBuffer = new char[ZZ_BUFFERSIZE];
        }


        /// <summary>
        /// Returns the current lexical state.
        /// </summary>
        public int YyState => zzLexicalState;


        /// <summary>
        /// Enters a new lexical state
        /// </summary>
        /// <param name="newState">the new lexical state</param>
        public void YyBegin(int newState)
        {
            zzLexicalState = newState;
        }


        /// <summary>
        /// Returns the text matched by the current regular expression.
        /// </summary>
        public string YyText => new string(zzBuffer, zzStartRead, zzMarkedPos - zzStartRead);


        /// <summary>
        /// Returns the character at position <paramref name="pos"/> from the 
        /// matched text.
        /// <para/>
        /// It is equivalent to YyText[pos], but faster
        /// </summary>
        /// <param name="pos">
        /// the position of the character to fetch. 
        /// A value from 0 to YyLength-1.
        /// </param>
        /// <returns>the character at position pos</returns>
        public char YyCharAt(int pos)
        {
            return zzBuffer[zzStartRead + pos];
        }


        /// <summary>
        /// Returns the length of the matched text region.
        /// </summary>
        public int YyLength => zzMarkedPos - zzStartRead;


        /// <summary>
        /// Reports an error that occured while scanning.
        /// <para/>
        /// In a wellformed scanner (no or only correct usage of 
        /// YyPushBack(int) and a match-all fallback rule) this method 
        /// will only be called with things that "Can't Possibly Happen".
        /// If this method is called, something is seriously wrong
        /// (e.g. a JFlex bug producing a faulty scanner etc.).
        /// <para/>
        /// Usual syntax/scanner level error handling should be done
        /// in error fallback rules.
        /// </summary>
        /// <param name="errorCode">the code of the errormessage to display</param>
        private static void ZzScanError(int errorCode) // LUCENENET: CA1822: Mark members as static
        {
            string message;
            // LUCENENET specific: Defensive check so we don't have to catch IndexOutOfRangeException
            if (errorCode >= 0 && errorCode < ZZ_ERROR_MSG.Length)
            {
                message = ZZ_ERROR_MSG[errorCode];
            }
            else
            {
                message = ZZ_ERROR_MSG[ZZ_UNKNOWN_ERROR];
            }

            throw Error.Create(message);
        }


        /// <summary>
        /// Pushes the specified amount of characters back into the input stream.
        /// <para/>
        /// They will be read again by then next call of the scanning method
        /// </summary>
        /// <param name="number">
        /// the number of characters to be read again.
        /// This number must not be greater than YyLength!
        /// </param>
        public void YyPushBack(int number)
        {
            if (number > YyLength)
                ZzScanError(ZZ_PUSHBACK_2BIG);

            zzMarkedPos -= number;
        }


        /// <summary>
        /// Resumes scanning until the next regular expression is matched,
        /// the end of input is encountered or an I/O-Error occurs.
        /// </summary>
        /// <returns>the next token</returns>
        /// <exception cref="IOException">if any I/O-Error occurs</exception>
        public int GetNextToken()
        {
            int zzInput;
            int zzAction;

            // cached fields:
            int zzCurrentPosL;
            int zzMarkedPosL;
            int zzEndReadL = zzEndRead;
            char[] zzBufferL = zzBuffer;
            char[] zzCMapL = ZZ_CMAP;

            int[] zzTransL = ZZ_TRANS;
            int[] zzRowMapL = ZZ_ROWMAP;
            int[] zzAttrL = ZZ_ATTRIBUTE;

            while (true)
            {
                zzMarkedPosL = zzMarkedPos;

                yychar += zzMarkedPosL - zzStartRead;

                zzAction = -1;

                zzCurrentPosL = zzCurrentPos = zzStartRead = zzMarkedPosL;

                zzState = ZZ_LEXSTATE[zzLexicalState];

                // set up zzAction for empty match case:
                int zzAttributes = zzAttrL[zzState];
                if ((zzAttributes & 1) == 1)
                {
                    zzAction = zzState;
                }



                while (true)
                {

                    if (zzCurrentPosL < zzEndReadL)
                        zzInput = zzBufferL[zzCurrentPosL++];
                    else if (zzAtEOF)
                    {
                        zzInput = YYEOF;
                        goto zzForActionBreak;
                    }
                    else
                    {
                        // store back cached positions
                        zzCurrentPos = zzCurrentPosL;
                        zzMarkedPos = zzMarkedPosL;
                        bool eof = ZzRefill();
                        // get translated positions and possibly new buffer
                        zzCurrentPosL = zzCurrentPos;
                        zzMarkedPosL = zzMarkedPos;
                        zzBufferL = zzBuffer;
                        zzEndReadL = zzEndRead;
                        if (eof)
                        {
                            zzInput = YYEOF;
                            goto zzForActionBreak;
                        }
                        else
                        {
                            zzInput = zzBufferL[zzCurrentPosL++];
                        }
                    }
                    int zzNext = zzTransL[zzRowMapL[zzState] + zzCMapL[zzInput]];
                    if (zzNext == -1) goto zzForActionBreak;
                    zzState = zzNext;

                    zzAttributes = zzAttrL[zzState];
                    if ((zzAttributes & 1) == 1)
                    {
                        zzAction = zzState;
                        zzMarkedPosL = zzCurrentPosL;
                        if ((zzAttributes & 8) == 8) goto zzForActionBreak;
                    }

                }
                zzForActionBreak:

                // store back cached position
                zzMarkedPos = zzMarkedPosL;

                switch (zzAction < 0 ? zzAction : ZZ_ACTION[zzAction])
                {
                    case 1:
                        {
                            numWikiTokensSeen = 0; positionInc = 1; /* Break so we don't hit fall-through warning: */ break;
                        }
                    case 47: break;
                    case 2:
                        {
                            positionInc = 1; return ALPHANUM;
                        }
                    case 48: break;
                    case 3:
                        {
                            positionInc = 1; return CJ;
                        }
                    case 49: break;
                    case 4:
                        {
                            numWikiTokensSeen = 0; positionInc = 1; currentTokType = EXTERNAL_LINK_URL; YyBegin(EXTERNAL_LINK_STATE);/* Break so we don't hit fall-through warning: */ break;
                        }
                    case 50: break;
                    case 5:
                        {
                            positionInc = 1; /* Break so we don't hit fall-through warning: */ break;
                        }
                    case 51: break;
                    case 6:
                        {
                            YyBegin(CATEGORY_STATE); numWikiTokensSeen++; return currentTokType;
                        }
                    case 52: break;
                    case 7:
                        {
                            YyBegin(INTERNAL_LINK_STATE); numWikiTokensSeen++; return currentTokType;
                        }
                    case 53: break;
                    case 8:
                        { /* Break so we don't hit fall-through warning: */
                            break;/* ignore */
                        }
                    case 54: break;
                    case 9:
                        {
                            if (numLinkToks == 0) { positionInc = 0; } else { positionInc = 1; }
                            numWikiTokensSeen++; currentTokType = EXTERNAL_LINK; YyBegin(EXTERNAL_LINK_STATE); numLinkToks++; return currentTokType;
                        }
                    case 55: break;
                    case 10:
                        {
                            numLinkToks = 0; positionInc = 0; YyBegin(YYINITIAL); /* Break so we don't hit fall-through warning: */ break;
                        }
                    case 56: break;
                    case 11:
                        {
                            currentTokType = BOLD; YyBegin(THREE_SINGLE_QUOTES_STATE); /* Break so we don't hit fall-through warning: */ break;
                        }
                    case 57: break;
                    case 12:
                        {
                            currentTokType = ITALICS; numWikiTokensSeen++; YyBegin(STRING); return currentTokType;/*italics*/
                        }
                    case 58: break;
                    case 13:
                        {
                            currentTokType = EXTERNAL_LINK; numWikiTokensSeen = 0; YyBegin(EXTERNAL_LINK_STATE); /* Break so we don't hit fall-through warning: */ break;
                        }
                    case 59: break;
                    case 14:
                        {
                            YyBegin(STRING); numWikiTokensSeen++; return currentTokType;
                        }
                    case 60: break;
                    case 15:
                        {
                            currentTokType = SUB_HEADING; numWikiTokensSeen = 0; YyBegin(STRING); /* Break so we don't hit fall-through warning: */ break;
                        }
                    case 61: break;
                    case 16:
                        {
                            currentTokType = HEADING; YyBegin(DOUBLE_EQUALS_STATE); numWikiTokensSeen++; return currentTokType;
                        }
                    case 62: break;
                    case 17:
                        {
                            YyBegin(DOUBLE_BRACE_STATE); numWikiTokensSeen = 0; return currentTokType;
                        }
                    case 63: break;
                    case 18:
                        { /* Break so we don't hit fall-through warning: */
                            break;/* ignore STRING */
                        }
                    case 64: break;
                    case 19:
                        {
                            YyBegin(STRING); numWikiTokensSeen++; return currentTokType;/* STRING ALPHANUM*/
                        }
                    case 65: break;
                    case 20:
                        {
                            numBalanced = 0; numWikiTokensSeen = 0; currentTokType = EXTERNAL_LINK; YyBegin(EXTERNAL_LINK_STATE); /* Break so we don't hit fall-through warning: */ break;
                        }
                    case 66: break;
                    case 21:
                        {
                            YyBegin(STRING); return currentTokType;/*pipe*/
                        }
                    case 67: break;
                    case 22:
                        {
                            numWikiTokensSeen = 0; positionInc = 1; if (numBalanced == 0) { numBalanced++; YyBegin(TWO_SINGLE_QUOTES_STATE); } else { numBalanced = 0; }/* Break so we don't hit fall-through warning: */
                            break;
                        }
                    case 68: break;
                    case 23:
                        {
                            numWikiTokensSeen = 0; positionInc = 1; YyBegin(DOUBLE_EQUALS_STATE);/* Break so we don't hit fall-through warning: */ break;
                        }
                    case 69: break;
                    case 24:
                        {
                            numWikiTokensSeen = 0; positionInc = 1; currentTokType = INTERNAL_LINK; YyBegin(INTERNAL_LINK_STATE);/* Break so we don't hit fall-through warning: */ break;
                        }
                    case 70: break;
                    case 25:
                        {
                            numWikiTokensSeen = 0; positionInc = 1; currentTokType = CITATION; YyBegin(DOUBLE_BRACE_STATE);/* Break so we don't hit fall-through warning: */ break;
                        }
                    case 71: break;
                    case 26:
                        {
                            YyBegin(YYINITIAL);/* Break so we don't hit fall-through warning: */ break;
                        }
                    case 72: break;
                    case 27:
                        {
                            numLinkToks = 0; YyBegin(YYINITIAL); /* Break so we don't hit fall-through warning: */ break;
                        }
                    case 73: break;
                    case 28:
                        {
                            currentTokType = INTERNAL_LINK; numWikiTokensSeen = 0; YyBegin(INTERNAL_LINK_STATE); /* Break so we don't hit fall-through warning: */ break;
                        }
                    case 74: break;
                    case 29:
                        {
                            currentTokType = INTERNAL_LINK; numWikiTokensSeen = 0; YyBegin(INTERNAL_LINK_STATE); /* Break so we don't hit fall-through warning: */ break;
                        }
                    case 75: break;
                    case 30:
                        {
                            YyBegin(YYINITIAL); /* Break so we don't hit fall-through warning: */ break;
                        }
                    case 76: break;
                    case 31:
                        {
                            numBalanced = 0; currentTokType = ALPHANUM; YyBegin(YYINITIAL); /* Break so we don't hit fall-through warning: */ break;/*end italics*/
                        }
                    case 77: break;
                    case 32:
                        {
                            numBalanced = 0; numWikiTokensSeen = 0; currentTokType = INTERNAL_LINK; YyBegin(INTERNAL_LINK_STATE); /* Break so we don't hit fall-through warning: */ break;
                        }
                    case 78: break;
                    case 33:
                        {
                            positionInc = 1; return APOSTROPHE;
                        }
                    case 79: break;
                    case 34:
                        {
                            positionInc = 1; return HOST;
                        }
                    case 80: break;
                    case 35:
                        {
                            positionInc = 1; return NUM;
                        }
                    case 81: break;
                    case 36:
                        {
                            positionInc = 1; return COMPANY;
                        }
                    case 82: break;
                    case 37:
                        {
                            currentTokType = BOLD_ITALICS; YyBegin(FIVE_SINGLE_QUOTES_STATE); /* Break so we don't hit fall-through warning: */ break;
                        }
                    case 83: break;
                    case 38:
                        {
                            numBalanced = 0; currentTokType = ALPHANUM; YyBegin(YYINITIAL); /* Break so we don't hit fall-through warning: */ break;/*end bold*/
                        }
                    case 84: break;
                    case 39:
                        {
                            numBalanced = 0; currentTokType = ALPHANUM; YyBegin(YYINITIAL); /* Break so we don't hit fall-through warning: */ break;/*end sub header*/
                        }
                    case 85: break;
                    case 40:
                        {
                            positionInc = 1; return ACRONYM;
                        }
                    case 86: break;
                    case 41:
                        {
                            positionInc = 1; return EMAIL;
                        }
                    case 87: break;
                    case 42:
                        {
                            numBalanced = 0; currentTokType = ALPHANUM; YyBegin(YYINITIAL); /* Break so we don't hit fall-through warning: */ break;/*end bold italics*/
                        }
                    case 88: break;
                    case 43:
                        {
                            positionInc = 1; numWikiTokensSeen++; YyBegin(EXTERNAL_LINK_STATE); return currentTokType;
                        }
                    case 89: break;
                    case 44:
                        {
                            numWikiTokensSeen = 0; positionInc = 1; currentTokType = CATEGORY; YyBegin(CATEGORY_STATE);/* Break so we don't hit fall-through warning: */ break;
                        }
                    case 90: break;
                    case 45:
                        {
                            currentTokType = CATEGORY; numWikiTokensSeen = 0; YyBegin(CATEGORY_STATE); /* Break so we don't hit fall-through warning: */ break;
                        }
                    case 91: break;
                    case 46:
                        {
                            numBalanced = 0; numWikiTokensSeen = 0; currentTokType = CATEGORY; YyBegin(CATEGORY_STATE); /* Break so we don't hit fall-through warning: */ break;
                        }
                    case 92: break;
                    default:
                        if (zzInput == YYEOF && zzStartRead == zzCurrentPos)
                        {
                            zzAtEOF = true;
                            return YYEOF;
                        }
                        else
                        {
                            ZzScanError(ZZ_NO_MATCH);
                        }
                        break;
                }
            }
        }
    }
}