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
    /// Simple counter class
    /// <para/>
    /// @lucene.internal
    /// @lucene.experimental
    /// </summary>
    public abstract class Counter
    {
        /// <summary>
        /// Adds the given delta to the counters current value.
        /// </summary>
        /// <param name="delta">
        ///          The delta to add. </param>
        /// <returns> The counters updated value. </returns>
        public abstract long AddAndGet(long delta);

        /// <summary>
        /// Gets the counters current value.
        /// </summary>
        public abstract long Value { get; }

        /// <summary>
        /// Returns the counters current value.
        /// </summary>
        /// <returns> The counters current value. </returns>
        [Obsolete("Use Value instead. This method will be removed in 4.8.0 release candidate.")]
        public virtual long Get() => Value;

        /// <summary>
        /// Returns a new counter. The returned counter is not thread-safe.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Counter NewCounter()
        {
            return NewCounter(false);
        }

        /// <summary>
        /// Returns a new counter.
        /// </summary>
        /// <param name="threadSafe">
        ///          <c>true</c> if the returned counter can be used by multiple
        ///          threads concurrently. </param>
        /// <returns> A new counter. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Counter NewCounter(bool threadSafe)
        {
            return threadSafe ? (Counter)new AtomicCounter() : new SerialCounter();
        }

        /// <summary>
        /// Returns this counter's <see cref="Value"/> implicitly.
        /// </summary>
        /// <param name="counter"></param>
        public static implicit operator long(Counter counter) => counter.Value; // LUCENENET specific

        private sealed class SerialCounter : Counter
        {
            private long count = 0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long AddAndGet(long delta)
            {
                return count += delta;
            }

            public override long Value => count;
        }

        private sealed class AtomicCounter : Counter
        {
            private readonly AtomicInt64 count = new AtomicInt64();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long AddAndGet(long delta)
            {
                return count.AddAndGet(delta);
            }

            public override long Value => count;
        }
    }
}