/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using Org.Apache.Lucene.Document;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Config;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Processors;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Config;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors
{
	/// <summary>
	/// This processors process
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.TermRangeQueryNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.TermRangeQueryNode</see>
	/// s. It reads the lower and
	/// upper bounds value from the
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.TermRangeQueryNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.TermRangeQueryNode</see>
	/// object and try
	/// to parse their values using a
	/// <see cref="Sharpen.DateFormat">Sharpen.DateFormat</see>
	/// . If the values cannot be
	/// parsed to a date value, it will only create the
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.TermRangeQueryNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.TermRangeQueryNode</see>
	/// using the non-parsed values. <br/>
	/// <br/>
	/// If a
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.LOCALE
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.LOCALE
	/// 	</see>
	/// is defined in the
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler"
	/// 	>Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler</see>
	/// it will be used to parse the date, otherwise
	/// <see cref="System.Globalization.CultureInfo.CurrentCulture()">System.Globalization.CultureInfo.CurrentCulture()
	/// 	</see>
	/// will be used. <br/>
	/// <br/>
	/// If a
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.DATE_RESOLUTION
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.DATE_RESOLUTION
	/// 	</see>
	/// is defined and the
	/// <see cref="Org.Apache.Lucene.Document.DateTools.Resolution">Org.Apache.Lucene.Document.DateTools.Resolution
	/// 	</see>
	/// is not <code>null</code> it will also be used to parse the
	/// date value. <br/>
	/// <br/>
	/// </summary>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.DATE_RESOLUTION
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.DATE_RESOLUTION
	/// 	</seealso>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.LOCALE
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.LOCALE
	/// 	</seealso>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.TermRangeQueryNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.TermRangeQueryNode</seealso>
	public class TermRangeQueryNodeProcessor : QueryNodeProcessorImpl
	{
		public TermRangeQueryNodeProcessor()
		{
		}

		// empty constructor
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PostProcessNode(QueryNode node)
		{
			if (node is TermRangeQueryNode)
			{
				TermRangeQueryNode termRangeNode = (TermRangeQueryNode)node;
				FieldQueryNode upper = termRangeNode.GetUpperBound();
				FieldQueryNode lower = termRangeNode.GetLowerBound();
				DateTools.Resolution dateRes = null;
				bool inclusive = false;
				CultureInfo locale = GetQueryConfigHandler().Get(StandardQueryConfigHandler.ConfigurationKeys
					.LOCALE);
				if (locale == null)
				{
					locale = CultureInfo.CurrentCulture;
				}
				TimeZoneInfo timeZone = GetQueryConfigHandler().Get(StandardQueryConfigHandler.ConfigurationKeys
					.TIMEZONE);
				if (timeZone == null)
				{
					timeZone = System.TimeZoneInfo.Local;
				}
				CharSequence field = termRangeNode.GetField();
				string fieldStr = null;
				if (field != null)
				{
					fieldStr = field.ToString();
				}
				FieldConfig fieldConfig = GetQueryConfigHandler().GetFieldConfig(fieldStr);
				if (fieldConfig != null)
				{
					dateRes = fieldConfig.Get(StandardQueryConfigHandler.ConfigurationKeys.DATE_RESOLUTION
						);
				}
				if (termRangeNode.IsUpperInclusive())
				{
					inclusive = true;
				}
				string part1 = lower.GetTextAsString();
				string part2 = upper.GetTextAsString();
				try
				{
					DateFormat df = DateFormat.GetDateInstance(DateFormat.SHORT, locale);
					df.SetLenient(true);
					if (part1.Length > 0)
					{
						DateTime d1 = df.Parse(part1);
						part1 = DateTools.DateToString(d1, dateRes);
						lower.SetText(part1);
					}
					if (part2.Length > 0)
					{
						DateTime d2 = df.Parse(part2);
						if (inclusive)
						{
							// The user can only specify the date, not the time, so make sure
							// the time is set to the latest possible time of that date to
							// really
							// include all documents:
							Calendar cal = Calendar.GetInstance(timeZone, locale);
							cal.SetTime(d2);
							cal.Set(Calendar.HOUR_OF_DAY, 23);
							cal.Set(Calendar.MINUTE, 59);
							cal.Set(Calendar.SECOND, 59);
							cal.Set(Calendar.MILLISECOND, 999);
							d2 = cal.GetTime();
						}
						part2 = DateTools.DateToString(d2, dateRes);
						upper.SetText(part2);
					}
				}
				catch (Exception)
				{
				}
			}
			// do nothing
			return node;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PreProcessNode(QueryNode node)
		{
			return node;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override IList<QueryNode> SetChildrenOrder(IList<QueryNode> children
			)
		{
			return children;
		}
	}
}
