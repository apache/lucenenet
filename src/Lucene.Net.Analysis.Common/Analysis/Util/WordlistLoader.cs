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
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.Util
{
	/// <summary>
	/// Loader for text files that represent a list of stopwords.
	/// </summary>
	/// <seealso cref="IOUtils"> to obtain <seealso cref="TextReader"/> instances
	/// @lucene.internal
	/// </seealso>
	public class WordlistLoader
	{
		private const int INITIAL_CAPACITY = 16;

		/// <summary>
		/// no instance
		/// </summary>
		private WordlistLoader()
		{
		}

		/// <summary>
		/// Reads lines from a Reader and adds every line as an entry to a CharArraySet (omitting
		/// leading and trailing whitespace). Every line of the Reader should contain only
		/// one word. The words need to be in lowercase if you make use of an
		/// Analyzer which uses LowerCaseFilter (like StandardAnalyzer).
		/// </summary>
		/// <param name="reader">Reader containing the wordlist</param>
		/// <param name="result">The <seealso cref="CharArraySet"/> to fill with the readers words</param>
		/// <returns>The given <seealso cref="CharArraySet"/> with the reader's words</returns>
		public static CharArraySet GetWordSet(TextReader reader, CharArraySet result)
		{
			string word = null;

			while ((word = reader.ReadLine()) != null)
			{
				result.Add(word.Trim());
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
		public static CharArraySet GetWordSet(TextReader reader, LuceneVersion matchVersion)
		{
			return GetWordSet(reader, new CharArraySet(matchVersion, WordlistLoader.INITIAL_CAPACITY, false));
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
		public static CharArraySet GetWordSet(TextReader reader, string comment, LuceneVersion matchVersion)
		{
			return GetWordSet(reader, comment, new CharArraySet(matchVersion, WordlistLoader.INITIAL_CAPACITY, false));
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
			string word = null;

			while ((word = reader.ReadLine()) != null)
			{
				if (word.StartsWith(comment, StringComparison.Ordinal) == false)
				{
					result.Add(word.Trim());
				}
			}

			return result;
		}

		/// <summary>
		/// Reads stopwords from a stopword list in Snowball format.
		/// <para>
		/// The snowball format is the following:
		/// <ul>
		/// <li>Lines may contain multiple words separated by whitespace.</li>
		/// <li>The comment character is the vertical line (&#124;).</li>
		/// <li>Lines may contain trailing comments.</li>
		/// </ul>
		/// </para>
		/// </summary>
		/// <param name="reader"> Reader containing a Snowball stopword list </param>
		/// <param name="result"> the <seealso cref="CharArraySet"/> to fill with the readers words </param>
		/// <returns> the given <seealso cref="CharArraySet"/> with the reader's words </returns>
		public static CharArraySet GetSnowballWordSet(TextReader reader, CharArraySet result)
		{
			string line = null;
			while ((line = reader.ReadLine()) != null)
			{
				var comment = line.IndexOf('|');
				if (comment >= 0)
				{
					line = line.Substring(0, comment);
				}

				// Splits the string by any whitespace characters: ' ', '\t', '\n', '\r'
				var words = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
				foreach (var word in words)
				{
					if (word.Length > 0)
					{
						result.Add(word);
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Reads stopwords from a stopword list in Snowball format.
		/// <para>
		/// The snowball format is the following:
		/// <ul>
		/// <li>Lines may contain multiple words separated by whitespace.</li>
		/// <li>The comment character is the vertical line (&#124;).</li>
		/// <li>Lines may contain trailing comments.</li>
		/// </ul>
		/// </para>
		/// </summary>
		/// <param name="reader"> Reader containing a Snowball stopword list </param>
		/// <param name="matchVersion"> the Lucene <seealso cref="Version"/> </param>
		/// <returns> A <seealso cref="CharArraySet"/> with the reader's words </returns>
		public static CharArraySet GetSnowballWordSet(TextReader reader, LuceneVersion matchVersion)
		{
			return GetSnowballWordSet(reader, new CharArraySet(matchVersion, WordlistLoader.INITIAL_CAPACITY, false));
		}
		
		/// <summary>
		/// Reads a stem dictionary. Each line contains:
		/// <pre>word<b>\t</b>stem</pre>
		/// (i.e. two tab separated words)
		/// </summary>
		/// <returns>Stem dictionary that overrules the stemming algorithm</returns>
		/// <exception cref="IOException">If there is a low-level I/O error.</exception>
		public static CharArrayMap<string> GetStemDict(TextReader reader, CharArrayMap<string> result)
		{
			string line;
			while ((line = reader.ReadLine()) != null)
			{
				var wordstem = line.Split(new[] { '\t' }, 2);
				result.Put(wordstem[0], wordstem[1]);
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
		public static IList<string> getLines(Stream stream, Encoding encoding)
		{
			TextReader reader = null;
			var success = false;

			try
			{
				reader = IOUtils.GetDecodingReader(stream, encoding);
				
				var lines = new List<string>();
				for (string word = null; (word = reader.ReadLine()) != null;)
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
					if (word.Length == 0)
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
					IOUtils.Close(reader);
				}
				else
				{
					IOUtils.CloseWhileHandlingException(reader);
				}
			}
		}
	}
}