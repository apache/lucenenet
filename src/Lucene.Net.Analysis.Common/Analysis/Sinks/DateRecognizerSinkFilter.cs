using System;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.Sinks
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
    /// <summary>
	/// Attempts to parse the <seealso cref="CharTermAttribute#buffer()"/> as a Date using a <seealso cref="java.text.DateFormat"/>.
	/// If the value is a Date, it will add it to the sink.
	/// <p/> 
	/// 
	/// 
	/// </summary>
	public class DateRecognizerSinkFilter : TeeSinkTokenFilter.SinkFilter
	{
	  public const string DATE_TYPE = "date";

	  protected internal DateFormat dateFormat;
	  protected internal CharTermAttribute termAtt;

	  /// <summary>
	  /// Uses {@link java.text.DateFormat#getDateInstance(int, Locale)
	  /// DateFormat#getDateInstance(DateFormat.DEFAULT, Locale.ROOT)} as 
	  /// the <seealso cref="java.text.DateFormat"/> object.
	  /// </summary>
	  public DateRecognizerSinkFilter() : this(DateFormat.getDateInstance(DateFormat.DEFAULT, Locale.ROOT))
	  {
	  }

	  public DateRecognizerSinkFilter(DateFormat dateFormat)
	  {
		this.dateFormat = dateFormat;
	  }

	  public override bool Accept(AttributeSource source)
	  {
		if (termAtt == null)
		{
            termAtt = source.AddAttribute <ICharTermAttribute>();
		}
		try
		{
		  DateTime date = dateFormat.Parse(termAtt.ToString()); //We don't care about the date, just that we can parse it as a date
		  if (date != null)
		  {
			return true;
		  }
		}
		catch (ParseException)
		{

		}

		return false;
	  }

	}

}