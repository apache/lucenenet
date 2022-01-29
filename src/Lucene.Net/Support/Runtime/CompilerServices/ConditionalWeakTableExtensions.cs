using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Runtime.CompilerServices
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

    internal static class ConditionalWeakTableExtensions
    {
#if !FEATURE_CONDITIONALWEAKTABLE_ADDORUPDATE
        /// <summary>
        /// AddOrUpdate-like patch for .NET Standard 2.0 and .NET Framework. Note this method is not threadsafe,
        /// so will require external locking to synchronize with other <see cref="ConditionalWeakTable{TKey, TValue}"/> operations.
        /// </summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="table">This <see cref="ConditionalWeakTable{TKey, TValue}"/>.</param>
        /// <param name="key">The key to add or update. May not be <c>null</c>.</param>
        /// <param name="value">The value to associate with key.</param>
        /// <exception cref="ArgumentNullException"><paramref name="table"/> or <paramref name="key"/> is <c>null</c>.</exception>
        public static void AddOrUpdate<TKey, TValue>(this ConditionalWeakTable<TKey, TValue> table, TKey key, TValue value)
            where TKey: class
            where TValue: class
        {
            if (table is null)
                throw new ArgumentNullException(nameof(table));

            if (table.TryGetValue(key, out _))
                table.Remove(key);
            table.Add(key, value);
        }
#endif
    }
}
