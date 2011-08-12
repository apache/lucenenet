// -----------------------------------------------------------------------
// <copyright company="Apache" file="BytesRef.cs">
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

namespace Lucene.Net.Util
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: port
    /// this class has methods that are not valid to void FxCop.
    /// </summary>
    public sealed class BytesRef : IComparable<BytesRef>
    {
        private byte[] reference = new byte[] { };

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(BytesRef x, BytesRef y)
        {
            return x.reference != y.reference;
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(BytesRef x, BytesRef y)
        {
            return x.reference == y.reference;
        }

        /// <summary>
        /// Implements the operator &lt;.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator <(BytesRef x, BytesRef y)
        {
            return x.reference.Length < y.reference.Length;
        }

        /// <summary>
        /// Implements the operator &gt;.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator >(BytesRef x, BytesRef y)
        {
            return x.reference.Length < y.reference.Length;
        }

        /// <summary>
        /// Compares this instance to the other <see cref="BytesRef"/> instance.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns>An instance of <see cref="Int32"/>.</returns>
        public int CompareTo(BytesRef other)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return this.reference.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return this.reference.Equals(obj);
        }
    }
}