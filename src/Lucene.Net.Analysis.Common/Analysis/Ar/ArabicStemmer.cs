namespace org.apache.lucene.analysis.ar
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

	using org.apache.lucene.analysis.util;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.apache.lucene.analysis.util.StemmerUtil.*;

	/// <summary>
	///  Stemmer for Arabic.
	///  <para>
	///  Stemming  is done in-place for efficiency, operating on a termbuffer.
	/// </para>
	///  <para>
	///  Stemming is defined as:
	///  <ul>
	///  <li> Removal of attached definite article, conjunction, and prepositions.
	///  <li> Stemming of common suffixes.
	/// </ul>
	/// 
	/// </para>
	/// </summary>
	public class ArabicStemmer
	{
	  public const char ALEF = '\u0627';
	  public const char BEH = '\u0628';
	  public const char TEH_MARBUTA = '\u0629';
	  public const char TEH = '\u062A';
	  public const char FEH = '\u0641';
	  public const char KAF = '\u0643';
	  public const char LAM = '\u0644';
	  public const char NOON = '\u0646';
	  public const char HEH = '\u0647';
	  public const char WAW = '\u0648';
	  public const char YEH = '\u064A';

	  public static readonly char[][] prefixes = {};

	  public static readonly char[][] suffixes = {};

	  /// <summary>
	  /// Stem an input buffer of Arabic text.
	  /// </summary>
	  /// <param name="s"> input buffer </param>
	  /// <param name="len"> length of input buffer </param>
	  /// <returns> length of input buffer after normalization </returns>
	  public virtual int stem(char[] s, int len)
	  {
		len = stemPrefix(s, len);
		len = stemSuffix(s, len);

		return len;
	  }

	  /// <summary>
	  /// Stem a prefix off an Arabic word. </summary>
	  /// <param name="s"> input buffer </param>
	  /// <param name="len"> length of input buffer </param>
	  /// <returns> new length of input buffer after stemming. </returns>
	  public virtual int stemPrefix(char[] s, int len)
	  {
		for (int i = 0; i < prefixes.Length; i++)
		{
		  if (startsWithCheckLength(s, len, prefixes[i]))
		  {
			return StemmerUtil.deleteN(s, 0, len, prefixes[i].Length);
		  }
		}
		return len;
	  }

	  /// <summary>
	  /// Stem suffix(es) off an Arabic word. </summary>
	  /// <param name="s"> input buffer </param>
	  /// <param name="len"> length of input buffer </param>
	  /// <returns> new length of input buffer after stemming </returns>
	  public virtual int stemSuffix(char[] s, int len)
	  {
		for (int i = 0; i < suffixes.Length; i++)
		{
		  if (endsWithCheckLength(s, len, suffixes[i]))
		  {
			len = StemmerUtil.deleteN(s, len - suffixes[i].Length, len, suffixes[i].Length);
		  }
		}
		return len;
	  }

	  /// <summary>
	  /// Returns true if the prefix matches and can be stemmed </summary>
	  /// <param name="s"> input buffer </param>
	  /// <param name="len"> length of input buffer </param>
	  /// <param name="prefix"> prefix to check </param>
	  /// <returns> true if the prefix matches and can be stemmed </returns>
	  internal virtual bool startsWithCheckLength(char[] s, int len, char[] prefix)
	  {
		if (prefix.Length == 1 && len < 4) // wa- prefix requires at least 3 characters
		{
		  return false;
		} // other prefixes require only 2.
		else if (len < prefix.Length + 2)
		{
		  return false;
		}
		else
		{
		  for (int i = 0; i < prefix.Length; i++)
		  {
			if (s[i] != prefix[i])
			{
			  return false;
			}
		  }

		  return true;
		}
	  }

	  /// <summary>
	  /// Returns true if the suffix matches and can be stemmed </summary>
	  /// <param name="s"> input buffer </param>
	  /// <param name="len"> length of input buffer </param>
	  /// <param name="suffix"> suffix to check </param>
	  /// <returns> true if the suffix matches and can be stemmed </returns>
	  internal virtual bool endsWithCheckLength(char[] s, int len, char[] suffix)
	  {
		if (len < suffix.Length + 2) // all suffixes require at least 2 characters after stemming
		{
		  return false;
		}
		else
		{
		  for (int i = 0; i < suffix.Length; i++)
		  {
			if (s[len - suffix.Length + i] != suffix[i])
			{
			  return false;
			}
		  }

		  return true;
		}
	  }
	}

}