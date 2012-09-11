/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System.Linq;
using Lucene.Net.Analysis.Hunspell;
using NUnit.Framework;

namespace Lucene.Net.Analyzers.Hunspell {
    [TestFixture]
    public class TestHunspellStemmer {
        [Test(Description = "Word 'drinkable' should be stemmed to 'drink' with the suffix 'able' being stripped")]
        public void TestStem_SimpleSuffix_EnUS() {
            var dictionary = HunspellDictionaryLoader.Dictionary("en_US");

            var stemmer = new HunspellStemmer(dictionary);
            var stems = stemmer.Stem("drinkable").ToList();

            Assert.AreEqual(2, stems.Count);
            Assert.AreEqual("drinkable", stems[0].Stem);
            Assert.AreEqual("drink", stems[1].Stem);
        }

        [Test(Description = "Word 'remove' should be stemmed to 'move' with the prefix 're' being stripped")]
        public void TestStem_SimplePrefix_EnUS() {
            var dictionary = HunspellDictionaryLoader.Dictionary("en_US");

            var stemmer = new HunspellStemmer(dictionary);
            var stems = stemmer.Stem("remove").ToList();

            Assert.AreEqual(1, stems.Count);
            Assert.AreEqual("move", stems[0].Stem);
        }

        [Test(Description = "Word 'drinkables' should be stemmed to 'drink' with the suffixes 's' and 'able' being removed recursively")]
        public void TestStem_RecursiveSuffix_EnUS() {
            var dictionary = HunspellDictionaryLoader.Dictionary("en_US");

            var stemmer = new HunspellStemmer(dictionary);
            var stems = stemmer.Stem("drinkables").ToList();

            Assert.AreEqual(1, stems.Count);
            Assert.AreEqual("drink", stems[0].Stem);
        }

        [Test(Description = "Word 'fietsen' should be stemmed to 'fiets' ('en' suffix stripped) while fiets should be stemmed to itself")]
        public void TestStem_fietsenFiets_NlNL() {
            var dictionary = HunspellDictionaryLoader.Dictionary("nl_NL");

            var stemmer = new HunspellStemmer(dictionary);
            var stems = stemmer.Stem("fietsen").ToList();

            Assert.AreEqual(2, stems.Count);
            Assert.AreEqual("fietsen", stems[0].Stem);
            Assert.AreEqual("fiets", stems[1].Stem);

            stems = stemmer.Stem("fiets").ToList();
            Assert.AreEqual(1, stems.Count);
            Assert.AreEqual("fiets", stems[0].Stem);
        }

        [Test(Description = "Word 'huizen' should be stemmed to 'huis' ('en' suffix stripped) while huis should be stemmed to huis and hui")]
        public void TestStem_huizenHuis_NlNL() {
            var dictionary = HunspellDictionaryLoader.Dictionary("nl_NL");

            var stemmer = new HunspellStemmer(dictionary);
            var stems = stemmer.Stem("huizen").ToList();

            Assert.AreEqual(2, stems.Count);
            Assert.AreEqual("huizen", stems[0].Stem);
            Assert.AreEqual("huis", stems[1].Stem);

            stems = stemmer.Stem("huis").ToList();
            Assert.AreEqual(2, stems.Count);
            Assert.AreEqual("huis", stems[0].Stem);
            Assert.AreEqual("hui", stems[1].Stem);
        }
    }
}