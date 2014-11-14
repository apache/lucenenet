using System.Collections.Generic;

namespace org.apache.lucene.analysis.hunspell
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


	using ResourceLoader = org.apache.lucene.analysis.util.ResourceLoader;
	using ResourceLoaderAware = org.apache.lucene.analysis.util.ResourceLoaderAware;
	using TokenFilterFactory = org.apache.lucene.analysis.util.TokenFilterFactory;
	using IOUtils = org.apache.lucene.util.IOUtils;

	/// <summary>
	/// TokenFilterFactory that creates instances of <seealso cref="HunspellStemFilter"/>.
	/// Example config for British English:
	/// <pre class="prettyprint">
	/// &lt;filter class=&quot;solr.HunspellStemFilterFactory&quot;
	///         dictionary=&quot;en_GB.dic,my_custom.dic&quot;
	///         affix=&quot;en_GB.aff&quot; 
	///         ignoreCase=&quot;false&quot;
	///         longestOnly=&quot;false&quot; /&gt;</pre>
	/// Both parameters dictionary and affix are mandatory.
	/// Dictionaries for many languages are available through the OpenOffice project.
	/// 
	/// See <a href="http://wiki.apache.org/solr/Hunspell">http://wiki.apache.org/solr/Hunspell</a>
	/// @lucene.experimental
	/// </summary>
	public class HunspellStemFilterFactory : TokenFilterFactory, ResourceLoaderAware
	{
	  private const string PARAM_DICTIONARY = "dictionary";
	  private const string PARAM_AFFIX = "affix";
	  private const string PARAM_RECURSION_CAP = "recursionCap";
	  private const string PARAM_IGNORE_CASE = "ignoreCase";
	  private const string PARAM_LONGEST_ONLY = "longestOnly";

	  private readonly string dictionaryFiles;
	  private readonly string affixFile;
	  private readonly bool ignoreCase;
	  private readonly bool longestOnly;
	  private Dictionary dictionary;

	  /// <summary>
	  /// Creates a new HunspellStemFilterFactory </summary>
	  public HunspellStemFilterFactory(IDictionary<string, string> args) : base(args)
	  {
		dictionaryFiles = require(args, PARAM_DICTIONARY);
		affixFile = get(args, PARAM_AFFIX);
		ignoreCase = getBoolean(args, PARAM_IGNORE_CASE, false);
		longestOnly = getBoolean(args, PARAM_LONGEST_ONLY, false);
		// this isnt necessary: we properly load all dictionaries.
		// but recognize and ignore for back compat
		getBoolean(args, "strictAffixParsing", true);
		// this isn't necessary: multi-stage stripping is fixed and 
		// flags like COMPLEXPREFIXES in the data itself control this.
		// but recognize and ignore for back compat
		getInt(args, "recursionCap", 0);
		if (args.Count > 0)
		{
		  throw new System.ArgumentException("Unknown parameters: " + args);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void inform(org.apache.lucene.analysis.util.ResourceLoader loader) throws java.io.IOException
	  public virtual void inform(ResourceLoader loader)
	  {
		string[] dicts = dictionaryFiles.Split(",", true);

		InputStream affix = null;
		IList<InputStream> dictionaries = new List<InputStream>();

		try
		{
		  dictionaries = new List<>();
		  foreach (string file in dicts)
		  {
			dictionaries.Add(loader.openResource(file));
		  }
		  affix = loader.openResource(affixFile);

		  this.dictionary = new Dictionary(affix, dictionaries, ignoreCase);
		}
		catch (ParseException e)
		{
		  throw new IOException("Unable to load hunspell data! [dictionary=" + dictionaries + ",affix=" + affixFile + "]", e);
		}
		finally
		{
		  IOUtils.closeWhileHandlingException(affix);
		  IOUtils.closeWhileHandlingException(dictionaries);
		}
	  }

	  public override TokenStream create(TokenStream tokenStream)
	  {
		return new HunspellStemFilter(tokenStream, dictionary, true, longestOnly);
	  }
	}

}