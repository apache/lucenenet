using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Parser
{
    /// <summary>
    /// This exception is thrown when parse errors are encountered.
    /// You can explicitly create objects of this exception type by
    /// calling the method generateParseException in the generated
    /// parser.
    /// 
    /// You can modify this class to customize your error reporting
    /// mechanisms so long as you retain the public fields.
    /// </summary>
    public class ParseException : QueryNodeParseException
    {
        /**
   * The version identifier for this Serializable class.
   * Increment only if the <i>serialized</i> form of the
   * class changes.
   */
        private static readonly long serialVersionUID = 1L;

        /**
         * This constructor is used by the method "generateParseException"
         * in the generated parser.  Calling this constructor generates
         * a new object of this type with the fields "currentToken",
         * "expectedTokenSequences", and "tokenImage" set.
         */
        public ParseException(Token currentTokenVal,
            int[][] expectedTokenSequencesVal, string[] tokenImageVal)
            : base(new MessageImpl(QueryParserMessages.INVALID_SYNTAX, Initialize(
                currentTokenVal, expectedTokenSequencesVal, tokenImageVal)))
        {
            this.currentToken = currentTokenVal;
            this.expectedTokenSequences = expectedTokenSequencesVal;
            this.tokenImage = tokenImageVal;
        }

        /**
         * The following constructors are for use by you for whatever
         * purpose you can think of.  Constructing the exception in this
         * manner makes the exception behave in the normal way - i.e., as
         * documented in the class "Throwable".  The fields "errorToken",
         * "expectedTokenSequences", and "tokenImage" do not contain
         * relevant information.  The JavaCC generated code does not use
         * these constructors.
         */

        public ParseException()
            : base(new MessageImpl(QueryParserMessages.INVALID_SYNTAX, "Error"))
        {
        }

        /** Constructor with message. */
        public ParseException(IMessage message)
            : base(message)
        {
        }


        /**
         * This is the last token that has been consumed successfully.  If
         * this object has been created due to a parse error, the token
         * followng this token will (therefore) be the first error token.
         */
        public Token currentToken;

        /**
         * Each entry in this array is an array of integers.  Each array
         * of integers represents a sequence of tokens (by their ordinal
         * values) that is expected at this point of the parse.
         */
        public int[][] expectedTokenSequences;

        /**
         * This is a reference to the "tokenImage" array of the generated
         * parser within which the parse error occurred.  This array is
         * defined in the generated ...Constants interface.
         */
        public string[] tokenImage;

        /**
         * It uses "currentToken" and "expectedTokenSequences" to generate a parse
         * error message and returns it.  If this object has been created
         * due to a parse error, and you do not catch it (it gets thrown
         * from the parser) the correct error message
         * gets displayed.
         */
        private static string Initialize(Token currentToken,
                                 int[][] expectedTokenSequences,
                                 string[] tokenImage)
        {
            //String eol = System.getProperty("line.separator", "\n");
            string eol = Environment.NewLine;
            StringBuilder expected = new StringBuilder();
            int maxSize = 0;
            for (int i = 0; i < expectedTokenSequences.Length; i++)
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
            Token tok = currentToken.next;
            for (int i = 0; i < maxSize; i++)
            {
                if (i != 0) retval += " ";
                if (tok.kind == 0)
                {
                    retval += tokenImage[0];
                    break;
                }
                retval += " " + tokenImage[tok.kind];
                retval += " \"";
                retval += Add_Escapes(tok.image);
                retval += " \"";
                tok = tok.next;
            }
            retval += "\" at line " + currentToken.next.beginLine + ", column " + currentToken.next.beginColumn;
            retval += "." + eol;
            if (expectedTokenSequences.Length == 1)
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

        /**
         * The end of line string for this machine.
         */
        //protected String eol = System.getProperty("line.separator", "\n");
        protected string eol = Environment.NewLine;

        /**
         * Used to convert raw characters to their escaped version
         * when these raw version cannot be used as part of an ASCII
         * string literal.
         */
        static string Add_Escapes(string str)
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
                            //string s = "0000" + Integer.toString(ch, 16);
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
