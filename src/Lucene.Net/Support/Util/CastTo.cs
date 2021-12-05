using System;
using System.Linq.Expressions;

namespace Lucene.Net.Util
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
    /// Class to cast to type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Target type</typeparam>
    internal static class CastTo<T>
    {
        /// <summary>
        /// Casts <paramref name="s"/> to <typeparamref name="T"/>.
        /// This does not cause boxing for value types.
        /// Useful in generic methods.
        /// </summary>
        /// <typeparam name="TSource">Source type to cast from. Usually a generic type.</typeparam>
        public static T From<TSource>(TSource s)
        {
            return Cache<TSource>.caster(s);
        }

        private static class Cache<TSource>
        {
            public static readonly Func<TSource, T> caster = Get();

            private static Func<TSource, T> Get()
            {
                var p = Expression.Parameter(typeof(TSource));
                var c = Expression.ConvertChecked(p, typeof(T));
                return Expression.Lambda<Func<TSource, T>>(c, p).Compile();
            }
        }
    }
}
