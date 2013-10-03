using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Parser
{
    public interface IEscapeQuerySyntax
    {
        ICharSequence Escape(ICharSequence text, CultureInfo locale, EscapeQuerySyntax.Type type);
    }

    public class EscapeQuerySyntax : IEscapeQuerySyntax
    {
        public enum Type
        {
            STRING,
            NORMAL
        }

        public ICharSequence Escape(ICharSequence text, CultureInfo locale, EscapeQuerySyntax.Type type)
        {
            return text;
        }
    }
}
