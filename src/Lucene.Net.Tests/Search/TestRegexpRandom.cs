using System.Text;
using Lucene.Net.Documents;

namespace Lucene.Net.Search
{
    using NUnit.Framework;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

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

    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;

    /// <summary>
    /// Create an index with terms from 000-999.
    /// Generates random regexps according to simple patterns,
    /// and validates the correct number of hits are returned.
    /// </summary>
    [TestFixture]
    public class TestRegexpRandom : LuceneTestCase
    {
        private IndexSearcher Searcher;
        private IndexReader Reader;
        private Directory Dir;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            Dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), Dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(TestUtil.NextInt(Random(), 50, 1000)));

            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.OmitNorms = true;
            Field field = NewField("field", "", customType);
            doc.Add(field);

            for (int i = 0; i < 1000; i++)
            {
                field.SetStringValue(i.ToString("D3"));
                writer.AddDocument(doc);
            }

            Reader = writer.Reader;
            writer.Dispose();
            Searcher = NewSearcher(Reader);
        }

        private char N()
        {
            return (char)(0x30 + Random().Next(10));
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
            Query wq = new RegexpQuery(new Term("field", FillPattern(pattern)));
            TopDocs docs = Searcher.Search(wq, 25);
            Assert.AreEqual(numHits, docs.TotalHits, "Incorrect hits for pattern: " + pattern);
        }

        [TearDown]
        public override void TearDown()
        {
            Reader.Dispose();
            Dir.Dispose();
            base.TearDown();
        }

        [Test]
        public virtual void TestRegexps()
        {
            int num = AtLeast(1);
            for (int i = 0; i < num; i++)
            {
                AssertPatternHits("NNN", 1);
                AssertPatternHits(".NN", 10);
                AssertPatternHits("N.N", 10);
                AssertPatternHits("NN.", 10);
            }

            for (int i = 0; i < num; i++)
            {
                AssertPatternHits(".{1,2}N", 100);
                AssertPatternHits("N.{1,2}", 100);
                AssertPatternHits(".{1,3}", 1000);

                AssertPatternHits("NN[3-7]", 5);
                AssertPatternHits("N[2-6][3-7]", 25);
                AssertPatternHits("[1-5][2-6][3-7]", 125);
                AssertPatternHits("[0-4][3-7][4-8]", 125);
                AssertPatternHits("[2-6][0-4]N", 25);
                AssertPatternHits("[2-6]NN", 5);

                AssertPatternHits("NN.*", 10);
                AssertPatternHits("N.*", 100);
                AssertPatternHits(".*", 1000);

                AssertPatternHits(".*NN", 10);
                AssertPatternHits(".*N", 100);

                AssertPatternHits("N.*N", 10);

                // combo of ? and * operators
                AssertPatternHits(".N.*", 100);
                AssertPatternHits("N..*", 100);

                AssertPatternHits(".*N.", 100);
                AssertPatternHits(".*..", 1000);
                AssertPatternHits(".*.N", 100);
            }
        }
    }
}