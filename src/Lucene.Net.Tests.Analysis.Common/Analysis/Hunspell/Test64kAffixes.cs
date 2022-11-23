// Lucene version compatibility level 4.10.4
using J2N;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
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

    // Tests that > 64k affixes actually works and doesnt overflow some internal int
    public class Test64kAffixes : LuceneTestCase
    {
        [Test]
        public void Test()
        {
            DirectoryInfo tempDir = CreateTempDir("64kaffixes");
            FileInfo affix = new FileInfo(System.IO.Path.Combine(tempDir.FullName, "64kaffixes.aff"));
            FileInfo dict = new FileInfo(System.IO.Path.Combine(tempDir.FullName, "64kaffixes.dic"));

            using var affixWriter = new StreamWriter(
                new FileStream(affix.FullName, FileMode.OpenOrCreate), Encoding.UTF8);

            // 65k affixes with flag 1, then an affix with flag 2
            affixWriter.Write("SET UTF-8\nFLAG num\nSFX 1 Y 65536\n");
            for (int i = 0; i < 65536; i++)
            {
                affixWriter.Write("SFX 1 0 " + i.ToHexString() + " .\n");
            }
            affixWriter.Write("SFX 2 Y 1\nSFX 2 0 s\n");
            affixWriter.Dispose();

            using var dictWriter = new StreamWriter(
                new FileStream(dict.FullName, FileMode.OpenOrCreate), Encoding.UTF8);


            // drink signed with affix 2 (takes -s)
            dictWriter.Write("1\ndrink/2\n");
            dictWriter.Dispose();

            using Stream affStream = new FileStream(affix.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            using Stream dictStream = new FileStream(dict.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite);

            Dictionary dictionary = new Dictionary(affStream, dictStream);
            Stemmer stemmer = new Stemmer(dictionary);
            // drinks should still stem to drink
            IList<CharsRef> stems = stemmer.Stem("drinks");
            assertEquals(1, stems.size());
            assertEquals("drink", stems[0].ToString());
        }
    }
}
