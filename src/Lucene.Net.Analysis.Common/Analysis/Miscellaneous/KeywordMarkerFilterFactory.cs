using System.Collections.Generic;
using TokenFilterFactory = Lucene.Net.Analysis.Util.TokenFilterFactory;

namespace org.apache.lucene.analysis.miscellaneous
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


	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using ResourceLoader = org.apache.lucene.analysis.util.ResourceLoader;
	using ResourceLoaderAware = org.apache.lucene.analysis.util.ResourceLoaderAware;
	using TokenFilterFactory = TokenFilterFactory;

	/// <summary>
	/// Factory for <seealso cref="KeywordMarkerFilter"/>.
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_keyword" class="solr.TextField" positionIncrementGap="100"&gt;
	///   &lt;analyzer&gt;
	///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
	///     &lt;filter class="solr.KeywordMarkerFilterFactory" protected="protectedkeyword.txt" pattern="^.+er$" ignoreCase="false"/&gt;
	///   &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	public class KeywordMarkerFilterFactory : TokenFilterFactory, ResourceLoaderAware
	{
	  public const string PROTECTED_TOKENS = "protected";
	  public const string PATTERN = "pattern";
	  private readonly string wordFiles;
	  private readonly string stringPattern;
	  private readonly bool ignoreCase;
	  private Pattern pattern;
	  private CharArraySet protectedWords;

	  /// <summary>
	  /// Creates a new KeywordMarkerFilterFactory </summary>
	  public KeywordMarkerFilterFactory(IDictionary<string, string> args) : base(args)
	  {
		wordFiles = get(args, PROTECTED_TOKENS);
		stringPattern = get(args, PATTERN);
		ignoreCase = getBoolean(args, "ignoreCase", false);
		if (args.Count > 0)
		{
		  throw new System.ArgumentException("Unknown parameters: " + args);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void inform(org.apache.lucene.analysis.util.ResourceLoader loader) throws java.io.IOException
	  public virtual void inform(ResourceLoader loader)
	  {
		if (wordFiles != null)
		{
		  protectedWords = getWordSet(loader, wordFiles, ignoreCase);
		}
		if (stringPattern != null)
		{
		  pattern = ignoreCase ? Pattern.compile(stringPattern, Pattern.CASE_INSENSITIVE | Pattern.UNICODE_CASE) : Pattern.compile(stringPattern);
		}
	  }

	  public virtual bool IgnoreCase
	  {
		  get
		  {
			return ignoreCase;
		  }
	  }

	  public override TokenStream create(TokenStream input)
	  {
		if (pattern != null)
		{
		  input = new PatternKeywordMarkerFilter(input, pattern);
		}
		if (protectedWords != null)
		{
		  input = new SetKeywordMarkerFilter(input, protectedWords);
		}
		return input;
	  }
	}

}