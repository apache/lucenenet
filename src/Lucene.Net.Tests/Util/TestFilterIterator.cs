using NUnit.Framework;
using System;
using System.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Util
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

    [TestFixture]
    public class TestFilterIterator : LuceneTestCase
    {
        private static readonly ISet<string> set = new JCG.SortedSet<string>(StringComparer.Ordinal) { "a", "b", "c" };

        private static void AssertNoMore<T1>(IEnumerator<T1> it)
        {
            Assert.IsFalse(it.MoveNext());
            Assert.IsFalse(it.MoveNext());
        }

        [Test]
        public virtual void TestEmpty()
        {
            IEnumerator<string> it = new FilterEnumerator<string>(set.GetEnumerator(), (s) => false);
            AssertNoMore(it);
        }

        [Test]
        public virtual void TestA1()
        {
            IEnumerator<string> it = new FilterEnumerator<string>(set.GetEnumerator(), (s) => "a".Equals(s, StringComparison.Ordinal));
            Assert.IsTrue(it.MoveNext());
            Assert.AreEqual("a", it.Current);
            AssertNoMore(it);
        }

        [Test]
        public virtual void TestA2()
        {
            IEnumerator<string> it = new FilterEnumerator<string>(set.GetEnumerator(), (s) => "a".Equals(s, StringComparison.Ordinal));
            // this time without check: Assert.IsTrue(it.hasNext());
            it.MoveNext();
            Assert.AreEqual("a", it.Current);
            AssertNoMore(it);
        }

        [Test]
        public virtual void TestB1()
        {
            IEnumerator<string> it = new FilterEnumerator<string>(set.GetEnumerator(), (s) => "b".Equals(s, StringComparison.Ordinal));
            Assert.IsTrue(it.MoveNext());
            Assert.AreEqual("b", it.Current);
            AssertNoMore(it);
        }

        [Test]
        public virtual void TestB2()
        {
            IEnumerator<string> it = new FilterEnumerator<string>(set.GetEnumerator(), (s) => "b".Equals(s, StringComparison.Ordinal));
            // this time without check: Assert.IsTrue(it.hasNext());
            it.MoveNext();
            Assert.AreEqual("b", it.Current);
            AssertNoMore(it);
        }

        [Test]
        public virtual void TestAll1()
        {
            IEnumerator<string> it = new FilterEnumerator<string>(set.GetEnumerator(), (s) => true);
            Assert.IsTrue(it.MoveNext());
            Assert.AreEqual("a", it.Current);
            Assert.IsTrue(it.MoveNext());
            Assert.AreEqual("b", it.Current);
            Assert.IsTrue(it.MoveNext());
            Assert.AreEqual("c", it.Current);
            AssertNoMore(it);
        }

        [Test]
        public virtual void TestAll2()
        {
            IEnumerator<string> it = new FilterEnumerator<string>(set.GetEnumerator(), (s) => true);
            it.MoveNext();
            Assert.AreEqual("a", it.Current);
            it.MoveNext();
            Assert.AreEqual("b", it.Current);
            it.MoveNext();
            Assert.AreEqual("c", it.Current);
            AssertNoMore(it);
        }

        //// LUCENENET specific: .NET doesn't support Remove(), so this test doesn't apply
        //[Test]
        //public virtual void TestUnmodifiable()
        //{
        //    IEnumerator<string> it = new FilterEnumerator<string>(Set.GetEnumerator(), (s) => true);
        //    it.MoveNext();
        //    Assert.AreEqual("a", it.Current);
        //    try
        //    {

        //        it.Remove(); 
        //        Assert.Fail("Should throw UnsupportedOperationException");
        //    }
        //    catch (Exception oue) when (oue.IsUnsupportedOperationException())
        //    {
        //        // pass
        //    }
        //}




        [Test]
        [Obsolete("This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual void TestEmptyIterator()
        {
            IEnumerator<string> it = new FilterIteratorAnonymousClass(set.GetEnumerator());
            AssertNoMore(it);
        }

        [Obsolete("This class will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        private sealed class FilterIteratorAnonymousClass : FilterIterator<string>
        {
            public FilterIteratorAnonymousClass(IEnumerator<string> iterator)
                : base(iterator)
            {
            }

            protected override bool PredicateFunction(string s)
            {
                return false;
            }
        }

        [Test]
        [Obsolete("This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual void TestA1Iterator()
        {
            IEnumerator<string> it = new FilterIteratorAnonymousClass2(set.GetEnumerator());
            Assert.IsTrue(it.MoveNext());
            Assert.AreEqual("a", it.Current);
            AssertNoMore(it);
        }

        [Obsolete("This class will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        private sealed class FilterIteratorAnonymousClass2 : FilterIterator<string>
        {
            public FilterIteratorAnonymousClass2(IEnumerator<string> iterator)
                : base(iterator)
            {
            }

            protected override bool PredicateFunction(string s)
            {
                return "a".Equals(s, StringComparison.Ordinal);
            }
        }

        [Test]
        [Obsolete("This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual void TestA2Iterator()
        {
            IEnumerator<string> it = new FilterIteratorAnonymousClass3(set.GetEnumerator());
            // this time without check: Assert.IsTrue(it.hasNext());
            it.MoveNext();
            Assert.AreEqual("a", it.Current);
            AssertNoMore(it);
        }

        [Obsolete("This class will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        private sealed class FilterIteratorAnonymousClass3 : FilterIterator<string>
        {
            public FilterIteratorAnonymousClass3(IEnumerator<string> iterator)
                : base(iterator)
            {
            }

            protected override bool PredicateFunction(string s)
            {
                return "a".Equals(s, StringComparison.Ordinal);
            }
        }

        [Test]
        [Obsolete("This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual void TestB1Iterator()
        {
            IEnumerator<string> it = new FilterIteratorAnonymousClass4(set.GetEnumerator());
            Assert.IsTrue(it.MoveNext());
            Assert.AreEqual("b", it.Current);
            AssertNoMore(it);
        }

        [Obsolete("This class will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        private sealed class FilterIteratorAnonymousClass4 : FilterIterator<string>
        {
            public FilterIteratorAnonymousClass4(IEnumerator<string> iterator)
                : base(iterator)
            {
            }

            protected override bool PredicateFunction(string s)
            {
                return "b".Equals(s, StringComparison.Ordinal);
            }
        }

        [Test]
        [Obsolete("This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual void TestB2Iterator()
        {
            IEnumerator<string> it = new FilterIteratorAnonymousClass5(set.GetEnumerator());
            // this time without check: Assert.IsTrue(it.hasNext());
            it.MoveNext();
            Assert.AreEqual("b", it.Current);
            AssertNoMore(it);
        }

        [Obsolete("This class will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        private sealed class FilterIteratorAnonymousClass5 : FilterIterator<string>
        {
            public FilterIteratorAnonymousClass5(IEnumerator<string> iterator)
                : base(iterator)
            {
            }

            protected override bool PredicateFunction(string s)
            {
                return "b".Equals(s, StringComparison.Ordinal);
            }
        }

        [Test]
        [Obsolete("This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual void TestAll1Iterator()
        {
            IEnumerator<string> it = new FilterIteratorAnonymousClass6(set.GetEnumerator());
            Assert.IsTrue(it.MoveNext());
            Assert.AreEqual("a", it.Current);
            Assert.IsTrue(it.MoveNext());
            Assert.AreEqual("b", it.Current);
            Assert.IsTrue(it.MoveNext());
            Assert.AreEqual("c", it.Current);
            AssertNoMore(it);
        }

        [Obsolete("This class will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        private sealed class FilterIteratorAnonymousClass6 : FilterIterator<string>
        {
            public FilterIteratorAnonymousClass6(IEnumerator<string> iterator)
                : base(iterator)
            {
            }

            protected override bool PredicateFunction(string s)
            {
                return true;
            }
        }

        [Test]
        [Obsolete("This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual void TestAll2Iterator()
        {
            IEnumerator<string> it = new FilterIteratorAnonymousClass7(set.GetEnumerator());
            it.MoveNext();
            Assert.AreEqual("a", it.Current);
            it.MoveNext();
            Assert.AreEqual("b", it.Current);
            it.MoveNext();
            Assert.AreEqual("c", it.Current);
            AssertNoMore(it);
        }

        [Obsolete("This class will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        private sealed class FilterIteratorAnonymousClass7 : FilterIterator<string>
        {
            public FilterIteratorAnonymousClass7(IEnumerator<string> iterator)
                : base(iterator)
            {
            }

            protected override bool PredicateFunction(string s)
            {
                return true;
            }
        }
    }
}