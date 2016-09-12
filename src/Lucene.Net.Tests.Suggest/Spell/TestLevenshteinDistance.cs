using Lucene.Net.Util;
using NUnit.Framework;

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

    public class TestLevenshteinDistance : LuceneTestCase
    {
        private IStringDistance sd = new LevensteinDistance();

        [Test]
        public void TestGetDistance()
        {
            float d = sd.GetDistance("al", "al");
            assertEquals(d, 1.0f, 0.001);
            d = sd.GetDistance("martha", "marhta");
            assertEquals(d, 0.6666, 0.001);
            d = sd.GetDistance("jones", "johnson");
            assertEquals(d, 0.4285, 0.001);
            d = sd.GetDistance("abcvwxyz", "cabvwxyz");
            assertEquals(d, 0.75, 0.001);
            d = sd.GetDistance("dwayne", "duane");
            assertEquals(d, 0.666, 0.001);
            d = sd.GetDistance("dixon", "dicksonx");
            assertEquals(d, 0.5, 0.001);
            d = sd.GetDistance("six", "ten");
            assertEquals(d, 0, 0.001);
            float d1 = sd.GetDistance("zac ephron", "zac efron");
            float d2 = sd.GetDistance("zac ephron", "kai ephron");
            assertEquals(d1, d2, 0.001);
            d1 = sd.GetDistance("brittney spears", "britney spears");
            d2 = sd.GetDistance("brittney spears", "brittney startzman");
            assertTrue(d1 > d2);
        }

        [Test]
        public void TestEmpty()
        {
            float d = sd.GetDistance("", "al");
            assertEquals(d, 0.0f, 0.001);
        }
    }
}
