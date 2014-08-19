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

namespace Lucene.Net.Util
{
    using System;
    using System.Reflection;
    using System.Threading;

    // ReSharper disable CSharpWarnings::CS1574
    // ReSharper disable once CSharpWarnings::CS1584
    /// <summary>
    ///     Replacement for Java's CloseableThreadLocal. Java's ThreadLocal keeps objects alive even after a thread dies.
    ///     CloseableThreadLocal was created in order to purge objects that are still in memory even though the
    ///     threads are already dead.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         .NET's <see cref="System.Threading.ThreadLocal{T}" /> implements <see cref="IDisposable" />,
    ///         which is similar to Closeable in Java.  The point of Lucene's CloseableThreadLocal is not that it
    ///         is disposable, but that it purges objects for dead threads rather than waiting for Java's garbage
    ///         collection to do it.
    ///     </para>
    ///     <para>

    ///         PCL, portable class libraries, currently does not support the <see cref="System.Threading.Thread" />
    ///         class. In order to get around this, <see cref="IPurgeStrategy" /> was created so that different
    ///         purge strategies can be swapped out.  The default strategy can't investigate threads, so the
    ///         default <see cref="PclPurgeStrategy" /> instead clears all internal hard references, calls
    ///         <see cref="GC.Collect()" />, gives <see cref="GC" /> time to work,
    ///         and then determines which references are dead and purges them. It would be a good idea to dispose
    ///         of any <see cref="System.Threading.Tasks.Task" />
    ///     </para>
    ///     <para>
    ///         The default strategy can be replaced with one similar to Java Version's that loops through threads
    ///         to see which threads are dead and purge the related objects. <see cref="IPurgeStrategy.AddAsync" />
    ///         is purposely instructed to run synchronously so that a strategy can get the current thread when
    ///         a value is added.
    ///     </para>
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class PurgeableThreadLocal<T> : IDisposable
    {
        private readonly bool isValueType;
        public IPurgeStrategy PurgeStrategy = null;
        private bool isDisposed;
        private ThreadLocal<WeakReference> threadLocal;


        /// <summary>
        ///     Initializes static members of the <see cref="PurgeableThreadLocal{T}" /> class.
        /// </summary>
        static PurgeableThreadLocal()
        {
            DefaultPurgeStrategy = new PclPurgeStrategy();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PurgeableThreadLocal{T}" /> class.
        /// </summary>
        /// <param name="valueFactory">The value factory.</param>
        /// <param name="trackAllValues">if set to <c>true</c> [track all values].</param>
        public PurgeableThreadLocal(Func<T> valueFactory = null, bool trackAllValues = false)
        {
            this.isValueType = typeof (T).GetTypeInfo().IsValueType;
            this.PurgeStrategy = DefaultPurgeStrategy;
            if (valueFactory != null)
            {
                this.threadLocal = new ThreadLocal<WeakReference>(() =>
                {
                    var value = valueFactory();
                    if (this.PurgeStrategy != null)
                        this.PurgeStrategy.AddAsync(value).RunSynchronously();

                    return new WeakReference(value, trackAllValues);
                });
                return;
            }

            this.threadLocal = new ThreadLocal<WeakReference>(trackAllValues);
        }

        /// <summary>
        ///     Gets or sets the default purge strategy.
        /// </summary>
        /// <value>The default purge strategy.</value>
        public static IPurgeStrategy DefaultPurgeStrategy { get; set; }

        /// <summary>
        ///     Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public T Value
        {
            get
            {
                this.CheckDisposed();
                var reference = this.threadLocal.Value;
                if (reference == null || !reference.IsAlive)
                {
                    T value = default(T);

                    // check for value type. 
                    // ReSharper disable once CompareNonConstrainedGenericWithNull
                    if (this.isValueType || value != null)
                        this.Set(value);

                    return value;
                }


                this.Purge();
                return (T) reference.Target;
            }
            set { this.Set(value); }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether this instance is alive.
        /// </summary>
        /// <value><c>true</c> if this instance is alive; otherwise, <c>false</c>.</value>
        public bool IsAlive
        {
            get
            {
                this.CheckDisposed();
                WeakReference reference = this.threadLocal.Value;
                return reference != null && reference.IsAlive;
            }
            // ReSharper disable once ValueParameterNotUsed
            set
            {
                this.CheckDisposed();
                if (value == false)
                    this.threadLocal.Value = null;
            }
        }


        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            this.Dispose(true);
        }

        /// <summary>
        ///     Purges this instance.
        /// </summary>
        protected void Purge()
        {
            if (this.PurgeStrategy != null)
                this.PurgeStrategy.PurgeAsync();
        }

        private void Set(T value)
        {
            this.CheckDisposed();
            this.threadLocal.Value = new WeakReference(value);
            if (this.PurgeStrategy != null)
                this.PurgeStrategy.AddAsync(value).RunSynchronously();

            this.Purge();
        }

        /// <summary>
        ///     Checks to see if <see cref="PurgeableThreadLocal{T}" /> has already been disposed.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException"></exception>
        private void CheckDisposed()
        {
            if (this.isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
        }

        /// <summary>
        ///     Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose">
        ///     <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only
        ///     unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool dispose)
        {
            if (this.isDisposed)
                return;

            if (this.PurgeStrategy != null)
            {
                this.PurgeStrategy.Dispose();
                this.PurgeStrategy = null;
            }

            this.threadLocal.Dispose();
            this.threadLocal = null;
            this.isDisposed = true;
        }

        /// <summary>
        ///     Finalizes an instance of the <see cref="PurgeableThreadLocal{T}" /> class.
        /// </summary>
        ~PurgeableThreadLocal()
        {
            this.Dispose(false);
        }
    }
}