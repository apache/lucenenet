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

using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Util
{
    [TestFixture]
    public class TestFilterIterator : LuceneTestCase
    {
        private static readonly SortedSet<string> Set = new SortedSet<string>(Arrays.AsList("a", "b", "c"));

        private static void AssertNoMore<T1>(IEnumerator<T1> it)
        {
            Assert.IsFalse(it.MoveNext());
            Assert.IsFalse(it.MoveNext());
        }

        [Test]
        public virtual void TestEmpty()
        {
            IEnumerator<string> it = new FilterIteratorAnonymousInnerClassHelper(this, Set.GetEnumerator());
            AssertNoMore(it);
        }

        private class FilterIteratorAnonymousInnerClassHelper : FilterIterator<string>
        {
            private readonly TestFilterIterator OuterInstance;

            public FilterIteratorAnonymousInnerClassHelper(TestFilterIterator outerInstance, IEnumerator<string> iterator)
                : base(iterator)
            {
                this.OuterInstance = outerInstance;
            }

            protected override bool PredicateFunction(string s)
            {
                return false;
            }
        }

        [Test]
        public virtual void TestA1()
        {
            IEnumerator<string> it = new FilterIteratorAnonymousInnerClassHelper2(this, Set.GetEnumerator());
            Assert.IsTrue(it.MoveNext());
            Assert.AreEqual("a", it.Current);
            AssertNoMore(it);
        }

        private class FilterIteratorAnonymousInnerClassHelper2 : FilterIterator<string>
        {
            private readonly TestFilterIterator OuterInstance;

            public FilterIteratorAnonymousInnerClassHelper2(TestFilterIterator outerInstance, IEnumerator<string> iterator)
                : base(iterator)
            {
                this.OuterInstance = outerInstance;
            }

            protected override bool PredicateFunction(string s)
            {
                return "a".Equals(s);
            }
        }

        [Test]
        public virtual void TestA2()
        {
            IEnumerator<string> it = new FilterIteratorAnonymousInnerClassHelper3(this, Set.GetEnumerator());
            // this time without check: Assert.IsTrue(it.hasNext());
            it.MoveNext();
            Assert.AreEqual("a", it.Current);
            AssertNoMore(it);
        }

        private class FilterIteratorAnonymousInnerClassHelper3 : FilterIterator<string>
        {
            private readonly TestFilterIterator OuterInstance;

            public FilterIteratorAnonymousInnerClassHelper3(TestFilterIterator outerInstance, IEnumerator<string> iterator)
                : base(iterator)
            {
                this.OuterInstance = outerInstance;
            }

            protected override bool PredicateFunction(string s)
            {
                return "a".Equals(s);
            }
        }

        [Test]
        public virtual void TestB1()
        {
            IEnumerator<string> it = new FilterIteratorAnonymousInnerClassHelper4(this, Set.GetEnumerator());
            Assert.IsTrue(it.MoveNext());
            Assert.AreEqual("b", it.Current);
            AssertNoMore(it);
        }

        private class FilterIteratorAnonymousInnerClassHelper4 : FilterIterator<string>
        {
            private readonly TestFilterIterator OuterInstance;

            public FilterIteratorAnonymousInnerClassHelper4(TestFilterIterator outerInstance, IEnumerator<string> iterator)
                : base(iterator)
            {
                this.OuterInstance = outerInstance;
            }

            protected override bool PredicateFunction(string s)
            {
                return "b".Equals(s);
            }
        }

        [Test]
        public virtual void TestB2()
        {
            IEnumerator<string> it = new FilterIteratorAnonymousInnerClassHelper5(this, Set.GetEnumerator());
            // this time without check: Assert.IsTrue(it.hasNext());
            it.MoveNext();
            Assert.AreEqual("b", it.Current);
            AssertNoMore(it);
        }

        private class FilterIteratorAnonymousInnerClassHelper5 : FilterIterator<string>
        {
            private readonly TestFilterIterator OuterInstance;

            public FilterIteratorAnonymousInnerClassHelper5(TestFilterIterator outerInstance, IEnumerator<string> iterator)
                : base(iterator)
            {
                this.OuterInstance = outerInstance;
            }

            protected override bool PredicateFunction(string s)
            {
                return "b".Equals(s);
            }
        }

        [Test]
        public virtual void TestAll1()
        {
            IEnumerator<string> it = new FilterIteratorAnonymousInnerClassHelper6(this, Set.GetEnumerator());
            Assert.IsTrue(it.MoveNext());
            Assert.AreEqual("a", it.Current);
            Assert.IsTrue(it.MoveNext());
            Assert.AreEqual("b", it.Current);
            Assert.IsTrue(it.MoveNext());
            Assert.AreEqual("c", it.Current);
            AssertNoMore(it);
        }

        private class FilterIteratorAnonymousInnerClassHelper6 : FilterIterator<string>
        {
            private readonly TestFilterIterator OuterInstance;

            public FilterIteratorAnonymousInnerClassHelper6(TestFilterIterator outerInstance, IEnumerator<string> iterator)
                : base(iterator)
            {
                this.OuterInstance = outerInstance;
            }

            protected override bool PredicateFunction(string s)
            {
                return true;
            }
        }

        [Test]
        public virtual void TestAll2()
        {
            IEnumerator<string> it = new FilterIteratorAnonymousInnerClassHelper7(this, Set.GetEnumerator());
            it.MoveNext();
            Assert.AreEqual("a", it.Current);
            it.MoveNext();
            Assert.AreEqual("b", it.Current);
            it.MoveNext();
            Assert.AreEqual("c", it.Current);
            AssertNoMore(it);
        }

        private class FilterIteratorAnonymousInnerClassHelper7 : FilterIterator<string>
        {
            private readonly TestFilterIterator OuterInstance;

            public FilterIteratorAnonymousInnerClassHelper7(TestFilterIterator outerInstance, IEnumerator<string> iterator)
                : base(iterator)
            {
                this.OuterInstance = outerInstance;
            }

            protected override bool PredicateFunction(string s)
            {
                return true;
            }
        }

        [Test]
        public virtual void TestUnmodifiable()
        {
            IEnumerator<string> it = new FilterIteratorAnonymousInnerClassHelper8(this, Set.GetEnumerator());
            it.MoveNext();
            Assert.AreEqual("a", it.Current);
            try
            {
                it.Reset();
                Assert.Fail("Should throw UnsupportedOperationException");
            }
            catch (NotImplementedException)
            {
                // pass
            }
        }

        private class FilterIteratorAnonymousInnerClassHelper8 : FilterIterator<string>
        {
            private readonly TestFilterIterator OuterInstance;

            public FilterIteratorAnonymousInnerClassHelper8(TestFilterIterator outerInstance, IEnumerator<string> iterator)
                : base(iterator)
            {
                this.OuterInstance = outerInstance;
            }

            protected override bool PredicateFunction(string s)
            {
                return true;
            }
        }
    }
}