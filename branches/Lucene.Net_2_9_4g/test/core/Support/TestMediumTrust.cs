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

using System;
using Lucene.Net.Search;
using Lucene.Net.Store;
using NUnit.Framework;

namespace Lucene.Net.Support
{
    [TestFixture]
    public class TestMediumTrust 
    {
        dynamic _PartiallyTrustedClass;

        public TestMediumTrust()
        {
            _PartiallyTrustedClass = new Lucene.Net.Test.PartiallyTrustedAppDomain<TestMethodsContainer>(); 
        }
        
        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            _PartiallyTrustedClass.Dispose();
        }


        [Test]
        public void TestIndexAndSearch()
        {
            string tempIndexDir = System.IO.Path.Combine(_PartiallyTrustedClass.TempDir, "testindex");
            try
            {
                _PartiallyTrustedClass.TestIndexAndSearch(tempIndexDir);
            }
            finally
            {
                if(System.IO.Directory.Exists(tempIndexDir))
                    System.IO.Directory.Delete(tempIndexDir,true);
            }
        }

        [Test]
        public void Test_Index_Term()
        {
            _PartiallyTrustedClass.Test_Index_Term();
        }

        [Test]
        public void Test_Search_NumericRangeQuery()
        {
            _PartiallyTrustedClass.Test_Search_NumericRangeQuery();
        }
        
        [Test]
        public void Test_Search_SortField()
        {
            _PartiallyTrustedClass.Test_Search_SortField();
        }

        [Test]
        public void Test_AlreadyClosedException()
        {
            _PartiallyTrustedClass.Test_AlreadyClosedException();
        }

        [Test]
        public void Test_AlreadyClosedException_Serialization()
        {
            try
            {
                _PartiallyTrustedClass.Test_AlreadyClosedException_Serialization();
            }
            catch (System.Security.SecurityException)
            {
                Assert.Ignore("This method failed with a security exception");
            }
        }

        [Test]
        public void Test_Util_Parameter()
        {
            _PartiallyTrustedClass.Test_Util_Parameter();
        }
        
        [Test]
        public void TestThisTest()
        {
            try
            {
                _PartiallyTrustedClass.MethodToFail();
                Assert.Fail("This call must fail-2");
            }
            catch (System.Security.SecurityException)
            {
            }
        }


        public class TestMethodsContainer : MarshalByRefObject
        {
            void TestIndexAndSearch(string tempDir)
            {
                Lucene.Net.Store.Directory dir = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(tempDir));

                Lucene.Net.Index.IndexWriter w = new Lucene.Net.Index.IndexWriter(dir, new Lucene.Net.Analysis.Standard.StandardAnalyzer(), true);
                Lucene.Net.Documents.Field f1 = new Lucene.Net.Documents.Field("field1", "dark side of the moon", Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.TOKENIZED);
                Lucene.Net.Documents.Field f2 = new Lucene.Net.Documents.Field("field2", "123", Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.UN_TOKENIZED);
                Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
                d.Add(f1);
                d.Add(f2);
                w.AddDocument(d);

                f1 = new Lucene.Net.Documents.Field("field1", "Fly me to the moon", Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.TOKENIZED);
                f2 = new Lucene.Net.Documents.Field("field2", "456", Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.UN_TOKENIZED);
                d = new Lucene.Net.Documents.Document();
                d.Add(f1);
                d.Add(f2);
                w.AddDocument(d);

                w.Close();

                Lucene.Net.Search.IndexSearcher searcher = new IndexSearcher(dir, true);

                Lucene.Net.Search.Sort sort = new Lucene.Net.Search.Sort();
                sort.SetSort(new Lucene.Net.Search.SortField("field2", Lucene.Net.Search.SortField.STRING));
                Lucene.Net.Search.Query q = new Lucene.Net.QueryParsers.QueryParser("field1", new Lucene.Net.Analysis.Standard.StandardAnalyzer()).Parse("moon");
                TopDocs td = searcher.Search(q, null, 100, sort);
                int resCount = td.ScoreDocs.Length;

                searcher.Close();
            }

            void Test_Index_Term()
            {
                string s = new Lucene.Net.Index.Term("field", "Text").ToString();
            }

            void Test_Search_NumericRangeQuery()
            {
                Lucene.Net.Search.Query q = NumericRangeQuery.NewIntRange("field", 0, 10, true, true);
            }

            void Test_Search_SortField()
            {
                bool b = new Lucene.Net.Search.SortField("field").GetUseLegacySearch();
            }

            void Test_AlreadyClosedException()
            {
                try
                {
                    throw new AlreadyClosedException("test");
                }
                catch (AlreadyClosedException)
                {
                }
            }

            void Test_AlreadyClosedException_Serialization()
            {
                AlreadyClosedException ace = new AlreadyClosedException("Test");
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                bf.Serialize(ms, ace);
                ms.Seek(0, System.IO.SeekOrigin.Begin);
                AlreadyClosedException ace2 = (AlreadyClosedException)bf.Deserialize(ms);
            }

            void Test_Util_Parameter()
            {
                string s = new PARAM("field").ToString();
            }

            [Serializable]
            public class PARAM : Lucene.Net.Util.Parameter
            {
                public PARAM(string field) : base(field)
                {
                }
            }
            
            string MethodToFail()
            {
                return System.Environment.GetEnvironmentVariable("TEMP");
            }
        }
    }
}