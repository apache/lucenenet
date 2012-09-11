/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.IO;
using System.Text;

using NUnit.Framework;

using SpellChecker.Net.Search.Spell;

using Lucene.Net.Store;

namespace SpellChecker.Net.Test.Search.Spell
{
    [TestFixture]
    public class TestPlainTextDictionary
    {
        [Test]
        public void TestBuild()
        {

            var LF = Environment.NewLine;
            var input = "oneword" + LF + "twoword" + LF + "threeword";
            var ptd = new PlainTextDictionary( new MemoryStream( Encoding.UTF8.GetBytes(input)) );
            var ramDir = new RAMDirectory();
            var spellChecker = new Net.Search.Spell.SpellChecker(ramDir);
            spellChecker.IndexDictionary(ptd);
            String[] similar = spellChecker.SuggestSimilar("treeword", 2);
            Assert.AreEqual(2, similar.Length);
            Assert.AreEqual(similar[0], "threeword");
            Assert.AreEqual(similar[1], "twoword");
        }
    }
}
