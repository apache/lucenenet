using System;
using System.Collections.Generic;
using System.IO;
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

    public static class DictionaryExtensions
    {
        public static void PutAll<TKey, TValue>(this IDictionary<TKey, TValue> dict, IEnumerable<KeyValuePair<TKey, TValue>> kvps)
        {
            foreach (var kvp in kvps)
            {
                dict[kvp.Key] = kvp.Value;
            }
        }

        // LUCENENET TODO: Maybe factor this out? Dictionaries already expose their entries and there is
        // little point in putting them into a set just so you can enumerate them.
        public static ISet<KeyValuePair<TKey, TValue>> EntrySet<TKey, TValue>(this IDictionary<TKey, TValue> dict)
        {
            ISet<KeyValuePair<TKey, TValue>> iset = new HashSet<KeyValuePair<TKey, TValue>>();
            foreach (KeyValuePair<TKey, TValue> kvp in dict)
            {
                iset.Add(kvp);
            }
            return iset;
        }

        public static TValue Put<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (dict == null)
                return default(TValue);

            var oldValue = dict.ContainsKey(key) ? dict[key] : default(TValue);
            dict[key] = value;
            return oldValue;
        }

        private static readonly int NONE = 0, SLASH = 1, UNICODE = 2, CONTINUE = 3,
            KEY_DONE = 4, IGNORE = 5;
        private static string lineSeparator = Environment.NewLine;


        // LUCENENET NOTE: Sourced from Apache Harmony:

        /// <summary>
        /// Loads properties from the specified <see cref="Stream"/>. The encoding is
        /// ISO8859-1. 
        /// </summary>
        /// <remarks>
        /// The Properties file is interpreted according to the
        /// following rules:
        /// <list type="bullet">
        ///     <item><description>
        ///         Empty lines are ignored.
        ///     </description></item>
        ///     <item><description>
        ///         Lines starting with either a "#" or a "!" are comment lines and are
        ///         ignored.
        ///     </description></item>
        ///     <item><description>
        ///         A backslash at the end of the line escapes the following newline
        ///         character ("\r", "\n", "\r\n"). If there's a whitespace after the
        ///         backslash it will just escape that whitespace instead of concatenating
        ///         the lines. This does not apply to comment lines.
        ///     </description></item>
        ///     <item><description>
        ///         A property line consists of the key, the space between the key and
        ///         the value, and the value. The key goes up to the first whitespace, "=" or
        ///         ":" that is not escaped. The space between the key and the value contains
        ///         either one whitespace, one "=" or one ":" and any number of additional
        ///         whitespaces before and after that character. The value starts with the
        ///         first character after the space between the key and the value.
        ///     </description></item>
        ///     <item><description>
        ///         Following escape sequences are recognized: "\ ", "\\", "\r", "\n",
        ///         "\!", "\#", "\t", "\b", "\f", and "&#92;uXXXX" (unicode character).
        ///     </description></item>
        /// </list>
        /// <para/>
        /// This method is to mimic and interoperate with the Properties class in Java, which
        /// is essentially a string dictionary that natively supports importing and exporting to this format.
        /// </remarks>
        /// <param name="dict">This dictionary.</param>
        /// <param name="input">The <see cref="Stream"/>.</param>
        /// <exception cref="IOException">If error occurs during reading from the <see cref="Stream"/>.</exception>
        public static void Load(this IDictionary<string, string> dict, Stream input)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            lock (dict)
            {
                int mode = NONE, unicode = 0, count = 0;
                char nextChar;
                char[] buf = new char[40];
                int offset = 0, keyLength = -1, intVal;
                bool firstChar = true;
                Stream bis = input;

                while (true)
                {
                    intVal = bis.ReadByte();
                    if (intVal == -1)
                    {
                        // if mode is UNICODE but has less than 4 hex digits, should
                        // throw an IllegalArgumentException
                        // luni.08=Invalid Unicode sequence: expected format \\uxxxx
                        if (mode == UNICODE && count < 4)
                        {
                            throw new ArgumentException("Invalid Unicode sequence: expected format \\uxxxx"); //$NON-NLS-1$
                        }
                        // if mode is SLASH and no data is read, should append '\u0000'
                        // to buf
                        if (mode == SLASH)
                        {
                            buf[offset++] = '\u0000';
                        }
                        break;
                    }
                    nextChar = (char)(intVal & 0xff);

                    if (offset == buf.Length)
                    {
                        char[] newBuf = new char[buf.Length * 2];
                        System.Array.Copy(buf, 0, newBuf, 0, offset);
                        buf = newBuf;
                    }
                    if (mode == UNICODE)
                    {
                        int digit = Character.Digit(nextChar, 16);
                        if (digit >= 0)
                        {
                            unicode = (unicode << 4) + digit;
                            if (++count < 4)
                            {
                                continue;
                            }
                        }
                        else if (count <= 4)
                        {
                            // luni.09=Invalid Unicode sequence: illegal character
                            throw new ArgumentException("Invalid Unicode sequence: illegal character"); //$NON-NLS-1$
                        }
                        mode = NONE;
                        buf[offset++] = (char)unicode;
                        if (nextChar != '\n')
                        {
                            continue;
                        }
                    }
                    if (mode == SLASH)
                    {
                        mode = NONE;
                        switch (nextChar)
                        {
                            case '\r':
                                mode = CONTINUE; // Look for a following \n
                                continue;
                            case '\n':
                                mode = IGNORE; // Ignore whitespace on the next line
                                continue;
                            case 'b':
                                nextChar = '\b';
                                break;
                            case 'f':
                                nextChar = '\f';
                                break;
                            case 'n':
                                nextChar = '\n';
                                break;
                            case 'r':
                                nextChar = '\r';
                                break;
                            case 't':
                                nextChar = '\t';
                                break;
                            case 'u':
                                mode = UNICODE;
                                unicode = count = 0;
                                continue;
                        }
                    }
                    else
                    {
                        switch (nextChar)
                        {
                            case '#':
                            case '!':
                                if (firstChar)
                                {
                                    while (true)
                                    {
                                        intVal = bis.ReadByte();
                                        if (intVal == -1)
                                        {
                                            break;
                                        }
                                        // & 0xff not required
                                        nextChar = (char)intVal;
                                        if (nextChar == '\r' || nextChar == '\n')
                                        {
                                            break;
                                        }
                                    }
                                    continue;
                                }
                                break;
                            case '\n':
                                if (mode == CONTINUE)
                                { // Part of a \r\n sequence
                                    mode = IGNORE; // Ignore whitespace on the next line
                                    continue;
                                }
                                // fall into the next case
                                mode = NONE;
                                firstChar = true;
                                if (offset > 0 || (offset == 0 && keyLength == 0))
                                {
                                    if (keyLength == -1)
                                    {
                                        keyLength = offset;
                                    }
                                    string temp = new string(buf, 0, offset);
                                    dict.Put(temp.Substring(0, keyLength), temp
                                            .Substring(keyLength));
                                }
                                keyLength = -1;
                                offset = 0;
                                continue;
                            case '\r':
                                mode = NONE;
                                firstChar = true;
                                if (offset > 0 || (offset == 0 && keyLength == 0))
                                {
                                    if (keyLength == -1)
                                    {
                                        keyLength = offset;
                                    }
                                    string temp = new string(buf, 0, offset);
                                    dict.Put(temp.Substring(0, keyLength), temp
                                            .Substring(keyLength));
                                }
                                keyLength = -1;
                                offset = 0;
                                continue;
                            case '\\':
                                if (mode == KEY_DONE)
                                {
                                    keyLength = offset;
                                }
                                mode = SLASH;
                                continue;
                            case ':':
                            case '=':
                                if (keyLength == -1)
                                { // if parsing the key
                                    mode = NONE;
                                    keyLength = offset;
                                    continue;
                                }
                                break;
                        }
                        if (nextChar < 256 && char.IsWhiteSpace(nextChar))
                        {
                            if (mode == CONTINUE)
                            {
                                mode = IGNORE;
                            }
                            // if key length == 0 or value length == 0
                            if (offset == 0 || offset == keyLength || mode == IGNORE)
                            {
                                continue;
                            }
                            if (keyLength == -1)
                            { // if parsing the key
                                mode = KEY_DONE;
                                continue;
                            }
                        }
                        if (mode == IGNORE || mode == CONTINUE)
                        {
                            mode = NONE;
                        }
                    }
                    firstChar = false;
                    if (mode == KEY_DONE)
                    {
                        keyLength = offset;
                        mode = NONE;
                    }
                    buf[offset++] = nextChar;
                }
                if (keyLength == -1 && offset > 0)
                {
                    keyLength = offset;
                }
                if (keyLength >= 0)
                {
                    string temp = new string(buf, 0, offset);
                    dict.Put(temp.Substring(0, keyLength), temp.Substring(keyLength));
                }
            }
        }

        /// <summary>
        /// Stores the mappings in this Properties to the specified
        /// <see cref="Stream"/>, putting the specified comment at the beginning. The
        /// output from this method is suitable for being read by the
        /// <see cref="Load(IDictionary{string, string}, Stream)"/> method.
        /// </summary>
        /// <param name="dict">This dictionary.</param>
        /// <param name="output">The output <see cref="Stream"/> to write to.</param>
        /// <param name="comments">The comments to put at the beginning.</param>
        /// <exception cref="IOException">If an error occurs during the write to the <see cref="Stream"/>.</exception>
        /// <exception cref="InvalidCastException">If the key or value of a mapping is not a <see cref="string"/>.</exception>
        public static void Store(this IDictionary<string, string> dict, Stream output, string comments)
        {
            lock (dict)
            {
                StreamWriter writer = new StreamWriter(output, Encoding.GetEncoding("iso-8859-1")); //$NON-NLS-1$
                if (comments != null)
                {
                    WriteComments(writer, comments);
                }
                writer.Write('#');
                writer.Write(new DateTime().ToString("yyyy-MM-dd"));
                writer.Write(lineSeparator);

                StringBuilder buffer = new StringBuilder(200);
                foreach (var entry in dict)
                {
                    string key = entry.Key;
                    DumpString(buffer, key, true);
                    buffer.Append('=');
                    DumpString(buffer, entry.Value, false);
                    buffer.Append(lineSeparator);
                    writer.Write(buffer.ToString());
                    buffer.Length = 0;
                }
                writer.Flush();
            }
        }

        private static void WriteComments(TextWriter writer, string comments)
        {
            writer.Write('#');
            char[] chars = comments.ToCharArray();
            for (int index = 0; index < chars.Length; index++)
            {
                if (chars[index] == '\r' || chars[index] == '\n')
                {
                    int indexPlusOne = index + 1;
                    if (chars[index] == '\r' && indexPlusOne < chars.Length
                            && chars[indexPlusOne] == '\n')
                    {
                        // "\r\n"
                        continue;
                    }
                    writer.Write(lineSeparator);
                    if (indexPlusOne < chars.Length
                            && (chars[indexPlusOne] == '#' || chars[indexPlusOne] == '!'))
                    {
                        // return char with either '#' or '!' afterward
                        continue;
                    }
                    writer.Write('#');
                }
                else
                {
                    writer.Write(chars[index]);
                }
            }
            writer.Write(lineSeparator);
        }

        private static void DumpString(StringBuilder buffer, string str, bool isKey)
        {
            int index = 0, length = str.Length;
            if (!isKey && index < length && str[index] == ' ')
            {
                buffer.Append("\\ "); //$NON-NLS-1$
                index++;
            }

            for (; index < length; index++)
            {
                char ch = str[index];
                switch (ch)
                {
                    case '\t':
                        buffer.Append("\\t"); //$NON-NLS-1$
                        break;
                    case '\n':
                        buffer.Append("\\n"); //$NON-NLS-1$
                        break;
                    case '\f':
                        buffer.Append("\\f"); //$NON-NLS-1$
                        break;
                    case '\r':
                        buffer.Append("\\r"); //$NON-NLS-1$
                        break;
                    default:
                        if ("\\#!=:".IndexOf(ch) >= 0 || (isKey && ch == ' '))
                        {
                            buffer.Append('\\');
                        }
                        if (ch >= ' ' && ch <= '~')
                        {
                            buffer.Append(ch);
                        }
                        else
                        {
                            buffer.Append(ToHexaDecimal(ch));
                        }
                        break;
                }
            }
        }

        private static char[] ToHexaDecimal(int ch)
        {
            char[] hexChars = { '\\', 'u', '0', '0', '0', '0' };
            int hexChar, index = hexChars.Length, copyOfCh = ch;
            do
            {
                hexChar = copyOfCh & 15;
                if (hexChar > 9)
                {
                    hexChar = hexChar - 10 + 'A';
                }
                else
                {
                    hexChar += '0';
                }
                hexChars[--index] = (char)hexChar;
                //} while ((copyOfCh >>>= 4) != 0);
            } while ((copyOfCh = (int)((uint)copyOfCh >> 4)) != 0);
            return hexChars;
        }
    }
}