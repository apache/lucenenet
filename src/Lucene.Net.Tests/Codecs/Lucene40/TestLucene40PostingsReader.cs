using J2N.Collections.Generic.Extensions;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Codecs.Lucene40
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
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using StringField = StringField;
    using Term = Lucene.Net.Index.Term;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;

    [TestFixture]
    public class TestLucene40PostingsReader : LuceneTestCase
    {
        internal static readonly string[] terms = LoadTerms();

        static string[] LoadTerms()
        {
            string[] terms = new string[100];
            for (int i = 0; i < terms.Length; i++)
            {
                terms[i] = Convert.ToString(i + 1);
            }
            return terms;
        }

        [OneTimeSetUp]
        public override void BeforeClass()
        {
            base.BeforeClass();
            OldFormatImpersonationIsActive = true; // explicitly instantiates ancient codec
        }

        /// <summary>
        /// tests terms with different probabilities of being in the document.
        ///  depends heavily on term vectors cross-check at checkIndex
        /// </summary>
        [Test]
        public virtual void TestPostings()
        {
            Directory dir = NewFSDirectory(CreateTempDir("postings"));
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetCodec(Codec.ForName("Lucene40"));
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

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
                idField.SetStringValue(Convert.ToString(i));
                titleField.SetStringValue(FieldValue(1));
                bodyField.SetStringValue(FieldValue(3));
                iw.AddDocument(doc);
                if (Random.Next(20) == 0)
                {
                    iw.DeleteDocuments(new Term("id", Convert.ToString(i)));
                }
            }
            if (Random.NextBoolean())
            {
                // delete 1-100% of docs
                iw.DeleteDocuments(new Term("title", terms[Random.Next(terms.Length)]));
            }
            iw.Dispose();
            dir.Dispose(); // checkindex
        }

        internal virtual IndexOptions IndexOptions()
        {
            switch (Random.Next(4))
            {
                case 0:
                    return Index.IndexOptions.DOCS_ONLY;

                case 1:
                    return Index.IndexOptions.DOCS_AND_FREQS;

                case 2:
                    return Index.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;

                default:
                    return Index.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
            }
        }

        internal virtual string FieldValue(int maxTF)
        {
            IList<string> shuffled = new JCG.List<string>();
            StringBuilder sb = new StringBuilder();
            int i = Random.Next(terms.Length);
            while (i < terms.Length)
            {
                int tf = TestUtil.NextInt32(Random, 1, maxTF);
                for (int j = 0; j < tf; j++)
                {
                    shuffled.Add(terms[i]);
                }
                i++;
            }
            shuffled.Shuffle(Random);
            foreach (string term in shuffled)
            {
                sb.Append(term);
                sb.Append(' ');
            }
            return sb.ToString();
        }
    }
}