using System;
using System.Collections.Generic;
using System.Globalization;
using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using Console = Lucene.Net.Support.SystemConsole;

namespace Lucene.Net.Search
{
    using BinaryDocValuesField = BinaryDocValuesField;
    using BytesRef = Lucene.Net.Util.BytesRef;

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

    using Codec = Lucene.Net.Codecs.Codec;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using DoubleField = DoubleField;
    using English = Lucene.Net.Util.English;
    using Field = Field;
    using SingleDocValuesField = SingleDocValuesField;
    using SingleField = SingleField;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using Int32Field = Int32Field;
    using Int64Field = Int64Field;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using NumericDocValuesField = NumericDocValuesField;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using SortedDocValuesField = SortedDocValuesField;
    using StoredField = StoredField;
    using Term = Lucene.Net.Index.Term;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// Tests IndexSearcher's searchAfter() method
    /// </summary>
    [TestFixture]
    public class TestSearchAfter : LuceneTestCase
    {
        private bool isVerbose = false;

        private Directory Dir;
        private IndexReader Reader;
        private IndexSearcher Searcher;

        // LUCENENET specific - need to execute this AFTER the base setup, or it won't be right
        //internal bool supportsDocValues = Codec.Default.Name.Equals("Lucene3x", StringComparison.Ordinal) == false;
        private int Iter;
        private IList<SortField> AllSortFields;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            // LUCENENET specific: Moved this logic here to ensure that it is executed
            // after the class is setup - a field is way to early to execute this.
            bool supportsDocValues = Codec.Default.Name.Equals("Lucene3x", StringComparison.Ordinal) == false;

            AllSortFields = new List<SortField> {
#pragma warning disable 612,618
                new SortField("byte", SortFieldType.BYTE, false),
                new SortField("short", SortFieldType.INT16, false),
#pragma warning restore 612,618
                new SortField("int", SortFieldType.INT32, false),
                new SortField("long", SortFieldType.INT64, false),
                new SortField("float", SortFieldType.SINGLE, false),
                new SortField("double", SortFieldType.DOUBLE, false),
                new SortField("bytes", SortFieldType.STRING, false),
                new SortField("bytesval", SortFieldType.STRING_VAL, false),
#pragma warning disable 612,618
                new SortField("byte", SortFieldType.BYTE, true),
                new SortField("short", SortFieldType.INT16, true),
#pragma warning restore 612,618
                new SortField("int", SortFieldType.INT32, true),
                new SortField("long", SortFieldType.INT64, true),
                new SortField("float", SortFieldType.SINGLE, true),
                new SortField("double", SortFieldType.DOUBLE, true),
                new SortField("bytes", SortFieldType.STRING, true),
                new SortField("bytesval", SortFieldType.STRING_VAL, true),
                SortField.FIELD_SCORE,
                SortField.FIELD_DOC
            };

            if (supportsDocValues)
            {
                AllSortFields.AddRange(new SortField[] {
                    new SortField("intdocvalues", SortFieldType.INT32, false),
                    new SortField("floatdocvalues", SortFieldType.SINGLE, false),
                    new SortField("sortedbytesdocvalues", SortFieldType.STRING, false),
                    new SortField("sortedbytesdocvaluesval", SortFieldType.STRING_VAL, false),
                    new SortField("straightbytesdocvalues", SortFieldType.STRING_VAL, false),
                    new SortField("intdocvalues", SortFieldType.INT32, true),
                    new SortField("floatdocvalues", SortFieldType.SINGLE, true),
                    new SortField("sortedbytesdocvalues", SortFieldType.STRING, true),
                    new SortField("sortedbytesdocvaluesval", SortFieldType.STRING_VAL, true),
                    new SortField("straightbytesdocvalues", SortFieldType.STRING_VAL, true)
                });
            }

            // Also test missing first / last for the "string" sorts:
            foreach (string field in new string[] { "bytes", "sortedbytesdocvalues" })
            {
                for (int rev = 0; rev < 2; rev++)
                {
                    bool reversed = rev == 0;
                    SortField sf = new SortField(field, SortFieldType.STRING, reversed);
                    sf.MissingValue = SortField.STRING_FIRST;
                    AllSortFields.Add(sf);

                    sf = new SortField(field, SortFieldType.STRING, reversed);
                    sf.MissingValue = SortField.STRING_LAST;
                    AllSortFields.Add(sf);
                }
            }

            int limit = AllSortFields.Count;
            for (int i = 0; i < limit; i++)
            {
                SortField sf = AllSortFields[i];
                if (sf.Type == SortFieldType.INT32)
                {
                    SortField sf2 = new SortField(sf.Field, SortFieldType.INT32, sf.IsReverse);
                    sf2.MissingValue = Random.Next();
                    AllSortFields.Add(sf2);
                }
                else if (sf.Type == SortFieldType.INT64)
                {
                    SortField sf2 = new SortField(sf.Field, SortFieldType.INT64, sf.IsReverse);
                    sf2.MissingValue = Random.NextInt64();
                    AllSortFields.Add(sf2);
                }
                else if (sf.Type == SortFieldType.SINGLE)
                {
                    SortField sf2 = new SortField(sf.Field, SortFieldType.SINGLE, sf.IsReverse);
                    sf2.MissingValue = (float)Random.NextDouble();
                    AllSortFields.Add(sf2);
                }
                else if (sf.Type == SortFieldType.DOUBLE)
                {
                    SortField sf2 = new SortField(sf.Field, SortFieldType.DOUBLE, sf.IsReverse);
                    sf2.MissingValue = Random.NextDouble();
                    AllSortFields.Add(sf2);
                }
            }

            Dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(
#if FEATURE_INSTANCE_TESTDATA_INITIALIZATION
                this,
#endif
                Random, Dir);
            int numDocs = AtLeast(200);
            for (int i = 0; i < numDocs; i++)
            {
                IList<Field> fields = new List<Field>();
                fields.Add(NewTextField("english", English.Int32ToEnglish(i), Field.Store.NO));
                fields.Add(NewTextField("oddeven", (i % 2 == 0) ? "even" : "odd", Field.Store.NO));
                fields.Add(NewStringField("byte", "" + ((sbyte)Random.Next()).ToString(CultureInfo.InvariantCulture), Field.Store.NO));
                fields.Add(NewStringField("short", "" + ((short)Random.Next()).ToString(CultureInfo.InvariantCulture), Field.Store.NO));
                fields.Add(new Int32Field("int", Random.Next(), Field.Store.NO));
                fields.Add(new Int64Field("long", Random.NextInt64(), Field.Store.NO));

                fields.Add(new SingleField("float", (float)Random.NextDouble(), Field.Store.NO));
                fields.Add(new DoubleField("double", Random.NextDouble(), Field.Store.NO));
                fields.Add(NewStringField("bytes", TestUtil.RandomRealisticUnicodeString(Random), Field.Store.NO));
                fields.Add(NewStringField("bytesval", TestUtil.RandomRealisticUnicodeString(Random), Field.Store.NO));
                fields.Add(new DoubleField("double", Random.NextDouble(), Field.Store.NO));

                if (supportsDocValues)
                {
                    fields.Add(new NumericDocValuesField("intdocvalues", Random.Next()));
                    fields.Add(new SingleDocValuesField("floatdocvalues", (float)Random.NextDouble()));
                    fields.Add(new SortedDocValuesField("sortedbytesdocvalues", new BytesRef(TestUtil.RandomRealisticUnicodeString(Random))));
                    fields.Add(new SortedDocValuesField("sortedbytesdocvaluesval", new BytesRef(TestUtil.RandomRealisticUnicodeString(Random))));
                    fields.Add(new BinaryDocValuesField("straightbytesdocvalues", new BytesRef(TestUtil.RandomRealisticUnicodeString(Random))));
                }
                Document document = new Document();
                document.Add(new StoredField("id", "" + i));
                if (isVerbose)
                {
                    Console.WriteLine("  add doc id=" + i);
                }
                foreach (Field field in fields)
                {
                    // So we are sometimes missing that field:
                    if (Random.Next(5) != 4)
                    {
                        document.Add(field);
                        if (isVerbose)
                        {
                            Console.WriteLine("    " + field);
                        }
                    }
                }

                iw.AddDocument(document);

                if (Random.Next(50) == 17)
                {
                    iw.Commit();
                }
            }
            Reader = iw.GetReader();
            iw.Dispose();
            Searcher = NewSearcher(Reader);
            if (isVerbose)
            {
                Console.WriteLine("  searcher=" + Searcher);
            }
        }

        [TearDown]
        public override void TearDown()
        {
            Reader.Dispose();
            Dir.Dispose();
            base.TearDown();
        }

        [Test, LongRunningTest]
        public virtual void TestQueries()
        {
            // LUCENENET specific: NUnit will crash with an OOM if we do the full test
            // with verbosity enabled. So, making this a manual setting that can be
            // turned on if, and only if, needed for debugging. If the setting is turned
            // on, we are decresing the number of iterations to only 1, which seems to
            // keep it from crashing.

            // Enable verbosity at the top of this file: isVerbose = true;

            // because the first page has a null 'after', we get a normal collector.
            // so we need to run the test a few times to ensure we will collect multiple
            // pages.
            int n = isVerbose ? 1 : AtLeast(20);
            for (int i = 0; i < n; i++)
            {
                Filter odd = new QueryWrapperFilter(new TermQuery(new Term("oddeven", "odd")));
                AssertQuery(new MatchAllDocsQuery(), null);
                AssertQuery(new TermQuery(new Term("english", "one")), null);
                AssertQuery(new MatchAllDocsQuery(), odd);
                AssertQuery(new TermQuery(new Term("english", "four")), odd);
                BooleanQuery bq = new BooleanQuery();
                bq.Add(new TermQuery(new Term("english", "one")), Occur.SHOULD);
                bq.Add(new TermQuery(new Term("oddeven", "even")), Occur.SHOULD);
                AssertQuery(bq, null);
            }
        }

        internal virtual void AssertQuery(Query query, Filter filter)
        {
            AssertQuery(query, filter, null);
            AssertQuery(query, filter, Sort.RELEVANCE);
            AssertQuery(query, filter, Sort.INDEXORDER);
            foreach (SortField sortField in AllSortFields)
            {
                AssertQuery(query, filter, new Sort(new SortField[] { sortField }));
            }
            for (int i = 0; i < 20; i++)
            {
                AssertQuery(query, filter, RandomSort);
            }
        }

        internal virtual Sort RandomSort
        {
            get
            {
                SortField[] sortFields = new SortField[TestUtil.NextInt32(Random, 2, 7)];
                for (int i = 0; i < sortFields.Length; i++)
                {
                    sortFields[i] = AllSortFields[Random.Next(AllSortFields.Count)];
                }
                return new Sort(sortFields);
            }
        }

        internal virtual void AssertQuery(Query query, Filter filter, Sort sort)
        {
            int maxDoc = Searcher.IndexReader.MaxDoc;
            TopDocs all;
            int pageSize = TestUtil.NextInt32(Random, 1, maxDoc * 2);
            if (isVerbose)
            {
                Console.WriteLine("\nassertQuery " + (Iter++) + ": query=" + query + " filter=" + filter + " sort=" + sort + " pageSize=" + pageSize);
            }
            bool doMaxScore = Random.NextBoolean();
            bool doScores = Random.NextBoolean();
            if (sort == null)
            {
                all = Searcher.Search(query, filter, maxDoc);
            }
            else if (sort == Sort.RELEVANCE)
            {
                all = Searcher.Search(query, filter, maxDoc, sort, true, doMaxScore);
            }
            else
            {
                all = Searcher.Search(query, filter, maxDoc, sort, doScores, doMaxScore);
            }
            if (isVerbose)
            {
                Console.WriteLine("  all.TotalHits=" + all.TotalHits);
                int upto = 0;
                foreach (ScoreDoc scoreDoc in all.ScoreDocs)
                {
                    Console.WriteLine("    hit " + (upto++) + ": id=" + Searcher.Doc(scoreDoc.Doc).Get("id") + " " + scoreDoc);
                }
            }
            int pageStart = 0;
            ScoreDoc lastBottom = null;
            while (pageStart < all.TotalHits)
            {
                TopDocs paged;
                if (sort == null)
                {
                    if (isVerbose)
                    {
                        Console.WriteLine("  iter lastBottom=" + lastBottom);
                    }
                    paged = Searcher.SearchAfter(lastBottom, query, filter, pageSize);
                }
                else
                {
                    if (isVerbose)
                    {
                        Console.WriteLine("  iter lastBottom=" + lastBottom);
                    }
                    if (sort == Sort.RELEVANCE)
                    {
                        paged = Searcher.SearchAfter(lastBottom, query, filter, pageSize, sort, true, doMaxScore);
                    }
                    else
                    {
                        paged = Searcher.SearchAfter(lastBottom, query, filter, pageSize, sort, doScores, doMaxScore);
                    }
                }
                if (isVerbose)
                {
                    Console.WriteLine("    " + paged.ScoreDocs.Length + " hits on page");
                }

                if (paged.ScoreDocs.Length == 0)
                {
                    break;
                }
                AssertPage(pageStart, all, paged);
                pageStart += paged.ScoreDocs.Length;
                lastBottom = paged.ScoreDocs[paged.ScoreDocs.Length - 1];
            }
            Assert.AreEqual(all.ScoreDocs.Length, pageStart);
        }

        internal virtual void AssertPage(int pageStart, TopDocs all, TopDocs paged)
        {
            Assert.AreEqual(all.TotalHits, paged.TotalHits);
            for (int i = 0; i < paged.ScoreDocs.Length; i++)
            {
                ScoreDoc sd1 = all.ScoreDocs[pageStart + i];
                ScoreDoc sd2 = paged.ScoreDocs[i];
                if (isVerbose)
                {
                    Console.WriteLine("    hit " + (pageStart + i));
                    Console.WriteLine("      expected id=" + Searcher.Doc(sd1.Doc).Get("id") + " " + sd1);
                    Console.WriteLine("        actual id=" + Searcher.Doc(sd2.Doc).Get("id") + " " + sd2);
                }
                Assert.AreEqual(sd1.Doc, sd2.Doc);
                Assert.AreEqual(sd1.Score, sd2.Score, 0f);
                if (sd1 is FieldDoc)
                {
                    Assert.IsTrue(sd2 is FieldDoc);
                    Assert.AreEqual(((FieldDoc)sd1).Fields, ((FieldDoc)sd2).Fields);
                }
            }
        }
    }
}