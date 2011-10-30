using System;
using System.IO;
using System.Collections;

namespace Lucene.Net.Analysis.De
{
	/// <summary>
	/// Loads a text file and adds every line as an entry to a Hashtable. Every line
	/// should contain only one word. If the file is not found or on any error, an
	/// empty table is returned.
	/// </summary>
	public class WordlistLoader
	{
		/// <summary>
		/// </summary>
		/// <param name="path">Path to the wordlist</param>
		/// <param name="wordfile">Name of the wordlist</param>
		/// <returns></returns>
		public static Hashtable GetWordtable( String path, String wordfile ) 
		{
			if ( path == null || wordfile == null ) 
			{
				return new Hashtable();
			}
			return GetWordtable(new FileInfo(path + "\\" + wordfile));
		}

		/// <summary>
		/// </summary>
		/// <param name="wordfile">Complete path to the wordlist</param>
		/// <returns></returns>
		public static Hashtable GetWordtable( String wordfile ) 
		{
			if ( wordfile == null ) 
			{
				return new Hashtable();
			}
			return GetWordtable( new FileInfo( wordfile ) );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="wordfile">File containing the wordlist</param>
		/// <returns></returns>
		public static Hashtable GetWordtable( FileInfo wordfile ) 
		{
			if ( wordfile == null ) 
			{
				return new Hashtable();
			}
			Hashtable result = null;
			try 
			{
				StreamReader lnr = new StreamReader(wordfile.FullName);
				String word = null;
				String[] stopwords = new String[100];
				int wordcount = 0;
				while ( ( word = lnr.ReadLine() ) != null ) 
				{
					wordcount++;
					if ( wordcount == stopwords.Length ) 
					{
						String[] tmp = new String[stopwords.Length + 50];
						Array.Copy( stopwords, 0, tmp, 0, wordcount );
						stopwords = tmp;
					}
					stopwords[wordcount-1] = word;
				}
				result = MakeWordTable( stopwords, wordcount );
			}
				// On error, use an empty table
			catch (IOException) 
			{
				result = new Hashtable();
			}
			return result;
		}

		/// <summary>
		/// Builds the wordlist table.
		/// </summary>
		/// <param name="words">Word that where read</param>
		/// <param name="length">Amount of words that where read into <tt>words</tt></param>
		/// <returns></returns>
		private static Hashtable MakeWordTable( String[] words, int length ) 
		{
			Hashtable table = new Hashtable( length );
			for ( int i = 0; i < length; i++ ) 
			{
				table.Add(words[i], words[i]);
			}
			return table;
		}
	}
}