using System;

namespace Lucene.Net.Document
{

	/*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements.  See the NOTICE file distributed with
	 * this work for additional information regarding copyright ownership.
	 * The ASF licenses this file to You under the Apache License, Version 2.0
	 * (the "License"); you may not use this file except in compliance with
	 * the License.  You may obtain a copy of the License at
	 *
	 *     http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */

	using Lucene.Net.Search; // for javadocs
	using PrefixQuery = Lucene.Net.Search.PrefixQuery;
	using TermRangeQuery = Lucene.Net.Search.TermRangeQuery;
	using NumericUtils = Lucene.Net.Util.NumericUtils; // for javadocs


	/// <summary>
	/// Provides support for converting dates to strings and vice-versa.
	/// The strings are structured so that lexicographic sorting orders 
	/// them by date, which makes them suitable for use as field values 
	/// and search terms.
	/// 
	/// <P>this class also helps you to limit the resolution of your dates. Do not
	/// save dates with a finer resolution than you really need, as then
	/// <seealso cref="TermRangeQuery"/> and <seealso cref="PrefixQuery"/> will require more memory and become slower.
	/// 
	/// <P>
	/// Another approach is <seealso cref="NumericUtils"/>, which provides
	/// a sortable binary representation (prefix encoded) of numeric values, which
	/// date/time are.
	/// For indexing a <seealso cref="Date"/> or <seealso cref="Calendar"/>, just get the unix timestamp as
	/// <code>long</code> using <seealso cref="Date#getTime"/> or <seealso cref="Calendar#getTimeInMillis"/> and
	/// index this as a numeric value with <seealso cref="LongField"/>
	/// and use <seealso cref="NumericRangeQuery"/> to query it.
	/// </summary>
	public class DateTools
	{

	  internal static readonly TimeZone GMT = TimeZone.getTimeZone("GMT");

	  private static readonly ThreadLocal<DateTime> TL_CAL = new ThreadLocalAnonymousInnerClassHelper();

	  private class ThreadLocalAnonymousInnerClassHelper : ThreadLocal<DateTime>
	  {
		  public ThreadLocalAnonymousInnerClassHelper()
		  {
		  }

		  protected internal override DateTime InitialValue()
		  {
			return DateTime.getInstance(GMT, Locale.ROOT);
		  }
	  }

	  //indexed by format length
	  private static readonly ThreadLocal<SimpleDateFormat[]> TL_FORMATS = new ThreadLocalAnonymousInnerClassHelper2();

	  private class ThreadLocalAnonymousInnerClassHelper2 : ThreadLocal<SimpleDateFormat[]>
	  {
		  public ThreadLocalAnonymousInnerClassHelper2()
		  {
		  }

		  protected internal override SimpleDateFormat[] InitialValue()
		  {
			SimpleDateFormat[] arr = new SimpleDateFormat[Resolution.MILLISECOND.formatLen + 1];
			foreach (Resolution resolution in Enum.GetValues(typeof(Resolution)))
			{
			  arr[resolution.formatLen] = (SimpleDateFormat)resolution.format.clone();
			}
			return arr;
		  }
	  }

	  // cannot create, the class has static methods only
	  private DateTools()
	  {
	  }

	  /// <summary>
	  /// Converts a Date to a string suitable for indexing.
	  /// </summary>
	  /// <param name="date"> the date to be converted </param>
	  /// <param name="resolution"> the desired resolution, see
	  ///  <seealso cref="#round(Date, DateTools.Resolution)"/> </param>
	  /// <returns> a string in format <code>yyyyMMddHHmmssSSS</code> or shorter,
	  ///  depending on <code>resolution</code>; using GMT as timezone  </returns>
	  public static string DateToString(DateTime date, Resolution resolution)
	  {
		return TimeToString(date, resolution);
	  }

	  /// <summary>
	  /// Converts a millisecond time to a string suitable for indexing.
	  /// </summary>
	  /// <param name="time"> the date expressed as milliseconds since January 1, 1970, 00:00:00 GMT </param>
	  /// <param name="resolution"> the desired resolution, see
	  ///  <seealso cref="#round(long, DateTools.Resolution)"/> </param>
	  /// <returns> a string in format <code>yyyyMMddHHmmssSSS</code> or shorter,
	  ///  depending on <code>resolution</code>; using GMT as timezone </returns>
	  public static string TimeToString(long time, Resolution resolution)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Date date = new java.util.Date(round(time, resolution));
		DateTime date = new DateTime(Round(time, resolution));
		return TL_FORMATS.get()[resolution.formatLen].format(date);
	  }

	  /// <summary>
	  /// Converts a string produced by <code>timeToString</code> or
	  /// <code>dateToString</code> back to a time, represented as the
	  /// number of milliseconds since January 1, 1970, 00:00:00 GMT.
	  /// </summary>
	  /// <param name="dateString"> the date string to be converted </param>
	  /// <returns> the number of milliseconds since January 1, 1970, 00:00:00 GMT </returns>
	  /// <exception cref="ParseException"> if <code>dateString</code> is not in the 
	  ///  expected format  </exception>
	  public static long StringToTime(string dateString)
	  {
		return StringToDate(dateString);
	  }

	  /// <summary>
	  /// Converts a string produced by <code>timeToString</code> or
	  /// <code>dateToString</code> back to a time, represented as a
	  /// Date object.
	  /// </summary>
	  /// <param name="dateString"> the date string to be converted </param>
	  /// <returns> the parsed time as a Date object </returns>
	  /// <exception cref="ParseException"> if <code>dateString</code> is not in the 
	  ///  expected format  </exception>
	  public static DateTime StringToDate(string dateString)
	  {
		try
		{
		  return TL_FORMATS.get()[dateString.Length].parse(dateString);
		}
		catch (Exception e)
		{
		  throw new ParseException("Input is not a valid date string: " + dateString, 0);
		}
	  }

	  /// <summary>
	  /// Limit a date's resolution. For example, the date <code>2004-09-21 13:50:11</code>
	  /// will be changed to <code>2004-09-01 00:00:00</code> when using
	  /// <code>Resolution.MONTH</code>. 
	  /// </summary>
	  /// <param name="resolution"> The desired resolution of the date to be returned </param>
	  /// <returns> the date with all values more precise than <code>resolution</code>
	  ///  set to 0 or 1 </returns>
	  public static DateTime Round(DateTime date, Resolution resolution)
	  {
		return new DateTime(Round(date, resolution));
	  }

	  /// <summary>
	  /// Limit a date's resolution. For example, the date <code>1095767411000</code>
	  /// (which represents 2004-09-21 13:50:11) will be changed to 
	  /// <code>1093989600000</code> (2004-09-01 00:00:00) when using
	  /// <code>Resolution.MONTH</code>.
	  /// </summary>
	  /// <param name="resolution"> The desired resolution of the date to be returned </param>
	  /// <returns> the date with all values more precise than <code>resolution</code>
	  ///  set to 0 or 1, expressed as milliseconds since January 1, 1970, 00:00:00 GMT </returns>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings("fallthrough") public static long round(long time, Resolution resolution)
	  public static long Round(long time, Resolution resolution)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Calendar calInstance = TL_CAL.get();
		DateTime calInstance = TL_CAL.get();
		calInstance.TimeInMillis = time;

		switch (resolution)
		{
		  //NOTE: switch statement fall-through is deliberate
		  case Lucene.Net.Document.DateTools.Resolution.YEAR:
			calInstance.set(DateTime.MONTH, 0);
			  goto case MONTH;
		  case Lucene.Net.Document.DateTools.Resolution.MONTH:
			calInstance.set(DateTime.DAY_OF_MONTH, 1);
			  goto case DAY;
		  case Lucene.Net.Document.DateTools.Resolution.DAY:
			calInstance.set(DateTime.HOUR_OF_DAY, 0);
			  goto case HOUR;
		  case Lucene.Net.Document.DateTools.Resolution.HOUR:
			calInstance.set(DateTime.MINUTE, 0);
			  goto case MINUTE;
		  case Lucene.Net.Document.DateTools.Resolution.MINUTE:
			calInstance.set(DateTime.SECOND, 0);
			  goto case SECOND;
		  case Lucene.Net.Document.DateTools.Resolution.SECOND:
			calInstance.set(DateTime.MILLISECOND, 0);
			  goto case MILLISECOND;
		  case Lucene.Net.Document.DateTools.Resolution.MILLISECOND:
			// don't cut off anything
			break;
		  default:
			throw new System.ArgumentException("unknown resolution " + resolution);
		}
		return calInstance.TimeInMillis;
	  }

	  /// <summary>
	  /// Specifies the time granularity. </summary>
	  public enum Resolution
	  {

		/// <summary>
		/// Limit a date's resolution to year granularity. </summary>
		YEAR = 4,
		/// <summary>
		/// Limit a date's resolution to month granularity. </summary>
		MONTH = 6,
		/// <summary>
		/// Limit a date's resolution to day granularity. </summary>
		DAY = 8,
		/// <summary>
		/// Limit a date's resolution to hour granularity. </summary>
		HOUR = 10,
		/// <summary>
		/// Limit a date's resolution to minute granularity. </summary>
		MINUTE = 12,
		/// <summary>
		/// Limit a date's resolution to second granularity. </summary>
		SECOND = 14,
		/// <summary>
		/// Limit a date's resolution to millisecond granularity. </summary>
		MILLISECOND = 17

//JAVA TO C# CONVERTER TODO TASK: Enums cannot contain fields in .NET:
//		final int formatLen;
//JAVA TO C# CONVERTER TODO TASK: Enums cannot contain fields in .NET:
//		final java.text.SimpleDateFormat format; //should be cloned before use, since it's not threadsafe

//JAVA TO C# CONVERTER TODO TASK: Enums cannot contain methods in .NET:
//		Resolution(int formatLen)
	//	{
	//	  this.formatLen = formatLen;
	//	  // formatLen 10's place:                     11111111
	//	  // formatLen  1's place:            12345678901234567
	//	  this.format = new SimpleDateFormat("yyyyMMddHHmmssSSS".substring(0,formatLen),Locale.ROOT);
	//	  this.format.setTimeZone(GMT);
	//	}

		/// <summary>
		/// this method returns the name of the resolution
		/// in lowercase (for backwards compatibility) 
		/// </summary>

	  }
	public static partial class EnumExtensionMethods
	{
		public override static string ToString(this Resolution instance)
		{
		  return base.ToString().ToLower(Locale.ROOT);
		}
	}

	}

}