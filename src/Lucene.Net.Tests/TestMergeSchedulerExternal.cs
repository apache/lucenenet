using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net
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

    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using Document = Documents.Document;
    using Field = Field;
    using ConcurrentMergeScheduler = Lucene.Net.Index.ConcurrentMergeScheduler;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using LogMergePolicy = Lucene.Net.Index.LogMergePolicy;
    using MergePolicy = Lucene.Net.Index.MergePolicy;
    using OneMerge = Lucene.Net.Index.MergePolicy.OneMerge;
    using MergeScheduler = Lucene.Net.Index.MergeScheduler;
    using MergeTrigger = Lucene.Net.Index.MergeTrigger;
    using Directory = Lucene.Net.Store.Directory;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

    /// <summary>
    /// Holds tests cases to verify external APIs are accessible
    /// while not being in Lucene.Net.Index package.
    /// </summary>
    public class TestMergeSchedulerExternal : LuceneTestCase
    {
        internal volatile bool mergeCalled;
        internal volatile bool mergeThreadCreated;
        internal volatile bool excCalled;

        private class MyMergeScheduler : ConcurrentMergeScheduler
        {
            private readonly TestMergeSchedulerExternal outerInstance;

            public MyMergeScheduler(TestMergeSchedulerExternal outerInstance)
            {
                this.outerInstance = outerInstance;
            }


            private class MyMergeThread : ConcurrentMergeScheduler.MergeThread
            {
                private readonly TestMergeSchedulerExternal.MyMergeScheduler outerInstance;

                public MyMergeThread(TestMergeSchedulerExternal.MyMergeScheduler outerInstance, IndexWriter writer, MergePolicy.OneMerge merge)
                    : base(outerInstance, writer, merge)
                {
                    this.outerInstance = outerInstance;
                    outerInstance.outerInstance.mergeThreadCreated = true;
                }
            }

            protected override MergeThread GetMergeThread(IndexWriter writer, MergePolicy.OneMerge merge)
            {
                MergeThread thread = new MyMergeThread(this, writer, merge);
                thread.SetThreadPriority((ThreadPriority)MergeThreadPriority);
                thread.IsBackground = (true);
                thread.Name = "MyMergeThread";
                return thread;
            }

            protected override void HandleMergeException(Exception t)
            {
                outerInstance.excCalled = true;
            }

            protected override void DoMerge(MergePolicy.OneMerge merge)
            {
                outerInstance.mergeCalled = true;
                base.DoMerge(merge);
            }
        }

        private class FailOnlyOnMerge : Failure
        {
            public override void Eval(MockDirectoryWrapper dir)
            {
                // LUCENENET specific: for these to work in release mode, we have added [MethodImpl(MethodImplOptions.NoInlining)]
                // to each possible target of the StackTraceHelper. If these change, so must the attribute on the target methods.
                if (StackTraceHelper.DoesStackTraceContainMethod("DoMerge"))
                {
                    throw new IOException("now failing during merge");
                }
            }
        }

        [Test]
        public void TestSubclassConcurrentMergeScheduler()
        {
            MockDirectoryWrapper dir = NewMockDirectory();
            dir.FailOn(new FailOnlyOnMerge());

            Document doc = new Document();
            Field idField = NewStringField("id", "", Field.Store.YES);
            doc.Add(idField);

            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergeScheduler(new MyMergeScheduler(this)).SetMaxBufferedDocs(2).SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH).SetMergePolicy(NewLogMergePolicy()));
            LogMergePolicy logMP = (LogMergePolicy)writer.Config.MergePolicy;
            logMP.MergeFactor = 10;
            for (int i = 0; i < 20; i++)
            {
                writer.AddDocument(doc);
            }

            ((MyMergeScheduler)writer.Config.MergeScheduler).Sync();
            writer.Dispose();

            Assert.IsTrue(mergeThreadCreated);
            Assert.IsTrue(mergeCalled);
            Assert.IsTrue(excCalled);
            dir.Dispose();
        }

        private class ReportingMergeScheduler : MergeScheduler
        {

            public override void Merge(IndexWriter writer, MergeTrigger trigger, bool newMergesFound)
            {
                MergePolicy.OneMerge merge = null;
                while ((merge = writer.NextMerge()) != null)
                {
                    if (Verbose)
                    {
                        Console.WriteLine("executing merge " + merge.SegString(writer.Directory));
                    }
                    writer.Merge(merge);
                }
            }

            protected override void Dispose(bool disposing)
            {
            }

        }

        [Test]
        public void TestCustomMergeScheduler()
        {
            // we don't really need to execute anything, just to make sure the custom MS
            // compiles. But ensure that it can be used as well, e.g., no other hidden
            // dependencies or something. Therefore, don't use any random API !
            Directory dir = new RAMDirectory();
            IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.SetMergeScheduler(new ReportingMergeScheduler());
            IndexWriter writer = new IndexWriter(dir, conf);
            writer.AddDocument(new Document());
            writer.Commit(); // trigger flush
            writer.AddDocument(new Document());
            writer.Commit(); // trigger flush
            writer.ForceMerge(1);
            writer.Dispose();
            dir.Dispose();
        }
    }
}