using Lucene.Net.Search.Suggest.Fst;
using Lucene.Net.Search.Suggest.Jaspell;
using Lucene.Net.Search.Suggest.Tst;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Search.Suggest
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

    public class PersistenceTest : LuceneTestCase
    {
        public readonly string[] keys = new string[] {
            "one",
            "two",
            "three",
            "four",
            "oneness",
            "onerous",
            "onesimus",
            "twofold",
            "twonk",
            "thrive",
            "through",
            "threat",
            "foundation",
            "fourier",
            "fourty"
        };

        [Test]
        public void TestTSTPersistence()
        {
            RunTest(typeof(TSTLookup), true);
        }

        [Test]
        public void TestJaspellPersistence()
        {
            RunTest(typeof(JaspellLookup), true);
        }

        [Test]
        public void TestFSTPersistence()
        {
            RunTest(typeof(FSTCompletionLookup), false);
        }

        private void RunTest(Type lookupClass,
            bool supportsExactWeights)
        {

            // Add all input keys.
            Lookup lookup = (Lookup)Activator.CreateInstance(lookupClass);
            Input[] keys = new Input[this.keys.Length];
            for (int i = 0; i < keys.Length; i++)
                keys[i] = new Input(this.keys[i], i);
            lookup.Build(new InputArrayEnumerator(keys));

            // Store the suggester.
            DirectoryInfo storeDir = CreateTempDir(this.GetType().Name);
            lookup.Store(new FileStream(Path.Combine(storeDir.FullName, "lookup.dat"), FileMode.OpenOrCreate));

            // Re-read it from disk.
            lookup = (Lookup)Activator.CreateInstance(lookupClass);
            lookup.Load(new FileStream(Path.Combine(storeDir.FullName, "lookup.dat"), FileMode.Open));

            // Assert validity.
            Random random = Random;
            long previous = long.MinValue;
            foreach (Input k in keys)
            {
                IList<Lookup.LookupResult> list = lookup.DoLookup(TestUtil.BytesToCharSequence(k.term, random).ToString(), false, 1);
                assertEquals(1, list.size());
                Lookup.LookupResult lookupResult = list[0];
                assertNotNull(k.term.Utf8ToString(), lookupResult.Key);

                if (supportsExactWeights)
                {
                    assertEquals(k.term.Utf8ToString(), k.v, lookupResult.Value);
                }
                else
                {
                    assertTrue(lookupResult.Value + ">=" + previous, lookupResult.Value >= previous);
                    previous = lookupResult.Value;
                }
            }
        }
    }
}
