using System.Collections.Generic;
using System.Reflection;

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
    /// <see cref="IEqualityComparer{T}"/> to patch value type support for generics in MONO AOT.
    /// Value types for generics in this environment at the time of this writing is
    /// not supported, but is currently under development and eventually should be.
    /// <para/>
    /// This class can be used to patch the behavior when using MONO AOT, at the cost
    /// of throwing an exception to reliably detect when generic types are not supported.
    /// See <a href=""></a>
    /// <para/>
    /// See LUCENENET-602.
    /// </summary>
    /// <typeparam name="T">The type of objects to compare.</typeparam>
    public sealed class EqualityComparer<T>
    {
        private static readonly bool IsValueType = typeof(T).GetTypeInfo().IsValueType;

        /// <summary>
        /// Returns a default equality comparer for the type specified by the generic argument.
        /// <para/>
        /// LUCENENET specific constant that is used for the comparer
        /// rather than creating a custom <see cref="IEqualityComparer{T}"/> for value types.
        /// See LUCENENET-602.
        /// </summary>
        public static System.Collections.Generic.EqualityComparer<T> Default { get; } = CreateComparer();

        private static System.Collections.Generic.EqualityComparer<T> CreateComparer()
        {
            if (!EqualityComparerConstants.ValueTypesSupported.HasValue)
            {
                if (EqualityComparerConstants.ValueTypesSupported == true)
                {
                    return System.Collections.Generic.EqualityComparer<T>.Default;
                }
                else
                {
                    return IsValueType ?
                        new ValueTypeEqualityComparer() :
                        System.Collections.Generic.EqualityComparer<T>.Default;
                }
            }

            // We test for an exception the first time this is called on this runtime instance,
            // and store it in the ValueTypesSupported property (called once for any value type).
            // This is not currently supported under MONO AOT compilation, but is under development,
            // so eventually the catch path will be unreachable.
            try
            {
                var result = System.Collections.Generic.EqualityComparer<T>.Default;
                EqualityComparerConstants.ValueTypesSupported = true;
                return result;
            }
            catch when (IsValueType)
            {
                EqualityComparerConstants.ValueTypesSupported = false;
                return new ValueTypeEqualityComparer();
            }
        }

        /// <summary>
        /// Comparer for any .NET value type.
        /// <para/>
        /// In some platforms, such as Xamarin iOS, the implementation of <see cref="System.Collections.Generic.EqualityComparer{T}.Default"/> doesn't
        /// work for value types. This class is used to provide equality comparers in cases where value types are required.
        /// </summary>
        internal class ValueTypeEqualityComparer : System.Collections.Generic.EqualityComparer<T> // where T : struct
        {
            /// <summary>
            /// Determines whether two objects of type T are equal.
            /// </summary>
            /// <param name="x">The first value type to compare.</param>
            /// <param name="y">The second value type to compare.</param>
            /// <returns><c>true</c> if the specified objects are equal; otherwise, <c>false</c>.</returns>
            public override bool Equals(T x, T y)
            {
                if (x != null)
                {
                    if (y != null) return x.Equals(y);
                    return false;
                }
                if (y != null) return false;
                return true;
            }

            /// <summary>
            /// Serves as the default hash function.
            /// <para/>
            /// This is the same as calling obj.GetHashCode().
            /// </summary>
            /// <param name="obj">The object for which to get a hash code.</param>
            /// <returns>A hash code for the specified object.</returns>
            public override int GetHashCode(T obj)
            {
                return obj == null ? 0 : obj.GetHashCode();
            }
        }
    }

    internal class EqualityComparerConstants
    {
        public static bool? ValueTypesSupported { get; set; } = null;
    }
}
