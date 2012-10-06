/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for Additional information regarding copyright ownership.
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;

using NUnit.Framework;

namespace Lucene.Net.Search
{
    [TestFixture]
    public class BooleanFilterTest : TestCase
    {
        private RAMDirectory directory;
        private IndexReader reader;

        [SetUp]
        protected void SetUp()
        {
            directory = new RAMDirectory();
            IndexWriter writer = new IndexWriter(directory, new WhitespaceAnalyzer(), true, IndexWriter.MaxFieldLength.UNLIMITED);

            //Add series of docs with filterable fields : acces rights, prices, dates and "in-stock" flags
            AddDoc(writer, "admin guest", "010", "20040101", "Y");
            AddDoc(writer, "guest", "020", "20040101", "Y");
            AddDoc(writer, "guest", "020", "20050101", "Y");
            AddDoc(writer, "admin", "020", "20050101", "Maybe");
            AddDoc(writer, "admin guest", "030", "20050101", "N");

            writer.Close();
            reader = IndexReader.Open(directory, true);
        }

        private void AddDoc(IndexWriter writer, String accessRights, String price, String date, String inStock)
        {
            Document doc = new Document();
            doc.Add(new Field("accessRights", accessRights, Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(new Field("price", price, Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(new Field("date", date, Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(new Field("inStock", inStock, Field.Store.YES, Field.Index.ANALYZED));
            writer.AddDocument(doc);
        }

        private Filter GetRangeFilter(String field, String lowerPrice, String upperPrice)
        {
            Filter f = new TermRangeFilter(field, lowerPrice, upperPrice, true, true);
            return f;
        }

        private Filter GetTermsFilter(String field, String text)
        {
            TermsFilter tf = new TermsFilter();
            tf.AddTerm(new Term(field, text));

            return tf;
        }

        private void TstFilterCard(String mes, int expected, Filter filt)
        {
            DocIdSetIterator disi = filt.GetDocIdSet(reader).Iterator();
            int actual = 0;
            while (disi.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
            {
                actual++;
            }
            Assert.AreEqual(expected, actual, mes);
        }

        [Test]
        public void TestShould()
        {
            BooleanFilter booleanFilter = new BooleanFilter();
            booleanFilter.Add(new FilterClause(GetTermsFilter("price", "030"), Occur.SHOULD));
            TstFilterCard("Should retrieves only 1 doc", 1, booleanFilter);
        }

        [Test]
        public void TestShoulds()
        {
            BooleanFilter booleanFilter = new BooleanFilter();
            booleanFilter.Add(new FilterClause(GetRangeFilter("price", "010", "020"), Occur.SHOULD));
            booleanFilter.Add(new FilterClause(GetRangeFilter("price", "020", "030"), Occur.SHOULD));
            TstFilterCard("Shoulds are Ored together", 5, booleanFilter);
        }

        [Test]
        public void TestShouldsAndMustNot()
        {
            BooleanFilter booleanFilter = new BooleanFilter();
            booleanFilter.Add(new FilterClause(GetRangeFilter("price", "010", "020"), Occur.SHOULD));
            booleanFilter.Add(new FilterClause(GetRangeFilter("price", "020", "030"), Occur.SHOULD));
            booleanFilter.Add(new FilterClause(GetTermsFilter("inStock", "N"), Occur.MUST_NOT));
            TstFilterCard("Shoulds Ored but AndNot", 4, booleanFilter);

            booleanFilter.Add(new FilterClause(GetTermsFilter("inStock", "Maybe"), Occur.MUST_NOT));
            TstFilterCard("Shoulds Ored but AndNots", 3, booleanFilter);
        }

        [Test]
        public void TestShouldsAndMust()
        {
            BooleanFilter booleanFilter = new BooleanFilter();
            booleanFilter.Add(new FilterClause(GetRangeFilter("price", "010", "020"), Occur.SHOULD));
            booleanFilter.Add(new FilterClause(GetRangeFilter("price", "020", "030"), Occur.SHOULD));
            booleanFilter.Add(new FilterClause(GetTermsFilter("accessRights", "admin"), Occur.MUST));
            TstFilterCard("Shoulds Ored but MUST", 3, booleanFilter);
        }

        [Test]
        public void TestShouldsAndMusts()
        {
            BooleanFilter booleanFilter = new BooleanFilter();
            booleanFilter.Add(new FilterClause(GetRangeFilter("price", "010", "020"), Occur.SHOULD));
            booleanFilter.Add(new FilterClause(GetRangeFilter("price", "020", "030"), Occur.SHOULD));
            booleanFilter.Add(new FilterClause(GetTermsFilter("accessRights", "admin"), Occur.MUST));
            booleanFilter.Add(new FilterClause(GetRangeFilter("date", "20040101", "20041231"), Occur.MUST));
            TstFilterCard("Shoulds Ored but MUSTs ANDED", 1, booleanFilter);
        }

        [Test]
        public void TestShouldsAndMustsAndMustNot()
        {
            BooleanFilter booleanFilter = new BooleanFilter();
            booleanFilter.Add(new FilterClause(GetRangeFilter("price", "030", "040"), Occur.SHOULD));
            booleanFilter.Add(new FilterClause(GetTermsFilter("accessRights", "admin"), Occur.MUST));
            booleanFilter.Add(new FilterClause(GetRangeFilter("date", "20050101", "20051231"), Occur.MUST));
            booleanFilter.Add(new FilterClause(GetTermsFilter("inStock", "N"), Occur.MUST_NOT));
            TstFilterCard("Shoulds Ored but MUSTs ANDED and MustNot", 0, booleanFilter);
        }

        [Test]
        public void TestJustMust()
        {
            BooleanFilter booleanFilter = new BooleanFilter();
            booleanFilter.Add(new FilterClause(GetTermsFilter("accessRights", "admin"), Occur.MUST));
            TstFilterCard("MUST", 3, booleanFilter);
        }

        [Test]
        public void TestJustMustNot()
        {
            BooleanFilter booleanFilter = new BooleanFilter();
            booleanFilter.Add(new FilterClause(GetTermsFilter("inStock", "N"), Occur.MUST_NOT));
            TstFilterCard("MUST_NOT", 4, booleanFilter);
        }

        [Test]
        public void TestMustAndMustNot()
        {
            BooleanFilter booleanFilter = new BooleanFilter();
            booleanFilter.Add(new FilterClause(GetTermsFilter("inStock", "N"), Occur.MUST));
            booleanFilter.Add(new FilterClause(GetTermsFilter("price", "030"), Occur.MUST_NOT));
            TstFilterCard("MUST_NOT wins over MUST for same docs", 0, booleanFilter);
        }
    }
}
