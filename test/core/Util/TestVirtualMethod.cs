using System;
using NUnit.Framework;

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

        private static readonly VirtualMethod<TestVirtualMethod> PublicTestMethod = new VirtualMethod<TestVirtualMethod>(typeof(TestVirtualMethod), "publicTest", typeof(string));
        private static readonly VirtualMethod<TestVirtualMethod> ProtectedTestMethod = new VirtualMethod<TestVirtualMethod>(typeof(TestVirtualMethod), "protectedTest", typeof(int));

        public virtual void PublicTest(string test)
        {
        }
        protected internal virtual void ProtectedTest(int test)
        {
        }

        internal class TestClass1 : TestVirtualMethod
        {
            public override void PublicTest(string test)
            {
            }
            protected internal override void ProtectedTest(int test)
            {
            }
        }

        internal class TestClass2 : TestClass1
        {
            protected internal override void ProtectedTest(int test) // make it public here
            {
            }
        }

        internal class TestClass3 : TestClass2
        {
            public override void PublicTest(string test)
            {
            }
        }

        internal class TestClass4 : TestVirtualMethod
        {
        }

        internal class TestClass5 : TestClass4
        {
        }

        [Test]
        public virtual void TestGeneral()
        {
            Assert.AreEqual(0, PublicTestMethod.getImplementationDistance(this.GetType()));
            Assert.AreEqual(1, PublicTestMethod.getImplementationDistance(typeof(TestClass1)));
            Assert.AreEqual(1, PublicTestMethod.getImplementationDistance(typeof(TestClass2)));
            Assert.AreEqual(3, PublicTestMethod.getImplementationDistance(typeof(TestClass3)));
            Assert.IsFalse(PublicTestMethod.isOverriddenAsOf(typeof(TestClass4)));
            Assert.IsFalse(PublicTestMethod.isOverriddenAsOf(typeof(TestClass5)));

            Assert.AreEqual(0, ProtectedTestMethod.getImplementationDistance(this.GetType()));
            Assert.AreEqual(1, ProtectedTestMethod.getImplementationDistance(typeof(TestClass1)));
            Assert.AreEqual(2, ProtectedTestMethod.getImplementationDistance(typeof(TestClass2)));
            Assert.AreEqual(2, ProtectedTestMethod.getImplementationDistance(typeof(TestClass3)));
            Assert.IsFalse(ProtectedTestMethod.isOverriddenAsOf(typeof(TestClass4)));
            Assert.IsFalse(ProtectedTestMethod.isOverriddenAsOf(typeof(TestClass5)));

            Assert.IsTrue(VirtualMethod.compareImplementationDistance(typeof(TestClass3), PublicTestMethod, ProtectedTestMethod) > 0);
            Assert.AreEqual(0, VirtualMethod.compareImplementationDistance(typeof(TestClass5), PublicTestMethod, ProtectedTestMethod));
        }

        [Test]
        public virtual void TestExceptions()
        {
            try
            {
	            // cast to Class to remove generics:
	            PublicTestMethod.getImplementationDistance(typeof((Type) LuceneTestCase));
	            Assert.Fail("LuceneTestCase is not a subclass and can never override publicTest(String)");
            }
            catch (System.ArgumentException arg)
            {
	            // pass
            }

            try
            {
	            new VirtualMethod<>(typeof(TestVirtualMethod), "bogus");
	            Assert.Fail("Method bogus() does not exist, so IAE should be thrown");
            }
            catch (System.ArgumentException arg)
            {
	            // pass
            }

            try
            {
	            new VirtualMethod<>(typeof(TestClass2), "publicTest", typeof(string));
	            Assert.Fail("Method publicTest(String) is not declared in TestClass2, so IAE should be thrown");
            }
            catch (System.ArgumentException arg)
            {
	            // pass
            }

            try
            {
	            // try to create a second instance of the same baseClass / method combination
	            new VirtualMethod<>(typeof(TestVirtualMethod), "publicTest", typeof(string));
	            Assert.Fail("Violating singleton status succeeded");
            }
            catch (System.NotSupportedException arg)
            {
	            // pass
            }
        }

    }

}