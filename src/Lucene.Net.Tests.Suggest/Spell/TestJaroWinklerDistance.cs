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

    public class TestJaroWinklerDistance : LuceneTestCase
    {
        private IStringDistance sd = new JaroWinklerDistance();

        [Test]
        public void TestGetDistance()
        {
            float d = sd.GetDistance("al", "al");
            assertTrue(d == 1.0f);
            d = sd.GetDistance("martha", "marhta");
            assertTrue(d > 0.961 && d < 0.962);
            d = sd.GetDistance("jones", "johnson");
            assertTrue(d > 0.832 && d < 0.833);
            d = sd.GetDistance("abcvwxyz", "cabvwxyz");
            assertTrue(d > 0.958 && d < 0.959);
            d = sd.GetDistance("dwayne", "duane");
            assertTrue(d > 0.84 && d < 0.841);
            d = sd.GetDistance("dixon", "dicksonx");
            assertTrue(d > 0.813 && d < 0.814);
            d = sd.GetDistance("fvie", "ten");
            assertTrue(d == 0f);
            float d1 = sd.GetDistance("zac ephron", "zac efron");
            float d2 = sd.GetDistance("zac ephron", "kai ephron");
            assertTrue(d1 > d2);
            d1 = sd.GetDistance("brittney spears", "britney spears");
            d2 = sd.GetDistance("brittney spears", "brittney startzman");
            assertTrue(d1 > d2);
        }
    }
}
