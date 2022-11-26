using System;
using System.Buffers;

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
    /// LUCENENET specific - A pooled implementation for <see cref="PriorityQueue{T}"/> 
    /// <para/><b>NOTE</b>: this class will allocate array from the shared pool, on disposal return the used array.
    /// <para/>
    /// @lucene.internal
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public abstract class PooledPriorityQueue<T> : PriorityQueueBase<T>, IDisposable
    {
        public override T[] AllocateHeapArray()
        {
            int heapSize = base.GetHeapSize();
            T[] h = ArrayPool<T>.Shared.Rent(heapSize);
            return h;
        }

        protected PooledPriorityQueue(int maxSize) // LUCENENET specific - made protected instead of public
            : this(maxSize, true)
        {
        }

        protected PooledPriorityQueue(int maxSize, bool prepopulate) // LUCENENET specific - made protected instead of public
            : base(maxSize, prepopulate)
        {
        }

        public void Dispose()
        {
            if (this.heap != null)
                ArrayPool<T>.Shared.Return(this.heap);
        }
    }
}