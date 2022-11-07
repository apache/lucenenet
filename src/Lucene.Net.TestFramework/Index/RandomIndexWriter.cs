using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Diagnostics;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using RandomizedTesting.Generators;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
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

    /// <summary>
    /// Silly class that randomizes the indexing experience.  EG
    /// it may swap in a different merge policy/scheduler; may
    /// commit periodically; may or may not forceMerge in the end,
    /// may flush by doc count instead of RAM, etc.
    /// </summary>
    public class RandomIndexWriter : IDisposable
    {
        public IndexWriter IndexWriter { get; set; } // LUCENENET: Renamed from w to IndexWriter to make it clear what this is.
        private readonly Random r;
        internal int docCount;
        internal int flushAt;
        private double flushAtFactor = 1.0;
        private bool getReaderCalled;
        private readonly Codec codec; // sugar

        public static IndexWriter MockIndexWriter(Directory dir, IndexWriterConfig conf, Random r)
        {
            // Randomly calls Thread.yield so we mixup thread scheduling
            Random random = new J2N.Randomizer(r.NextInt64());
            return MockIndexWriter(dir, conf, new TestPointAnonymousClass(random));
        }

        private sealed class TestPointAnonymousClass : ITestPoint
        {
            private readonly Random random;

            public TestPointAnonymousClass(Random random)
            {
                this.random = random;
            }

            public void Apply(string message)
            {
                if (random.Next(4) == 2)
                {
                    Thread.Yield();
                }
            }
        }

        public static IndexWriter MockIndexWriter(Directory dir, IndexWriterConfig conf, ITestPoint testPoint)
        {
            conf.SetInfoStream(new TestPointInfoStream(conf.InfoStream, testPoint));
            return new IndexWriter(dir, conf);
        }


        /// <summary>
        /// Create a <see cref="RandomIndexWriter"/> with a random config: Uses <see cref="LuceneTestCase.TEST_VERSION_CURRENT"/> and <see cref="MockAnalyzer"/>.
        /// </summary>
        public RandomIndexWriter(Random r, Directory dir)
            : this(r, dir, LuceneTestCase.NewIndexWriterConfig(r, LuceneTestCase.TEST_VERSION_CURRENT, new MockAnalyzer(r)))
        {
        }

        /// <summary>
        /// Create a <see cref="RandomIndexWriter"/> with a random config: Uses <see cref="LuceneTestCase.TEST_VERSION_CURRENT"/>.
        /// </summary>
        public RandomIndexWriter(Random r, Directory dir, Analyzer a)
            : this(r, dir, LuceneTestCase.NewIndexWriterConfig(r, LuceneTestCase.TEST_VERSION_CURRENT, a))
        {
        }

        /// <summary>
        /// Creates a <see cref="RandomIndexWriter"/> with a random config.
        /// </summary>
        public RandomIndexWriter(Random r, Directory dir, LuceneVersion v, Analyzer a)
            : this(r, dir, LuceneTestCase.NewIndexWriterConfig(r, v, a))
        {
        }

        /// <summary>
        /// Creates a <see cref="RandomIndexWriter"/> with the provided config </summary>
        public RandomIndexWriter(Random r, Directory dir, IndexWriterConfig c)
        {
            // TODO: this should be solved in a different way; Random should not be shared (!).
            this.r = new J2N.Randomizer(r.NextInt64());
            IndexWriter = MockIndexWriter(dir, c, r);
            flushAt = TestUtil.NextInt32(r, 10, 1000);
            codec = IndexWriter.Config.Codec;
            if (LuceneTestCase.Verbose)
            {
                Console.WriteLine("RIW dir=" + dir + " config=" + IndexWriter.Config);
                Console.WriteLine("codec default=" + codec.Name);
            }

            // Make sure we sometimes test indices that don't get
            // any forced merges:
            doRandomForceMerge = !(c.MergePolicy is NoMergePolicy) && r.NextBoolean();
        }

        /// <summary>
        /// Adds a Document. </summary>
        /// <seealso cref="IndexWriter.AddDocument(IEnumerable{IIndexableField})"/>
        public virtual void AddDocument(IEnumerable<IIndexableField> doc)
        {
            AddDocument(doc, IndexWriter.Analyzer);
        }

        public virtual void AddDocument(IEnumerable<IIndexableField> doc, Analyzer a)
        {
            if (r.Next(5) == 3)
            {
                // TODO: maybe, we should simply buffer up added docs
                // (but we need to clone them), and only when
                // getReader, commit, etc. are called, we do an
                // addDocuments?  Would be better testing.
                IndexWriter.AddDocuments(new EnumerableAnonymousClass<IIndexableField>(doc), a);
            }
            else
            {
                IndexWriter.AddDocument(doc, a);
            }

            MaybeCommit();
        }

        private sealed class EnumerableAnonymousClass<IndexableField> : IEnumerable<IEnumerable<IndexableField>>
        {
            private readonly IEnumerable<IndexableField> doc;

            public EnumerableAnonymousClass(IEnumerable<IndexableField> doc)
            {
                this.doc = doc;
            }

            public IEnumerator<IEnumerable<IndexableField>> GetEnumerator()
            {
                return new EnumeratorAnonymousClass(this);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private sealed class EnumeratorAnonymousClass : IEnumerator<IEnumerable<IndexableField>>
            {
                private readonly EnumerableAnonymousClass<IndexableField> outerInstance;

                public EnumeratorAnonymousClass(EnumerableAnonymousClass<IndexableField> outerInstance)
                {
                    this.outerInstance = outerInstance;
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
                    current = outerInstance.doc;
                    return true;
                }

                public IEnumerable<IndexableField> Current => current;

                object IEnumerator.Current => Current;

                public void Reset()
                    => throw new NotImplementedException();

                public void Dispose()
                {
                    // LUCENENET: Intentionally blank
                }
            }
        }

        private void MaybeCommit()
        {
            if (docCount++ == flushAt)
            {
                if (LuceneTestCase.Verbose)
                {
                    Console.WriteLine("RIW.add/updateDocument: now doing a commit at docCount=" + docCount);
                }
                IndexWriter.Commit();
                flushAt += TestUtil.NextInt32(r, (int)(flushAtFactor * 10), (int)(flushAtFactor * 1000));
                if (flushAtFactor < 2e6)
                {
                    // gradually but exponentially increase time b/w flushes
                    flushAtFactor *= 1.05;
                }
            }
        }

        public virtual void AddDocuments(IEnumerable<IEnumerable<IIndexableField>> docs)
        {
            IndexWriter.AddDocuments(docs);
            MaybeCommit();
        }

        public virtual void UpdateDocuments(Term delTerm, IEnumerable<IEnumerable<IIndexableField>> docs)
        {
            IndexWriter.UpdateDocuments(delTerm, docs);
            MaybeCommit();
        }

        /// <summary>
        /// Updates a document. </summary>
        /// <see cref="IndexWriter.UpdateDocument(Term, IEnumerable{IIndexableField})"/>
        public virtual void UpdateDocument(Term t, IEnumerable<IIndexableField> doc)
        {
            if (r.Next(5) == 3)
            {
                IndexWriter.UpdateDocuments(t, new EnumerableAnonymousClass2(doc));
            }
            else
            {
                IndexWriter.UpdateDocument(t, doc);
            }
            MaybeCommit();
        }

        private sealed class EnumerableAnonymousClass2 : IEnumerable<IEnumerable<IIndexableField>>
        {
            private readonly IEnumerable<IIndexableField> doc;

            public EnumerableAnonymousClass2(IEnumerable<IIndexableField> doc)
            {
                this.doc = doc;
            }

            public IEnumerator<IEnumerable<IIndexableField>> GetEnumerator()
            {
                return new EnumeratorAnonymousClass2(this);
            }

            IEnumerator IEnumerable.GetEnumerator() 
                => GetEnumerator();

            private sealed class EnumeratorAnonymousClass2 : IEnumerator<IEnumerable<IIndexableField>>
            {
                private readonly EnumerableAnonymousClass2 outerInstance;

                public EnumeratorAnonymousClass2(EnumerableAnonymousClass2 outerInstance)
                {
                    this.outerInstance = outerInstance;
                }

                internal bool done;
                private IEnumerable<IIndexableField> current;

                public bool MoveNext()
                {
                    if (done)
                    {
                        return false;
                    }

                    done = true;
                    current = outerInstance.doc;
                    return true;
                }

                public IEnumerable<IIndexableField> Current => current;

                object IEnumerator.Current => Current;

                public void Reset()
                    => throw new NotImplementedException();

                public void Dispose()
                {
                    // LUCENENET: Intentionally blank
                }
            }
        }

        public virtual void AddIndexes(params Directory[] dirs)
            => IndexWriter.AddIndexes(dirs);

        public virtual void AddIndexes(params IndexReader[] readers)
            => IndexWriter.AddIndexes(readers);

        public virtual void UpdateNumericDocValue(Term term, string field, long? value)
            => IndexWriter.UpdateNumericDocValue(term, field, value);

        public virtual void UpdateBinaryDocValue(Term term, string field, BytesRef value)
            => IndexWriter.UpdateBinaryDocValue(term, field, value);

        public virtual void DeleteDocuments(Term term)
            => IndexWriter.DeleteDocuments(term);

        public virtual void DeleteDocuments(Query q)
            => IndexWriter.DeleteDocuments(q);

        public virtual void Commit()
            => IndexWriter.Commit();

        public virtual int NumDocs
            => IndexWriter.NumDocs;

        public virtual int MaxDoc
            => IndexWriter.MaxDoc;

        public virtual void DeleteAll()
            => IndexWriter.DeleteAll();

        public virtual DirectoryReader GetReader()
            => GetReader(true);

        private bool doRandomForceMerge = true;
        private bool doRandomForceMergeAssert = true;

        public virtual void ForceMergeDeletes(bool doWait)
            => IndexWriter.ForceMergeDeletes(doWait);

        public virtual void ForceMergeDeletes()
            => IndexWriter.ForceMergeDeletes();

        public virtual bool DoRandomForceMerge
        {
            get => doRandomForceMerge; // LUCENENET specific - added getter (to follow MSDN property guidelines)
            set => doRandomForceMerge = value;
        }

        public virtual bool DoRandomForceMergeAssert
        {
            get => doRandomForceMergeAssert; // LUCENENET specific - added getter (to follow MSDN property guidelines)
            set => doRandomForceMergeAssert = value;
        }

#pragma warning disable IDE1006 // Naming Styles
        private void _DoRandomForceMerge() // LUCENENET specific - added leading underscore to keep this from colliding with the DoRandomForceMerge property
#pragma warning restore IDE1006 // Naming Styles
        {
            if (doRandomForceMerge)
            {
                int segCount = IndexWriter.SegmentCount;
                if (r.NextBoolean() || segCount == 0)
                {
                    // full forceMerge
                    if (LuceneTestCase.Verbose)
                    {
                        Console.WriteLine("RIW: doRandomForceMerge(1)");
                    }
                    IndexWriter.ForceMerge(1);
                }
                else
                {
                    // partial forceMerge
                    int limit = TestUtil.NextInt32(r, 1, segCount);
                    if (LuceneTestCase.Verbose)
                    {
                        Console.WriteLine("RIW: doRandomForceMerge(" + limit + ")");
                    }
                    IndexWriter.ForceMerge(limit);
                    if (Debugging.AssertsEnabled) Debugging.Assert(!doRandomForceMergeAssert || IndexWriter.SegmentCount <= limit,"limit={0} actual={1}", limit, IndexWriter.SegmentCount);
                }
            }
        }

        public virtual DirectoryReader GetReader(bool applyDeletions)
        {
            getReaderCalled = true;
            if (r.Next(20) == 2)
            {
                _DoRandomForceMerge();
            }
            // If we are writing with PreFlexRW, force a full
            // IndexReader.open so terms are sorted in codepoint
            // order during searching:
            if (!applyDeletions || !codec.Name.Equals("Lucene3x", StringComparison.Ordinal) && r.NextBoolean())
            {
                if (LuceneTestCase.Verbose)
                {
                    Console.WriteLine("RIW.getReader: use NRT reader");
                }
                if (r.Next(5) == 1)
                {
                    IndexWriter.Commit();
                }
                return IndexWriter.GetReader(applyDeletions);
            }
            else
            {
                if (LuceneTestCase.Verbose)
                {
                    Console.WriteLine("RIW.getReader: open new reader");
                }
                IndexWriter.Commit();
                if (r.NextBoolean())
                {
                    return DirectoryReader.Open(IndexWriter.Directory, TestUtil.NextInt32(r, 1, 10));
                }
                else
                {
                    return IndexWriter.GetReader(applyDeletions);
                }
            }
        }

        // LUCENENET specific: Implemented dispose pattern

        /// <summary>
        /// Dispose this writer. </summary>
        /// <seealso cref="IndexWriter.Dispose()"/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose this writer. </summary>
        /// <seealso cref="IndexWriter.Dispose()"/>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // if someone isn't using getReader() API, we want to be sure to
                // forceMerge since presumably they might open a reader on the dir.
                if (getReaderCalled == false && r.Next(8) == 2)
                {
                    _DoRandomForceMerge();
                }
                IndexWriter.Dispose();
            }
        }

        /// <summary>
        /// Forces a forceMerge.
        /// <para/>
        /// NOTE: this should be avoided in tests unless absolutely necessary,
        /// as it will result in less test coverage. </summary>
        /// <seealso cref="IndexWriter.ForceMerge(int)"/>
        public virtual void ForceMerge(int maxSegmentCount)
        {
            IndexWriter.ForceMerge(maxSegmentCount);
        }

        // LUCENENET specific - de-nested TestPointInfoStream

        // LUCENENET specific - de-nested ITestPoint
    }

    public sealed class TestPointInfoStream : InfoStream
    {
        private readonly InfoStream @delegate;
        private readonly ITestPoint testPoint;

        public TestPointInfoStream(InfoStream @delegate, ITestPoint testPoint)
        {
            this.@delegate = @delegate ?? new NullInfoStream();
            this.testPoint = testPoint;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                @delegate.Dispose();
            }
        }

        public override void Message(string component, string message)
        {
            if ("TP".Equals(component, StringComparison.Ordinal))
            {
                testPoint.Apply(message);
            }
            if (@delegate.IsEnabled(component))
            {
                @delegate.Message(component, message);
            }
        }

        public override bool IsEnabled(string component)
        {
            return "TP".Equals(component, StringComparison.Ordinal) || @delegate.IsEnabled(component);
        }
    }

    /// <summary>
    /// Simple interface that is executed for each <c>TP</c> <see cref="InfoStream"/> component
    /// message. See also <see cref="RandomIndexWriter.MockIndexWriter(Directory, IndexWriterConfig, ITestPoint)"/>.
    /// </summary>
    public interface ITestPoint
    {
        void Apply(string message);
    }
}