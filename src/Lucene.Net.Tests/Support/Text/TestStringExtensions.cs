using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;
#if !FEATURE_STRING_CONTAINS_STRINGCOMPARISON
using System;
#endif

namespace Lucene.Net.Support.Text
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

    [TestFixture, LuceneNetSpecific]
    public class TestStringExtensions : LuceneTestCase
    {
        [Test]
        public void TestContainsAny()
        {
            Assert.IsTrue("hello".ContainsAny(new[] { 'h', 'e', 'l', 'o' }));
            Assert.IsFalse("hello".ContainsAny(new[] { 'x', 'y', 'z' }));
        }

#if !FEATURE_STRING_CONTAINS_STRINGCOMPARISON
        [Test]
        public void TestContainsWithStringComparison()
        {
            Assert.IsTrue("hello".Contains("ell", StringComparison.Ordinal));
            Assert.IsFalse("hello".Contains("world", StringComparison.Ordinal));
            Assert.IsTrue("hello".Contains("ELL", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse("hello".Contains("WORLD", StringComparison.OrdinalIgnoreCase));
        }
#endif

        [TestCase("segments.gen")]
        [TestCase("_0.cfs")]
        [TestCase("_0_Lucene41_0.tip")]
        [TestCase(".hidden")]
        [TestCase("a..b..c")]
        [TestCase("name-with-dashes_and_underscores.ext")]
        [LuceneNetSpecific]
        public void TestIsValidSinglePathComponent_AcceptsValidNames(string path)
        {
            Assert.IsTrue(path.IsValidSinglePathComponent(),
                $"expected '{path}' to be a valid single path component");
        }

        [TestCase("")]                          // empty
        [TestCase(".")]                         // current-directory literal
        [TestCase("..")]                        // parent-directory literal
        [TestCase("foo/bar")]                   // forward-slash separator
        [TestCase("foo\\bar")]                  // backslash separator (rejected even on POSIX)
        [TestCase("../../a/b")]                 // multi-level forward-slash sequence
        [TestCase("..\\..\\a\\b")]              // multi-level backslash sequence
        [TestCase("..\\a")]                     // single-level backslash sequence
        [TestCase("/a/b")]                      // absolute POSIX path
        [TestCase("/tmp/file.txt")]             // absolute POSIX path
        [TestCase("C:\\folder\\file")]          // path containing a Windows volume separator
        [TestCase("C:\\file.txt")]              // path containing a Windows volume separator
        [TestCase("name\0extra")]               // embedded NUL
        [TestCase("name/")]                     // trailing forward slash
        [TestCase("name\\")]                    // trailing backslash
        [TestCase("\\")]                        // bare backslash
        [TestCase("/")]                         // bare forward slash
        [LuceneNetSpecific]
        public void TestIsValidSinglePathComponent_RejectsInvalidNames(string path)
        {
            Assert.IsFalse(path.IsValidSinglePathComponent(),
                $"expected '{path}' to be rejected as a single path component");
        }
    }
}
