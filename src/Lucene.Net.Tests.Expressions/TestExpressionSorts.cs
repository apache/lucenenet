using J2N.Collections.Generic.Extensions;
using Lucene.Net.Documents;
using Lucene.Net.Expressions.JS;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System.Globalization;

namespace Lucene.Net.Expressions
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
    /// Tests some basic expressions against different queries,
    /// and fieldcache/docvalues fields against an equivalent sort.
    /// </summary>


    [SuppressCodecs("Lucene3x")]
    public class TestExpressionSorts : LuceneTestCase
    {
        private Directory dir;
        private IndexReader reader;
        private IndexSearcher searcher;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            dir = NewDirectory();
            var iw = new RandomIndexWriter(Random, dir);
            int numDocs = TestUtil.NextInt32(Random, 2049, 4000);
            for (int i = 0; i < numDocs; i++)
            {
                var document = new Document
                {
                    NewTextField("english", English.Int32ToEnglish(i), Field.Store.NO),
                    NewTextField("oddeven", (i % 2 == 0) ? "even" : "odd", Field.Store.NO),
                    NewStringField("byte", string.Empty + (unchecked((byte) Random.Next())).ToString(CultureInfo.InvariantCulture), Field.Store.NO),
                    NewStringField("short", string.Empty + ((short) Random.Next()).ToString(CultureInfo.InvariantCulture), Field.Store.NO),
                    new Int32Field("int", Random.Next(), Field.Store.NO),
                    new Int64Field("long", Random.NextInt64(), Field.Store.NO),

                    new SingleField("float", Random.NextSingle(), Field.Store.NO),
                    new DoubleField("double", Random.NextDouble(), Field.Store.NO),

                    new NumericDocValuesField("intdocvalues", Random.Next()),
                    new SingleDocValuesField("floatdocvalues", Random.NextSingle())
                };
                iw.AddDocument(document);
            }
            reader = iw.GetReader();
            iw.Dispose();
            searcher = NewSearcher(reader);
        }

        [TearDown]
        public override void TearDown()
        {
            reader.Dispose();
            dir.Dispose();
            base.TearDown();
        }

        [Test]
        public virtual void TestQueries()
        {
            int n = AtLeast(4);
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
                // force in order
                bq.Add(new TermQuery(new Term("english", "two")), Occur.SHOULD);
                bq.MinimumNumberShouldMatch = 2;
                AssertQuery(bq, null);
            }
        }

        
        internal virtual void AssertQuery(Query query, Filter filter)
        {
            for (int i = 0; i < 10; i++)
            {
                bool reversed = Random.NextBoolean();
                SortField[] fields =
                {
                    new SortField("int", SortFieldType.INT32, reversed),
                    new SortField("long", SortFieldType.INT64, reversed),
                    new SortField("float", SortFieldType.SINGLE, reversed),
                    new SortField("double", SortFieldType.DOUBLE, reversed),
                    new SortField("intdocvalues", SortFieldType.INT32, reversed),
                    new SortField("floatdocvalues", SortFieldType.SINGLE, reversed),
                    new SortField("score", SortFieldType.SCORE)
                };
                fields.Shuffle(Random);
                int numSorts = TestUtil.NextInt32(Random, 1, fields.Length);
                AssertQuery(query, filter, new Sort(Arrays.CopyOfRange(fields, 0, numSorts)));
            }
        }

        
        internal virtual void AssertQuery(Query query, Filter filter, Sort sort)
        {
            int size = TestUtil.NextInt32(Random, 1, searcher.IndexReader.MaxDoc / 5);
            TopDocs expected = searcher.Search(query, filter, size, sort, Random.NextBoolean(), Random.NextBoolean());

            // make our actual sort, mutating original by replacing some of the 
            // sortfields with equivalent expressions

            SortField[] original = sort.GetSort();
            SortField[] mutated = new SortField[original.Length];
            for (int i = 0; i < mutated.Length; i++)
            {
                if (Random.Next(3) > 0)
                {
                    SortField s = original[i];
                    Expression expr = JavascriptCompiler.Compile(s.Field);
                    SimpleBindings simpleBindings = new SimpleBindings();
                    simpleBindings.Add(s);
                    bool reverse = s.Type == SortFieldType.SCORE || s.IsReverse;
                    mutated[i] = expr.GetSortField(simpleBindings, reverse);
                }
                else
                {
                    mutated[i] = original[i];
                }
            }
            Sort mutatedSort = new Sort(mutated);
            TopDocs actual = searcher.Search(query, filter, size, mutatedSort, Random.NextBoolean(), Random.NextBoolean());
            CheckHits.CheckEqual(query, expected.ScoreDocs, actual.ScoreDocs);
            if (size < actual.TotalHits)
            {
                expected = searcher.SearchAfter(expected.ScoreDocs[size - 1], query, filter, size, sort);
                actual = searcher.SearchAfter(actual.ScoreDocs[size - 1], query, filter, size, mutatedSort);
                CheckHits.CheckEqual(query, expected.ScoreDocs, actual.ScoreDocs);
            }
        }
    }
}
