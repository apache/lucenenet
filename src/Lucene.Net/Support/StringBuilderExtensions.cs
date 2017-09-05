using System;
using System.Text;

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

    public static class StringBuilderExtensions
    {
        /// <summary>
        /// Causes this character sequence to be replaced by the reverse of
        /// the sequence. If there are any surrogate pairs included in the
        /// sequence, these are treated as single characters for the
        /// reverse operation. Thus, the order of the high-low surrogates
        /// is never reversed.
        /// <para/>
        /// Let <c>n</c> be the character length of this character sequence
        /// (not the length in <see cref="char"/> values) just prior to
        /// execution of the <see cref="Reverse"/> method. Then the
        /// character at index <c>k</c> in the new character sequence is
        /// equal to the character at index <c>n-k-1</c> in the old
        /// character sequence.
        /// <para/>
        /// Note that the reverse operation may result in producing
        /// surrogate pairs that were unpaired low-surrogates and
        /// high-surrogates before the operation. For example, reversing
        /// "&#92;uDC00&#92;uD800" produces "&#92;uD800&#92;uDC00" which is
        /// a valid surrogate pair.
        /// </summary>
        /// <param name="text">this <see cref="StringBuilder"/></param>
        /// <returns>a reference to this <see cref="StringBuilder"/>.</returns>
        public static StringBuilder Reverse(this StringBuilder text)
        {
            bool hasSurrogate = false;
            int codePointCount = text.Length;
            int n = text.Length - 1;
            for (int j = (n - 1) >> 1; j >= 0; --j)
            {
                char temp = text[j];
                char temp2 = text[n - j];
                if (!hasSurrogate)
                {
                    hasSurrogate = (temp >= Character.MIN_SURROGATE && temp <= Character.MAX_SURROGATE)
                        || (temp2 >= Character.MIN_SURROGATE && temp2 <= Character.MAX_SURROGATE);
                }
                text[j] = temp2;
                text[n - j] = temp;
            }
            if (hasSurrogate)
            {
                // Reverse back all valid surrogate pairs
                for (int i = 0; i < text.Length - 1; i++)
                {
                    char c2 = text[i];
                    if (char.IsLowSurrogate(c2))
                    {
                        char c1 = text[i + 1];
                        if (char.IsHighSurrogate(c1))
                        {
                            text[i++] = c1;
                            text[i] = c2;
                        }
                    }
                }
            }

            return text;
        }

        /// <summary>
        /// Returns the number of Unicode code points in the specified text
        /// range of this <see cref="StringBuilder"/>. The text range begins at the specified
        /// <paramref name="beginIndex"/> and extends to the <see cref="char"/> at
        /// index <c>endIndex - 1</c>. Thus the length (in
        /// <see cref="char"/>s) of the text range is
        /// <c>endIndex-beginIndex</c>. Unpaired surrogates within
        /// this sequence count as one code point each.
        /// </summary>
        /// <param name="text">this <see cref="StringBuilder"/></param>
        /// <param name="beginIndex">the index to the first <see cref="char"/> of the text range.</param>
        /// <param name="endIndex">the index after the last <see cref="char"/> of the text range.</param>
        /// <returns>the number of Unicode code points in the specified text range.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// if the <paramref name="beginIndex"/> is negative, or <paramref name="endIndex"/>
        /// is larger than the length of this sequence, or
        /// <paramref name="beginIndex"/> is larger than <paramref name="endIndex"/>.
        /// </exception>
        public static int CodePointCount(this StringBuilder text, int beginIndex, int endIndex)
        {
            if (beginIndex < 0 || endIndex > text.Length || beginIndex > endIndex)
            {
                throw new IndexOutOfRangeException();
            }
            return Character.CodePointCountImpl(text.GetChars(), beginIndex, endIndex - beginIndex);
        }

        /// <summary>
        /// Returns the character (Unicode code point) at the specified index. 
        /// The index refers to char values (Unicode code units) and ranges from 0 to Length - 1.
        /// <para/>
        /// If the char value specified at the given index is in the high-surrogate range, 
        /// the following index is less than the length of this sequence, and the char value 
        /// at the following index is in the low-surrogate range, then the 
        /// supplementary code point corresponding to this surrogate pair is returned. 
        /// Otherwise, the char value at the given index is returned.
        /// </summary>
        /// <param name="text">this <see cref="StringBuilder"/></param>
        /// <param name="index">the index to the char values</param>
        /// <returns>the code point value of the character at the index</returns>
        /// <exception cref="IndexOutOfRangeException">if the index argument is negative or not less than the length of this sequence.</exception>
        public static int CodePointAt(this StringBuilder text, int index)
        {
            if ((index < 0) || (index >= text.Length))
            {
                throw new IndexOutOfRangeException();
            }
            return Character.CodePointAt(text.ToString(), index);
        }

        /// <summary>
        /// Copies the array from the <see cref="StringBuilder"/> into a new array
        /// and returns it.
        /// </summary>
        /// <param name="text">this <see cref="StringBuilder"/></param>
        /// <returns></returns>
        public static char[] GetChars(this StringBuilder text)
        {
            char[] chars = new char[text.Length];
            text.CopyTo(0, chars, 0, text.Length);
            return chars;
        }

        /// <summary>
        /// Appends the string representation of the <paramref name="codePoint"/>
        /// argument to this sequence.
        /// 
        /// <para>
        /// The argument is appended to the contents of this sequence.
        /// The length of this sequence increases by <see cref="Character.CharCount(int)"/>.
        /// </para>
        /// <para>
        /// The overall effect is exactly as if the argument were
        /// converted to a <see cref="char"/> array by the method
        /// <see cref="Character.ToChars(int)"/> and the character in that array
        /// were then <see cref="StringBuilder.Append(char[])">appended</see> to this 
        /// <see cref="StringBuilder"/>.
        /// </para>
        /// </summary>
        /// <param name="text">This <see cref="StringBuilder"/>.</param>
        /// <param name="codePoint">a Unicode code point</param>
        /// <returns>a reference to this object.</returns>
        public static StringBuilder AppendCodePoint(this StringBuilder text, int codePoint)
        {
            text.Append(Character.ToChars(codePoint));
            return text;
        }

        /// <summary>
        /// Searches for the first index of the specified character. The search for
        /// the character starts at the beginning and moves towards the end.
        /// </summary>
        /// <param name="text">This <see cref="StringBuilder"/>.</param>
        /// <param name="value">The string to find.</param>
        /// <returns>The index of the specified character, or -1 if the character isn't found.</returns>
        public static int IndexOf(this StringBuilder text, string value)
        {
            return IndexOf(text, value, 0);
        }

        /// <summary>
        /// Searches for the index of the specified character. The search for the
        /// character starts at the specified offset and moves towards the end.
        /// </summary>
        /// <param name="text">This <see cref="StringBuilder"/>.</param>
        /// <param name="value">The string to find.</param>
        /// <param name="startIndex">The starting offset.</param>
        /// <returns>The index of the specified character, or -1 if the character isn't found.</returns>
        public static int IndexOf(this StringBuilder text, string value, int startIndex)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            if (value == null)
                throw new ArgumentNullException("value");

            int index;
            int length = value.Length;
            int maxSearchLength = (text.Length - length) + 1;

            for (int i = startIndex; i < maxSearchLength; ++i)
            {
                if (text[i] == value[0])
                {
                    index = 1;
                    while ((index < length) && (text[i + index] == value[index]))
                        ++index;

                    if (index == length)
                        return i;
                }
            }

            return -1;
        }
    }
}