using Lucene.Net.Documents;
using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
{
    public class TermRangeQueryNodeProcessor : QueryNodeProcessor
    {
        public TermRangeQueryNodeProcessor()
        {
            // empty constructor
        }

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            if (node is TermRangeQueryNode)
            {
                TermRangeQueryNode termRangeNode = (TermRangeQueryNode)node;
                FieldQueryNode upper = termRangeNode.UpperBound;
                FieldQueryNode lower = termRangeNode.LowerBound;

                DateTools.Resolution dateRes = null;
                bool inclusive = false;
                CultureInfo locale = QueryConfigHandler.Get(StandardQueryConfigHandler.ConfigurationKeys.LOCALE);

                if (locale == null)
                {
                    locale = CultureInfo.CurrentCulture;
                }

                TimeZoneInfo timeZone = QueryConfigHandler.Get(StandardQueryConfigHandler.ConfigurationKeys.TIMEZONE);

                if (timeZone == null)
                {
                    timeZone = TimeZoneInfo.Local;
                }

                ICharSequence field = termRangeNode.Field;
                String fieldStr = null;

                if (field != null)
                {
                    fieldStr = field.ToString();
                }

                FieldConfig fieldConfig = QueryConfigHandler.GetFieldConfig(fieldStr);

                if (fieldConfig != null)
                {
                    dateRes = fieldConfig.Get(StandardQueryConfigHandler.ConfigurationKeys.DATE_RESOLUTION);
                }

                if (termRangeNode.IsUpperInclusive)
                {
                    inclusive = true;
                }

                String part1 = lower.TextAsString;
                String part2 = upper.TextAsString;

                try
                {
                    DateTimeFormatInfo df = DateTimeFormatInfo.GetInstance(locale);
                    //df.setLenient(true);

                    if (part1.Length > 0)
                    {
                        DateTime d1 = DateTime.Parse(part1, df);
                        part1 = DateTools.DateToString(d1, dateRes);
                        lower.Text = new StringCharSequenceWrapper(part1);
                    }

                    if (part2.Length > 0)
                    {
                        DateTime d2 = DateTime.Parse(part2, df);
                        if (inclusive)
                        {
                            // The user can only specify the date, not the time, so make sure
                            // the time is set to the latest possible time of that date to
                            // really
                            // include all documents:
                            d2 = d2.Date.Add(new TimeSpan(0, 23, 59, 59, 999));
                        }

                        part2 = DateTools.DateToString(d2, dateRes);
                        upper.Text = new StringCharSequenceWrapper(part2);
                    }

                }
                catch (Exception)
                {
                    // do nothing
                }

            }

            return node;
        }

        protected override IQueryNode PreProcessNode(IQueryNode node)
        {
            return node;
        }

        protected override IList<IQueryNode> SetChildrenOrder(IList<IQueryNode> children)
        {
            return children;
        }
    }
}
