using System;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [TestFixture]
    public class TestVirtualMethod : LuceneTestCase
    {
        private static readonly VirtualMethod<TestVirtualMethod> publicTestMethod =
            new VirtualMethod<TestVirtualMethod>(typeof(TestVirtualMethod), "publicTest", typeof(string));
        private static VirtualMethod<TestVirtualMethod> protectedTestMethod =
            new VirtualMethod<TestVirtualMethod>(typeof(TestVirtualMethod), "protectedTest", typeof(int));

        public void publicTest(string test) { }
        protected void protectedTest(int test) { }

        internal class TestClass1 : TestVirtualMethod
        {
            public override void publicTest(string test) { }
            protected override void protectedTest(int test) { }
        }

        internal class TestClass2 : TestClass1
        {
            public override void protectedTest(int test) { }
        }

        internal class TestClass3 : TestClass2
        {
            public override void publicTest(string test) { }
        }

        internal class TestClass4 : TestVirtualMethod
        {
        }

        internal class TestClass5 : TestClass4
        {
        }

        [Test]
        public void TestGeneral()
        {
            Assert.AreEqual(0, publicTestMethod.GetImplementationDistance(this.GetType()));
            Assert.AreEqual(1, publicTestMethod.GetImplementationDistance(typeof(TestClass1)));
            Assert.AreEqual(1, publicTestMethod.GetImplementationDistance(typeof(TestClass2)));
            Assert.AreEqual(3, publicTestMethod.GetImplementationDistance(typeof(TestClass3)));
            Assert.IsFalse(publicTestMethod.IsOverriddenAsOf(typeof(TestClass4)));
            Assert.IsFalse(publicTestMethod.IsOverriddenAsOf(typeof(TestClass5)));

            Assert.AreEqual(0, protectedTestMethod.GetImplementationDistance(this.GetType()));
            Assert.AreEqual(1, protectedTestMethod.GetImplementationDistance(typeof(TestClass1)));
            Assert.AreEqual(2, protectedTestMethod.GetImplementationDistance(typeof(TestClass2)));
            Assert.AreEqual(2, protectedTestMethod.GetImplementationDistance(typeof(TestClass3)));
            Assert.IsFalse(protectedTestMethod.IsOverriddenAsOf(typeof(TestClass4)));
            Assert.IsFalse(protectedTestMethod.IsOverriddenAsOf(typeof(TestClass5)));

            Assert.IsTrue(VirtualMethod.compareImplementationDistance(typeof(TestClass3), publicTestMethod, protectedTestMethod) > 0);
            Assert.AreEqual(0, VirtualMethod.compareImplementationDistance(typeof(TestClass5), publicTestMethod, protectedTestMethod));
        }

        [Test]
        public void TestExceptions()
        {
            Assert.Throws<ArgumentException>(() =>
                {
                    publicTestMethod.GetImplementationDistance((Type)typeof(LuceneTestCase));
                }, "LuceneTestCase is not a subclass and can never override publicTest(string)")

            Assert.Throws<ArgumentException>(() =>
                {
                    new VirtualMethod<TestVirtualMethod>(typeof(TestVirtualMethod), "bogus");
                }, "Method bogus() does not exist, so IAE should be thrown");

            Assert.Throws<ArgumentException>(() =>
                {
                    new VirtualMethod<TestClass2>(typeof(TestClass2), "publicTest", typeof(string));
                }, "Method publicTest(string) is not declared in TestClass2, so IAE should be thrown");

            Assert.Throws<InvalidOperationException>(() =>
                {
                    new VirtualMethod<TestVirtualMethod>(typeof(TestVirtualMethod), "publicTest", typeof(string));
                }, "Violating singleton status succeeded");

        }
    }
}
