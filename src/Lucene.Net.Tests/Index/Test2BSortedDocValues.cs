using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using NUnit.Framework;
using System;
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

    using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Document = Documents.Document;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using SortedDocValuesField = SortedDocValuesField;

    [SuppressCodecs("Lucene3x")]
    [Ignore("very slow")]
    [TestFixture]
    public class Test2BSortedDocValues : LuceneTestCase
    {
        // indexes Integer.MAX_VALUE docs with a fixed binary field
        [Test]
        public virtual void TestFixedSorted()
        {
            BaseDirectoryWrapper dir = NewFSDirectory(CreateTempDir("2BFixedSorted"));
            if (dir is MockDirectoryWrapper)
            {
                ((MockDirectoryWrapper)dir).Throttling = Throttling.NEVER;
            }

            IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))
                                .SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH)
                                .SetRAMBufferSizeMB(256.0)
                                .SetMergeScheduler(new ConcurrentMergeScheduler())
                                .SetMergePolicy(NewLogMergePolicy(false, 10))
                                .SetOpenMode(OpenMode.CREATE));

            Document doc = new Document();
            var bytes = new byte[2];
            BytesRef data = new BytesRef(bytes);
            SortedDocValuesField dvField = new SortedDocValuesField("dv", data);
            doc.Add(dvField);

            for (int i = 0; i < int.MaxValue; i++)
            {
                bytes[0] = (byte)(i >> 8);
                bytes[1] = (byte)i;
                w.AddDocument(doc);
                if (i % 100000 == 0)
                {
                    Console.WriteLine("indexed: " + i);
                    Console.Out.Flush();
                }
            }

            w.ForceMerge(1);
            w.Dispose();

            Console.WriteLine("verifying...");
            Console.Out.Flush();

            DirectoryReader r = DirectoryReader.Open(dir);
            int expectedValue = 0;
            foreach (AtomicReaderContext context in r.Leaves)
            {
                AtomicReader reader = context.AtomicReader;
                BytesRef scratch = new BytesRef();
                BinaryDocValues dv = reader.GetSortedDocValues("dv");
                for (int i = 0; i < reader.MaxDoc; i++)
                {
                    bytes[0] = (byte)(expectedValue >> 8);
                    bytes[1] = (byte)expectedValue;
                    dv.Get(i, scratch);
                    Assert.AreEqual(data, scratch);
                    expectedValue++;
                }
            }

            r.Dispose();
            dir.Dispose();
        }

        // indexes Integer.MAX_VALUE docs with a fixed binary field
        [Test]
        public virtual void Test2BOrds()
        {
            BaseDirectoryWrapper dir = NewFSDirectory(CreateTempDir("2BOrds"));
            if (dir is MockDirectoryWrapper)
            {
                ((MockDirectoryWrapper)dir).Throttling = Throttling.NEVER;
            }

            var config = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))
                            .SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH)
                            .SetRAMBufferSizeMB(256.0)
                            .SetMergeScheduler(new ConcurrentMergeScheduler())
                            .SetMergePolicy(NewLogMergePolicy(false, 10))
                            .SetOpenMode(OpenMode.CREATE);
            IndexWriter w = new IndexWriter(dir, config);

            Document doc = new Document();
            var bytes = new byte[4];
            BytesRef data = new BytesRef(bytes);
            SortedDocValuesField dvField = new SortedDocValuesField("dv", data);
            doc.Add(dvField);

            for (int i = 0; i < int.MaxValue; i++)
            {
                bytes[0] = (byte)(i >> 24);
                bytes[1] = (byte)(i >> 16);
                bytes[2] = (byte)(i >> 8);
                bytes[3] = (byte)i;
                w.AddDocument(doc);
                if (i % 100000 == 0)
                {
                    Console.WriteLine("indexed: " + i);
                    Console.Out.Flush();
                }
            }

            w.ForceMerge(1);
            w.Dispose();

            Console.WriteLine("verifying...");
            Console.Out.Flush();

            DirectoryReader r = DirectoryReader.Open(dir);
            int counter = 0;
            foreach (AtomicReaderContext context in r.Leaves)
            {
                AtomicReader reader = context.AtomicReader;
                BytesRef scratch = new BytesRef();
                BinaryDocValues dv = reader.GetSortedDocValues("dv");
                for (int i = 0; i < reader.MaxDoc; i++)
                {
                    bytes[0] = (byte)(counter >> 24);
                    bytes[1] = (byte)(counter >> 16);
                    bytes[2] = (byte)(counter >> 8);
                    bytes[3] = (byte)counter;
                    counter++;
                    dv.Get(i, scratch);
                    Assert.AreEqual(data, scratch);
                }
            }

            r.Dispose();
            dir.Dispose();
        }

        // TODO: variable
    }
}