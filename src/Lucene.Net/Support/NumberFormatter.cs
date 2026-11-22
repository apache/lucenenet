using System;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Formats that are not (yet) supported by J2N. Typically,
    /// these are Java-like number formats.
    /// </summary>
    internal class NumberFormatter
    {
        /// <summary>
        /// Formats a 32-bit signed integer value as a character span representation
        /// of the provided <paramref name="value"/> argument interpreted as an unsigned
        /// integer in base 2, matching Java's <c>Integer.toBinaryString(int)</c>.
        /// </summary>
        /// <param name="value">An integer to be converted to binary ASCII digits.</param>
        /// <param name="destination">
        /// When this method returns <c>true</c>, contains the binary (base-2) ASCII
        /// representation of <paramref name="value"/> with no leading zeros.
        /// </param>
        /// <param name="charsWritten">
        /// When this method returns <c>true</c>, the number of characters written to
        /// <paramref name="destination"/>; otherwise, <c>0</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="destination"/> was large enough to hold the
        /// formatted value; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// If <paramref name="value"/> is negative, the unsigned integer value is
        /// <paramref name="value"/> plus 2<superscript>32</superscript> (4,294,967,296);
        /// otherwise, it is equal to <paramref name="value"/>. This value is written as
        /// ASCII <c>'0'</c> and <c>'1'</c> characters with no extra leading zeros.
        /// <para/>
        /// A <paramref name="destination"/> length of at least 32 is sufficient to hold
        /// any possible result value (even for <c>0</c>, 1 character is required).
        /// </remarks>
        public static bool TryFormatBinary(int value, Span<char> destination, out int charsWritten)
        {
            uint u = unchecked((uint)value);   // Java interprets int bits as unsigned
            int bitCount = 32;

            // Find first non-zero bit (Java trims leading zeros)
            int start = 0;
            while (start < bitCount - 1 && ((u >> (bitCount - 1 - start)) & 1) == 0)
                start++;

            int length = bitCount - start;
            if (destination.Length < length)
            {
                charsWritten = 0;
                return false;
            }

            for (int i = 0; i < length; i++)
            {
                int bitIndex = bitCount - 1 - (start + i);
                destination[i] = ((u >> bitIndex) & 1) == 1 ? '1' : '0';
            }

            charsWritten = length;
            return true;
        }
    }
}
