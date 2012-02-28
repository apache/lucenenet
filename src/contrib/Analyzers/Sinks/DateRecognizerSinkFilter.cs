using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.Sinks
{
    /**
  * Attempts to parse the {@link org.apache.lucene.analysis.Token#termBuffer()} as a Date using a <see cref="System.IFormatProvider"/>.
  * If the value is a Date, it will add it to the sink.
  * <p/> 
  *
  **/
    public class DateRecognizerSinkFilter : TeeSinkTokenFilter.SinkFilter
    {
        public const string DATE_TYPE = "date";

        protected IFormatProvider dateFormat;
        protected TermAttribute termAtt;

        /**
         * Uses <see cref="System.Globalization.CultureInfo.CurrentCulture.DateTimeFormatInfo"/> as the <see cref="IFormatProvider"/> object.
         */
        public DateRecognizerSinkFilter()
            : this(System.Globalization.CultureInfo.CurrentCulture)
        {

        }

        public DateRecognizerSinkFilter(IFormatProvider dateFormat)
        {
            this.dateFormat = dateFormat;
        }

        public override bool Accept(AttributeSource source)
        {
            if (termAtt == null)
            {
                termAtt = source.AddAttribute<TermAttribute>();
            }
            try
            {
                DateTime date = DateTime.Parse(termAtt.Term(), dateFormat);//We don't care about the date, just that we can parse it as a date
                if (date != null)
                {
                    return true;
                }
            }
            catch (FormatException e)
            {

            }

            return false;
        }

    }
}