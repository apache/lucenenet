// LUCENENET TODO: Do we need this class?

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
