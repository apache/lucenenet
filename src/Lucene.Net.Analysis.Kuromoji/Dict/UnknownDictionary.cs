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

        public override string GetReading(int wordId, char[] surface, int off, int len)
        {
            return null;
        }

        public override string GetInflectionType(int wordId)
        {
            return null;
        }

        public override string GetInflectionForm(int wordId)
        {
            return null;
        }

        public static UnknownDictionary Instance => SingletonHolder.INSTANCE;

        private static class SingletonHolder
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
