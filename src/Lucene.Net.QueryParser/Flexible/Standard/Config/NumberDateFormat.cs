using Lucene.Net.Documents;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Config
{
    /// <summary>
    /// LUCENENET specific enum for mimicking the Java DateFormat
    /// </summary>
    public enum DateStyle
    {
        FULL, LONG,
        MEDIUM, SHORT
    }

    /// <summary>
    /// This {@link Format} parses {@link Long} into date strings and vice-versa. It
    /// uses the given {@link DateFormat} to parse and format dates, but before, it
    /// converts {@link Long} to {@link Date} objects or vice-versa.
    /// </summary>
    public class NumberDateFormat : NumberFormat
    {
        //private static readonly long serialVersionUID = 964823936071308283L;

        // The .NET ticks representing January 1, 1970 0:00:00, also known as the "epoch".
        private const long EPOCH = 621355968000000000;

        //private readonly DateFormat dateFormat;
        private readonly DateStyle dateStyle;
        private readonly DateStyle timeStyle;
        //private readonly CultureInfo locale;
        private readonly TimeZoneInfo timeZone;

        /**
         * Constructs a {@link NumberDateFormat} object using the given {@link DateFormat}.
         * 
         * @param dateFormat {@link DateFormat} used to parse and format dates
         */
        //public NumberDateFormat(DateFormat dateFormat)
        //{
        //    this.dateFormat = dateFormat;
        //}

        //public NumberDateFormat(string dateFormat, CultureInfo locale)
        //{
        //    this.dateFormat = dateFormat;
        //    this.locale = locale;
        //}

        //public NumberDateFormat(string dateFormat)
        //    : this(dateFormat, CultureInfo.CurrentCulture)
        //{
        //}

        public NumberDateFormat(DateStyle dateStyle, DateStyle timeStyle, CultureInfo locale, TimeZoneInfo timeZone)
            : base(locale)
        {
            this.dateStyle = dateStyle;
            this.timeStyle = timeStyle;
            //this.locale = locale;
            this.timeZone = timeZone;
        }

        public override string Format(double number)
        {
            //long ticks = (long)number + EPOCH;
            //return new DateTime(ticks).ToString(GetDateFormat(), this.locale);

            return new DateTime(EPOCH).AddMilliseconds(number).ToString(GetDateFormat(), this.locale);
        }


        public override string Format(long number)
        {
            //long ticks = number + EPOCH;
            //return new DateTime(ticks).ToString(GetDateFormat(), this.locale);

            return new DateTime(EPOCH).AddMilliseconds(number).ToString(GetDateFormat(), this.locale);
        }


        public override /*Number*/ object Parse(string source)
        {
            return (DateTime.Parse(source, this.locale) - new DateTime(EPOCH)).TotalMilliseconds;

            //DateTime date = DateTime.Parse(source, this.locale);
            //return date.Ticks - EPOCH;

            //DateTime date = dateFormat.parse(source, parsePosition);
            //return (date == null) ? null : date.getTime();
        }


        public override string Format(object number)
        {
            //long ticks = Convert.ToInt64(number) + EPOCH;
            //return new DateTime(ticks).ToString(GetDateFormat(), this.locale);

            return new DateTime(EPOCH).AddMilliseconds(Convert.ToInt64(number)).ToString(GetDateFormat(), this.locale);
        }


        private string GetDateFormat()
        {
            string datePattern = "", timePattern = "";

            switch (dateStyle)
            {
                case DateStyle.SHORT:
                    datePattern = locale.DateTimeFormat.ShortDatePattern;
                    break;
                case DateStyle.MEDIUM:
                    datePattern = locale.DateTimeFormat.LongDatePattern
                        .Replace("dddd,", "").Replace(", dddd", "") // Remove the day of the week
                        .Replace("MMMM", "MMM"); // Replace month with abbreviated month
                    break;
                case DateStyle.LONG:
                    datePattern = locale.DateTimeFormat.LongDatePattern
                        .Replace("dddd,", "").Replace(", dddd", ""); // Remove the day of the week
                    break;
                case DateStyle.FULL:
                    datePattern = locale.DateTimeFormat.LongDatePattern;
                    break;
            }

            switch (timeStyle)
            {
                case DateStyle.SHORT:
                    timePattern = locale.DateTimeFormat.ShortTimePattern;
                    break;
                case DateStyle.MEDIUM:
                    timePattern = locale.DateTimeFormat.LongTimePattern;
                    break;
                case DateStyle.LONG:
                    timePattern = locale.DateTimeFormat.LongTimePattern; // LUCENENET TODO: Time zone info not being added
                    break;
                case DateStyle.FULL:
                    timePattern = locale.DateTimeFormat.LongTimePattern; // LUCENENET TODO: Time zone info not being added, but Java doc is unclear on what the difference is between this and LONG
                    break;
            }

            return string.Concat(datePattern, " ", timePattern);
        }
    }
}


//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Lucene.Net.QueryParsers.Flexible.Standard.Config
//{
//    /// <summary>
//    /// This {@link Format} parses {@link Long} into date strings and vice-versa. It
//    /// uses the given {@link DateFormat} to parse and format dates, but before, it
//    /// converts {@link Long} to {@link Date} objects or vice-versa.
//    /// </summary>
//    public class NumberDateFormat : NumberFormat
//    {
//        //private static readonly long serialVersionUID = 964823936071308283L;

//        private readonly DateFormat dateFormat;

//        /**
//         * Constructs a {@link NumberDateFormat} object using the given {@link DateFormat}.
//         * 
//         * @param dateFormat {@link DateFormat} used to parse and format dates
//         */
//        public NumberDateFormat(DateFormat dateFormat)
//        {
//            this.dateFormat = dateFormat;
//        }


//        public override StringBuilder format(double number, StringBuilder toAppendTo,
//            FieldPosition pos)
//        {
//            return dateFormat.format(new Date((long)number), toAppendTo, pos);
//        }


//        public override StringBuilder format(long number, StringBuilder toAppendTo,
//            FieldPosition pos)
//        {
//            return dateFormat.format(new Date(number), toAppendTo, pos);
//        }


//        public override /*Number*/ object Parse(string source, ParsePosition parsePosition)
//        {
//            DateTime date = dateFormat.parse(source, parsePosition);
//            return (date == null) ? null : date.getTime();
//        }


//        public override StringBuffer format(object number, StringBuilder toAppendTo,
//            FieldPosition pos)
//        {
//            return dateFormat.format(number, toAppendTo, pos);
//        }
//    }
//}
