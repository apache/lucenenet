// Some tests adapted from Apache Harmony:
// https://github.com/apache/harmony/blob/02970cb7227a335edd2c8457ebdde0195a735733/classlib/modules/concurrent/src/test/java/ConcurrentHashMapTest.java
// https://github.com/apache/harmony/blob/02970cb7227a335edd2c8457ebdde0195a735733/classlib/modules/luni/src/test/api/common/org/apache/harmony/luni/tests/java/util/CollectionsTest.java

using J2N.Threading;
using Lucene.Net.Attributes;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
#nullable enable

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

    /// <summary>
    /// Tests for <see cref="ConcurrentHashSet{T}"/>.
    /// </summary>
    /// <remarks>
    /// Some tests (those not marked with <see cref="LuceneNetSpecificAttribute"/>)
    /// are adapted from Apache Harmony's ConcurrentHashMapTest class. This class
    /// tests ConcurrentHashMap, which is a dictionary, but the key behavior is
    /// similar to <see cref="ConcurrentHashSet{T}"/>.
    /// </remarks>
    public class TestConcurrentHashSet : JSR166TestCase
    {
        /// <summary>
        /// Used by <see cref="TestSynchronizedSet"/>
        /// </summary>
        // Ported from https://github.com/apache/harmony/blob/02970cb7227a335edd2c8457ebdde0195a735733/classlib/modules/luni/src/test/api/common/org/apache/harmony/luni/tests/java/util/CollectionsTest.java#L66-L76
        private static readonly object[] objArray = LoadObjArray();

        private static object[] LoadObjArray() // LUCENENET: avoid static constructors
        {
            object[] objArray = new object[1000];
            for (int i = 0; i < objArray.Length; i++)
            {
                objArray[i] = i;
            }
            return objArray;
        }

        /// <summary>
        /// Used by <see cref="TestSynchronizedSet"/>
        /// </summary>
        /// <remarks>
        /// Implements ThreadJob instead of Runnable, as that's what we have access to here.
        /// </remarks>
        // Ported from https://github.com/apache/harmony/blob/02970cb7227a335edd2c8457ebdde0195a735733/classlib/modules/luni/src/test/api/common/org/apache/harmony/luni/tests/java/util/CollectionsTest.java#L88-L159
        public class SynchCollectionChecker : ThreadJob
        {
            private ISet<object> col; // LUCENENET: was Collection, but we need to use ISet to access the IsSupersetOf method

            // private int colSize; // LUCENENET: converted to local variable

            private readonly int totalToRun;

            private readonly bool offset;

            private volatile int numberOfChecks /* = 0 */;

            private bool result = true;

            private readonly List<object> normalCountingList;

            private readonly List<object> offsetCountingList;

            public override void Run()
            {
                // ensure the list either contains the numbers from 0 to size-1 or
                // the numbers from size to 2*size -1
                while (numberOfChecks < totalToRun)
                {
                    UninterruptableMonitor.Enter(col);
                    try
                    {
                        if (!(col.Count == 0
                              || col.IsSupersetOf(normalCountingList)
                              || col.IsSupersetOf(offsetCountingList)))
                            result = false;
                        col.clear();
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(col);
                    }
                    if (offset)
                        col.UnionWith(offsetCountingList);
                    else
                        col.UnionWith(normalCountingList);
                    Interlocked.Increment(ref numberOfChecks); // was: numberOfChecks++;
                }
            }

            public SynchCollectionChecker(ISet<object> c, bool offset,
                int totalChecks)
            {
                // The collection to test, whether to offset the filler values by
                // size or not, and the min number of iterations to run
                totalToRun = totalChecks;
                col = c;
                int colSize = c.size();
                normalCountingList = new List<object>(colSize);
                offsetCountingList = new List<object>(colSize);
                for (int counter = 0; counter < colSize; counter++)
                    normalCountingList.Add(counter);
                for (int counter = 0; counter < colSize; counter++)
                    offsetCountingList.Add(counter + colSize);
                col.clear();
                if (offset)
                    col.UnionWith(offsetCountingList);
                else
                    col.UnionWith(normalCountingList);
                this.offset = offset; // LUCENENET - this line was missing from the original code
            }

            public bool Offset
                // answer true iff the list is filled with a counting sequence
                // starting at the value size to 2*size - 1
                // else the list with be filled starting at 0 to size - 1
                => offset;

            public bool Result
                // answer true iff no corruption has been found in the collection
                => result;

            public int NumberOfChecks
                // answer the number of checks that have been performed on the list
                => numberOfChecks;
        }

        [Test, LuceneNetSpecific]
        public void TestExceptWith()
        {
            // Numbers 0-8, 10-80, 99
            var initialSet = Enumerable.Range(1, 8)
                .Concat(Enumerable.Range(1, 8).Select(i => i * 10))
                .Append(99)
                .Append(0);

            var hashSet = new ConcurrentHashSet<int>(initialSet);

            Parallel.ForEach(Enumerable.Range(1, 8), i =>
            {
                // Remove i and i * 10, i.e. 1 and 10, 2 and 20, etc.
                var except = new[] { i, i * 10 };
                hashSet.ExceptWith(except);
            });

            Assert.AreEqual(2, hashSet.Count);
            Assert.IsTrue(hashSet.Contains(0));
            Assert.IsTrue(hashSet.Contains(99));
        }

        /// <summary>
        /// Create a set from Integers 1-5.
        /// </summary>
        /// <remarks>
        /// In the Harmony tests, this returns a ConcurrentHashMap,
        /// hence the name. Retaining the name, even though this is not a map,
        /// for consistency with the original tests.
        /// </remarks>
        private static ConcurrentHashSet<object> Map5()
        {
            ConcurrentHashSet<object> map = new ConcurrentHashSet<object>();
            assertTrue(map.IsEmpty);
            map.Add(one);
            map.Add(two);
            map.Add(three);
            map.Add(four);
            map.Add(five);
            assertFalse(map.IsEmpty);
            assertEquals(5, map.Count);
            return map;
        }

        /// <summary>
        /// clear removes all items
        /// </summary>
        [Test]
        public void TestClear()
        {
            ConcurrentHashSet<object> map = Map5();
            map.Clear();
            assertEquals(map.size(), 0);
        }

        /// <summary>
        /// Sets with same contents are equal
        /// </summary>
        [Test]
        [Ignore("ConcurrentHashSet does not currently implement structural Equals")]
        public void TestEquals()
        {
            ConcurrentHashSet<object> map1 = Map5();
            ConcurrentHashSet<object> map2 = Map5();
            assertEquals(map1, map2);
            assertEquals(map2, map1);
            map1.Clear();
            assertFalse(map1.Equals(map2));
            assertFalse(map2.Equals(map1));
        }

        /// <summary>
        /// contains returns true for contained value
        /// </summary>
        /// <remarks>
        /// This was <c>testContainsKey</c> in the Harmony tests,
        /// but we're using keys as values here.
        /// </remarks>
        [Test]
        public void TestContains()
        {
            ConcurrentHashSet<object> map = Map5();
            assertTrue(map.Contains(one));
            assertFalse(map.Contains(zero));
        }

        /// <summary>
        /// enumeration returns an enumeration containing the correct
        /// elements
        /// </summary>
        [Test]
        public void TestEnumeration()
        {
            ConcurrentHashSet<object> map = Map5();
            using IEnumerator<object> e = map.GetEnumerator();
            int count = 0;
            while (e.MoveNext())
            {
                count++;
                Assert.IsNotNull(e.Current); // LUCENENET specific - original test did not have an assert here
            }

            assertEquals(5, count);
        }

        // LUCENENET - omitted testGet because it is not applicable to a set

        /// <summary>
        /// IsEmpty is true of empty map and false for non-empty
        /// </summary>
        [Test]
        public void TestIsEmpty()
        {
            ConcurrentHashSet<object> empty = new ConcurrentHashSet<object>();
            ConcurrentHashSet<object> map = Map5();
            assertTrue(empty.IsEmpty);
            assertFalse(map.IsEmpty);
        }

        // LUCENENET - omitted testKeys, testKeySet, testKeySetToArray, testValuesToArray, testEntrySetToArray,
        // testValues, testEntrySet

        /// <summary>
        /// UnionAll adds all elements from the given set
        /// </summary>
        /// <remarks>
        /// This was adapted from testPutAll in the Harmony tests.
        /// </remarks>
        [Test]
        public void TestUnionWith()
        {
            ConcurrentHashSet<object> empty = new ConcurrentHashSet<object>();
            ConcurrentHashSet<object> map = Map5();
            empty.UnionWith(map);
            assertEquals(5, empty.size());
            assertTrue(empty.Contains(one));
            assertTrue(empty.Contains(two));
            assertTrue(empty.Contains(three));
            assertTrue(empty.Contains(four));
            assertTrue(empty.Contains(five));
        }

        // LUCENENET - omitted testPutIfAbsent, testPutIfAbsent2, testReplace, testReplace2, testReplaceValue, testReplaceValue2

        /// <summary>
        /// remove removes the correct value from the set
        /// </summary>
        [Test]
        public void TestRemove()
        {
            ConcurrentHashSet<object> map = Map5();
            map.remove(five);
            assertEquals(4, map.size());
            assertFalse(map.Contains(five));
        }

        /// <summary>
        /// remove(value) removes only if value present
        /// </summary>
        [Test]
        public void TestRemove2()
        {
            ConcurrentHashSet<object> map = Map5();
            map.remove(five);
            assertEquals(4, map.size());
            assertFalse(map.Contains(five));
            map.remove(zero); // LUCENENET specific - modified, zero is not in the set
            assertEquals(4, map.Count);
            assertFalse(map.Contains(zero)); // LUCENENET specific - ensure zero was not added
        }

        /// <summary>
        /// size returns the correct values
        /// </summary>
        [Test]
        public void TestCount()
        {
            ConcurrentHashSet<object> map = Map5();
            // ReSharper disable once CollectionNeverUpdated.Local - indeed, that's what we're testing here
            ConcurrentHashSet<object> empty = new ConcurrentHashSet<object>();
            assertEquals(0, empty.Count);
            assertEquals(5, map.Count);
        }

        // LUCENENET - testToString omitted, could use Collections.ToString instead

        /// <summary>
        /// Cannot create with negative capacity
        /// </summary>
        [Test]
        public void TestConstructor1() {
            try
            {
                _ = new ConcurrentHashSet<object>(8, -1);
                shouldThrow();
            }
            catch (ArgumentOutOfRangeException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException
            {
            }
        }

        /// <summary>
        /// Cannot create with negative concurrency level
        /// </summary>
        [Test]
        public void TestConstructor2()
        {
            try
            {
                _ = new ConcurrentHashSet<object>(-1, 100);
                shouldThrow();
            }
            catch (ArgumentOutOfRangeException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException
            {
            }
        }

        // LUCENENET - testConstructor3 omitted, we don't have a single-int-argument constructor
        // LUCENENET - omitted *_NullPointerException tests that are not relevant

        /// <summary>
        /// Contains(null) should not throw.
        /// </summary>
        /// <remarks>
        /// This differs from the ConcurrentHashMap tests in that we support null values.
        /// </remarks>
        [Test, LuceneNetSpecific]
        public void TestNullSupport()
        {
            ConcurrentHashSet<object?> c = new ConcurrentHashSet<object?>
            {
                null // implicitly calls Add which should not throw on null
            };
            Assert.IsTrue(c.Contains(null));
            c.Add(null); // should keep set the same
            Assert.IsTrue(c.Contains(null));
            Assert.AreEqual(1, c.Count);
            Assert.IsTrue(c.TryRemove(null));
            Assert.IsFalse(c.Contains(null));
            Assert.AreEqual(0, c.Count);
        }

        // LUCENENET - omitted testSerialization due to lack of serialization support
        // LUCENENET - omitted testSetValueWriteThrough as that is not applicable to a set

        // Ported from https://github.com/apache/harmony/blob/02970cb7227a335edd2c8457ebdde0195a735733/classlib/modules/luni/src/test/api/common/org/apache/harmony/luni/tests/java/util/CollectionsTest.java#L1474-L1532
        /// <summary>
        /// Apache Harmony test for java.util.Collections#synchronizedSet(java.util.Set), adapted for <see cref="ConcurrentHashSet{T}"/>.
        /// </summary>
        [Test]
        public void TestSynchronizedSet()
        {
            HashSet<object> smallSet = new HashSet<object>();
            for (int i = 0; i < 50; i++)
            {
                smallSet.Add(objArray[i]);
            }

            const int numberOfLoops = 200;
            ConcurrentHashSet<object> synchSet = new ConcurrentHashSet<object>(smallSet); // was: Collections.synchronizedSet(smallSet);
            // Replacing the previous line with the line below should cause the test
            // to fail--the set below isn't synchronized
            // ISet<object> synchSet = smallSet;

            SynchCollectionChecker normalSynchChecker = new SynchCollectionChecker(
                synchSet, false, numberOfLoops);
            SynchCollectionChecker offsetSynchChecker = new SynchCollectionChecker(
                synchSet, true, numberOfLoops);
            ThreadJob normalThread = normalSynchChecker;
            ThreadJob offsetThread = offsetSynchChecker;
            normalThread.Start();
            offsetThread.Start();
            while ((normalSynchChecker.NumberOfChecks < numberOfLoops)
                   || (offsetSynchChecker.NumberOfChecks < numberOfLoops))
            {
                try
                {
                    Thread.Sleep(10);
                }
                catch (Exception e) when (e.IsInterruptedException())
                {
                    //Expected
                }
            }
            assertTrue("Returned set corrupted by multiple thread access",
                normalSynchChecker.Result
                && offsetSynchChecker.Result);
            try
            {
                normalThread.Join(5000);
                offsetThread.Join(5000);
            }
            catch (Exception e) when (e.IsInterruptedException())
            {
                fail("join() interrupted");
            }

            ISet<object?> mySet = new ConcurrentHashSet<object?>(smallSet); // was: Collections.synchronizedSet(smallSet);
            mySet.Add(null);
            assertTrue("Trying to use nulls in list failed", mySet.Contains(null));

            smallSet = new HashSet<object>();
            for (int i = 0; i < 100; i++)
            {
                smallSet.Add(objArray[i]);
            }
            new Support_SetTest(new ConcurrentHashSet<int>(smallSet.Cast<int>())) // LUCENENET: add cast to int
                .RunTest();

            //Test self reference
            mySet = new ConcurrentHashSet<object?>(smallSet); // was: Collections.synchronizedSet(smallSet);
            mySet.Add(mySet); // LUCENENET specific - references are not the same when wrapping via constructor, so adding mySet instead of smallSet
            assertTrue("should contain self ref", Collections.ToString(mySet).IndexOf("(this", StringComparison.Ordinal) > -1);
        }
    }
}
