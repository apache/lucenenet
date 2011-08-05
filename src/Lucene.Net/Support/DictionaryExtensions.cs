// -----------------------------------------------------------------------
// <copyright file="DictionaryExtensions.cs" company="Apache">
//
//      Licensed to the Apache Software Foundation (ASF) under one or more
//      contributor license agreements.  See the NOTICE file distributed with
//      this work for additional information regarding copyright ownership.
//      The ASF licenses this file to You under the Apache License, Version 2.0
//      (the "License"); you may not use this file except in compliance with
//      the License.  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.
//
// </copyright>
// -----------------------------------------------------------------------

namespace Lucene.Net.Support
{
    using System.Collections.Generic;

    /// <summary>
    /// Extension methods for <see cref="IDictionary{TKey,TValue}"/>
    /// </summary>
    /// <remarks>
    ///     <note>
    ///        <para>
    ///             <b>C# File: </b> <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/src/Lucene.Net/Support/DictionaryExtensions.cs">
    ///              src/Lucene.Net/Support/DictionaryExtensions.cs
    ///             </a>
    ///         </para>
    ///         <para>
    ///             <b>C# Tests: </b>  <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/test/Lucene.Net.Test/Util/Support/DictionaryExtensionsTest.cs">
    ///             test/Lucene.Net.Test/Support/DictionaryExtensionsTest.cs
    ///             </a>
    ///         </para>
    ///     </note>
    /// </remarks>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Gets the associated value of the specified key if it exists, otherwise it returns <c>default(<typeparamref name="TValue"/>)</c>.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        /// <returns>the <typeparamref name="TValue"/> or default value associated with the specified key.</returns>
        public static TValue GetDefaultedValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue value;
            dictionary.TryGetValue(key, out value);

            return value;
        }       
    }
}
