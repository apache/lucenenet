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
    using BinaryDocValuesField = BinaryDocValuesField;
    using ByteArrayDataInput = Lucene.Net.Store.ByteArrayDataInput;
    using ByteArrayDataOutput = Lucene.Net.Store.ByteArrayDataOutput;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Document = Documents.Document;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;

    [SuppressCodecs("Lucene3x")]
    [Ignore("takes ~ 45 minutes")]
    [TestFixture]
    public class Test2BBinaryDocValues : LuceneTestCase
    {
        // indexes Integer.MAX_VALUE docs with a fixed binary field
        [Test]
        public virtual void TestFixedBinary()
        {
            BaseDirectoryWrapper dir = NewFSDirectory(CreateTempDir("2BFixedBinary"));
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
            BinaryDocValuesField dvField = new BinaryDocValuesField("dv", data);
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
            int expectedValue = 0;
            foreach (AtomicReaderContext context in r.Leaves)
            {
                AtomicReader reader = context.AtomicReader;
                BytesRef scratch = new BytesRef();
                BinaryDocValues dv = reader.GetBinaryDocValues("dv");
                for (int i = 0; i < reader.MaxDoc; i++)
                {
                    bytes[0] = (byte)(expectedValue >> 24);
                    bytes[1] = (byte)(expectedValue >> 16);
                    bytes[2] = (byte)(expectedValue >> 8);
                    bytes[3] = (byte)expectedValue;
                    dv.Get(i, scratch);
                    Assert.AreEqual(data, scratch);
                    expectedValue++;
                }
            }

            r.Dispose();
            dir.Dispose();
        }

        // indexes Integer.MAX_VALUE docs with a variable binary field
        [Test]
        public virtual void TestVariableBinary()
        {
            BaseDirectoryWrapper dir = NewFSDirectory(CreateTempDir("2BVariableBinary"));
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
            ByteArrayDataOutput encoder = new ByteArrayDataOutput(bytes);
            BytesRef data = new BytesRef(bytes);
            BinaryDocValuesField dvField = new BinaryDocValuesField("dv", data);
            doc.Add(dvField);

            for (int i = 0; i < int.MaxValue; i++)
            {
                encoder.Reset(bytes);
                encoder.WriteVInt32(i % 65535); // 1, 2, or 3 bytes
                data.Length = encoder.Position;
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
            ByteArrayDataInput input = new ByteArrayDataInput();
            foreach (AtomicReaderContext context in r.Leaves)
            {
                AtomicReader reader = context.AtomicReader;
                BytesRef scratch = new BytesRef(bytes);
                BinaryDocValues dv = reader.GetBinaryDocValues("dv");
                for (int i = 0; i < reader.MaxDoc; i++)
                {
                    dv.Get(i, scratch);
                    input.Reset(scratch.Bytes, scratch.Offset, scratch.Length);
                    Assert.AreEqual(expectedValue % 65535, input.ReadVInt32());
                    Assert.IsTrue(input.Eof);
                    expectedValue++;
                }
            }

            r.Dispose();
            dir.Dispose();
        }
    }
}