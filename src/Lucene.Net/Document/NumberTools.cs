/*
 * Copyright 2004 The Apache Software Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

namespace Lucene.Net.Documents
{
	
	/// <summary> Provides support for converting longs to Strings, and back again. The strings
	/// are structured so that lexicographic sorting order is preserved.
	/// 
	/// <p>
	/// That is, if l1 is less than l2 for any two longs l1 and l2, then
	/// NumberTools.longToString(l1) is lexicographically less than
	/// NumberTools.longToString(l2). (Similarly for "greater than" and "equals".)
	/// 
	/// <p>
	/// This class handles <b>all</b> long values (unlike
	/// {@link Lucene.Net.document.DateField}).
	/// 
	/// </summary>
	/// <author>  Matt Quail (spud at madbean dot com)
	/// </author>
	public class NumberTools
	{
		
		private const int RADIX = 16;
		
		private const char NEGATIVE_PREFIX = '-';
		
		// NB: NEGATIVE_PREFIX must be < POSITIVE_PREFIX
		private const char POSITIVE_PREFIX = '0';
		
		//NB: this must be less than
		/// <summary> Equivalent to longToString(Long.MIN_VALUE)</summary>
		public static readonly System.String MIN_STRING_VALUE = NEGATIVE_PREFIX + "0000000000000000";
		
		/// <summary> Equivalent to longToString(Long.MAX_VALUE)</summary>
		public static readonly System.String MAX_STRING_VALUE = POSITIVE_PREFIX + "7fffffffffffffff";
		
		/// <summary> The length of (all) strings returned by {@link #longToString}</summary>
		public static readonly int STR_SIZE = MIN_STRING_VALUE.Length;
		
		/// <summary> Converts a long to a String suitable for indexing.</summary>
		public static System.String LongToString(long l)
		{
            if (l == System.Int64.MinValue)
			{
				// special case, because long is not symetric around zero
				return MIN_STRING_VALUE;
			}
			
			System.Text.StringBuilder buf = new System.Text.StringBuilder(STR_SIZE);
			
			if (l < 0)
			{
				buf.Append(NEGATIVE_PREFIX);
				l = System.Int64.MaxValue + l + 1;
			}
			else
			{
				buf.Append(POSITIVE_PREFIX);
			}
			System.String num = System.Convert.ToString(l, RADIX);
			
			int padLen = STR_SIZE - num.Length - buf.Length;
			while (padLen-- > 0)
			{
				buf.Append('0');
			}
			buf.Append(num);
			
			return buf.ToString();
		}
		
		/// <summary> Converts a String that was returned by {@link #longToString} back to a
		/// long.
		/// 
		/// </summary>
		/// <throws>  IllegalArgumentException </throws>
		/// <summary>             if the input is null
		/// </summary>
		/// <throws>  NumberFormatException </throws>
		/// <summary>             if the input does not parse (it was not a String returned by
		/// longToString()).
		/// </summary>
		public static long StringToLong(System.String str)
		{
			if (str == null)
			{
				throw new System.NullReferenceException("string cannot be null");
			}
			if (str.Length != STR_SIZE)
			{
				throw new System.FormatException("string is the wrong size");
			}
			
			if (str.Equals(MIN_STRING_VALUE))
			{
				return System.Int64.MinValue;
			}
			
			char prefix = str[0];
			long l = System.Convert.ToInt64(str.Substring(1), RADIX);
			
			if (prefix == POSITIVE_PREFIX)
			{
				// nop
			}
			else if (prefix == NEGATIVE_PREFIX)
			{
				l = l - System.Int64.MaxValue - 1;
			}
			else
			{
				throw new System.FormatException("string does not begin with the correct prefix");
			}
			
			return l;
		}
	}
}