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
using System.Threading;

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
    /// value.  When you call {@link #close}, these hard
    /// references are cleared and then GC is freely able to
    /// reclaim space by objects stored in it. 
    /// </summary>
    /// 

    public class CloseableThreadLocal<T> where T : class
    {
        private ThreadLocal<WeakReference> t = new ThreadLocal<WeakReference>();

        private Dictionary<Thread, T> hardRefs = new Dictionary<Thread, T>();


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
            t.Set(new WeakReference(@object));

            lock (hardRefs)
            {
                //hardRefs[Thread.CurrentThread] = @object;
                hardRefs.Add(Thread.CurrentThread,@object);

                // Purge dead threads
                foreach (var thread in new List<Thread>(hardRefs.Keys))
                {
                    if (!thread.IsAlive)
                        hardRefs.Remove(thread);
                }

            }
        }

        public virtual void Close()
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


    // {{DIGY}} 
    // This causes security exception in Medium Trust 
    // TestCase: Lucene.Net.Support.TestMediumTrust.TestIndexAndSearch
    //class WeakReference<T> : WeakReference where T : class
    //{
    //    public WeakReference(T target) : base(target)
    //    {
    //    }

    //    public WeakReference(T target, bool trackResurrection)  : base(target, trackResurrection)
    //    {
    //    }

    //    public new T Target
    //    {
    //        get { return (T)base.Target; }
    //    }

    //    public T Get()
    //    {
    //        return (T)base.Target;
    //    }
    //}
}