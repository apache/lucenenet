/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using Lucene.Net.Analysis;
using Lucene.Net.Attributes;
using Lucene.Net.Codecs;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using Version = Lucene.Net.Util.LuceneVersion;

#pragma warning disable 612, 618
namespace Lucene.Net.Support.Threading
{
    [SuppressCodecs("Lucene3x")] // Suppress non-writable codecs
    [TestFixture]
    public class TestCloseableThreadLocal : LuceneTestCase
    {
        [Test, LuceneNetSpecific]
        public void TestMemLeakage()
        {
            DisposableThreadLocalProfiler.EnableIDisposableThreadLocalProfiler = true;

            int LoopCount = 100;
            Analyzer[] analyzers = new Analyzer[LoopCount];
            RAMDirectory[] dirs = new RAMDirectory[LoopCount];
            IndexWriter[] indexWriters = new IndexWriter[LoopCount];

            System.Threading.Tasks.Parallel.For(0, LoopCount, (i) =>
                                                                  {
                                                                      analyzers[i] = new Lucene.Net.Analysis.Standard.StandardAnalyzer(Version.LUCENE_CURRENT);
                                                                      dirs[i] = new RAMDirectory();
                                                                      var conf = new IndexWriterConfig(Version.LUCENE_CURRENT, analyzers[i]);
                                                                      indexWriters[i] = new IndexWriter(dirs[i], conf /*analyzers[i], true, IndexWriter.MaxFieldLength.UNLIMITED*/);
                                                                  });

            System.Threading.Tasks.Parallel.For(0, LoopCount, (i) =>
                                                                  {
                                                                      Document document = new Document();
                                                                      document.Add(new Field("field", "some test", Field.Store.NO, Field.Index.ANALYZED));
                                                                      indexWriters[i].AddDocument(document);
                                                                  });

            System.Threading.Tasks.Parallel.For(0, LoopCount, (i) =>
                                                                  {
                                                                      analyzers[i].Dispose();
                                                                      indexWriters[i].Dispose();
                                                                  });

            System.Threading.Tasks.Parallel.For(0, LoopCount, (i) =>
                                                                  {
                                                                      using (IndexReader reader = DirectoryReader.Open(dirs[i]))
                                                                      {
                                                                          IndexSearcher searcher = new IndexSearcher(reader);
                                                                          TopDocs d = searcher.Search(new TermQuery(new Term("field", "test")), 10);
                                                                      }
                                                                  });

            System.Threading.Tasks.Parallel.For(0, LoopCount, (i) => dirs[i].Dispose());

            GC.Collect(GC.MaxGeneration);
            GC.WaitForPendingFinalizers();

            int aliveObjects = 0;
            foreach (WeakReference w in DisposableThreadLocalProfiler.Instances)
            {
                object o = w.Target;
                if (o != null) aliveObjects++;
            }

            DisposableThreadLocalProfiler.EnableIDisposableThreadLocalProfiler = false;

            Assert.AreEqual(0, aliveObjects);
        }
    }
}

#if NET35

namespace System.Threading.Tasks
{
    public static class Parallel
    {
        public static void For(int start, int end, Action<int> loopAction)
        {
            for(int i = start; i < end; i++)
            {
                loopAction(i);
            }
        }
    }
}
#pragma warning restore 612, 618
#endif