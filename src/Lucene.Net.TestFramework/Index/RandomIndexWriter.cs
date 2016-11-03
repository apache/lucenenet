using Lucene.Net.Randomized.Generators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Lucene.Net.Util;

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
    using Codec = Lucene.Net.Codecs.Codec;
    using Directory = Lucene.Net.Store.Directory;
    using InfoStream = Lucene.Net.Util.InfoStream;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using NullInfoStream = Lucene.Net.Util.NullInfoStream;
    using Query = Lucene.Net.Search.Query;
    using Similarity = Search.Similarities.Similarity;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// Silly class that randomizes the indexing experience.  EG
    ///  it may swap in a different merge policy/scheduler; may
    ///  commit periodically; may or may not forceMerge in the end,
    ///  may flush by doc count instead of RAM, etc.
    /// </summary>

    public class RandomIndexWriter : IDisposable
    {
        public IndexWriter w;
        private readonly Random r;
        internal int DocCount;
        internal int FlushAt;
        private double FlushAtFactor = 1.0;
        private bool GetReaderCalled;
        private readonly Codec Codec; // sugar

        public static IndexWriter MockIndexWriter(Directory dir, IndexWriterConfig conf, Random r)
        {
            // Randomly calls Thread.yield so we mixup thread scheduling
            Random random = new Random(r.Next());
            return MockIndexWriter(dir, conf, new TestPointAnonymousInnerClassHelper(random));
        }

        private class TestPointAnonymousInnerClassHelper : TestPoint
        {
            private Random Random;

            public TestPointAnonymousInnerClassHelper(Random random)
            {
                this.Random = random;
            }

            public virtual void Apply(string message)
            {
                if (Random.Next(4) == 2)
                {
                    System.Threading.Thread.Sleep(0);
                }
            }
        }

        public static IndexWriter MockIndexWriter(Directory dir, IndexWriterConfig conf, TestPoint testPoint)
        {
            conf.InfoStream = new TestPointInfoStream(conf.InfoStream, testPoint);
            return new IndexWriter(dir, conf);
        }

        /// <summary>
        /// create a RandomIndexWriter with a random config: Uses TEST_VERSION_CURRENT and MockAnalyzer
        ///
        /// LUCENENET specific
        /// Similarity and TimeZone parameters allow a RandomIndexWriter to be
        /// created without adding a dependency on 
        /// <see cref="LuceneTestCase.ClassEnv.Similarity"/> and
        /// <see cref="LuceneTestCase.ClassEnv.TimeZone"/>
        /// </summary>
        public RandomIndexWriter(Random r, Directory dir, Similarity similarity, TimeZone timezone)
            : this(r, dir, LuceneTestCase.NewIndexWriterConfig(r, LuceneTestCase.TEST_VERSION_CURRENT, new MockAnalyzer(r), similarity, timezone))
        {
        }

        /// <summary>
        /// create a RandomIndexWriter with a random config: Uses TEST_VERSION_CURRENT
        ///
        /// LUCENENET specific
        /// Similarity and TimeZone parameters allow a RandomIndexWriter to be
        /// created without adding a dependency on 
        /// <see cref="LuceneTestCase.ClassEnv.Similarity"/> and
        /// <see cref="LuceneTestCase.ClassEnv.TimeZone"/>
        /// </summary>
        public RandomIndexWriter(Random r, Directory dir, Analyzer a, Similarity similarity, TimeZone timezone)
            : this(r, dir, LuceneTestCase.NewIndexWriterConfig(r, LuceneTestCase.TEST_VERSION_CURRENT, a, similarity, timezone))
        {
        }

        /// <summary>
        /// Creates a RandomIndexWriter with a random config
        ///
        /// LUCENENET specific
        /// Similarity and TimeZone parameters allow a RandomIndexWriter to be
        /// created without adding a dependency on 
        /// <see cref="LuceneTestCase.ClassEnv.Similarity"/> and
        /// <see cref="LuceneTestCase.ClassEnv.TimeZone"/>
        /// </summary>
        public RandomIndexWriter(Random r, Directory dir, LuceneVersion v, Analyzer a, Similarity similarity, TimeZone timezone)
            : this(r, dir, LuceneTestCase.NewIndexWriterConfig(r, v, a, similarity, timezone))
        {
        }

        /// <summary>
        /// create a RandomIndexWriter with the provided config </summary>
        public RandomIndexWriter(Random r, Directory dir, IndexWriterConfig c)
        {
            // TODO: this should be solved in a different way; Random should not be shared (!).
            this.r = new Random(r.Next());
            w = MockIndexWriter(dir, c, r);
            FlushAt = TestUtil.NextInt(r, 10, 1000);
            Codec = w.Config.Codec;
            if (LuceneTestCase.VERBOSE)
            {
                Console.WriteLine("RIW dir=" + dir + " config=" + w.Config);
                Console.WriteLine("codec default=" + Codec.Name);
            }

            // Make sure we sometimes test indices that don't get
            // any forced merges:
            DoRandomForceMerge_Renamed = !(c.MergePolicy is NoMergePolicy) && r.NextBoolean();
        }

        /// <summary>
        /// Adds a Document. </summary>
        /// <seealso cref= IndexWriter#addDocument(Iterable) </seealso>
        public virtual void AddDocument(IEnumerable<IndexableField> doc)
        {
            AddDocument(doc, w.Analyzer);
        }

        public virtual void AddDocument(IEnumerable<IndexableField> doc, Analyzer a)
        {
            if (r.Next(5) == 3)
            {
                // TODO: maybe, we should simply buffer up added docs
                // (but we need to clone them), and only when
                // getReader, commit, etc. are called, we do an
                // addDocuments?  Would be better testing.
                w.AddDocuments(new IterableAnonymousInnerClassHelper<IndexableField>(this, doc), a);
            }
            else
            {
                w.AddDocument(doc, a);
            }

            MaybeCommit();
        }

        private class IterableAnonymousInnerClassHelper<IndexableField> : IEnumerable<IEnumerable<IndexableField>>
        {
            private readonly RandomIndexWriter OuterInstance;

            private IEnumerable<IndexableField> Doc;

            public IterableAnonymousInnerClassHelper(RandomIndexWriter outerInstance, IEnumerable<IndexableField> doc)
            {
                this.OuterInstance = outerInstance;
                this.Doc = doc;
            }

            public IEnumerator<IEnumerable<IndexableField>> GetEnumerator()
            {
                return new IteratorAnonymousInnerClassHelper(this);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private class IteratorAnonymousInnerClassHelper : IEnumerator<IEnumerable<IndexableField>>
            {
                private readonly IterableAnonymousInnerClassHelper<IndexableField> OuterInstance;

                public IteratorAnonymousInnerClassHelper(IterableAnonymousInnerClassHelper<IndexableField> outerInstance)
                {
                    this.OuterInstance = outerInstance;
                }

                internal bool done;
                private IEnumerable<IndexableField> current;

                public bool MoveNext()
                {
                    if (done)
                    {
                        return false;
                    }

                    done = true;
                    current = OuterInstance.Doc;
                    return true;
                }

                public IEnumerable<IndexableField> Current
                {
                    get { return current; }
                }

                object System.Collections.IEnumerator.Current
                {
                    get { return Current; }
                }

                public void Reset()
                {
                    throw new NotImplementedException();
                }

                public void Dispose()
                {
                }
            }
        }

        private void MaybeCommit()
        {
            if (DocCount++ == FlushAt)
            {
                if (LuceneTestCase.VERBOSE)
                {
                    Console.WriteLine("RIW.add/updateDocument: now doing a commit at docCount=" + DocCount);
                }
                w.Commit();
                FlushAt += TestUtil.NextInt(r, (int)(FlushAtFactor * 10), (int)(FlushAtFactor * 1000));
                if (FlushAtFactor < 2e6)
                {
                    // gradually but exponentially increase time b/w flushes
                    FlushAtFactor *= 1.05;
                }
            }
        }

        public virtual void AddDocuments(IEnumerable<IEnumerable<IndexableField>> docs)
        {
            w.AddDocuments(docs);
            MaybeCommit();
        }

        public virtual void UpdateDocuments(Term delTerm, IEnumerable<IEnumerable<IndexableField>> docs)
        {
            w.UpdateDocuments(delTerm, docs);
            MaybeCommit();
        }

        /// <summary>
        /// Updates a document. </summary>
        /// <seealso cref= IndexWriter#updateDocument(Term, Iterable) </seealso>
        public virtual void UpdateDocument(Term t, IEnumerable<IndexableField> doc)
        {
            if (r.Next(5) == 3)
            {
                w.UpdateDocuments(t, new IterableAnonymousInnerClassHelper2(this, doc));
            }
            else
            {
                w.UpdateDocument(t, doc);
            }
            MaybeCommit();
        }

        private class IterableAnonymousInnerClassHelper2 : IEnumerable<IEnumerable<IndexableField>>
        {
            private readonly RandomIndexWriter OuterInstance;

            private IEnumerable<IndexableField> Doc;

            public IterableAnonymousInnerClassHelper2(RandomIndexWriter outerInstance, IEnumerable<IndexableField> doc)
            {
                this.OuterInstance = outerInstance;
                this.Doc = doc;
            }

            public IEnumerator<IEnumerable<IndexableField>> GetEnumerator()
            {
                return new IteratorAnonymousInnerClassHelper2(this);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private class IteratorAnonymousInnerClassHelper2 : IEnumerator<IEnumerable<IndexableField>>
            {
                private readonly IterableAnonymousInnerClassHelper2 OuterInstance;

                public IteratorAnonymousInnerClassHelper2(IterableAnonymousInnerClassHelper2 outerInstance)
                {
                    this.OuterInstance = outerInstance;
                }

                internal bool done;
                private IEnumerable<IndexableField> current;

                public bool MoveNext()
                {
                    if (done)
                    {
                        return false;
                    }

                    done = true;
                    current = OuterInstance.Doc;
                    return true;
                }

                public IEnumerable<IndexableField> Current
                {
                    get { return current; }
                }

                object System.Collections.IEnumerator.Current
                {
                    get { return Current; }
                }

                public virtual void Reset()
                {
                    throw new NotImplementedException();
                }

                public void Dispose()
                {
                }
            }
        }

        public virtual void AddIndexes(params Directory[] dirs)
        {
            w.AddIndexes(dirs);
        }

        public virtual void AddIndexes(params IndexReader[] readers)
        {
            w.AddIndexes(readers);
        }

        public virtual void UpdateNumericDocValue(Term term, string field, long? value)
        {
            w.UpdateNumericDocValue(term, field, value);
        }

        public virtual void UpdateBinaryDocValue(Term term, string field, BytesRef value)
        {
            w.UpdateBinaryDocValue(term, field, value);
        }

        public virtual void DeleteDocuments(Term term)
        {
            w.DeleteDocuments(term);
        }

        public virtual void DeleteDocuments(Query q)
        {
            w.DeleteDocuments(q);
        }

        public virtual void Commit()
        {
            w.Commit();
        }

        public virtual int NumDocs()
        {
            return w.NumDocs();
        }

        public virtual int MaxDoc()
        {
            return w.MaxDoc;
        }

        public virtual void DeleteAll()
        {
            w.DeleteAll();
        }

        public virtual DirectoryReader Reader
        {
            get
            {
                return GetReader(true);
            }
        }

        private bool DoRandomForceMerge_Renamed = true;
        private bool DoRandomForceMergeAssert_Renamed = true;

        public virtual void ForceMergeDeletes(bool doWait)
        {
            w.ForceMergeDeletes(doWait);
        }

        public virtual void ForceMergeDeletes()
        {
            w.ForceMergeDeletes();
        }

        public virtual bool RandomForceMerge
        {
            set
            {
                DoRandomForceMerge_Renamed = value;
            }
        }

        public virtual bool DoRandomForceMergeAssert
        {
            set
            {
                DoRandomForceMergeAssert_Renamed = value;
            }
        }

        private void DoRandomForceMerge()
        {
            if (DoRandomForceMerge_Renamed)
            {
                int segCount = w.SegmentCount;
                if (r.NextBoolean() || segCount == 0)
                {
                    // full forceMerge
                    if (LuceneTestCase.VERBOSE)
                    {
                        Console.WriteLine("RIW: doRandomForceMerge(1)");
                    }
                    w.ForceMerge(1);
                }
                else
                {
                    // partial forceMerge
                    int limit = TestUtil.NextInt(r, 1, segCount);
                    if (LuceneTestCase.VERBOSE)
                    {
                        Console.WriteLine("RIW: doRandomForceMerge(" + limit + ")");
                    }
                    w.ForceMerge(limit);
                    Debug.Assert(!DoRandomForceMergeAssert_Renamed || w.SegmentCount <= limit, "limit=" + limit + " actual=" + w.SegmentCount);
                }
            }
        }

        public virtual DirectoryReader GetReader(bool applyDeletions)
        {
            GetReaderCalled = true;
            if (r.Next(20) == 2)
            {
                DoRandomForceMerge();
            }
            // If we are writing with PreFlexRW, force a full
            // IndexReader.open so terms are sorted in codepoint
            // order during searching:
            if (!applyDeletions || !Codec.Name.Equals("Lucene3x") && r.NextBoolean())
            {
                if (LuceneTestCase.VERBOSE)
                {
                    Console.WriteLine("RIW.getReader: use NRT reader");
                }
                if (r.Next(5) == 1)
                {
                    w.Commit();
                }
                return w.GetReader(applyDeletions);
            }
            else
            {
                if (LuceneTestCase.VERBOSE)
                {
                    Console.WriteLine("RIW.getReader: open new reader");
                }
                w.Commit();
                if (r.NextBoolean())
                {
                    return DirectoryReader.Open(w.Directory, TestUtil.NextInt(r, 1, 10));
                }
                else
                {
                    return w.GetReader(applyDeletions);
                }
            }
        }

        /// <summary>
        /// Close this writer. </summary>
        /// <seealso cref= IndexWriter#close() </seealso>
        public void Dispose()
        {
            // if someone isn't using getReader() API, we want to be sure to
            // forceMerge since presumably they might open a reader on the dir.
            if (GetReaderCalled == false && r.Next(8) == 2)
            {
                DoRandomForceMerge();
            }
            w.Dispose();
        }

        /// <summary>
        /// Forces a forceMerge.
        /// <p>
        /// NOTE: this should be avoided in tests unless absolutely necessary,
        /// as it will result in less test coverage. </summary>
        /// <seealso cref= IndexWriter#forceMerge(int) </seealso>
        public virtual void ForceMerge(int maxSegmentCount)
        {
            w.ForceMerge(maxSegmentCount);
        }

        public sealed class TestPointInfoStream : InfoStream
        {
            internal readonly InfoStream @delegate;
            internal readonly TestPoint TestPoint;

            public TestPointInfoStream(InfoStream @delegate, TestPoint testPoint)
            {
                this.@delegate = @delegate ?? new NullInfoStream();
                this.TestPoint = testPoint;
            }

            public override void Dispose()
            {
                @delegate.Dispose();
            }

            public override void Message(string component, string message)
            {
                if ("TP".Equals(component))
                {
                    TestPoint.Apply(message);
                }
                if (@delegate.IsEnabled(component))
                {
                    @delegate.Message(component, message);
                }
            }

            public override bool IsEnabled(string component)
            {
                return "TP".Equals(component) || @delegate.IsEnabled(component);
            }
        }

        /// <summary>
        /// Simple interface that is executed for each <tt>TP</tt> <seealso cref="InfoStream"/> component
        /// message. See also <seealso cref="RandomIndexWriter#mockIndexWriter(Directory, IndexWriterConfig, TestPoint)"/>
        /// </summary>
        public interface TestPoint
        {
            void Apply(string message);
        }
    }
}