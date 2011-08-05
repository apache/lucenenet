// -----------------------------------------------------------------------
// <copyright file="WeakReferenceOfT.cs" company="Apache">
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
    ///     Represents a weak reference of <typeparamref name="T"/>, which references an object while still allowing
    ///     that object to be reclaimed by garbage collection.
    /// </summary>
    /// <remarks>
    ///    <note>
    ///        <para>
    ///             <b>C# File: </b> <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/src/Lucene.Net/Support/WeakReferenceOfT.cs">
    ///              src/Lucene.Net/Support/WeakReferenceOfTTest.cs
    ///             </a>
    ///         </para>
    ///         <para>
    ///             <b>C# Tests: </b>  <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/test/Lucene.Net.Test/Util/Support/WeakReferenceOfTTest.cs">
    ///             test/Lucene.Net.Test/Support/WeakReferenceOfTTest.cs
    ///             </a>
    ///         </para>
    ///     </note>
    ///     <para>
    ///         This was implemented to be the C# equivalent of has a Java WeakReference of T. It should be
    ///         noted that <a href="http://msdn.microsoft.com/en-us/library/gg712832%28v=vs.96%29.aspx">Silverlight 5 
    ///         already supports WeakReference of T</a>.
    ///     </para>
    /// </remarks>
    /// <typeparam name="T">The <typeparamref name="T"/> type.</typeparam>
    public class WeakReference<T> : WeakReference
        where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WeakReference&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="target">The target.</param>
        public WeakReference(T target)
            : base(target) 
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WeakReference&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="trackResurrection">if set to <c>true</c> [track resurrection].</param>
        public WeakReference(T target, bool trackResurrection)
            : base(target, trackResurrection)
        {
        }

        /// <summary>
        /// Gets the object (the target) referenced by the current <see cref="T:System.WeakReference"/> object.
        /// </summary>
        /// <value></value>
        /// <returns>null if the object referenced by the current <see cref="T:System.WeakReference"/> object has been garbage collected; otherwise, a reference to the object referenced by the current <see cref="T:System.WeakReference"/> object.</returns>
        /// <exception cref="T:System.InvalidOperationException">The reference to the target object is invalid. This exception can be thrown while setting this property if the value is a null reference or if the object has been finalized during the set operation.</exception>
        public new T Target
        {
            get { return (T)base.Target; }
        }
    }
}
