using Lucene.Net.Codecs;
using Lucene.Net.Store;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Ko.Dict
{
    public sealed class CharacterDefinition
    {
        public static readonly string FILENAME_SUFFIX = ".dat";
        public static readonly string HEADER = "ko_cd";
        public static readonly int VERSION = 1;

        public static readonly int CLASS_COUNT = Enum.GetValues(typeof(CharacterClass)).Length;

        // only used internally for lookup:
        private enum CharacterClass : byte
        {
            NGRAM, DEFAULT, SPACE, SYMBOL, NUMERIC, ALPHA, CYRILLIC, GREEK, HIRAGANA, KATAKANA, KANJI, HANGUL, HANJA, HANJANUMERIC
        }

        private readonly byte[] characterCategoryMap = new byte[0x10000];

        private readonly bool[] invokeMap = new bool[CLASS_COUNT];
        private readonly bool[] groupMap = new bool[CLASS_COUNT];

        // the classes:
        public static readonly byte NGRAM = (byte)CharacterClass.NGRAM;
        public static readonly byte DEFAULT = (byte)CharacterClass.DEFAULT;
        public static readonly byte SPACE = (byte)CharacterClass.SPACE;
        public static readonly byte SYMBOL = (byte)CharacterClass.SYMBOL;
        public static readonly byte NUMERIC = (byte)CharacterClass.NUMERIC;
        public static readonly byte ALPHA = (byte)CharacterClass.ALPHA;
        public static readonly byte CYRILLIC = (byte)CharacterClass.CYRILLIC;
        public static readonly byte GREEK = (byte)CharacterClass.GREEK;
        public static readonly byte HIRAGANA = (byte)CharacterClass.HIRAGANA;
        public static readonly byte KATAKANA = (byte)CharacterClass.KATAKANA;
        public static readonly byte KANJI = (byte)CharacterClass.KANJI;
        public static readonly byte HANGUL = (byte)CharacterClass.HANGUL;
        public static readonly byte HANJA = (byte)CharacterClass.HANJA;
        public static readonly byte HANJANUMERIC = (byte)CharacterClass.HANJANUMERIC;


        private CharacterDefinition()
        {
            using Stream @is = BinaryDictionary.GetTypeResource(GetType(), FILENAME_SUFFIX);
            DataInput @in = new InputStreamDataInput(@is);
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

        public bool IsHanja(char c)
        {
            byte characterClass = characterCategoryMap[c];
            return characterClass == HANJA || characterClass == HANJANUMERIC;
        }

        public bool IsHangul(char c) {
            return GetCharacterClass(c) == HANGUL;
        }

        public bool HasCoda(char ch){
            return ((ch - 0xAC00) % 0x001C) != 0;
        }

        public static byte LookupCharacterClass(string characterClassName)
        {
            return (byte)Enum.Parse(typeof(CharacterClass), characterClassName, true);
        }

        public static CharacterDefinition Instance => SingletonHolder.INSTANCE;

        private class SingletonHolder
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