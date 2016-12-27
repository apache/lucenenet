using System;
using System.Threading;

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

    /// <summary>
    /// Manages reference counting for a given object. Extensions can override
    /// <seealso cref="#release()"/> to do custom logic when reference counting hits 0.
    /// </summary>
    public class RefCount<T>
    {
        //private readonly AtomicInteger refCount = new AtomicInteger(1);
        private int refCount = 1;

        protected internal readonly T @object; // LUCENENET TODO: rename m_

        public RefCount(T @object)
        {
            this.@object = @object;
        }

        /// <summary>
        /// Called when reference counting hits 0. By default this method does nothing,
        /// but extensions can override to e.g. release resources attached to object
        /// that is managed by this class.
        /// </summary>
        protected virtual void Release()
        {
        }

        /// <summary>
        /// Decrements the reference counting of this object. When reference counting
        /// hits 0, calls <seealso cref="#release()"/>.
        /// </summary>
        public void DecRef()
        {
            int rc = Interlocked.Decrement(ref refCount);
            if (rc == 0)
            {
                bool success = false;
                try
                {
                    Release();
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        // Put reference back on failure
                        Interlocked.Increment(ref refCount);
                    }
                }
            }
            else if (rc < 0)
            {
                throw new InvalidOperationException("too many decRef calls: refCount is " + rc + " after decrement");
            }
        }

        public T Get()
        {
            return @object;
        }

        /// <summary>
        /// Returns the current reference count. </summary>
        public int GetRefCount // LUCENENET TODO: rename RefCount
        {
            get
            {
                //LUCENE TO-DO read operations atomic in 64 bit
                /*if (IntPtr.Size == 4)
                {
                    long refCount_ = 0;
                    Interlocked.Exchange(ref refCount_, (long)refCount);
                    return (int)Interlocked.Read(ref refCount_);
                }*/
                return refCount;
            }
        }

        /// <summary>
        /// Increments the reference count. Calls to this method must be matched with
        /// calls to <seealso cref="#decRef()"/>.
        /// </summary>
        public void IncRef()
        {
            Interlocked.Increment(ref refCount);
        }
    }
}