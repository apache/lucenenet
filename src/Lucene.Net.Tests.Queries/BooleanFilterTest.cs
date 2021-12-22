// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Tests.Queries
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

    public class BooleanFilterTest : LuceneTestCase
    {
        private Directory directory;
        private AtomicReader reader;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false));

            //Add series of docs with filterable fields : acces rights, prices, dates and "in-stock" flags
            AddDoc(writer, @"admin guest", @"010", @"20040101", @"Y");
            AddDoc(writer, @"guest", @"020", @"20040101", @"Y");
            AddDoc(writer, @"guest", @"020", @"20050101", @"Y");
            AddDoc(writer, @"admin", @"020", @"20050101", @"Maybe");
            AddDoc(writer, @"admin guest", @"030", @"20050101", @"N");
            reader = SlowCompositeReaderWrapper.Wrap(writer.GetReader());
            writer.Dispose();
        }

        [TearDown]
        public override void TearDown()
        {
            reader.Dispose();
            directory.Dispose();
            base.TearDown();
        }

        private void AddDoc(RandomIndexWriter writer, string accessRights, string price, string date, string inStock)
        {
            Document doc = new Document();
            doc.Add(NewTextField(@"accessRights", accessRights, Field.Store.YES));
            doc.Add(NewTextField(@"price", price, Field.Store.YES));
            doc.Add(NewTextField(@"date", date, Field.Store.YES));
            doc.Add(NewTextField(@"inStock", inStock, Field.Store.YES));
            writer.AddDocument(doc);
        }

        private Filter GetRangeFilter(string field, string lowerPrice, string upperPrice)
        {
            Filter f = TermRangeFilter.NewStringRange(field, lowerPrice, upperPrice, true, true);
            return f;
        }

        private Filter GetTermsFilter(string field, string text)
        {
            return new TermsFilter(new Term(field, text));
        }

        private Filter GetWrappedTermQuery(string field, string text)
        {
            return new QueryWrapperFilter(new TermQuery(new Term(field, text)));
        }

        private Filter GetEmptyFilter()
        {
            return new FilterAnonymousClass();
        }

        private sealed class FilterAnonymousClass : Filter
        {
            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                return new FixedBitSet(context.Reader.MaxDoc);
            }
        }

        private Filter GetNullDISFilter()
        {
            return new FilterAnonymousClass1();
        }

        private sealed class FilterAnonymousClass1 : Filter
        {
            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                return null;
            }
        }

        private Filter GetNullDISIFilter()
        {
            return new FilterAnonymousClass2();
        }

        private sealed class DocIdSetAnonymousClass : DocIdSet
        {
            public override DocIdSetIterator GetIterator()
            {
                return null;
            }

            public override bool IsCacheable => true;
        }

        private sealed class FilterAnonymousClass2 : Filter
        {
            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                return new DocIdSetAnonymousClass();
            }
        }

        private void TstFilterCard(string mes, int expected, Filter filt)
        {
            DocIdSet docIdSet = filt.GetDocIdSet(reader.AtomicContext, reader.LiveDocs);
            int actual = 0;
            if (docIdSet != null)
            {
                DocIdSetIterator disi = docIdSet.GetIterator();
                while (disi.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                {
                    actual++;
                }
            }

            assertEquals(mes, expected, actual);
        }

        [Test]
        public void TestShould()
        {
            BooleanFilter booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetTermsFilter(@"price", @"030"), Occur.SHOULD);
            TstFilterCard(@"Should retrieves only 1 doc", 1, booleanFilter);

            // same with a real DISI (no OpenBitSetIterator)
            booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetWrappedTermQuery(@"price", @"030"), Occur.SHOULD);
            TstFilterCard(@"Should retrieves only 1 doc", 1, booleanFilter);
        }

        [Test]
        public void TestShoulds()
        {
            BooleanFilter booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetRangeFilter(@"price", @"010", @"020"), Occur.SHOULD);
            booleanFilter.Add(GetRangeFilter(@"price", @"020", @"030"), Occur.SHOULD);
            TstFilterCard(@"Shoulds are Ored together", 5, booleanFilter);
        }

        [Test]
        public void TestShouldsAndMustNot()
        {
            BooleanFilter booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetRangeFilter(@"price", @"010", @"020"), Occur.SHOULD);
            booleanFilter.Add(GetRangeFilter(@"price", @"020", @"030"), Occur.SHOULD);
            booleanFilter.Add(GetTermsFilter(@"inStock", @"N"), Occur.MUST_NOT);
            TstFilterCard(@"Shoulds Ored but AndNot", 4, booleanFilter);

            booleanFilter.Add(GetTermsFilter(@"inStock", @"Maybe"), Occur.MUST_NOT);
            TstFilterCard(@"Shoulds Ored but AndNots", 3, booleanFilter);

            // same with a real DISI (no OpenBitSetIterator)
            booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetRangeFilter(@"price", @"010", @"020"), Occur.SHOULD);
            booleanFilter.Add(GetRangeFilter(@"price", @"020", @"030"), Occur.SHOULD);
            booleanFilter.Add(GetWrappedTermQuery(@"inStock", @"N"), Occur.MUST_NOT);
            TstFilterCard(@"Shoulds Ored but AndNot", 4, booleanFilter);

            booleanFilter.Add(GetWrappedTermQuery(@"inStock", @"Maybe"), Occur.MUST_NOT);
            TstFilterCard(@"Shoulds Ored but AndNots", 3, booleanFilter);
        }

        [Test]
        public void TestShouldsAndMust()
        {
            BooleanFilter booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetRangeFilter(@"price", @"010", @"020"), Occur.SHOULD);
            booleanFilter.Add(GetRangeFilter(@"price", @"020", @"030"), Occur.SHOULD);
            booleanFilter.Add(GetTermsFilter(@"accessRights", @"admin"), Occur.MUST);
            TstFilterCard(@"Shoulds Ored but MUST", 3, booleanFilter);

            // same with a real DISI (no OpenBitSetIterator)
            booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetRangeFilter(@"price", @"010", @"020"), Occur.SHOULD);
            booleanFilter.Add(GetRangeFilter(@"price", @"020", @"030"), Occur.SHOULD);
            booleanFilter.Add(GetWrappedTermQuery(@"accessRights", @"admin"), Occur.MUST);
            TstFilterCard(@"Shoulds Ored but MUST", 3, booleanFilter);
        }

        [Test]
        public void TestShouldsAndMusts()
        {
            BooleanFilter booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetRangeFilter(@"price", @"010", @"020"), Occur.SHOULD);
            booleanFilter.Add(GetRangeFilter(@"price", @"020", @"030"), Occur.SHOULD);
            booleanFilter.Add(GetTermsFilter(@"accessRights", @"admin"), Occur.MUST);
            booleanFilter.Add(GetRangeFilter(@"date", @"20040101", @"20041231"), Occur.MUST);
            TstFilterCard(@"Shoulds Ored but MUSTs ANDED", 1, booleanFilter);
        }

        [Test]
        public void TestShouldsAndMustsAndMustNot()
        {
            BooleanFilter booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetRangeFilter(@"price", @"030", @"040"), Occur.SHOULD);
            booleanFilter.Add(GetTermsFilter(@"accessRights", @"admin"), Occur.MUST);
            booleanFilter.Add(GetRangeFilter(@"date", @"20050101", @"20051231"), Occur.MUST);
            booleanFilter.Add(GetTermsFilter(@"inStock", @"N"), Occur.MUST_NOT);
            TstFilterCard(@"Shoulds Ored but MUSTs ANDED and MustNot", 0, booleanFilter);

            // same with a real DISI (no OpenBitSetIterator)
            booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetRangeFilter(@"price", @"030", @"040"), Occur.SHOULD);
            booleanFilter.Add(GetWrappedTermQuery(@"accessRights", @"admin"), Occur.MUST);
            booleanFilter.Add(GetRangeFilter(@"date", @"20050101", @"20051231"), Occur.MUST);
            booleanFilter.Add(GetWrappedTermQuery(@"inStock", @"N"), Occur.MUST_NOT);
            TstFilterCard(@"Shoulds Ored but MUSTs ANDED and MustNot", 0, booleanFilter);
        }

        [Test]
        public void TestJustMust()
        {
            BooleanFilter booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetTermsFilter(@"accessRights", @"admin"), Occur.MUST);
            TstFilterCard(@"MUST", 3, booleanFilter);

            // same with a real DISI (no OpenBitSetIterator)
            booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetWrappedTermQuery(@"accessRights", @"admin"), Occur.MUST);
            TstFilterCard(@"MUST", 3, booleanFilter);
        }

        [Test]
        public void TestJustMustNot()
        {
            BooleanFilter booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetTermsFilter(@"inStock", @"N"), Occur.MUST_NOT);
            TstFilterCard(@"MUST_NOT", 4, booleanFilter);

            // same with a real DISI (no OpenBitSetIterator)
            booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetWrappedTermQuery(@"inStock", @"N"), Occur.MUST_NOT);
            TstFilterCard(@"MUST_NOT", 4, booleanFilter);
        }

        [Test]
        public void TestMustAndMustNot()
        {
            BooleanFilter booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetTermsFilter(@"inStock", @"N"), Occur.MUST);
            booleanFilter.Add(GetTermsFilter(@"price", @"030"), Occur.MUST_NOT);
            TstFilterCard(@"MUST_NOT wins over MUST for same docs", 0, booleanFilter);

            // same with a real DISI (no OpenBitSetIterator)
            booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetWrappedTermQuery(@"inStock", @"N"), Occur.MUST);
            booleanFilter.Add(GetWrappedTermQuery(@"price", @"030"), Occur.MUST_NOT);
            TstFilterCard(@"MUST_NOT wins over MUST for same docs", 0, booleanFilter);
        }

        [Test]
        public void TestEmpty()
        {
            BooleanFilter booleanFilter = new BooleanFilter();
            TstFilterCard(@"empty BooleanFilter returns no results", 0, booleanFilter);
        }

        [Test]
        public void TestCombinedNullDocIdSets()
        {
            BooleanFilter booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetTermsFilter(@"price", @"030"), Occur.MUST);
            booleanFilter.Add(GetNullDISFilter(), Occur.MUST);
            TstFilterCard(@"A MUST filter that returns a null DIS should never return documents", 0, booleanFilter);

            booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetTermsFilter(@"price", @"030"), Occur.MUST);
            booleanFilter.Add(GetNullDISIFilter(), Occur.MUST);
            TstFilterCard(@"A MUST filter that returns a null DISI should never return documents", 0, booleanFilter);

            booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetTermsFilter(@"price", @"030"), Occur.SHOULD);
            booleanFilter.Add(GetNullDISFilter(), Occur.SHOULD);
            TstFilterCard(@"A SHOULD filter that returns a null DIS should be invisible", 1, booleanFilter);

            booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetTermsFilter(@"price", @"030"), Occur.SHOULD);
            booleanFilter.Add(GetNullDISIFilter(), Occur.SHOULD);
            TstFilterCard(@"A SHOULD filter that returns a null DISI should be invisible", 1, booleanFilter);

            booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetTermsFilter(@"price", @"030"), Occur.MUST);
            booleanFilter.Add(GetNullDISFilter(), Occur.MUST_NOT);
            TstFilterCard(@"A MUST_NOT filter that returns a null DIS should be invisible", 1, booleanFilter);

            booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetTermsFilter(@"price", @"030"), Occur.MUST);
            booleanFilter.Add(GetNullDISIFilter(), Occur.MUST_NOT);
            TstFilterCard(@"A MUST_NOT filter that returns a null DISI should be invisible", 1, booleanFilter);
        }

        [Test]
        public void TestJustNullDocIdSets()
        {
            BooleanFilter booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetNullDISFilter(), Occur.MUST);
            TstFilterCard(@"A MUST filter that returns a null DIS should never return documents", 0, booleanFilter);

            booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetNullDISIFilter(), Occur.MUST);
            TstFilterCard(@"A MUST filter that returns a null DISI should never return documents", 0, booleanFilter);

            booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetNullDISFilter(), Occur.SHOULD);
            TstFilterCard(@"A single SHOULD filter that returns a null DIS should never return documents", 0, booleanFilter);

            booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetNullDISIFilter(), Occur.SHOULD);
            TstFilterCard(@"A single SHOULD filter that returns a null DISI should never return documents", 0, booleanFilter);

            booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetNullDISFilter(), Occur.MUST_NOT);
            TstFilterCard(@"A single MUST_NOT filter that returns a null DIS should be invisible", 5, booleanFilter);

            booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetNullDISIFilter(), Occur.MUST_NOT);
            TstFilterCard(@"A single MUST_NOT filter that returns a null DIS should be invisible", 5, booleanFilter);
        }

        [Test]
        public void TestNonMatchingShouldsAndMusts()
        {
            BooleanFilter booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetEmptyFilter(), Occur.SHOULD);
            booleanFilter.Add(GetTermsFilter(@"accessRights", @"admin"), Occur.MUST);
            TstFilterCard(@">0 shoulds with no matches should return no docs", 0, booleanFilter);

            booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetNullDISFilter(), Occur.SHOULD);
            booleanFilter.Add(GetTermsFilter(@"accessRights", @"admin"), Occur.MUST);
            TstFilterCard(@">0 shoulds with no matches should return no docs", 0, booleanFilter);

            booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetNullDISIFilter(), Occur.SHOULD);
            booleanFilter.Add(GetTermsFilter(@"accessRights", @"admin"), Occur.MUST);
            TstFilterCard(@">0 shoulds with no matches should return no docs", 0, booleanFilter);
        }

        [Test]
        public void TestToStringOfBooleanFilterContainingTermsFilter()
        {
            BooleanFilter booleanFilter = new BooleanFilter();
            booleanFilter.Add(GetTermsFilter(@"inStock", @"N"), Occur.MUST);
            booleanFilter.Add(GetTermsFilter(@"isFragile", @"Y"), Occur.MUST);

            assertEquals(@"BooleanFilter(+inStock:N +isFragile:Y)", booleanFilter.ToString());
        }

        [Test]
        public void TestToStringOfWrappedBooleanFilters()
        {
            BooleanFilter orFilter = new BooleanFilter();

            BooleanFilter stockFilter = new BooleanFilter();
            stockFilter.Add(new FilterClause(GetTermsFilter(@"inStock", @"Y"), Occur.MUST));
            stockFilter.Add(new FilterClause(GetTermsFilter(@"barCode", @"12345678"), Occur.MUST));

            orFilter.Add(new FilterClause(stockFilter, Occur.SHOULD));

            BooleanFilter productPropertyFilter = new BooleanFilter();
            productPropertyFilter.Add(new FilterClause(GetTermsFilter(@"isHeavy", @"N"), Occur.MUST));
            productPropertyFilter.Add(new FilterClause(GetTermsFilter(@"isDamaged", @"Y"), Occur.MUST));

            orFilter.Add(new FilterClause(productPropertyFilter, Occur.SHOULD));

            BooleanFilter composedFilter = new BooleanFilter();
            composedFilter.Add(new FilterClause(orFilter, Occur.MUST));
            assertEquals(@"BooleanFilter(+BooleanFilter(BooleanFilter(+inStock:Y +barCode:12345678) BooleanFilter(+isHeavy:N +isDamaged:Y)))", composedFilter.ToString());
        }
    }
}
