using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;
using System.IO;
using Assert = Lucene.Net.TestFramework.Assert;
using Version = Lucene.Net.Util.LuceneVersion;

#pragma warning disable 612, 618
namespace Lucene.Net.Support
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

    [SuppressCodecs("Lucene3x")] // Suppress non-writable codecs
    [TestFixture]
    public class TestOldPatches : LuceneTestCase
    {
        ////-------------------------------------------
        //[Test]
        //[Description("LUCENENET-183")]
        //public void Test_SegmentTermVector_IndexOf()
        //{
        //    Lucene.Net.Store.RAMDirectory directory = new Lucene.Net.Store.RAMDirectory();
        //    Lucene.Net.Analysis.Analyzer analyzer = new Lucene.Net.Analysis.Core.WhitespaceAnalyzer(Version.LUCENE_CURRENT);
        //    var conf = new IndexWriterConfig(Version.LUCENE_CURRENT, analyzer);
        //    Lucene.Net.Index.IndexWriter writer = new Lucene.Net.Index.IndexWriter(directory, conf /*analyzer, Lucene.Net.Index.IndexWriter.MaxFieldLength.LIMITED*/);
        //    Lucene.Net.Documents.Document document = new Lucene.Net.Documents.Document();
        //    document.Add(new Lucene.Net.Documents.Field("contents", new System.IO.StreamReader(new System.IO.MemoryStream(System.Text.Encoding.ASCII.GetBytes("a_ a0"))), Lucene.Net.Documents.Field.TermVector.WITH_OFFSETS));
        //    writer.AddDocument(document);
        //    Lucene.Net.Index.IndexReader reader = writer.GetReader();
        //    Lucene.Net.Index.TermPositionVector tpv = reader.GetTermFreqVector(0, "contents") as Lucene.Net.Index.TermPositionVector;
        //    //Console.WriteLine("tpv: " + tpv);
        //    int index = tpv.IndexOf("a_", StringComparison.Ordinal);
        //    Assert.AreEqual(index, 1, "See the issue: LUCENENET-183");
        //}

        //-------------------------------------------
        // LUCENENENET: Microsoft no longer considers it good practice to use binary serialization
        // in new applications. Therefore, we are no longer marking queries serializable
        // (It isn't serializable in Lucene 4.8.0 anymore anyway).
        // See: https://github.com/dotnet/corefx/issues/23584
        //#if FEATURE_SERIALIZABLE
        //        [Test]
        //        [Description("LUCENENET-170")]
        //        public void Test_Util_Parameter()
        //        {
        //            Lucene.Net.Search.BooleanQuery queryPreSerialized = new Lucene.Net.Search.BooleanQuery();
        //            queryPreSerialized.Add(new Lucene.Net.Search.TermQuery(new Lucene.Net.Index.Term("country", "Russia")), Occur.MUST);
        //            queryPreSerialized.Add(new Lucene.Net.Search.TermQuery(new Lucene.Net.Index.Term("country", "France")), Occur.MUST);

        //            //now serialize it 
        //            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter serializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
        //            System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
        //#pragma warning disable SYSLIB0011 // Type or member is obsolete (BinaryFormatter)
        //            serializer.Serialize(memoryStream, queryPreSerialized);

        //            //now deserialize 
        //            memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
        //            Lucene.Net.Search.BooleanQuery queryPostSerialized = (Lucene.Net.Search.BooleanQuery)serializer.Deserialize(memoryStream);
        //#pragma warning restore SYSLIB0011 // Type or member is obsolete (BinaryFormatter)
        //            memoryStream.Close();

        //            Assert.AreEqual(queryPreSerialized, queryPostSerialized, "See the issue: LUCENENET-170");
        //        }
        //#endif

        // LUCENENENET: Microsoft no longer considers it good practice to use binary serialization
        // in new applications. Therefore, we are no longer marking RAMDirectory serializable
        // (It isn't serializable in Lucene 4.8.0 anymore anyway).
        // See: https://github.com/dotnet/corefx/issues/23584

        //        //-------------------------------------------
        //#if FEATURE_SERIALIZABLE
        //        [Test]
        //        [Description("LUCENENET-174")]
        //        public void Test_Store_RAMDirectory()
        //        {
        //            Lucene.Net.Store.RAMDirectory ramDIR = new Lucene.Net.Store.RAMDirectory();

        //            //Index 1 Doc
        //            Lucene.Net.Analysis.Analyzer analyzer = new Lucene.Net.Analysis.Core.WhitespaceAnalyzer(Version.LUCENE_CURRENT);
        //            var conf = new IndexWriterConfig(Version.LUCENE_CURRENT, analyzer);
        //            Lucene.Net.Index.IndexWriter wr = new Lucene.Net.Index.IndexWriter(ramDIR, conf /*new Lucene.Net.Analysis.WhitespaceAnalyzer(), true, IndexWriter.MaxFieldLength.UNLIMITED*/);
        //            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
        //            doc.Add(new Lucene.Net.Documents.Field("field1", "value1 value11", Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.ANALYZED));
        //            wr.AddDocument(doc);
        //            wr.Dispose();

        //            //now serialize it 
        //            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter serializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
        //            System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
        //            serializer.Serialize(memoryStream, ramDIR);

        //            //Close DIR
        //            ramDIR.Dispose();
        //            ramDIR = null;

        //            //now deserialize 
        //            memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
        //            Lucene.Net.Store.RAMDirectory ramDIR2 = (Lucene.Net.Store.RAMDirectory)serializer.Deserialize(memoryStream);

        //            //Add 1 more doc
        //            Lucene.Net.Analysis.Analyzer analyzer2 = new Lucene.Net.Analysis.Core.WhitespaceAnalyzer(Version.LUCENE_CURRENT);
        //            var conf2 = new IndexWriterConfig(Version.LUCENE_CURRENT, analyzer);
        //            wr = new Lucene.Net.Index.IndexWriter(ramDIR2, conf2 /*new Lucene.Net.Analysis.WhitespaceAnalyzer(), false, IndexWriter.MaxFieldLength.UNLIMITED*/);
        //            doc = new Lucene.Net.Documents.Document();
        //            doc.Add(new Lucene.Net.Documents.Field("field1", "value1 value11", Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.ANALYZED));
        //            wr.AddDocument(doc);
        //            wr.Dispose();

        //            Lucene.Net.Search.TopDocs topDocs;
        //            //Search
        //            using (var reader = DirectoryReader.Open(ramDIR2))
        //            {
        //                Lucene.Net.Search.IndexSearcher s = new Lucene.Net.Search.IndexSearcher(reader);
        //                Lucene.Net.QueryParsers.Classic.QueryParser qp = new Lucene.Net.QueryParsers.Classic.QueryParser(Version.LUCENE_CURRENT, "field1", new Lucene.Net.Analysis.Standard.StandardAnalyzer(Version.LUCENE_CURRENT));
        //                Lucene.Net.Search.Query q = qp.Parse("value1");
        //                topDocs = s.Search(q, 100);
        //            }

        //            Assert.AreEqual(topDocs.TotalHits, 2, "See the issue: LUCENENET-174");
        //        }
        //#endif


        //-------------------------------------------
        [Test]
        [Description("LUCENENET-150")]
        public void Test_Index_ReusableStringReader()
        {
            var conf = new IndexWriterConfig(Version.LUCENE_CURRENT, new TestAnalyzer());
            Lucene.Net.Index.IndexWriter wr = new Lucene.Net.Index.IndexWriter(new Lucene.Net.Store.RAMDirectory(), conf /*new TestAnalyzer(), true, IndexWriter.MaxFieldLength.UNLIMITED*/);

            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
            Lucene.Net.Documents.Field f1 = new Lucene.Net.Documents.Field("f1", TEST_STRING, Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.ANALYZED);
            doc.Add(f1);
            wr.AddDocument(doc);

            wr.Dispose();
        }

        private const string TEST_STRING = "First Line\nSecond Line";

        private class TestAnalyzer : Lucene.Net.Analysis.Analyzer
        {
            public TestAnalyzer()
                //: base(new TestReuseStrategy())
            { }

            // Lucene.Net 3.0.3:
            //public override Lucene.Net.Analysis.TokenStream TokenStream(string fieldName, System.IO.TextReader reader)
            //{
            //    return new TestTokenizer(reader);
            //}

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                return new TokenStreamComponents(new TestTokenizer(reader));
            }

            protected internal override TextReader InitReader(string fieldName, TextReader reader)
            {
                var r = new ReusableStringReader();
                r.SetValue(reader.ReadToEnd());
                return r;
            }
        }

        //class TestReuseStrategy : Lucene.Net.Analysis.ReuseStrategy
        //{
        //    public override TokenStreamComponents GetReusableComponents(Analyzer analyzer, string fieldName)
        //    {
        //        return null;
        //    }

        //    public override void SetReusableComponents(Analyzer analyzer, string fieldName, TokenStreamComponents components)
        //    {
        //        throw new NotImplementedException();
        //    }
        //}

        private class TestTokenizer : Lucene.Net.Analysis.Tokenizer
        {
            public TestTokenizer(System.IO.TextReader reader)
                : base(reader)
            {
                //Caution: "Reader" is actually of type "ReusableStringReader" and some 
                //methods (for ex. "ReadToEnd", "Peek",  "ReadLine") is not implemented. 

                Assert.AreEqual("ReusableStringReader", reader.GetType().Name);
                Assert.AreEqual("First Line", reader.ReadLine(), "\"ReadLine\" method is not implemented");
                Assert.AreEqual("Second Line", reader.ReadToEnd(), "\"ReadToEnd\" method is not implemented");
            }

            public override sealed bool IncrementToken()
            {
                return false;
            }
        }

        // There is no IsCurrent() on IndexReader in Lucene 4.8.0
        //[Test]
        //[Description("LUCENENET-374")]
        //public void Test_IndexReader_IsCurrent()
        //{
        //    RAMDirectory ramDir = new RAMDirectory();
        //    var conf = new IndexWriterConfig(Version.LUCENE_CURRENT, new Analysis.Core.KeywordAnalyzer());
        //    IndexWriter writer = new IndexWriter(ramDir, conf /*new KeywordAnalyzer(), true, new IndexWriter.MaxFieldLength(1000)*/);
        //    Field field = new Field("TEST", "mytest", Field.Store.YES, Field.Index.ANALYZED);
        //    Document doc = new Document();
        //    doc.Add(field);
        //    writer.AddDocument(doc);

        //    IndexReader reader = writer.GetReader();

        //    writer.DeleteDocuments(new Lucene.Net.Index.Term("TEST", "mytest"));

        //    Assert.IsFalse(reader.IsCurrent());

        //    int resCount1 = new IndexSearcher(reader).Search(new TermQuery(new Term("TEST", "mytest")),100).TotalHits; 
        //    Assert.AreEqual(1, resCount1);

        //    writer.Commit();

        //    Assert.IsFalse(reader.IsCurrent());

        //    int resCount2 = new IndexSearcher(reader).Search(new TermQuery(new Term("TEST", "mytest")),100).TotalHits;
        //    Assert.AreEqual(1, resCount2, "Reopen not invoked yet, resultCount must still be 1.");

        //    reader = reader.Reopen();
        //    Assert.IsTrue(reader.IsCurrent());

        //    int resCount3 = new IndexSearcher(reader).Search(new TermQuery(new Term("TEST", "mytest")), 100).TotalHits;
        //    Assert.AreEqual(0, resCount3, "After reopen, resultCount must be 0.");

        //    reader.Close();
        //    writer.Dispose();
        //}


        // LUCENENET TODO: Should IndexSearcher really implement MarshalByrefObj?
        ////-------------------------------------------
        //int ANYPORT = 0;
        //[Test] 
        //[Description("LUCENENET-100")]
        //public void Test_Search_FieldDoc()
        //{
        //    ANYPORT = new Random((int)(DateTime.Now.Ticks & 0x7fffffff)).Next(50000) + 10000;
        //    LUCENENET_100_CreateIndex();

        //    try
        //    {
        //        System.Runtime.Remoting.Channels.ChannelServices.RegisterChannel(new System.Runtime.Remoting.Channels.Tcp.TcpChannel(ANYPORT),false);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //    }

        //    var reader = DirectoryReader.Open(LUCENENET_100_Dir);
        //    Lucene.Net.Search.IndexSearcher indexSearcher = new Lucene.Net.Search.IndexSearcher(reader);
        //    System.Runtime.Remoting.RemotingServices.Marshal(indexSearcher, "Searcher");
         

        //    LUCENENET_100_ClientSearch();

        //    //Wait Client to finish
        //    while (LUCENENET_100_testFinished == false) System.Threading.Thread.Sleep(10);
                        
        //    if (LUCENENET_100_Exception != null) throw LUCENENET_100_Exception;
        //}

        //Lucene.Net.Store.RAMDirectory LUCENENET_100_Dir = new Lucene.Net.Store.RAMDirectory();
        //bool LUCENENET_100_testFinished = false;
        //Exception LUCENENET_100_Exception = null;


        //void LUCENENET_100_ClientSearch()
        //{
        //    try
        //    {
        //        Lucene.Net.Search.Searchable s = (Lucene.Net.Search.Searchable)Activator.GetObject(typeof(Lucene.Net.Search.Searchable), @"tcp://localhost:" + ANYPORT + "/Searcher");
        //        Lucene.Net.Search.MultiSearcher searcher = new Lucene.Net.Search.MultiSearcher(new Lucene.Net.Search.Searchable[] { s });

        //        Lucene.Net.Search.Query q = new Lucene.Net.Search.TermQuery(new Lucene.Net.Index.Term("field1", "moon"));

        //        Lucene.Net.Search.Sort sort = new Lucene.Net.Search.Sort();
        //        sort.SetSort(new Lucene.Net.Search.SortField("field2", Lucene.Net.Search.SortField.INT));

        //        Lucene.Net.Search.TopDocs h = searcher.Search(q, null, 100, sort);
        //        if (h.ScoreDocs.Length != 2) LUCENENET_100_Exception = new SupportClassException("Test_Search_FieldDoc Error. ");
        //    }
        //    catch (SupportClassException ex)
        //    {
        //        LUCENENET_100_Exception = ex;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex);
        //    }
        //    finally
        //    {
        //        LUCENENET_100_testFinished = true;
        //    }
        //}

        //void LUCENENET_100_CreateIndex()
        //{
        //    Lucene.Net.Index.IndexWriter w = new Lucene.Net.Index.IndexWriter(LUCENENET_100_Dir, new Lucene.Net.Analysis.Standard.StandardAnalyzer(Version.LUCENE_CURRENT), true, IndexWriter.MaxFieldLength.UNLIMITED);

        //    Lucene.Net.Documents.Field f1 = new Lucene.Net.Documents.Field("field1", "dark side of the moon", Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.ANALYZED);
        //    Lucene.Net.Documents.Field f2 = new Lucene.Net.Documents.Field("field2", "123", Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.NOT_ANALYZED);
        //    Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
        //    d.Add(f1);
        //    d.Add(f2);
        //    w.AddDocument(d);

        //    f1 = new Lucene.Net.Documents.Field("field1", "Fly me to the moon", Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.ANALYZED);
        //    f2 = new Lucene.Net.Documents.Field("field2", "456", Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.NOT_ANALYZED);
        //    d = new Lucene.Net.Documents.Document();
        //    d.Add(f1);
        //    d.Add(f2);
        //    w.AddDocument(d);

        //    w.Dispose();
        //}

        ////-------------------------------------------
    }
}
#pragma warning restore 612, 618
