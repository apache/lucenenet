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

using NUnit.Framework;

using SpellChecker.Net.Search.Spell;

namespace SpellChecker.Net.Test.Search.Spell
{
    [TestFixture]
    public class TestNGramDistance
    {
        [Test]
        public void TestGetDistance1()
        {
            StringDistance nsd = new NGramDistance(1);
            float d = nsd.GetDistance("al", "al");
            Assert.AreEqual(d, 1.0f, 0.001);
            d = nsd.GetDistance("a", "a");
            Assert.AreEqual(d, 1.0f, 0.001);
            d = nsd.GetDistance("b", "a");
            Assert.AreEqual(d, 0.0f, 0.001);
            d = nsd.GetDistance("martha", "marhta");
            Assert.AreEqual(d, 0.6666, 0.001);
            d = nsd.GetDistance("jones", "johnson");
            Assert.AreEqual(d, 0.4285, 0.001);
            d = nsd.GetDistance("natural", "contrary");
            Assert.AreEqual(d, 0.25, 0.001);
            d = nsd.GetDistance("abcvwxyz", "cabvwxyz");
            Assert.AreEqual(d, 0.75, 0.001);
            d = nsd.GetDistance("dwayne", "duane");
            Assert.AreEqual(d, 0.666, 0.001);
            d = nsd.GetDistance("dixon", "dicksonx");
            Assert.AreEqual(d, 0.5, 0.001);
            d = nsd.GetDistance("six", "ten");
            Assert.AreEqual(d, 0, 0.001);
            float d1 = nsd.GetDistance("zac ephron", "zac efron");
            float d2 = nsd.GetDistance("zac ephron", "kai ephron");
            Assert.AreEqual(d1, d2, 0.001);
            d1 = nsd.GetDistance("brittney spears", "britney spears");
            d2 = nsd.GetDistance("brittney spears", "brittney startzman");
            Assert.IsTrue(d1 > d2);
            d1 = nsd.GetDistance("12345678", "12890678");
            d2 = nsd.GetDistance("12345678", "72385698");
            Assert.AreEqual(d1, d2, 001);
        }

        [Test]
        public void TestGetDistance2()
        {
            StringDistance sd = new NGramDistance(2);
            float d = sd.GetDistance("al", "al");
            Assert.AreEqual(d, 1.0f, 0.001);
            d = sd.GetDistance("a", "a");
            Assert.AreEqual(d, 1.0f, 0.001);
            d = sd.GetDistance("b", "a");
            Assert.AreEqual(d, 0.0f, 0.001);
            d = sd.GetDistance("a", "aa");
            Assert.AreEqual(d, 0.5f, 0.001);
            d = sd.GetDistance("martha", "marhta");
            Assert.AreEqual(d, 0.6666, 0.001);
            d = sd.GetDistance("jones", "johnson");
            Assert.AreEqual(d, 0.4285, 0.001);
            d = sd.GetDistance("natural", "contrary");
            Assert.AreEqual(d, 0.25, 0.001);
            d = sd.GetDistance("abcvwxyz", "cabvwxyz");
            Assert.AreEqual(d, 0.625, 0.001);
            d = sd.GetDistance("dwayne", "duane");
            Assert.AreEqual(d, 0.5833, 0.001);
            d = sd.GetDistance("dixon", "dicksonx");
            Assert.AreEqual(d, 0.5, 0.001);
            d = sd.GetDistance("six", "ten");
            Assert.AreEqual(d, 0, 0.001);
            float d1 = sd.GetDistance("zac ephron", "zac efron");
            float d2 = sd.GetDistance("zac ephron", "kai ephron");
            Assert.IsTrue(d1 > d2);
            d1 = sd.GetDistance("brittney spears", "britney spears");
            d2 = sd.GetDistance("brittney spears", "brittney startzman");
            Assert.IsTrue(d1 > d2);
            d1 = sd.GetDistance("0012345678", "0012890678");
            d2 = sd.GetDistance("0012345678", "0072385698");
            Assert.AreEqual(d1, d2, 0.001);
        }

        [Test]
        public void TestGetDistance3()
        {
            StringDistance sd = new NGramDistance(3);
            float d = sd.GetDistance("al", "al");
            Assert.AreEqual(d, 1.0f, 0.001);
            d = sd.GetDistance("a", "a");
            Assert.AreEqual(d, 1.0f, 0.001);
            d = sd.GetDistance("b", "a");
            Assert.AreEqual(d, 0.0f, 0.001);
            d = sd.GetDistance("martha", "marhta");
            Assert.AreEqual(d, 0.7222, 0.001);
            d = sd.GetDistance("jones", "johnson");
            Assert.AreEqual(d, 0.4762, 0.001);
            d = sd.GetDistance("natural", "contrary");
            Assert.AreEqual(d, 0.2083, 0.001);
            d = sd.GetDistance("abcvwxyz", "cabvwxyz");
            Assert.AreEqual(d, 0.5625, 0.001);
            d = sd.GetDistance("dwayne", "duane");
            Assert.AreEqual(d, 0.5277, 0.001);
            d = sd.GetDistance("dixon", "dicksonx");
            Assert.AreEqual(d, 0.4583, 0.001);
            d = sd.GetDistance("six", "ten");
            Assert.AreEqual(d, 0, 0.001);
            float d1 = sd.GetDistance("zac ephron", "zac efron");
            float d2 = sd.GetDistance("zac ephron", "kai ephron");
            Assert.IsTrue(d1 > d2);
            d1 = sd.GetDistance("brittney spears", "britney spears");
            d2 = sd.GetDistance("brittney spears", "brittney startzman");
            Assert.IsTrue(d1 > d2);
            d1 = sd.GetDistance("0012345678", "0012890678");
            d2 = sd.GetDistance("0012345678", "0072385698");
            Assert.IsTrue(d1 < d2);
        }

        public void TestEmpty()
        {
            StringDistance nsd = new NGramDistance(1);
            float d = nsd.GetDistance("", "al");
            Assert.AreEqual(d, 0.0f, 0.001);
        }

    }


}
