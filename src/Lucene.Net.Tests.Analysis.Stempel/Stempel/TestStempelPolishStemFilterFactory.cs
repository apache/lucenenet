using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Analysis.Stempel
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

    /// <summary>
    /// Tests for <see cref="StempelPolishStemFilterFactory"/>
    /// </summary>
    public class TestStempelPolishStemFilterFactory : BaseTokenStreamTestCase
    {
        [Test]
        public void TestBasics()
        {
            TextReader reader = new StringReader("studenta studenci");
            StempelPolishStemFilterFactory factory = new StempelPolishStemFilterFactory(new Dictionary<string, string>());
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = factory.Create(stream);
            AssertTokenStreamContents(stream,
                new string[] { "student", "student" });
        }

        /** Test that bogus arguments result in exception */
        [Test]
        public void TestBogusArguments()
        {
            try
            {
                new StempelPolishStemFilterFactory(new Dictionary<string, string>() { { "bogusArg", "bogusValue" } });
                fail();
            }
            catch (ArgumentException expected)
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}
