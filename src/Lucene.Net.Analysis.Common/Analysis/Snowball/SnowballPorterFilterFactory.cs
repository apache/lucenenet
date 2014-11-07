using System;
using System.Collections.Generic;

namespace org.apache.lucene.analysis.snowball
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


	using SetKeywordMarkerFilter = org.apache.lucene.analysis.miscellaneous.SetKeywordMarkerFilter;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using ResourceLoader = org.apache.lucene.analysis.util.ResourceLoader;
	using ResourceLoaderAware = org.apache.lucene.analysis.util.ResourceLoaderAware;
	using TokenFilterFactory = org.apache.lucene.analysis.util.TokenFilterFactory;
	using SnowballProgram = org.tartarus.snowball.SnowballProgram;

	/// <summary>
	/// Factory for <seealso cref="SnowballFilter"/>, with configurable language
	/// <para>
	/// Note: Use of the "Lovins" stemmer is not recommended, as it is implemented with reflection.
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_snowballstem" class="solr.TextField" positionIncrementGap="100"&gt;
	///   &lt;analyzer&gt;
	///     &lt;tokenizer class="solr.StandardTokenizerFactory"/&gt;
	///     &lt;filter class="solr.LowerCaseFilterFactory"/&gt;
	///     &lt;filter class="solr.SnowballPorterFilterFactory" protected="protectedkeyword.txt" language="English"/&gt;
	///   &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </para>
	/// </summary>
	public class SnowballPorterFilterFactory : TokenFilterFactory, ResourceLoaderAware
	{
	  public const string PROTECTED_TOKENS = "protected";

	  private readonly string language;
	  private readonly string wordFiles;
	  private Type stemClass;
	  private CharArraySet protectedWords = null;

	  /// <summary>
	  /// Creates a new SnowballPorterFilterFactory </summary>
	  public SnowballPorterFilterFactory(IDictionary<string, string> args) : base(args)
	  {
		language = get(args, "language", "English");
		wordFiles = get(args, PROTECTED_TOKENS);
		if (args.Count > 0)
		{
		  throw new System.ArgumentException("Unknown parameters: " + args);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void inform(org.apache.lucene.analysis.util.ResourceLoader loader) throws java.io.IOException
	  public virtual void inform(ResourceLoader loader)
	  {
		string className = "org.tartarus.snowball.ext." + language + "Stemmer";
		stemClass = loader.newInstance(className, typeof(SnowballProgram)).GetType();

		if (wordFiles != null)
		{
		  protectedWords = getWordSet(loader, wordFiles, false);
		}
	  }

	  public override TokenFilter create(TokenStream input)
	  {
		SnowballProgram program;
		try
		{
		  program = stemClass.newInstance();
		}
		catch (Exception e)
		{
		  throw new Exception("Error instantiating stemmer for language " + language + "from class " + stemClass, e);
		}

		if (protectedWords != null)
		{
		  input = new SetKeywordMarkerFilter(input, protectedWords);
		}
		return new SnowballFilter(input, program);
	  }
	}


}