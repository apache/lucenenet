// -----------------------------------------------------------------------
// <copyright file="WeakKeyReferenceOfT.cs" company="Apache">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;


    /// <summary>
    ///     A reference to the weak <c>TKey</c> reference in the <see cref="WeakDictionary{TKey,TValue}"/>
    ///     that will hold reference to the key even if the target is GC'ed (Garbage Collected), 
    ///     so that the dictionary can still find the key to remove the dead reference.
    /// </summary>
    /// <typeparam name="T">The <typeparamref name="T"/> type.</typeparam>
    /// <remarks>
    ///   <note>
    ///        <para>
    ///             <b>C# File: </b> <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/src/Lucene.Net/Support/WeakKeyReferenceOfT.cs">
    ///              src/Lucene.Net/Support/WeakKeyReferenceOfT.cs
    ///             </a>
    ///         </para>
    ///         <para>
    ///             <b>C# Tests: </b>  <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/test/Lucene.Net.Test/Util/Support/WeakKeyReferenceOfTTest.cs">
    ///             test/Lucene.Net.Test/Support/WeakKeyReferenceOfTTest.cs
    ///             </a>
    ///         </para>
    ///     </note>
    /// </remarks>
    internal sealed class WeakKeyReference<T> : WeakReference<T>
        where T : class
    {
        /// <summary>
        /// The hashcode of the <typeparamref name="T"/> key.
        /// </summary>
        public readonly int HashCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="WeakKeyReference&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="comparer">The comparer.</param>
        public WeakKeyReference(T key, WeakKeyComparer<T> comparer)
            : base(key)
        {
            // retain the object's hash code immediately so that even
            // if the target is GC'ed we will be able to find and
            // remove the dead weak reference.
            this.HashCode = comparer.GetHashCode(key);
        }
    }
}
