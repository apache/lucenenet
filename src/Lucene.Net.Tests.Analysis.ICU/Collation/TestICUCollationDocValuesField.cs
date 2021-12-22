using ICU4N.Text;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Globalization;

namespace Lucene.Net.Collation
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
    /// trivial test of ICUCollationDocValuesField
    /// </summary>
    [SuppressCodecs("Lucene3x")]
    public class TestICUCollationDocValuesField : LuceneTestCase
    {
        [Test]
        public void TestBasic()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            Field field = NewField("field", "", StringField.TYPE_STORED);
            ICUCollationDocValuesField collationField = new ICUCollationDocValuesField("collated", Collator.GetInstance(new CultureInfo("en")));
            doc.Add(field);
            doc.Add(collationField);

            field.SetStringValue("ABC");
            collationField.SetStringValue("ABC");
            iw.AddDocument(doc);

            field.SetStringValue("abc");
            collationField.SetStringValue("abc");
            iw.AddDocument(doc);

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher @is = NewSearcher(ir);

            SortField sortField = new SortField("collated", SortFieldType.STRING);

            TopDocs td = @is.Search(new MatchAllDocsQuery(), 5, new Sort(sortField));
            assertEquals("abc", ir.Document(td.ScoreDocs[0].Doc).Get("field"));
            assertEquals("ABC", ir.Document(td.ScoreDocs[1].Doc).Get("field"));
            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestRanges()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            Field field = NewField("field", "", StringField.TYPE_STORED);
            Collator collator = Collator.GetInstance(CultureInfo.CurrentCulture); // uses -Dtests.locale
            if (Random.nextBoolean())
            {
                collator.Strength = CollationStrength.Primary;
            }
            ICUCollationDocValuesField collationField = new ICUCollationDocValuesField("collated", collator);
            doc.Add(field);
            doc.Add(collationField);

            int numDocs = AtLeast(500);
            for (int i = 0; i < numDocs; i++)
            {
                String value = TestUtil.RandomSimpleString(Random);
                field.SetStringValue(value);
                collationField.SetStringValue(value);
                iw.AddDocument(doc);
            }

            IndexReader ir = iw.GetReader();
            iw.Dispose();
            IndexSearcher @is = NewSearcher(ir);

            int numChecks = AtLeast(100);
            for (int i = 0; i < numChecks; i++)
            {
                String start = TestUtil.RandomSimpleString(Random);
                String end = TestUtil.RandomSimpleString(Random);
                BytesRef lowerVal = new BytesRef(collator.GetCollationKey(start).ToByteArray());
                BytesRef upperVal = new BytesRef(collator.GetCollationKey(end).ToByteArray());
                Query query = new ConstantScoreQuery(FieldCacheRangeFilter.NewBytesRefRange("collated", lowerVal, upperVal, true, true));
                DoTestRanges(@is, start, end, query, collator);
            }

            ir.Dispose();
            dir.Dispose();
        }

        private void DoTestRanges(IndexSearcher @is, String startPoint, String endPoint, Query query, Collator collator)
        {
            QueryUtils.Check(query);

            // positive test
            TopDocs docs = @is.Search(query, @is.IndexReader.MaxDoc);
            foreach (ScoreDoc doc in docs.ScoreDocs)
            {
                String value = @is.Doc(doc.Doc).Get("field");
                assertTrue(collator.Compare(value, startPoint) >= 0);
                assertTrue(collator.Compare(value, endPoint) <= 0);
            }

            // negative test
            BooleanQuery bq = new BooleanQuery();
            bq.Add(new MatchAllDocsQuery(), Occur.SHOULD);
            bq.Add(query, Occur.MUST_NOT);
            docs = @is.Search(bq, @is.IndexReader.MaxDoc);
            foreach (ScoreDoc doc in docs.ScoreDocs)
            {
                String value = @is.Doc(doc.Doc).Get("field");
                assertTrue(collator.Compare(value, startPoint) < 0 || collator.Compare(value, endPoint) > 0);
            }
        }
    }
}
