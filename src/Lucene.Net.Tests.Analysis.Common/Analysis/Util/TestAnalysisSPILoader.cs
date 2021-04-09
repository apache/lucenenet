// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.CharFilters;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Util
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

    public class TestAnalysisSPILoader : LuceneTestCase
    {

        private IDictionary<string, string> VersionArgOnly()
        {
            return new HashMapAnonymousClass();
        }

        private sealed class HashMapAnonymousClass : Dictionary<string, string>
        {
            public HashMapAnonymousClass()
            {
                this["luceneMatchVersion"] = TEST_VERSION_CURRENT.ToString();
            }

        }

        [Test]
        public virtual void TestLookupTokenizer()
        {
            assertSame(typeof(WhitespaceTokenizerFactory), TokenizerFactory.ForName("Whitespace", VersionArgOnly()).GetType());
            assertSame(typeof(WhitespaceTokenizerFactory), TokenizerFactory.ForName("WHITESPACE", VersionArgOnly()).GetType());
            assertSame(typeof(WhitespaceTokenizerFactory), TokenizerFactory.ForName("whitespace", VersionArgOnly()).GetType());
        }

        [Test]
        public virtual void TestBogusLookupTokenizer()
        {
            try
            {
                TokenizerFactory.ForName("sdfsdfsdfdsfsdfsdf", new Dictionary<string, string>());
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                //
            }

            try
            {
                TokenizerFactory.ForName("!(**#$U*#$*", new Dictionary<string, string>());
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                //
            }
        }

        [Test]
        public virtual void TestLookupTokenizerClass()
        {
            assertSame(typeof(WhitespaceTokenizerFactory), TokenizerFactory.LookupClass("Whitespace"));
            assertSame(typeof(WhitespaceTokenizerFactory), TokenizerFactory.LookupClass("WHITESPACE"));
            assertSame(typeof(WhitespaceTokenizerFactory), TokenizerFactory.LookupClass("whitespace"));
        }

        [Test]
        public virtual void TestBogusLookupTokenizerClass()
        {
            try
            {
                TokenizerFactory.LookupClass("sdfsdfsdfdsfsdfsdf");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                //
            }

            try
            {
                TokenizerFactory.LookupClass("!(**#$U*#$*");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                //
            }
        }

        [Test]
        public virtual void TestAvailableTokenizers()
        {
            assertTrue(TokenizerFactory.AvailableTokenizers.Contains("whitespace"));
        }

        [Test]
        public virtual void TestLookupTokenFilter()
        {
            assertSame(typeof(LowerCaseFilterFactory), TokenFilterFactory.ForName("Lowercase", VersionArgOnly()).GetType());
            assertSame(typeof(LowerCaseFilterFactory), TokenFilterFactory.ForName("LOWERCASE", VersionArgOnly()).GetType());
            assertSame(typeof(LowerCaseFilterFactory), TokenFilterFactory.ForName("lowercase", VersionArgOnly()).GetType());

            assertSame(typeof(RemoveDuplicatesTokenFilterFactory), TokenFilterFactory.ForName("RemoveDuplicates", VersionArgOnly()).GetType());
            assertSame(typeof(RemoveDuplicatesTokenFilterFactory), TokenFilterFactory.ForName("REMOVEDUPLICATES", VersionArgOnly()).GetType());
            assertSame(typeof(RemoveDuplicatesTokenFilterFactory), TokenFilterFactory.ForName("removeduplicates", VersionArgOnly()).GetType());
        }

        [Test]
        public virtual void TestBogusLookupTokenFilter()
        {
            try
            {
                TokenFilterFactory.ForName("sdfsdfsdfdsfsdfsdf", new Dictionary<string, string>());
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                //
            }

            try
            {
                TokenFilterFactory.ForName("!(**#$U*#$*", new Dictionary<string, string>());
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                //
            }
        }

        [Test]
        public virtual void TestLookupTokenFilterClass()
        {
            assertSame(typeof(LowerCaseFilterFactory), TokenFilterFactory.LookupClass("Lowercase"));
            assertSame(typeof(LowerCaseFilterFactory), TokenFilterFactory.LookupClass("LOWERCASE"));
            assertSame(typeof(LowerCaseFilterFactory), TokenFilterFactory.LookupClass("lowercase"));

            assertSame(typeof(RemoveDuplicatesTokenFilterFactory), TokenFilterFactory.LookupClass("RemoveDuplicates"));
            assertSame(typeof(RemoveDuplicatesTokenFilterFactory), TokenFilterFactory.LookupClass("REMOVEDUPLICATES"));
            assertSame(typeof(RemoveDuplicatesTokenFilterFactory), TokenFilterFactory.LookupClass("removeduplicates"));
        }

        [Test]
        public virtual void TestBogusLookupTokenFilterClass()
        {
            try
            {
                TokenFilterFactory.LookupClass("sdfsdfsdfdsfsdfsdf");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                //
            }

            try
            {
                TokenFilterFactory.LookupClass("!(**#$U*#$*");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                //
            }
        }

        [Test]
        public virtual void TestAvailableTokenFilters()
        {
            assertTrue(TokenFilterFactory.AvailableTokenFilters.Contains("lowercase"));
            assertTrue(TokenFilterFactory.AvailableTokenFilters.Contains("removeduplicates"));
        }

        [Test]
        public virtual void TestLookupCharFilter()
        {
            assertSame(typeof(HTMLStripCharFilterFactory), CharFilterFactory.ForName("HTMLStrip", VersionArgOnly()).GetType());
            assertSame(typeof(HTMLStripCharFilterFactory), CharFilterFactory.ForName("HTMLSTRIP", VersionArgOnly()).GetType());
            assertSame(typeof(HTMLStripCharFilterFactory), CharFilterFactory.ForName("htmlstrip", VersionArgOnly()).GetType());
        }

        [Test]
        public virtual void TestBogusLookupCharFilter()
        {
            try
            {
                CharFilterFactory.ForName("sdfsdfsdfdsfsdfsdf", new Dictionary<string, string>());
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                //
            }

            try
            {
                CharFilterFactory.ForName("!(**#$U*#$*", new Dictionary<string, string>());
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                //
            }
        }

        [Test]
        public virtual void TestLookupCharFilterClass()
        {
            assertSame(typeof(HTMLStripCharFilterFactory), CharFilterFactory.LookupClass("HTMLStrip"));
            assertSame(typeof(HTMLStripCharFilterFactory), CharFilterFactory.LookupClass("HTMLSTRIP"));
            assertSame(typeof(HTMLStripCharFilterFactory), CharFilterFactory.LookupClass("htmlstrip"));
        }

        [Test]
        public virtual void TestBogusLookupCharFilterClass()
        {
            try
            {
                CharFilterFactory.LookupClass("sdfsdfsdfdsfsdfsdf");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                //
            }

            try
            {
                CharFilterFactory.LookupClass("!(**#$U*#$*");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                //
            }
        }

        [Test]
        public virtual void TestAvailableCharFilters()
        {
            assertTrue(CharFilterFactory.AvailableCharFilters.Contains("htmlstrip"));
        }
    }
}