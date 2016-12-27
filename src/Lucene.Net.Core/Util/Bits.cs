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
    /// @lucene.experimental
    /// </summary>
    public interface Bits // LUCENENET TODO: Rename IBits (or better yet, change this to an abstract class so we can properly nest its subclasses)
    {
        /// <summary>
        /// Returns the value of the bit with the specified <code>index</code>.
        /// </summary>
        /// <param name="index"> index, should be non-negative and &lt; <seealso cref="#length()"/>.
        ///        The result of passing negative or out of bounds values is undefined
        ///        by this interface, <b>just don't do it!</b> </param>
        /// <returns> <code>true</code> if the bit is set, <code>false</code> otherwise. </returns>
        bool Get(int index);

        /// <summary>
        /// Returns the number of bits in this set </summary>
        int Length(); // LUCENENET TODO: Change to property getter
    }

    public static class BitsHelpers
    {
        public static readonly Bits[] EMPTY_ARRAY = new Bits[0];
    }

    /// <summary>
    /// Bits impl of the specified length with all bits set.
    /// </summary>
    public class Bits_MatchAllBits : Bits // LUCENENET TODO: Rename MatchAllBits and nest in type named Bits (after Bits is named IBits)
    {
        private readonly int _len;

        public Bits_MatchAllBits(int len)
        {
            _len = len;
        }

        public bool Get(int index)
        {
            return true;
        }

        public int Length()
        {
            return _len;
        }

        public override int GetHashCode() // LUCENENET TODO: This wasn't in Lucene - is it needed ?
        {
            return ("MatchAllBits" + _len).GetHashCode();
        }
    }

    /// <summary>
    /// Bits impl of the specified length with no bits set.
    /// </summary>
    public class Bits_MatchNoBits : Bits
    {
        private readonly int _len;

        public Bits_MatchNoBits(int len)
        {
            _len = len;
        }

        public bool Get(int index)
        {
            return false;
        }

        public int Length()
        {
            return _len;
        }

        public override int GetHashCode() // LUCENENET TODO: This wasn't in Lucene - is it needed ?
        {
            return ("MatchNoBits" + _len).GetHashCode();
        }
    }
}