// Lucene version compatibility level 4.8.1
using J2N;
using J2N.Text;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;

namespace Lucene.Net.Analysis.Util
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
    /// <see cref="CharacterUtils"/> provides a unified interface to Character-related
    /// operations to implement backwards compatible character operations based on a
    /// <see cref="LuceneVersion"/> instance.
    /// 
    /// @lucene.internal
    /// </summary>
    public abstract class CharacterUtils
    {
        // LUCENENET specific class for supporting broken Unicode support in Lucene 3.0.
        // See the TestCharArraySet.TestSupplementaryCharsBWCompat()
        // and TestCharArraySet.TestSingleHighSurrogateBWComapt() tests.
        private static readonly CharacterUtils JAVA_4_BW_COMPAT = new Java4CharacterUtilsBWCompatibility();

        private static readonly CharacterUtils JAVA_4 = new Java4CharacterUtils();
        private static readonly CharacterUtils JAVA_5 = new Java5CharacterUtils();

        /// <summary>
        /// Returns a <see cref="CharacterUtils"/> implementation according to the given
        /// <see cref="LuceneVersion"/> instance.
        /// </summary>
        /// <param name="matchVersion">
        ///          a version instance </param>
        /// <returns> a <see cref="CharacterUtils"/> implementation according to the given
        ///         <see cref="LuceneVersion"/> instance. </returns>
        public static CharacterUtils GetInstance(LuceneVersion matchVersion)
        {
#pragma warning disable 612, 618
            return matchVersion.OnOrAfter(LuceneVersion.LUCENE_31) 
                ? JAVA_5 
                : JAVA_4_BW_COMPAT;
#pragma warning restore 612, 618
        }

        /// <summary>
        /// Return a <see cref="CharacterUtils"/> instance compatible with Java 1.4. </summary>
        public static CharacterUtils GetJava4Instance(LuceneVersion matchVersion) // LUCENENET specific - added matchVersion parameter so we can support backward compatible Unicode support
        {
#pragma warning disable 612, 618
            return matchVersion.OnOrAfter(LuceneVersion.LUCENE_31) ? JAVA_4 : JAVA_4_BW_COMPAT;
#pragma warning restore 612, 618
        }

        /// <summary>
        /// Returns the code point at the given index of the <see cref="string"/>.
        /// Depending on the <see cref="LuceneVersion"/> passed to
        /// <see cref="CharacterUtils.GetInstance(LuceneVersion)"/> this method mimics the behavior
        /// of <c>Character.CodePointAt(char[], int)</c> as it would have been
        /// available on a Java 1.4 JVM or on a later virtual machine version.
        /// </summary>
        /// <param name="seq">
        ///          a character sequence </param>
        /// <param name="offset">
        ///          the offset to the char values in the chars array to be converted
        /// </param>
        /// <returns> the Unicode code point at the given index </returns>
        /// <exception cref="ArgumentNullException">
        ///           - if the sequence is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///           - if the value offset is negative or not less than the length of
        ///           the character sequence. </exception>
        public abstract int CodePointAt(string seq, int offset);

        /// <summary>
        /// Returns the code point at the given index of the <see cref="ICharSequence"/>.
        /// Depending on the <see cref="LuceneVersion"/> passed to
        /// <see cref="CharacterUtils.GetInstance(LuceneVersion)"/> this method mimics the behavior
        /// of <c>Character.CodePointAt(char[], int)</c> as it would have been
        /// available on a Java 1.4 JVM or on a later virtual machine version.
        /// </summary>
        /// <param name="seq">
        ///          a character sequence </param>
        /// <param name="offset">
        ///          the offset to the char values in the chars array to be converted
        /// </param>
        /// <returns> the Unicode code point at the given index </returns>
        /// <exception cref="ArgumentNullException">
        ///           - if the sequence is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///           - if the value offset is negative or not less than the length of
        ///           the character sequence. </exception>
        public abstract int CodePointAt(ICharSequence seq, int offset);

        /// <summary>
        /// Returns the code point at the given index of the char array where only elements
        /// with index less than the limit are used.
        /// Depending on the <see cref="LuceneVersion"/> passed to
        /// <see cref="CharacterUtils.GetInstance(LuceneVersion)"/> this method mimics the behavior
        /// of <c>Character.CodePointAt(char[], int)</c> as it would have been
        /// available on a Java 1.4 JVM or on a later virtual machine version.
        /// </summary>
        /// <param name="chars">
        ///          a character array </param>
        /// <param name="offset">
        ///          the offset to the char values in the chars array to be converted </param>
        /// <param name="limit"> the index afer the last element that should be used to calculate
        ///        codepoint.  
        /// </param>
        /// <returns> the Unicode code point at the given index </returns>
        /// <exception cref="ArgumentNullException">
        ///           - if the array is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///           - if the value offset is negative or not less than the length of
        ///           the char array. </exception>
        public abstract int CodePointAt(char[] chars, int offset, int limit);

        /// <summary>
        /// Return the number of characters in <paramref name="seq"/>. </summary>
        public abstract int CodePointCount(string seq);

        /// <summary>
        /// Return the number of characters in <paramref name="seq"/>. </summary>
        public abstract int CodePointCount(ICharSequence seq);

        /// <summary>
        /// Return the number of characters in <paramref name="seq"/>. </summary>
        public abstract int CodePointCount(char[] seq);

        /// <summary>
        /// Return the number of characters in <paramref name="seq"/>. </summary>
        public abstract int CodePointCount(StringBuilder seq);

        /// <summary>
        /// Creates a new <see cref="CharacterBuffer"/> and allocates a <see cref="T:char[]"/>
        /// of the given bufferSize.
        /// </summary>
        /// <param name="bufferSize">
        ///          the internal char buffer size, must be <c>&gt;= 2</c> </param>
        /// <returns> a new <see cref="CharacterBuffer"/> instance. </returns>
        public static CharacterBuffer NewCharacterBuffer(int bufferSize)
        {
            if (bufferSize < 2)
            {
                // LUCENENET: Changed from IllegalArgumentException to ArgumentOutOfRangeException
                throw new ArgumentOutOfRangeException(nameof(bufferSize), "buffersize must be >= 2");
            }
            return new CharacterBuffer(new char[bufferSize], 0, 0);
        }


        /// <summary>
        /// Converts each unicode codepoint to lowerCase via <see cref="TextInfo.ToLower(string)"/> in the invariant culture starting 
        /// at the given offset. </summary>
        /// <param name="buffer"> the char buffer to lowercase </param>
        /// <param name="offset"> the offset to start at </param>
        /// <param name="length"> the number of characters in the buffer to lower case </param>
        public virtual void ToLower(char[] buffer, int offset, int length) // LUCENENET specific - marked virtual so we can override the default
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(buffer.Length >= length);
                Debugging.Assert(offset <= 0 && offset <= buffer.Length);
            }

            // Reduce allocations by using the stack and spans
            var source = new ReadOnlySpan<char>(buffer, offset, length);
            var destination = buffer.AsSpan(offset, length);
            var spare = length * sizeof(char) <= Constants.MaxStackByteLimit ? stackalloc char[length] : new char[length];
            source.ToLower(spare, CultureInfo.InvariantCulture);
            spare.CopyTo(destination);

            //// Slight optimization, eliminating a few method calls internally
            //CultureInfo.InvariantCulture.TextInfo
            //    .ToLower(new string(buffer, offset, length))
            //    .CopyTo(0, buffer, offset, length);

            //// Optimization provided by Vincent Van Den Berghe: 
            //// http://search-lucene.com/m/Lucene.Net/j1zMf1uckOzOYqsi?subj=Proposal+to+speed+up+implementation+of+LowercaseFilter+charUtils+ToLower
            //new string(buffer, offset, length)
            //    .ToLowerInvariant()
            //    .CopyTo(0, buffer, offset, length);

            //// Original (slow) Lucene implementation:
            //int limit = length - offset;
            //for (int i = offset; i < limit;)
            //{
            //    i += Character.ToChars(
            //        Character.ToLower(
            //            CodePointAt(buffer, i, limit), CultureInfo.InvariantCulture), buffer, i);
            //}
        }

        /// <summary>
        /// Converts each unicode codepoint to UpperCase via <see cref="TextInfo.ToUpper(string)"/> in the invariant culture starting 
        /// at the given offset. </summary>
        /// <param name="buffer"> the char buffer to UPPERCASE </param>
        /// <param name="offset"> the offset to start at </param>
        /// <param name="length"> the number of characters in the buffer to lower case </param>
        public virtual void ToUpper(char[] buffer, int offset, int length) // LUCENENET specific - marked virtual so we can override the default
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(buffer.Length >= length);
                Debugging.Assert(offset <= 0 && offset <= buffer.Length);
            }

            // Reduce 2 heap allocations by using the stack and spans
            var source = new ReadOnlySpan<char>(buffer, offset, length);
            var destination = buffer.AsSpan(offset, length);
            var spare = length * sizeof(char) <= Constants.MaxStackByteLimit ? stackalloc char[length] : new char[length];
            source.ToUpper(spare, CultureInfo.InvariantCulture);
            spare.CopyTo(destination);

            //// Slight optimization, eliminating a few method calls internally
            //CultureInfo.InvariantCulture.TextInfo
            //    .ToUpper(new string(buffer, offset, length))
            //    .CopyTo(0, buffer, offset, length);

            //// Optimization provided by Vincent Van Den Berghe: 
            //// http://search-lucene.com/m/Lucene.Net/j1zMf1uckOzOYqsi?subj=Proposal+to+speed+up+implementation+of+LowercaseFilter+charUtils+ToLower
            //new string(buffer, offset, length)
            //    .ToUpperInvariant()
            //    .CopyTo(0, buffer, offset, length);

            //// Original (slow) Lucene implementation:
            //int limit = length - offset;
            //for (int i = offset; i < limit;)
            //{
            //    i += Character.ToChars(
            //        Character.ToUpper(
            //            CodePointAt(buffer, i, limit), CultureInfo.InvariantCulture), buffer, i);
            //}
        }

        /// <summary>
        /// Converts a sequence of .NET characters to a sequence of unicode code points. </summary>
        ///  <returns> The number of code points written to the destination buffer.  </returns>
        public int ToCodePoints(char[] src, int srcOff, int srcLen, int[] dest, int destOff)
        {
            if (srcLen < 0)
            {
                // LUCENENET: Changed from IllegalArgumentException to ArgumentOutOfRangeException
                throw new ArgumentOutOfRangeException(nameof(srcLen), "srcLen must be >= 0");
            }
            int codePointCount = 0;
            for (int i = 0; i < srcLen; )
            {
                int cp = CodePointAt(src, srcOff + i, srcOff + srcLen);
                int charCount = Character.CharCount(cp);
                dest[destOff + codePointCount++] = cp;
                i += charCount;
            }
            return codePointCount;
        }

        /// <summary>
        /// Converts a sequence of unicode code points to a sequence of .NET characters. </summary>
        ///  <returns> the number of chars written to the destination buffer  </returns>
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "By design")]
        public int ToChars(int[] src, int srcOff, int srcLen, char[] dest, int destOff)
        {
            if (srcLen < 0)
            {
                // LUCENENET: Changed from IllegalArgumentException to ArgumentOutOfRangeException
                throw new ArgumentOutOfRangeException(nameof(srcLen), "srcLen must be >= 0");
            }
            int written = 0;
            for (int i = 0; i < srcLen; ++i)
            {
                written += Character.ToChars(src[srcOff + i], dest, destOff + written);
            }
            return written;
        }

        /// <summary>
        /// Fills the <see cref="CharacterBuffer"/> with characters read from the given
        /// reader <see cref="TextReader"/>. This method tries to read <code>numChars</code>
        /// characters into the <see cref="CharacterBuffer"/>, each call to fill will start
        /// filling the buffer from offset <c>0</c> up to <paramref name="numChars"/>.
        /// In case code points can span across 2 java characters, this method may
        /// only fill <c>numChars - 1</c> characters in order not to split in
        /// the middle of a surrogate pair, even if there are remaining characters in
        /// the <see cref="TextReader"/>.
        /// <para>
        /// Depending on the <see cref="LuceneVersion"/> passed to
        /// <see cref="CharacterUtils.GetInstance(LuceneVersion)"/> this method implements
        /// supplementary character awareness when filling the given buffer. For all
        /// <see cref="LuceneVersion"/> &gt; 3.0 <see cref="Fill(CharacterBuffer, TextReader, int)"/> guarantees
        /// that the given <see cref="CharacterBuffer"/> will never contain a high surrogate
        /// character as the last element in the buffer unless it is the last available
        /// character in the reader. In other words, high and low surrogate pairs will
        /// always be preserved across buffer boarders.
        /// </para>
        /// <para>
        /// A return value of <c>false</c> means that this method call exhausted
        /// the reader, but there may be some bytes which have been read, which can be
        /// verified by checking whether <c>buffer.Length &gt; 0</c>.
        /// </para>
        /// </summary>
        /// <param name="buffer">
        ///          the buffer to fill. </param>
        /// <param name="reader">
        ///          the reader to read characters from. </param>
        /// <param name="numChars">
        ///          the number of chars to read </param>
        /// <returns> <code>false</code> if and only if reader.read returned -1 while trying to fill the buffer </returns>
        /// <exception cref="IOException">
        ///           if the reader throws an <see cref="IOException"/>. </exception>
        public abstract bool Fill(CharacterBuffer buffer, TextReader reader, int numChars);

        /// <summary>
        /// Convenience method which calls <c>Fill(buffer, reader, buffer.Buffer.Length)</c>. </summary>
        public virtual bool Fill(CharacterBuffer buffer, TextReader reader)
        {
            return Fill(buffer, reader, buffer.Buffer.Length);
        }

        /// <summary>
        /// Return the index within <c>buf[start:start+count]</c> which is by <paramref name="offset"/>
        /// code points from <paramref name="index"/>. 
        /// </summary>
        public abstract int OffsetByCodePoints(char[] buf, int start, int count, int index, int offset);

        private static int ReadFully(TextReader reader, char[] dest, int offset, int len)
        {
            int read = 0;
            while (read < len)
            {
                int r = reader.Read(dest, offset + read, len - read);
                if (r <= 0)
                {
                    break;
                }
                read += r;
            }
            return read;
        }

        private sealed class Java5CharacterUtils : CharacterUtils
        {
            public override int CodePointAt(string seq, int offset)
            {
                return Character.CodePointAt(seq, offset);
            }
            public override int CodePointAt(ICharSequence seq, int offset)
            {
                return Character.CodePointAt(seq, offset);
            }

            public override int CodePointAt(char[] chars, int offset, int limit)
            {
                return Character.CodePointAt(chars, offset, limit);
            }

            public override bool Fill(CharacterBuffer buffer, TextReader reader, int numChars)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(buffer.Buffer.Length >= 2);
                if (numChars < 2 || numChars > buffer.Buffer.Length)
                {
                    // LUCENENET: Changed from IllegalArgumentException to ArgumentOutOfRangeException
                    throw new ArgumentOutOfRangeException(nameof(numChars), "numChars must be >= 2 and <= the buffer size");
                }
                char[] charBuffer = buffer.Buffer;
                buffer.offset = 0;
                int offset;

                // Install the previously saved ending high surrogate:
                if (buffer.lastTrailingHighSurrogate != 0)
                {
                    charBuffer[0] = buffer.lastTrailingHighSurrogate;
                    buffer.lastTrailingHighSurrogate = (char)0;
                    offset = 1;
                }
                else
                {
                    offset = 0;
                }

                int read = ReadFully(reader, charBuffer, offset, numChars - offset);

                buffer.length = offset + read;
                bool result = buffer.length == numChars;
                if (buffer.length < numChars)
                {
                    // We failed to fill the buffer. Even if the last char is a high
                    // surrogate, there is nothing we can do
                    return result;
                }

                if (char.IsHighSurrogate(charBuffer[buffer.length - 1]))
                {
                    buffer.lastTrailingHighSurrogate = charBuffer[--buffer.length];
                }
                return result;
            }

            public override int CodePointCount(string seq)
            {
                if (seq is null)
                    throw new ArgumentNullException(nameof(seq)); // LUCENENET specific - added null guard clause

                return Character.CodePointCount(seq, 0, seq.Length);
            }

            public override int CodePointCount(ICharSequence seq)
            {
                if (seq is null)
                    throw new ArgumentNullException(nameof(seq)); // LUCENENET specific - added null guard clause

                return Character.CodePointCount(seq, 0, seq.Length);
            }

            public override int CodePointCount(char[] seq)
            {
                if (seq is null)
                    throw new ArgumentNullException(nameof(seq)); // LUCENENET specific - added null guard clause

                return Character.CodePointCount(seq, 0, seq.Length);
            }

            public override int CodePointCount(StringBuilder seq)
            {
                if (seq is null)
                    throw new ArgumentNullException(nameof(seq)); // LUCENENET specific - added null guard clause

                return Character.CodePointCount(seq, 0, seq.Length);
            }

            public override int OffsetByCodePoints(char[] buf, int start, int count, int index, int offset)
            {
                return Character.OffsetByCodePoints(buf, start, count, index, offset);
            }
        }

        // LUCENENET specific - not sealed so we can make another override to handle BW compatibility
        // with broken unicode support (Lucene 3.0). See the TestCharArraySet.TestSupplementaryCharsBWCompat()
        // and TestCharArraySet.TestSingleHighSurrogateBWComapt() tests.
        private class Java4CharacterUtils : CharacterUtils
        {
            public override int CodePointAt(string seq, int offset)
            {
                // LUCENENET specific - added guard clauses
                if (seq is null)
                    throw new ArgumentNullException(nameof(seq));
                if (offset < 0 || offset >= seq.Length)
                    throw new ArgumentOutOfRangeException(nameof(offset));

                return seq[offset];
            }

            public override int CodePointAt(ICharSequence seq, int offset)
            {
                // LUCENENET specific - added guard clauses
                if (seq is null)
                    throw new ArgumentNullException(nameof(seq));
                if (offset < 0 || offset >= seq.Length)
                    throw new ArgumentOutOfRangeException(nameof(offset));

                return seq[offset];
            }

            public override int CodePointAt(char[] chars, int offset, int limit)
            {
                if (chars is null)
                    throw new ArgumentNullException(nameof(chars)); // LUCENENET specific - added null guard clause
                if (offset >= limit)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset), $"{nameof(offset)} must be less than limit.");
                }
                // LUCENENET specific - added array bound check
                if (offset < 0  || offset >= chars.Length)
                    throw new ArgumentOutOfRangeException(nameof(offset), $"{nameof(offset)} must not be negative and be less than chars.Length.");

                return chars[offset];
            }

            public override bool Fill(CharacterBuffer buffer, TextReader reader, int numChars)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(buffer.Buffer.Length >= 1);
                if (numChars < 1 || numChars > buffer.Buffer.Length)
                {
                    // LUCENENET: Changed from IllegalArgumentException to ArgumentOutOfRangeException
                    throw new ArgumentOutOfRangeException(nameof(numChars), "numChars must be >= 1 and <= the buffer size");
                }
                buffer.offset = 0;
                int read = ReadFully(reader, buffer.Buffer, 0, numChars);
                buffer.length = read;
                buffer.lastTrailingHighSurrogate = (char)0;
                return read == numChars;
            }

            public override int CodePointCount(string seq)
            {
                if (seq is null)
                    throw new ArgumentNullException(nameof(seq)); // LUCENENET specific - added null guard clause

                return seq.Length;
            }

            public override int CodePointCount(ICharSequence seq)
            {
                if (seq is null)
                    throw new ArgumentNullException(nameof(seq)); // LUCENENET specific - added null guard clause

                return seq.Length;
            }

            public override int CodePointCount(char[] seq)
            {
                if (seq is null)
                    throw new ArgumentNullException(nameof(seq)); // LUCENENET specific - added null guard clause

                return seq.Length;
            }

            public override int CodePointCount(StringBuilder seq)
            {
                if (seq is null)
                    throw new ArgumentNullException(nameof(seq)); // LUCENENET specific - added null guard clause

                return seq.Length;
            }

            public override int OffsetByCodePoints(char[] buf, int start, int count, int index, int offset)
            {
                // LUCENENET: Checks for int overflow
                uint result = (uint)index + (uint)offset;
                if (result < 0 || result > count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index) + " + " + nameof(offset), "index + offset must be >= 0 and <= count");
                }
                return (int)result;
            }
        }

        // LUCENENET specific class to handle BW compatibility
        // with broken unicode support (Lucene 3.0). See the TestCharArraySet.TestSupplementaryCharsBWCompat()
        // and TestCharArraySet.TestSingleHighSurrogateBWComapt() tests. This just provides the old (slower)
        // implementation that represents the original Lucene toUpperCase and toLowerCase methods.
        private class Java4CharacterUtilsBWCompatibility : Java4CharacterUtils
        {
            public override void ToLower(char[] buffer, int offset, int limit)
            {
                if (buffer is null)
                    throw new ArgumentNullException(nameof(buffer)); // LUCENENET specific - added null guard clause

                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(buffer.Length >= limit);
                    Debugging.Assert(offset <= 0 && offset <= buffer.Length);
                }

                for (int i = offset; i < limit;)
                {
                    i += Character.ToChars(
                        Character.ToLower(
                            CodePointAt(buffer, i, limit), CultureInfo.InvariantCulture), buffer, i);
                }
            }

            public override void ToUpper(char[] buffer, int offset, int limit)
            {
                if (buffer is null)
                    throw new ArgumentNullException(nameof(buffer)); // LUCENENET specific - added null guard clause

                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(buffer.Length >= limit);
                    Debugging.Assert(offset <= 0 && offset <= buffer.Length);
                }

                for (int i = offset; i < limit;)
                {
                    i += Character.ToChars(
                        Character.ToUpper(
                            CodePointAt(buffer, i, limit), CultureInfo.InvariantCulture), buffer, i);
                }
            }
        }

        /// <summary>
        /// A simple IO buffer to use with
        /// <see cref="CharacterUtils.Fill(CharacterBuffer, TextReader)"/>.
        /// </summary>
        public sealed class CharacterBuffer
        {
            private readonly char[] buffer;
            internal int offset;
            internal int length;
            // NOTE: not private so outer class can access without
            // $access methods:
            internal char lastTrailingHighSurrogate;

            internal CharacterBuffer(char[] buffer, int offset, int length)
            {
                this.buffer = buffer;
                this.offset = offset;
                this.length = length;
            }

            /// <summary>
            /// Returns the internal buffer
            /// </summary>
            /// <returns> the buffer </returns>
            [WritableArray]
            [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
            public char[] Buffer => buffer;

            /// <summary>
            /// Returns the data offset in the internal buffer.
            /// </summary>
            /// <returns> the offset </returns>
            public int Offset => offset;

            /// <summary>
            /// Return the length of the data in the internal buffer starting at
            /// <see cref="Offset"/>
            /// </summary>
            /// <returns> the length </returns>
            public int Length => length;

            /// <summary>
            /// Resets the CharacterBuffer. All internals are reset to its default
            /// values.
            /// </summary>
            public void Reset()
            {
                offset = 0;
                length = 0;
                lastTrailingHighSurrogate = (char)0;
            }
        }
    }
}