using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Surround.Parser
{
    public class ParseException : Exception
    {
        private const long serialVersionUID = 1L;

        public ParseException(Token currentTokenVal,
                        int[][] expectedTokenSequencesVal,
                        String[] tokenImageVal
                       )
            : base(Initialise(currentTokenVal, expectedTokenSequencesVal, tokenImageVal))
        {
            currentToken = currentTokenVal;
            expectedTokenSequences = expectedTokenSequencesVal;
            tokenImage = tokenImageVal;
        }

        public ParseException()
            : base()
        {
        }

        /** Constructor with message. */
        public ParseException(String message)
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
        public String[] tokenImage;

        /**
         * It uses "currentToken" and "expectedTokenSequences" to generate a parse
         * error message and returns it.  If this object has been created
         * due to a parse error, and you do not catch it (it gets thrown
         * from the parser) the correct error message
         * gets displayed.
         */
        private static String Initialise(Token currentToken,
                                 int[][] expectedTokenSequences,
                                 String[] tokenImage)
        {
            String eol = ConfigurationManager.AppSettings["line.separator"] ?? "\n";
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
            String retval = "Encountered \"";
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
                retval += AddEscapes(tok.image);
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
        protected String eol = ConfigurationManager.AppSettings["line.separator"] ?? "\n";

        /**
         * Used to convert raw characters to their escaped version
         * when these raw version cannot be used as part of an ASCII
         * string literal.
         */
        internal static String AddEscapes(String str)
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
                            String s = "0000" + Convert.ToString((int)ch, 16);
                            retval.Append("\\u" + s.Substring(s.Length - 4, s.Length));
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
