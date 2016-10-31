using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Codecs.Lucene40
{
    using Lucene.Net.Randomized.Generators;
    using NUnit.Framework;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

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

    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using StringField = StringField;
    using Term = Lucene.Net.Index.Term;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;

    [TestFixture]
    public class TestLucene40PostingsReader : LuceneTestCase
    {
        internal static readonly string[] Terms = new string[100];

        static TestLucene40PostingsReader()
        {
            for (int i = 0; i < Terms.Length; i++)
            {
                Terms[i] = Convert.ToString(i + 1);
            }
        }

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because OLD_FORMAT_IMPERSONATION_IS_ACTIVE is no longer static.
        /// </summary>
        [OneTimeSetUp]
        public void BeforeClass()
        {
            OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true; // explicitly instantiates ancient codec
        }

        /// <summary>
        /// tests terms with different probabilities of being in the document.
        ///  depends heavily on term vectors cross-check at checkIndex
        /// </summary>
        [Test]
        public virtual void TestPostings()
        {
            Directory dir = NewFSDirectory(CreateTempDir("postings"));
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            iwc.SetCodec(Codec.ForName("Lucene40"));
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwc);

            Document doc = new Document();

            // id field
            FieldType idType = new FieldType(StringField.TYPE_NOT_STORED);
            idType.StoreTermVectors = true;
            Field idField = new Field("id", "", idType);
            doc.Add(idField);

            // title field: short text field
            FieldType titleType = new FieldType(TextField.TYPE_NOT_STORED);
            titleType.StoreTermVectors = true;
            titleType.StoreTermVectorPositions = true;
            titleType.StoreTermVectorOffsets = true;
            titleType.IndexOptions = IndexOptions();
            Field titleField = new Field("title", "", titleType);
            doc.Add(titleField);

            // body field: long text field
            FieldType bodyType = new FieldType(TextField.TYPE_NOT_STORED);
            bodyType.StoreTermVectors = true;
            bodyType.StoreTermVectorPositions = true;
            bodyType.StoreTermVectorOffsets = true;
            bodyType.IndexOptions = IndexOptions();
            Field bodyField = new Field("body", "", bodyType);
            doc.Add(bodyField);

            int numDocs = AtLeast(1000);
            for (int i = 0; i < numDocs; i++)
            {
                idField.StringValue = Convert.ToString(i);
                titleField.StringValue = FieldValue(1);
                bodyField.StringValue = FieldValue(3);
                iw.AddDocument(doc);
                if (Random().Next(20) == 0)
                {
                    iw.DeleteDocuments(new Term("id", Convert.ToString(i)));
                }
            }
            if (Random().NextBoolean())
            {
                // delete 1-100% of docs
                iw.DeleteDocuments(new Term("title", Terms[Random().Next(Terms.Length)]));
            }
            iw.Dispose();
            dir.Dispose(); // checkindex
        }

        internal virtual FieldInfo.IndexOptions IndexOptions()
        {
            switch (Random().Next(4))
            {
                case 0:
                    return FieldInfo.IndexOptions.DOCS_ONLY;

                case 1:
                    return FieldInfo.IndexOptions.DOCS_AND_FREQS;

                case 2:
                    return FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;

                default:
                    return FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
            }
        }

        internal virtual string FieldValue(int maxTF)
        {
            IList<string> shuffled = new List<string>();
            StringBuilder sb = new StringBuilder();
            int i = Random().Next(Terms.Length);
            while (i < Terms.Length)
            {
                int tf = TestUtil.NextInt(Random(), 1, maxTF);
                for (int j = 0; j < tf; j++)
                {
                    shuffled.Add(Terms[i]);
                }
                i++;
            }
            shuffled = CollectionsHelper.Shuffle(shuffled);
            foreach (string term in shuffled)
            {
                sb.Append(term);
                sb.Append(' ');
            }
            return sb.ToString();
        }
    }
}