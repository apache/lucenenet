using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Parser
{
    public interface ICharStream
    {
        char ReadChar();

        [Obsolete]
        int Column { get; }

        [Obsolete]
        int Line { get; }

        int EndColumn { get; }

        int EndLine { get; }

        int BeginColumn { get; }

        int BeginLine { get; }

        void Backup(int amount);

        char BeginToken();

        string Image { get; }

        char[] GetSuffix(int len);

        void Done();
    }
}
