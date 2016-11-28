using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Parser
{
    /// <summary>
    /// A parser needs to implement {@link EscapeQuerySyntax} to allow the QueryNode
    /// to escape the queries, when the toQueryString method is called.
    /// </summary>
    public interface IEscapeQuerySyntax
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"> text to be escaped</param>
        /// <param name="locale">locale for the current query</param>
        /// <param name="type">select the type of escape operation to use</param>
        /// <returns>escaped text</returns>
        ICharSequence Escape(ICharSequence text, CultureInfo locale, EscapeQuerySyntax.Type type);
    }

    public static class EscapeQuerySyntax
    {
        /// <summary>
        /// Type of escaping: String for escaping syntax,
        /// NORMAL for escaping reserved words (like AND) in terms
        /// </summary>
        public enum Type
        {
            STRING,
            NORMAL
        }
    }
}
