// LUCENENENET: Microsoft no longer considers it good practice to use binary serialization
// in new applications. Therefore, we are no longer marking queries serializable
// (It isn't serializable in Lucene 4.8.0 anymore anyway).
// See: https://github.com/dotnet/corefx/issues/23584

//#if FEATURE_SERIALIZABLE
//using Lucene.Net.Attributes;
//using Lucene.Net.Index;
//using Lucene.Net.Search;
//using Lucene.Net.Util;
//using NUnit.Framework;
//using Assert = Lucene.Net.TestFramework.Assert;

//#pragma warning disable 612, 618
//namespace Lucene.Net.Support
//{
//    /*
//     * Licensed to the Apache Software Foundation (ASF) under one or more
//     * contributor license agreements.  See the NOTICE file distributed with
//     * this work for additional information regarding copyright ownership.
//     * The ASF licenses this file to You under the Apache License, Version 2.0
//     * (the "License"); you may not use this file except in compliance with
//     * the License.  You may obtain a copy of the License at
//     *
//     *     http://www.apache.org/licenses/LICENSE-2.0
//     *
//     * Unless required by applicable law or agreed to in writing, software
//     * distributed under the License is distributed on an "AS IS" BASIS,
//     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     * See the License for the specific language governing permissions and
//     * limitations under the License.
//     */

//    [SuppressCodecs("Lucene3x")] // Suppress non-writable codecs
//    [TestFixture]
//    public class TestSerialization : LuceneTestCase
//    {
//        Lucene.Net.Store.RAMDirectory dir = null;

//        [SetUp]
//        public void Setup()
//        {
//            dir = new Lucene.Net.Store.RAMDirectory();
//            Index();
//        }

//        void Index()
//        {
//            var conf = new IndexWriterConfig(LuceneVersion.LUCENE_CURRENT, new Lucene.Net.Analysis.Core.WhitespaceAnalyzer(LuceneVersion.LUCENE_CURRENT));
//            Lucene.Net.Index.IndexWriter wr = new Lucene.Net.Index.IndexWriter(dir, conf/*new Lucene.Net.Analysis.Core.WhitespaceAnalyzer(LuceneVersion.LUCENE_CURRENT), Lucene.Net.Index.IndexWriter.MaxFieldLength.UNLIMITED*/);

//            Lucene.Net.Documents.Document doc = null;
//            Lucene.Net.Documents.Field f = null;

//            doc = new Lucene.Net.Documents.Document();
//            f = new Lucene.Net.Documents.Field("field", "a b c d", Lucene.Net.Documents.Field.Store.NO, Lucene.Net.Documents.Field.Index.ANALYZED);
//            doc.Add(f);
//            wr.AddDocument(doc);

//            doc = new Lucene.Net.Documents.Document();
//            f = new Lucene.Net.Documents.Field("field", "a b a d", Lucene.Net.Documents.Field.Store.NO, Lucene.Net.Documents.Field.Index.ANALYZED);
//            doc.Add(f);
//            wr.AddDocument(doc);

//            doc = new Lucene.Net.Documents.Document();
//            f = new Lucene.Net.Documents.Field("field", "a b e f", Lucene.Net.Documents.Field.Store.NO, Lucene.Net.Documents.Field.Index.ANALYZED);
//            doc.Add(f);
//            wr.AddDocument(doc);

//            doc = new Lucene.Net.Documents.Document();
//            f = new Lucene.Net.Documents.Field("field", "x y z", Lucene.Net.Documents.Field.Store.NO, Lucene.Net.Documents.Field.Index.ANALYZED);
//            doc.Add(f);
//            wr.AddDocument(doc);

//            wr.Dispose();
//        }


//        [Test, LuceneNetSpecific]
//        [Description("LUCENENET-338  (also see LUCENENET-170)")]
//        public void TestBooleanQuerySerialization()
//        {
//            Lucene.Net.Search.BooleanQuery lucQuery = new Lucene.Net.Search.BooleanQuery();

//            lucQuery.Add(new Lucene.Net.Search.TermQuery(new Lucene.Net.Index.Term("field", "x")), Occur.MUST);

//            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
//            System.IO.MemoryStream ms = new System.IO.MemoryStream();
//#pragma warning disable SYSLIB0011 // Type or member is obsolete (BinaryFormatter)
//            bf.Serialize(ms, lucQuery);
//            ms.Seek(0, System.IO.SeekOrigin.Begin);
//            Lucene.Net.Search.BooleanQuery lucQuery2 = (Lucene.Net.Search.BooleanQuery)bf.Deserialize(ms);
//#pragma warning restore SYSLIB0011 // Type or member is obsolete (BinaryFormatter)
//            ms.Close();

//            Assert.AreEqual(lucQuery, lucQuery2, "Error in serialization");

//            using var reader = DirectoryReader.Open(dir);
//            //Lucene.Net.Search.IndexSearcher searcher = new Lucene.Net.Search.IndexSearcher(dir, true);
//            Lucene.Net.Search.IndexSearcher searcher = new Lucene.Net.Search.IndexSearcher(reader);

//            int hitCount = searcher.Search(lucQuery, 20).TotalHits;

//            //searcher.Close();
//            searcher = new Lucene.Net.Search.IndexSearcher(reader);

//            int hitCount2 = searcher.Search(lucQuery2, 20).TotalHits;

//            Assert.AreEqual(hitCount, hitCount2, "Error in serialization - different hit counts");
//        }
//    }
//}
//#pragma warning restore 612, 618
//#endif