using NUnit.Framework;
using System;
using System.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using JCG = J2N.Collections.Generic;

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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using IOContext = Lucene.Net.Store.IOContext;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestSegmentReader : LuceneTestCase
    {
        private Directory dir;
        private Document testDoc;
        private SegmentReader reader;

        //TODO: Setup the reader w/ multiple documents
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            dir = NewDirectory();
            testDoc = new Document();
            DocHelper.SetupDoc(testDoc);
            SegmentCommitInfo info = DocHelper.WriteDoc(Random, dir, testDoc);
            reader = new SegmentReader(info, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, IOContext.READ);
        }

        [TearDown]
        public override void TearDown()
        {
            reader.Dispose();
            dir.Dispose();
            base.TearDown();
        }

        [Test]
        public virtual void Test()
        {
            Assert.IsTrue(dir != null);
            Assert.IsTrue(reader != null);
            Assert.IsTrue(DocHelper.NameValues.Count > 0);
            Assert.IsTrue(DocHelper.NumFields(testDoc) == DocHelper.All.Count);
        }

        [Test]
        public virtual void TestDocument()
        {
            Assert.IsTrue(reader.NumDocs == 1);
            Assert.IsTrue(reader.MaxDoc >= 1);
            Document result = reader.Document(0);
            Assert.IsTrue(result != null);
            //There are 2 unstored fields on the document that are not preserved across writing
            Assert.IsTrue(DocHelper.NumFields(result) == DocHelper.NumFields(testDoc) - DocHelper.Unstored.Count);

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
            ICollection<string> allFieldNames = new JCG.HashSet<string>();
            ICollection<string> indexedFieldNames = new JCG.HashSet<string>();
            ICollection<string> notIndexedFieldNames = new JCG.HashSet<string>();
            ICollection<string> tvFieldNames = new JCG.HashSet<string>();
            ICollection<string> noTVFieldNames = new JCG.HashSet<string>();

            foreach (FieldInfo fieldInfo in reader.FieldInfos)
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
                Assert.IsTrue(DocHelper.NameValues.ContainsKey(s) == true || s.Length == 0); // LUCENENET: CA1820: Test for empty strings using string length
            }

            Assert.IsTrue(indexedFieldNames.Count == DocHelper.Indexed.Count);
            foreach (string s in indexedFieldNames)
            {
                Assert.IsTrue(DocHelper.Indexed.ContainsKey(s) == true || s.Length == 0); // LUCENENET: CA1820: Test for empty strings using string length
            }

            Assert.IsTrue(notIndexedFieldNames.Count == DocHelper.Unindexed.Count);
            //Get all indexed fields that are storing term vectors
            Assert.IsTrue(tvFieldNames.Count == DocHelper.Termvector.Count);

            Assert.IsTrue(noTVFieldNames.Count == DocHelper.Notermvector.Count);
        }

        [Test]
        public virtual void TestTerms()
        {
            Fields fields = MultiFields.GetFields(reader);
            foreach (string field in fields)
            {
                Terms terms = fields.GetTerms(field);
                Assert.IsNotNull(terms);
                TermsEnum termsEnum = terms.GetEnumerator();
                while (termsEnum.MoveNext())
                {
                    BytesRef term = termsEnum.Term;
                    Assert.IsTrue(term != null);
                    string fieldValue = (string)DocHelper.NameValues[field];
                    Assert.IsTrue(fieldValue.IndexOf(term.Utf8ToString(), StringComparison.Ordinal) != -1);
                }
            }

            DocsEnum termDocs = TestUtil.Docs(Random, reader, DocHelper.TEXT_FIELD_1_KEY, new BytesRef("field"), MultiFields.GetLiveDocs(reader), null, 0);
            Assert.IsTrue(termDocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);

            termDocs = TestUtil.Docs(Random, reader, DocHelper.NO_NORMS_KEY, new BytesRef(DocHelper.NO_NORMS_TEXT), MultiFields.GetLiveDocs(reader), null, 0);

            Assert.IsTrue(termDocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);

            DocsAndPositionsEnum positions = MultiFields.GetTermPositionsEnum(reader, MultiFields.GetLiveDocs(reader), DocHelper.TEXT_FIELD_1_KEY, new BytesRef("field"));
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
                } catch (Exception e) when (e.IsIOException()) {
                  e.printStackTrace();
                  Assert.IsTrue(false);
                }
            */

            CheckNorms(reader);
        }

        public static void CheckNorms(AtomicReader reader)
        {
            // test omit norms
            for (int i = 0; i < DocHelper.Fields.Length; i++)
            {
                IIndexableField f = DocHelper.Fields[i];
                if (f.IndexableFieldType.IsIndexed)
                {
                    Assert.AreEqual(reader.GetNormValues(f.Name) != null, !f.IndexableFieldType.OmitNorms);
                    Assert.AreEqual(reader.GetNormValues(f.Name) != null, !DocHelper.NoNorms.ContainsKey(f.Name));
                    if (reader.GetNormValues(f.Name) is null)
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
            Terms result = reader.GetTermVectors(0).GetTerms(DocHelper.TEXT_FIELD_2_KEY);
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count);
            TermsEnum termsEnum = result.GetEnumerator();
            while (termsEnum.MoveNext())
            {
                string term = termsEnum.Term.Utf8ToString();
                int freq = (int)termsEnum.TotalTermFreq;
                Assert.IsTrue(DocHelper.FIELD_2_TEXT.IndexOf(term, StringComparison.Ordinal) != -1);
                Assert.IsTrue(freq > 0);
            }

            Fields results = reader.GetTermVectors(0);
            Assert.IsTrue(results != null);
            Assert.AreEqual(3, results.Count, "We do not have 3 term freq vectors");
        }

        [Test]
        public virtual void TestOutOfBoundsAccess()
        {
            int numDocs = reader.MaxDoc;
            try
            {
                reader.Document(-1);
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIndexOutOfBoundsException())
            {
            }

            try
            {
                reader.GetTermVectors(-1);
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIndexOutOfBoundsException())
            {
            }

            try
            {
                reader.Document(numDocs);
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIndexOutOfBoundsException())
            {
            }

            try
            {
                reader.GetTermVectors(numDocs);
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIndexOutOfBoundsException())
            {
            }
        }
    }
}