using J2N.Threading.Atomic;
using System;
using System.Runtime.CompilerServices;

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
    /// <see cref="Release()"/> to do custom logic when reference counting hits 0.
    /// </summary>
    public class RefCount<T>
    {
        private readonly AtomicInt32 refCount = new AtomicInt32(1);

        protected internal readonly T m_object;

        public RefCount(T @object)
        {
            this.m_object = @object;
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
        /// hits 0, calls <see cref="Release()"/>.
        /// </summary>
        public void DecRef()
        {
            int rc = refCount.DecrementAndGet();
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
                        refCount.IncrementAndGet();
                    }
                }
            }
            else if (rc < 0)
            {
                throw IllegalStateException.Create("too many DecRef() calls: refCount is " + rc + " after decrement");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get()
        {
            return m_object;
        }

        /// <summary>
        /// Returns the current reference count. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetRefCount() // LUCENENET NOTE: although this would be a good candidate for a property, doing so would cause a naming conflict
        {
            return refCount;
        }

        /// <summary>
        /// Increments the reference count. Calls to this method must be matched with
        /// calls to <see cref="DecRef()"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncRef()
        {
            refCount.IncrementAndGet();
        }
    }
}