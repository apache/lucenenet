using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Threading;

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

    public class TestWorstCaseTestBehavior : LuceneTestCase
    {
#if !NETSTANDARD
        [Ignore("Ignored in Lucene")]
        [Test]
        public virtual void TestThreadLeak()
        {
            ThreadClass t = new ThreadAnonymousInnerClassHelper(this);
            t.Start();

            while (!t.IsAlive)
            {
                Thread.Yield();
            }

            // once alive, leave it to run outside of the test scope.
        }
#endif

        private class ThreadAnonymousInnerClassHelper : ThreadClass
        {
            private readonly TestWorstCaseTestBehavior OuterInstance;

            public ThreadAnonymousInnerClassHelper(TestWorstCaseTestBehavior outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override void Run()
            {
#if !NETSTANDARD
                try
                {
#endif
                    Thread.Sleep(10000);
#if !NETSTANDARD
                }
#pragma warning disable 168
                catch (ThreadInterruptedException e)
#pragma warning restore 168
                {
                    // Ignore.
                }
#endif
            }
        }

        [Ignore("Ignored in Lucene")]
        [Test]
        public virtual void TestLaaaaaargeOutput()
        {
            string message = "I will not OOM on large output";
            int howMuch = 250 * 1024 * 1024;
            for (int i = 0; i < howMuch; i++)
            {
                if (i > 0)
                {
                    Console.Write(",\n");
                }
                Console.Write(message);
                howMuch -= message.Length; // approximately.
            }
            Console.WriteLine(".");
        }


        [Ignore("Ignored in Lucene")]
        [Test]
        public virtual void TestProgressiveOutput()
        {
            for (int i = 0; i < 20; i++)
            {
                Console.WriteLine("Emitting sysout line: " + i);
                Console.Error.WriteLine("Emitting syserr line: " + i);
                Console.Out.Flush();
                Console.Error.Flush();
                Thread.Sleep(1000);
            }
        }

        [Ignore("Ignored in Lucene")]
        [Test]
        public virtual void TestUncaughtException()
        {
            ThreadClass t = new ThreadAnonymousInnerClassHelper2(this);
            t.Start();
            t.Join();
        }

        private class ThreadAnonymousInnerClassHelper2 : ThreadClass
        {
            private readonly TestWorstCaseTestBehavior OuterInstance;

            public ThreadAnonymousInnerClassHelper2(TestWorstCaseTestBehavior outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override void Run()
            {
                throw new Exception("foobar");
            }
        }

        [Ignore("Ignored in Lucene")]
#if !NETSTANDARD
        // LUCENENET: There is no Timeout on NUnit for .NET Core.
        [Timeout(500)]
#endif
        [Test]
        public virtual void TestTimeout()
        {
            Thread.Sleep(5000);
        }


        [Ignore("Ignored in Lucene")]
#if !NETSTANDARD
        // LUCENENET: There is no Timeout on NUnit for .NET Core.
        [Timeout(1000)]
#endif
        [Test]
        public virtual void TestZombie()
        {
            while (true)
            {
#if !NETSTANDARD
                try
                {
#endif
                    Thread.Sleep(1000);
#if !NETSTANDARD
                }
#pragma warning disable 168
                catch (ThreadInterruptedException e)
#pragma warning restore 168
                {
                }
#endif
            }
        }
    }
}