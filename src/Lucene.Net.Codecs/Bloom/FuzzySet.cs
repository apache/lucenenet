using Lucene.Net.Diagnostics;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Diagnostics;
using System.IO;

namespace Lucene.Net.Codecs.Bloom
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
    /// A class used to represent a set of many, potentially large, values (e.g. many
    /// long strings such as URLs), using a significantly smaller amount of memory.
    /// <para/>
    /// The set is "lossy" in that it cannot definitively state that is does contain
    /// a value but it <em>can</em> definitively say if a value is <em>not</em> in
    /// the set. It can therefore be used as a Bloom Filter.
    /// <para/>
    /// Another application of the set is that it can be used to perform fuzzy counting because
    /// it can estimate reasonably accurately how many unique values are contained in the set. 
    /// <para/>
    /// This class is NOT threadsafe.
    /// <para/>
    /// Internally a Bitset is used to record values and once a client has finished recording
    /// a stream of values the <see cref="Downsize(float)"/> method can be used to create a suitably smaller set that
    /// is sized appropriately for the number of values recorded and desired saturation levels. 
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class FuzzySet
    {
        public static readonly int VERSION_SPI = 1; // HashFunction used to be loaded through a SPI
        public static readonly int VERSION_START = VERSION_SPI;
        public static readonly int VERSION_CURRENT = 2;

        public static HashFunction HashFunctionForVersion(int version)
        {
            if (version < VERSION_START)
                throw new ArgumentOutOfRangeException(nameof(version), "Version " + version + " is too old, expected at least " +
                                                   VERSION_START);// LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)

            if (version > VERSION_CURRENT)
                throw new ArgumentOutOfRangeException(nameof(version), "Version " + version + " is too new, expected at most " +
                                                   VERSION_CURRENT);// LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)

            return MurmurHash2.INSTANCE;
        }

        /// <remarks>
        /// Result from <see cref="FuzzySet.Contains(BytesRef)"/>:
        /// can never return definitively YES (always MAYBE), 
        /// but can sometimes definitely return NO.
        /// </remarks>
        public enum ContainsResult
        {
            MAYBE,
            NO
        };

        private readonly HashFunction _hashFunction;
        private readonly FixedBitSet _filter;
        private readonly int _bloomSize;

        //The sizes of BitSet used are all numbers that, when expressed in binary form,
        //are all ones. This is to enable fast downsizing from one bitset to another
        //by simply ANDing each set index in one bitset with the size of the target bitset
        // - this provides a fast modulo of the number. Values previously accumulated in
        // a large bitset and then mapped to a smaller set can be looked up using a single
        // AND operation of the query term's hash rather than needing to perform a 2-step
        // translation of the query term that mirrors the stored content's reprojections.
        private static readonly int[] _usableBitSetSizes = LoadUsableBitSetSizes(); // LUCENENET: marked readonly
        private static int[] LoadUsableBitSetSizes() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
            var usableBitSetSizes = new int[30];
            const int mask = 1;
            var size = mask;
            for (var i = 0; i < usableBitSetSizes.Length; i++)
            {
                size = (size << 1) | mask;
                usableBitSetSizes[i] = size;
            }
            return usableBitSetSizes;
        }

        /// <summary>
        /// Rounds down required <paramref name="maxNumberOfBits"/> to the nearest number that is made up
        /// of all ones as a binary number.  
        /// Use this method where controlling memory use is paramount.
        /// </summary>
        public static int GetNearestSetSize(int maxNumberOfBits)
        {
            int result = _usableBitSetSizes[0];
            for (int i = 0; i < _usableBitSetSizes.Length; i++)
            {
                if (_usableBitSetSizes[i] <= maxNumberOfBits)
                {
                    result = _usableBitSetSizes[i];
                }
            }
            return result;
        }

        /// <summary>
        /// Use this method to choose a set size where accuracy (low content saturation) is more important
        /// than deciding how much memory to throw at the problem.
        /// </summary>
        /// <param name="maxNumberOfValuesExpected"></param>
        /// <param name="desiredSaturation">A number between 0 and 1 expressing the % of bits set once all values have been recorded.</param>
        /// <returns>The size of the set nearest to the required size.</returns>
        public static int GetNearestSetSize(int maxNumberOfValuesExpected,
            float desiredSaturation)
        {
            // Iterate around the various scales of bitset from smallest to largest looking for the first that
            // satisfies value volumes at the chosen saturation level
            for (int i = 0; i < _usableBitSetSizes.Length; i++)
            {
                int numSetBitsAtDesiredSaturation = (int)(_usableBitSetSizes[i] * desiredSaturation);
                int estimatedNumUniqueValues = GetEstimatedNumberUniqueValuesAllowingForCollisions(
                    _usableBitSetSizes[i], numSetBitsAtDesiredSaturation);
                if (estimatedNumUniqueValues > maxNumberOfValuesExpected)
                {
                    return _usableBitSetSizes[i];
                }
            }
            return -1;
        }

        public static FuzzySet CreateSetBasedOnMaxMemory(int maxNumBytes)
        {
            var setSize = GetNearestSetSize(maxNumBytes);
            return new FuzzySet(new FixedBitSet(setSize + 1), setSize, HashFunctionForVersion(VERSION_CURRENT));
        }

        public static FuzzySet CreateSetBasedOnQuality(int maxNumUniqueValues, float desiredMaxSaturation)
        {
            var setSize = GetNearestSetSize(maxNumUniqueValues, desiredMaxSaturation);
            return new FuzzySet(new FixedBitSet(setSize + 1), setSize, HashFunctionForVersion(VERSION_CURRENT));
        }

        private FuzzySet(FixedBitSet filter, int bloomSize, HashFunction hashFunction)
        {
            _filter = filter;
            _bloomSize = bloomSize;
            _hashFunction = hashFunction;
        }

        /// <summary>
        /// The main method required for a Bloom filter which, given a value determines set membership.
        /// Unlike a conventional set, the fuzzy set returns <see cref="ContainsResult.NO"/> or 
        /// <see cref="ContainsResult.MAYBE"/> rather than <c>true</c> or <c>false</c>.
        /// </summary>
        /// <returns><see cref="ContainsResult.NO"/> or <see cref="ContainsResult.MAYBE"/></returns>
        public virtual ContainsResult Contains(BytesRef value)
        {
            var hash = _hashFunction.Hash(value);
            if (hash < 0)
            {
                hash = hash*-1;
            }
            return MayContainValue(hash);
        }

        /// <summary>
        /// Serializes the data set to file using the following format:
        /// <list type="bullet">
        ///     <item><description>FuzzySet --&gt;FuzzySetVersion,HashFunctionName,BloomSize,
        ///         NumBitSetWords,BitSetWord<sup>NumBitSetWords</sup></description></item> 
        ///     <item><description>HashFunctionName --&gt; String (<see cref="DataOutput.WriteString(string)"/>) The
        ///         name of a ServiceProvider registered <see cref="HashFunction"/></description></item>
        ///     <item><description>FuzzySetVersion --&gt; Uint32 (<see cref="DataOutput.WriteInt32(int)"/>) The version number of the <see cref="FuzzySet"/> class</description></item>
        ///     <item><description>BloomSize --&gt; Uint32 (<see cref="DataOutput.WriteInt32(int)"/>) The modulo value used
        ///         to project hashes into the field's Bitset</description></item>
        ///     <item><description>NumBitSetWords --&gt; Uint32 (<see cref="DataOutput.WriteInt32(int)"/>) The number of
        ///         longs (as returned from <see cref="FixedBitSet.GetBits()"/>)</description></item>
        ///     <item><description>BitSetWord --&gt; Long (<see cref="DataOutput.WriteInt64(long)"/>) A long from the array
        ///         returned by <see cref="FixedBitSet.GetBits()"/></description></item>
        /// </list>
        /// </summary>
        /// <param name="output">Data output stream.</param>
        /// <exception cref="IOException">If there is a low-level I/O error.</exception>
        public virtual void Serialize(DataOutput output)
        {
            output.WriteInt32(VERSION_CURRENT);
            output.WriteInt32(_bloomSize);
            var bits = _filter.GetBits();
            output.WriteInt32(bits.Length);
            foreach (var t in bits)
            {
                // Can't used VLong encoding because cant cope with negative numbers
                // output by FixedBitSet
                output.WriteInt64(t);
            }
        }

        public static FuzzySet Deserialize(DataInput input)
        {
            var version = input.ReadInt32();
            if (version == VERSION_SPI)
                input.ReadString();
           
            var hashFunction = HashFunctionForVersion(version);
            var bloomSize = input.ReadInt32();
            var numLongs = input.ReadInt32();
            var longs = new long[numLongs];
            for (var i = 0; i < numLongs; i++)
            {
                longs[i] = input.ReadInt64();
            }
            var bits = new FixedBitSet(longs, bloomSize + 1);
            return new FuzzySet(bits, bloomSize, hashFunction);
        }

        private ContainsResult MayContainValue(int positiveHash)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert((positiveHash >= 0));

            // Bloom sizes are always base 2 and so can be ANDed for a fast modulo
            var pos = positiveHash & _bloomSize;
            return _filter.Get(pos) ? ContainsResult.MAYBE : ContainsResult.NO;
        }

        /// <summary>
        /// Records a value in the set. The referenced bytes are hashed and then modulo n'd where n is the
        /// chosen size of the internal bitset.
        /// </summary>
        /// <param name="value">The Key value to be hashed.</param>
        /// <exception cref="IOException">If there is a low-level I/O error.</exception>
        public virtual void AddValue(BytesRef value)
        {
            var hash = _hashFunction.Hash(value);
            if (hash < 0)
            {
                hash = hash*-1;
            }
            // Bitmasking using bloomSize is effectively a modulo operation.
            var bloomPos = hash & _bloomSize;
            _filter.Set(bloomPos);
        }

        /// <param name="targetMaxSaturation">
        /// A number between 0 and 1 describing the % of bits that would ideally be set in the result. 
        /// Lower values have better accuracy but require more space.
        /// </param>
        /// <return>A smaller <see cref="FuzzySet"/> or <c>null</c> if the current set is already over-saturated.</return>
        public virtual FuzzySet Downsize(float targetMaxSaturation)
        {
            var numBitsSet = _filter.Cardinality;
            FixedBitSet rightSizedBitSet;
            var rightSizedBitSetSize = _bloomSize;
            //Hopefully find a smaller size bitset into which we can project accumulated values while maintaining desired saturation level
            for (int i = 0; i < _usableBitSetSizes.Length; i++)
            {
                int candidateBitsetSize = _usableBitSetSizes[i];
                float candidateSaturation = (float)numBitsSet
                    / (float)candidateBitsetSize;
                if (candidateSaturation <= targetMaxSaturation)
                {
                    rightSizedBitSetSize = candidateBitsetSize;
                    break;
                }
            }
            // Re-project the numbers to a smaller space if necessary
            if (rightSizedBitSetSize < _bloomSize)
            {
                // Reset the choice of bitset to the smaller version
                rightSizedBitSet = new FixedBitSet(rightSizedBitSetSize + 1);
                // Map across the bits from the large set to the smaller one
                var bitIndex = 0;
                do
                {
                    bitIndex = _filter.NextSetBit(bitIndex);
                    if (bitIndex < 0) continue;

                    // Project the larger number into a smaller one effectively
                    // modulo-ing by using the target bitset size as a mask
                    var downSizedBitIndex = bitIndex & rightSizedBitSetSize;
                    rightSizedBitSet.Set(downSizedBitIndex);
                    bitIndex++;
                } while ((bitIndex >= 0) && (bitIndex <= _bloomSize));
            }
            else
            {
                return null;
            }
            return new FuzzySet(rightSizedBitSet, rightSizedBitSetSize, _hashFunction);
        }

        public virtual int GetEstimatedUniqueValues()
        {
            return GetEstimatedNumberUniqueValuesAllowingForCollisions(_bloomSize, _filter.Cardinality);
        }

        /// <summary>
        /// Given a <paramref name="setSize"/> and a the number of set bits, produces an estimate of the number of unique values recorded.
        /// </summary>
        public static int GetEstimatedNumberUniqueValuesAllowingForCollisions(
            int setSize, int numRecordedBits)
        {
            double setSizeAsDouble = setSize;
            double numRecordedBitsAsDouble = numRecordedBits;
            var saturation = numRecordedBitsAsDouble/setSizeAsDouble;
            var logInverseSaturation = Math.Log(1 - saturation)*-1;
            return (int) (setSizeAsDouble*logInverseSaturation);
        }

        public virtual float GetSaturation()
        {
            var numBitsSet = _filter.Cardinality;
            return numBitsSet/(float) _bloomSize;
        }

        public virtual long RamBytesUsed()
        {
            return RamUsageEstimator.SizeOf(_filter.GetBits());
        }
    }
}
