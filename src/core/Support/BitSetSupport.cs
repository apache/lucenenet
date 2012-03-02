namespace Lucene.Net.Support
{
    /// <summary>
    /// This class provides supporting methods of java.util.BitSet
    /// that are not present in System.Collections.BitArray.
    /// </summary>
    public class BitSetSupport
    {
        /// <summary>
        /// Returns the next set bit at or after index, or -1 if no such bit exists.
        /// </summary>
        /// <param name="bitArray"></param>
        /// <param name="index">the index of bit array at which to start checking</param>
        /// <returns>the next set bit or -1</returns>
        public static int NextSetBit(System.Collections.BitArray bitArray, int index)
        {
            while (index < bitArray.Length)
            {
                // if index bit is set, return it
                // otherwise check next index bit
                if (bitArray.Get(index))
                    return index;
                else
                    index++;
            }
            // if no bits are set at or after index, return -1
            return -1;
        }

        /// <summary>
        /// Returns the next un-set bit at or after index, or -1 if no such bit exists.
        /// </summary>
        /// <param name="bitArray"></param>
        /// <param name="index">the index of bit array at which to start checking</param>
        /// <returns>the next set bit or -1</returns>
        public static int NextClearBit(System.Collections.BitArray bitArray, int index)
        {
            while (index < bitArray.Length)
            {
                // if index bit is not set, return it
                // otherwise check next index bit
                if (!bitArray.Get(index))
                    return index;
                else
                    index++;
            }
            // if no bits are set at or after index, return -1
            return -1;
        }

        /// <summary>
        /// Returns the number of bits set to true in this BitSet.
        /// </summary>
        /// <param name="bits">The BitArray object.</param>
        /// <returns>The number of bits set to true in this BitSet.</returns>
        public static int Cardinality(System.Collections.BitArray bits)
        {
            int count = 0;
            for (int i = 0; i < bits.Count; i++)
            {
                if (bits[i])
                    count++;
            }
            return count;
        }
    }
}