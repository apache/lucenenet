using System;

namespace Lucene.Net.Document
{


	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	/*using Rule = org.junit.Rule;
	using RuleChain = org.junit.rules.RuleChain;
	using TestRule = org.junit.rules.TestRule;

	using SystemPropertiesRestoreRule = com.carrotsearch.randomizedtesting.rules.SystemPropertiesRestoreRule;*/
    using NUnit.Framework;

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
	public class TestDateTools : LuceneTestCase
	{
	  public TestRule TestRules = RuleChain.outerRule(new SystemPropertiesRestoreRule());

	  public virtual void TestStringToDate()
	  {

		DateTime d = null;
		d = DateTools.StringToDate("2004");
		Assert.AreEqual("2004-01-01 00:00:00:000", IsoFormat(d));
		d = DateTools.StringToDate("20040705");
		Assert.AreEqual("2004-07-05 00:00:00:000", IsoFormat(d));
		d = DateTools.StringToDate("200407050910");
		Assert.AreEqual("2004-07-05 09:10:00:000", IsoFormat(d));
		d = DateTools.StringToDate("20040705091055990");
		Assert.AreEqual("2004-07-05 09:10:55:990", IsoFormat(d));

		try
		{
		  d = DateTools.StringToDate("97"); // no date
		  Assert.Fail();
		} // expected exception
		catch (ParseException e)
		{
		}
		try
		{
		  d = DateTools.StringToDate("200401011235009999"); // no date
		  Assert.Fail();
		} // expected exception
		catch (ParseException e)
		{
		}
		try
		{
		  d = DateTools.StringToDate("aaaa"); // no date
		  Assert.Fail();
		} // expected exception
		catch (ParseException e)
		{
		}

	  }

	  public virtual void TestStringtoTime()
	  {
		long time = DateTools.StringToTime("197001010000");
		// we use default locale since LuceneTestCase randomizes it
		DateTime cal = new GregorianCalendar(TimeZone.getTimeZone("GMT"), Locale.Default);
		cal.clear();
		cal = new DateTime(1970, 0, 1, 0, 0, 0); // hour, minute, second -  year=1970, month=january, day=1
		cal.set(DateTime.MILLISECOND, 0);
		Assert.AreEqual(cal.Time, time);
		cal = new DateTime(1980, 1, 2, 11, 5, 0); // hour, minute, second -  year=1980, month=february, day=2
		cal.set(DateTime.MILLISECOND, 0);
		time = DateTools.StringToTime("198002021105");
		Assert.AreEqual(cal.Time, time);
	  }

	  public virtual void TestDateAndTimetoString()
	  {
		// we use default locale since LuceneTestCase randomizes it
		DateTime cal = new GregorianCalendar(TimeZone.getTimeZone("GMT"), Locale.Default);
		cal.clear();
		cal = new DateTime(2004, 1, 3, 22, 8, 56); // hour, minute, second -  year=2004, month=february(!), day=3
		cal.set(DateTime.MILLISECOND, 333);

		string dateString;
		dateString = DateTools.DateToString(cal, DateTools.Resolution.YEAR);
		Assert.AreEqual("2004", dateString);
		Assert.AreEqual("2004-01-01 00:00:00:000", IsoFormat(DateTools.StringToDate(dateString)));

        dateString = DateTools.DateToString(cal, DateTools.Resolution.MONTH);
		Assert.AreEqual("200402", dateString);
        Assert.AreEqual("2004-02-01 00:00:00:000", IsoFormat(DateTools.StringToDate(dateString)));

        dateString = DateTools.DateToString(cal, DateTools.Resolution.DAY);
		Assert.AreEqual("20040203", dateString);
        Assert.AreEqual("2004-02-03 00:00:00:000", IsoFormat(DateTools.StringToDate(dateString)));

        dateString = DateTools.DateToString(cal, DateTools.Resolution.HOUR);
		Assert.AreEqual("2004020322", dateString);
        Assert.AreEqual("2004-02-03 22:00:00:000", IsoFormat(DateTools.StringToDate(dateString)));

        dateString = DateTools.DateToString(cal, DateTools.Resolution.MINUTE);
		Assert.AreEqual("200402032208", dateString);
        Assert.AreEqual("2004-02-03 22:08:00:000", IsoFormat(DateTools.StringToDate(dateString)));

        dateString = DateTools.DateToString(cal, DateTools.Resolution.SECOND);
		Assert.AreEqual("20040203220856", dateString);
        Assert.AreEqual("2004-02-03 22:08:56:000", IsoFormat(DateTools.StringToDate(dateString)));

        dateString = DateTools.DateToString(cal, DateTools.Resolution.MILLISECOND);
		Assert.AreEqual("20040203220856333", dateString);
        Assert.AreEqual("2004-02-03 22:08:56:333", IsoFormat(DateTools.StringToDate(dateString)));

		// date before 1970:
		cal = new DateTime(1961, 2, 5, 23, 9, 51); // hour, minute, second -  year=1961, month=march(!), day=5
		cal.set(DateTime.MILLISECOND, 444);
        dateString = DateTools.DateToString(cal, DateTools.Resolution.MILLISECOND);
		Assert.AreEqual("19610305230951444", dateString);
        Assert.AreEqual("1961-03-05 23:09:51:444", IsoFormat(DateTools.StringToDate(dateString)));

        dateString = DateTools.DateToString(cal, DateTools.Resolution.HOUR);
		Assert.AreEqual("1961030523", dateString);
        Assert.AreEqual("1961-03-05 23:00:00:000", IsoFormat(DateTools.StringToDate(dateString)));

		// timeToString:
		cal = new DateTime(1970, 0, 1, 0, 0, 0); // hour, minute, second -  year=1970, month=january, day=1
		cal.set(DateTime.MILLISECOND, 0);
		dateString = DateTools.TimeToString(cal.Time, DateTools.Resolution.MILLISECOND);
		Assert.AreEqual("19700101000000000", dateString);

		cal = new DateTime(1970, 0, 1, 1, 2, 3); // hour, minute, second -  year=1970, month=january, day=1
		cal.set(DateTime.MILLISECOND, 0);
		dateString = DateTools.TimeToString(cal.Time, DateTools.Resolution.MILLISECOND);
		Assert.AreEqual("19700101010203000", dateString);
	  }

	  public virtual void TestRound()
	  {
		// we use default locale since LuceneTestCase randomizes it
		DateTime cal = new GregorianCalendar(TimeZone.getTimeZone("GMT"), Locale.Default);
		cal.clear();
		cal = new DateTime(2004, 1, 3, 22, 8, 56); // hour, minute, second -  year=2004, month=february(!), day=3
		cal.set(DateTime.MILLISECOND, 333);
		DateTime date = cal;
		Assert.AreEqual("2004-02-03 22:08:56:333", IsoFormat(date));

		DateTime dateYear = DateTools.Round(date, DateTools.Resolution.YEAR);
		Assert.AreEqual("2004-01-01 00:00:00:000", IsoFormat(dateYear));

		DateTime dateMonth = DateTools.Round(date, DateTools.Resolution.MONTH);
		Assert.AreEqual("2004-02-01 00:00:00:000", IsoFormat(dateMonth));

		DateTime dateDay = DateTools.Round(date, DateTools.Resolution.DAY);
		Assert.AreEqual("2004-02-03 00:00:00:000", IsoFormat(dateDay));

		DateTime dateHour = DateTools.Round(date, DateTools.Resolution.HOUR);
		Assert.AreEqual("2004-02-03 22:00:00:000", IsoFormat(dateHour));

		DateTime dateMinute = DateTools.Round(date, DateTools.Resolution.MINUTE);
		Assert.AreEqual("2004-02-03 22:08:00:000", IsoFormat(dateMinute));

		DateTime dateSecond = DateTools.Round(date, DateTools.Resolution.SECOND);
		Assert.AreEqual("2004-02-03 22:08:56:000", IsoFormat(dateSecond));

		DateTime dateMillisecond = DateTools.Round(date, DateTools.Resolution.MILLISECOND);
		Assert.AreEqual("2004-02-03 22:08:56:333", IsoFormat(dateMillisecond));

		// long parameter:
		long dateYearLong = DateTools.Round(date, DateTools.Resolution.YEAR);
		Assert.AreEqual("2004-01-01 00:00:00:000", IsoFormat(new DateTime(dateYearLong)));

		long dateMillisecondLong = DateTools.Round(date, DateTools.Resolution.MILLISECOND);
		Assert.AreEqual("2004-02-03 22:08:56:333", IsoFormat(new DateTime(dateMillisecondLong)));
	  }

	  private string IsoFormat(DateTime date)
	  {
		SimpleDateFormat sdf = new SimpleDateFormat("yyyy-MM-dd HH:mm:ss:SSS", Locale.ROOT);
		sdf.TimeZone = TimeZone.getTimeZone("GMT");
		return sdf.Format(date);
	  }

	  public virtual void TestDateToolsUTC()
	  {
		// Sun, 30 Oct 2005 00:00:00 +0000 -- the last second of 2005's DST in Europe/London
		long time = 1130630400;
		try
		{
			TimeZone.Default = TimeZone.getTimeZone("Europe/London"); // "GMT"
			string d1 = DateTools.DateToString(new DateTime(time * 1000), DateTools.Resolution.MINUTE);
			string d2 = DateTools.DateToString(new DateTime((time+3600) * 1000), DateTools.Resolution.MINUTE);
			Assert.IsFalse(d1.Equals(d2), "different times");
			Assert.AreEqual(DateTools.StringToTime(d1), time * 1000, "midnight");
			Assert.AreEqual(DateTools.StringToTime(d2), (time+3600) * 1000, "later");
		}
		finally
		{
			TimeZone.Default = null;
		}
	  }

	}

}