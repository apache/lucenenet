#if FEATURE_RANDOMIZEDCONTEXT
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
using System.Threading;

namespace Lucene.Net.Randomized
{
    public static class ThreadGroupExtensions
    {
        private static readonly object globalLock = new object();

        public static ThreadGroup GetThreadGroup(this Thread thread)
        {
            if (thread.IsAlive)
            {
                lock (ThreadGroup.GroupLock)
                {
                    foreach (var group in ThreadGroup.Groups)
                    {
                        group.Prune();
                        foreach (var weak in group)
                        {
                            if (thread == (Thread)weak.Target)
                                return group;
                        }
                    }

                    ThreadGroup.Root.Add(thread);
                    return ThreadGroup.Root;
                }
            }
            return null;
        }
    }

    public class ThreadGroup : IEnumerable<WeakReference>, IDisposable
    {
        private List<WeakReference> threads;
        private static object s_groupLock = new Object();

        internal static object GroupLock
        {
            get
            {
                if (s_groupLock == null)
                    s_groupLock = new Object();
                return s_groupLock;
            }
        }

        internal static List<ThreadGroup> Groups { get; set; }

        static ThreadGroup()
        {
            Groups = new List<ThreadGroup>();
            Root = new ThreadGroup("Root");
        }

        public static ThreadGroup Root { get; set; }

        public string Name { get; protected set; }

        public ThreadGroup Parent { get; protected set; }

        public ThreadGroup(string name)
            : this(name, null)
        {
        }

        public ThreadGroup(string name, ThreadGroup parent)
        {
            this.Parent = parent;
            this.Name = name;
            this.threads = new List<WeakReference>();
            lock (GroupLock)
            {
                Groups.Add(this);
            }
        }

        internal void Add(Thread instance)
        {
            var threadRef = new WeakReference(instance);
            this.threads.Add(threadRef);
        }

        internal void Prune()
        {
            var copy = this.threads.ToList();
            foreach (var item in copy)
            {
                if (!item.IsAlive)
                    this.threads.Remove(item);
            }
        }

        public IEnumerator<WeakReference> GetEnumerator()
        {
            return this.threads.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.threads.GetEnumerator();
        }

        // LUCENENET specific: Implemented dispose pattern
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (GroupLock)
                {
                    Groups.Remove(this);
                }
            }
        }
    }
}
#endif