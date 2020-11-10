using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
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
    /// An <see cref="IEnumerator{T}"/> implementation that filters elements with a boolean predicate. </summary>
    // LUCENENET specific - simplifed the logic, as this is much easier to do in .NET
    public sealed class FilterEnumerator<T> : IEnumerator<T>
    {
        private readonly IEnumerator<T> iter;
        private readonly Predicate<T> predicateFunction;
        private T current;

        /// <summary>
        /// Initializes a new instance of <see cref="FilterEnumerator{T}"/> with the specified <paramref name="baseEnumerator"/> and <paramref name="predicateFunction"/>.
        /// </summary>
        /// <param name="baseEnumerator"></param>
        /// <param name="predicateFunction">Returns <c>true</c>, if this element should be set to <see cref="Current"/> by <see cref="MoveNext()"/>.</param>
        public FilterEnumerator(IEnumerator<T> baseEnumerator, Predicate<T> predicateFunction)
        {
            this.iter = baseEnumerator ?? throw new ArgumentNullException(nameof(baseEnumerator));
            this.predicateFunction = predicateFunction ?? throw new ArgumentNullException(nameof(predicateFunction));
            current = default;
        }

        public bool MoveNext()
        {
            while (iter.MoveNext())
            {
                if (predicateFunction(iter.Current))
                {
                    current = iter.Current;
                    return true;
                }
            }
            current = default;
            return false;
        }

        // LUCENENET specific - seems logical to call reset on the underlying implementation
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            current = default;
            iter.Reset();
        }

        public T Current => current;

        object System.Collections.IEnumerator.Current => current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => iter.Dispose();
    }

    /// <summary>
    /// An <see cref="IEnumerator{T}"/> implementation that filters elements with a boolean predicate. </summary>
    /// <seealso cref="PredicateFunction(T)"/>
    [Obsolete("Use FilterEnumerator<T> instead. This class will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public abstract class FilterIterator<T> : IEnumerator<T>
    {
        private readonly IEnumerator<T> iter;
        private T next = default;
        private bool nextIsSet = false;
        private T current = default;

        /// <summary>
        /// Returns <c>true</c>, if this element should be set to <see cref="Current"/> by <see cref="SetNext()"/>. </summary>
        protected abstract bool PredicateFunction(T @object);

        protected FilterIterator(IEnumerator<T> baseIterator) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
            this.iter = baseIterator;
        }

        public bool MoveNext()
        {
            if (!(nextIsSet || SetNext()))
            {
                return false;
            }

            if (Debugging.AssertsEnabled) Debugging.Assert(nextIsSet);
            try
            {
                current = next;
            }
            finally
            {
                nextIsSet = false;
                next = default;
            }
            return true;
        }

        // LUCENENET specific - seems logical to call reset on the underlying implementation
        public void Reset()
        {
            iter.Reset();
        }

        private bool SetNext()
        {
            while (iter.MoveNext())
            {
                T @object = iter.Current;
                if (PredicateFunction(@object))
                {
                    next = @object;
                    nextIsSet = true;
                    return true;
                }
            }
            return false;
        }

        public T Current => current;

        object System.Collections.IEnumerator.Current => Current;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}