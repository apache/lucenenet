// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Synonym
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
    /// @since solr 1.4
    /// </summary>
    public class TestMultiWordSynonyms_ : BaseTokenStreamFactoryTestCase
    {

        /// @deprecated Remove this test in 5.0 
        [Test]
        [Obsolete("Remove this test in 5.0")]
        public virtual void TestMultiWordSynonymsOld()
        {
            IList<string> rules = new JCG.List<string>();
            rules.Add("a b c,d");
            SlowSynonymMap synMap = new SlowSynonymMap(true);
            SlowSynonymFilterFactory.ParseRules(rules, synMap, "=>", ",", true, null);

            SlowSynonymFilter ts = new SlowSynonymFilter(new MockTokenizer(new StringReader("a e"), MockTokenizer.WHITESPACE, false), synMap);
            // This fails because ["e","e"] is the value of the token stream
            AssertTokenStreamContents(ts, new string[] { "a", "e" });
        }

        [Test]
        public virtual void TestMultiWordSynonyms()
        {
            TextReader reader = new StringReader("a e");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Synonym", TEST_VERSION_CURRENT, new StringMockResourceLoader("a b c,d"), "synonyms", "synonyms.txt").Create(stream);
            // This fails because ["e","e"] is the value of the token stream
            AssertTokenStreamContents(stream, new string[] { "a", "e" });
        }
    }
}