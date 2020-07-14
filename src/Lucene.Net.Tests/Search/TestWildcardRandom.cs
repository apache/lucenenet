using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System;
using System.Text;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Search
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

    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// Create an index with terms from 000-999.
    /// Generates random wildcards according to patterns,
    /// and validates the correct number of hits are returned.
    /// </summary>

    [TestFixture]
    public class TestWildcardRandom : LuceneTestCase
    {
        private IndexSearcher searcher;
        private IndexReader reader;
        private Directory dir;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(TestUtil.NextInt32(Random, 50, 1000)));

            Document doc = new Document();
            Field field = NewStringField("field", "", Field.Store.NO);
            doc.Add(field);

            for (int i = 0; i < 1000; i++)
            {
                field.SetStringValue(i.ToString("D3"));
                writer.AddDocument(doc);
            }

            reader = writer.GetReader();
            searcher = NewSearcher(reader);
            writer.Dispose();
            if (Verbose)
            {
                Console.WriteLine("TEST: setUp searcher=" + searcher);
            }
        }

        private char N()
        {
            return (char)(0x30 + Random.Next(10));
        }

        private string FillPattern(string wildcardPattern)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < wildcardPattern.Length; i++)
            {
                switch (wildcardPattern[i])
                {
                    case 'N':
                        sb.Append(N());
                        break;

                    default:
                        sb.Append(wildcardPattern[i]);
                        break;
                }
            }
            return sb.ToString();
        }

        private void AssertPatternHits(string pattern, int numHits)
        {
            // TODO: run with different rewrites
            string filledPattern = FillPattern(pattern);
            if (Verbose)
            {
                Console.WriteLine("TEST: run wildcard pattern=" + pattern + " filled=" + filledPattern);
            }
            Query wq = new WildcardQuery(new Term("field", filledPattern));
            TopDocs docs = searcher.Search(wq, 25);
            Assert.AreEqual(numHits, docs.TotalHits, "Incorrect hits for pattern: " + pattern);
        }

        [TearDown]
        public override void TearDown()
        {
            reader.Dispose();
            dir.Dispose();
            base.TearDown();
        }

        [Test]
        public virtual void TestWildcards()
        {
            ;
            int num = AtLeast(1);
            for (int i = 0; i < num; i++)
            {
                AssertPatternHits("NNN", 1);
                AssertPatternHits("?NN", 10);
                AssertPatternHits("N?N", 10);
                AssertPatternHits("NN?", 10);
            }

            for (int i = 0; i < num; i++)
            {
                AssertPatternHits("??N", 100);
                AssertPatternHits("N??", 100);
                AssertPatternHits("???", 1000);

                AssertPatternHits("NN*", 10);
                AssertPatternHits("N*", 100);
                AssertPatternHits("*", 1000);

                AssertPatternHits("*NN", 10);
                AssertPatternHits("*N", 100);

                AssertPatternHits("N*N", 10);

                // combo of ? and * operators
                AssertPatternHits("?N*", 100);
                AssertPatternHits("N?*", 100);

                AssertPatternHits("*N?", 100);
                AssertPatternHits("*??", 1000);
                AssertPatternHits("*?N", 100);
            }
        }
    }
}