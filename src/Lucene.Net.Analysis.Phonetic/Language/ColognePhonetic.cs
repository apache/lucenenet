// commons-codec version compatibility level: 1.9
using Lucene.Net.Support;
using System;
using System.Globalization;

namespace Lucene.Net.Analysis.Phonetic.Language
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
    /// Encodes a string into a Cologne Phonetic value.
    /// </summary>
    /// <remarks>
    /// Implements the <a href="http://de.wikipedia.org/wiki/K%C3%B6lner_Phonetik">K&#214;lner Phonetik</a>
    /// (Cologne Phonetic) algorithm issued by Hans Joachim Postel in 1969.
    /// <para/>
    /// The <i>K&#214;lner Phonetik</i> is a phonetic algorithm which is optimized for the German language.
    /// It is related to the well-known soundex algorithm.
    /// <para/>
    /// <h2>Algorithm</h2>
    /// <list type="bullet">
    ///     <item>
    ///         <term>Step 1:</term>
    ///         <description>
    ///             After preprocessing (conversion to upper case, transcription of <a
    ///             href="http://en.wikipedia.org/wiki/Germanic_umlaut">germanic umlauts</a>, removal of non alphabetical characters) the
    ///             letters of the supplied text are replaced by their phonetic code according to the following table.
    ///             <list type="table">
    ///                 <listheader>
    ///                     <term>Letter</term>
    ///                     <term>Context</term>
    ///                     <term>Code</term>
    ///                 </listheader>
    ///                 <item>
    ///                     <term>A, E, I, J, O, U, Y</term>
    ///                     <term></term>
    ///                     <term>0</term>
    ///                 </item>
    ///                 <item>
    ///                     <term>H</term>
    ///                     <term></term>
    ///                     <term>-</term>
    ///                 </item>
    ///                 <item>
    ///                     <term>B</term>
    ///                     <term></term>
    ///                     <term>1</term>
    ///                 </item>
    ///                 <item>
    ///                     <term>P</term>
    ///                     <term>not before H</term>
    ///                     <term>1</term>
    ///                 </item>
    ///                 <item>
    ///                     <term>D, T</term>
    ///                     <term>not before C, S, Z</term>
    ///                     <term>2</term>
    ///                 </item>
    ///                 <item>
    ///                     <term>F, V, W</term>
    ///                     <term></term>
    ///                     <term>3</term>
    ///                 </item>
    ///                 <item>
    ///                     <term>P</term>
    ///                     <term>before H</term>
    ///                     <term>3</term>
    ///                 </item>
    ///                 <item>
    ///                     <term>G, K, Q</term>
    ///                     <term></term>
    ///                     <term>4</term>
    ///                 </item>
    ///                 <item>
    ///                     <term>C</term>
    ///                     <term>t onset before A, H, K, L, O, Q, R, U, X <para>OR</para>
    ///                     before A, H, K, O, Q, U, X except after S, Z</term>
    ///                     <term>4</term>
    ///                 </item>
    ///                 <item>
    ///                     <term>X</term>
    ///                     <term>not after C, K, Q</term>
    ///                     <term>48</term>
    ///                 </item>
    ///                 <item>
    ///                     <term>L</term>
    ///                     <term></term>
    ///                     <term>5</term>
    ///                 </item>
    ///                 <item>
    ///                     <term>M, N</term>
    ///                     <term></term>
    ///                     <term>6</term>
    ///                 </item>
    ///                 <item>
    ///                     <term>R</term>
    ///                     <term></term>
    ///                     <term>7</term>
    ///                 </item>
    ///                 <item>
    ///                     <term>S, Z</term>
    ///                     <term></term>
    ///                     <term>8</term>
    ///                 </item>
    ///                 <item>
    ///                     <term>C</term>
    ///                     <term>after S, Z <para>OR</para>
    ///                     at onset except before A, H, K, L, O, Q, R, U, X <para>OR</para>
    ///                     not before A, H, K, O, Q, U, X
    ///                     </term>
    ///                     <term>8</term>
    ///                 </item>
    ///                 <item>
    ///                     <term>D, T</term>
    ///                     <term>before C, S, Z</term>
    ///                     <term>8</term>
    ///                 </item>
    ///                 <item>
    ///                     <term>X</term>
    ///                     <term>after C, K, Q</term>
    ///                     <term>8</term>
    ///                 </item>
    ///             </list>
    ///             <para>
    ///                 <small><i>(Source: <a href= "http://de.wikipedia.org/wiki/K%C3%B6lner_Phonetik#Buchstabencodes" >Wikipedia (de):
    ///                 K&#214;lner Phonetik -- Buchstabencodes</a>)</i></small>
    ///             </para>
    ///             <h4>Example:</h4>
    ///             <c>"M&#220;ller-L&#220;denscheidt" => "MULLERLUDENSCHEIDT" => "6005507500206880022"</c>
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>Step 2:</term>
    ///         <description>
    ///             Collapse of all multiple consecutive code digits.
    ///             <h4>Example:</h4>
    ///             <c>"6005507500206880022" => "6050750206802"</c>
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>Step 3:</term>
    ///         <description>
    ///             Removal of all codes "0" except at the beginning. This means that two or more identical consecutive digits can occur
    ///             if they occur after removing the "0" digits.
    ///             <h4>Example:</h4>
    ///             <c>"6050750206802" => "65752682"</c>
    ///         </description>
    ///     </item>
    /// </list>
    /// <para/>
    /// This class is thread-safe.
    /// <para/>
    /// See: <a href="http://de.wikipedia.org/wiki/K%C3%B6lner_Phonetik">Wikipedia (de): K&#246;lner Phonetik (in German)</a>
    /// <para/>
    /// since 1.5
    /// </remarks>
    public class ColognePhonetic : IStringEncoder
    {
        // Predefined char arrays for better performance and less GC load
        private static readonly char[] AEIJOUY = new char[] { 'A', 'E', 'I', 'J', 'O', 'U', 'Y' };
        private static readonly char[] SCZ = new char[] { 'S', 'C', 'Z' };
        private static readonly char[] WFPV = new char[] { 'W', 'F', 'P', 'V' };
        private static readonly char[] GKQ = new char[] { 'G', 'K', 'Q' };
        private static readonly char[] CKQ = new char[] { 'C', 'K', 'Q' };
        private static readonly char[] AHKLOQRUX = new char[] { 'A', 'H', 'K', 'L', 'O', 'Q', 'R', 'U', 'X' };
        private static readonly char[] SZ = new char[] { 'S', 'Z' };
        private static readonly char[] AHOUKQX = new char[] { 'A', 'H', 'O', 'U', 'K', 'Q', 'X' };
        private static readonly char[] TDX = new char[] { 'T', 'D', 'X' };

        /// <summary>
        /// This class is not thread-safe; the field <see cref="m_length"/> is mutable.
        /// However, it is not shared between threads, as it is constructed on demand
        /// by the method <see cref="ColognePhonetic.GetColognePhonetic(string)"/>.
        /// </summary>
        private abstract class CologneBuffer
        {

            protected readonly char[] m_data;

            protected int m_length = 0;

            protected CologneBuffer(char[] data) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            {
                this.m_data = data;
                this.m_length = data.Length;
            }

            protected CologneBuffer(int buffSize) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            {
                this.m_data = new char[buffSize];
                this.m_length = 0;
            }

            protected abstract char[] CopyData(int start, int length);

            public virtual int Length => m_length;

            public override string ToString()
            {
                return new string(CopyData(0, m_length));
            }
        }

        private class CologneOutputBuffer : CologneBuffer
        {
            public CologneOutputBuffer(int buffSize)
                : base(buffSize)
            {
            }

            public void AddRight(char chr)
            {
                m_data[m_length] = chr;
                m_length++;
            }

            protected override char[] CopyData(int start, int length)
            {
                char[] newData = new char[length];
                Arrays.Copy(m_data, start, newData, 0, length);
                return newData;
            }
        }

        private class CologneInputBuffer : CologneBuffer
        {
            public CologneInputBuffer(char[] data)
                : base(data)
            {
            }

            public virtual void AddLeft(char ch)
            {
                m_length++;
                m_data[GetNextPos()] = ch;
            }

            protected override char[] CopyData(int start, int length)
            {
                char[] newData = new char[length];
                Arrays.Copy(m_data, m_data.Length - this.m_length + start, newData, 0, length);
                return newData;
            }

            public virtual char GetNextChar()
            {
                return m_data[GetNextPos()];
            }

            protected virtual int GetNextPos()
            {
                return m_data.Length - m_length;
            }

            public virtual char RemoveNext()
            {
                char ch = GetNextChar();
                m_length--;
                return ch;
            }
        }

        /// <summary>
        /// Maps some Germanic characters to plain for internal processing. The following characters are mapped:
        /// <list type="bullet">
        ///     <item><description>capital a, umlaut mark</description></item>
        ///     <item><description>capital u, umlaut mark</description></item>
        ///     <item><description>capital o, umlaut mark</description></item>
        ///     <item><description>small sharp s, German</description></item>
        /// </list>
        /// </summary>
        private static readonly char[][] PREPROCESS_MAP = {
            new char[] {'\u00C4', 'A'}, // capital a, umlaut mark
            new char[] {'\u00DC', 'U'}, // capital u, umlaut mark
            new char[] {'\u00D6', 'O'}, // capital o, umlaut mark
            new char[] {'\u00DF', 'S'} // small sharp s, German
        };

        /// <summary>
        /// Returns whether the array contains the key, or not.
        /// </summary>
        private static bool ArrayContains(char[] arr, char key)
        {
            foreach (char element in arr)
            {
                if (element == key)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// <para>
        /// Implements the <i>K&#246;lner Phonetik</i> algorithm.
        /// </para>
        /// <para>
        /// In contrast to the initial description of the algorithm, this implementation does the encoding in one pass.
        /// </para>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <returns>The corresponding encoding according to the <i>K&#246;lner Phonetik</i> algorithm</returns>
        public virtual string GetColognePhonetic(string text)
        {
            if (text is null)
            {
                return null;
            }

            text = Preprocess(text);

            CologneOutputBuffer output = new CologneOutputBuffer(text.Length * 2);
            CologneInputBuffer input = new CologneInputBuffer(text.ToCharArray());

            char nextChar;

            char lastChar = '-';
            char lastCode = '/';
            char code;
            char chr;

            int rightLength = input.Length;

            while (rightLength > 0)
            {
                chr = input.RemoveNext();

                if ((rightLength = input.Length) > 0)
                {
                    nextChar = input.GetNextChar();
                }
                else
                {
                    nextChar = '-';
                }

                if (ArrayContains(AEIJOUY, chr))
                {
                    code = '0';
                }
                else if (chr == 'H' || chr < 'A' || chr > 'Z')
                {
                    if (lastCode == '/')
                    {
                        continue;
                    }
                    code = '-';
                }
                else if (chr == 'B' || (chr == 'P' && nextChar != 'H'))
                {
                    code = '1';
                }
                else if ((chr == 'D' || chr == 'T') && !ArrayContains(SCZ, nextChar))
                {
                    code = '2';
                }
                else if (ArrayContains(WFPV, chr))
                {
                    code = '3';
                }
                else if (ArrayContains(GKQ, chr))
                {
                    code = '4';
                }
                else if (chr == 'X' && !ArrayContains(CKQ, lastChar))
                {
                    code = '4';
                    input.AddLeft('S');
                    rightLength++;
                }
                else if (chr == 'S' || chr == 'Z')
                {
                    code = '8';
                }
                else if (chr == 'C')
                {
                    if (lastCode == '/')
                    {
                        if (ArrayContains(AHKLOQRUX, nextChar))
                        {
                            code = '4';
                        }
                        else
                        {
                            code = '8';
                        }
                    }
                    else
                    {
                        if (ArrayContains(SZ, lastChar) || !ArrayContains(AHOUKQX, nextChar))
                        {
                            code = '8';
                        }
                        else
                        {
                            code = '4';
                        }
                    }
                }
                else if (ArrayContains(TDX, chr))
                {
                    code = '8';
                }
                else if (chr == 'R')
                {
                    code = '7';
                }
                else if (chr == 'L')
                {
                    code = '5';
                }
                else if (chr == 'M' || chr == 'N')
                {
                    code = '6';
                }
                else
                {
                    code = chr;
                }

                if (code != '-' && (lastCode != code && (code != '0' || lastCode == '/') || code < '0' || code > '8'))
                {
                    output.AddRight(code);
                }

                lastChar = chr;
                lastCode = code;
            }
            return output.ToString();
        }

        // LUCENENET specific - in .NET we don't need an object overload, since strings are sealed anyway.
        //@Override
        //    public Object encode(final Object object) throws EncoderException
        //{
        //        if (!(object instanceof String)) {
        //        throw new EncoderException("This method's parameter was expected to be of the type " +
        //            String.class.getName() +
        //                ". But actually it was of the type " +
        //                object.getClass().getName() +
        //                ".");
        //        }
        //        return encode((String) object);
        //    }


        public virtual string Encode(string text)
        {
            return GetColognePhonetic(text);
        }

        public virtual bool IsEncodeEqual(string text1, string text2)
        {
            return GetColognePhonetic(text1).Equals(GetColognePhonetic(text2), StringComparison.Ordinal);
        }

        private static readonly CultureInfo LOCALE_GERMAN = new CultureInfo("de");

        /// <summary>
        /// Converts the string to upper case and replaces germanic characters as defined in <see cref="PREPROCESS_MAP"/>.
        /// </summary>
        private static string Preprocess(string text) // LUCENENET: CA1822: Mark members as static
        {
            text = LOCALE_GERMAN.TextInfo.ToUpper(text);

            char[] chrs = text.ToCharArray();

            for (int index = 0; index < chrs.Length; index++)
            {
                if (chrs[index] > 'Z')
                {
                    foreach (char[] element in PREPROCESS_MAP)
                    {
                        if (chrs[index] == element[0])
                        {
                            chrs[index] = element[1];
                            break;
                        }
                    }
                }
            }
            return new string(chrs);
        }
    }
}
