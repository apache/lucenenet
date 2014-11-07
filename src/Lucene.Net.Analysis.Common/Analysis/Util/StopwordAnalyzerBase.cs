using System;

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

namespace org.apache.lucene.analysis.util
{


	using IOUtils = org.apache.lucene.util.IOUtils;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// Base class for Analyzers that need to make use of stopword sets. 
	/// 
	/// </summary>
	public abstract class StopwordAnalyzerBase : Analyzer
	{

	  /// <summary>
	  /// An immutable stopword set
	  /// </summary>
	  protected internal readonly CharArraySet stopwords;

	  protected internal readonly Version matchVersion;

	  /// <summary>
	  /// Returns the analyzer's stopword set or an empty set if the analyzer has no
	  /// stopwords
	  /// </summary>
	  /// <returns> the analyzer's stopword set or an empty set if the analyzer has no
	  ///         stopwords </returns>
	  public virtual CharArraySet StopwordSet
	  {
		  get
		  {
			return stopwords;
		  }
	  }

	  /// <summary>
	  /// Creates a new instance initialized with the given stopword set
	  /// </summary>
	  /// <param name="version">
	  ///          the Lucene version for cross version compatibility </param>
	  /// <param name="stopwords">
	  ///          the analyzer's stopword set </param>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: protected StopwordAnalyzerBase(final org.apache.lucene.util.Version version, final CharArraySet stopwords)
	  protected internal StopwordAnalyzerBase(Version version, CharArraySet stopwords)
	  {
		matchVersion = version;
		// analyzers should use char array set for stopwords!
		this.stopwords = stopwords == null ? CharArraySet.EMPTY_SET : CharArraySet.unmodifiableSet(CharArraySet.copy(version, stopwords));
	  }

	  /// <summary>
	  /// Creates a new Analyzer with an empty stopword set
	  /// </summary>
	  /// <param name="version">
	  ///          the Lucene version for cross version compatibility </param>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: protected StopwordAnalyzerBase(final org.apache.lucene.util.Version version)
	  protected internal StopwordAnalyzerBase(Version version) : this(version, null)
	  {
	  }

	  /// <summary>
	  /// Creates a CharArraySet from a file resource associated with a class. (See
	  /// <seealso cref="Class#getResourceAsStream(String)"/>).
	  /// </summary>
	  /// <param name="ignoreCase">
	  ///          <code>true</code> if the set should ignore the case of the
	  ///          stopwords, otherwise <code>false</code> </param>
	  /// <param name="aClass">
	  ///          a class that is associated with the given stopwordResource </param>
	  /// <param name="resource">
	  ///          name of the resource file associated with the given class </param>
	  /// <param name="comment">
	  ///          comment string to ignore in the stopword file </param>
	  /// <returns> a CharArraySet containing the distinct stopwords from the given
	  ///         file </returns>
	  /// <exception cref="IOException">
	  ///           if loading the stopwords throws an <seealso cref="IOException"/> </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected static CharArraySet loadStopwordSet(final boolean ignoreCase, final Class aClass, final String resource, final String comment) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
	  protected internal static CharArraySet loadStopwordSet(bool ignoreCase, Type aClass, string resource, string comment)
	  {
		Reader reader = null;
		try
		{
		  reader = IOUtils.getDecodingReader(aClass.getResourceAsStream(resource), StandardCharsets.UTF_8);
		  return WordlistLoader.getWordSet(reader, comment, new CharArraySet(Version.LUCENE_CURRENT, 16, ignoreCase));
		}
		finally
		{
		  IOUtils.close(reader);
		}

	  }

	  /// <summary>
	  /// Creates a CharArraySet from a file.
	  /// </summary>
	  /// <param name="stopwords">
	  ///          the stopwords file to load
	  /// </param>
	  /// <param name="matchVersion">
	  ///          the Lucene version for cross version compatibility </param>
	  /// <returns> a CharArraySet containing the distinct stopwords from the given
	  ///         file </returns>
	  /// <exception cref="IOException">
	  ///           if loading the stopwords throws an <seealso cref="IOException"/> </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected static CharArraySet loadStopwordSet(java.io.File stopwords, org.apache.lucene.util.Version matchVersion) throws java.io.IOException
	  protected internal static CharArraySet loadStopwordSet(File stopwords, Version matchVersion)
	  {
		Reader reader = null;
		try
		{
		  reader = IOUtils.getDecodingReader(stopwords, StandardCharsets.UTF_8);
		  return WordlistLoader.getWordSet(reader, matchVersion);
		}
		finally
		{
		  IOUtils.close(reader);
		}
	  }

	  /// <summary>
	  /// Creates a CharArraySet from a file.
	  /// </summary>
	  /// <param name="stopwords">
	  ///          the stopwords reader to load
	  /// </param>
	  /// <param name="matchVersion">
	  ///          the Lucene version for cross version compatibility </param>
	  /// <returns> a CharArraySet containing the distinct stopwords from the given
	  ///         reader </returns>
	  /// <exception cref="IOException">
	  ///           if loading the stopwords throws an <seealso cref="IOException"/> </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected static CharArraySet loadStopwordSet(java.io.Reader stopwords, org.apache.lucene.util.Version matchVersion) throws java.io.IOException
	  protected internal static CharArraySet loadStopwordSet(Reader stopwords, Version matchVersion)
	  {
		try
		{
		  return WordlistLoader.getWordSet(stopwords, matchVersion);
		}
		finally
		{
		  IOUtils.close(stopwords);
		}
	  }
	}

}