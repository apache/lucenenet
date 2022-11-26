using System;

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
    /// A <see cref="PriorityQueue{T}"/> maintains a partial ordering of its elements such that the
    /// element with least priority can always be found in constant time. Put()'s and Pop()'s
    /// require log(size) time.
    ///
    /// <para/><b>NOTE</b>: this class will pre-allocate a full array of
    /// length <c>maxSize+1</c> if instantiated via the
    /// <see cref="PriorityQueue(int, bool)"/> constructor with
    /// <c>prepopulate</c> set to <c>true</c>. That maximum
    /// size can grow as we insert elements over the time.
    /// <para/>
    /// @lucene.internal
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public abstract class PriorityQueue<T> : PriorityQueueBase<T>
    {
        public override T[] AllocateHeapArray()
        {
            int heapSize = base.GetHeapSize();
            T[] h = new T[heapSize];
            return h;
        }

        protected PriorityQueue(int maxSize) // LUCENENET specific - made protected instead of public
            : this(maxSize, true)
        {
        }

        protected PriorityQueue(int maxSize, bool prepopulate) // LUCENENET specific - made protected instead of public
            : base(maxSize, prepopulate)
        {
        }
    }
}