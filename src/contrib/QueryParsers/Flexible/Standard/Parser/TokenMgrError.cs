using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Parser
{
    public class TokenMgrError : Exception
    {
        private const long serialVersionUID = 1L;

        internal const int LEXICAL_ERROR = 0;
        internal const int STATIC_LEXER_ERROR = 1;
        internal const int INVALID_LEXICAL_STATE = 2;
        internal const int LOOP_DETECTED = 3;

        int errorCode;

        protected static string AddEscapes(string str)
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
                            String s = "0000" + Convert.ToInt32(ch.ToString(), 16);
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

        protected static string LexicalError(bool EOFSeen, int lexState, int errorLine, int errorColumn, String errorAfter, char curChar)
        {
            return ("Lexical error at line " +
                  errorLine + ", column " +
                  errorColumn + ".  Encountered: " +
                  (EOFSeen ? "<EOF> " : ("\"" + AddEscapes(curChar.ToString()) + "\"") + " (" + (int)curChar + "), ") +
                  "after : \"" + AddEscapes(errorAfter) + "\"");
        }

        public override string Message
        {
            get
            {
                return base.Message;
            }
        }

        public TokenMgrError()
        {
        }

        public TokenMgrError(String message, int reason)
            : base(message)
        {
            errorCode = reason;
        }

        public TokenMgrError(bool EOFSeen, int lexState, int errorLine, int errorColumn, String errorAfter, char curChar, int reason)
            : this(LexicalError(EOFSeen, lexState, errorLine, errorColumn, errorAfter, curChar), reason)
        {            
        }
    }
}
