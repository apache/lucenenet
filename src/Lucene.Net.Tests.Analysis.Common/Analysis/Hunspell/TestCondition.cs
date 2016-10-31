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

    public class TestCondition : StemmerTestBase
    {
        [OneTimeSetUp]
        public static void BeforeClass()
        {
            Init("condition.aff", "condition.dic");
        }

        [Test]
        public virtual void TestStemming()
        {
            AssertStemsTo("hello", "hello");
            AssertStemsTo("try", "try");
            AssertStemsTo("tried", "try");
            AssertStemsTo("work", "work");
            AssertStemsTo("worked", "work");
            AssertStemsTo("rework", "work");
            AssertStemsTo("reworked", "work");
            AssertStemsTo("retried");
            AssertStemsTo("workied");
            AssertStemsTo("tryed");
            AssertStemsTo("tryied");
            AssertStemsTo("helloed");
        }
    }
}