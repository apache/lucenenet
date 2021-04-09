using Lucene.Net.Documents;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Globalization;
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
    using Field = Field;
    using FieldType = FieldType;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// Tests that a useful exception is thrown when attempting to index a term that is
    /// too large
    /// </summary>
    /// <seealso cref= IndexWriter#MAX_TERM_LENGTH </seealso>
    [TestFixture]
    public class TestExceedMaxTermLength : LuceneTestCase
    {
        private static readonly int minTestTermLength = IndexWriter.MAX_TERM_LENGTH + 1;
        private static readonly int maxTestTermLegnth = IndexWriter.MAX_TERM_LENGTH * 2;

        internal Directory dir = null;

        [SetUp]
        public virtual void CreateDir()
        {
            dir = NewDirectory();
        }

        [TearDown]
        public virtual void DestroyDir()
        {
            dir.Dispose();
            dir = null;
        }

        [Test]
        public virtual void Test()
        {
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(Random, TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            try
            {
                FieldType ft = new FieldType();
                ft.IsIndexed = true;
                ft.IsStored = Random.NextBoolean();
                ft.Freeze();

                Document doc = new Document();
                if (Random.NextBoolean())
                {
                    // totally ok short field value
                    doc.Add(new Field(TestUtil.RandomSimpleString(Random, 1, 10), TestUtil.RandomSimpleString(Random, 1, 10), ft));
                }
                // problematic field
                string name = TestUtil.RandomSimpleString(Random, 1, 50);
                string value = TestUtil.RandomSimpleString(Random, minTestTermLength, maxTestTermLegnth);
                Field f = new Field(name, value, ft);
                if (Random.NextBoolean())
                {
                    // totally ok short field value
                    doc.Add(new Field(TestUtil.RandomSimpleString(Random, 1, 10), TestUtil.RandomSimpleString(Random, 1, 10), ft));
                }
                doc.Add(f);

                try
                {
                    w.AddDocument(doc);
                    Assert.Fail("Did not get an exception from adding a monster term");
                }
                catch (Exception e) when (e.IsIllegalArgumentException())
                {
                    string maxLengthMsg = Convert.ToString(IndexWriter.MAX_TERM_LENGTH, CultureInfo.InvariantCulture);
                    string msg = e.Message;
                    Assert.IsTrue(msg.Contains("immense term"), "IllegalArgumentException didn't mention 'immense term': " + msg);
                    Assert.IsTrue(msg.Contains(maxLengthMsg), "IllegalArgumentException didn't mention max length (" + maxLengthMsg + "): " + msg);
                    Assert.IsTrue(msg.Contains(name), "IllegalArgumentException didn't mention field name (" + name + "): " + msg);
                }
            }
            finally
            {
                w.Dispose();
            }
        }
    }
}