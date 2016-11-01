using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using NUnit.Framework;
using System;

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
    using Document = Documents.Document;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using NumericDocValuesField = NumericDocValuesField;

    //@TimeoutSuite(millis = 80 * TimeUnits.HOUR) @Ignore("takes ~ 30 minutes") @SuppressCodecs("Lucene3x") public class Test2BNumericDocValues extends Lucene.Net.Util.LuceneTestCase
    [SuppressCodecs("Lucene3x")]
    [Ignore("takes ~ 30 minutes")]
    [TestFixture]
    public class Test2BNumericDocValues : LuceneTestCase
    {
        // indexes Integer.MAX_VALUE docs with an increasing dv field
        [Test, LongRunningTest]
        public virtual void TestNumerics([ValueSource(typeof(ConcurrentMergeSchedulers), "Values")]IConcurrentMergeScheduler scheduler)
        {
            BaseDirectoryWrapper dir = NewFSDirectory(CreateTempDir("2BNumerics"));
            if (dir is MockDirectoryWrapper)
            {
                ((MockDirectoryWrapper)dir).Throttling = MockDirectoryWrapper.Throttling_e.NEVER;
            }

            IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))
           .SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH).SetRAMBufferSizeMB(256.0).SetMergeScheduler(scheduler).SetMergePolicy(NewLogMergePolicy(false, 10)).SetOpenMode(IndexWriterConfig.OpenMode_e.CREATE));

            Document doc = new Document();
            NumericDocValuesField dvField = new NumericDocValuesField("dv", 0);
            doc.Add(dvField);

            for (int i = 0; i < int.MaxValue; i++)
            {
                dvField.LongValue = i;
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
            long expectedValue = 0;
            foreach (AtomicReaderContext context in r.Leaves)
            {
                AtomicReader reader = context.AtomicReader;
                NumericDocValues dv = reader.GetNumericDocValues("dv");
                for (int i = 0; i < reader.MaxDoc; i++)
                {
                    Assert.AreEqual(expectedValue, dv.Get(i));
                    expectedValue++;
                }
            }

            r.Dispose();
            dir.Dispose();
        }
    }
}