using NUnit.Framework;
using System;

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
    public class TestVirtualMethod : LuceneTestCase
    {
        private static readonly VirtualMethod PublicTestMethod;
        private static readonly VirtualMethod ProtectedTestMethod;

        static TestVirtualMethod()
        {
            PublicTestMethod = new VirtualMethod(typeof(BaseTestVirtualMethod), "PublicTest", typeof(string));
            ProtectedTestMethod = new VirtualMethod(typeof(BaseTestVirtualMethod), "ProtectedTest", typeof(int));
        }

        /// <summary>
        /// LUCENENET specific class used here because inheriting test classes messes up the context
        /// that the tests are run in. So, we substitute a class that has no tests.
        /// </summary>
        public class BaseTestVirtualMethod
        {

            public virtual void PublicTest(string test)
            {
            }

            protected virtual void ProtectedTest(int test)
            {
            }

        }

        internal class TestClass1 : BaseTestVirtualMethod
        {
            public override void PublicTest(string test)
            {
            }

            protected override void ProtectedTest(int test)
            {
            }
        }

        internal class TestClass2 : TestClass1
        {
            protected override void ProtectedTest(int test) // make it public here
            {
            }
        }

        internal class TestClass3 : TestClass2
        {
            public override void PublicTest(string test)
            {
            }
        }

        internal class TestClass4 : BaseTestVirtualMethod
        {
        }

        internal class TestClass5 : TestClass4
        {
        }

        [Test]
        public virtual void TestGeneral()
        {
            // LUCENENET: Substituted BaseTestVirtualMethod for this class, but the logic is the same.
            Assert.AreEqual(0, PublicTestMethod.GetImplementationDistance(typeof(BaseTestVirtualMethod)));
            Assert.AreEqual(1, PublicTestMethod.GetImplementationDistance(typeof(TestClass1)));
            Assert.AreEqual(1, PublicTestMethod.GetImplementationDistance(typeof(TestClass2)));
            Assert.AreEqual(3, PublicTestMethod.GetImplementationDistance(typeof(TestClass3)));
            Assert.IsFalse(PublicTestMethod.IsOverriddenAsOf(typeof(TestClass4)));
            Assert.IsFalse(PublicTestMethod.IsOverriddenAsOf(typeof(TestClass5)));

            // LUCENENET: Substituted BaseTestVirtualMethod for this class, but the logic is the same.
            Assert.AreEqual(0, ProtectedTestMethod.GetImplementationDistance(typeof(BaseTestVirtualMethod)));
            Assert.AreEqual(1, ProtectedTestMethod.GetImplementationDistance(typeof(TestClass1)));
            Assert.AreEqual(2, ProtectedTestMethod.GetImplementationDistance(typeof(TestClass2)));
            Assert.AreEqual(2, ProtectedTestMethod.GetImplementationDistance(typeof(TestClass3)));
            Assert.IsFalse(ProtectedTestMethod.IsOverriddenAsOf(typeof(TestClass4)));
            Assert.IsFalse(ProtectedTestMethod.IsOverriddenAsOf(typeof(TestClass5)));

            Assert.IsTrue(VirtualMethod.CompareImplementationDistance(typeof(TestClass3), PublicTestMethod, ProtectedTestMethod) > 0);
            Assert.AreEqual(0, VirtualMethod.CompareImplementationDistance(typeof(TestClass5), PublicTestMethod, ProtectedTestMethod));
        }

        [Test]
        public virtual void TestExceptions()
        {
            try
            {
                // cast to Class to remove generics:
                PublicTestMethod.GetImplementationDistance(typeof(LuceneTestCase));
                Assert.Fail("LuceneTestCase is not a subclass and can never override publicTest(String)");
            }
            catch (System.ArgumentException arg)
            {
                // pass
            }

            try
            {
                new VirtualMethod(typeof(BaseTestVirtualMethod), "bogus");
                Assert.Fail("Method bogus() does not exist, so IAE should be thrown");
            }
            catch (System.ArgumentException arg)
            {
                // pass
            }

            try
            {
                new VirtualMethod(typeof(TestClass2), "PublicTest", typeof(string));
            }
            catch (System.ArgumentException arg)
            {
                Assert.Fail("Method publicTest(String) is declared in TestClass2, so IAE should not be thrown");
            }

            try
            {
                // try to create a second instance of the same baseClass / method combination
                new VirtualMethod(typeof(BaseTestVirtualMethod), "PublicTest", typeof(string));
                Assert.Fail("Violating singleton status succeeded");
            }
            catch (System.ArgumentException arg)
            {
                // pass
            }
        }
    }
}