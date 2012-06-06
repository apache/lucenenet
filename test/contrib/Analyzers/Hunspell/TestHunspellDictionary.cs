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
using NUnit.Framework;

namespace Lucene.Net.Analyzers.Hunspell {
    [TestFixture]
    public class TestHunspellDictionary {
        [Test(Description = "en_US affix and dict files are loaded without error, with 2 suffixes for 'ings' being loaded, 2 prefixes for 'in' and 1 word for 'drink' ")]
        public void TestHunspellDictionary_LoadEnUSDict() {
            var dictionary = HunspellDictionaryLoader.Dictionary("en_US");

            Assert.AreEqual(2, dictionary.LookupSuffix(new[] { 'i', 'n', 'g', 's' }, 0, 4).Count());
            Assert.AreEqual(1, dictionary.LookupPrefix(new[] { 'i', 'n' }, 0, 2).Count());
            Assert.AreEqual(1, dictionary.LookupWord("drink").Count());
        }

        [Test(Description = "fr-moderne affix and dict files are loaded without error")]
        public void TestHunspellDictionary_LoadFrModerneDict() {
            Assert.DoesNotThrow(() => HunspellDictionaryLoader.Dictionary("fr-moderne"));
        }
    }
}