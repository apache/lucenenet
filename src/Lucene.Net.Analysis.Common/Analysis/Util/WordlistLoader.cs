using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Util;
using org.apache.lucene.analysis.util;
using Version = System.Version;

namespace Lucene.Net.Analysis.Util
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
	/// Loader for text files that represent a list of stopwords.
	/// </summary>
	/// <seealso cref= IOUtils to obtain <seealso cref="Reader"/> instances
	/// @lucene.internal </seealso>
	public class WordlistLoader
	{

	  private const int INITIAL_CAPACITY = 16;

	  /// <summary>
	  /// no instance </summary>
	  private WordlistLoader()
	  {
	  }

	  /// <summary>
	  /// Reads lines from a Reader and adds every line as an entry to a CharArraySet (omitting
	  /// leading and trailing whitespace). Every line of the Reader should contain only
	  /// one word. The words need to be in lowercase if you make use of an
	  /// Analyzer which uses LowerCaseFilter (like StandardAnalyzer).
	  /// </summary>
	  /// <param name="reader"> Reader containing the wordlist </param>
	  /// <param name="result"> the <seealso cref="CharArraySet"/> to fill with the readers words </param>
	  /// <returns> the given <seealso cref="CharArraySet"/> with the reader's words </returns>
	  public static CharArraySet GetWordSet(TextReader reader, CharArraySet result)
	  {
		BufferedReader br = null;
		try
		{
		  br = getBufferedReader(reader);
		  string word = null;
		  while ((word = br.readLine()) != null)
		  {
			result.add(word.Trim());
		  }
		}
		finally
		{
		  IOUtils.close(br);
		}
		return result;
	  }

	  /// <summary>
	  /// Reads lines from a Reader and adds every line as an entry to a CharArraySet (omitting
	  /// leading and trailing whitespace). Every line of the Reader should contain only
	  /// one word. The words need to be in lowercase if you make use of an
	  /// Analyzer which uses LowerCaseFilter (like StandardAnalyzer).
	  /// </summary>
	  /// <param name="reader"> Reader containing the wordlist </param>
	  /// <param name="matchVersion"> the Lucene <seealso cref="System.Version"/> </param>
	  /// <returns> A <seealso cref="CharArraySet"/> with the reader's words </returns>
	  public static CharArraySet GetWordSet(TextReader reader, Version matchVersion)
	  {
		return GetWordSet(reader, new CharArraySet(matchVersion, INITIAL_CAPACITY, false));
	  }

	  /// <summary>
	  /// Reads lines from a Reader and adds every non-comment line as an entry to a CharArraySet (omitting
	  /// leading and trailing whitespace). Every line of the Reader should contain only
	  /// one word. The words need to be in lowercase if you make use of an
	  /// Analyzer which uses LowerCaseFilter (like StandardAnalyzer).
	  /// </summary>
	  /// <param name="reader"> Reader containing the wordlist </param>
	  /// <param name="comment"> The string representing a comment. </param>
	  /// <param name="matchVersion"> the Lucene <seealso cref="Version"/> </param>
	  /// <returns> A CharArraySet with the reader's words </returns>
	  public static CharArraySet GetWordSet(TextReader reader, string comment, Version matchVersion)
	  {
		return GetWordSet(reader, comment, new CharArraySet(matchVersion, INITIAL_CAPACITY, false));
	  }

	  /// <summary>
	  /// Reads lines from a Reader and adds every non-comment line as an entry to a CharArraySet (omitting
	  /// leading and trailing whitespace). Every line of the Reader should contain only
	  /// one word. The words need to be in lowercase if you make use of an
	  /// Analyzer which uses LowerCaseFilter (like StandardAnalyzer).
	  /// </summary>
	  /// <param name="reader"> Reader containing the wordlist </param>
	  /// <param name="comment"> The string representing a comment. </param>
	  /// <param name="result"> the <seealso cref="CharArraySet"/> to fill with the readers words </param>
	  /// <returns> the given <seealso cref="CharArraySet"/> with the reader's words </returns>
	  public static CharArraySet GetWordSet(TextReader reader, string comment, CharArraySet result)
	  {
		BufferedReader br = null;
		try
		{
		  br = getBufferedReader(reader);
		  string word = null;
		  while ((word = br.ReadLine()) != null)
		  {
			if (word.StartsWith(comment, StringComparison.Ordinal) == false)
			{
			  result.add(word.Trim());
			}
		  }
		}
		finally
		{
		  IOUtils.Close(br);
		}
		return result;
	  }


	  /// <summary>
	  /// Reads stopwords from a stopword list in Snowball format.
	  /// <para>
	  /// The snowball format is the following:
	  /// <ul>
	  /// <li>Lines may contain multiple words separated by whitespace.
	  /// <li>The comment character is the vertical line (&#124;).
	  /// <li>Lines may contain trailing comments.
	  /// </ul>
	  /// </para>
	  /// </summary>
	  /// <param name="reader"> Reader containing a Snowball stopword list </param>
	  /// <param name="result"> the <seealso cref="CharArraySet"/> to fill with the readers words </param>
	  /// <returns> the given <seealso cref="CharArraySet"/> with the reader's words </returns>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public static CharArraySet getSnowballWordSet(java.io.Reader reader, CharArraySet result) throws java.io.IOException
	  public static CharArraySet getSnowballWordSet(Reader reader, CharArraySet result)
	  {
		BufferedReader br = null;
		try
		{
		  br = getBufferedReader(reader);
		  string line = null;
		  while ((line = br.readLine()) != null)
		  {
			int comment = line.IndexOf('|');
			if (comment >= 0)
			{
				line = line.Substring(0, comment);
			}
			string[] words = line.Split("\\s+", true);
			for (int i = 0; i < words.Length; i++)
			{
			  if (words[i].Length > 0)
			  {
				  result.add(words[i]);
			  }
			}
		  }
		}
		finally
		{
		  IOUtils.close(br);
		}
		return result;
	  }

	  /// <summary>
	  /// Reads stopwords from a stopword list in Snowball format.
	  /// <para>
	  /// The snowball format is the following:
	  /// <ul>
	  /// <li>Lines may contain multiple words separated by whitespace.
	  /// <li>The comment character is the vertical line (&#124;).
	  /// <li>Lines may contain trailing comments.
	  /// </ul>
	  /// </para>
	  /// </summary>
	  /// <param name="reader"> Reader containing a Snowball stopword list </param>
	  /// <param name="matchVersion"> the Lucene <seealso cref="Version"/> </param>
	  /// <returns> A <seealso cref="CharArraySet"/> with the reader's words </returns>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public static CharArraySet getSnowballWordSet(java.io.Reader reader, org.apache.lucene.util.Version matchVersion) throws java.io.IOException
	  public static CharArraySet getSnowballWordSet(Reader reader, Version matchVersion)
	  {
		return getSnowballWordSet(reader, new CharArraySet(matchVersion, INITIAL_CAPACITY, false));
	  }


	  /// <summary>
	  /// Reads a stem dictionary. Each line contains:
	  /// <pre>word<b>\t</b>stem</pre>
	  /// (i.e. two tab separated words)
	  /// </summary>
	  /// <returns> stem dictionary that overrules the stemming algorithm </returns>
	  /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public static CharArrayMap<String> getStemDict(java.io.Reader reader, CharArrayMap<String> result) throws java.io.IOException
	  public static CharArrayMap<string> getStemDict(Reader reader, CharArrayMap<string> result)
	  {
		BufferedReader br = null;
		try
		{
		  br = getBufferedReader(reader);
		  string line;
		  while ((line = br.readLine()) != null)
		  {
			string[] wordstem = line.Split("\t", 2);
			result.put(wordstem[0], wordstem[1]);
		  }
		}
		finally
		{
		  IOUtils.close(br);
		}
		return result;
	  }

	  /// <summary>
	  /// Accesses a resource by name and returns the (non comment) lines containing
	  /// data using the given character encoding.
	  /// 
	  /// <para>
	  /// A comment line is any line that starts with the character "#"
	  /// </para>
	  /// </summary>
	  /// <returns> a list of non-blank non-comment lines with whitespace trimmed </returns>
	  /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public static java.util.List<String> getLines(java.io.InputStream stream, java.nio.charset.Charset charset) throws java.io.IOException
	  public static IList<string> getLines(InputStream stream, Charset charset)
	  {
		BufferedReader input = null;
		List<string> lines;
		bool success = false;
		try
		{
		  input = getBufferedReader(IOUtils.getDecodingReader(stream, charset));

		  lines = new List<>();
		  for (string word = null; (word = input.readLine()) != null;)
		  {
			// skip initial bom marker
			if (lines.Count == 0 && word.Length > 0 && word[0] == '\uFEFF')
			{
			  word = word.Substring(1);
			}
			// skip comments
			if (word.StartsWith("#", StringComparison.Ordinal))
			{
				continue;
			}
			word = word.Trim();
			// skip blank lines
			if (word.length() == 0)
			{
				continue;
			}
			lines.Add(word);
		  }
		  success = true;
		  return lines;
		}
		finally
		{
		  if (success)
		  {
			IOUtils.close(input);
		  }
		  else
		  {
			IOUtils.closeWhileHandlingException(input);
		  }
		}
	  }

	  private static BufferedReader getBufferedReader(Reader reader)
	  {
		return (reader is BufferedReader) ? (BufferedReader) reader : new BufferedReader(reader);
	  }

	}

}