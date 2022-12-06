using Lucene.Net.Support;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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
    /// Interface for Bitset-like structures.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public interface IBits
    {
        /// <summary>
        /// Returns the value of the bit with the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index"> Index, should be non-negative and &lt; <see cref="Length"/>.
        ///        The result of passing negative or out of bounds values is undefined
        ///        by this interface, <b>just don't do it!</b> </param>
        /// <returns> <c>true</c> if the bit is set, <c>false</c> otherwise. </returns>
        bool Get(int index);

        /// <summary>
        /// Returns the number of bits in this set </summary>
        int Length { get; }
    }

    public static class Bits
    {
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("Performance", "S3887:Use an immutable collection or reduce the accessibility of the non-private readonly field", Justification = "Collection is immutable")]
        [SuppressMessage("Performance", "S2386:Use an immutable collection or reduce the accessibility of the public static field", Justification = "Collection is immutable")]
        public static readonly IBits[] EMPTY_ARRAY = Arrays.Empty<IBits>();

        /// <summary>
        /// Bits impl of the specified length with all bits set.
        /// </summary>
        public class MatchAllBits : IBits
        {
            private readonly int _len;

            public MatchAllBits(int len)
            {
                _len = len;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Get(int index)
            {
                return true;
            }

            public int Length => _len;
        }

        /// <summary>
        /// Bits impl of the specified length with no bits set.
        /// </summary>
        public class MatchNoBits : IBits
        {
            private readonly int _len;

            public MatchNoBits(int len)
            {
                _len = len;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Get(int index)
            {
                return false;
            }

            public int Length => _len;
        }
    }
}