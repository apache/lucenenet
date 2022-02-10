using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using JCG = J2N.Collections.Generic;

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
    /// .NET's built-in <see cref="ThreadLocal{T}"/> has a serious flaw:
    /// internally, it creates an array with an internal lattice structure
    /// which in turn causes the garbage collector to cause long blocking pauses
    /// when tearing the structure down. See
    /// <a href="https://ayende.com/blog/189761-A/production-postmortem-the-slow-slowdown-of-large-systems">
    /// https://ayende.com/blog/189761-A/production-postmortem-the-slow-slowdown-of-large-systems</a>
    /// for a more detailed explanation.
    /// <para/>
    /// This is a completely different problem than in Java which the ClosableThreadLocal&lt;T&gt; class is
    /// meant to solve, so <see cref="DisposableThreadLocal{T}"/> is specific to Lucene.NET and can be used
    /// as a direct replacement for ClosableThreadLocal&lt;T&gt;.
    /// <para/>
    /// This class works around the issue by using an alternative approach than using <see cref="ThreadLocal{T}"/>.
    /// It keeps track of each thread's local and global state in order to later optimize garbage collection.
    /// A complete explanation can be found at 
    /// <a href="https://ayende.com/blog/189793-A/the-design-and-implementation-of-a-better-threadlocal-t">
    /// https://ayende.com/blog/189793-A/the-design-and-implementation-of-a-better-threadlocal-t</a>.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    /// <typeparam name="T">Specifies the type of data stored per-thread.</typeparam>
    public sealed class DisposableThreadLocal<T> : IDisposable
    {
        [ThreadStatic]
        private static CurrentThreadState _state;

        private readonly WeakReferenceCompareValue<DisposableThreadLocal<T>> selfReference;
        private ConcurrentDictionary<WeakReferenceCompareValue<CurrentThreadState>, T> _values = new ConcurrentDictionary<WeakReferenceCompareValue<CurrentThreadState>, T>();
        private readonly Func<T> _valueFactory;
        private bool _disposed;
        private static int globalVersion;

        /// <summary>
        /// Initializes the <see cref="DisposableThreadLocal{T}"/> instance.
        /// </summary>
        /// <remarks>
        /// The default value of <typeparamref name="T"/> is used to initialize
        /// the instance when <see cref="Value"/> is accessed for the first time.
        /// </remarks>
        public DisposableThreadLocal()
        {
            selfReference = new WeakReferenceCompareValue<DisposableThreadLocal<T>>(this);
        }

        /// <summary>
        /// Initializes the <see cref="DisposableThreadLocal{T}"/> instance with the
        /// specified <paramref name="valueFactory"/> function.
        /// </summary>
        /// <param name="valueFactory">The <see cref="Func{T, TResult}"/> invoked to produce a
        /// lazily-initialized value when an attempt is made to retrieve <see cref="Value"/>
        /// without it having been previously initialized.</param>
        /// <exception cref="ArgumentNullException"><paramref name="valueFactory"/> is <c>null</c>.</exception>
        public DisposableThreadLocal(Func<T> valueFactory)
        {
            _valueFactory = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));
            selfReference = new WeakReferenceCompareValue<DisposableThreadLocal<T>>(this);
        }

        /// <summary>
        /// Gets a collection for all of the values currently stored by all of the threads that have accessed this instance.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The <see cref="DisposableThreadLocal{T}"/> instance has been disposed.</exception>
        public ICollection<T> Values
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_disposed)
                    throw AlreadyClosedException.Create(nameof(DisposableThreadLocal<T>), message: null);
                return _values.Values;
            }
        }

        /// <summary>
        /// Gets whether Value is initialized on the current thread.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The <see cref="DisposableThreadLocal{T}"/> instance has been disposed.</exception>
        public bool IsValueCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_disposed)
                    throw AlreadyClosedException.Create(nameof(DisposableThreadLocal<T>), message: null);

                return _state != null && _values.ContainsKey(_state.selfReference);
            }
        }

        [Obsolete("Use Value instead. This method will be removed in 4.8.0 release candidate.")]
        public T Get() => Value;

        [Obsolete("Use Value instead. This method will be removed in 4.8.0 release candidate.")]
        public void Set(T value) => Value = value;

        /// <summary>
        /// Gets or sets the value of this instance for the current thread.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The <see cref="DisposableThreadLocal{T}"/> instance has been disposed.</exception>
        /// <remarks>
        /// If this instance was not previously initialized for the current thread, accessing Value will attempt to
        /// initialize it. If an initialization function was supplied during the construction, that initialization
        /// will happen by invoking the function to retrieve the initial value for <see cref="Value"/>. Otherwise, the default
        /// value of <typeparamref name="T"/> will be used.
        /// </remarks>
        public T Value
        {
            get
            {
                if (_disposed)
                    throw AlreadyClosedException.Create(nameof(DisposableThreadLocal<T>), message: null);
                (_state ??= new CurrentThreadState()).Register(this);
                if (_values.TryGetValue(_state.selfReference, out var v) == false &&
                    _valueFactory != null)
                {
                    v = _valueFactory();
                    _values[_state.selfReference] = v;
                }

                return v;
            }
            set
            {
                if (_disposed)
                    throw AlreadyClosedException.Create(nameof(DisposableThreadLocal<T>), message: null);

                (_state ??= new CurrentThreadState()).Register(this);
                _values[_state.selfReference] = value;
            }
        }

        /// <summary>
        /// Releases the resources used by this <see cref="DisposableThreadLocal{T}"/> instance.
        /// </summary>
        public void Dispose()
        {
            var copy = _values;
            if (copy is null)
                return;

            copy = Interlocked.CompareExchange(ref _values, null, copy);
            if (copy is null)
                return;

            Interlocked.Increment(ref globalVersion);
            _disposed = true;
            _values = null;
        }

        private sealed class CurrentThreadState
        {
            private readonly HashSet<WeakReferenceCompareValue<DisposableThreadLocal<T>>> _parents
                = new HashSet<WeakReferenceCompareValue<DisposableThreadLocal<T>>>();

            public readonly WeakReferenceCompareValue<CurrentThreadState> selfReference;

            private readonly LocalState _localState = new LocalState();

            public CurrentThreadState()
            {
                selfReference = new WeakReferenceCompareValue<CurrentThreadState>(this);
            }

            public void Register(DisposableThreadLocal<T> parent)
            {
                _parents.Add(parent.selfReference);
                int localVersion = _localState.localVersion;
                var globalVersion = DisposableThreadLocal<T>.globalVersion;
                if (localVersion != globalVersion)
                {
                    // a thread local instance was disposed, let's check
                    // if we need to do cleanup here
                    RemoveDisposedParents();
                    _localState.localVersion = globalVersion;
                }
            }

            private void RemoveDisposedParents()
            {
                var toRemove = new JCG.List<WeakReferenceCompareValue<DisposableThreadLocal<T>>>();
                foreach (var local in _parents)
                {
                    if (local.TryGetTarget(out var target) == false || target._disposed)
                    {
                        toRemove.Add(local);
                    }
                }

                foreach (var remove in toRemove)
                {
                    _parents.Remove(remove);
                }
            }

            ~CurrentThreadState()
            {
                foreach (var parent in _parents)
                {
                    if (parent.TryGetTarget(out var liveParent) == false)
                        continue;

                    var copy = liveParent._values;
                    if (copy is null)
                        continue;
                    copy.TryRemove(selfReference, out _);
                }
            }
        }

        private sealed class WeakReferenceCompareValue<TK> : IEquatable<WeakReferenceCompareValue<TK>>
            where TK : class
        {
            private readonly WeakReference<TK> _weak;
            private readonly int _hashCode;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGetTarget(out TK target)
            {
                return _weak.TryGetTarget(out target);
            }

            public WeakReferenceCompareValue(TK instance)
            {
                _hashCode = instance.GetHashCode();
                _weak = new WeakReference<TK>(instance);
            }

            public bool Equals(WeakReferenceCompareValue<TK> other)
            {
                if (other is null)
                    return false;
                if (ReferenceEquals(this, other))
                    return true;
                if (_hashCode != other._hashCode)
                    return false;
                if (_weak.TryGetTarget(out var x) == false ||
                    other._weak.TryGetTarget(out var y) == false)
                    return false;
                return ReferenceEquals(x, y);
            }

            public override bool Equals(object obj)
            {
                if (obj is null)
                    return false;
                if (ReferenceEquals(this, obj))
                    return true;
                if (obj.GetType() == typeof(TK))
                {
                    int hashCode = obj.GetHashCode();
                    if (hashCode != _hashCode)
                        return false;
                    if (_weak.TryGetTarget(out var other) == false)
                        return false;
                    return ReferenceEquals(other, obj);
                }
                if (obj.GetType() != GetType())
                    return false;
                return Equals((WeakReferenceCompareValue<TK>)obj);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode()
            {
                return _hashCode;
            }
        }

        private sealed class LocalState
        {
            public int localVersion;
        }
    }
}