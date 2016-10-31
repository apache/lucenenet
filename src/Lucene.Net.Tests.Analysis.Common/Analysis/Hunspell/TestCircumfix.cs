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

    public class TestCircumfix_ : StemmerTestBase
    {
        [OneTimeSetUp]
        public static void BeforeClass()
        {
            Init("circumfix.aff", "circumfix.dic");
        }

        [Test]
        public virtual void TestCircumfix()
        {
            AssertStemsTo("nagy", "nagy");
            AssertStemsTo("nagyobb", "nagy");
            AssertStemsTo("legnagyobb", "nagy");
            AssertStemsTo("legeslegnagyobb", "nagy");
            AssertStemsTo("nagyobbobb");
            AssertStemsTo("legnagy");
            AssertStemsTo("legeslegnagy");
        }
    }
}