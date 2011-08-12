// -----------------------------------------------------------------------
// <copyright company="Apache" file="ThreadLocalOfT.cs">
//
//      Licensed to the Apache Software Foundation (ASF) under one or more
//      contributor license agreements.  See the NOTICE file distributed with
//      this work for additional information regarding copyright ownership.
//      The ASF licenses this file to You under the Apache License, Version 2.0
//      (the "License"); you may not use this file except in compliance with
//      the License.  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.
//
// </copyright>
// -----------------------------------------------------------------------

namespace Lucene.Net.Support.Threading
{
    using System;


    /// <summary>
    /// Provides thread-local storage of data.
    /// </summary>
    /// <typeparam name="T">The type of data to be stored.</typeparam>
    public class ThreadLocal<T> : IDisposable
    {
        private LocalDataStoreSlot slot;
        private Exception cachedException;
        private bool disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadLocal&lt;T&gt;"/> class.
        /// </summary>
        public ThreadLocal()
        {
            this.Factory = () => {
                return default(T);
            };

            this.slot = ThreadData.AllocateDataSlot();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadLocal&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="valueFactory">The value factory.</param>
        public ThreadLocal(Func<T> valueFactory)
        {
            this.Factory = valueFactory;
            this.slot = ThreadData.AllocateDataSlot();
        }



        /// <summary>
        /// Gets or sets the value. 
        /// </summary>
        /// <remarks>
        ///     <para>
        ///     If the value will attempt to auto initialize using the supplied 
        ///     <see cref="Func{T}" /> <c>valueFactory</c> or default constructor.
        ///     If an exception is thrown, it is cached and rethrown each time this 
        ///     property is attempted to be accessed.
        ///     </para>
        /// </remarks>
        /// <value>The value.</value>
        /// <exception cref="ObjectDisposedException">
        ///     Thrown when <see cref="ThreadLocal{T}" /> is already disposed. 
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     The initialization function attempted to reference Value recursively.
        /// </exception>
        public T Value
        {
            get
            {
                if (this.disposed)
                    throw new ObjectDisposedException("This instance is already disposed");

                if (this.cachedException != null)
                    throw this.cachedException;
            
                return this.FetchValue();
            }

            set
            {
                if (this.disposed)
                    throw new ObjectDisposedException("This instance is already disposed");

                if (this.cachedException != null)
                    throw this.cachedException;

                var state = this.GetState();
                state.Initialized = true;
                state.FetchValue = () => value;
            }
        }


        /// <summary>
        /// Gets a value indicating whether the value has been created.
        /// </summary>
        /// <value>
        ///     <c>true</c> if the value is created; otherwise, <c>false</c>.
        /// </value>
        /// <exception cref="ObjectDisposedException">
        ///     Thrown when <see cref="ThreadLocal{T}" /> is already disposed. 
        /// </exception>
        public bool IsValueCreated
        {
            get
            {
                if (this.disposed)
                    throw new ObjectDisposedException("This instance is already disposed");

                if (this.cachedException != null)
                    throw this.cachedException;

                return this.IsThreadLocalInitialized;
            }
        }

        private Func<T> Factory { get; set; }

        private bool IsThreadLocalInitialized
        {
            get
            {
                var state = this.GetState();
                return state.Initialized;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Factory = null;
                ThreadData.FreeLocalSlotData(this.slot.SlotId, this.slot.IsThreadLocal);
                this.slot = null;
                this.disposed = true;
            }
        }

        private T FetchValue()
        {
            var state = this.GetState();
            if (state.Initializing)
                throw new InvalidOperationException("The initialization function attempted to reference Value recursively");

            return state.FetchValue();
        }

      

        private ValueState GetState()
        {
            var state = ThreadData.GetData(this.slot) as ValueState;
            if (state == null)
            {
                state = this.CreateState();
                ThreadData.SetData(this.slot, state);
            }

            return state;
        }

        private ValueState CreateState()
        {
            var factory = this.Factory;
            var state = new ValueState();
            state.FetchValue = () => {
                state.Initializing = true;
                try
                {
                    T value = factory();
                    state.Initializing = false;
                    state.Initialized = true;
                    state.FetchValue = () => { return value; };

                    return value;
                }
                catch (Exception ex)
                {
                    this.cachedException = ex;
                    throw;
                }
            };

            return state;
        }

        private class ValueState
        {
            public bool Initializing { get; set; }

            public bool Initialized { get; set; }

            public Func<T> FetchValue { get; set; }
        }
    }
}