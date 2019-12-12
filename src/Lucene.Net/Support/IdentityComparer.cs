using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Support
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
    /// Represents a comparison operation that tests equality by reference
    /// instead of equality by value. Basically, the comparison is done by
    /// checking if <see cref="System.Object.ReferenceEquals(object, object)"/>
    /// returns true rather than by calling the "Equals" function.
    /// </summary>
    /// <typeparam name="T">The type of object to test for reference equality. Must be a class, not a struct.</typeparam>
    public class IdentityComparer<T> : IEqualityComparer<T> where T : class
    {
        /// <summary>
        /// Gets an <see cref="IdentityComparer{T}"/> object that tests equality by reference
        /// instead of equality by value. Basically, the comparison is done by
        /// checking if <see cref="System.Object.ReferenceEquals(object, object)"/>
        /// returns true rather than by calling the "Equals" function.
        /// </summary>
        public static IdentityComparer<T> Default => new IdentityComparer<T>();

        internal IdentityComparer()
        { }

        public bool Equals(T x, T y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }

    /// <summary>
    /// Represents a comparison operation that tests equality by reference
    /// instead of equality by value. Basically, the comparison is done by
    /// checking if <see cref="System.Object.ReferenceEquals(object, object)"/>
    /// returns true rather than by calling the "Equals" function.
    /// <para/>
    /// Note that the assumption is that the object is passed will be a reference type,
    /// although it is not strictly enforced.
    /// </summary>
    public class IdentityComparer : IEqualityComparer
    {
        /// <summary>
        /// Gets an <see cref="IdentityComparer{T}"/> object that tests equality by reference
        /// instead of equality by value. Basically, the comparison is done by
        /// checking if <see cref="System.Object.ReferenceEquals(object, object)"/>
        /// returns true rather than by calling the "Equals" function.
        /// </summary>
        public static IdentityComparer Default => new IdentityComparer();

        internal IdentityComparer()
        { }

        public new bool Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}