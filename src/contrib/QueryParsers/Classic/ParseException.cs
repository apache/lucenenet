using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Classic
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

        public ParseException(String message)
            : base(message)
        {
        }

        // .NET Port: not present in Java version but needed for inner exception
        public ParseException(String message, Exception innerException)
            : base(message, innerException)
        {
        }


        public Token currentToken;

        public int[][] expectedTokenSequences;

        public String[] tokenImage;

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
                retval += Add_escapes(tok.image);
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

        protected String eol = ConfigurationManager.AppSettings["line.separator"] ?? "\n";

        static String Add_escapes(String str)
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
                            String s = "0000" + Convert.ToString(ch, 16);
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
