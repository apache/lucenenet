// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Core
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
    /// Testcase for <seealso cref="TypeTokenFilterFactory"/>
    /// </summary>
    public class TestTypeTokenFilterFactory : BaseTokenStreamFactoryTestCase
    {

        [Test]
        public virtual void TestInform()
        {
            TypeTokenFilterFactory factory = (TypeTokenFilterFactory)TokenFilterFactory("Type", "types", "stoptypes-1.txt", "enablePositionIncrements", "true");
            ICollection<string> types = factory.StopTypes;
            assertTrue("types is null and it shouldn't be", types != null);
            assertTrue("types Size: " + types.Count + " is not: " + 2, types.Count == 2);
            assertTrue("enablePositionIncrements was set to true but not correctly parsed", factory.EnablePositionIncrements);

            factory = (TypeTokenFilterFactory)TokenFilterFactory("Type", "types", "stoptypes-1.txt, stoptypes-2.txt", "enablePositionIncrements", "false", "useWhitelist", "true");
            types = factory.StopTypes;
            assertTrue("types is null and it shouldn't be", types != null);
            assertTrue("types Size: " + types.Count + " is not: " + 4, types.Count == 4);
            assertTrue("enablePositionIncrements was set to false but not correctly parsed", !factory.EnablePositionIncrements);
        }

        [Test]
        public virtual void TestCreationWithBlackList()
        {
            TokenFilterFactory factory = TokenFilterFactory("Type", "types", "stoptypes-1.txt, stoptypes-2.txt", "enablePositionIncrements", "true");
            NumericTokenStream input = new NumericTokenStream();
            input.SetInt32Value(123);
            factory.Create(input);
        }

        [Test]
        public virtual void TestCreationWithWhiteList()
        {
            TokenFilterFactory factory = TokenFilterFactory("Type", "types", "stoptypes-1.txt, stoptypes-2.txt", "enablePositionIncrements", "true", "useWhitelist", "true");
            NumericTokenStream input = new NumericTokenStream();
            input.SetInt32Value(123);
            factory.Create(input);
        }

        [Test]
        public virtual void TestMissingTypesParameter()
        {
            try
            {
                TokenFilterFactory("Type", "enablePositionIncrements", "false");
                fail("not supplying 'types' parameter should cause an IllegalArgumentException");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // everything ok
            }
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            try
            {
                TokenFilterFactory("Type", "types", "stoptypes-1.txt", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}