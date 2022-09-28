using System;

namespace Lucene.Net.Analysis.Ko.Dict
{

    /// <summary>
    /// Dictionary for unknown-word handling.
    /// </summary>
    public class UnknownDictionary : BinaryDictionary
    {
        private readonly CharacterDefinition characterDefinition = CharacterDefinition.Instance;

        private UnknownDictionary()
        {
        }

        public virtual int Lookup(char[] text, int offset, int len)
        {
            if (!characterDefinition.IsGroup(text[offset]))
            {
                return 1;
            }

            // Extract unknown word. Characters with the same character class are considered to be part of unknown word
            byte characterIdOfFirstCharacter = characterDefinition.GetCharacterClass(text[offset]);
            int length = 1;
            for (int i = 1; i < len; i++)
            {
                if (characterIdOfFirstCharacter == characterDefinition.GetCharacterClass(text[offset + i]))
                {
                    length++;
                }
                else
                {
                    break;
                }
            }

            return length;
        }

        public virtual CharacterDefinition CharacterDefinition => characterDefinition;

        public override string GetReading(int wordId)
        {
            return null;
        }

        public override IDictionary.Morpheme[] GetMorphemes(int wordId, char[] surfaceForm, int off, int len)
        {
            return null;
        }

        public static UnknownDictionary Instance => SingletonHolder.INSTANCE;

        private class SingletonHolder
        {
            internal static readonly UnknownDictionary INSTANCE = LoadInstance();
            private static UnknownDictionary LoadInstance() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
            {
                try
                {
                    return new UnknownDictionary();
                }
                catch (Exception ioe) when (ioe.IsIOException())
                {
                    throw RuntimeException.Create("Cannot load UnknownDictionary.", ioe);
                }
            }
        }
    }
}
