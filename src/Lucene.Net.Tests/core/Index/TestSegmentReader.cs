using System;
using System.Collections.Generic;

namespace Lucene.Net.Index
{
    using NUnit.Framework;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;

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

    using Document = Documents.Document;
    using IOContext = Lucene.Net.Store.IOContext;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestSegmentReader : LuceneTestCase
    {
        private Directory Dir;
        private Document TestDoc;
        private SegmentReader Reader;

        //TODO: Setup the reader w/ multiple documents
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            Dir = NewDirectory();
            TestDoc = new Document();
            DocHelper.SetupDoc(TestDoc);
            SegmentCommitInfo info = DocHelper.WriteDoc(Random(), Dir, TestDoc);
            Reader = new SegmentReader(info, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, IOContext.READ);
        }

        [TearDown]
        public override void TearDown()
        {
            Reader.Dispose();
            Dir.Dispose();
            base.TearDown();
        }

        [Test]
        public virtual void Test()
        {
            Assert.IsTrue(Dir != null);
            Assert.IsTrue(Reader != null);
            Assert.IsTrue(DocHelper.NameValues.Count > 0);
            Assert.IsTrue(DocHelper.NumFields(TestDoc) == DocHelper.All.Count);
        }

        [Test]
        public virtual void TestDocument()
        {
            Assert.IsTrue(Reader.NumDocs == 1);
            Assert.IsTrue(Reader.MaxDoc >= 1);
            Document result = Reader.Document(0);
            Assert.IsTrue(result != null);
            //There are 2 unstored fields on the document that are not preserved across writing
            Assert.IsTrue(DocHelper.NumFields(result) == DocHelper.NumFields(TestDoc) - DocHelper.Unstored.Count);

            IList<IIndexableField> fields = result.Fields;
            foreach (IIndexableField field in fields)
            {
                Assert.IsTrue(field != null);
                Assert.IsTrue(DocHelper.NameValues.ContainsKey(field.Name));
            }
        }

        [Test]
        public virtual void TestGetFieldNameVariations()
        {
            ICollection<string> allFieldNames = new HashSet<string>();
            ICollection<string> indexedFieldNames = new HashSet<string>();
            ICollection<string> notIndexedFieldNames = new HashSet<string>();
            ICollection<string> tvFieldNames = new HashSet<string>();
            ICollection<string> noTVFieldNames = new HashSet<string>();

            foreach (FieldInfo fieldInfo in Reader.FieldInfos)
            {
                string name = fieldInfo.Name;
                allFieldNames.Add(name);
                if (fieldInfo.IsIndexed)
                {
                    indexedFieldNames.Add(name);
                }
                else
                {
                    notIndexedFieldNames.Add(name);
                }
                if (fieldInfo.HasVectors)
                {
                    tvFieldNames.Add(name);
                }
                else if (fieldInfo.IsIndexed)
                {
                    noTVFieldNames.Add(name);
                }
            }

            Assert.IsTrue(allFieldNames.Count == DocHelper.All.Count);
            foreach (string s in allFieldNames)
            {
                Assert.IsTrue(DocHelper.NameValues.ContainsKey(s) == true || s.Equals(""));
            }

            Assert.IsTrue(indexedFieldNames.Count == DocHelper.Indexed.Count);
            foreach (string s in indexedFieldNames)
            {
                Assert.IsTrue(DocHelper.Indexed.ContainsKey(s) == true || s.Equals(""));
            }

            Assert.IsTrue(notIndexedFieldNames.Count == DocHelper.Unindexed.Count);
            //Get all indexed fields that are storing term vectors
            Assert.IsTrue(tvFieldNames.Count == DocHelper.Termvector.Count);

            Assert.IsTrue(noTVFieldNames.Count == DocHelper.Notermvector.Count);
        }

        [Test]
        public virtual void TestTerms()
        {
            Fields fields = MultiFields.GetFields(Reader);
            foreach (string field in fields)
            {
                Terms terms = fields.Terms(field);
                Assert.IsNotNull(terms);
                TermsEnum termsEnum = terms.Iterator(null);
                while (termsEnum.Next() != null)
                {
                    BytesRef term = termsEnum.Term;
                    Assert.IsTrue(term != null);
                    string fieldValue = (string)DocHelper.NameValues[field];
                    Assert.IsTrue(fieldValue.IndexOf(term.Utf8ToString()) != -1);
                }
            }

            DocsEnum termDocs = TestUtil.Docs(Random(), Reader, DocHelper.TEXT_FIELD_1_KEY, new BytesRef("field"), MultiFields.GetLiveDocs(Reader), null, 0);
            Assert.IsTrue(termDocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);

            termDocs = TestUtil.Docs(Random(), Reader, DocHelper.NO_NORMS_KEY, new BytesRef(DocHelper.NO_NORMS_TEXT), MultiFields.GetLiveDocs(Reader), null, 0);

            Assert.IsTrue(termDocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);

            DocsAndPositionsEnum positions = MultiFields.GetTermPositionsEnum(Reader, MultiFields.GetLiveDocs(Reader), DocHelper.TEXT_FIELD_1_KEY, new BytesRef("field"));
            // NOTE: prior rev of this test was failing to first
            // call next here:
            Assert.IsTrue(positions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.IsTrue(positions.DocID == 0);
            Assert.IsTrue(positions.NextPosition() >= 0);
        }

        [Test]
        public virtual void TestNorms()
        {
            //TODO: Not sure how these work/should be tested
            /*
                try {
                  byte [] norms = reader.norms(DocHelper.TEXT_FIELD_1_KEY);
                  System.out.println("Norms: " + norms);
                  Assert.IsTrue(norms != null);
                } catch (IOException e) {
                  e.printStackTrace();
                  Assert.IsTrue(false);
                }
            */

            CheckNorms(Reader);
        }

        public static void CheckNorms(AtomicReader reader)
        {
            // test omit norms
            for (int i = 0; i < DocHelper.Fields.Length; i++)
            {
                IIndexableField f = DocHelper.Fields[i];
                if (f.FieldType.IsIndexed)
                {
                    Assert.AreEqual(reader.GetNormValues(f.Name) != null, !f.FieldType.OmitNorms);
                    Assert.AreEqual(reader.GetNormValues(f.Name) != null, !DocHelper.NoNorms.ContainsKey(f.Name));
                    if (reader.GetNormValues(f.Name) == null)
                    {
                        // test for norms of null
                        NumericDocValues norms = MultiDocValues.GetNormValues(reader, f.Name);
                        Assert.IsNull(norms);
                    }
                }
            }
        }

        [Test]
        public virtual void TestTermVectors()
        {
            Terms result = Reader.GetTermVectors(0).Terms(DocHelper.TEXT_FIELD_2_KEY);
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count);
            TermsEnum termsEnum = result.Iterator(null);
            while (termsEnum.Next() != null)
            {
                string term = termsEnum.Term.Utf8ToString();
                int freq = (int)termsEnum.TotalTermFreq;
                Assert.IsTrue(DocHelper.FIELD_2_TEXT.IndexOf(term) != -1);
                Assert.IsTrue(freq > 0);
            }

            Fields results = Reader.GetTermVectors(0);
            Assert.IsTrue(results != null);
            Assert.AreEqual(3, results.Count, "We do not have 3 term freq vectors");
        }

        [Test]
        public virtual void TestOutOfBoundsAccess()
        {
            int numDocs = Reader.MaxDoc;
            try
            {
                Reader.Document(-1);
                Assert.Fail();
            }
            catch (System.IndexOutOfRangeException expected)
            {
            }

            try
            {
                Reader.GetTermVectors(-1);
                Assert.Fail();
            }
            catch (System.IndexOutOfRangeException expected)
            {
            }

            try
            {
                Reader.Document(numDocs);
                Assert.Fail();
            }
            catch (System.IndexOutOfRangeException expected)
            {
            }

            try
            {
                Reader.GetTermVectors(numDocs);
                Assert.Fail();
            }
            catch (System.IndexOutOfRangeException expected)
            {
            }
        }
    }
}