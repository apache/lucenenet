using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

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
    using TextField = TextField;

    /// <summary>
    /// Some tests for <seealso cref="ParallelAtomicReader"/>s with empty indexes
    /// </summary>
    [TestFixture]
    public class TestParallelReaderEmptyIndex : LuceneTestCase
    {
        /// <summary>
        /// Creates two empty indexes and wraps a ParallelReader around. Adding this
        /// reader to a new index should not throw any exception.
        /// </summary>
        [Test]
        public virtual void TestEmptyIndex()
        {
            Directory rd1 = NewDirectory();
            IndexWriter iw = new IndexWriter(rd1, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            iw.Dispose();
            // create a copy:
            Directory rd2 = NewDirectory(rd1);

            Directory rdOut = NewDirectory();

            IndexWriter iwOut = new IndexWriter(rdOut, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));

            ParallelAtomicReader apr = new ParallelAtomicReader(SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(rd1)), SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(rd2)));

            // When unpatched, Lucene crashes here with a NoSuchElementException (caused by ParallelTermEnum)
            iwOut.AddIndexes(apr);
            iwOut.ForceMerge(1);

            // 2nd try with a readerless parallel reader
            iwOut.AddIndexes(new ParallelAtomicReader());
            iwOut.ForceMerge(1);

            ParallelCompositeReader cpr = new ParallelCompositeReader(DirectoryReader.Open(rd1), DirectoryReader.Open(rd2));

            // When unpatched, Lucene crashes here with a NoSuchElementException (caused by ParallelTermEnum)
            iwOut.AddIndexes(cpr);
            iwOut.ForceMerge(1);

            // 2nd try with a readerless parallel reader
            iwOut.AddIndexes(new ParallelCompositeReader());
            iwOut.ForceMerge(1);

            iwOut.Dispose();
            rdOut.Dispose();
            rd1.Dispose();
            rd2.Dispose();
        }

        /// <summary>
        /// this method creates an empty index (numFields=0, numDocs=0) but is marked
        /// to have TermVectors. Adding this index to another index should not throw
        /// any exception.
        /// </summary>
        [Test]
        public virtual void TestEmptyIndexWithVectors()
        {
            Directory rd1 = NewDirectory();
            {
                if (Verbose)
                {
                    Console.WriteLine("\nTEST: make 1st writer");
                }
                IndexWriter iw = new IndexWriter(rd1, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
                Document doc = new Document();
                Field idField = NewTextField("id", "", Field.Store.NO);
                doc.Add(idField);
                FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
                customType.StoreTermVectors = true;
                doc.Add(NewField("test", "", customType));
                idField.SetStringValue("1");
                iw.AddDocument(doc);
                doc.Add(NewTextField("test", "", Field.Store.NO));
                idField.SetStringValue("2");
                iw.AddDocument(doc);
                iw.Dispose();

                IndexWriterConfig dontMergeConfig = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))).SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
                if (Verbose)
                {
                    Console.WriteLine("\nTEST: make 2nd writer");
                }
                IndexWriter writer = new IndexWriter(rd1, dontMergeConfig);

                writer.DeleteDocuments(new Term("id", "1"));
                writer.Dispose();
                IndexReader ir = DirectoryReader.Open(rd1);
                Assert.AreEqual(2, ir.MaxDoc);
                Assert.AreEqual(1, ir.NumDocs);
                ir.Dispose();

                iw = new IndexWriter(rd1, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND));
                iw.ForceMerge(1);
                iw.Dispose();
            }

            Directory rd2 = NewDirectory();
            {
                IndexWriter iw = new IndexWriter(rd2, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
                Document doc = new Document();
                iw.AddDocument(doc);
                iw.Dispose();
            }

            Directory rdOut = NewDirectory();

            IndexWriter iwOut = new IndexWriter(rdOut, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            DirectoryReader reader1, reader2;
            ParallelAtomicReader pr = new ParallelAtomicReader(SlowCompositeReaderWrapper.Wrap(reader1 = DirectoryReader.Open(rd1)), SlowCompositeReaderWrapper.Wrap(reader2 = DirectoryReader.Open(rd2)));

            // When unpatched, Lucene crashes here with an ArrayIndexOutOfBoundsException (caused by TermVectorsWriter)
            iwOut.AddIndexes(pr);

            // ParallelReader closes any IndexReader you added to it:
            pr.Dispose();

            // assert subreaders were closed
            Assert.AreEqual(0, reader1.RefCount);
            Assert.AreEqual(0, reader2.RefCount);

            rd1.Dispose();
            rd2.Dispose();

            iwOut.ForceMerge(1);
            iwOut.Dispose();

            rdOut.Dispose();
        }
    }
}