// Some code adapted from Apache Harmony: https://github.com/apache/harmony/blob/02970cb7227a335edd2c8457ebdde0195a735733/classlib/modules/concurrent/src/main/java/java/util/concurrent/CountDownLatch.java

#region Copyright 2010 by Apache Harmony, Licensed under the Apache License, Version 2.0
/*  Licensed to the Apache Software Foundation (ASF) under one or more
 *  contributor license agreements.  See the NOTICE file distributed with
 *  this work for additional information regarding copyright ownership.
 *  The ASF licenses this file to You under the Apache License, Version 2.0
 *  (the "License"); you may not use this file except in compliance with
 *  the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */
#endregion

// Other aspects adapted from .NET's CountdownEvent: https://github.com/dotnet/runtime/blob/38496302e54e1b6fb11a998b297492f3fdfbfd0c/src/libraries/System.Threading/src/System/Threading/CountdownEvent.cs

using System;
using System.Threading;

namespace Lucene.Net.Support.Threading
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
    /// A synchronization aid that allows one or more threads to wait until
    /// a set of operations being performed in other threads completes.
    /// <para/>
    /// A <see cref="CountDownLatch"/> is initialized with a given <em>count</em>.
    /// The <see cref="Await()"/> methods block until the current count reaches
    /// zero due to invocations of the <see cref="CountDown()"/> method, after which
    /// all waiting threads are released and any subsequent invocations of
    /// <see cref="Await()"/> return immediately. This is a one-shot phenomenon:
    /// the count cannot be reset.
    /// <para/>
    /// A <see cref="CountDownLatch"/> is a versatile synchronization tool
    /// and can be used for a number of purposes. A <see cref="CountDownLatch"/>
    /// initialized with a count of one serves as a simple on/off latch, or gate:
    /// all threads invoking <see cref="Await()"/> wait at the gate until it is
    /// opened by a thread invoking <see cref="CountDown()"/>. A
    /// <see cref="CountDownLatch"/> initialized to <em>N</em> can be used to make
    /// one thread wait until <em>N</em> threads have completed some action, or
    /// some action has been completed N times.
    /// <para/>
    /// A useful property of a <see cref="CountDownLatch"/> is that it
    /// doesn't require that threads calling <see cref="CountDown()"/> wait for
    /// the count to reach zero before proceeding, it simply prevents any
    /// thread from proceeding past an <see cref="Await()"/> until all
    /// threads could pass.
    /// <para/>
    /// LUCENENET specific: This type is similar to <see cref="CountdownEvent"/>,
    /// but unlike <see cref="CountdownEvent.Signal()"/>, <see cref="CountDown()"/>
    /// can be called any number of times after the count reaches zero without
    /// throwing. This matches Java's <c>CountDownLatch.countDown()</c> semantics.
    /// </summary>
    internal class CountDownLatch
    {
        // LUCENENET: the current count and ManualResetEventSlim for signaling, instead of Java's Sync class
        private volatile int _currentCount;
        private readonly ManualResetEventSlim _event;

        /// <summary>
        /// Constructs a <see cref="CountDownLatch"/> initialized with the given <paramref name="count"/>.
        /// </summary>
        /// <param name="count">The number of times <see cref="CountDown()"/> must be invoked
        /// before threads can pass through <see cref="Await()"/></param>
        /// <exception cref="ArgumentOutOfRangeException">if <paramref name="count"/> is negative</exception>
        public CountDownLatch(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "count < 0");
            }

            _currentCount = count;
            _event = new ManualResetEventSlim(count == 0);
        }

        /// <summary>
        /// Causes the current thread to wait until the latch has counted down to
        /// zero.
        /// <para/>
        /// If the current count is zero then this method returns immediately.
        /// <para/>
        /// If the current count is greater than zero then the current
        /// thread becomes disabled for thread scheduling purposes and lies
        /// dormant until the count reaches zero due to invocations of the
        /// <see cref="CountDown()"/> method.
        /// <para/>
        /// LUCENENET specific: Unlike Java's <c>await()</c>, which throws
        /// <c>InterruptedException</c> if the thread is interrupted while
        /// waiting, this overload blocks unconditionally until the count
        /// reaches zero. Callers that need cancellation/interrupt support
        /// should use the timeout overload or a higher-level coordination
        /// primitive.
        /// </summary>
        public void Await()
        {
            _event.Wait(Timeout.Infinite);
        }

        /// <summary>
        /// Causes the current thread to wait until the latch has counted down to
        /// zero, or the specified waiting time elapses.
        /// <para/>
        /// If the current count is zero then this method returns immediately
        /// with the value <c>true</c>.
        /// <para/>
        /// If the current count is greater than zero then the current
        /// thread becomes disabled for thread scheduling purposes and lies
        /// dormant until one of two things happen:
        /// <list type="bullet">
        ///     <item><description>The count reaches zero due to invocations of the
        ///         <see cref="CountDown()"/> method; or</description></item>
        ///     <item><description>The specified waiting time elapses.</description></item>
        /// </list>
        /// <para/>
        /// If the count reaches zero then the method returns with the
        /// value <c>true</c>.
        /// <para/>
        /// If the specified waiting time elapses then the value <c>false</c>
        /// is returned. If the time is less than or equal to zero, the method
        /// will not wait at all.
        /// </summary>
        /// <param name="timeout">The maximum time to wait.</param>
        /// <returns><c>true</c> if the count reached zero, or <c>false</c>
        /// if the waiting time elapsed before the count reached zero.</returns>
        public bool Await(TimeSpan timeout)
        {
            return _event.Wait(timeout);
        }

        /// <summary>
        /// Decrements the count of the latch, releasing all waiting threads if
        /// the count reaches zero.
        /// <para />
        /// If the current count is greater than zero then it is decremented.
        /// If the new count is zero then all waiting threads are re-enabled for
        /// thread scheduling purposes.
        /// <para />
        /// If the current count equals zero then nothing happens.
        /// </summary>
        public void CountDown()
        {
            if (_currentCount <= 0)
            {
                // Try to avoid unnecessary decrementing of the count below zero,
                // but a couple concurrent races below zero are fine
                return;
            }

            int newCount = Interlocked.Decrement(ref _currentCount);
            if (newCount <= 0)
            {
                _event.Set();
            }
        }

        /// <summary>
        /// Returns the current count.
        /// <para/>
        /// This property is typically used for debugging and testing purposes.
        /// </summary>
        /// <remarks>
        /// In Java, <c>getCount()</c> returns <c>long</c>, but internally it's
        /// always an int. Here we retain the Java return type. There is no need
        /// to make the internal count 64-bit.
        /// <para/>
        /// The returned value is clamped at zero. The internal counter may
        /// briefly drop below zero under concurrent <see cref="CountDown()"/>
        /// calls that race past the early-return guard, but that detail is
        /// hidden here to match Java's <c>getCount()</c> semantics, which never
        /// returns a negative value.
        /// </remarks>
        public long Count => Math.Max(0, _currentCount);

        /// <summary>
        /// Returns a string identifying this latch, as well as its state.
        /// The state, in brackets, includes the String <c>"Count ="</c>
        /// followed by the current count.
        /// </summary>
        /// <returns>a string identifying this latch, as well as its state</returns>
        public override string ToString() => $"{base.ToString()}[Count = {_currentCount}]";
    }
}
