using Lucene.Net.Util;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Lucene.Net.Codecs
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
    /// A utility class to write missing values for SORTED as if they were the empty string
    /// (to simulate pre-Lucene4.5 dv behavior for testing old codecs).
    /// </summary>
    public static class MissingOrdRemapper // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        /// <summary>
        /// Insert an empty byte[] to the front of this enumerable.</summary>
        public static IEnumerable<BytesRef> InsertEmptyValue(IEnumerable<BytesRef> iterable)
        {
            return new IterableAnonymousClass(iterable);
        }

        private class IterableAnonymousClass : IEnumerable<BytesRef>
        {
            private readonly IEnumerable<BytesRef> iterable;

            public IterableAnonymousClass(IEnumerable<BytesRef> iterable)
            {
                this.iterable = iterable;
            }

            public IEnumerator<BytesRef> GetEnumerator()
            {
                return new IteratorAnonymousClass(this);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private class IteratorAnonymousClass : IEnumerator<BytesRef>
            {
                public IteratorAnonymousClass(IterableAnonymousClass outerInstance)
                {
                    seenEmpty = false;
                    @in = outerInstance.iterable.GetEnumerator();
                }

                private bool seenEmpty;
                private readonly IEnumerator<BytesRef> @in;
                private BytesRef current;

                public bool MoveNext()
                {
                    if (!seenEmpty)
                    {
                        seenEmpty = true;
                        current = new BytesRef();
                        return true;
                    }

                    if (@in.MoveNext())
                    {
                        current = @in.Current;
                        return true;
                    }

                    return false;
                }

                public BytesRef Current => current;

                object IEnumerator.Current => Current;

                public void Reset()
                    => throw new NotSupportedException();

                public void Dispose()
                     => @in.Dispose();
            }
        }

        /// <summary>
        /// Remaps ord -1 to ord 0 on this enumerable. </summary>
        public static IEnumerable<long?> MapMissingToOrd0(IEnumerable<long?> iterable)
        {
            return new IterableAnonymousClass2(iterable);
        }

        private class IterableAnonymousClass2 : IEnumerable<long?>
        {
            private readonly IEnumerable<long?> iterable;

            public IterableAnonymousClass2(IEnumerable<long?> iterable)
            {
                this.iterable = iterable;
            }

            public IEnumerator<long?> GetEnumerator()
            {
                return new IteratorAnonymousClass2(this);
            }

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            private class IteratorAnonymousClass2 : IEnumerator<long?>
            {
                public IteratorAnonymousClass2(IterableAnonymousClass2 outerInstance)
                {
                    @in = outerInstance.iterable.GetEnumerator();
                }

                private readonly IEnumerator<long?> @in;
                private long current;

                public bool MoveNext()
                {
                    if (!@in.MoveNext())
                    {
                        return false;
                    }

                    long n = @in.Current.Value;

                    current = n == -1 ? 0 : n;

                    return true;
                }

                public long? Current => current;

                object IEnumerator.Current => Current;

                public void Reset()
                    => throw new NotSupportedException();

                public void Dispose()
                    => @in.Dispose();
            }
        }

        /// <summary>
        /// Remaps every ord+1 on this enumerable. </summary>
        public static IEnumerable<long?> MapAllOrds(IEnumerable<long?> iterable)
        {
            return new IterableAnonymousClass3(iterable);
        }

        private class IterableAnonymousClass3 : IEnumerable<long?>
        {
            private readonly IEnumerable<long?> iterable;

            public IterableAnonymousClass3(IEnumerable<long?> iterable)
            {
                this.iterable = iterable;
            }

            public IEnumerator<long?> GetEnumerator()
            {
                return new IteratorAnonymousClass3(this);
            }

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            private class IteratorAnonymousClass3 : IEnumerator<long?>
            {
                public IteratorAnonymousClass3(IterableAnonymousClass3 outerInstance)
                {
                    @in = outerInstance.iterable.GetEnumerator();
                }

                private readonly IEnumerator<long?> @in;
                private long current;

                public bool MoveNext()
                {
                    if (!@in.MoveNext())
                    {
                        return false;
                    }

                    long n = @in.Current.Value;
                    current = n + 1;

                    return true;
                }

                public long? Current => current;

                object IEnumerator.Current => Current;

                public void Reset()
                    => throw new NotSupportedException();

                public void Dispose()
                    => @in.Dispose();
            }
        }
    }
}