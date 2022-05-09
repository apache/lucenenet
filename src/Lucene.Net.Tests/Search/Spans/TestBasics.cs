﻿using J2N.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Search.Spans
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using English = Lucene.Net.Util.English;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// Tests basic search capabilities.
    ///
    /// <p>Uses a collection of 1000 documents, each the english rendition of their
    /// document number.  For example, the document numbered 333 has text "three
    /// hundred thirty three".
    ///
    /// <p>Tests are each a single query, and its hits are checked to ensure that
    /// all and only the correct documents are returned, thus providing end-to-end
    /// testing of the indexing and search code.
    ///
    /// </summary>
    [TestFixture]
    public class TestBasics : LuceneTestCase
    {
        private static IndexSearcher searcher;
        private static IndexReader reader;
        private static Directory directory;

        internal sealed class SimplePayloadFilter : TokenFilter
        {
            internal int pos;
            internal readonly IPayloadAttribute payloadAttr;
            internal readonly ICharTermAttribute termAttr;

            public SimplePayloadFilter(TokenStream input)
                : base(input)
            {
                pos = 0;
                payloadAttr = input.AddAttribute<IPayloadAttribute>();
                termAttr = input.AddAttribute<ICharTermAttribute>();
            }

            public override bool IncrementToken()
            {
                if (m_input.IncrementToken())
                {
#pragma warning disable 612, 618
                    payloadAttr.Payload = new BytesRef(("pos: " + pos).GetBytes(IOUtils.CHARSET_UTF_8));
#pragma warning restore 612, 618
                    pos++;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public override void Reset()
            {
                base.Reset();
                pos = 0;
            }
        }

        internal static Analyzer simplePayloadAnalyzer;

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because NewIndexWriterConfig is no longer static.
        /// </summary>
        [OneTimeSetUp]
        public override void BeforeClass()
        {
            base.BeforeClass();

            simplePayloadAnalyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader2) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader2, MockTokenizer.SIMPLE, true);
                return new TokenStreamComponents(tokenizer, new SimplePayloadFilter(tokenizer));
            });

             directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, simplePayloadAnalyzer).SetMaxBufferedDocs(TestUtil.NextInt32(Random, 100, 1000)).SetMergePolicy(NewLogMergePolicy()));
            //writer.infoStream = System.out;
            for (int i = 0; i < 2000; i++)
            {
                Document doc = new Document();
                doc.Add(NewTextField("field", English.Int32ToEnglish(i), Field.Store.YES));
                writer.AddDocument(doc);
            }
            reader = writer.GetReader();
            searcher = NewSearcher(reader);
            writer.Dispose();
        }

        [OneTimeTearDown]
        public override void AfterClass()
        {
            reader.Dispose();
            directory.Dispose();
            searcher = null;
            reader = null;
            directory = null;
            simplePayloadAnalyzer = null;
            base.AfterClass();
        }

        [Test]
        public virtual void TestTerm()
        {
            Query query = new TermQuery(new Term("field", "seventy"));
            CheckHits(query, new int[] { 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 270, 271, 272, 273, 274, 275, 276, 277, 278, 279, 370, 371, 372, 373, 374, 375, 376, 377, 378, 379, 470, 471, 472, 473, 474, 475, 476, 477, 478, 479, 570, 571, 572, 573, 574, 575, 576, 577, 578, 579, 670, 671, 672, 673, 674, 675, 676, 677, 678, 679, 770, 771, 772, 773, 774, 775, 776, 777, 778, 779, 870, 871, 872, 873, 874, 875, 876, 877, 878, 879, 970, 971, 972, 973, 974, 975, 976, 977, 978, 979, 1070, 1071, 1072, 1073, 1074, 1075, 1076, 1077, 1078, 1079, 1170, 1171, 1172, 1173, 1174, 1175, 1176, 1177, 1178, 1179, 1270, 1271, 1272, 1273, 1274, 1275, 1276, 1277, 1278, 1279, 1370, 1371, 1372, 1373, 1374, 1375, 1376, 1377, 1378, 1379, 1470, 1471, 1472, 1473, 1474, 1475, 1476, 1477, 1478, 1479, 1570, 1571, 1572, 1573, 1574, 1575, 1576, 1577, 1578, 1579, 1670, 1671, 1672, 1673, 1674, 1675, 1676, 1677, 1678, 1679, 1770, 1771, 1772, 1773, 1774, 1775, 1776, 1777, 1778, 1779, 1870, 1871, 1872, 1873, 1874, 1875, 1876, 1877, 1878, 1879, 1970, 1971, 1972, 1973, 1974, 1975, 1976, 1977, 1978, 1979 });
        }

        [Test]
        public virtual void TestTerm2()
        {
            Query query = new TermQuery(new Term("field", "seventish"));
            CheckHits(query, new int[] { });
        }

        [Test]
        public virtual void TestPhrase()
        {
            PhraseQuery query = new PhraseQuery();
            query.Add(new Term("field", "seventy"));
            query.Add(new Term("field", "seven"));
            CheckHits(query, new int[] { 77, 177, 277, 377, 477, 577, 677, 777, 877, 977, 1077, 1177, 1277, 1377, 1477, 1577, 1677, 1777, 1877, 1977 });
        }

        [Test]
        public virtual void TestPhrase2()
        {
            PhraseQuery query = new PhraseQuery();
            query.Add(new Term("field", "seventish"));
            query.Add(new Term("field", "sevenon"));
            CheckHits(query, new int[] { });
        }

        [Test]
        public virtual void TestBoolean()
        {
            BooleanQuery query = new BooleanQuery();
            query.Add(new TermQuery(new Term("field", "seventy")), Occur.MUST);
            query.Add(new TermQuery(new Term("field", "seven")), Occur.MUST);
            CheckHits(query, new int[] { 77, 177, 277, 377, 477, 577, 677, 770, 771, 772, 773, 774, 775, 776, 777, 778, 779, 877, 977, 1077, 1177, 1277, 1377, 1477, 1577, 1677, 1770, 1771, 1772, 1773, 1774, 1775, 1776, 1777, 1778, 1779, 1877, 1977 });
        }

        [Test]
        public virtual void TestBoolean2()
        {
            BooleanQuery query = new BooleanQuery();
            query.Add(new TermQuery(new Term("field", "sevento")), Occur.MUST);
            query.Add(new TermQuery(new Term("field", "sevenly")), Occur.MUST);
            CheckHits(query, new int[] { });
        }

        [Test]
        public virtual void TestSpanNearExact()
        {
            SpanTermQuery term1 = new SpanTermQuery(new Term("field", "seventy"));
            SpanTermQuery term2 = new SpanTermQuery(new Term("field", "seven"));
            SpanNearQuery query = new SpanNearQuery(new SpanQuery[] { term1, term2 }, 0, true);
            CheckHits(query, new int[] { 77, 177, 277, 377, 477, 577, 677, 777, 877, 977, 1077, 1177, 1277, 1377, 1477, 1577, 1677, 1777, 1877, 1977 });

            Assert.IsTrue(searcher.Explain(query, 77).Value > 0.0f);
            Assert.IsTrue(searcher.Explain(query, 977).Value > 0.0f);

            QueryUtils.Check(term1);
            QueryUtils.Check(term2);
            QueryUtils.CheckUnequal(term1, term2);
        }

        [Test]
        public virtual void TestSpanTermQuery()
        {
            SpanTermQuery term1 = new SpanTermQuery(new Term("field", "seventy"));
            CheckHits(term1, new int[] { 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 270, 271, 272, 273, 274, 275, 276, 277, 278, 279, 370, 371, 372, 373, 374, 375, 376, 377, 378, 379, 470, 471, 472, 473, 474, 475, 476, 477, 478, 479, 570, 571, 572, 573, 574, 575, 576, 577, 578, 579, 670, 671, 672, 673, 674, 675, 676, 677, 678, 679, 770, 771, 772, 773, 774, 775, 776, 777, 778, 779, 870, 871, 872, 873, 874, 875, 876, 877, 878, 879, 970, 971, 972, 973, 974, 975, 976, 977, 978, 979, 1070, 1071, 1072, 1073, 1074, 1075, 1076, 1077, 1078, 1079, 1170, 1270, 1370, 1470, 1570, 1670, 1770, 1870, 1970, 1171, 1172, 1173, 1174, 1175, 1176, 1177, 1178, 1179, 1271, 1272, 1273, 1274, 1275, 1276, 1277, 1278, 1279, 1371, 1372, 1373, 1374, 1375, 1376, 1377, 1378, 1379, 1471, 1472, 1473, 1474, 1475, 1476, 1477, 1478, 1479, 1571, 1572, 1573, 1574, 1575, 1576, 1577, 1578, 1579, 1671, 1672, 1673, 1674, 1675, 1676, 1677, 1678, 1679, 1771, 1772, 1773, 1774, 1775, 1776, 1777, 1778, 1779, 1871, 1872, 1873, 1874, 1875, 1876, 1877, 1878, 1879, 1971, 1972, 1973, 1974, 1975, 1976, 1977, 1978, 1979 });
        }

        [Test]
        public virtual void TestSpanNearUnordered()
        {
            SpanTermQuery term1 = new SpanTermQuery(new Term("field", "nine"));
            SpanTermQuery term2 = new SpanTermQuery(new Term("field", "six"));
            SpanNearQuery query = new SpanNearQuery(new SpanQuery[] { term1, term2 }, 4, false);

            CheckHits(query, new int[] { 609, 629, 639, 649, 659, 669, 679, 689, 699, 906, 926, 936, 946, 956, 966, 976, 986, 996, 1609, 1629, 1639, 1649, 1659, 1669, 1679, 1689, 1699, 1906, 1926, 1936, 1946, 1956, 1966, 1976, 1986, 1996 });
        }

        [Test]
        public virtual void TestSpanNearOrdered()
        {
            SpanTermQuery term1 = new SpanTermQuery(new Term("field", "nine"));
            SpanTermQuery term2 = new SpanTermQuery(new Term("field", "six"));
            SpanNearQuery query = new SpanNearQuery(new SpanQuery[] { term1, term2 }, 4, true);
            CheckHits(query, new int[] { 906, 926, 936, 946, 956, 966, 976, 986, 996, 1906, 1926, 1936, 1946, 1956, 1966, 1976, 1986, 1996 });
        }

        [Test]
        public virtual void TestSpanNot()
        {
            SpanTermQuery term1 = new SpanTermQuery(new Term("field", "eight"));
            SpanTermQuery term2 = new SpanTermQuery(new Term("field", "one"));
            SpanNearQuery near = new SpanNearQuery(new SpanQuery[] { term1, term2 }, 4, true);
            SpanTermQuery term3 = new SpanTermQuery(new Term("field", "forty"));
            SpanNotQuery query = new SpanNotQuery(near, term3);

            CheckHits(query, new int[] { 801, 821, 831, 851, 861, 871, 881, 891, 1801, 1821, 1831, 1851, 1861, 1871, 1881, 1891 });

            Assert.IsTrue(searcher.Explain(query, 801).Value > 0.0f);
            Assert.IsTrue(searcher.Explain(query, 891).Value > 0.0f);
        }

        [Test]
        public virtual void TestSpanWithMultipleNotSingle()
        {
            SpanTermQuery term1 = new SpanTermQuery(new Term("field", "eight"));
            SpanTermQuery term2 = new SpanTermQuery(new Term("field", "one"));
            SpanNearQuery near = new SpanNearQuery(new SpanQuery[] { term1, term2 }, 4, true);
            SpanTermQuery term3 = new SpanTermQuery(new Term("field", "forty"));

            SpanOrQuery or = new SpanOrQuery(term3);

            SpanNotQuery query = new SpanNotQuery(near, or);

            CheckHits(query, new int[] { 801, 821, 831, 851, 861, 871, 881, 891, 1801, 1821, 1831, 1851, 1861, 1871, 1881, 1891 });

            Assert.IsTrue(searcher.Explain(query, 801).Value > 0.0f);
            Assert.IsTrue(searcher.Explain(query, 891).Value > 0.0f);
        }

        [Test]
        public virtual void TestSpanWithMultipleNotMany()
        {
            SpanTermQuery term1 = new SpanTermQuery(new Term("field", "eight"));
            SpanTermQuery term2 = new SpanTermQuery(new Term("field", "one"));
            SpanNearQuery near = new SpanNearQuery(new SpanQuery[] { term1, term2 }, 4, true);
            SpanTermQuery term3 = new SpanTermQuery(new Term("field", "forty"));
            SpanTermQuery term4 = new SpanTermQuery(new Term("field", "sixty"));
            SpanTermQuery term5 = new SpanTermQuery(new Term("field", "eighty"));

            SpanOrQuery or = new SpanOrQuery(term3, term4, term5);

            SpanNotQuery query = new SpanNotQuery(near, or);

            CheckHits(query, new int[] { 801, 821, 831, 851, 871, 891, 1801, 1821, 1831, 1851, 1871, 1891 });

            Assert.IsTrue(searcher.Explain(query, 801).Value > 0.0f);
            Assert.IsTrue(searcher.Explain(query, 891).Value > 0.0f);
        }

        [Test]
        public virtual void TestNpeInSpanNearWithSpanNot()
        {
            SpanTermQuery term1 = new SpanTermQuery(new Term("field", "eight"));
            SpanTermQuery term2 = new SpanTermQuery(new Term("field", "one"));
            SpanNearQuery near = new SpanNearQuery(new SpanQuery[] { term1, term2 }, 4, true);
            SpanTermQuery hun = new SpanTermQuery(new Term("field", "hundred"));
            SpanTermQuery term3 = new SpanTermQuery(new Term("field", "forty"));
            SpanNearQuery exclude = new SpanNearQuery(new SpanQuery[] { hun, term3 }, 1, true);

            SpanNotQuery query = new SpanNotQuery(near, exclude);

            CheckHits(query, new int[] { 801, 821, 831, 851, 861, 871, 881, 891, 1801, 1821, 1831, 1851, 1861, 1871, 1881, 1891 });

            Assert.IsTrue(searcher.Explain(query, 801).Value > 0.0f);
            Assert.IsTrue(searcher.Explain(query, 891).Value > 0.0f);
        }

        [Test]
        public virtual void TestNpeInSpanNearInSpanFirstInSpanNot()
        {
            int n = 5;
            SpanTermQuery hun = new SpanTermQuery(new Term("field", "hundred"));
            SpanTermQuery term40 = new SpanTermQuery(new Term("field", "forty"));
            SpanTermQuery term40c = (SpanTermQuery)term40.Clone();

            SpanFirstQuery include = new SpanFirstQuery(term40, n);
            SpanNearQuery near = new SpanNearQuery(new SpanQuery[] { hun, term40c }, n - 1, true);
            SpanFirstQuery exclude = new SpanFirstQuery(near, n - 1);
            SpanNotQuery q = new SpanNotQuery(include, exclude);

            CheckHits(q, new int[] { 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 1040, 1041, 1042, 1043, 1044, 1045, 1046, 1047, 1048, 1049, 1140, 1141, 1142, 1143, 1144, 1145, 1146, 1147, 1148, 1149, 1240, 1241, 1242, 1243, 1244, 1245, 1246, 1247, 1248, 1249, 1340, 1341, 1342, 1343, 1344, 1345, 1346, 1347, 1348, 1349, 1440, 1441, 1442, 1443, 1444, 1445, 1446, 1447, 1448, 1449, 1540, 1541, 1542, 1543, 1544, 1545, 1546, 1547, 1548, 1549, 1640, 1641, 1642, 1643, 1644, 1645, 1646, 1647, 1648, 1649, 1740, 1741, 1742, 1743, 1744, 1745, 1746, 1747, 1748, 1749, 1840, 1841, 1842, 1843, 1844, 1845, 1846, 1847, 1848, 1849, 1940, 1941, 1942, 1943, 1944, 1945, 1946, 1947, 1948, 1949 });
        }

        [Test]
        public virtual void TestSpanNotWindowOne()
        {
            SpanTermQuery term1 = new SpanTermQuery(new Term("field", "eight"));
            SpanTermQuery term2 = new SpanTermQuery(new Term("field", "forty"));
            SpanNearQuery near = new SpanNearQuery(new SpanQuery[] { term1, term2 }, 4, true);
            SpanTermQuery term3 = new SpanTermQuery(new Term("field", "one"));
            SpanNotQuery query = new SpanNotQuery(near, term3, 1, 1);

            CheckHits(query, new int[] { 840, 842, 843, 844, 845, 846, 847, 848, 849, 1840, 1842, 1843, 1844, 1845, 1846, 1847, 1848, 1849 });

            Assert.IsTrue(searcher.Explain(query, 840).Value > 0.0f);
            Assert.IsTrue(searcher.Explain(query, 1842).Value > 0.0f);
        }

        [Test]
        public virtual void TestSpanNotWindowTwoBefore()
        {
            SpanTermQuery term1 = new SpanTermQuery(new Term("field", "eight"));
            SpanTermQuery term2 = new SpanTermQuery(new Term("field", "forty"));
            SpanNearQuery near = new SpanNearQuery(new SpanQuery[] { term1, term2 }, 4, true);
            SpanTermQuery term3 = new SpanTermQuery(new Term("field", "one"));
            SpanNotQuery query = new SpanNotQuery(near, term3, 2, 0);

            CheckHits(query, new int[] { 840, 841, 842, 843, 844, 845, 846, 847, 848, 849 });

            Assert.IsTrue(searcher.Explain(query, 840).Value > 0.0f);
            Assert.IsTrue(searcher.Explain(query, 849).Value > 0.0f);
        }

        [Test]
        public virtual void TestSpanNotWindowNeg()
        {
            //test handling of invalid window < 0
            SpanTermQuery term1 = new SpanTermQuery(new Term("field", "eight"));
            SpanTermQuery term2 = new SpanTermQuery(new Term("field", "one"));
            SpanNearQuery near = new SpanNearQuery(new SpanQuery[] { term1, term2 }, 4, true);
            SpanTermQuery term3 = new SpanTermQuery(new Term("field", "forty"));

            SpanOrQuery or = new SpanOrQuery(term3);

            SpanNotQuery query = new SpanNotQuery(near, or);

            CheckHits(query, new int[] { 801, 821, 831, 851, 861, 871, 881, 891, 1801, 1821, 1831, 1851, 1861, 1871, 1881, 1891 });

            Assert.IsTrue(searcher.Explain(query, 801).Value > 0.0f);
            Assert.IsTrue(searcher.Explain(query, 891).Value > 0.0f);
        }

        [Test]
        public virtual void TestSpanNotWindowDoubleExcludesBefore()
        {
            //test hitting two excludes before an include
            SpanTermQuery term1 = new SpanTermQuery(new Term("field", "forty"));
            SpanTermQuery term2 = new SpanTermQuery(new Term("field", "two"));
            SpanNearQuery near = new SpanNearQuery(new SpanTermQuery[] { term1, term2 }, 2, true);
            SpanTermQuery exclude = new SpanTermQuery(new Term("field", "one"));

            SpanNotQuery query = new SpanNotQuery(near, exclude, 4, 1);

            CheckHits(query, new int[] { 42, 242, 342, 442, 542, 642, 742, 842, 942 });

            Assert.IsTrue(searcher.Explain(query, 242).Value > 0.0f);
            Assert.IsTrue(searcher.Explain(query, 942).Value > 0.0f);
        }

        [Test]
        public virtual void TestSpanFirst()
        {
            SpanTermQuery term1 = new SpanTermQuery(new Term("field", "five"));
            SpanFirstQuery query = new SpanFirstQuery(term1, 1);

            CheckHits(query, new int[] { 5, 500, 501, 502, 503, 504, 505, 506, 507, 508, 509, 510, 511, 512, 513, 514, 515, 516, 517, 518, 519, 520, 521, 522, 523, 524, 525, 526, 527, 528, 529, 530, 531, 532, 533, 534, 535, 536, 537, 538, 539, 540, 541, 542, 543, 544, 545, 546, 547, 548, 549, 550, 551, 552, 553, 554, 555, 556, 557, 558, 559, 560, 561, 562, 563, 564, 565, 566, 567, 568, 569, 570, 571, 572, 573, 574, 575, 576, 577, 578, 579, 580, 581, 582, 583, 584, 585, 586, 587, 588, 589, 590, 591, 592, 593, 594, 595, 596, 597, 598, 599 });

            Assert.IsTrue(searcher.Explain(query, 5).Value > 0.0f);
            Assert.IsTrue(searcher.Explain(query, 599).Value > 0.0f);
        }

        [Test]
        public virtual void TestSpanPositionRange()
        {
            SpanPositionRangeQuery query;
            SpanTermQuery term1 = new SpanTermQuery(new Term("field", "five"));
            query = new SpanPositionRangeQuery(term1, 1, 2);
            CheckHits(query, new int[] { 25, 35, 45, 55, 65, 75, 85, 95 });
            Assert.IsTrue(searcher.Explain(query, 25).Value > 0.0f);
            Assert.IsTrue(searcher.Explain(query, 95).Value > 0.0f);

            query = new SpanPositionRangeQuery(term1, 0, 1);
            CheckHits(query, new int[] { 5, 500, 501, 502, 503, 504, 505, 506, 507, 508, 509, 510, 511, 512, 513, 514, 515, 516, 517, 518, 519, 520, 521, 522, 523, 524, 525, 526, 527, 528, 529, 530, 531, 532, 533, 534, 535, 536, 537, 538, 539, 540, 541, 542, 543, 544, 545, 546, 547, 548, 549, 550, 551, 552, 553, 554, 555, 556, 557, 558, 559, 560, 561, 562, 563, 564, 565, 566, 567, 568, 569, 570, 571, 572, 573, 574, 575, 576, 577, 578, 579, 580, 581, 582, 583, 584, 585, 586, 587, 588, 589, 590, 591, 592, 593, 594, 595, 596, 597, 598, 599 });

            query = new SpanPositionRangeQuery(term1, 6, 7);
            CheckHits(query, new int[] { });
        }

        [Test]
        public virtual void TestSpanPayloadCheck()
        {
            SpanTermQuery term1 = new SpanTermQuery(new Term("field", "five"));
#pragma warning disable 612, 618
            BytesRef pay = new BytesRef(("pos: " + 5).GetBytes(IOUtils.CHARSET_UTF_8));
#pragma warning restore 612, 618
            SpanQuery query = new SpanPayloadCheckQuery(term1, new JCG.List<byte[]>() { pay.Bytes });
            CheckHits(query, new int[] { 1125, 1135, 1145, 1155, 1165, 1175, 1185, 1195, 1225, 1235, 1245, 1255, 1265, 1275, 1285, 1295, 1325, 1335, 1345, 1355, 1365, 1375, 1385, 1395, 1425, 1435, 1445, 1455, 1465, 1475, 1485, 1495, 1525, 1535, 1545, 1555, 1565, 1575, 1585, 1595, 1625, 1635, 1645, 1655, 1665, 1675, 1685, 1695, 1725, 1735, 1745, 1755, 1765, 1775, 1785, 1795, 1825, 1835, 1845, 1855, 1865, 1875, 1885, 1895, 1925, 1935, 1945, 1955, 1965, 1975, 1985, 1995 });
            Assert.IsTrue(searcher.Explain(query, 1125).Value > 0.0f);

            SpanTermQuery term2 = new SpanTermQuery(new Term("field", "hundred"));
            SpanNearQuery snq;
            SpanQuery[] clauses;
            IList<byte[]> list;
            BytesRef pay2;
            clauses = new SpanQuery[2];
            clauses[0] = term1;
            clauses[1] = term2;
            snq = new SpanNearQuery(clauses, 0, true);
#pragma warning disable 612, 618
            pay = new BytesRef(("pos: " + 0).GetBytes(IOUtils.CHARSET_UTF_8));
            pay2 = new BytesRef(("pos: " + 1).GetBytes(IOUtils.CHARSET_UTF_8));
#pragma warning restore 612, 618
            list = new JCG.List<byte[]>();
            list.Add(pay.Bytes);
            list.Add(pay2.Bytes);
            query = new SpanNearPayloadCheckQuery(snq, list);
            CheckHits(query, new int[] { 500, 501, 502, 503, 504, 505, 506, 507, 508, 509, 510, 511, 512, 513, 514, 515, 516, 517, 518, 519, 520, 521, 522, 523, 524, 525, 526, 527, 528, 529, 530, 531, 532, 533, 534, 535, 536, 537, 538, 539, 540, 541, 542, 543, 544, 545, 546, 547, 548, 549, 550, 551, 552, 553, 554, 555, 556, 557, 558, 559, 560, 561, 562, 563, 564, 565, 566, 567, 568, 569, 570, 571, 572, 573, 574, 575, 576, 577, 578, 579, 580, 581, 582, 583, 584, 585, 586, 587, 588, 589, 590, 591, 592, 593, 594, 595, 596, 597, 598, 599 });
            clauses = new SpanQuery[3];
            clauses[0] = term1;
            clauses[1] = term2;
            clauses[2] = new SpanTermQuery(new Term("field", "five"));
            snq = new SpanNearQuery(clauses, 0, true);
#pragma warning disable 612, 618
            pay = new BytesRef(("pos: " + 0).GetBytes(IOUtils.CHARSET_UTF_8));
            pay2 = new BytesRef(("pos: " + 1).GetBytes(IOUtils.CHARSET_UTF_8));
            BytesRef pay3 = new BytesRef(("pos: " + 2).GetBytes(IOUtils.CHARSET_UTF_8));
#pragma warning restore 612, 618
            list = new JCG.List<byte[]>();
            list.Add(pay.Bytes);
            list.Add(pay2.Bytes);
            list.Add(pay3.Bytes);
            query = new SpanNearPayloadCheckQuery(snq, list);
            CheckHits(query, new int[] { 505 });
        }

        [Test]
        public virtual void TestComplexSpanChecks()
        {
            SpanTermQuery one = new SpanTermQuery(new Term("field", "one"));
            SpanTermQuery thous = new SpanTermQuery(new Term("field", "thousand"));
            //should be one position in between
            SpanTermQuery hundred = new SpanTermQuery(new Term("field", "hundred"));
            SpanTermQuery three = new SpanTermQuery(new Term("field", "three"));

            SpanNearQuery oneThous = new SpanNearQuery(new SpanQuery[] { one, thous }, 0, true);
            SpanNearQuery hundredThree = new SpanNearQuery(new SpanQuery[] { hundred, three }, 0, true);
            SpanNearQuery oneThousHunThree = new SpanNearQuery(new SpanQuery[] { oneThous, hundredThree }, 1, true);
            SpanQuery query;
            //this one's too small
            query = new SpanPositionRangeQuery(oneThousHunThree, 1, 2);
            CheckHits(query, new int[] { });
            //this one's just right
            query = new SpanPositionRangeQuery(oneThousHunThree, 0, 6);
            CheckHits(query, new int[] { 1103, 1203, 1303, 1403, 1503, 1603, 1703, 1803, 1903 });

            var payloads = new JCG.List<byte[]>();
#pragma warning disable 612, 618
            BytesRef pay = new BytesRef(("pos: " + 0).GetBytes(IOUtils.CHARSET_UTF_8));
            BytesRef pay2 = new BytesRef(("pos: " + 1).GetBytes(IOUtils.CHARSET_UTF_8));
            BytesRef pay3 = new BytesRef(("pos: " + 3).GetBytes(IOUtils.CHARSET_UTF_8));
            BytesRef pay4 = new BytesRef(("pos: " + 4).GetBytes(IOUtils.CHARSET_UTF_8));
#pragma warning restore 612, 618
            payloads.Add(pay.Bytes);
            payloads.Add(pay2.Bytes);
            payloads.Add(pay3.Bytes);
            payloads.Add(pay4.Bytes);
            query = new SpanNearPayloadCheckQuery(oneThousHunThree, payloads);
            CheckHits(query, new int[] { 1103, 1203, 1303, 1403, 1503, 1603, 1703, 1803, 1903 });
        }

        [Test]
        public virtual void TestSpanOr()
        {
            SpanTermQuery term1 = new SpanTermQuery(new Term("field", "thirty"));
            SpanTermQuery term2 = new SpanTermQuery(new Term("field", "three"));
            SpanNearQuery near1 = new SpanNearQuery(new SpanQuery[] { term1, term2 }, 0, true);
            SpanTermQuery term3 = new SpanTermQuery(new Term("field", "forty"));
            SpanTermQuery term4 = new SpanTermQuery(new Term("field", "seven"));
            SpanNearQuery near2 = new SpanNearQuery(new SpanQuery[] { term3, term4 }, 0, true);

            SpanOrQuery query = new SpanOrQuery(near1, near2);

            CheckHits(query, new int[] { 33, 47, 133, 147, 233, 247, 333, 347, 433, 447, 533, 547, 633, 647, 733, 747, 833, 847, 933, 947, 1033, 1047, 1133, 1147, 1233, 1247, 1333, 1347, 1433, 1447, 1533, 1547, 1633, 1647, 1733, 1747, 1833, 1847, 1933, 1947 });

            Assert.IsTrue(searcher.Explain(query, 33).Value > 0.0f);
            Assert.IsTrue(searcher.Explain(query, 947).Value > 0.0f);
        }

        [Test]
        public virtual void TestSpanExactNested()
        {
            SpanTermQuery term1 = new SpanTermQuery(new Term("field", "three"));
            SpanTermQuery term2 = new SpanTermQuery(new Term("field", "hundred"));
            SpanNearQuery near1 = new SpanNearQuery(new SpanQuery[] { term1, term2 }, 0, true);
            SpanTermQuery term3 = new SpanTermQuery(new Term("field", "thirty"));
            SpanTermQuery term4 = new SpanTermQuery(new Term("field", "three"));
            SpanNearQuery near2 = new SpanNearQuery(new SpanQuery[] { term3, term4 }, 0, true);

            SpanNearQuery query = new SpanNearQuery(new SpanQuery[] { near1, near2 }, 0, true);

            CheckHits(query, new int[] { 333, 1333 });

            Assert.IsTrue(searcher.Explain(query, 333).Value > 0.0f);
        }

        [Test]
        public virtual void TestSpanNearOr()
        {
            SpanTermQuery t1 = new SpanTermQuery(new Term("field", "six"));
            SpanTermQuery t3 = new SpanTermQuery(new Term("field", "seven"));

            SpanTermQuery t5 = new SpanTermQuery(new Term("field", "seven"));
            SpanTermQuery t6 = new SpanTermQuery(new Term("field", "six"));

            SpanOrQuery to1 = new SpanOrQuery(t1, t3);
            SpanOrQuery to2 = new SpanOrQuery(t5, t6);

            SpanNearQuery query = new SpanNearQuery(new SpanQuery[] { to1, to2 }, 10, true);

            CheckHits(query, new int[] { 606, 607, 626, 627, 636, 637, 646, 647, 656, 657, 666, 667, 676, 677, 686, 687, 696, 697, 706, 707, 726, 727, 736, 737, 746, 747, 756, 757, 766, 767, 776, 777, 786, 787, 796, 797, 1606, 1607, 1626, 1627, 1636, 1637, 1646, 1647, 1656, 1657, 1666, 1667, 1676, 1677, 1686, 1687, 1696, 1697, 1706, 1707, 1726, 1727, 1736, 1737, 1746, 1747, 1756, 1757, 1766, 1767, 1776, 1777, 1786, 1787, 1796, 1797 });
        }

        [Test]
        public virtual void TestSpanComplex1()
        {
            SpanTermQuery t1 = new SpanTermQuery(new Term("field", "six"));
            SpanTermQuery t2 = new SpanTermQuery(new Term("field", "hundred"));
            SpanNearQuery tt1 = new SpanNearQuery(new SpanQuery[] { t1, t2 }, 0, true);

            SpanTermQuery t3 = new SpanTermQuery(new Term("field", "seven"));
            SpanTermQuery t4 = new SpanTermQuery(new Term("field", "hundred"));
            SpanNearQuery tt2 = new SpanNearQuery(new SpanQuery[] { t3, t4 }, 0, true);

            SpanTermQuery t5 = new SpanTermQuery(new Term("field", "seven"));
            SpanTermQuery t6 = new SpanTermQuery(new Term("field", "six"));

            SpanOrQuery to1 = new SpanOrQuery(tt1, tt2);
            SpanOrQuery to2 = new SpanOrQuery(t5, t6);

            SpanNearQuery query = new SpanNearQuery(new SpanQuery[] { to1, to2 }, 100, true);

            CheckHits(query, new int[] { 606, 607, 626, 627, 636, 637, 646, 647, 656, 657, 666, 667, 676, 677, 686, 687, 696, 697, 706, 707, 726, 727, 736, 737, 746, 747, 756, 757, 766, 767, 776, 777, 786, 787, 796, 797, 1606, 1607, 1626, 1627, 1636, 1637, 1646, 1647, 1656, 1657, 1666, 1667, 1676, 1677, 1686, 1687, 1696, 1697, 1706, 1707, 1726, 1727, 1736, 1737, 1746, 1747, 1756, 1757, 1766, 1767, 1776, 1777, 1786, 1787, 1796, 1797 });
        }

        [Test]
        public virtual void TestSpansSkipTo()
        {
            SpanTermQuery t1 = new SpanTermQuery(new Term("field", "seventy"));
            SpanTermQuery t2 = new SpanTermQuery(new Term("field", "seventy"));
            Spans s1 = MultiSpansWrapper.Wrap(searcher.TopReaderContext, t1);
            Spans s2 = MultiSpansWrapper.Wrap(searcher.TopReaderContext, t2);

            Assert.IsTrue(s1.MoveNext());
            Assert.IsTrue(s2.MoveNext());

            bool hasMore = true;

            do
            {
                hasMore = SkipToAccoringToJavaDocs(s1, s1.Doc + 1);
                Assert.AreEqual(hasMore, s2.SkipTo(s2.Doc + 1));
                Assert.AreEqual(s1.Doc, s2.Doc);
            } while (hasMore);
        }

        /// <summary>
        /// Skips to the first match beyond the current, whose document number is
        /// greater than or equal to <i>target</i>. <p>Returns true iff there is such
        /// a match.  <p>Behaves as if written: <pre>
        ///   boolean skipTo(int target) {
        ///     do {
        ///       if (!next())
        ///       return false;
        ///     } while (target > doc());
        ///     return true;
        ///   }
        /// </pre>
        /// </summary>
        private bool SkipToAccoringToJavaDocs(Spans s, int target)
        {
            do
            {
                if (!s.MoveNext())
                {
                    return false;
                }
            } while (target > s.Doc);
            return true;
        }

        private void CheckHits(Query query, int[] results)
        {
            Search.CheckHits.DoCheckHits(Random, query, "field", searcher, results);
        }
    }
}