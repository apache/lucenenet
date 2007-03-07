/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
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

namespace Lucene.Net.Analysis
{
	
	/// <summary> Loader for text files that represent a list of stopwords.
	/// 
	/// </summary>
	/// <author>  Gerhard Schwarz
	/// </author>
	/// <version>  $Id: WordlistLoader.java 192989 2005-06-22 19:59:03Z dnaber $
	/// </version>
	public class WordlistLoader
	{
		
		/// <summary> Loads a text file and adds every line as an entry to a HashSet (omitting
		/// leading and trailing whitespace). Every line of the file should contain only
		/// one word. The words need to be in lowercase if you make use of an
		/// Analyzer which uses LowerCaseFilter (like StandardAnalyzer).
		/// 
		/// </summary>
		/// <param name="wordfile">File containing the wordlist
		/// </param>
		/// <returns> A HashSet with the file's words
		/// </returns>
		public static System.Collections.Hashtable GetWordSet(System.IO.FileInfo wordfile)
		{
			System.Collections.Hashtable result = new System.Collections.Hashtable();
			System.IO.TextReader reader = null;
			try
			{
				reader = new System.IO.StreamReader(wordfile.FullName, System.Text.Encoding.Default);
				result = GetWordSet(reader);
			}
			finally
			{
				if (reader != null)
					reader.Close();
			}
			return result;
		}
		
		/// <summary> Reads lines from a Reader and adds every line as an entry to a HashSet (omitting
		/// leading and trailing whitespace). Every line of the Reader should contain only
		/// one word. The words need to be in lowercase if you make use of an
		/// Analyzer which uses LowerCaseFilter (like StandardAnalyzer).
		/// 
		/// </summary>
		/// <param name="reader">Reader containing the wordlist
		/// </param>
		/// <returns> A HashSet with the reader's words
		/// </returns>
		public static System.Collections.Hashtable GetWordSet(System.IO.TextReader reader)
		{
			System.Collections.Hashtable result = new System.Collections.Hashtable();
			System.IO.TextReader br = null;
			try
			{
				br = (System.IO.TextReader) reader;
				System.String word = null;
				while ((word = br.ReadLine()) != null)
				{
                    System.String tmp = word.Trim();
					result.Add(tmp, tmp);
				}
			}
			finally
			{
				if (br != null)
					br.Close();
			}
			return result;
		}
		
		
		/// <summary> Builds a wordlist table, using words as both keys and values
		/// for backward compatibility.
		/// 
		/// </summary>
		/// <param name="wordSet">  stopword set
		/// </param>
		private static System.Collections.Hashtable MakeWordTable(System.Collections.Hashtable wordSet)
		{
			System.Collections.Hashtable table = System.Collections.Hashtable.Synchronized(new System.Collections.Hashtable());
			for (System.Collections.IEnumerator iter = wordSet.GetEnumerator(); iter.MoveNext(); )
			{
				System.String word = (System.String) iter.Current;
				table[word] = word;
			}
			return table;
		}
	}
}