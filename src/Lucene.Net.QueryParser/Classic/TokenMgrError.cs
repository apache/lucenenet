using System;
using System.Text;

namespace Lucene.Net.QueryParsers.Classic
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

	/// <summary>Token Manager Error. </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
	public class TokenMgrError : Exception
	{
        /*
		* Ordinals for various reasons why an Error of this type can be thrown.
		*/

        /// <summary> Lexical error occurred.</summary>
        internal static readonly int LEXICAL_ERROR = 0;

        /// <summary> An attempt was made to create a second instance of a static token manager.</summary>
        internal static readonly int STATIC_LEXER_ERROR = 1;

        /// <summary> Tried to change to an invalid lexical state.</summary>
        internal static readonly int INVALID_LEXICAL_STATE = 2;

        /// <summary> Detected (and bailed out of) an infinite loop in the token manager.</summary>
        internal static readonly int LOOP_DETECTED = 3;

        /// <summary> Indicates the reason why the exception is thrown. It will have
        /// one of the above 4 values.
        /// </summary>
        internal int errorCode;

        /// <summary> 
        /// Replaces unprintable characters by their escaped (or unicode escaped)
        /// equivalents in the given string
        /// </summary>
        protected internal static string AddEscapes(string str)
        {
            StringBuilder retval = new StringBuilder();
            char ch;
            for (int i = 0; i < str.Length; i++)
            {
                switch (str[i])
                {

                    case (char)(0):
                        continue;

                    case '\b':
                        retval.Append("\\b");
                        continue;

                    case '\t':
                        retval.Append("\\t");
                        continue;

                    case '\n':
                        retval.Append("\\n");
                        continue;

                    case '\f':
                        retval.Append("\\f");
                        continue;

                    case '\r':
                        retval.Append("\\r");
                        continue;

                    case '\"':
                        retval.Append("\\\"");
                        continue;

                    case '\'':
                        retval.Append("\\\'");
                        continue;

                    case '\\':
                        retval.Append("\\\\");
                        continue;

                    default:
                        if ((ch = str[i]) < 0x20 || ch > 0x7e)
                        {
                            string s = "0000" + Convert.ToString(ch, 16);
                            retval.Append("\\u" + s.Substring(s.Length - 4, (s.Length) - (s.Length - 4)));
                        }
                        else
                        {
                            retval.Append(ch);
                        }
                        continue;

                }
            }
            return retval.ToString();
        }

        /// <summary>
        /// Returns a detailed message for the Error when it is thrown by the
        /// token manager to indicate a lexical error.
        /// </summary>
        /// <remarks>You can customize the lexical error message by modifying this method.</remarks>
        /// <param name="EOFSeen">indicates if EOF caused the lexical error</param>
        /// <param name="lexState">lexical state in which this error occurred</param>
        /// <param name="errorLine">line number when the error occurred</param>
        /// <param name="errorColumn">column number when the error occurred</param>
        /// <param name="errorAfter">prefix that was seen before this error occurred</param>
        /// <param name="curChar">the offending character</param>
        /// <returns>Detailed error message</returns>
        protected internal static string LexicalError(bool EOFSeen, int lexState, int errorLine, int errorColumn, string errorAfter, char curChar)
        {
            return ("Lexical error at line " +
                errorLine + ", column " +
                errorColumn + ".  Encountered: " +
                (EOFSeen ? "<EOF> " : ("\"" + AddEscapes(Convert.ToString(curChar)) + "\"") + " (" + (int)curChar + "), ") +
                "after : \"" + AddEscapes(errorAfter) + "\"");
        }

		/// <summary> 
        /// You can also modify the body of this method to customize your error messages.
		/// For example, cases like LOOP_DETECTED and INVALID_LEXICAL_STATE are not
		/// of end-users concern, so you can return something like :
		/// 
		/// "Internal Error : Please file a bug report .... "
		/// 
		/// from this method for such cases in the release version of your parser.
		/// </summary>
		public override string Message
		{
			get { return base.Message; }
		}
		
		/*
		* Constructors of various flavors follow.
		*/
		
		/// <summary>No arg constructor. </summary>
		public TokenMgrError()
		{
		}
		
		/// <summary>Constructor with message and reason. </summary>
		public TokenMgrError(string message, int reason)
            : base(message)
		{
			errorCode = reason;
		}
		
		/// <summary>Full Constructor. </summary>
		public TokenMgrError(bool EOFSeen, int lexState, int errorLine, int errorColumn, string errorAfter, char curChar, int reason)
            : this(LexicalError(EOFSeen, lexState, errorLine, errorColumn, errorAfter, curChar), reason)
		{
		}
	}
}