/**
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

using NUnit.Framework;

using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using MockRAMDirectory = Lucene.Net.Store.MockRAMDirectory;
using Directory = Lucene.Net.Store.Directory;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using MergePolicy = Lucene.Net.Index.MergePolicy;
using ConcurrentMergeScheduler = Lucene.Net.Index.ConcurrentMergeScheduler;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;

namespace Lucene.Net.Test
{
    /**
     * Holds tests cases to verify external APIs are accessible
     * while not being in org.apache.lucene.index package.
     */
    [TestFixture]
    public class TestMergeSchedulerExternal : LuceneTestCase
    {

        internal volatile bool mergeCalled;
        internal volatile bool mergeThreadCreated;
        internal volatile bool excCalled;

        private class MyMergeException : System.Exception
        {
            Directory dir;
            public MyMergeException(System.Exception exc, Directory dir)
                : base(null, exc)
            {
                this.dir = dir;
            }
        }

        private class MyMergeScheduler : ConcurrentMergeScheduler
        {

            internal TestMergeSchedulerExternal enclosingInstance;

            public MyMergeScheduler(TestMergeSchedulerExternal enclosingInstance)
                : base()
            {
                this.enclosingInstance = enclosingInstance;
            }

            private class MyMergeThread : ConcurrentMergeScheduler.MergeThread
            {
                public MyMergeThread(ConcurrentMergeScheduler scheduler, IndexWriter writer, MergePolicy.OneMerge merge)
                    : base(scheduler, writer, merge)
                {
                    ((MyMergeScheduler)scheduler).enclosingInstance.mergeThreadCreated = true;
                }
            }

            override protected MergeThread GetMergeThread(IndexWriter writer, MergePolicy.OneMerge merge)
            {
                MergeThread thread = new MyMergeThread(this, writer, merge);
                thread.SetThreadPriority(GetMergeThreadPriority());
                thread.IsBackground = true;
                thread.Name = "MyMergeThread";
                return thread;
            }

            override protected void HandleMergeException(System.Exception t)
            {
                enclosingInstance.excCalled = true;
            }

            override protected void DoMerge(MergePolicy.OneMerge merge)
            {
                enclosingInstance.mergeCalled = true;
                base.DoMerge(merge);
            }
        }

        private class FailOnlyOnMerge : MockRAMDirectory.Failure
        {
            override public void Eval(MockRAMDirectory dir)
            {
                System.Diagnostics.StackFrame[] frames = new System.Diagnostics.StackTrace().GetFrames();
                for (int i = 0; i < frames.Length; i++)
                {
                    if ("DoMerge".Equals(frames[i].GetMethod().Name))
                        throw new System.IO.IOException("now failing during merge");
                }
            }
        }

        [Test]
        public void TestSubclassConcurrentMergeScheduler()
        {
            MockRAMDirectory dir = new MockRAMDirectory();
            dir.FailOn(new FailOnlyOnMerge());

            Document doc = new Document();
            Field idField = new Field("id", "", Field.Store.YES, Field.Index.NOT_ANALYZED);
            doc.Add(idField);

            IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true, IndexWriter.MaxFieldLength.LIMITED);
            MyMergeScheduler ms = new MyMergeScheduler(this);
            writer.SetMergeScheduler(ms);
            writer.SetMaxBufferedDocs(2);
            writer.SetRAMBufferSizeMB(IndexWriter.DISABLE_AUTO_FLUSH);
            for (int i = 0; i < 20; i++)
                writer.AddDocument(doc);

            ms.Sync();
            writer.Close();

            Assert.IsTrue(mergeThreadCreated);
            Assert.IsTrue(mergeCalled);
            Assert.IsTrue(excCalled);
            dir.Close();
            Assert.IsTrue(ConcurrentMergeScheduler.AnyUnhandledExceptions());
        }
    }
}
