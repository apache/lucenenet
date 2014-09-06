using System.Globalization;

namespace Lucene.Net.Search
{
    using NUnit.Framework;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Lucene.Net.Document.Document;
    using Field = Lucene.Net.Document.Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IntField = Lucene.Net.Document.IntField;
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
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestMultiValuedNumericRangeQuery : LuceneTestCase
    {
        /// <summary>
        /// Tests NumericRangeQuery on a multi-valued field (multiple numeric values per document).
        /// this test ensures, that a classical TermRangeQuery returns exactly the same document numbers as
        /// NumericRangeQuery (see SOLR-1322 for discussion) and the multiple precision terms per numeric value
        /// do not interfere with multiple numeric values.
        /// </summary>
        [Test]
        public virtual void TestMultiValuedNRQ()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(TestUtil.NextInt(Random(), 50, 1000)));

            //DecimalFormat format = new DecimalFormat("00000000000", new DecimalFormatSymbols(Locale.ROOT));
            NumberFormatInfo f = new NumberFormatInfo();
            f.NumberDecimalSeparator = ".";
            f.NumberDecimalDigits = 0;

            int num = AtLeast(500);
            for (int l = 0; l < num; l++)
            {
                Document doc = new Document();
                for (int m = 0, c = Random().Next(10); m <= c; m++)
                {
                    int value = Random().Next(int.MaxValue);
                    doc.Add(NewStringField("asc", value.ToString(f), Field.Store.NO));
                    doc.Add(new IntField("trie", value, Field.Store.NO));
                }
                writer.AddDocument(doc);
            }
            IndexReader reader = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(reader);
            num = AtLeast(50);
            for (int i = 0; i < num; i++)
            {
                int lower = Random().Next(int.MaxValue);
                int upper = Random().Next(int.MaxValue);
                if (lower > upper)
                {
                    int a = lower;
                    lower = upper;
                    upper = a;
                }
                TermRangeQuery cq = TermRangeQuery.NewStringRange("asc", lower.ToString(f), upper.ToString(f), true, true);
                NumericRangeQuery<int> tq = NumericRangeQuery.NewIntRange("trie", lower, upper, true, true);
                TopDocs trTopDocs = searcher.Search(cq, 1);
                TopDocs nrTopDocs = searcher.Search(tq, 1);
                Assert.AreEqual(trTopDocs.TotalHits, nrTopDocs.TotalHits, "Returned count for NumericRangeQuery and TermRangeQuery must be equal");
            }
            reader.Dispose();
            directory.Dispose();
        }
    }
}