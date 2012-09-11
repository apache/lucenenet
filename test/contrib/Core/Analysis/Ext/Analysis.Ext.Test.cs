/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
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

using Lucene.Net.Analysis.Ext;
using Lucene.Net.Store;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers;
using NUnit.Framework;

namespace Lucene.Net.Test.Analysis.Ext
{
    [TestFixture]
    class TestAnalysisExt
    {
        [SetUp]
        public void Setup()
        {
            
        }

        IndexSearcher CreateIndex(string data,Analyzer analyzer)
        {
            RAMDirectory dir = new RAMDirectory();
            IndexWriter wr = new IndexWriter(dir, analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED);
            Document doc = new Document();
            doc.Add(new Field("field", data, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
            wr.AddDocument(doc);
            wr.Close();

            return new IndexSearcher(IndexReader.Open(dir, true));
        }

        [Test]
        public void TestSingleCharTokenAnalyzer()
        {
            Analyzer analyzer = new SingleCharTokenAnalyzer();
            IndexSearcher src = CreateIndex("someuser@gmail.com 1234567890 abcdefgh", analyzer);

            var p = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, "field", analyzer)
                        {
                            DefaultOperator = QueryParser.Operator.AND,
                            EnablePositionIncrements = true
                        };

            TopDocs td = null;

            td = src.Search(p.Parse("usergmail"), 10);
            Assert.AreEqual(0, td.TotalHits);

            td = src.Search(p.Parse("gmailcom"), 10);
            Assert.AreEqual(0, td.TotalHits);

            td = src.Search(p.Parse("678"), 10);
            Assert.AreEqual(1, td.TotalHits);

            td = src.Search(p.Parse("someuser"), 10);
            Assert.AreEqual(1, td.TotalHits);

            td = src.Search(p.Parse("omeuse"), 10);
            Assert.AreEqual(1, td.TotalHits);

            td = src.Search(p.Parse("omeuse 6789"), 10);
            Assert.AreEqual(1, td.TotalHits);

            td = src.Search(p.Parse("user gmail"), 10);
            Assert.AreEqual(1, td.TotalHits);

            td = src.Search(p.Parse("\"user gmail\""), 10);
            Assert.AreEqual(1, td.TotalHits);

            td = src.Search(p.Parse("user@gmail"), 10);
            Assert.AreEqual(1, td.TotalHits);

            td = src.Search(p.Parse("gmail.com"), 10);
            Assert.AreEqual(1, td.TotalHits);

            td = src.Search(p.Parse("\"gmail.com 1234\""), 10);
            Assert.AreEqual(1, td.TotalHits);

            td = src.Search(p.Parse("\"gmail.com defg\""), 10);
            Assert.AreEqual(0, td.TotalHits);

            td = src.Search(p.Parse("gmail.com defg"), 10);
            Assert.AreEqual(1, td.TotalHits);
        }

        //[Test]
        //public void TestSingleCharTokenAnalyzerHighlight()
        //{
        //    Analyzer analyzer = new SingleCharTokenAnalyzer();
        //    IndexSearcher src = CreateIndex("someuser@gmail.com 1234567890 abcdefgh", analyzer);

        //    QueryParser p = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, "field", analyzer);
        //    p.SetDefaultOperator(QueryParser.Operator.AND);
        //    p.SetMultiTermRewriteMethod(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE);

        //    Lucene.Net.Search.Vectorhighlight.FastVectorHighlighter fvh = new Lucene.Net.Search.Vectorhighlight.FastVectorHighlighter(true, true);

        //    Query q = null;
        //    string[] fragments = null;

        //    q = p.Parse("cde");
        //    fragments = fvh.GetBestFragments(fvh.GetFieldQuery(q), src.GetIndexReader(), 0, "field", 256, 10);
        //    Assert.IsTrue(fragments != null && fragments.Length > 0 && fragments[0].IndexOf("<b>cde</b>") >= 0);
            
        //    q = p.Parse("2345");
        //    fragments = fvh.GetBestFragments(fvh.GetFieldQuery(q), src.GetIndexReader(), 0, "field", 256, 10);
        //    Assert.IsTrue(fragments != null && fragments.Length > 0 && fragments[0].IndexOf("<b>2345</b>") >= 0);
            
        //    q = p.Parse("gmail 1234");
        //    fragments = fvh.GetBestFragments(fvh.GetFieldQuery(q), src.GetIndexReader(), 0, "field", 256, 10);
        //    Assert.IsTrue(fragments != null && fragments.Length > 0 && fragments[0].IndexOf("<b>gmail</b>.com <b>1234</b>") >= 0);
            
        //    /*
        //    q = p.Parse("gmail.com");
        //    fragments = fvh.GetBestFragments(fvh.GetFieldQuery(q), src.GetIndexReader(), 0, "field", 256, 10);
        //    Assert.IsTrue(fragments != null && fragments.Length > 0 && fragments[0].IndexOf("??????????") >= 0);
        //    System.Diagnostics.Debug.WriteLine(fragments[0]);
        //    */
        //}

        [Test]
        public void TestUnaccentedWordAnalyzer()
        {
            TopDocs td = null;
            string text = "Name.Surname@gmail.com 123.456 ğüşıöç%ĞÜŞİÖÇ$ΑΒΓΔΕΖ#АБВГДЕ SSß";
            string[] expectedTokens = new string[] { "name", "surname", "gmail", "com", "123", "456", "gusioc", "gusioc", "αβγδεζ" , "абвгде", "ssss"};

            UnaccentedWordAnalyzer analyzer = new UnaccentedWordAnalyzer();
            TokenStream ts = analyzer.TokenStream("", new System.IO.StringReader(text));
            
            int i = 0;
            ITermAttribute termAttribute = ts.GetAttribute<ITermAttribute>();
            while (ts.IncrementToken())
            {
                Assert.AreEqual(expectedTokens[i++], termAttribute.Term);
                System.Diagnostics.Debug.WriteLine(termAttribute.Term);
            }

            QueryParser p = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, "field", analyzer);
            IndexSearcher src = CreateIndex(text, analyzer);
            
            td = src.Search(p.Parse("ĞÜŞıöç"), 10);
            Assert.AreEqual(1, td.TotalHits);

            td = src.Search(p.Parse("name"), 10);
            Assert.AreEqual(1, td.TotalHits);

            td = src.Search(p.Parse("surname"), 10);
            Assert.AreEqual(1, td.TotalHits);

            td = src.Search(p.Parse("NAME.surname"), 10);
            Assert.AreEqual(1, td.TotalHits);

            td = src.Search(p.Parse("surname@gmail"), 10);
            Assert.AreEqual(1, td.TotalHits);

            td = src.Search(p.Parse("name@gmail"), 10);
            Assert.AreEqual(0, td.TotalHits);

            td = src.Search(p.Parse("456"), 10);
            Assert.AreEqual(1, td.TotalHits);

            td = src.Search(p.Parse("123.456"), 10);
            Assert.AreEqual(1, td.TotalHits);
        }
    }
}
