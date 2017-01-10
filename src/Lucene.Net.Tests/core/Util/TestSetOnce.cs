using System.Threading;
using Lucene.Net.Support;
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
    public class TestSetOnce : LuceneTestCase
    {
        private sealed class SetOnceThread : ThreadClass
        {
            internal SetOnce<int?> Set;
            internal bool Success = false;
            internal readonly Random RAND;

            public SetOnceThread(Random random)
            {
                RAND = new Random(random.Next());
            }

            public override void Run()
            {
                try
                {
                    Sleep(RAND.Next(10)); // sleep for a short time
                    Set.Set(new int?(Convert.ToInt32(Name.Substring(2))));
                    Success = true;
                }
#if !NETSTANDARD
                catch (ThreadInterruptedException)
                {
                    // ignore
                }
#endif
                catch (Exception)
                {
                    // TODO: change exception type
                    // expected.
                    Success = false;
                }
            }
        }

        [Test]
        public virtual void TestEmptyCtor()
        {
            SetOnce<int?> set = new SetOnce<int?>();
            Assert.IsNull(set.Get());
        }

        [Test]
        [ExpectedException(typeof(SetOnce<int?>.AlreadySetException))]
        public virtual void TestSettingCtor()
        {
            SetOnce<int?> set = new SetOnce<int?>(new int?(5));
            Assert.AreEqual(5, (int)set.Get());
            set.Set(new int?(7));
        }

        [Test]
        [ExpectedException(typeof(SetOnce<int?>.AlreadySetException))]
        public virtual void TestSetOnce_mem()
        {
            SetOnce<int?> set = new SetOnce<int?>();
            set.Set(new int?(5));
            Assert.AreEqual(5, (int)set.Get());
            set.Set(new int?(7));
        }

        [Test]
        public virtual void TestSetMultiThreaded()
        {
            SetOnce<int?> set = new SetOnce<int?>();
            SetOnceThread[] threads = new SetOnceThread[10];
            Random random = Random();
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new SetOnceThread(random);
                threads[i].Name = "t-" + (i + 1);
                threads[i].Set = set;
            }

            foreach (ThreadClass t in threads)
            {
                t.Start();
            }

            foreach (ThreadClass t in threads)
            {
                t.Join();
            }

            foreach (SetOnceThread t in threads)
            {
                if (t.Success)
                {
                    int expectedVal = Convert.ToInt32(t.Name.Substring(2));
                    Assert.AreEqual(expectedVal, t.Set.Get(), "thread " + t.Name);
                }
            }
        }
    }
}