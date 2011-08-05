// -----------------------------------------------------------------------
// <copyright file="WeakKeyComparerOfT.cs" company="Apache">
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
    /// TODO: Summary
    /// </summary>
    /// <typeparam name="T">The type for the comparers of the TKey Type for the Dictionary.</typeparam>
    /// <remarks>
    ///    <note>
    ///        <para>
    ///             <b>C# File: </b> <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/src/Lucene.Net/Support/WeakKeyComparerOfT.cs">
    ///              src/Lucene.Net/Support/WeakKeyComparerOfT.cs
    ///             </a>
    ///         </para>
    ///     </note>
    /// </remarks>
    internal sealed class WeakKeyComparer<T> : IEqualityComparer<object>
        where T : class
    {
        private IEqualityComparer<T> comparer;

        internal WeakKeyComparer(IEqualityComparer<T> comparer)
        {
            if (comparer == null)
                comparer = EqualityComparer<T>.Default;

            this.comparer = comparer;
        }

        /// <summary>
        /// Gets the internal comparer.
        /// </summary>
        /// <value>The internal comparer.</value>
        public IEqualityComparer<T> InternalComparer
        {
            get { return this.comparer; }
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public int GetHashCode(object obj)
        {
            WeakKeyReference<T> weakKey = obj as WeakKeyReference<T>;
            
            if (weakKey != null) 
                return weakKey.HashCode;

            return this.comparer.GetHashCode((T)obj);
        }

        // Note: There are actually 9 cases to handle here.
        //
        //  Let Wa = Alive Weak Reference
        //  Let Wd = Dead Weak Reference
        //  Let S  = Strong Reference
        //  
        //  x  | y  | Equals(x,y)
        // -------------------------------------------------
        //  Wa | Wa | comparer.Equals(x.Target, y.Target) 
        //  Wa | Wd | false
        //  Wa | S  | comparer.Equals(x.Target, y)
        //  Wd | Wa | false
        //  Wd | Wd | x == y
        //  Wd | S  | false
        //  S  | Wa | comparer.Equals(x, y.Target)
        //  S  | Wd | false
        //  S  | S  | comparer.Equals(x, y)
        // -------------------------------------------------
        public new bool Equals(object x, object y)
        {
            bool parameterXIsDead, parameterYIsDead;
            
            T first = GetTarget(x, out parameterXIsDead);
            T second = GetTarget(y, out parameterYIsDead);

            if (parameterXIsDead)
                return parameterYIsDead ? x == y : false;

            if (parameterYIsDead)
                return false;

            return this.comparer.Equals(first, second);
        }

        private static T GetTarget(object obj, out bool isDead)
        {
            WeakKeyReference<T> weakreference = obj as WeakKeyReference<T>;
            T target;
            
            if (weakreference != null)
            {
                target = weakreference.Target;
                isDead = !weakreference.IsAlive;
            }
            else
            {
                target = (T)obj;
                isDead = false;
            }

            return target;
        }
    }   
}
