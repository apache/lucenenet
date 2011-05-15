using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace Lucene.Net.Analysis
{
	/// <summary>
	/// Loads a text file and adds every line as an entry to a Hashtable. Every line
	/// should contain only one word. If the file is not found or on any error, an
	/// empty table is returned.
	/// </summary>
	public class WordlistLoader
	{
		/// <summary>
		/// Load words table from the file
		/// </summary>
		/// <param name="path">Path to the wordlist</param>
		/// <param name="wordfile">Name of the wordlist</param>
		/// <returns></returns>
        public static ICollection<string> GetWordtable(String path, String wordfile) 
		{
			if ( path == null || wordfile == null ) 
			{
				return new List<string>();
			}
			return GetWordtable(new FileInfo(path + "\\" + wordfile));
		}

		/// <summary>
		/// Load words table from the file
		/// </summary>
		/// <param name="wordfile">Complete path to the wordlist</param>
		/// <returns></returns>
        public static ICollection<string> GetWordtable(String wordfile) 
		{
			if ( wordfile == null ) 
			{
				return new List<string>();
			}
			return GetWordtable( new FileInfo( wordfile ) );
		}

		/// <summary>
		/// Load words table from the file 
		/// </summary>
		/// <param name="wordfile">File containing the wordlist</param>
		/// <returns></returns>
		public static ICollection<string> GetWordtable( FileInfo wordfile ) 
		{
			if ( wordfile == null ) 
			{
				return new List<string>();
			}			
			StreamReader lnr = new StreamReader(wordfile.FullName);
			return GetWordtable(lnr);
		}

		/// <summary>
		/// Reads lines from a Reader and adds every line as an entry to a HashSet (omitting
		/// leading and trailing whitespace). Every line of the Reader should contain only
		/// one word. The words need to be in lowercase if you make use of an
		/// Analyzer which uses LowerCaseFilter (like StandardAnalyzer).
		/// </summary>
		/// <param name="reader">Reader containing the wordlist</param>
		/// <returns>A Hashtable with the reader's words</returns>
		public static ICollection<string> GetWordtable(TextReader reader)
		{
			ICollection<string> result = new List<string>();			
			try 
			{				
				List<string> stopWords = new List<string>();
				String word = null;
				while ( ( word = reader.ReadLine() ) != null ) 
				{
					stopWords.Add(word.Trim());
				}
				result = MakeWordTable(stopWords.ToArray(), stopWords.Count);
			}
				// On error, use an empty table
			catch (IOException) 
			{
				result = new List<string>();
			}
			return result;
		}


		/// <summary>
		/// Builds the wordlist table.
		/// </summary>
		/// <param name="words">Word that where read</param>
		/// <param name="length">Amount of words that where read into <tt>words</tt></param>
		/// <returns></returns>
		private static ICollection<string> MakeWordTable( String[] words, int length ) 
		{
			List<string> table = new List<string>( length );
			for ( int i = 0; i < length; i++ ) 
			{
				table.Add(words[i]);
			}
			return table;
		}
	}
}