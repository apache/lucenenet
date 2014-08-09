/**
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
 */
namespace Lucene.Net.Support
{
    /// <summary>
    /// Summary description for SupportExtensionMethods
    /// </summary>
    public static class SupportExtensionMethods
    {



        /// <summary>
        /// Clones and casts the new instance to the same type of the old instance. 
        /// </summary>
        /// <typeparam name="T">The Type that implements <see cref="Lucene.Net.Support.ICloneable"/></typeparam>
        /// <param name="instance">The instance of type T that will be cloned.</param>
        /// <param name="deepClone">Instructs instance to perform a deep clone when true.</param>
        /// <returns>A new clone of type T</returns>
        public static T CloneAndCast<T>(this T instance, bool deepClone = false) where T : ICloneable
        {
            Check.NotNull("instance", instance);

            return (T)instance.Clone(deepClone);
        }

        /// <summary>
        /// Computes the integer into Austin Appleby's MurmurHash3
        /// </summary>
        /// <remarks>
        ///     <para>
        ///     <see cref="https://code.google.com/p/smhasher/wiki/MurmurHash3">MurmurHash3</see>
        ///     </para>
        /// </remarks>
        /// <param name="value">The value, usually from a <see cref="System.Object.GetHashCode"/>.</param>
        /// <returns>The computed hash.</returns>
        public static int ComputeMurmurHash3(this int value)
        {
            uint x = (uint)value;

            x ^= x >> 16;
            x *= 0x85ebca6b;
            x ^= x >> 13;
            x *= 0xc2b2ae35;
            x ^= x >> 16;

            return (int)x;
        }
    }
}