using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Search.Spell
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

    public class TestPlainTextDictionary : LuceneTestCase
    {
        [Test]
        public void TestBuild()
        {
            string LF = Environment.NewLine;
            string input = "oneword" + LF + "twoword" + LF + "threeword";
            PlainTextDictionary ptd = new PlainTextDictionary(new StringReader(input));
            Store.Directory ramDir = NewDirectory();
            SpellChecker spellChecker = new SpellChecker(ramDir);
            spellChecker.IndexDictionary(ptd, NewIndexWriterConfig(TEST_VERSION_CURRENT, null), false);
            string[] similar = spellChecker.SuggestSimilar("treeword", 2);
            assertEquals(2, similar.Length);
            assertEquals(similar[0], "threeword");
            assertEquals(similar[1], "oneword");
            spellChecker.Dispose();
            ramDir.Dispose();
        }
    }
}
