using System;
using System.Collections.Generic;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Util;
using org.apache.lucene.analysis.commongrams;
using org.apache.lucene.analysis.util;

namespace Lucene.Net.Analysis.CommonGrams
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
	/// Constructs a <seealso cref="CommonGramsFilter"/>.
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_cmmngrms" class="solr.TextField" positionIncrementGap="100"&gt;
	///   &lt;analyzer&gt;
	///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
	///     &lt;filter class="solr.CommonGramsFilterFactory" words="commongramsstopwords.txt" ignoreCase="false"/&gt;
	///   &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	public class CommonGramsFilterFactory : TokenFilterFactory, ResourceLoaderAware
	{
	  // TODO: shared base class for Stop/Keep/CommonGrams? 
	  private CharArraySet commonWords;
	  private readonly string commonWordFiles;
	  private readonly string format;
	  private readonly bool ignoreCase;

	  /// <summary>
	  /// Creates a new CommonGramsFilterFactory </summary>
	  public CommonGramsFilterFactory(IDictionary<string, string> args) : base(args)
	  {
		commonWordFiles = get(args, "words");
		format = get(args, "format");
		ignoreCase = getBoolean(args, "ignoreCase", false);
		if (args.Count > 0)
		{
		  throw new System.ArgumentException("Unknown parameters: " + args);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void inform(ResourceLoader loader) throws java.io.IOException
	  public virtual void inform(ResourceLoader loader)
	  {
		if (commonWordFiles != null)
		{
		  if ("snowball".Equals(format, StringComparison.CurrentCultureIgnoreCase))
		  {
			commonWords = getSnowballWordSet(loader, commonWordFiles, ignoreCase);
		  }
		  else
		  {
			commonWords = GetWordSet(loader, commonWordFiles, ignoreCase);
		  }
		}
		else
		{
		  commonWords = StopAnalyzer.ENGLISH_STOP_WORDS_SET;
		}
	  }

	  public virtual bool IgnoreCase
	  {
		  get
		  {
			return ignoreCase;
		  }
	  }

	  public virtual CharArraySet CommonWords
	  {
		  get
		  {
			return commonWords;
		  }
	  }

	  public override TokenFilter Create(TokenStream input)
	  {
		CommonGramsFilter commonGrams = new CommonGramsFilter(luceneMatchVersion, input, commonWords);
		return commonGrams;
	  }
	}



}