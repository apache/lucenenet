// Lucene version compatibility level 4.10.4
using NUnit.Framework;

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

    public class TestSpaces : StemmerTestBase
    {
        public override void BeforeClass()
        {
            base.BeforeClass();
            Init("spaces.aff", "spaces.dic");
        }

        [Test]
        public void TestStemming()
        {
            AssertStemsTo("four", "four");
            AssertStemsTo("fours", "four");
            AssertStemsTo("five", "five");
            AssertStemsTo("forty four", "forty four");
            AssertStemsTo("forty fours", "forty four");
            AssertStemsTo("forty five", "forty five");
            AssertStemsTo("fifty", "50");
            AssertStemsTo("fiftys", "50");
            AssertStemsTo("sixty", "60");
            AssertStemsTo("sixty four", "64");
            AssertStemsTo("fifty four", "54");
            AssertStemsTo("fifty fours", "54");
        }
    }
}
