// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.Analysis.CharFilters
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

    public class TestMappingCharFilterFactory : BaseTokenStreamFactoryTestCase
    {
        [Test]
        public virtual void TestParseString()
        {

            MappingCharFilterFactory f = (MappingCharFilterFactory)CharFilterFactory("Mapping");

            try
            {
                f.ParseString("\\");
                fail("escape character cannot be alone.");
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
            }

            assertEquals("unexpected escaped characters", "\\\"\n\t\r\b\f", f.ParseString("\\\\\\\"\\n\\t\\r\\b\\f"));
            assertEquals("unexpected escaped characters", "A", f.ParseString("\\u0041"));
            assertEquals("unexpected escaped characters", "AB", f.ParseString("\\u0041\\u0042"));

            try
            {
                f.ParseString("\\u000");
                fail("invalid length check.");
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
            }

            try
            {
                f.ParseString("\\u123x");
                fail("invalid hex number check.");
            }
            catch (FormatException)
            {
            }
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            try
            {
                CharFilterFactory("Mapping", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}