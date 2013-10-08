using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Config
{
    public class NumberDateFormat : IFormatProvider, ICustomFormatter
    {
        // .NET Port: I'm not really certain about this implementation. Any help is appreciated. -- PI

        private const long serialVersionUID = 964823936071308283L;
  
        private readonly DateTimeFormatInfo dateFormat;

        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public NumberDateFormat(DateTimeFormatInfo dateFormat)
        {
            this.dateFormat = dateFormat;
        }
        
        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(ICustomFormatter))
                return this;
            else
                return null;
        }

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (arg is double || arg is long)
                return DateTimeFromUnixTimestampMillis((long)arg).ToString(dateFormat);

            return Convert.ToDateTime(arg).ToString(dateFormat);
        }

        public static DateTime DateTimeFromUnixTimestampMillis(long millis)
        {
            return UnixEpoch.AddMilliseconds(millis);
        }
    }
}
