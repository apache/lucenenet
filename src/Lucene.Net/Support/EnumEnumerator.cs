using System;
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
    /// Helper class to facilitate dotnet enumerables
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EnumEnumerator<T> : IEnumerator<T>, IEnumerable<T>
    {
        public static EnumEnumerator<T> CreateWithCapturedNext(Func<T> next)
        {
            T current = default(T);
            return new EnumEnumerator<T>(() => { current = next(); return current != null; }, () => current);
        }

        private readonly Func<bool> next;
        private readonly Action dispose;
        private readonly Func<T> currentFactory;

        private bool started = false;

        public EnumEnumerator(Func<bool> next, Func<T> currentFactory, Action dispose = null)
        {
            this.next = next;
            this.dispose = dispose;
            this.currentFactory = currentFactory;
        }

        public T Current => started ? currentFactory() : default(T);

        object IEnumerator.Current => Current;

        public bool MoveNext() => (started = next());

        public void Reset() => throw new NotImplementedException();

        public void Dispose() => dispose?.Invoke();

        public IEnumerator<T> GetEnumerator() => this;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}