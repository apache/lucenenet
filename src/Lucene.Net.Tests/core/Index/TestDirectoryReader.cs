using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Lucene.Net.Documents;
using Lucene.Net.Search;

namespace Lucene.Net.Index
{
    using Lucene.Net.Support;
    using NUnit.Framework;
    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using Lucene41PostingsFormat = Lucene.Net.Codecs.Lucene41.Lucene41PostingsFormat;
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
    using NoSuchDirectoryException = Lucene.Net.Store.NoSuchDirectoryException;
    using OpenMode_e = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
    using StoredField = StoredField;
    using StringField = StringField;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;

    [TestFixture]
    public class TestDirectoryReader : LuceneTestCase
    {
        [Test]
        public virtual void TestDocument()
        {
            SegmentReader[] readers = new SegmentReader[2];
            Directory dir = NewDirectory();
            Document doc1 = new Document();
            Document doc2 = new Document();
            DocHelper.SetupDoc(doc1);
            DocHelper.SetupDoc(doc2);
            DocHelper.WriteDoc(Random(), dir, doc1);
            DocHelper.WriteDoc(Random(), dir, doc2);
            DirectoryReader reader = DirectoryReader.Open(dir);
            Assert.IsTrue(reader != null);
            Assert.IsTrue(reader is StandardDirectoryReader);

            Document newDoc1 = reader.Document(0);
            Assert.IsTrue(newDoc1 != null);
            Assert.IsTrue(DocHelper.NumFields(newDoc1) == DocHelper.NumFields(doc1) - DocHelper.Unstored.Count);
            Document newDoc2 = reader.Document(1);
            Assert.IsTrue(newDoc2 != null);
            Assert.IsTrue(DocHelper.NumFields(newDoc2) == DocHelper.NumFields(doc2) - DocHelper.Unstored.Count);
            Terms vector = reader.GetTermVectors(0).Terms(DocHelper.TEXT_FIELD_2_KEY);
            Assert.IsNotNull(vector);

            reader.Dispose();
            if (readers[0] != null)
            {
                readers[0].Dispose();
            }
            if (readers[1] != null)
            {
                readers[1].Dispose();
            }
            dir.Dispose();
        }

        [Test]
        public virtual void TestMultiTermDocs()
        {
            Directory ramDir1 = NewDirectory();
            AddDoc(Random(), ramDir1, "test foo", true);
            Directory ramDir2 = NewDirectory();
            AddDoc(Random(), ramDir2, "test blah", true);
            Directory ramDir3 = NewDirectory();
            AddDoc(Random(), ramDir3, "test wow", true);

            IndexReader[] readers1 = new IndexReader[] { DirectoryReader.Open(ramDir1), DirectoryReader.Open(ramDir3) };
            IndexReader[] readers2 = new IndexReader[] { DirectoryReader.Open(ramDir1), DirectoryReader.Open(ramDir2), DirectoryReader.Open(ramDir3) };
            MultiReader mr2 = new MultiReader(readers1);
            MultiReader mr3 = new MultiReader(readers2);

            // test mixing up TermDocs and TermEnums from different readers.
            TermsEnum te2 = MultiFields.GetTerms(mr2, "body").Iterator(null);
            te2.SeekCeil(new BytesRef("wow"));
            DocsEnum td = TestUtil.Docs(Random(), mr2, "body", te2.Term(), MultiFields.GetLiveDocs(mr2), null, 0);

            TermsEnum te3 = MultiFields.GetTerms(mr3, "body").Iterator(null);
            te3.SeekCeil(new BytesRef("wow"));
            td = TestUtil.Docs(Random(), te3, MultiFields.GetLiveDocs(mr3), td, 0);

            int ret = 0;

            // this should blow up if we forget to check that the TermEnum is from the same
            // reader as the TermDocs.
            while (td.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
            {
                ret += td.DocID();
            }

            // really a dummy assert to ensure that we got some docs and to ensure that
            // nothing is eliminated by hotspot
            Assert.IsTrue(ret > 0);
            readers1[0].Dispose();
            readers1[1].Dispose();
            readers2[0].Dispose();
            readers2[1].Dispose();
            readers2[2].Dispose();
            ramDir1.Dispose();
            ramDir2.Dispose();
            ramDir3.Dispose();
        }

        private void AddDoc(Random random, Directory ramDir1, string s, bool create)
        {
            IndexWriter iw = new IndexWriter(ramDir1, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)).SetOpenMode(create ? OpenMode_e.CREATE : OpenMode_e.APPEND));
            Document doc = new Document();
            doc.Add(NewTextField("body", s, Field.Store.NO));
            iw.AddDocument(doc);
            iw.Dispose();
        }

        [Test]
        public virtual void TestIsCurrent()
        {
            Directory d = NewDirectory();
            IndexWriter writer = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            AddDocumentWithFields(writer);
            writer.Dispose();
            // set up reader:
            DirectoryReader reader = DirectoryReader.Open(d);
            Assert.IsTrue(reader.Current);
            // modify index by adding another document:
            writer = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.APPEND));
            AddDocumentWithFields(writer);
            writer.Dispose();
            Assert.IsFalse(reader.Current);
            // re-create index:
            writer = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.CREATE));
            AddDocumentWithFields(writer);
            writer.Dispose();
            Assert.IsFalse(reader.Current);
            reader.Dispose();
            d.Dispose();
        }

        /// <summary>
        /// Tests the IndexReader.getFieldNames implementation </summary>
        /// <exception cref="Exception"> on error </exception>
        [Test]
        public virtual void TestGetFieldNames()
        {
            Directory d = NewDirectory();
            // set up writer
            IndexWriter writer = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));

            Document doc = new Document();

            FieldType customType3 = new FieldType();
            customType3.Stored = true;

            doc.Add(new StringField("keyword", "test1", Field.Store.YES));
            doc.Add(new TextField("text", "test1", Field.Store.YES));
            doc.Add(new Field("unindexed", "test1", customType3));
            doc.Add(new TextField("unstored", "test1", Field.Store.NO));
            writer.AddDocument(doc);

            writer.Dispose();
            // set up reader
            DirectoryReader reader = DirectoryReader.Open(d);
            FieldInfos fieldInfos = MultiFields.GetMergedFieldInfos(reader);
            Assert.IsNotNull(fieldInfos.FieldInfo("keyword"));
            Assert.IsNotNull(fieldInfos.FieldInfo("text"));
            Assert.IsNotNull(fieldInfos.FieldInfo("unindexed"));
            Assert.IsNotNull(fieldInfos.FieldInfo("unstored"));
            reader.Dispose();
            // add more documents
            writer = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.APPEND).SetMergePolicy(NewLogMergePolicy()));
            // want to get some more segments here
            int mergeFactor = ((LogMergePolicy)writer.Config.MergePolicy).MergeFactor;
            for (int i = 0; i < 5 * mergeFactor; i++)
            {
                doc = new Document();
                doc.Add(new StringField("keyword", "test1", Field.Store.YES));
                doc.Add(new TextField("text", "test1", Field.Store.YES));
                doc.Add(new Field("unindexed", "test1", customType3));
                doc.Add(new TextField("unstored", "test1", Field.Store.NO));
                writer.AddDocument(doc);
            }
            // new fields are in some different segments (we hope)
            for (int i = 0; i < 5 * mergeFactor; i++)
            {
                doc = new Document();
                doc.Add(new StringField("keyword2", "test1", Field.Store.YES));
                doc.Add(new TextField("text2", "test1", Field.Store.YES));
                doc.Add(new Field("unindexed2", "test1", customType3));
                doc.Add(new TextField("unstored2", "test1", Field.Store.NO));
                writer.AddDocument(doc);
            }
            // new termvector fields

            FieldType customType5 = new FieldType(TextField.TYPE_STORED);
            customType5.StoreTermVectors = true;
            FieldType customType6 = new FieldType(TextField.TYPE_STORED);
            customType6.StoreTermVectors = true;
            customType6.StoreTermVectorOffsets = true;
            FieldType customType7 = new FieldType(TextField.TYPE_STORED);
            customType7.StoreTermVectors = true;
            customType7.StoreTermVectorPositions = true;
            FieldType customType8 = new FieldType(TextField.TYPE_STORED);
            customType8.StoreTermVectors = true;
            customType8.StoreTermVectorOffsets = true;
            customType8.StoreTermVectorPositions = true;

            for (int i = 0; i < 5 * mergeFactor; i++)
            {
                doc = new Document();
                doc.Add(new TextField("tvnot", "tvnot", Field.Store.YES));
                doc.Add(new Field("termvector", "termvector", customType5));
                doc.Add(new Field("tvoffset", "tvoffset", customType6));
                doc.Add(new Field("tvposition", "tvposition", customType7));
                doc.Add(new Field("tvpositionoffset", "tvpositionoffset", customType8));
                writer.AddDocument(doc);
            }

            writer.Dispose();

            // verify fields again
            reader = DirectoryReader.Open(d);
            fieldInfos = MultiFields.GetMergedFieldInfos(reader);

            ICollection<string> allFieldNames = new HashSet<string>();
            ICollection<string> indexedFieldNames = new HashSet<string>();
            ICollection<string> notIndexedFieldNames = new HashSet<string>();
            ICollection<string> tvFieldNames = new HashSet<string>();

            foreach (FieldInfo fieldInfo in fieldInfos)
            {
                string name = fieldInfo.Name;
                allFieldNames.Add(name);
                if (fieldInfo.Indexed)
                {
                    indexedFieldNames.Add(name);
                }
                else
                {
                    notIndexedFieldNames.Add(name);
                }
                if (fieldInfo.HasVectors())
                {
                    tvFieldNames.Add(name);
                }
            }

            Assert.IsTrue(allFieldNames.Contains("keyword"));
            Assert.IsTrue(allFieldNames.Contains("text"));
            Assert.IsTrue(allFieldNames.Contains("unindexed"));
            Assert.IsTrue(allFieldNames.Contains("unstored"));
            Assert.IsTrue(allFieldNames.Contains("keyword2"));
            Assert.IsTrue(allFieldNames.Contains("text2"));
            Assert.IsTrue(allFieldNames.Contains("unindexed2"));
            Assert.IsTrue(allFieldNames.Contains("unstored2"));
            Assert.IsTrue(allFieldNames.Contains("tvnot"));
            Assert.IsTrue(allFieldNames.Contains("termvector"));
            Assert.IsTrue(allFieldNames.Contains("tvposition"));
            Assert.IsTrue(allFieldNames.Contains("tvoffset"));
            Assert.IsTrue(allFieldNames.Contains("tvpositionoffset"));

            // verify that only indexed fields were returned
            Assert.AreEqual(11, indexedFieldNames.Count); // 6 original + the 5 termvector fields
            Assert.IsTrue(indexedFieldNames.Contains("keyword"));
            Assert.IsTrue(indexedFieldNames.Contains("text"));
            Assert.IsTrue(indexedFieldNames.Contains("unstored"));
            Assert.IsTrue(indexedFieldNames.Contains("keyword2"));
            Assert.IsTrue(indexedFieldNames.Contains("text2"));
            Assert.IsTrue(indexedFieldNames.Contains("unstored2"));
            Assert.IsTrue(indexedFieldNames.Contains("tvnot"));
            Assert.IsTrue(indexedFieldNames.Contains("termvector"));
            Assert.IsTrue(indexedFieldNames.Contains("tvposition"));
            Assert.IsTrue(indexedFieldNames.Contains("tvoffset"));
            Assert.IsTrue(indexedFieldNames.Contains("tvpositionoffset"));

            // verify that only unindexed fields were returned
            Assert.AreEqual(2, notIndexedFieldNames.Count); // the following fields
            Assert.IsTrue(notIndexedFieldNames.Contains("unindexed"));
            Assert.IsTrue(notIndexedFieldNames.Contains("unindexed2"));

            // verify index term vector fields
            Assert.AreEqual(4, tvFieldNames.Count, tvFieldNames.ToString()); // 4 field has term vector only
            Assert.IsTrue(tvFieldNames.Contains("termvector"));

            reader.Dispose();
            d.Dispose();
        }

        [Test, MaxTime(40000)]
        public virtual void TestTermVectors()
        {
            Directory d = NewDirectory();
            // set up writer
            IndexWriter writer = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));
            // want to get some more segments here
            // new termvector fields
            int mergeFactor = ((LogMergePolicy)writer.Config.MergePolicy).MergeFactor;
            FieldType customType5 = new FieldType(TextField.TYPE_STORED);
            customType5.StoreTermVectors = true;
            FieldType customType6 = new FieldType(TextField.TYPE_STORED);
            customType6.StoreTermVectors = true;
            customType6.StoreTermVectorOffsets = true;
            FieldType customType7 = new FieldType(TextField.TYPE_STORED);
            customType7.StoreTermVectors = true;
            customType7.StoreTermVectorPositions = true;
            FieldType customType8 = new FieldType(TextField.TYPE_STORED);
            customType8.StoreTermVectors = true;
            customType8.StoreTermVectorOffsets = true;
            customType8.StoreTermVectorPositions = true;
            for (int i = 0; i < 5 * mergeFactor; i++)
            {
                Document doc = new Document();
                doc.Add(new TextField("tvnot", "one two two three three three", Field.Store.YES));
                doc.Add(new Field("termvector", "one two two three three three", customType5));
                doc.Add(new Field("tvoffset", "one two two three three three", customType6));
                doc.Add(new Field("tvposition", "one two two three three three", customType7));
                doc.Add(new Field("tvpositionoffset", "one two two three three three", customType8));

                writer.AddDocument(doc);
            }
            writer.Dispose();
            d.Dispose();
        }

        internal virtual void AssertTermDocsCount(string msg, IndexReader reader, Term term, int expected)
        {
            DocsEnum tdocs = TestUtil.Docs(Random(), reader, term.Field, new BytesRef(term.Text()), MultiFields.GetLiveDocs(reader), null, 0);
            int count = 0;
            if (tdocs != null)
            {
                while (tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                {
                    count++;
                }
            }
            Assert.AreEqual(expected, count, msg + ", count mismatch");
        }

        [Test]
        public virtual void TestBinaryFields()
        {
            Directory dir = NewDirectory();
            byte[] bin = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));

            for (int i = 0; i < 10; i++)
            {
                AddDoc(writer, "document number " + (i + 1));
                AddDocumentWithFields(writer);
                AddDocumentWithDifferentFields(writer);
                AddDocumentWithTermVectorFields(writer);
            }
            writer.Dispose();
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.APPEND).SetMergePolicy(NewLogMergePolicy()));
            Document doc = new Document();
            doc.Add(new StoredField("bin1", bin));
            doc.Add(new TextField("junk", "junk text", Field.Store.NO));
            writer.AddDocument(doc);
            writer.Dispose();
            DirectoryReader reader = DirectoryReader.Open(dir);
            Document doc2 = reader.Document(reader.MaxDoc - 1);
            IndexableField[] fields = doc2.GetFields("bin1");
            Assert.IsNotNull(fields);
            Assert.AreEqual(1, fields.Length);
            IndexableField b1 = fields[0];
            Assert.IsTrue(b1.BinaryValue != null);
            BytesRef bytesRef = b1.BinaryValue;
            Assert.AreEqual(bin.Length, bytesRef.Length);
            for (int i = 0; i < bin.Length; i++)
            {
                Assert.AreEqual(bin[i], bytesRef.Bytes[i + bytesRef.Offset]);
            }
            reader.Dispose();
            // force merge

            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.APPEND).SetMergePolicy(NewLogMergePolicy()));
            writer.ForceMerge(1);
            writer.Dispose();
            reader = DirectoryReader.Open(dir);
            doc2 = reader.Document(reader.MaxDoc - 1);
            fields = doc2.GetFields("bin1");
            Assert.IsNotNull(fields);
            Assert.AreEqual(1, fields.Length);
            b1 = fields[0];
            Assert.IsTrue(b1.BinaryValue != null);
            bytesRef = b1.BinaryValue;
            Assert.AreEqual(bin.Length, bytesRef.Length);
            for (int i = 0; i < bin.Length; i++)
            {
                Assert.AreEqual(bin[i], bytesRef.Bytes[i + bytesRef.Offset]);
            }
            reader.Dispose();
            dir.Dispose();
        }

        /* ??? public void testOpenEmptyDirectory() throws IOException{
          String dirName = "test.empty";
          File fileDirName = new File(dirName);
          if (!fileDirName.exists()) {
            fileDirName.mkdir();
          }
          try {
            DirectoryReader.Open(fileDirName);
            Assert.Fail("opening DirectoryReader on empty directory failed to produce FileNotFoundException/NoSuchFileException");
          } catch (FileNotFoundException | NoSuchFileException e) {
            // GOOD
          }
          rmDir(fileDirName);
        }*/

        [Test]
        public virtual void TestFilesOpenClose()
        {
            // Create initial data set
            DirectoryInfo dirFile = CreateTempDir("TestIndexReader.testFilesOpenClose");
            Directory dir = NewFSDirectory(dirFile);
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            AddDoc(writer, "test");
            writer.Dispose();
            dir.Dispose();

            // Try to erase the data - this ensures that the writer closed all files
            System.IO.Directory.Delete(dirFile.FullName, true);
            dir = NewFSDirectory(dirFile);

            // Now create the data set again, just as before
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.CREATE));
            AddDoc(writer, "test");
            writer.Dispose();
            dir.Dispose();

            // Now open existing directory and test that reader closes all files
            dir = NewFSDirectory(dirFile);
            DirectoryReader reader1 = DirectoryReader.Open(dir);
            reader1.Dispose();
            dir.Dispose();

            // The following will fail if reader did not close
            // all files
            System.IO.Directory.Delete(dirFile.FullName, true);
        }

        [Test]
        public virtual void TestOpenReaderAfterDelete()
        {
            DirectoryInfo dirFile = CreateTempDir("deletetest");
            Directory dir = NewFSDirectory(dirFile);
            try
            {
                DirectoryReader.Open(dir);
                Assert.Fail("expected FileNotFoundException/NoSuchFileException");
            }
            catch (System.IO.FileNotFoundException /*| NoSuchFileException*/ e)
            {
                // expected
            }

            dirFile.Delete();

            // Make sure we still get a CorruptIndexException (not NPE):
            try
            {
                DirectoryReader.Open(dir);
                Assert.Fail("expected FileNotFoundException/NoSuchFileException");
            }
            catch (System.IO.FileNotFoundException /*| NoSuchFileException*/ e)
            {
                // expected
            }

            dir.Dispose();
        }

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because NewStringField, NewTextField, NewField methods
        /// are no longer static.
        /// </summary>
        internal void AddDocumentWithFields(IndexWriter writer)
        {
            Document doc = new Document();

            FieldType customType3 = new FieldType();
            customType3.Stored = true;
            doc.Add(NewStringField("keyword", "test1", Field.Store.YES));
            doc.Add(NewTextField("text", "test1", Field.Store.YES));
            doc.Add(NewField("unindexed", "test1", customType3));
            doc.Add(new TextField("unstored", "test1", Field.Store.NO));
            writer.AddDocument(doc);
        }

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because NewStringField, NewTextField, NewField methods
        /// are no longer static.
        /// </summary>
        internal void AddDocumentWithDifferentFields(IndexWriter writer)
        {
            Document doc = new Document();

            FieldType customType3 = new FieldType();
            customType3.Stored = true;
            doc.Add(NewStringField("keyword2", "test1", Field.Store.YES));
            doc.Add(NewTextField("text2", "test1", Field.Store.YES));
            doc.Add(NewField("unindexed2", "test1", customType3));
            doc.Add(new TextField("unstored2", "test1", Field.Store.NO));
            writer.AddDocument(doc);
        }

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because NewTextField, NewField methods are no longer
        /// static.
        /// </summary>
        internal void AddDocumentWithTermVectorFields(IndexWriter writer)
        {
            Document doc = new Document();
            FieldType customType5 = new FieldType(TextField.TYPE_STORED);
            customType5.StoreTermVectors = true;
            FieldType customType6 = new FieldType(TextField.TYPE_STORED);
            customType6.StoreTermVectors = true;
            customType6.StoreTermVectorOffsets = true;
            FieldType customType7 = new FieldType(TextField.TYPE_STORED);
            customType7.StoreTermVectors = true;
            customType7.StoreTermVectorPositions = true;
            FieldType customType8 = new FieldType(TextField.TYPE_STORED);
            customType8.StoreTermVectors = true;
            customType8.StoreTermVectorOffsets = true;
            customType8.StoreTermVectorPositions = true;
            doc.Add(NewTextField("tvnot", "tvnot", Field.Store.YES));
            doc.Add(NewField("termvector", "termvector", customType5));
            doc.Add(NewField("tvoffset", "tvoffset", customType6));
            doc.Add(NewField("tvposition", "tvposition", customType7));
            doc.Add(NewField("tvpositionoffset", "tvpositionoffset", customType8));

            writer.AddDocument(doc);
        }

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because NewTextField is no longer static.
        /// </summary>
        internal void AddDoc(IndexWriter writer, string value)
        {
            Document doc = new Document();
            doc.Add(NewTextField("content", value, Field.Store.NO));
            writer.AddDocument(doc);
        }

        // TODO: maybe this can reuse the logic of test dueling codecs?
        public static void AssertIndexEquals(DirectoryReader index1, DirectoryReader index2)
        {
            Assert.AreEqual(index1.NumDocs, index2.NumDocs, "IndexReaders have different values for numDocs.");
            Assert.AreEqual(index1.MaxDoc, index2.MaxDoc, "IndexReaders have different values for maxDoc.");
            Assert.AreEqual(index1.HasDeletions, index2.HasDeletions, "Only one IndexReader has deletions.");
            Assert.AreEqual(index1.Leaves.Count == 1, index2.Leaves.Count == 1, "Single segment test differs.");

            // check field names
            FieldInfos fieldInfos1 = MultiFields.GetMergedFieldInfos(index1);
            FieldInfos fieldInfos2 = MultiFields.GetMergedFieldInfos(index2);
            Assert.AreEqual(fieldInfos1.Size(), fieldInfos2.Size(), "IndexReaders have different numbers of fields.");
            int numFields = fieldInfos1.Size();
            for (int fieldID = 0; fieldID < numFields; fieldID++)
            {
                FieldInfo fieldInfo1 = fieldInfos1.FieldInfo(fieldID);
                FieldInfo fieldInfo2 = fieldInfos2.FieldInfo(fieldID);
                Assert.AreEqual(fieldInfo1.Name, fieldInfo2.Name, "Different field names.");
            }

            // check norms
            foreach (FieldInfo fieldInfo in fieldInfos1)
            {
                string curField = fieldInfo.Name;
                NumericDocValues norms1 = MultiDocValues.GetNormValues(index1, curField);
                NumericDocValues norms2 = MultiDocValues.GetNormValues(index2, curField);
                if (norms1 != null && norms2 != null)
                {
                    // todo: generalize this (like TestDuelingCodecs assert)
                    for (int i = 0; i < index1.MaxDoc; i++)
                    {
                        Assert.AreEqual(norms1.Get(i), norms2.Get(i), "Norm different for doc " + i + " and field '" + curField + "'.");
                    }
                }
                else
                {
                    Assert.IsNull(norms1);
                    Assert.IsNull(norms2);
                }
            }

            // check deletions
            Bits liveDocs1 = MultiFields.GetLiveDocs(index1);
            Bits liveDocs2 = MultiFields.GetLiveDocs(index2);
            for (int i = 0; i < index1.MaxDoc; i++)
            {
                Assert.AreEqual(liveDocs1 == null || !liveDocs1.Get(i), liveDocs2 == null || !liveDocs2.Get(i), "Doc " + i + " only deleted in one index.");
            }

            // check stored fields
            for (int i = 0; i < index1.MaxDoc; i++)
            {
                if (liveDocs1 == null || liveDocs1.Get(i))
                {
                    Document doc1 = index1.Document(i);
                    Document doc2 = index2.Document(i);
                    IList<IndexableField> field1 = doc1.Fields;
                    IList<IndexableField> field2 = doc2.Fields;
                    Assert.AreEqual(field1.Count, field2.Count, "Different numbers of fields for doc " + i + ".");
                    IEnumerator<IndexableField> itField1 = field1.GetEnumerator();
                    IEnumerator<IndexableField> itField2 = field2.GetEnumerator();
                    while (itField1.MoveNext())
                    {
                        Field curField1 = (Field)itField1.Current;
                        itField2.MoveNext();
                        Field curField2 = (Field)itField2.Current;
                        Assert.AreEqual(curField1.Name, curField2.Name, "Different fields names for doc " + i + ".");
                        Assert.AreEqual(curField1.StringValue, curField2.StringValue, "Different field values for doc " + i + ".");
                    }
                }
            }

            // check dictionary and posting lists
            Fields fields1 = MultiFields.GetFields(index1);
            Fields fields2 = MultiFields.GetFields(index2);
            IEnumerator<string> fenum2 = fields2.GetEnumerator();
            Bits liveDocs = MultiFields.GetLiveDocs(index1);
            foreach (string field1 in fields1)
            {
                fenum2.MoveNext();
                Assert.AreEqual(field1, fenum2.Current, "Different fields");
                Terms terms1 = fields1.Terms(field1);
                if (terms1 == null)
                {
                    Assert.IsNull(fields2.Terms(field1));
                    continue;
                }
                TermsEnum enum1 = terms1.Iterator(null);

                Terms terms2 = fields2.Terms(field1);
                Assert.IsNotNull(terms2);
                TermsEnum enum2 = terms2.Iterator(null);

                while (enum1.Next() != null)
                {
                    Assert.AreEqual(enum1.Term(), enum2.Next(), "Different terms");
                    DocsAndPositionsEnum tp1 = enum1.DocsAndPositions(liveDocs, null);
                    DocsAndPositionsEnum tp2 = enum2.DocsAndPositions(liveDocs, null);

                    while (tp1.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                    {
                        Assert.IsTrue(tp2.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
                        Assert.AreEqual(tp1.DocID(), tp2.DocID(), "Different doc id in postinglist of term " + enum1.Term() + ".");
                        Assert.AreEqual(tp1.Freq(), tp2.Freq(), "Different term frequence in postinglist of term " + enum1.Term() + ".");
                        for (int i = 0; i < tp1.Freq(); i++)
                        {
                            Assert.AreEqual(tp1.NextPosition(), tp2.NextPosition(), "Different positions in postinglist of term " + enum1.Term() + ".");
                        }
                    }
                }
            }
            Assert.IsFalse(fenum2.MoveNext());
        }

        [Test]
        public virtual void TestGetIndexCommit()
        {
            Directory d = NewDirectory();

            // set up writer
            IndexWriter writer = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2).SetMergePolicy(NewLogMergePolicy(10)));
            for (int i = 0; i < 27; i++)
            {
                AddDocumentWithFields(writer);
            }
            writer.Dispose();

            SegmentInfos sis = new SegmentInfos();
            sis.Read(d);
            DirectoryReader r = DirectoryReader.Open(d);
            IndexCommit c = r.IndexCommit;

            Assert.AreEqual(sis.SegmentsFileName, c.SegmentsFileName);

            Assert.IsTrue(c.Equals(r.IndexCommit));

            // Change the index
            writer = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.APPEND).SetMaxBufferedDocs(2).SetMergePolicy(NewLogMergePolicy(10)));
            for (int i = 0; i < 7; i++)
            {
                AddDocumentWithFields(writer);
            }
            writer.Dispose();

            DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
            Assert.IsNotNull(r2);
            Assert.IsFalse(c.Equals(r2.IndexCommit));
            Assert.IsFalse(r2.IndexCommit.SegmentCount == 1);
            r2.Dispose();

            writer = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.APPEND));
            writer.ForceMerge(1);
            writer.Dispose();

            r2 = DirectoryReader.OpenIfChanged(r);
            Assert.IsNotNull(r2);
            Assert.IsNull(DirectoryReader.OpenIfChanged(r2));
            Assert.AreEqual(1, r2.IndexCommit.SegmentCount);

            r.Dispose();
            r2.Dispose();
            d.Dispose();
        }

        internal Document CreateDocument(string id)
        {
            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.Tokenized = false;
            customType.OmitNorms = true;

            doc.Add(NewField("id", id, customType));
            return doc;
        }

        // LUCENE-1468 -- make sure on attempting to open an
        // DirectoryReader on a non-existent directory, you get a
        // good exception
        [Test]
        public virtual void TestNoDir()
        {
            DirectoryInfo tempDir = CreateTempDir("doesnotexist");
            System.IO.Directory.Delete(tempDir.FullName, true);
            Directory dir = NewFSDirectory(tempDir);
            try
            {
                DirectoryReader.Open(dir);
                Assert.Fail("did not hit expected exception");
            }
            catch (NoSuchDirectoryException nsde)
            {
                // expected
            }
            dir.Dispose();
        }

        // LUCENE-1509
        [Test]
        public virtual void TestNoDupCommitFileNames()
        {
            Directory dir = NewDirectory();

            IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2));
            writer.AddDocument(CreateDocument("a"));
            writer.AddDocument(CreateDocument("a"));
            writer.AddDocument(CreateDocument("a"));
            writer.Dispose();

            ICollection<IndexCommit> commits = DirectoryReader.ListCommits(dir);
            foreach (IndexCommit commit in commits)
            {
                ICollection<string> files = commit.FileNames;
                HashSet<string> seen = new HashSet<string>();
                foreach (String fileName in files)
                {
                    Assert.IsTrue(!seen.Contains(fileName), "file " + fileName + " was duplicated");
                    seen.Add(fileName);
                }
            }

            dir.Dispose();
        }

        // LUCENE-1579: Ensure that on a reopened reader, that any
        // shared segments reuse the doc values arrays in
        // FieldCache
        [Test]
        public virtual void TestFieldCacheReuseAfterReopen()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy(10)));
            Document doc = new Document();
            doc.Add(NewStringField("number", "17", Field.Store.NO));
            writer.AddDocument(doc);
            writer.Commit();

            // Open reader1
            DirectoryReader r = DirectoryReader.Open(dir);
            AtomicReader r1 = GetOnlySegmentReader(r);
            FieldCache.Ints ints = FieldCache.DEFAULT.GetInts(r1, "number", false);
            Assert.AreEqual(17, ints.Get(0));

            // Add new segment
            writer.AddDocument(doc);
            writer.Commit();

            // Reopen reader1 --> reader2
            DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
            Assert.IsNotNull(r2);
            r.Dispose();
            AtomicReader sub0 = (AtomicReader)r2.Leaves[0].Reader;
            FieldCache.Ints ints2 = FieldCache.DEFAULT.GetInts(sub0, "number", false);
            r2.Dispose();
            Assert.IsTrue(ints == ints2);

            writer.Dispose();
            dir.Dispose();
        }

        // LUCENE-1586: getUniqueTermCount
        [Test]
        public virtual void TestUniqueTermCount()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            Document doc = new Document();
            doc.Add(NewTextField("field", "a b c d e f g h i j k l m n o p q r s t u v w x y z", Field.Store.NO));
            doc.Add(NewTextField("number", "0 1 2 3 4 5 6 7 8 9", Field.Store.NO));
            writer.AddDocument(doc);
            writer.AddDocument(doc);
            writer.Commit();

            DirectoryReader r = DirectoryReader.Open(dir);
            AtomicReader r1 = GetOnlySegmentReader(r);
            Assert.AreEqual(36, r1.Fields.UniqueTermCount);
            writer.AddDocument(doc);
            writer.Commit();
            DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
            Assert.IsNotNull(r2);
            r.Dispose();

            foreach (AtomicReaderContext s in r2.Leaves)
            {
                Assert.AreEqual(36, ((AtomicReader)s.Reader).Fields.UniqueTermCount);
            }
            r2.Dispose();
            writer.Dispose();
            dir.Dispose();
        }

        // LUCENE-1609: don't load terms index
        [Test]
        public virtual void TestNoTermsIndex()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetCodec(TestUtil.AlwaysPostingsFormat(new Lucene41PostingsFormat())));
            Document doc = new Document();
            doc.Add(NewTextField("field", "a b c d e f g h i j k l m n o p q r s t u v w x y z", Field.Store.NO));
            doc.Add(NewTextField("number", "0 1 2 3 4 5 6 7 8 9", Field.Store.NO));
            writer.AddDocument(doc);
            writer.AddDocument(doc);
            writer.Dispose();

            DirectoryReader r = DirectoryReader.Open(dir, -1);
            try
            {
                r.DocFreq(new Term("field", "f"));
                Assert.Fail("did not hit expected exception");
            }
            catch (InvalidOperationException ise)
            {
                // expected
            }

            Assert.AreEqual(-1, ((SegmentReader)r.Leaves[0].Reader).TermInfosIndexDivisor);
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetCodec(TestUtil.AlwaysPostingsFormat(new Lucene41PostingsFormat())).SetMergePolicy(NewLogMergePolicy(10)));
            writer.AddDocument(doc);
            writer.Dispose();

            // LUCENE-1718: ensure re-open carries over no terms index:
            DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
            Assert.IsNotNull(r2);
            Assert.IsNull(DirectoryReader.OpenIfChanged(r2));
            r.Dispose();
            IList<AtomicReaderContext> leaves = r2.Leaves;
            Assert.AreEqual(2, leaves.Count);
            foreach (AtomicReaderContext ctx in leaves)
            {
                try
                {
                    ctx.Reader.DocFreq(new Term("field", "f"));
                    Assert.Fail("did not hit expected exception");
                }
                catch (InvalidOperationException ise)
                {
                    // expected
                }
            }
            r2.Dispose();
            dir.Dispose();
        }

        // LUCENE-2046
        [Test]
        public virtual void TestPrepareCommitIsCurrent()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            writer.Commit();
            Document doc = new Document();
            writer.AddDocument(doc);
            DirectoryReader r = DirectoryReader.Open(dir);
            Assert.IsTrue(r.Current);
            writer.AddDocument(doc);
            writer.PrepareCommit();
            Assert.IsTrue(r.Current);
            DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
            Assert.IsNull(r2);
            writer.Commit();
            Assert.IsFalse(r.Current);
            writer.Dispose();
            r.Dispose();
            dir.Dispose();
        }

        // LUCENE-2753
        [Test]
        public virtual void TestListCommits()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, null).SetIndexDeletionPolicy(new SnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy())));
            SnapshotDeletionPolicy sdp = (SnapshotDeletionPolicy)writer.Config.DelPolicy;
            writer.AddDocument(new Document());
            writer.Commit();
            sdp.Snapshot();
            writer.AddDocument(new Document());
            writer.Commit();
            sdp.Snapshot();
            writer.AddDocument(new Document());
            writer.Commit();
            sdp.Snapshot();
            writer.Dispose();
            long currentGen = 0;
            foreach (IndexCommit ic in DirectoryReader.ListCommits(dir))
            {
                Assert.IsTrue(currentGen < ic.Generation, "currentGen=" + currentGen + " commitGen=" + ic.Generation);
                currentGen = ic.Generation;
            }
            dir.Dispose();
        }

        // Make sure totalTermFreq works correctly in the terms
        // dict cache
        [Test]
        public virtual void TestTotalTermFreqCached()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            Document d = new Document();
            d.Add(NewTextField("f", "a a b", Field.Store.NO));
            writer.AddDocument(d);
            DirectoryReader r = writer.Reader;
            writer.Dispose();
            try
            {
                // Make sure codec impls totalTermFreq (eg PreFlex doesn't)
                Assume.That(r.TotalTermFreq(new Term("f", new BytesRef("b"))) != -1);
                Assert.AreEqual(1, r.TotalTermFreq(new Term("f", new BytesRef("b"))));
                Assert.AreEqual(2, r.TotalTermFreq(new Term("f", new BytesRef("a"))));
                Assert.AreEqual(1, r.TotalTermFreq(new Term("f", new BytesRef("b"))));
            }
            finally
            {
                r.Dispose();
                dir.Dispose();
            }
        }

        [Test]
        public virtual void TestGetSumDocFreq()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            Document d = new Document();
            d.Add(NewTextField("f", "a", Field.Store.NO));
            writer.AddDocument(d);
            d = new Document();
            d.Add(NewTextField("f", "b", Field.Store.NO));
            writer.AddDocument(d);
            DirectoryReader r = writer.Reader;
            writer.Dispose();
            try
            {
                // Make sure codec impls getSumDocFreq (eg PreFlex doesn't)
                Assume.That(r.GetSumDocFreq("f") != -1);
                Assert.AreEqual(2, r.GetSumDocFreq("f"));
            }
            finally
            {
                r.Dispose();
                dir.Dispose();
            }
        }

        [Test]
        public virtual void TestGetDocCount()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            Document d = new Document();
            d.Add(NewTextField("f", "a", Field.Store.NO));
            writer.AddDocument(d);
            d = new Document();
            d.Add(NewTextField("f", "a", Field.Store.NO));
            writer.AddDocument(d);
            DirectoryReader r = writer.Reader;
            writer.Dispose();
            try
            {
                // Make sure codec impls getSumDocFreq (eg PreFlex doesn't)
                Assume.That(r.GetDocCount("f") != -1);
                Assert.AreEqual(2, r.GetDocCount("f"));
            }
            finally
            {
                r.Dispose();
                dir.Dispose();
            }
        }

        [Test]
        public virtual void TestGetSumTotalTermFreq()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            Document d = new Document();
            d.Add(NewTextField("f", "a b b", Field.Store.NO));
            writer.AddDocument(d);
            d = new Document();
            d.Add(NewTextField("f", "a a b", Field.Store.NO));
            writer.AddDocument(d);
            DirectoryReader r = writer.Reader;
            writer.Dispose();
            try
            {
                // Make sure codec impls getSumDocFreq (eg PreFlex doesn't)
                Assume.That(r.GetSumTotalTermFreq("f") != -1);
                Assert.AreEqual(6, r.GetSumTotalTermFreq("f"));
            }
            finally
            {
                r.Dispose();
                dir.Dispose();
            }
        }

        // LUCENE-2474
        [Test]
        public virtual void TestReaderFinishedListener()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));
            ((LogMergePolicy)writer.Config.MergePolicy).MergeFactor = 3;
            writer.AddDocument(new Document());
            writer.Commit();
            writer.AddDocument(new Document());
            writer.Commit();
            DirectoryReader reader = writer.Reader;
            int[] closeCount = new int[1];
            IndexReader.ReaderClosedListener listener = new ReaderClosedListenerAnonymousInnerClassHelper(this, reader, closeCount);

            reader.AddReaderClosedListener(listener);

            reader.Dispose();

            // Close the top reader, its the only one that should be closed
            Assert.AreEqual(1, closeCount[0]);
            writer.Dispose();

            DirectoryReader reader2 = DirectoryReader.Open(dir);
            reader2.AddReaderClosedListener(listener);

            closeCount[0] = 0;
            reader2.Dispose();
            Assert.AreEqual(1, closeCount[0]);
            dir.Dispose();
        }

        private class ReaderClosedListenerAnonymousInnerClassHelper : IndexReader.ReaderClosedListener
        {
            private readonly TestDirectoryReader OuterInstance;

            private DirectoryReader Reader;
            private int[] CloseCount;

            public ReaderClosedListenerAnonymousInnerClassHelper(TestDirectoryReader outerInstance, DirectoryReader reader, int[] closeCount)
            {
                this.OuterInstance = outerInstance;
                this.Reader = reader;
                this.CloseCount = closeCount;
            }

            public void OnClose(IndexReader reader)
            {
                CloseCount[0]++;
            }
        }

        [Test]
        public virtual void TestOOBDocID()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            writer.AddDocument(new Document());
            DirectoryReader r = writer.Reader;
            writer.Dispose();
            r.Document(0);
            try
            {
                r.Document(1);
                Assert.Fail("did not hit exception");
            }
            catch (System.ArgumentException iae)
            {
                // expected
            }
            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestTryIncRef()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            writer.AddDocument(new Document());
            writer.Commit();
            DirectoryReader r = DirectoryReader.Open(dir);
            Assert.IsTrue(r.TryIncRef());
            r.DecRef();
            r.Dispose();
            Assert.IsFalse(r.TryIncRef());
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestStressTryIncRef()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            writer.AddDocument(new Document());
            writer.Commit();
            DirectoryReader r = DirectoryReader.Open(dir);
            int numThreads = AtLeast(2);

            IncThread[] threads = new IncThread[numThreads];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new IncThread(r, Random());
                threads[i].Start();
            }
            Thread.Sleep(100);

            Assert.IsTrue(r.TryIncRef());
            r.DecRef();
            r.Dispose();

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Join();
                Assert.IsNull(threads[i].Failed);
            }
            Assert.IsFalse(r.TryIncRef());
            writer.Dispose();
            dir.Dispose();
        }

        internal class IncThread : ThreadClass
        {
            internal readonly IndexReader ToInc;
            internal readonly Random Random;
            internal Exception Failed;

            internal IncThread(IndexReader toInc, Random random)
            {
                this.ToInc = toInc;
                this.Random = random;
            }

            public override void Run()
            {
                try
                {
                    while (ToInc.TryIncRef())
                    {
                        Assert.IsFalse(ToInc.HasDeletions);
                        ToInc.DecRef();
                    }
                    Assert.IsFalse(ToInc.TryIncRef());
                }
                catch (Exception e)
                {
                    Failed = e;
                }
            }
        }

        [Test]
        public virtual void TestLoadCertainFields()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("field1", "foobar", Field.Store.YES));
            doc.Add(NewStringField("field2", "foobaz", Field.Store.YES));
            writer.AddDocument(doc);
            DirectoryReader r = writer.Reader;
            writer.Dispose();
            HashSet<string> fieldsToLoad = new HashSet<string>();
            Assert.AreEqual(0, r.Document(0, fieldsToLoad).Fields.Count);
            fieldsToLoad.Add("field1");
            Document doc2 = r.Document(0, fieldsToLoad);
            Assert.AreEqual(1, doc2.Fields.Count);
            Assert.AreEqual("foobar", doc2.Get("field1"));
            r.Dispose();
            dir.Dispose();
        }

        /// @deprecated just to ensure IndexReader static methods work
        [Obsolete("just to ensure IndexReader static methods work")]
        [Test]
        public virtual void TestBackwards()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetCodec(TestUtil.AlwaysPostingsFormat(new Lucene41PostingsFormat())));
            Document doc = new Document();
            doc.Add(NewTextField("field", "a b c d e f g h i j k l m n o p q r s t u v w x y z", Field.Store.NO));
            doc.Add(NewTextField("number", "0 1 2 3 4 5 6 7 8 9", Field.Store.NO));
            writer.AddDocument(doc);

            // open(IndexWriter, boolean)
            DirectoryReader r = IndexReader.Open(writer, true);
            Assert.AreEqual(1, r.DocFreq(new Term("field", "f")));
            r.Dispose();
            writer.AddDocument(doc);
            writer.Dispose();

            // open(Directory)
            r = IndexReader.Open(dir);
            Assert.AreEqual(2, r.DocFreq(new Term("field", "f")));
            r.Dispose();

            // open(IndexCommit)
            IList<IndexCommit> commits = DirectoryReader.ListCommits(dir);
            Assert.AreEqual(1, commits.Count);
            r = IndexReader.Open(commits[0]);
            Assert.AreEqual(2, r.DocFreq(new Term("field", "f")));
            r.Dispose();

            // open(Directory, int)
            r = IndexReader.Open(dir, -1);
            try
            {
                r.DocFreq(new Term("field", "f"));
                Assert.Fail("did not hit expected exception");
            }
            catch (InvalidOperationException ise)
            {
                // expected
            }
            Assert.AreEqual(-1, ((SegmentReader)r.Leaves[0].Reader).TermInfosIndexDivisor);
            r.Dispose();

            // open(IndexCommit, int)
            r = IndexReader.Open(commits[0], -1);
            try
            {
                r.DocFreq(new Term("field", "f"));
                Assert.Fail("did not hit expected exception");
            }
            catch (InvalidOperationException ise)
            {
                // expected
            }
            Assert.AreEqual(-1, ((SegmentReader)r.Leaves[0].Reader).TermInfosIndexDivisor);
            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestIndexExistsOnNonExistentDirectory()
        {
            DirectoryInfo tempDir = CreateTempDir("testIndexExistsOnNonExistentDirectory");
            tempDir.Delete();
            Directory dir = NewFSDirectory(tempDir);
            Console.WriteLine("dir=" + dir);
            Assert.IsFalse(DirectoryReader.IndexExists(dir));
            dir.Dispose();
        }
    }
}