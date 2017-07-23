using Lucene.Net.Analysis.Ja.Dict;

namespace Lucene.Net.Analysis.Ja.Util
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

    public class UnknownDictionaryWriter : BinaryDictionaryWriter
    {
        private readonly CharacterDefinitionWriter characterDefinition = new CharacterDefinitionWriter();

        public UnknownDictionaryWriter(int size)
            : base(typeof(UnknownDictionary), size)
        {
        }

        public override int Put(string[] entry)
        {
            // Get wordId of current entry
            int wordId = m_buffer.Position;

            // Put entry
            int result = base.Put(entry);

            // Put entry in targetMap
            int characterId = CharacterDefinition.LookupCharacterClass(entry[0]);
            AddMapping(characterId, wordId);
            return result;
        }

        /// <summary>
        /// Put mapping from unicode code point to character class.
        /// </summary>
        /// <param name="codePoint">Code point.</param>
        /// <param name="characterClassName">Character class name.</param>
        public virtual void PutCharacterCategory(int codePoint, string characterClassName)
        {
            characterDefinition.PutCharacterCategory(codePoint, characterClassName);
        }

        public virtual void PutInvokeDefinition(string characterClassName, int invoke, int group, int length)
        {
            characterDefinition.PutInvokeDefinition(characterClassName, invoke, group, length);
        }

        public override void Write(string baseDir)
        {
            base.Write(baseDir);
            characterDefinition.Write(baseDir);
        }
    }
}
