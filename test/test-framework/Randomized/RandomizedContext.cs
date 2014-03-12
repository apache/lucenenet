/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Lucene.Net.Support;

namespace Lucene.Net.Randomized
{
    public class RandomizedContext : IDisposable
    {
        private static readonly object globalLock = new object();
        private static readonly object contextLock = new object();

        private class ThreadResources
        {
            public ThreadResources()
            {
                this.Queue = new Queue<Randomness>();
            }

            public Queue<Randomness> Queue { get; private set; }
        }

        private static readonly IdentityHashMap<ThreadGroup, RandomizedContext> contexts = 
            new IdentityHashMap<ThreadGroup, RandomizedContext>();

        private readonly WeakDictionary<Thread, ThreadResources> threadResources
            = new WeakDictionary<Thread, ThreadResources>();

        private readonly ThreadGroup threadGroup;
        private Type suiteClass;
        private volatile Boolean isDisposed = false;
        private RandomizedRunner runner;

        public Type GetTargetType
        {
            get
            {
                this.GuardDiposed();
                return this.suiteClass;
            }
        }

        public int RunnerSeed
        {
            get
            {
                return this.runner.Randomness.Seed;
            }
        }

        private RandomizedContext(ThreadGroup group, Type suiteClass, RandomizedRunner runner)
        {
            this.threadGroup = group;
            this.suiteClass = suiteClass;
            this.runner = runner; 
        }



        private static RandomizedContext Context(Thread thread)
        {
            return null;
        }

        private void GuardDiposed()
        {
            if (this.isDisposed)
                throw new ObjectDisposedException("RandomContext is diposed for thread," + Thread.CurrentThread.Name + ".");
        }


        static RandomizedContext Create(ThreadGroup tg, Type suiteClass, RandomizedRunner runner) {
            lock(globalLock) {
                var context = new RandomizedContext(tg, suiteClass, runner);
                contexts.Add(tg, context);
                context.threadResources.Add(Thread.CurrentThread, new ThreadResources());
                return context;
            }
        }



        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
