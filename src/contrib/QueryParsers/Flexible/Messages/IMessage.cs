using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Messages
{
    public interface IMessage
    {
        string Key { get; }

        object[] Arguments { get; }

        string LocalizedMessage { get; }

        string GetLocalizedMessage(CultureInfo locale);
    }
}
