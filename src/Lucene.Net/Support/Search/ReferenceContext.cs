using System;

namespace Lucene.Net.Search
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
    /// <see cref="ReferenceContext{T}"/> holds a reference instance and
    /// ensures it is properly de-referenced from its corresponding <see cref="ReferenceManager{G}"/>
    /// when <see cref="Dispose()"/> is called. This class is primarily intended
    /// to be used with a using block.
    /// <para/>
    /// LUCENENET specific
    /// </summary>
    /// <typeparam name="T">The reference type</typeparam>
    public sealed class ReferenceContext<T> : IDisposable
        where T : class
    {
        private readonly ReferenceManager<T> referenceManager;
        private T reference;

        internal ReferenceContext(ReferenceManager<T> referenceManager)
        {
            this.referenceManager = referenceManager;
            this.reference = referenceManager.Acquire();
        }

        /// <summary>
        /// The reference acquired from the <see cref="ReferenceManager{G}"/>.
        /// </summary>
        public T Reference => reference;

        /// <summary>
        /// Ensures the reference is properly de-referenced from its <see cref="ReferenceManager{G}"/>.
        /// After this call, <see cref="Reference"/> will be <c>null</c>.
        /// </summary>
        public void Dispose()
        {
            if (this.reference != null)
            {
                this.referenceManager.Release(this.reference);
                this.reference = null;
            }
        }
    }
}
