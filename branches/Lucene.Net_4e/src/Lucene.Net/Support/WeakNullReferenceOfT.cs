// -----------------------------------------------------------------------
// <copyright file="WeakNullReferenceOfT.cs" company="Apache">
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
    /// Represents a weak reference of null for the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the target.</typeparam>
    /// <remarks>
    ///     <note>
    ///        <para>
    ///             <b>C# File: </b> <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/src/Lucene.Net/Support/WeakNullReferenceOfT.cs">
    ///              src/Lucene.Net/Support/WeakNullReferenceOfT.cs
    ///             </a>
    ///         </para>
    ///     </note>
    /// </remarks>
    public class WeakNullReference<T> : WeakReference<T> 
        where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WeakNullReference&lt;T&gt;"/> class.
        /// </summary>
        public WeakNullReference() 
            : base(null) 
        {
        }

        /// <summary>
        /// Gets an indication whether the object referenced by the current <see cref="T:System.WeakReference"/> object has been garbage collected.
        /// </summary>
        /// <value></value>
        /// <returns>true if the object referenced by the current <see cref="T:System.WeakReference"/> object has not been garbage collected and is still accessible; otherwise, false.</returns>
        public override bool IsAlive
        {
            get
            {
                return true;
            }
        }
    }
}
