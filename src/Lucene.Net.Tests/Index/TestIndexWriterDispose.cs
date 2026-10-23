using Lucene.Net.Analysis;
using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
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

    /// <summary>
    /// Tests the <see cref="IndexWriter"/> dispose contract for subclasses
    /// that override <c>Dispose(bool disposing)</c>. See #1399.
    /// </summary>
    [LuceneNetSpecific]
    public class TestIndexWriterDispose : LuceneTestCase
    {
        private sealed class DisposeTrackingIndexWriter : IndexWriter
        {
            public int DisposingTrueCount { get; private set; }
            public int DisposingFalseCount { get; private set; }
            public bool WasOpenBeforeBaseDispose { get; private set; }
            public bool WasClosedAfterBaseDispose { get; private set; }
            public bool FinallyBlockRan { get; private set; }
            public Action BeforeBaseDispose { get; set; }

            public DisposeTrackingIndexWriter(Directory directory, IndexWriterConfig config)
                : base(directory, config)
            {
            }

            public void DisposeFromFinalizer() => Dispose(disposing: false);

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    DisposingTrueCount++;
                }
                else
                {
                    DisposingFalseCount++;
                }

                try
                {
                    if (disposing)
                    {
                        WasOpenBeforeBaseDispose = IsOpen();
                        BeforeBaseDispose?.Invoke();
                    }

                    base.Dispose(disposing);

                    if (disposing)
                    {
                        WasClosedAfterBaseDispose = !IsOpen();
                    }
                }
                finally
                {
                    FinallyBlockRan = true;
                }
            }

            private bool IsOpen()
            {
                try
                {
                    _ = Config;
                    return true;
                }
                catch (Exception e) when (e.IsAlreadyClosedException())
                {
                    return false;
                }
            }
        }

        private DisposeTrackingIndexWriter NewTrackingWriter(Directory dir)
        {
            return new DisposeTrackingIndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
        }

        private static Document NewDoc()
        {
            Document doc = new Document();
            doc.Add(new StringField("id", "1", Field.Store.NO));
            return doc;
        }

        private static int CommittedDocCount(Directory dir)
        {
            if (!DirectoryReader.IndexExists(dir))
            {
                return 0;
            }
            using DirectoryReader reader = DirectoryReader.Open(dir);
            return reader.NumDocs;
        }

        [Test]
        public void TestDisposeClosesIndexInsideOverriddenDispose()
        {
            using Directory dir = NewDirectory();
            DisposeTrackingIndexWriter writer = NewTrackingWriter(dir);
            writer.BeforeBaseDispose = () => writer.AddDocument(NewDoc());

            writer.Dispose();

            Assert.AreEqual(1, writer.DisposingTrueCount);
            Assert.IsTrue(writer.WasOpenBeforeBaseDispose, "writer should still be open before base.Dispose(true)");
            Assert.IsTrue(writer.WasClosedAfterBaseDispose, "writer should be closed after base.Dispose(true)");
            Assert.IsFalse(IndexWriter.IsLocked(dir));
            Assert.AreEqual(1, CommittedDocCount(dir), "changes made before base.Dispose(true) should be committed");
        }

        [Test]
        public void TestDisposeRunsSubclassFinallyWhenCloseThrows()
        {
            using Directory dir = NewDirectory();
            DisposeTrackingIndexWriter writer = NewTrackingWriter(dir);
            writer.AddDocument(NewDoc());
            writer.PrepareCommit();

            // Closing with a pending commit throws IllegalStateException and leaves the writer open
            Assert.Throws<InvalidOperationException>(() => writer.Dispose());
            Assert.IsTrue(writer.FinallyBlockRan);

            writer.Commit();
            writer.Dispose();
            Assert.AreEqual(1, CommittedDocCount(dir));
        }

        [Test]
        public void TestCloseDoesNotDisposeAndLaterDisposeIsSafe()
        {
            using Directory dir = NewDirectory();
            DisposeTrackingIndexWriter writer = NewTrackingWriter(dir);
            writer.AddDocument(NewDoc());

#pragma warning disable 612, 618
            writer.Close(false);
#pragma warning restore 612, 618

            Assert.AreEqual(0, writer.DisposingTrueCount, "Close(bool) should not invoke Dispose(bool)");
            Assert.IsFalse(IndexWriter.IsLocked(dir));
            Assert.AreEqual(1, CommittedDocCount(dir));

            writer.Dispose();

            Assert.AreEqual(1, writer.DisposingTrueCount);
            Assert.IsFalse(writer.WasOpenBeforeBaseDispose);
            Assert.AreEqual(1, CommittedDocCount(dir));
        }

        [Test]
        public void TestDisposeAfterRollbackDoesNotCommit()
        {
            using Directory dir = NewDirectory();
            DisposeTrackingIndexWriter writer = NewTrackingWriter(dir);
            writer.AddDocument(NewDoc());

            writer.Rollback();
            writer.Dispose();

            Assert.AreEqual(1, writer.DisposingTrueCount);
            Assert.AreEqual(0, CommittedDocCount(dir));
        }

        [Test]
        public void TestDisposeTwiceIsSafe()
        {
            using Directory dir = NewDirectory();
            DisposeTrackingIndexWriter writer = NewTrackingWriter(dir);
            writer.AddDocument(NewDoc());

            writer.Dispose();
            writer.Dispose();

            Assert.AreEqual(2, writer.DisposingTrueCount);
            Assert.AreEqual(1, CommittedDocCount(dir));
        }

        [Test]
        public void TestDisposeFromFinalizerDoesNotCloseIndex()
        {
            using Directory dir = NewDirectory();
            DisposeTrackingIndexWriter writer = NewTrackingWriter(dir);

            writer.DisposeFromFinalizer();

            Assert.AreEqual(1, writer.DisposingFalseCount);
            Assert.IsTrue(IndexWriter.IsLocked(dir), "Dispose(false) should not close the index");

            writer.Dispose();
            Assert.IsFalse(IndexWriter.IsLocked(dir));
        }
    }
}
