/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Lucene.Net.Support;
using System;
using System.Diagnostics;
using System.Threading;

namespace Lucene.Net.Randomized
{
    /// <summary>
    /// A random with a delegate, preventing <see cref="System.Random."/>and locked
    /// to be used by a single thread. This is the equivelant to AssertRandom
    /// </summary>
    public class SingleThreadedRandom : Random, IDisposable
    {
        private Random @delegate;
        private readonly WeakReference ownerRef;
        private readonly string ownerName;
        private string trace;

        private volatile Boolean isDisposed = true;

        public SingleThreadedRandom(ThreadClass owner, Random @delegate)
            : base(0)
        {
            this.@delegate = @delegate;
            this.ownerRef = new WeakReference(owner);
            this.ownerName = owner.Name;
            this.trace = Environment.StackTrace;
        }

        public override int Next()
        {
            this.Guard();
            return this.@delegate.Next();
        }

        public override int Next(int maxValue)
        {
            this.Guard();
            return this.@delegate.Next(maxValue);
        }

        public override int Next(int minValue, int maxValue)
        {
            this.Guard();
            return this.@delegate.Next(minValue, maxValue);
        }

        public override void NextBytes(byte[] buffer)
        {
            this.Guard();
            this.@delegate.NextBytes(buffer);
        }

        public override double NextDouble()
        {
            this.Guard();
            return this.@delegate.NextDouble();
        }

        public override bool Equals(object obj)
        {
            this.Guard();
            return this.@delegate.Equals(obj);
        }

        public override int GetHashCode()
        {
            this.Guard();
            return this.@delegate.GetHashCode();
        }

        private void Guard()
        {
            /* checkValid(); */

            if (!this.isDisposed)
                throw new ObjectDisposedException(
                    "This instance of SingleThreadRandom has been disposed.  ");

            Thread owner = ownerRef.Target as Thread;

            if (owner == null || owner != Thread.CurrentThread)
            {
                var message = "The SingleThreadRandom instance was created for thread," + ownerName +
                                " and must not be shared.  The current thread is " + Thread.CurrentThread.Name + ".";

                throw new InvalidOperationException(message,
                    new IllegalStateException("The instance was illegally accessed\n" + this.trace));
            }
        }

        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    this.@delegate = null;
                    this.ownerRef.Target = null;
                }

                this.isDisposed = true;
            }
        }
    }
}