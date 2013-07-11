using System;
using System.Threading;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [TestFixture]
    public class TestSetOnce : LuceneTestCase
    {
        private class SetOnceThread : ThreadClass
        {
            internal SetOnce<int> set;
            internal bool success = false;
            internal Random RAND;

            public SetOnceThread(Random random)
            {
                RAND = new Random(random.Next());
            }

            public override void Run()
            {
                try
                {
                    Sleep(RAND.Next(10)); // sleep for a short time
                    set.Set(int.Parse(Name.Substring(2)));
                    success = true;
                }
                catch (ThreadInterruptedException e)
                {
                    // ignore
                }
                catch (SystemException e)
                {
                    // TODO: change exception type
                    // expected.
                    success = false;
                }
            }
        }

        //@Test
        [Test]
        public void TestEmptyCtor()
        {
            var set = new SetOnce<int>();
            assertNull(set.Get());
        }

        //@Test(expected=SetOnce<>.AlreadySetException.class)
        [Test]
        [ExpectedException(typeof(SetOnce<>.AlreadySetException))]
        public void TestSettingCtor()
        {
            var set = new SetOnce<int>(5);
            assertEquals(5, set.Get());
            set.Set(7);
        }

        //@Test(expected=SetOnce<>.AlreadySetException.class)
        [Test]
        [ExpectedException(typeof(SetOnce<>.AlreadySetException))]
        public void testSetOnce()
        {
            var set = new SetOnce<int>();
            set.Set(5);
            assertEquals(5, set.Get());
            set.Set(7);
        }

        [Test]
        public void TestSetMultiThreaded()
        {
            var set = new SetOnce<int>();
            var threads = new SetOnceThread[10];
            for (var i = 0; i < threads.Length; i++)
            {
                threads[i] = new SetOnceThread(new Random()) {Name = "t-" + (i + 1), set = set};
            }

            foreach (var t in threads)
            {
                t.Start();
            }

            foreach (var t in threads)
            {
                t.Join();
            }

            foreach (SetOnceThread t in threads)
            {
                if (t.success)
                {
                    var expectedVal = int.Parse(t.Name.Substring(2));
                    assertEquals("thread " + t.Name, expectedVal, t.set.Get());
                }
            }
        }
    }
}
