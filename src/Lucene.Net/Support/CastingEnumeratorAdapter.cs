using System.Collections;
using System.Collections.Generic;

namespace Lucene.Net.Support
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
    /// LUCENENET specific struct used to adapt an <see cref="IEnumerator{T}"/> to one with a different type parameter.
    /// </summary>
    /// <typeparam name="T">The type of elements in the original enumerator.</typeparam>
    /// <typeparam name="U">The type of elements in the adapted enumerator.</typeparam>
    internal readonly struct CastingEnumeratorAdapter<T, U> : IEnumerator<U>
        where T : U
    {
        private readonly IEnumerator<T> enumerator;

        public CastingEnumeratorAdapter(IEnumerator<T> enumerator)
        {
            this.enumerator = enumerator;
        }

        public U Current => enumerator.Current;

        object IEnumerator.Current => enumerator.Current;

        public bool MoveNext() => enumerator.MoveNext();

        public void Reset() => enumerator.Reset();

        public void Dispose() => enumerator.Dispose();
    }
}
