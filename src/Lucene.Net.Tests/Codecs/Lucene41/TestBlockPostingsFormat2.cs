using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System;
using System.Text;

namespace Lucene.Net.Codecs.Lucene41
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
    using IIndexableField = Lucene.Net.Index.IIndexableField;
    using IndexOptions = Lucene.Net.Index.IndexOptions;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using OpenMode = Lucene.Net.Index.OpenMode;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;

    /// <summary>
    /// Tests special cases of BlockPostingsFormat
    /// </summary>
    [TestFixture]
    public class TestBlockPostingsFormat2 : LuceneTestCase
    {
        internal Directory dir;
        internal RandomIndexWriter iw;
        internal IndexWriterConfig iwc;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            dir = NewFSDirectory(CreateTempDir("testDFBlockSize"));
            iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetCodec(TestUtil.AlwaysPostingsFormat(new Lucene41PostingsFormat()));
            iw = new RandomIndexWriter(Random, dir, (IndexWriterConfig)iwc.Clone());
            iw.DoRandomForceMerge = false; // we will ourselves
        }

        [TearDown]
        public override void TearDown()
        {
            this.iw.Dispose();
            TestUtil.CheckIndex(dir); // for some extra coverage, checkIndex before we forceMerge
            iwc.SetOpenMode(OpenMode.APPEND);
            IndexWriter iw = new IndexWriter(dir, (IndexWriterConfig)iwc.Clone());
            iw.ForceMerge(1);
            iw.Dispose();
            dir.Dispose(); // just force a checkindex for now
            base.TearDown();
        }

        private Document NewDocument()
        {
            Document doc = new Document();
            foreach (IndexOptions option in Enum.GetValues(typeof(IndexOptions)))
            {
                // LUCENENET: skip the "NONE" option that we added
                if (option == IndexOptions.NONE)
                {
                    continue;
                }

                var ft = new FieldType(TextField.TYPE_NOT_STORED)
                {
                    StoreTermVectors = true,
                    StoreTermVectorOffsets = true,
                    StoreTermVectorPositions = true,
                    StoreTermVectorPayloads = true,
                    IndexOptions = option
                };
                // turn on tvs for a cross-check, since we rely upon checkindex in this test (for now)
                doc.Add(new Field(option.ToString(), "", ft));
            }
            return doc;
        }

        /// <summary>
        /// tests terms with df = blocksize </summary>
        [Test]
        public virtual void TestDFBlockSize()
        {
            Document doc = NewDocument();
            for (int i = 0; i < Lucene41PostingsFormat.BLOCK_SIZE; i++)
            {
                foreach (IIndexableField f in doc.Fields)
                {
                    ((Field)f).SetStringValue(f.Name + " " + f.Name + "_2");
                }
                iw.AddDocument(doc);
            }
        }

        /// <summary>
        /// tests terms with df % blocksize = 0 </summary>
        [Test]
        public virtual void TestDFBlockSizeMultiple()
        {
            Document doc = NewDocument();
            for (int i = 0; i < Lucene41PostingsFormat.BLOCK_SIZE * 16; i++)
            {
                foreach (IIndexableField f in doc.Fields)
                {
                    ((Field)f).SetStringValue(f.Name + " " + f.Name + "_2");
                }
                iw.AddDocument(doc);
            }
        }

        /// <summary>
        /// tests terms with ttf = blocksize </summary>
        [Test]
        public virtual void TestTTFBlockSize()
        {
            Document doc = NewDocument();
            for (int i = 0; i < Lucene41PostingsFormat.BLOCK_SIZE / 2; i++)
            {
                foreach (IIndexableField f in doc.Fields)
                {
                    ((Field)f).SetStringValue(f.Name + " " + f.Name + " " + f.Name + "_2 " + f.Name + "_2");
                }
                iw.AddDocument(doc);
            }
        }

        /// <summary>
        /// tests terms with ttf % blocksize = 0 </summary>
        [Test]
        public virtual void TestTTFBlockSizeMultiple()
        {
            Document doc = NewDocument();
            for (int i = 0; i < Lucene41PostingsFormat.BLOCK_SIZE / 2; i++)
            {
                foreach (IIndexableField f in doc.Fields)
                {
                    string proto = (f.Name + " " + f.Name + " " + f.Name + " " + f.Name + " " + f.Name + "_2 " + f.Name + "_2 " + f.Name + "_2 " + f.Name + "_2");
                    StringBuilder val = new StringBuilder();
                    for (int j = 0; j < 16; j++)
                    {
                        val.Append(proto);
                        val.Append(' ');
                    }
                    ((Field)f).SetStringValue(val.ToString());
                }
                iw.AddDocument(doc);
            }
        }
    }
}