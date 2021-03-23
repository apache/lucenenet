// Lucene version compatibility level 4.8.1
using J2N;
using Lucene.Net.Analysis.TokenAttributes;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Lucene.Net.Analysis.Cjk
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
    /// CJKTokenizer is designed for Chinese, Japanese, and Korean languages.
    /// <para>  
    /// The tokens returned are every two adjacent characters with overlap match.
    /// </para>
    /// <para>
    /// Example: "java C1C2C3C4" will be segmented to: "java" "C1C2" "C2C3" "C3C4".
    /// </para>
    /// Additionally, the following is applied to Latin text (such as English):
    /// <list type="bullet">
    ///     <item><description>Text is converted to lowercase.</description></item>
    ///     <item><description>Numeric digits, '+', '#', and '_' are tokenized as letters.</description></item>
    ///     <item><description>Full-width forms are converted to half-width forms.</description></item>
    /// </list>
    /// For more info on Asian language (Chinese, Japanese, and Korean) text segmentation:
    /// please search  <a
    /// href="http://www.google.com/search?q=word+chinese+segment">google</a>
    /// </summary>
    /// @deprecated Use StandardTokenizer, CJKWidthFilter, CJKBigramFilter, and LowerCaseFilter instead. 
    [Obsolete("Use StandardTokenizer, CJKWidthFilter, CJKBigramFilter, and LowerCaseFilter instead.")]
    public sealed class CJKTokenizer : Tokenizer
    {
        //~ Static fields/initializers ---------------------------------------------
        /// <summary>
        /// Word token type </summary>
        internal const int WORD_TYPE = 0;

        /// <summary>
        /// Single byte token type </summary>
        internal const int SINGLE_TOKEN_TYPE = 1;

        /// <summary>
        /// Double byte token type </summary>
        internal const int DOUBLE_TOKEN_TYPE = 2;

        /// <summary>
        /// Names for token types </summary>
        internal static readonly string[] TOKEN_TYPE_NAMES = new string[] { "word", "single", "double" };

        /// <summary>
        /// Max word length </summary>
        private const int MAX_WORD_LEN = 255;

        /// <summary>
        /// buffer size: </summary>
        private const int IO_BUFFER_SIZE = 256;

        /// <summary>
        /// Regular expression for testing Unicode character class <c>\p{IsHalfwidthandFullwidthForms}</c>.</summary>
        // LUCENENET specific
        private static readonly Regex HALFWIDTH_AND_FULLWIDTH_FORMS = new Regex(@"\p{IsHalfwidthandFullwidthForms}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Regular expression for testing Unicode character class <c>\p{IsBasicLatin}</c>.</summary>
        // LUCENENET specific
        private static readonly Regex BASIC_LATIN = new Regex(@"\p{IsBasicLatin}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        //~ Instance fields --------------------------------------------------------

        /// <summary>
        /// word offset, used to imply which character(in ) is parsed </summary>
        private int offset = 0;

        /// <summary>
        /// the index used only for ioBuffer </summary>
        private int bufferIndex = 0;

        /// <summary>
        /// data length </summary>
        private int dataLen = 0;

        /// <summary>
        /// character buffer, store the characters which are used to compose 
        /// the returned Token
        /// </summary>
        private readonly char[] buffer = new char[MAX_WORD_LEN];

        /// <summary>
        /// I/O buffer, used to store the content of the input(one of the
        /// members of Tokenizer)
        /// </summary>
        private readonly char[] ioBuffer = new char[IO_BUFFER_SIZE];

        /// <summary>
        /// word type: single=>ASCII  double=>non-ASCII word=>default </summary>
        private int tokenType = WORD_TYPE;

        /// <summary>
        /// tag: previous character is a cached double-byte character  "C1C2C3C4"
        /// ----(set the C1 isTokened) C1C2 "C2C3C4" ----(set the C2 isTokened)
        /// C1C2 C2C3 "C3C4" ----(set the C3 isTokened) "C1C2 C2C3 C3C4"
        /// </summary>
        private bool preIsTokened = false;

        private ICharTermAttribute termAtt;
        private IOffsetAttribute offsetAtt;
        private ITypeAttribute typeAtt;

        //~ Constructors -----------------------------------------------------------

        /// <summary>
        /// Construct a token stream processing the given input.
        /// </summary>
        /// <param name="in"> I/O reader </param>
        public CJKTokenizer(TextReader @in)
            : base(@in)
        {
            Init();
        }

        public CJKTokenizer(AttributeFactory factory, TextReader @in)
            : base(factory, @in)
        {
            Init();
        }

        private void Init()
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
            typeAtt = AddAttribute<ITypeAttribute>();
        }

        //~ Methods ----------------------------------------------------------------

        /// <summary>
        /// Returns true for the next token in the stream, or false at EOS.
        /// See http://java.sun.com/j2se/1.3/docs/api/java/lang/Character.UnicodeBlock.html
        /// for detail.
        /// </summary>
        /// <returns> false for end of stream, true otherwise
        /// </returns>
        /// <exception cref="IOException"> when read error
        ///         happened in the InputStream
        ///  </exception>
        public override bool IncrementToken()
        {
            ClearAttributes();

            // how many character(s) has been stored in buffer 

            while (true) // loop until we find a non-empty token
            {

                int length = 0;

                // the position used to create Token 
                int start = offset;

                while (true) // loop until we've found a full token
                {
                    // current character
                    char c;

                    offset++;

                    if (bufferIndex >= dataLen)
                    {
                        dataLen = m_input.Read(ioBuffer, 0, ioBuffer.Length);
                        bufferIndex = 0;
                    }

                    if (dataLen <= 0)
                    {
                        if (length > 0)
                        {
                            if (preIsTokened == true)
                            {
                                length = 0;
                                preIsTokened = false;
                            }
                            else
                            {
                                offset--;
                            }

                            break;
                        }
                        else
                        {
                            offset--;
                            return false;
                        }
                    }
                    else
                    {
                        //get current character
                        c = ioBuffer[bufferIndex++];
                    }

                    //if the current character is ASCII or Extend ASCII
                    // LUCENENET Port Reference: https://msdn.microsoft.com/en-us/library/20bw873z.aspx#SupportedNamedBlocks
                    string charAsString = c + "";
                    bool isHalfwidthAndFullwidthForms = false;
                    // LUCENENET: This condition only works because the ranges of BASIC_LATIN and HALFWIDTH_AND_FULLWIDTH_FORMS do not overlap
                    if (BASIC_LATIN.IsMatch(charAsString) || (isHalfwidthAndFullwidthForms = HALFWIDTH_AND_FULLWIDTH_FORMS.IsMatch(charAsString)))
                    {
                        if (isHalfwidthAndFullwidthForms)
                        {
                            int i = (int)c;
                            if (i >= 65281 && i <= 65374)
                            {
                                // convert certain HALFWIDTH_AND_FULLWIDTH_FORMS to BASIC_LATIN
                                i = i - 65248;
                                c = (char)i;
                            }
                        }

                        // if the current character is a letter or "_" "+" "#"
                        if (char.IsLetterOrDigit(c) || ((c == '_') || (c == '+') || (c == '#')))
                        {
                            if (length == 0)
                            {
                                // "javaC1C2C3C4linux" <br>
                                //      ^--: the current character begin to token the ASCII
                                // letter
                                start = offset - 1;
                            }
                            else if (tokenType == DOUBLE_TOKEN_TYPE)
                            {
                                // "javaC1C2C3C4linux" <br>
                                //              ^--: the previous non-ASCII
                                // : the current character
                                offset--;
                                bufferIndex--;

                                if (preIsTokened == true)
                                {
                                    // there is only one non-ASCII has been stored
                                    length = 0;
                                    preIsTokened = false;
                                    break;
                                }
                                else
                                {
                                    break;
                                }
                            }

                            // store the LowerCase(c) in the buffer
                            buffer[length++] = char.ToLowerInvariant(c);
                            tokenType = SINGLE_TOKEN_TYPE;

                            // break the procedure if buffer overflowed!
                            if (length == MAX_WORD_LEN)
                            {
                                break;
                            }
                        }
                        else if (length > 0)
                        {
                            if (preIsTokened == true)
                            {
                                length = 0;
                                preIsTokened = false;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        // non-ASCII letter, e.g."C1C2C3C4"
                        if (Character.IsLetter(c))
                        {
                            if (length == 0)
                            {
                                start = offset - 1;
                                buffer[length++] = c;
                                tokenType = DOUBLE_TOKEN_TYPE;
                            }
                            else
                            {
                                if (tokenType == SINGLE_TOKEN_TYPE)
                                {
                                    offset--;
                                    bufferIndex--;

                                    //return the previous ASCII characters
                                    break;
                                }
                                else
                                {
                                    buffer[length++] = c;
                                    tokenType = DOUBLE_TOKEN_TYPE;

                                    if (length == 2)
                                    {
                                        offset--;
                                        bufferIndex--;
                                        preIsTokened = true;

                                        break;
                                    }
                                }
                            }
                        }
                        else if (length > 0)
                        {
                            if (preIsTokened == true)
                            {
                                // empty the buffer
                                length = 0;
                                preIsTokened = false;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }

                if (length > 0)
                {
                    termAtt.CopyBuffer(buffer, 0, length);
                    offsetAtt.SetOffset(CorrectOffset(start), CorrectOffset(start + length));
                    typeAtt.Type = TOKEN_TYPE_NAMES[tokenType];
                    return true;
                }
                else if (dataLen <= 0)
                {
                    offset--;
                    return false;
                }

                // Cycle back and try for the next token (don't
                // return an empty string)
            }
        }

        public override sealed void End()
        {
            base.End();
            // set final offset
            int finalOffset = CorrectOffset(offset);
            this.offsetAtt.SetOffset(finalOffset, finalOffset);
        }

        public override void Reset()
        {
            base.Reset();
            offset = bufferIndex = dataLen = 0;
            preIsTokened = false;
            tokenType = WORD_TYPE;
        }
    }
}