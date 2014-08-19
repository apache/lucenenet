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
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    // ReSharper disable CSharpWarnings::CS1574

    /// <summary>
    /// Default <see cref="IPurgeStrategy"/> for <see cref="PurgeableThreadLocal{T}"/> that 
    /// uses <see cref="GC"/> and <see cref="WeakReference"/>s to determine which objects
    /// should be purged.
    /// </summary>
    public class PclPurgeStrategy : IPurgeStrategy
    {
        private readonly List<object> hardReferences = new List<object>();
        private readonly List<WeakReference> weakReferences = new List<WeakReference>(); 
        private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);

        // Increase this to decrease frequency of purging in get:
        private const int PurgeMultiplier = 20;

        // On each get or set we decrement this; when it hits 0 we
        // purge.  After purge, we set this to
        // PURGE_MULTIPLIER * stillAliveCount.  this keeps
        // amortized cost of purging linear.
        private int countUntilPurge = PurgeMultiplier;


        /// <summary>
        /// Add the value asynchronously.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns><see cref="Task" /></returns>
        public Task AddAsync(object value)
        {
            return new Task(async () =>
            {
                await this.semaphoreSlim.WaitAsync();

                try
                {
                    this.hardReferences.Add(value);
                    this.weakReferences.Add(new WeakReference(value));
                }
                finally
                {
                    this.semaphoreSlim.Release();
                }
            });

        }

        /// <summary>
        /// Purges the object references asynchronously.
        /// </summary>
        /// <returns><see cref="Task" /></returns>
        public async Task PurgeAsync()
        {
            if (Interlocked.Decrement(ref this.countUntilPurge) != 0) 
                return;

            await this.semaphoreSlim.WaitAsync();
            try
            {
                var stillAliveCount = 0;
                
                this.hardReferences.Clear();

                GC.Collect();

                // let GC do it's thing.
                await Task.Delay(1000);

                foreach (var reference in this.weakReferences)
                {
                    if (!reference.IsAlive) 
                        continue;

                    this.hardReferences.Add(reference);
                    stillAliveCount++;
                }

                var nextCount = (1 + stillAliveCount)*PurgeMultiplier;
                if (nextCount <= 0)
                {
                    // defensive: int overflow!
                    nextCount = 1000000;
                }

                Interlocked.Exchange(ref this.countUntilPurge, nextCount);

            }
            finally
            {
                this.semaphoreSlim.Release();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            this.Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            this.semaphoreSlim.Dispose();
            this.hardReferences.Clear();
            this.weakReferences.Clear();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="PclPurgeStrategy"/> class.
        /// </summary>
        ~PclPurgeStrategy()
        {
            this.Dispose(false);
        }

    }
}
