/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for Additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Lucene.Net;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using Lucene.Net.Store;
using NUnit.Framework;

namespace Contrib.Regex.Test
{
    public class TestRegexQuery : TestCase
    {
        private IndexSearcher searcher;
        private const String FN = "field";

        [SetUp]
        public void SetUp()
        {
            RAMDirectory directory = new RAMDirectory();
            try
            {
                IndexWriter writer = new IndexWriter(directory, new SimpleAnalyzer(), true,
                                                     IndexWriter.MaxFieldLength.LIMITED);
                Document doc = new Document();
                doc.Add(new Field(FN, "the quick brown fox jumps over the lazy dog", Field.Store.NO, Field.Index.ANALYZED));
                writer.AddDocument(doc);
                writer.Optimize();
                writer.Close();
                searcher = new IndexSearcher(directory, true);
            }
            catch (Exception e)
            {
                Assert.Fail(e.ToString());
            }
        }
        [TearDown]
        public void TearDown()
        {
            try
            {
                searcher.Close();
            }
            catch (Exception e)
            {
                Assert.Fail(e.ToString());
            }
        }

        private static Term NewTerm(String value) { return new Term(FN, value); }

        private int RegexQueryNrHits(String regex, IRegexCapabilities capability)
        {
            RegexQuery query = new RegexQuery(NewTerm(regex));

            if (capability != null)
                query.RegexImplementation = capability;

            return searcher.Search(query, null, 1000).TotalHits;
        }

        private int SpanRegexQueryNrHits(String regex1, String regex2, int slop, bool ordered)
        {
            SpanRegexQuery srq1 = new SpanRegexQuery(NewTerm(regex1));
            SpanRegexQuery srq2 = new SpanRegexQuery(NewTerm(regex2));
            SpanNearQuery query = new SpanNearQuery(new SpanQuery[] { srq1, srq2 }, slop, ordered);

            return searcher.Search(query, null, 1000).TotalHits;
        }

        [Test]
        public void TestMatchAll()
        {
            Assert.Ignore("Difference in behavior of .NET and Java");
            //TermEnum terms = new RegexQuery(new Term(FN, "jum.")).GetEnum(searcher.IndexReader);
            //These terms match in .NET's regex engine.  I feel there's not much I can do about it.
            //// no term should match
            //Assert.Null(terms.Term());
            //Assert.False(terms.Next());
        }

        [Test]
        public void TestRegex1()
        {
            Assert.AreEqual(1, RegexQueryNrHits("^q.[aeiou]c.*$", null));
        }

        [Test]
        public void TestRegex2()
        {
            Assert.AreEqual(0, RegexQueryNrHits("^.[aeiou]c.*$", null));
        }

        [Test]
        public void TestRegex3()
        {
            Assert.AreEqual(0, RegexQueryNrHits("^q.[aeiou]c$", null));
        }

        [Test]
        public void TestSpanRegex1()
        {
            Assert.AreEqual(1, SpanRegexQueryNrHits("^q.[aeiou]c.*$", "dog", 6, true));
        }

        [Test]
        public void TestSpanRegex2()
        {
            Assert.AreEqual(0, SpanRegexQueryNrHits("^q.[aeiou]c.*$", "dog", 5, true));
        }

        [Test]
        public void TestEquals()
        {
            RegexQuery query1 = new RegexQuery(NewTerm("foo.*"));
            //query1.SetRegexImplementation(new JakartaRegexpCapabilities());

            RegexQuery query2 = new RegexQuery(NewTerm("foo.*"));
            Assert.True(query1.Equals(query2));
        }

        [Test]
        public void TestJavaUtilCaseSensativeFail()
        {
            Assert.AreEqual(0, RegexQueryNrHits("^.*DOG.*$", null));
        }

        [Test]
        public void TestJavaUtilCaseInsensative()
        {
            //Assert.AreEqual(1, RegexQueryNrHits("^.*DOG.*$", new CSharpRegexCapabilities()));
        }
    }
}