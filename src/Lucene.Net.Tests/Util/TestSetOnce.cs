using J2N.Threading;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Globalization;
using Assert = Lucene.Net.TestFramework.Assert;

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
        private class Integer // LUCENENET specific class for testing (since int? is not a reference type)
        {
            public Integer(int value)
            {
                this.value = value;
            }

            internal int value;
        }

        private sealed class SetOnceThread : ThreadJob
        {
            internal SetOnce<Integer> set;
            internal bool success = false;
            internal readonly Random RAND;

            public SetOnceThread(Random random)
            {
                RAND = new J2N.Randomizer(random.NextInt64());
            }

            public override void Run()
            {
                try
                {
                    Sleep(RAND.Next(10)); // sleep for a short time
                    set.Set(new Integer(Convert.ToInt32(Name.Substring(2), CultureInfo.InvariantCulture)));
                    success = true;
                }
                catch (Exception e) when (e.IsInterruptedException())
                {
                    // ignore
                }
                catch (Exception e) when (e.IsRuntimeException())
                {
                    // TODO: change exception type
                    // expected.
                    success = false;
                }
            }
        }

        [Test]
        public virtual void TestEmptyCtor()
        {
            SetOnce<Integer> set = new SetOnce<Integer>();
            Assert.IsNull(set.Get());
        }

        [Test]
        public virtual void TestSettingCtor()
        {
            SetOnce<Integer> set = new SetOnce<Integer>(new Integer(5));
            Assert.AreEqual(5, set.Get().value);
            Assert.Throws<AlreadySetException>(() => set.Set(new Integer(7)));
        }

        [Test]
        public virtual void TestSetOnce_mem()
        {
            SetOnce<Integer> set = new SetOnce<Integer>();
            set.Set(new Integer(5));
            Assert.AreEqual(5, set.Get().value);
            Assert.Throws<AlreadySetException>(() => set.Set(new Integer(7)));
        }

        [Test]
        public virtual void TestSetMultiThreaded()
        {
            SetOnce<Integer> set = new SetOnce<Integer>();
            SetOnceThread[] threads = new SetOnceThread[10];
            Random random = Random;
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new SetOnceThread(random);
                threads[i].Name = "t-" + (i + 1);
                threads[i].set = set;
            }

            foreach (ThreadJob t in threads)
            {
                t.Start();
            }

            foreach (ThreadJob t in threads)
            {
                t.Join();
            }

            foreach (SetOnceThread t in threads)
            {
                if (t.success)
                {
                    int expectedVal = Convert.ToInt32(t.Name.Substring(2));
                    Assert.AreEqual(expectedVal, t.set.Get().value, "thread " + t.Name);
                }
            }
        }
    }
}