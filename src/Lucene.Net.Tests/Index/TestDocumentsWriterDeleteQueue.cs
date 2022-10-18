using J2N.Threading;
using J2N.Threading.Atomic;
using Lucene.Net.Search;
using Lucene.Net.Support.Threading;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Assert = Lucene.Net.TestFramework.Assert;
using JCG = J2N.Collections.Generic;

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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using DeleteSlice = Lucene.Net.Index.DocumentsWriterDeleteQueue.DeleteSlice;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using TermQuery = Lucene.Net.Search.TermQuery;

    /// <summary>
    /// Unit test for <seealso cref="DocumentsWriterDeleteQueue"/>
    /// </summary>
    [TestFixture]
    public class TestDocumentsWriterDeleteQueue : LuceneTestCase
    {
        [Test]
        public virtual void TestUpdateDelteSlices()
        {
            DocumentsWriterDeleteQueue queue = new DocumentsWriterDeleteQueue();
            int size = 200 + Random.Next(500) * RandomMultiplier;
            int[] ids = new int[size];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = Random.Next();
            }
            DeleteSlice slice1 = queue.NewSlice();
            DeleteSlice slice2 = queue.NewSlice();
            BufferedUpdates bd1 = new BufferedUpdates();
            BufferedUpdates bd2 = new BufferedUpdates();
            int last1 = 0;
            int last2 = 0;
            ISet<Term> uniqueValues = new JCG.HashSet<Term>();
            for (int j = 0; j < ids.Length; j++)
            {
                int i = ids[j];
                // create an array here since we compare identity below against tailItem
                Term[] term = new Term[] { new Term("id", i.ToString()) };
                uniqueValues.Add(term[0]);
                queue.AddDelete(term);
                if (Random.Next(20) == 0 || j == ids.Length - 1)
                {
                    queue.UpdateSlice(slice1);
                    Assert.IsTrue(slice1.IsTailItem(term));
                    slice1.Apply(bd1, j);
                    AssertAllBetween(last1, j, bd1, ids);
                    last1 = j + 1;
                }
                if (Random.Next(10) == 5 || j == ids.Length - 1)
                {
                    queue.UpdateSlice(slice2);
                    Assert.IsTrue(slice2.IsTailItem(term));
                    slice2.Apply(bd2, j);
                    AssertAllBetween(last2, j, bd2, ids);
                    last2 = j + 1;
                }
                Assert.AreEqual(j + 1, queue.NumGlobalTermDeletes);
            }
            assertEquals(uniqueValues, bd1.terms.Keys);
            assertEquals(uniqueValues, bd2.terms.Keys);
            var frozenSet = new JCG.HashSet<Term>();
            foreach (Term t in queue.FreezeGlobalBuffer(null).GetTermsEnumerable())
            {
                BytesRef bytesRef = new BytesRef();
                bytesRef.CopyBytes(t.Bytes);
                frozenSet.Add(new Term(t.Field, bytesRef));
            }
            assertEquals(uniqueValues, frozenSet);
            assertEquals("num deletes must be 0 after freeze", 0, queue.NumGlobalTermDeletes);
        }

        // LUCENENET specific: Since the keys of a dictionary do not implement ISet<T>, we
        // re-implement the comparison rather than reallocate the entire collection
        internal void assertEquals(ISet<Term> expected, ICollection<Term> uniqueActual)
        {
            if (!SetEqualsCollection(expected, uniqueActual))
                fail();
        }

        private bool SetEqualsCollection(ISet<Term> setA, ICollection<Term> setB)
        {
            if (ReferenceEquals(setA, setB))
                return true;

            if (setA is null)
                return setB is null;
            else if (setB is null)
                return false;

            if (setA.Count != setB.Count)
                return false;

            // same operation as containsAll()
            foreach (var eB in setB)
            {
                bool contains = false;
                foreach (var eA in setA)
                {
                    if (eA.Equals(eB))
                    {
                        contains = true;
                        break;
                    }
                }
                if (!contains)
                    return false;
            }

            return true;
        }

        private void AssertAllBetween(int start, int end, BufferedUpdates deletes, int[] ids)
        {
            for (int i = start; i <= end; i++)
            {
                Assert.AreEqual(Convert.ToInt32(end), deletes.terms[new Term("id", ids[i].ToString())]);
            }
        }

        [Test]
        public virtual void TestClear()
        {
            DocumentsWriterDeleteQueue queue = new DocumentsWriterDeleteQueue();
            Assert.IsFalse(queue.AnyChanges());
            queue.Clear();
            Assert.IsFalse(queue.AnyChanges());
            int size = 200 + Random.Next(500) * RandomMultiplier;
            int termsSinceFreeze = 0;
            int queriesSinceFreeze = 0;
            for (int i = 0; i < size; i++)
            {
                Term term = new Term("id", "" + i);
                if (Random.Next(10) == 0)
                {
                    queue.AddDelete(new TermQuery(term));
                    queriesSinceFreeze++;
                }
                else
                {
                    queue.AddDelete(term);
                    termsSinceFreeze++;
                }
                Assert.IsTrue(queue.AnyChanges());
                if (Random.Next(10) == 0)
                {
                    queue.Clear();
                    queue.TryApplyGlobalSlice();
                    Assert.IsFalse(queue.AnyChanges());
                }
            }
        }

        [Test]
        public virtual void TestAnyChanges()
        {
            DocumentsWriterDeleteQueue queue = new DocumentsWriterDeleteQueue();
            int size = 200 + Random.Next(500) * RandomMultiplier;
            int termsSinceFreeze = 0;
            int queriesSinceFreeze = 0;
            for (int i = 0; i < size; i++)
            {
                Term term = new Term("id", "" + i);
                if (Random.Next(10) == 0)
                {
                    queue.AddDelete(new TermQuery(term));
                    queriesSinceFreeze++;
                }
                else
                {
                    queue.AddDelete(term);
                    termsSinceFreeze++;
                }
                Assert.IsTrue(queue.AnyChanges());
                if (Random.Next(5) == 0)
                {
                    FrozenBufferedUpdates freezeGlobalBuffer = queue.FreezeGlobalBuffer(null);
                    Assert.AreEqual(termsSinceFreeze, freezeGlobalBuffer.termCount);
                    Assert.AreEqual(queriesSinceFreeze, ((Query[])freezeGlobalBuffer.queries.Clone()).Length);
                    queriesSinceFreeze = 0;
                    termsSinceFreeze = 0;
                    Assert.IsFalse(queue.AnyChanges());
                }
            }
        }

        [Test]
        public virtual void TestPartiallyAppliedGlobalSlice()
        {
            DocumentsWriterDeleteQueue queue = new DocumentsWriterDeleteQueue();
            System.Reflection.FieldInfo field = typeof(DocumentsWriterDeleteQueue).GetField("globalBufferLock", 
                BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);
            ReentrantLock @lock = (ReentrantLock)field.GetValue(queue);
            @lock.Lock();
            var t = new ThreadAnonymousClass(this, queue);
            t.Start();
            t.Join();
            @lock.Unlock();
            Assert.IsTrue(queue.AnyChanges(), "changes in del queue but not in slice yet");
            queue.TryApplyGlobalSlice();
            Assert.IsTrue(queue.AnyChanges(), "changes in global buffer");
            FrozenBufferedUpdates freezeGlobalBuffer = queue.FreezeGlobalBuffer(null);
            Assert.IsTrue(freezeGlobalBuffer.Any());
            Assert.AreEqual(1, freezeGlobalBuffer.termCount);
            Assert.IsFalse(queue.AnyChanges(), "all changes applied");
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly TestDocumentsWriterDeleteQueue outerInstance;

            private DocumentsWriterDeleteQueue queue;

            public ThreadAnonymousClass(TestDocumentsWriterDeleteQueue outerInstance, DocumentsWriterDeleteQueue queue)
            {
                this.outerInstance = outerInstance;
                this.queue = queue;
            }

            public override void Run()
            {
                queue.AddDelete(new Term("foo", "bar"));
            }
        }

        [Test]
        [Slow]
        public virtual void TestStressDeleteQueue()
        {
            DocumentsWriterDeleteQueue queue = new DocumentsWriterDeleteQueue();
            ISet<Term> uniqueValues = new JCG.HashSet<Term>();
            int size = 10000 + Random.Next(500) * RandomMultiplier;
            int[] ids = new int[size];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = Random.Next();
                uniqueValues.Add(new Term("id", ids[i].ToString()));
            }
            CountdownEvent latch = new CountdownEvent(1);
            AtomicInt32 index = new AtomicInt32(0);
            int numThreads = 2 + Random.Next(5);
            UpdateThread[] threads = new UpdateThread[numThreads];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new UpdateThread(queue, index, ids, latch);
                threads[i].Start();
            }
            latch.Signal();
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Join();
            }

            foreach (UpdateThread updateThread in threads)
            {
                DeleteSlice slice = updateThread.slice;
                queue.UpdateSlice(slice);
                BufferedUpdates deletes = updateThread.deletes;
                slice.Apply(deletes, BufferedUpdates.MAX_INT32);
                assertEquals(uniqueValues, deletes.terms.Keys);
            }
            queue.TryApplyGlobalSlice();
            ISet<Term> frozenSet = new JCG.HashSet<Term>();
            foreach (Term t in queue.FreezeGlobalBuffer(null).GetTermsEnumerable())
            {
                BytesRef bytesRef = new BytesRef();
                bytesRef.CopyBytes(t.Bytes);
                frozenSet.Add(new Term(t.Field, bytesRef));
            }
            Assert.AreEqual(0, queue.NumGlobalTermDeletes, "num deletes must be 0 after freeze");
            Assert.AreEqual(uniqueValues.Count, frozenSet.Count);
            assertEquals(uniqueValues, frozenSet);
        }

        private class UpdateThread : ThreadJob
        {
            internal readonly DocumentsWriterDeleteQueue queue;
            internal readonly AtomicInt32 index;
            internal readonly int[] ids;
            internal readonly DeleteSlice slice;
            internal readonly BufferedUpdates deletes;
            internal readonly CountdownEvent latch;

            protected internal UpdateThread(DocumentsWriterDeleteQueue queue, AtomicInt32 index, int[] ids, CountdownEvent latch)
            {
                this.queue = queue;
                this.index = index;
                this.ids = ids;
                this.slice = queue.NewSlice();
                deletes = new BufferedUpdates();
                this.latch = latch;
            }

            public override void Run()
            {
                try
                {
                    latch.Wait();
                }
                catch (Exception ie) when (ie.IsInterruptedException())
                {
                    throw new Util.ThreadInterruptedException(ie);
                }

                int i = 0;
                while ((i = index.GetAndIncrement()) < ids.Length)
                {
                    Term term = new Term("id", ids[i].ToString());
                    queue.Add(term, slice);
                    Assert.IsTrue(slice.IsTailItem(term));
                    slice.Apply(deletes, BufferedUpdates.MAX_INT32);
                }
            }
        }
    }
}