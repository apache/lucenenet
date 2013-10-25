using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Support
{
    public static class StringExtensions
    {
        public static bool EqualsIgnoreCase(this string value, string other)
        {
            return string.Equals(value, other, StringComparison.OrdinalIgnoreCase);
        }

        public static bool EqualsIgnoreCase(this object value, string other)
        {
            return string.Equals((value == null ? null : value.ToString()), other, StringComparison.OrdinalIgnoreCase);
        }
    }
}
