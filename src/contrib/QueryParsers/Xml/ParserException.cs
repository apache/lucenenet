using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Xml
{
    public class ParserException : Exception
    {
        public ParserException()
            : base()
        {
        }

        public ParserException(string message)
            : base(message)
        {
        }

        public ParserException(string message, Exception cause)
            : base(message, cause)
        {
        }

        public ParserException(Exception cause)
            : base("A parser error has occurred.", cause)
        {
        }
    }
}
