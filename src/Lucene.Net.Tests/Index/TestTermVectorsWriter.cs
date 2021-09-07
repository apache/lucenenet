using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System;
using System.IO;
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

    using Analyzer = Lucene.Net.Analysis.Analyzer;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using CachingTokenFilter = Lucene.Net.Analysis.CachingTokenFilter;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using MockTokenFilter = Lucene.Net.Analysis.MockTokenFilter;
    using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;
    using StringField = StringField;
    using TextField = TextField;
    using TokenStream = Lucene.Net.Analysis.TokenStream;

    /// <summary>
    /// tests for writing term vectors </summary>
    [TestFixture]
    public class TestTermVectorsWriter : LuceneTestCase
    {
        // LUCENE-1442
        [Test]
        public virtual void TestDoubleOffsetCounting()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document doc = new Document();
            FieldType customType = new FieldType(StringField.TYPE_NOT_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorOffsets = true;
            Field f = NewField("field", "abcd", customType);
            doc.Add(f);
            doc.Add(f);
            Field f2 = NewField("field", "", customType);
            doc.Add(f2);
            doc.Add(f);
            w.AddDocument(doc);
            w.Dispose();

            IndexReader r = DirectoryReader.Open(dir);
            Terms vector = r.GetTermVectors(0).GetTerms("field");
            Assert.IsNotNull(vector);
            TermsEnum termsEnum = vector.GetEnumerator();
            Assert.IsTrue(termsEnum.MoveNext());
            Assert.AreEqual("", termsEnum.Term.Utf8ToString());

            // Token "" occurred once
            Assert.AreEqual(1, termsEnum.TotalTermFreq);

            DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
            Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            dpEnum.NextPosition();
            Assert.AreEqual(8, dpEnum.StartOffset);
            Assert.AreEqual(8, dpEnum.EndOffset);
            Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());

            // Token "abcd" occurred three times
            Assert.IsTrue(termsEnum.MoveNext());
            Assert.AreEqual(new BytesRef("abcd"), termsEnum.Term);
            dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
            Assert.AreEqual(3, termsEnum.TotalTermFreq);

            Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            dpEnum.NextPosition();
            Assert.AreEqual(0, dpEnum.StartOffset);
            Assert.AreEqual(4, dpEnum.EndOffset);

            dpEnum.NextPosition();
            Assert.AreEqual(4, dpEnum.StartOffset);
            Assert.AreEqual(8, dpEnum.EndOffset);

            dpEnum.NextPosition();
            Assert.AreEqual(8, dpEnum.StartOffset);
            Assert.AreEqual(12, dpEnum.EndOffset);

            Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());
            Assert.IsFalse(termsEnum.MoveNext());
            r.Dispose();
            dir.Dispose();
        }

        // LUCENE-1442
        [Test]
        public virtual void TestDoubleOffsetCounting2()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorOffsets = true;
            Field f = NewField("field", "abcd", customType);
            doc.Add(f);
            doc.Add(f);
            w.AddDocument(doc);
            w.Dispose();

            IndexReader r = DirectoryReader.Open(dir);
            TermsEnum termsEnum = r.GetTermVectors(0).GetTerms("field").GetEnumerator();
            Assert.IsTrue(termsEnum.MoveNext());
            DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
            Assert.AreEqual(2, termsEnum.TotalTermFreq);

            Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            dpEnum.NextPosition();
            Assert.AreEqual(0, dpEnum.StartOffset);
            Assert.AreEqual(4, dpEnum.EndOffset);

            dpEnum.NextPosition();
            Assert.AreEqual(5, dpEnum.StartOffset);
            Assert.AreEqual(9, dpEnum.EndOffset);
            Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());

            r.Dispose();
            dir.Dispose();
        }

        // LUCENE-1448
        [Test]
        public virtual void TestEndOffsetPositionCharAnalyzer()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorOffsets = true;
            Field f = NewField("field", "abcd   ", customType);
            doc.Add(f);
            doc.Add(f);
            w.AddDocument(doc);
            w.Dispose();

            IndexReader r = DirectoryReader.Open(dir);
            TermsEnum termsEnum = r.GetTermVectors(0).GetTerms("field").GetEnumerator();
            Assert.IsTrue(termsEnum.MoveNext());
            DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
            Assert.AreEqual(2, termsEnum.TotalTermFreq);

            Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            dpEnum.NextPosition();
            Assert.AreEqual(0, dpEnum.StartOffset);
            Assert.AreEqual(4, dpEnum.EndOffset);

            dpEnum.NextPosition();
            Assert.AreEqual(8, dpEnum.StartOffset);
            Assert.AreEqual(12, dpEnum.EndOffset);
            Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());

            r.Dispose();
            dir.Dispose();
        }

        // LUCENE-1448
        [Test]
        public virtual void TestEndOffsetPositionWithCachingTokenFilter()
        {
            Directory dir = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random);
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
            Document doc = new Document();
            Exception priorException = null; // LUCENENET: No need to cast to IOExcpetion
            TokenStream stream = analyzer.GetTokenStream("field", new StringReader("abcd   "));
            try
            {
                stream.Reset(); // TODO: weird to reset before wrapping with CachingTokenFilter... correct?
                TokenStream cachedStream = new CachingTokenFilter(stream);
                FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
                customType.StoreTermVectors = true;
                customType.StoreTermVectorPositions = true;
                customType.StoreTermVectorOffsets = true;
                Field f = new Field("field", cachedStream, customType);
                doc.Add(f);
                doc.Add(f);
                w.AddDocument(doc);
            }
            catch (Exception e) when (e.IsIOException())
            {
                priorException = e;
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(priorException, stream);
            }
            w.Dispose();

            IndexReader r = DirectoryReader.Open(dir);
            TermsEnum termsEnum = r.GetTermVectors(0).GetTerms("field").GetEnumerator();
            Assert.IsTrue(termsEnum.MoveNext());
            DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
            Assert.AreEqual(2, termsEnum.TotalTermFreq);

            Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            dpEnum.NextPosition();
            Assert.AreEqual(0, dpEnum.StartOffset);
            Assert.AreEqual(4, dpEnum.EndOffset);

            dpEnum.NextPosition();
            Assert.AreEqual(8, dpEnum.StartOffset);
            Assert.AreEqual(12, dpEnum.EndOffset);
            Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());

            r.Dispose();
            dir.Dispose();
        }

        // LUCENE-1448
        [Test]
        public virtual void TestEndOffsetPositionStopFilter()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.SIMPLE, true, MockTokenFilter.ENGLISH_STOPSET)));
            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorOffsets = true;
            Field f = NewField("field", "abcd the", customType);
            doc.Add(f);
            doc.Add(f);
            w.AddDocument(doc);
            w.Dispose();

            IndexReader r = DirectoryReader.Open(dir);
            TermsEnum termsEnum = r.GetTermVectors(0).GetTerms("field").GetEnumerator();
            Assert.IsTrue(termsEnum.MoveNext());
            DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
            Assert.AreEqual(2, termsEnum.TotalTermFreq);

            Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            dpEnum.NextPosition();
            Assert.AreEqual(0, dpEnum.StartOffset);
            Assert.AreEqual(4, dpEnum.EndOffset);

            dpEnum.NextPosition();
            Assert.AreEqual(9, dpEnum.StartOffset);
            Assert.AreEqual(13, dpEnum.EndOffset);
            Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());

            r.Dispose();
            dir.Dispose();
        }

        // LUCENE-1448
        [Test]
        public virtual void TestEndOffsetPositionStandard()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorOffsets = true;
            Field f = NewField("field", "abcd the  ", customType);
            Field f2 = NewField("field", "crunch man", customType);
            doc.Add(f);
            doc.Add(f2);
            w.AddDocument(doc);
            w.Dispose();

            IndexReader r = DirectoryReader.Open(dir);
            TermsEnum termsEnum = r.GetTermVectors(0).GetTerms("field").GetEnumerator();
            Assert.IsTrue(termsEnum.MoveNext());
            DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);

            Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            dpEnum.NextPosition();
            Assert.AreEqual(0, dpEnum.StartOffset);
            Assert.AreEqual(4, dpEnum.EndOffset);

            Assert.IsTrue(termsEnum.MoveNext());
            dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
            Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            dpEnum.NextPosition();
            Assert.AreEqual(11, dpEnum.StartOffset);
            Assert.AreEqual(17, dpEnum.EndOffset);

            Assert.IsTrue(termsEnum.MoveNext());
            dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
            Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            dpEnum.NextPosition();
            Assert.AreEqual(18, dpEnum.StartOffset);
            Assert.AreEqual(21, dpEnum.EndOffset);

            r.Dispose();
            dir.Dispose();
        }

        // LUCENE-1448
        [Test]
        public virtual void TestEndOffsetPositionStandardEmptyField()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorOffsets = true;
            Field f = NewField("field", "", customType);
            Field f2 = NewField("field", "crunch man", customType);
            doc.Add(f);
            doc.Add(f2);
            w.AddDocument(doc);
            w.Dispose();

            IndexReader r = DirectoryReader.Open(dir);
            TermsEnum termsEnum = r.GetTermVectors(0).GetTerms("field").GetEnumerator();
            Assert.IsTrue(termsEnum.MoveNext());
            DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);

            Assert.AreEqual(1, (int)termsEnum.TotalTermFreq);
            Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            dpEnum.NextPosition();
            Assert.AreEqual(1, dpEnum.StartOffset);
            Assert.AreEqual(7, dpEnum.EndOffset);

            Assert.IsTrue(termsEnum.MoveNext());
            dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
            Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            dpEnum.NextPosition();
            Assert.AreEqual(8, dpEnum.StartOffset);
            Assert.AreEqual(11, dpEnum.EndOffset);

            r.Dispose();
            dir.Dispose();
        }

        // LUCENE-1448
        [Test]
        public virtual void TestEndOffsetPositionStandardEmptyField2()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorOffsets = true;

            Field f = NewField("field", "abcd", customType);
            doc.Add(f);
            doc.Add(NewField("field", "", customType));

            Field f2 = NewField("field", "crunch", customType);
            doc.Add(f2);

            w.AddDocument(doc);
            w.Dispose();

            IndexReader r = DirectoryReader.Open(dir);
            TermsEnum termsEnum = r.GetTermVectors(0).GetTerms("field").GetEnumerator();
            Assert.IsTrue(termsEnum.MoveNext());
            DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);

            Assert.AreEqual(1, (int)termsEnum.TotalTermFreq);
            Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            dpEnum.NextPosition();
            Assert.AreEqual(0, dpEnum.StartOffset);
            Assert.AreEqual(4, dpEnum.EndOffset);

            Assert.IsTrue(termsEnum.MoveNext());
            dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
            Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            dpEnum.NextPosition();
            Assert.AreEqual(6, dpEnum.StartOffset);
            Assert.AreEqual(12, dpEnum.EndOffset);

            r.Dispose();
            dir.Dispose();
        }

        // LUCENE-1168
        [Test]
        public virtual void TestTermVectorCorruption()
        {
            // LUCENENET specific - log the current locking strategy used and HResult values
            // for assistance troubleshooting problems on Linux/macOS
            LogNativeFSFactoryDebugInfo();

            Directory dir = NewDirectory();
            for (int iter = 0; iter < 2; iter++)
            {
                IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(2).SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH)).SetMergeScheduler(new SerialMergeScheduler()).SetMergePolicy(new LogDocMergePolicy()));

                Document document = new Document();
                FieldType customType = new FieldType();
                customType.IsStored = true;

                Field storedField = NewField("stored", "stored", customType);
                document.Add(storedField);
                writer.AddDocument(document);
                writer.AddDocument(document);

                document = new Document();
                document.Add(storedField);
                FieldType customType2 = new FieldType(StringField.TYPE_NOT_STORED);
                customType2.StoreTermVectors = true;
                customType2.StoreTermVectorPositions = true;
                customType2.StoreTermVectorOffsets = true;
                Field termVectorField = NewField("termVector", "termVector", customType2);

                document.Add(termVectorField);
                writer.AddDocument(document);
                writer.ForceMerge(1);
                writer.Dispose();

                IndexReader reader = DirectoryReader.Open(dir);
                for (int i = 0; i < reader.NumDocs; i++)
                {
                    reader.Document(i);
                    reader.GetTermVectors(i);
                }
                reader.Dispose();

                writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(2).SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH)).SetMergeScheduler(new SerialMergeScheduler()).SetMergePolicy(new LogDocMergePolicy()));

                Directory[] indexDirs = new Directory[] { new MockDirectoryWrapper(Random, new RAMDirectory(dir, NewIOContext(Random))) };
                writer.AddIndexes(indexDirs);
                writer.ForceMerge(1);
                writer.Dispose();
            }
            dir.Dispose();
        }

        // LUCENE-1168
        [Test]
        public virtual void TestTermVectorCorruption2()
        {
            Directory dir = NewDirectory();
            for (int iter = 0; iter < 2; iter++)
            {
                IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(2).SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH)).SetMergeScheduler(new SerialMergeScheduler()).SetMergePolicy(new LogDocMergePolicy()));

                Document document = new Document();

                FieldType customType = new FieldType();
                customType.IsStored = true;

                Field storedField = NewField("stored", "stored", customType);
                document.Add(storedField);
                writer.AddDocument(document);
                writer.AddDocument(document);

                document = new Document();
                document.Add(storedField);
                FieldType customType2 = new FieldType(StringField.TYPE_NOT_STORED);
                customType2.StoreTermVectors = true;
                customType2.StoreTermVectorPositions = true;
                customType2.StoreTermVectorOffsets = true;
                Field termVectorField = NewField("termVector", "termVector", customType2);
                document.Add(termVectorField);
                writer.AddDocument(document);
                writer.ForceMerge(1);
                writer.Dispose();

                IndexReader reader = DirectoryReader.Open(dir);
                Assert.IsNull(reader.GetTermVectors(0));
                Assert.IsNull(reader.GetTermVectors(1));
                Assert.IsNotNull(reader.GetTermVectors(2));
                reader.Dispose();
            }
            dir.Dispose();
        }

        // LUCENE-1168
        [Test]
        public virtual void TestTermVectorCorruption3()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(2).SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH)).SetMergeScheduler(new SerialMergeScheduler()).SetMergePolicy(new LogDocMergePolicy()));

            Document document = new Document();
            FieldType customType = new FieldType();
            customType.IsStored = true;

            Field storedField = NewField("stored", "stored", customType);
            document.Add(storedField);
            FieldType customType2 = new FieldType(StringField.TYPE_NOT_STORED);
            customType2.StoreTermVectors = true;
            customType2.StoreTermVectorPositions = true;
            customType2.StoreTermVectorOffsets = true;
            Field termVectorField = NewField("termVector", "termVector", customType2);
            document.Add(termVectorField);
            for (int i = 0; i < 10; i++)
            {
                writer.AddDocument(document);
            }
            writer.Dispose();

            writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(2).SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH)).SetMergeScheduler(new SerialMergeScheduler()).SetMergePolicy(new LogDocMergePolicy()));
            for (int i = 0; i < 6; i++)
            {
                writer.AddDocument(document);
            }

            writer.ForceMerge(1);
            writer.Dispose();

            IndexReader reader = DirectoryReader.Open(dir);
            for (int i = 0; i < 10; i++)
            {
                reader.GetTermVectors(i);
                reader.Document(i);
            }
            reader.Dispose();
            dir.Dispose();
        }

        // LUCENE-1008
        [Test]
        public virtual void TestNoTermVectorAfterTermVector()
        {
            Directory dir = NewDirectory();
            IndexWriter iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document document = new Document();
            FieldType customType2 = new FieldType(StringField.TYPE_NOT_STORED);
            customType2.StoreTermVectors = true;
            customType2.StoreTermVectorPositions = true;
            customType2.StoreTermVectorOffsets = true;
            document.Add(NewField("tvtest", "a b c", customType2));
            iw.AddDocument(document);
            document = new Document();
            document.Add(NewTextField("tvtest", "x y z", Field.Store.NO));
            iw.AddDocument(document);
            // Make first segment
            iw.Commit();

            FieldType customType = new FieldType(StringField.TYPE_NOT_STORED);
            customType.StoreTermVectors = true;
            document.Add(NewField("tvtest", "a b c", customType));
            iw.AddDocument(document);
            // Make 2nd segment
            iw.Commit();

            iw.ForceMerge(1);
            iw.Dispose();
            dir.Dispose();
        }

        // LUCENE-1010
        [Test]
        public virtual void TestNoTermVectorAfterTermVectorMerge()
        {
            Directory dir = NewDirectory();
            IndexWriter iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document document = new Document();
            FieldType customType = new FieldType(StringField.TYPE_NOT_STORED);
            customType.StoreTermVectors = true;
            document.Add(NewField("tvtest", "a b c", customType));
            iw.AddDocument(document);
            iw.Commit();

            document = new Document();
            document.Add(NewTextField("tvtest", "x y z", Field.Store.NO));
            iw.AddDocument(document);
            // Make first segment
            iw.Commit();

            iw.ForceMerge(1);

            FieldType customType2 = new FieldType(StringField.TYPE_NOT_STORED);
            customType2.StoreTermVectors = true;
            document.Add(NewField("tvtest", "a b c", customType2));
            iw.AddDocument(document);
            // Make 2nd segment
            iw.Commit();
            iw.ForceMerge(1);

            iw.Dispose();
            dir.Dispose();
        }
    }
}