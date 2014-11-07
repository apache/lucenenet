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


	using StemmerOverrideMap = org.apache.lucene.analysis.miscellaneous.StemmerOverrideFilter.StemmerOverrideMap;
	using ResourceLoader = org.apache.lucene.analysis.util.ResourceLoader;
	using ResourceLoaderAware = org.apache.lucene.analysis.util.ResourceLoaderAware;
	using TokenFilterFactory = TokenFilterFactory;

	/// <summary>
	/// Factory for <seealso cref="StemmerOverrideFilter"/>.
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_dicstem" class="solr.TextField" positionIncrementGap="100"&gt;
	///   &lt;analyzer&gt;
	///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
	///     &lt;filter class="solr.StemmerOverrideFilterFactory" dictionary="dictionary.txt" ignoreCase="false"/&gt;
	///   &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	public class StemmerOverrideFilterFactory : TokenFilterFactory, ResourceLoaderAware
	{
	  private StemmerOverrideMap dictionary;
	  private readonly string dictionaryFiles;
	  private readonly bool ignoreCase;

	  /// <summary>
	  /// Creates a new StemmerOverrideFilterFactory </summary>
	  public StemmerOverrideFilterFactory(IDictionary<string, string> args) : base(args)
	  {
		dictionaryFiles = get(args, "dictionary");
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
		if (dictionaryFiles != null)
		{
		  assureMatchVersion();
		  IList<string> files = splitFileNames(dictionaryFiles);
		  if (files.Count > 0)
		  {
			StemmerOverrideFilter.Builder builder = new StemmerOverrideFilter.Builder(ignoreCase);
			foreach (string file in files)
			{
			  IList<string> list = getLines(loader, file.Trim());
			  foreach (string line in list)
			  {
				string[] mapping = line.Split("\t", 2);
				builder.add(mapping[0], mapping[1]);
			  }
			}
			dictionary = builder.build();
		  }
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
		return dictionary == null ? input : new StemmerOverrideFilter(input, dictionary);
	  }
	}

}