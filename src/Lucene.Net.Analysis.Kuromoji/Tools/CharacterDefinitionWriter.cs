using Lucene.Net.Analysis.Ja.Dict;
using Lucene.Net.Codecs;
using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.IO;

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

    public sealed class CharacterDefinitionWriter
    {
        private readonly byte[] characterCategoryMap = new byte[0x10000];

        private readonly bool[] invokeMap = new bool[CharacterDefinition.CLASS_COUNT];
        private readonly bool[] groupMap = new bool[CharacterDefinition.CLASS_COUNT];

        /// <summary>
        /// Constructor for building. TODO: remove write access
        /// </summary>
        public CharacterDefinitionWriter()
        {
            Arrays.Fill(characterCategoryMap, CharacterDefinition.DEFAULT);
        }

        /// <summary>
        /// Put mapping from unicode code point to character class.
        /// </summary>
        /// <param name="codePoint">Code point.</param>
        /// <param name="characterClassName">Character class name.</param>
        public void PutCharacterCategory(int codePoint, string characterClassName)
        {
            characterClassName = characterClassName.Split(' ')[0]; // use first
                                                                    // category
                                                                    // class

            // Override Nakaguro
            if (codePoint == 0x30FB)
            {
                characterClassName = "SYMBOL";
            }
            characterCategoryMap[codePoint] = CharacterDefinition.LookupCharacterClass(characterClassName);
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public void PutInvokeDefinition(string characterClassName, int invoke, int group, int length)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            byte characterClass = CharacterDefinition.LookupCharacterClass(characterClassName);
            invokeMap[characterClass] = invoke == 1;
            groupMap[characterClass] = group == 1;
            // TODO: length def ignored
        }

        public void Write(string baseDir)
        {
            //string filename = baseDir + System.IO.Path.DirectorySeparatorChar +
            //    typeof(CharacterDefinition).FullName.Replace('.', System.IO.Path.DirectorySeparatorChar) + CharacterDefinition.FILENAME_SUFFIX;

            // LUCENENET specific: we don't need to do a "classpath" output directory, since we
            // are changing the implementation to read files dynamically instead of making the
            // user recompile with the new files.
            string filename = System.IO.Path.Combine(baseDir, typeof(CharacterDefinition).Name + CharacterDefinition.FILENAME_SUFFIX);
            //new File(filename).getParentFile().mkdirs();
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(baseDir));
            using Stream os = new FileStream(filename, FileMode.Create, FileAccess.Write);
            DataOutput @out = new OutputStreamDataOutput(os);
            CodecUtil.WriteHeader(@out, CharacterDefinition.HEADER, CharacterDefinition.VERSION);
            @out.WriteBytes(characterCategoryMap, 0, characterCategoryMap.Length);
            for (int i = 0; i < CharacterDefinition.CLASS_COUNT; i++)
            {
                byte b = (byte)(
                  (invokeMap[i] ? 0x01 : 0x00) |
                  (groupMap[i] ? 0x02 : 0x00)
                );
                @out.WriteByte(b);
            }
        }
    }
}
