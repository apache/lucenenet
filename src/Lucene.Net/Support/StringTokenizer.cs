// This class was sourced from the Apache Harmony project
// https://svn.apache.org/repos/asf/harmony/enhanced/java/trunk/

using System;

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

    /// <summary>
    /// The <see cref="StringTokenizer"/> class allows an application to break a string
    /// into tokens by performing code point comparison. The <see cref="StringTokenizer"/>
    /// methods do not distinguish among identifiers, numbers, and quoted strings,
    /// nor do they recognize and skip comments.
    /// </summary>
    /// <remarks>
    /// The set of delimiters (the codepoints that separate tokens) may be specified
    /// either at creation time or on a per-token basis.
    /// <para/>
    /// An instance of <see cref="StringTokenizer"/> behaves in one of three ways,
    /// depending on whether it was created with the <c>returnDelimiters</c> flag
    /// having the value <c>true</c> or <c>false</c>:
    /// <list type="bullet">
    /// <item><description>If returnDelims is <c>false</c>, delimiter code points serve to separate
    /// tokens. A token is a maximal sequence of consecutive code points that are not
    /// delimiters.</description></item>
    /// <item><description>If returnDelims is <c>true</c>, delimiter code points are themselves
    /// considered to be tokens. In this case a token will be received for each
    /// delimiter code point.</description></item>
    /// </list>
    /// <para/>
    /// A token is thus either one delimiter code point, or a maximal sequence of
    /// consecutive code points that are not delimiters.
    /// <para/>
    /// A <see cref="StringTokenizer"/> object internally maintains a current position
    /// within the string to be tokenized. Some operations advance this current
    /// position past the code point processed.
    /// <para/>
    /// A token is returned by taking a substring of the string that was used to
    /// create the <see cref="StringTokenizer"/> object.
    /// <para/>
    /// Here's an example of the use of the default delimiter <see cref="StringTokenizer"/>:
    ///
    /// <code>
    /// StringTokenizer st = new StringTokenizer(&quot;this is a test&quot;);
    /// while (st.HasMoreTokens()) {
    ///     println(st.NextToken());
    /// }
    /// </code>
    ///
    /// <para/>
    /// This prints the following output: 
    ///
    /// <code>
    ///     this
    ///     is
    ///     a
    ///     test
    /// </code>
    ///
    /// <para/>
    /// Here's an example of how to use a <see cref="StringTokenizer"/> with a user
    /// specified delimiter: 
    ///
    /// <code>
    /// StringTokenizer st = new StringTokenizer(
    ///         &quot;this is a test with supplementary characters \ud800\ud800\udc00\udc00&quot;,
    ///         &quot; \ud800\udc00&quot;);
    /// while (st.HasMoreTokens()) {
    ///     println(st.NextToken());
    /// }
    /// </code>
    ///
    /// <para/>
    /// This prints the following output:
    ///
    /// <code>
    ///     this
    ///     is
    ///     a
    ///     test
    ///     with
    ///     supplementary
    ///     characters
    ///     \ud800
    ///     \udc00
    /// </code>
    ///
    /// </remarks>
    public class StringTokenizer
    {
        private string str;
        private string delimiters;
        private bool returnDelimiters;
        private int position;

        /// <summary>
        /// Constructs a new <see cref="StringTokenizer"/> for the parameter string using
        /// whitespace as the delimiter. The <see cref="returnDelimiters"/> flag is set to
        /// <c>false</c>.
        /// </summary>
        /// <param name="str">The string to be tokenized.</param>      
        public StringTokenizer(string str)
            : this(str, " \t\n\r\f", false)
        {
        }

        /// <summary>
        /// Constructs a new <see cref="StringTokenizer"/> for the parameter string using
        /// the specified delimiters. The <see cref="returnDelimiters"/> flag is set to
        /// <c>false</c>. If <paramref name="delimiters"/> is <c>null</c>, this constructor
        /// doesn't throw an <see cref="Exception"/>, but later calls to some methods might
        /// throw an <see cref="ArgumentNullException"/> or <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <param name="str">The string to be tokenized.</param>
        /// <param name="delimiters">The delimiters to use.</param>
        public StringTokenizer(string str, string delimiters)
            : this(str, delimiters, false)
        {
        }

        /// <summary>
        /// Constructs a new <see cref="StringTokenizer"/> for the parameter string using
        /// the specified delimiters, returning the delimiters as tokens if the
        /// parameter <paramref name="returnDelimiters"/> is <c>true</c>. If <paramref name="delimiters"/>
        /// is null this constructor doesn't throw an <see cref="Exception"/>, but later
        /// calls to some methods might throw an <see cref="ArgumentNullException"/> or <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <param name="str">The string to be tokenized.</param>
        /// <param name="delimiters">The delimiters to use.</param>
        /// <param name="returnDelimiters"><c>true</c> to return each delimiter as a token.</param>
        public StringTokenizer(string str, string delimiters,
                bool returnDelimiters)
        {
            if (str != null)
            {
                this.str = str;
                this.delimiters = delimiters;
                this.returnDelimiters = returnDelimiters;
                this.position = 0;
            }
            else
                throw new ArgumentNullException("str");
        }

        /// <summary>
        /// Returns the number of unprocessed tokens remaining in the string.
        /// </summary>
        /// <returns>number of tokens that can be retreived before an 
        /// <see cref="Exception"/> will result from a call to <see cref="NextToken()"/>.</returns>
        public virtual int CountTokens()
        {
            int count = 0;
            bool inToken = false;
            for (int i = position, length = str.Length; i < length; i++)
            {
                if (delimiters.IndexOf(str[i], 0) >= 0)
                {
                    if (returnDelimiters)
                        count++;
                    if (inToken)
                    {
                        count++;
                        inToken = false;
                    }
                }
                else
                {
                    inToken = true;
                }
            }
            if (inToken)
                count++;
            return count;
        }

        /// <summary>
        /// Returns <c>true</c> if unprocessed tokens remain.
        /// </summary>
        /// <returns><c>true</c> if unprocessed tokens remain.</returns>
        public bool HasMoreTokens()
        {
            if (delimiters == null)
            {
                throw new ArgumentNullException("delimiters");
            }
            int length = str.Length;
            if (position < length)
            {
                if (returnDelimiters)
                    return true; // there is at least one character and even if
                                 // it is a delimiter it is a token

                // otherwise find a character which is not a delimiter
                for (int i = position; i < length; i++)
                    if (delimiters.IndexOf(str[i], 0) == -1)
                        return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the next token in the string as a <see cref="string"/>.
        /// </summary>
        /// <returns>Next token in the string as a <see cref="string"/>.</returns>
        /// <exception cref="InvalidOperationException">If no tokens remain.</exception>
        public string NextToken()
        {
            if (delimiters == null)
            {
                throw new ArgumentNullException();
            }
            int i = position;
            int length = str.Length;

            if (i < length)
            {
                if (returnDelimiters)
                {
                    if (delimiters.IndexOf(str[position], 0) >= 0)
                        return str[position++].ToString();
                    for (position++; position < length; position++)
                        if (delimiters.IndexOf(str[position], 0) >= 0)
                            return str.Substring(i, position - i);
                    return str.Substring(i);
                }

                while (i < length && delimiters.IndexOf(str[i], 0) >= 0)
                    i++;
                position = i;
                if (i < length)
                {
                    for (position++; position < length; position++)
                        if (delimiters.IndexOf(str[position], 0) >= 0)
                            return str.Substring(i, position - i);
                    return str.Substring(i);
                }
            }
            throw new InvalidOperationException("No more elements");
        }

        /// <summary>
        /// Returns the next token in the string as a <see cref="string"/>. The delimiters
        /// used are changed to the specified delimiters.
        /// </summary>
        /// <param name="delims">The new delimiters to use.</param>
        /// <returns>Next token in the string as a <see cref="string"/>.</returns>
        /// <exception cref="InvalidOperationException">If no tokens remain.</exception>
        public string NextToken(string delims)
        {
            this.delimiters = delims;
            return NextToken();
        }
    }
}