/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Linq;
using System.Reflection;
using Lucene.Net.Search;
using NUnit.Framework;

namespace Lucene.Net.Support
{
    [TestFixture]
    public class TestSerialization
    {
        Lucene.Net.Store.RAMDirectory dir = null;

        [SetUp]
        public void Setup()
        {
            dir = new Lucene.Net.Store.RAMDirectory();
            Index();
        }

        void Index()
        {
            Lucene.Net.Index.IndexWriter wr = new Lucene.Net.Index.IndexWriter(dir, new Lucene.Net.Analysis.WhitespaceAnalyzer(), Lucene.Net.Index.IndexWriter.MaxFieldLength.UNLIMITED);

            Lucene.Net.Documents.Document doc = null;
            Lucene.Net.Documents.Field f = null;

            doc = new Lucene.Net.Documents.Document();
            f = new Lucene.Net.Documents.Field("field", "a b c d", Lucene.Net.Documents.Field.Store.NO, Lucene.Net.Documents.Field.Index.ANALYZED);
            doc.Add(f);
            wr.AddDocument(doc);

            doc = new Lucene.Net.Documents.Document();
            f = new Lucene.Net.Documents.Field("field", "a b a d", Lucene.Net.Documents.Field.Store.NO, Lucene.Net.Documents.Field.Index.ANALYZED);
            doc.Add(f);
            wr.AddDocument(doc);

            doc = new Lucene.Net.Documents.Document();
            f = new Lucene.Net.Documents.Field("field", "a b e f", Lucene.Net.Documents.Field.Store.NO, Lucene.Net.Documents.Field.Index.ANALYZED);
            doc.Add(f);
            wr.AddDocument(doc);
            
            doc = new Lucene.Net.Documents.Document();
            f = new Lucene.Net.Documents.Field("field", "x y z", Lucene.Net.Documents.Field.Store.NO, Lucene.Net.Documents.Field.Index.ANALYZED);
            doc.Add(f);
            wr.AddDocument(doc);
            
            wr.Close();
        }


        [Test]
        [Description("LUCENENET-338  (also see LUCENENET-170)")]
        public void TestBooleanQuerySerialization()
        {
            Lucene.Net.Search.BooleanQuery lucQuery = new Lucene.Net.Search.BooleanQuery();

            lucQuery.Add(new Lucene.Net.Search.TermQuery(new Lucene.Net.Index.Term("field", "x")), Occur.MUST);
            
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            bf.Serialize(ms, lucQuery);
            ms.Seek(0, System.IO.SeekOrigin.Begin);
            Lucene.Net.Search.BooleanQuery lucQuery2 = (Lucene.Net.Search.BooleanQuery)bf.Deserialize(ms);
            ms.Close();

            Assert.AreEqual(lucQuery, lucQuery2, "Error in serialization");

            Lucene.Net.Search.IndexSearcher searcher = new Lucene.Net.Search.IndexSearcher(dir, true);

            int hitCount = searcher.Search(lucQuery, 20).TotalHits;
            
            searcher.Close();
            searcher = new Lucene.Net.Search.IndexSearcher(dir, true);
            
            int hitCount2 = searcher.Search(lucQuery2, 20).TotalHits;

            Assert.AreEqual(hitCount, hitCount2, "Error in serialization - different hit counts");
        }
    }
}
