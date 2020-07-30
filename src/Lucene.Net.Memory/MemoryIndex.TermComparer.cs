using System;
using System.Collections.Generic;

namespace Lucene.Net.Index.Memory
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

    public partial class MemoryIndex
    {
        private class TermComparer
        {
            /// <summary>
            /// Sorts term entries into ascending order; also works for
            /// <see cref="Array.BinarySearch{T}(T[], T, IComparer{T})"/> and 
            /// <see cref="Array.Sort{T}(T[], IComparer{T})"/>.
            /// </summary>
            public static int KeyComparer<TKey, TValue>(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
                where TKey : class, IComparable<TKey>
            {
                if (x.Key == y.Key) return 0;
                return typeof(TKey) == typeof(string)
                           ? string.Compare(x.Key as string, y.Key as string, StringComparison.Ordinal)
                           : x.Key.CompareTo(y.Key);
            }
        }

        private sealed class TermComparer<TKey, TValue> : TermComparer, IComparer<KeyValuePair<TKey, TValue>>
            where TKey : class, IComparable<TKey>
        {
            public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
            {
                return KeyComparer(x, y);
            }
        }
    }
}
