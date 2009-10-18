/**
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

using System;
using System.Threading;
using System.Collections;

namespace Lucene.Net.Util
{
    // {{dougsale-2.4.0}}
    // so here is the doc from the java version:

    /** Java's builtin ThreadLocal has a serious flaw:
     *  it can take an arbitrarily long amount of time to
     *  dereference the things you had stored in it, even once the
     *  ThreadLocal instance itself is no longer referenced.
     *  This is because there is single, master map stored for
     *  each thread, which all ThreadLocals share, and that
     *  master map only periodically purges "stale" entries.
     *
     *  While not technically a memory leak, because eventually
     *  the memory will be reclaimed, it can take a long time
     *  and you can easily hit OutOfMemoryError because from the
     *  GC's standpoint the stale entries are not reclaimaible.
     * 
     *  This class works around that, by only enrolling
     *  WeakReference values into the ThreadLocal, and
     *  separately holding a hard reference to each stored
     *  value.  When you call {@link #close}, these hard
     *  references are cleared and then GC is freely able to
     *  reclaim space by objects stored in it.
     */

    // i'm not sure if C#'s Thread.SetData(System.LocalDataStoreSlot, object) has the same issue.
    // For now, i'll just implement this using Thread.SetData(System.LocalDataStoreSlot, object)
    // and Thread.GetData(System.LocalDataStoreSlot) so that we're API compliant.



    //public class CloseableThreadLocal
    //{
    //    private System.LocalDataStoreSlot dataSlot;

    //    public CloseableThreadLocal()
    //    {
    //        dataSlot = Thread.AllocateDataSlot();
    //    }

    //    virtual protected object InitialValue()
    //    {
    //        return null;
    //    }

    //    public object Get()
    //    {
    //        object v = Thread.GetData(dataSlot);
    //        if (v == null)
    //        {
    //            v = InitialValue();
    //            Set(v);
    //        }
    //        return v;
    //    }
                
    //    public void Set(object v)
    //    {
    //        Thread.SetData(dataSlot, v);
    //    }

    //    public void Close()
    //    {
    //    }
    //}

    public class CloseableThreadLocal
    {
        private System.LocalDataStoreSlot t = Thread.AllocateDataSlot();
        private Hashtable hardRefs = new Hashtable();

        virtual protected object InitialValue()
        {
            return null;
        }

        public object Get()
        {
            WeakReference weakRef = (WeakReference)Thread.GetData(t);
            if (weakRef == null || weakRef.Target==null)
            {
                Object iv = InitialValue();
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
                Object v = weakRef.Target;
                // This can never be null, because we hold a hard
                // reference to the underlying object:
                //assert v != null;
                return v;
            }
        }

        public void Set(object obj)
        {
            Thread.SetData(t, new WeakReference(obj));
        
            lock (hardRefs)
            {
                ArrayList tmp = new ArrayList();
                hardRefs[Thread.CurrentThread] = obj;

                foreach (Thread th in hardRefs.Keys)
                {
                    //collect items to remove but don't remove them in the middle of enumeration. 
                    if (!th.IsAlive) tmp.Add(th);
                }
                foreach (Thread th in tmp)
                {
                    hardRefs.Remove(th);
                }
            }
        }
  

        public void Close()
        {
            // Clear the hard refs; then, the only remaining refs to
            // all values we were storing are weak (unless somewhere
            // else is still using them) and so GC may reclaim them:
            hardRefs = null;
            t = null;
        }
    }
}
