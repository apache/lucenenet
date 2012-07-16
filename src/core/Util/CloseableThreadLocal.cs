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
using Lucene.Net.Support;

#if NET35
using Lucene.Net.Support.Compatibility;
#endif

namespace Lucene.Net.Util
{

    /// <summary>Java's builtin ThreadLocal has a serious flaw:
    /// it can take an arbitrarily long amount of time to
    /// dereference the things you had stored in it, even once the
    /// ThreadLocal instance itself is no longer referenced.
    /// This is because there is single, master map stored for
    /// each thread, which all ThreadLocals share, and that
    /// master map only periodically purges "stale" entries.
    /// 
    /// While not technically a memory leak, because eventually
    /// the memory will be reclaimed, it can take a long time
    /// and you can easily hit OutOfMemoryError because from the
    /// GC's standpoint the stale entries are not reclaimaible.
    /// 
    /// This class works around that, by only enrolling
    /// WeakReference values into the ThreadLocal, and
    /// separately holding a hard reference to each stored
    /// value.  When you call <see cref="Close" />, these hard
    /// references are cleared and then GC is freely able to
    /// reclaim space by objects stored in it. 
    /// </summary>
    /// 

    public class CloseableThreadLocal<T> : IDisposable where T : class
    {
        // NOTE: Java has WeakReference<T>.  This isn't available for .Net until 4.5 (according to msdn docs)
        private ThreadLocal<WeakReference> t = new ThreadLocal<WeakReference>();

        private IDictionary<Thread, T> hardRefs = new HashMap<Thread, T>();

        private bool isDisposed;

        public virtual T InitialValue()
        {
            return null;
        }

        public virtual T Get()
        {
            WeakReference weakRef = t.Get();
            if (weakRef == null)
            {
                T iv = InitialValue();
                if (iv != null)
                {
                    Set(iv);
                    return iv;
                }
                else
                    return null;
            }
            else
            {
                return (T)weakRef.Get();
            }
        }

        public virtual void Set(T @object)
        {
            //+-- For Debuging
            if (CloseableThreadLocalProfiler.EnableCloseableThreadLocalProfiler == true)
            {
                lock (CloseableThreadLocalProfiler.Instances)
                {
                    CloseableThreadLocalProfiler.Instances.Add(new WeakReference(@object));
                }
            }
            //+--

            t.Set(new WeakReference(@object));

            lock (hardRefs)
            {
                //hardRefs[Thread.CurrentThread] = @object;
                hardRefs.Add(Thread.CurrentThread, @object);
                
                // Java's iterator can remove, .NET's cannot
                var threadsToRemove = hardRefs.Keys.Where(thread => !thread.IsAlive).ToList();
                // Purge dead threads
                foreach (var thread in threadsToRemove)
                {
                    hardRefs.Remove(thread);
                }
            }
        }

        [Obsolete("Use Dispose() instead")]
        public virtual void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;

            if (disposing)
            {
                // Clear the hard refs; then, the only remaining refs to
                // all values we were storing are weak (unless somewhere
                // else is still using them) and so GC may reclaim them:
                hardRefs = null;
                // Take care of the current thread right now; others will be
                // taken care of via the WeakReferences.
                if (t != null)
                {
                    t.Remove();
                }
                t = null;
            }

            isDisposed = true;
        }
    }

    internal static class CloseableThreadLocalExtensions
    {
        public static void Set<T>(this ThreadLocal<T> t, T val)
        {
            t.Value = val;
        }

        public static T Get<T>(this ThreadLocal<T> t)
        {
            return t.Value;
        }

        public static void Remove<T>(this ThreadLocal<T> t)
        {
            t.Dispose();
        }

        public static object Get(this WeakReference w)
        {
            return w.Target;
        }
    }

    //// {{DIGY}}
    //// To compile against Framework 2.0
    //// Uncomment below class
    //public class ThreadLocal<T> : IDisposable
    //{
    //    [ThreadStatic]
    //    static SupportClass.WeakHashTable slots;

    //    void Init()
    //    {
    //        if (slots == null) slots = new SupportClass.WeakHashTable();
    //    }

    //    public T Value
    //    {
    //        set
    //        {
    //            Init();
    //            slots.Add(this, value);
    //        }
    //        get
    //        {
    //            Init();
    //            return (T)slots[this];
    //        }
    //    }

    //    public void Dispose()
    //    {
    //        if (slots != null) slots.Remove(this);
    //    }
    //}
}
