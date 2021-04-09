using Lucene.Net.Support;
using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Index
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

    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

    [TestFixture]
    public class Test2BDocs : LuceneTestCase
    {
        internal static Directory dir;

        [OneTimeSetUp]
        public override void BeforeClass()
        {
            base.BeforeClass();

            dir = NewFSDirectory(CreateTempDir("2Bdocs"));
            IndexWriter iw = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, null));
            Document doc = new Document();
            for (int i = 0; i < 262144; i++)
            {
                iw.AddDocument(doc);
            }
            iw.ForceMerge(1);
            iw.Dispose();
        }

        [OneTimeTearDown]
        public override void AfterClass()
        {
            dir.Dispose();
            dir = null;
            base.AfterClass();
        }

        [Test]
        public virtual void TestOverflow()
        {
            DirectoryReader ir = DirectoryReader.Open(dir);
            IndexReader[] subReaders = new IndexReader[8192];
            Arrays.Fill(subReaders, ir);
            try
            {
                new MultiReader(subReaders);
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                // expected
            }
            ir.Dispose();
        }

        [Test]
        public virtual void TestExactlyAtLimit()
        {
            Directory dir2 = NewFSDirectory(CreateTempDir("2BDocs2"));
            IndexWriter iw = new IndexWriter(dir2, new IndexWriterConfig(TEST_VERSION_CURRENT, null));
            Document doc = new Document();
            for (int i = 0; i < 262143; i++)
            {
                iw.AddDocument(doc);
            }
            iw.Dispose();
            DirectoryReader ir = DirectoryReader.Open(dir);
            DirectoryReader ir2 = DirectoryReader.Open(dir2);
            IndexReader[] subReaders = new IndexReader[8192];
            Arrays.Fill(subReaders, ir);
            subReaders[subReaders.Length - 1] = ir2;
            MultiReader mr = new MultiReader(subReaders);
            Assert.AreEqual(int.MaxValue, mr.MaxDoc);
            Assert.AreEqual(int.MaxValue, mr.NumDocs);
            ir.Dispose();
            ir2.Dispose();
            dir2.Dispose();
        }
    }
}