using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Util
{
    /// <summary>
    /// Licensed to the Apache Software Foundation (ASF) under one or more
    /// contributor license agreements. See the NOTICE file distributed with this
    /// work for additional information regarding copyright ownership. The ASF
    /// licenses this file to You under the Apache License, Version 2.0 (the
    /// "License"); you may not use this file except in compliance with the License.
    /// You may obtain a copy of the License at
    ///
    /// http://www.apache.org/licenses/LICENSE-2.0
    ///
    /// Unless required by applicable law or agreed to in writing, software
    /// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
    /// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
    /// License for the specific language governing permissions and limitations under
    /// the License.
    /// </summary>

    /// <summary>
    /// An <seealso cref="Iterator"/> implementation that filters elements with a boolean predicate. </summary>
    /// <seealso cref= #predicateFunction </seealso>
    public abstract class FilterIterator<T> : IEnumerator<T>
    {
        private readonly IEnumerator<T> iter;
        private T next = default(T);
        private bool NextIsSet = false;
        private T current = default(T);

        /// <summary>
        /// returns true, if this element should be returned by <seealso cref="#next()"/>. </summary>
        protected abstract bool PredicateFunction(T @object);

        public FilterIterator(IEnumerator<T> baseIterator)
        {
            this.iter = baseIterator;
        }

        public bool MoveNext()
        {
            if (!(NextIsSet || SetNext()))
            {
                return false;
            }

            Debug.Assert(NextIsSet);
            try
            {
                current = next;
            }
            finally
            {
                NextIsSet = false;
                next = default(T);
            }
            return true;
        }

        public void Reset()
        {
            throw new NotImplementedException(); // LUCENENET TODO: Change to NotSupportedException
        }

        private bool SetNext()
        {
            while (iter.MoveNext())
            {
                T @object = iter.Current;
                if (PredicateFunction(@object))
                {
                    next = @object;
                    NextIsSet = true;
                    return true;
                }
            }
            return false;
        }

        public T Current
        {
            get { return current; }
        }

        object System.Collections.IEnumerator.Current
        {
            get { return Current; }
        }

        public void Dispose()
        {
        }
    }
}