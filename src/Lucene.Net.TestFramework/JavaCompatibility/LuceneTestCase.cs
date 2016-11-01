using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Lucene.Net.Util
{
    public abstract partial class LuceneTestCase
    {
        public static void assertTrue(bool condition)
        {
            Assert.IsTrue(condition);
        }

        public static void assertTrue(string message, bool condition)
        {
            Assert.IsTrue(condition, message);
        }

        public static void assertFalse(bool condition)
        {
            Assert.IsFalse(condition);
        }

        public static void assertFalse(string message, bool condition)
        {
            Assert.IsFalse(condition, message);
        }

        public static void assertEquals(object expected, object actual)
        {
            Assert.AreEqual(expected, actual);
        }

        public static void assertEquals(string message, object expected, object actual)
        {
            Assert.AreEqual(expected, actual, message);
        }

        public static void assertEquals(long expected, long actual)
        {
            Assert.AreEqual(expected, actual);
        }

        public static void assertEquals(string message, long expected, long actual)
        {
            Assert.AreEqual(expected, actual, message);
        }

        public static void assertEquals<T>(ISet<T> expected, ISet<T> actual)
        {
            Assert.True(expected.SetEquals(actual));
        }

        public static void assertEquals<T>(string message, ISet<T> expected, ISet<T> actual)
        {
            Assert.True(expected.SetEquals(actual), message);
        }

        public static void assertEquals<T, S>(IDictionary<T, S> expected, IDictionary<T, S> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);
            foreach (var key in expected.Keys)
            {
                Assert.AreEqual(expected[key], actual[key]);
            }
        }

        public static void assertNotSame(object unexpected, object actual)
        {
            Assert.AreNotSame(unexpected, actual);
        }

        public static void assertNotSame(string message, object unexpected, object actual)
        {
            Assert.AreNotSame(unexpected, actual, message);
        }

        protected static void assertEquals(double d1, double d2, double delta)
        {
            Assert.AreEqual(d1, d2, delta);
        }

        protected static void assertEquals(string msg, double d1, double d2, double delta)
        {
            Assert.AreEqual(d1, d2, delta, msg);
        }

        protected static void assertNotNull(object o)
        {
            Assert.NotNull(o);
        }

        protected static void assertNotNull(string msg, object o)
        {
            Assert.NotNull(o, msg);
        }

        protected static void assertNull(object o)
        {
            Assert.Null(o);
        }

        protected static void assertNull(string msg, object o)
        {
            Assert.Null(o, msg);
        }

        protected static void assertArrayEquals(IEnumerable a1, IEnumerable a2)
        {
            CollectionAssert.AreEqual(a1, a2);
        }

        protected static void assertSame(Object expected, Object actual)
        {
            Assert.AreSame(expected, actual);
        }

        protected static void assertSame(string message, Object expected, Object actual)
        {
            Assert.AreSame(expected, actual, message);
        }

        protected static void fail()
        {
            Assert.Fail();
        }

        protected static void fail(string message)
        {
            Assert.Fail(message);
        }


        protected static ISet<T> AsSet<T>(params T[] args)
        {
            return new HashSet<T>(args);
        }
    }
}
