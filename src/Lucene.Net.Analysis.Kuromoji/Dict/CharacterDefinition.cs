using Lucene.Net.Codecs;
using Lucene.Net.Store;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Ja.Dict
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
    /// Character category data.
    /// </summary>
    public sealed class CharacterDefinition
    {
        public const string FILENAME_SUFFIX = ".dat";
        public const string HEADER = "kuromoji_cd";
        // ReSharper disable once ConvertToConstant.Global - VERSION should be a field
        public static readonly int VERSION = 1;

        public static readonly int CLASS_COUNT = Enum.GetValues(typeof(CharacterClass)).Length;

        // only used internally for lookup:
        private enum CharacterClass : byte
        {
            NGRAM, DEFAULT, SPACE, SYMBOL, NUMERIC, ALPHA, CYRILLIC, GREEK, HIRAGANA, KATAKANA, KANJI, KANJINUMERIC
        }

        private readonly byte[] characterCategoryMap = new byte[0x10000];

        private readonly bool[] invokeMap = new bool[CLASS_COUNT];
        private readonly bool[] groupMap = new bool[CLASS_COUNT];

        // the classes:
        public const byte NGRAM = (byte)CharacterClass.NGRAM;
        public const byte DEFAULT = (byte)CharacterClass.DEFAULT;
        public const byte SPACE = (byte)CharacterClass.SPACE;
        public const byte SYMBOL = (byte)CharacterClass.SYMBOL;
        public const byte NUMERIC = (byte)CharacterClass.NUMERIC;
        public const byte ALPHA = (byte)CharacterClass.ALPHA;
        public const byte CYRILLIC = (byte)CharacterClass.CYRILLIC;
        public const byte GREEK = (byte)CharacterClass.GREEK;
        public const byte HIRAGANA = (byte)CharacterClass.HIRAGANA;
        public const byte KATAKANA = (byte)CharacterClass.KATAKANA;
        public const byte KANJI = (byte)CharacterClass.KANJI;
        public const byte KANJINUMERIC = (byte)CharacterClass.KANJINUMERIC;

        private CharacterDefinition()
        {
            using Stream @is = BinaryDictionary.GetTypeResource(GetType(), FILENAME_SUFFIX);
            using var @in = new InputStreamDataInput(@is, leaveOpen: true); // LUCENENET: CA2000: Use using statement
            CodecUtil.CheckHeader(@in, HEADER, VERSION, VERSION);
            @in.ReadBytes(characterCategoryMap, 0, characterCategoryMap.Length);
            for (int i = 0; i < CLASS_COUNT; i++)
            {
                byte b = @in.ReadByte();
                invokeMap[i] = (b & 0x01) != 0;
                groupMap[i] = (b & 0x02) != 0;
            }
        }

        public byte GetCharacterClass(char c)
        {
            return characterCategoryMap[c];
        }

        public bool IsInvoke(char c)
        {
            return invokeMap[characterCategoryMap[c]];
        }

        public bool IsGroup(char c)
        {
            return groupMap[characterCategoryMap[c]];
        }

        public bool IsKanji(char c)
        {
            byte characterClass = characterCategoryMap[c];
            return characterClass == KANJI || characterClass == KANJINUMERIC;
        }

        public static byte LookupCharacterClass(string characterClassName)
        {
            return (byte)Enum.Parse(typeof(CharacterClass), characterClassName, true);
        }

        public static CharacterDefinition Instance => SingletonHolder.INSTANCE;

        private static class SingletonHolder
        {
            internal static readonly CharacterDefinition INSTANCE = LoadInstance();
            private static CharacterDefinition LoadInstance() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
            {
                try
                {
                    return new CharacterDefinition();
                }
                catch (Exception ioe) when (ioe.IsIOException())
                {
                    throw RuntimeException.Create("Cannot load CharacterDefinition.", ioe);
                }
            }
        }
    }
}
