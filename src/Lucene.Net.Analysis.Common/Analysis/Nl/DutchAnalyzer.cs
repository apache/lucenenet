using System;

namespace org.apache.lucene.analysis.nl
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

	using LowerCaseFilter = org.apache.lucene.analysis.core.LowerCaseFilter;
	using StopFilter = org.apache.lucene.analysis.core.StopFilter;
	using SetKeywordMarkerFilter = org.apache.lucene.analysis.miscellaneous.SetKeywordMarkerFilter;
	using StemmerOverrideMap = org.apache.lucene.analysis.miscellaneous.StemmerOverrideFilter.StemmerOverrideMap;
	using StemmerOverrideFilter = org.apache.lucene.analysis.miscellaneous.StemmerOverrideFilter;
	using SnowballFilter = org.apache.lucene.analysis.snowball.SnowballFilter;
	using StandardFilter = org.apache.lucene.analysis.standard.StandardFilter;
	using StandardTokenizer = org.apache.lucene.analysis.standard.StandardTokenizer;
	using StandardAnalyzer = org.apache.lucene.analysis.standard.StandardAnalyzer; // for javadoc
	using org.apache.lucene.analysis.util;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using WordlistLoader = org.apache.lucene.analysis.util.WordlistLoader;
	using CharsRef = org.apache.lucene.util.CharsRef;
	using IOUtils = org.apache.lucene.util.IOUtils;
	using Version = org.apache.lucene.util.Version;


	/// <summary>
	/// <seealso cref="Analyzer"/> for Dutch language. 
	/// <para>
	/// Supports an external list of stopwords (words that
	/// will not be indexed at all), an external list of exclusions (word that will
	/// not be stemmed, but indexed) and an external list of word-stem pairs that overrule
	/// the algorithm (dictionary stemming).
	/// A default set of stopwords is used unless an alternative list is specified, but the
	/// exclusion list is empty by default.
	/// </para>
	/// 
	/// <a name="version"/>
	/// <para>You must specify the required <seealso cref="Version"/>
	/// compatibility when creating DutchAnalyzer:
	/// <ul>
	///   <li> As of 3.6, <seealso cref="#DutchAnalyzer(Version, CharArraySet)"/> and
	///        <seealso cref="#DutchAnalyzer(Version, CharArraySet, CharArraySet)"/> also populate
	///        the default entries for the stem override dictionary
	///   <li> As of 3.1, Snowball stemming is done with SnowballFilter, 
	///        LowerCaseFilter is used prior to StopFilter, and Snowball 
	///        stopwords are used by default.
	///   <li> As of 2.9, StopFilter preserves position
	///        increments
	/// </ul>
	/// 
	/// </para>
	/// <para><b>NOTE</b>: This class uses the same <seealso cref="Version"/>
	/// dependent settings as <seealso cref="StandardAnalyzer"/>.</para>
	/// </summary>
	public sealed class DutchAnalyzer : Analyzer
	{

	  /// <summary>
	  /// File containing default Dutch stopwords. </summary>
	  public const string DEFAULT_STOPWORD_FILE = "dutch_stop.txt";

	  /// <summary>
	  /// Returns an unmodifiable instance of the default stop-words set. </summary>
	  /// <returns> an unmodifiable instance of the default stop-words set. </returns>
	  public static CharArraySet DefaultStopSet
	  {
		  get
		  {
			return DefaultSetHolder.DEFAULT_STOP_SET;
		  }
	  }

	  private class DefaultSetHolder
	  {
		internal static readonly CharArraySet DEFAULT_STOP_SET;
		internal static readonly CharArrayMap<string> DEFAULT_STEM_DICT;
		static DefaultSetHolder()
		{
		  try
		  {
			DEFAULT_STOP_SET = WordlistLoader.getSnowballWordSet(IOUtils.getDecodingReader(typeof(SnowballFilter), DEFAULT_STOPWORD_FILE, StandardCharsets.UTF_8), Version.LUCENE_CURRENT);
		  }
		  catch (IOException)
		  {
			// default set should always be present as it is part of the
			// distribution (JAR)
			throw new Exception("Unable to load default stopword set");
		  }

		  DEFAULT_STEM_DICT = new CharArrayMap<>(Version.LUCENE_CURRENT, 4, false);
		  DEFAULT_STEM_DICT.put("fiets", "fiets"); //otherwise fiet
		  DEFAULT_STEM_DICT.put("bromfiets", "bromfiets"); //otherwise bromfiet
		  DEFAULT_STEM_DICT.put("ei", "eier");
		  DEFAULT_STEM_DICT.put("kind", "kinder");
		}
	  }


	  /// <summary>
	  /// Contains the stopwords used with the StopFilter.
	  /// </summary>
	  private readonly CharArraySet stoptable;

	  /// <summary>
	  /// Contains words that should be indexed but not stemmed.
	  /// </summary>
	  private CharArraySet excltable = CharArraySet.EMPTY_SET;

	  private readonly StemmerOverrideMap stemdict;

	  // null if on 3.1 or later - only for bw compat
	  private readonly CharArrayMap<string> origStemdict;
	  private readonly Version matchVersion;

	  /// <summary>
	  /// Builds an analyzer with the default stop words (<seealso cref="#getDefaultStopSet()"/>) 
	  /// and a few default entries for the stem exclusion table.
	  /// 
	  /// </summary>
	  public DutchAnalyzer(Version matchVersion) : this(matchVersion, DefaultSetHolder.DEFAULT_STOP_SET, CharArraySet.EMPTY_SET, DefaultSetHolder.DEFAULT_STEM_DICT)
	  {
		// historically, only this ctor populated the stem dict!!!!!
	  }

	  public DutchAnalyzer(Version matchVersion, CharArraySet stopwords) : this(matchVersion, stopwords, CharArraySet.EMPTY_SET, matchVersion.onOrAfter(Version.LUCENE_36) ? DefaultSetHolder.DEFAULT_STEM_DICT : CharArrayMap.emptyMap<string>())
	  {
		// historically, this ctor never the stem dict!!!!!
		// so we populate it only for >= 3.6
	  }

	  public DutchAnalyzer(Version matchVersion, CharArraySet stopwords, CharArraySet stemExclusionTable) : this(matchVersion, stopwords, stemExclusionTable, matchVersion.onOrAfter(Version.LUCENE_36) ? DefaultSetHolder.DEFAULT_STEM_DICT : CharArrayMap.emptyMap<string>())
	  {
		// historically, this ctor never the stem dict!!!!!
		// so we populate it only for >= 3.6
	  }

	  public DutchAnalyzer(Version matchVersion, CharArraySet stopwords, CharArraySet stemExclusionTable, CharArrayMap<string> stemOverrideDict)
	  {
		this.matchVersion = matchVersion;
		this.stoptable = CharArraySet.unmodifiableSet(CharArraySet.copy(matchVersion, stopwords));
		this.excltable = CharArraySet.unmodifiableSet(CharArraySet.copy(matchVersion, stemExclusionTable));
		if (stemOverrideDict.Empty || !matchVersion.onOrAfter(Version.LUCENE_31))
		{
		  this.stemdict = null;
		  this.origStemdict = CharArrayMap.unmodifiableMap(CharArrayMap.copy(matchVersion, stemOverrideDict));
		}
		else
		{
		  this.origStemdict = null;
		  // we don't need to ignore case here since we lowercase in this analyzer anyway
		  StemmerOverrideFilter.Builder builder = new StemmerOverrideFilter.Builder(false);
		  CharArrayMap<string>.EntryIterator iter = stemOverrideDict.entrySet().GetEnumerator();
		  CharsRef spare = new CharsRef();
		  while (iter.hasNext())
		  {
			char[] nextKey = iter.nextKey();
			spare.copyChars(nextKey, 0, nextKey.Length);
			builder.add(spare, iter.currentValue());
		  }
		  try
		  {
			this.stemdict = builder.build();
		  }
		  catch (IOException ex)
		  {
			throw new Exception("can not build stem dict", ex);
		  }
		}
	  }

	  /// <summary>
	  /// Returns a (possibly reused) <seealso cref="TokenStream"/> which tokenizes all the 
	  /// text in the provided <seealso cref="Reader"/>.
	  /// </summary>
	  /// <returns> A <seealso cref="TokenStream"/> built from a <seealso cref="StandardTokenizer"/>
	  ///   filtered with <seealso cref="StandardFilter"/>, <seealso cref="LowerCaseFilter"/>, 
	  ///   <seealso cref="StopFilter"/>, <seealso cref="SetKeywordMarkerFilter"/> if a stem exclusion set is provided,
	  ///   <seealso cref="StemmerOverrideFilter"/>, and <seealso cref="SnowballFilter"/> </returns>
	  protected internal override TokenStreamComponents createComponents(string fieldName, Reader aReader)
	  {
		if (matchVersion.onOrAfter(Version.LUCENE_31))
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.Tokenizer source = new org.apache.lucene.analysis.standard.StandardTokenizer(matchVersion, aReader);
		  Tokenizer source = new StandardTokenizer(matchVersion, aReader);
		  TokenStream result = new StandardFilter(matchVersion, source);
		  result = new LowerCaseFilter(matchVersion, result);
		  result = new StopFilter(matchVersion, result, stoptable);
		  if (!excltable.Empty)
		  {
			result = new SetKeywordMarkerFilter(result, excltable);
		  }
		  if (stemdict != null)
		  {
			result = new StemmerOverrideFilter(result, stemdict);
		  }
		  result = new SnowballFilter(result, new org.tartarus.snowball.ext.DutchStemmer());
		  return new TokenStreamComponents(source, result);
		}
		else
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.Tokenizer source = new org.apache.lucene.analysis.standard.StandardTokenizer(matchVersion, aReader);
		  Tokenizer source = new StandardTokenizer(matchVersion, aReader);
		  TokenStream result = new StandardFilter(matchVersion, source);
		  result = new StopFilter(matchVersion, result, stoptable);
		  if (!excltable.Empty)
		  {
			result = new SetKeywordMarkerFilter(result, excltable);
		  }
		  result = new DutchStemFilter(result, origStemdict);
		  return new TokenStreamComponents(source, result);
		}
	  }
	}

}