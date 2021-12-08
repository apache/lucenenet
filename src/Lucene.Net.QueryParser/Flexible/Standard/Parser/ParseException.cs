using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
#if FEATURE_SERIALIZABLE_EXCEPTIONS
using System.Runtime.Serialization;
#endif
#if FEATURE_CODE_ACCESS_SECURITY
using System.Security.Permissions;
#endif
using System.Text;
#nullable enable

namespace Lucene.Net.QueryParsers.Flexible.Standard.Parser
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
    /// This exception is thrown when parse errors are encountered.
    /// You can explicitly create objects of this exception type by
    /// calling the method generateParseException in the generated
    /// parser.
    /// 
    /// You can modify this class to customize your error reporting
    /// mechanisms so long as you retain the public fields.
    /// </summary>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    // LUCENENET specific: Refactored constructors to be more like a .NET type and eliminated IMessage/NLS support.
    public class ParseException : QueryNodeParseException
    {
        /// <summary>
        /// This constructor is used by the method "GenerateParseException"
        /// in the generated parser.  Calling this constructor generates
        /// a new object of this type with the fields <see cref="CurrentToken"/>,
        /// <see cref="ExpectedTokenSequences"/>, and <see cref="TokenImage"/> set.
        /// </summary>
        public ParseException(Token currentToken,
            int[][] expectedTokenSequences, string[] tokenImage)
            : base(string.Format(QueryParserMessages.INVALID_SYNTAX!, Initialize(
                currentToken, expectedTokenSequences, tokenImage)))
        {
            this.CurrentToken = currentToken;
            this.ExpectedTokenSequences = expectedTokenSequences;
            this.TokenImage = tokenImage;
        }

        /// <summary>
        /// The following constructors are for use by you for whatever
        /// purpose you can think of.  Constructing the exception in this
        /// manner makes the exception behave in the normal way - i.e., as
        /// documented in the class "Throwable".  The fields "errorToken",
        /// "expectedTokenSequences", and "tokenImage" do not contain
        /// relevant information.  The JavaCC generated code does not use
        /// these constructors.
        /// </summary>
        public ParseException()
            : base(string.Format(QueryParserMessages.INVALID_SYNTAX!, "Error")!)
        {
        }

        /// <summary>
        /// Constructor with message.
        /// </summary>
        public ParseException(string? message)
            : base(message)
        {
        }

        /// <summary>
        /// Constructor with message and inner exception.
        /// </summary>
        // LUCENENET specific - to allow inner exception to be added to the stack trace.
        public ParseException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected ParseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            CurrentToken = (Token)info.GetValue("CurrentToken", typeof(Token))!;
            ExpectedTokenSequences = (int[][])info.GetValue("ExpectedTokenSequences", typeof(int[][]))!;
            TokenImage = (string[])info.GetValue("TokenImage", typeof(string[]))!;
        }

#if FEATURE_CODE_ACCESS_SECURITY
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("CurrentToken", CurrentToken, typeof(Token));
            info.AddValue("ExpectedTokenSequences", ExpectedTokenSequences, typeof(int[][]));
            info.AddValue("TokenImage", TokenImage, typeof(string[]));
        }
#endif

        /// <summary>
        /// This is the last token that has been consumed successfully.  If
        /// this object has been created due to a parse error, the token
        /// followng this token will (therefore) be the first error token.
        /// </summary>
        public Token? CurrentToken { get; private set; }

        /// <summary>
        /// Each entry in this array is an array of integers.  Each array
        /// of integers represents a sequence of tokens (by their ordinal
        /// values) that is expected at this point of the parse.
        /// </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public int[][]? ExpectedTokenSequences { get; private set; }

        /// <summary>
        /// This is a reference to the "tokenImage" array of the generated
        /// parser within which the parse error occurred.  This array is
        /// defined in the generated ...Constants interface.
        /// </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public string[]? TokenImage { get; private set; }

        /// <summary>
        /// It uses <paramref name="currentToken"/> and <paramref name="expectedTokenSequences"/> to generate a parse
        /// error message and returns it.  If this object has been created
        /// due to a parse error, and you do not catch it (it gets thrown
        /// from the parser) the correct error message
        /// gets displayed.
        /// </summary>
        /// <param name="currentToken"></param>
        /// <param name="expectedTokenSequences"></param>
        /// <param name="tokenImage"></param>
        /// <returns></returns>
        private static string Initialize(Token currentToken,
                                 IList<int[]> expectedTokenSequences,
                                 string[] tokenImage)
        {
            string eol = Environment.NewLine;
            StringBuilder expected = new StringBuilder();
            int maxSize = 0;
            for (int i = 0; i < expectedTokenSequences.Count; i++)
            {
                if (maxSize < expectedTokenSequences[i].Length)
                {
                    maxSize = expectedTokenSequences[i].Length;
                }
                for (int j = 0; j < expectedTokenSequences[i].Length; j++)
                {
                    expected.Append(tokenImage[expectedTokenSequences[i][j]]).Append(' ');
                }
                if (expectedTokenSequences[i][expectedTokenSequences[i].Length - 1] != 0)
                {
                    expected.Append("...");
                }
                expected.Append(eol).Append("    ");
            }
            string retval = "Encountered \"";
            Token tok = currentToken.Next;
            for (int i = 0; i < maxSize; i++)
            {
                if (i != 0) retval += " ";
                if (tok.Kind == 0)
                {
                    retval += tokenImage[0];
                    break;
                }
                retval += " " + tokenImage[tok.Kind];
                retval += " \"";
                retval += AddEscapes(tok.Image);
                retval += " \"";
                tok = tok.Next;
            }
            retval += "\" at line " + currentToken.Next.BeginLine + ", column " + currentToken.Next.BeginColumn;
            retval += "." + eol;
            if (expectedTokenSequences.Count == 1)
            {
                retval += "Was expecting:" + eol + "    ";
            }
            else
            {
                retval += "Was expecting one of:" + eol + "    ";
            }
            retval += expected.ToString();
            return retval;
        }

        // LUCENENET: Removed eol field, since this is available on Environment.NewLine;

        /// <summary>
        /// Used to convert raw characters to their escaped version
        /// when these raw version cannot be used as part of an ASCII
        /// string literal.
        /// </summary>
        private static string AddEscapes(string str)
        {
            StringBuilder retval = new StringBuilder();
            char ch;
            for (int i = 0; i < str.Length; i++)
            {
                switch (str[i])
                {
                    case (char)0:
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
                            retval.Append("\\u" + s.Substring(s.Length - 4, s.Length - (s.Length - 4)));
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
    }
}
