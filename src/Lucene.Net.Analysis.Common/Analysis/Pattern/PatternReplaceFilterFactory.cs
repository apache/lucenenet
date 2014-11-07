using System.Collections.Generic;
using TokenFilterFactory = Lucene.Net.Analysis.Util.TokenFilterFactory;

namespace org.apache.lucene.analysis.pattern
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

	using TokenFilterFactory = TokenFilterFactory;


	/// <summary>
	/// Factory for <seealso cref="PatternReplaceFilter"/>. 
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_ptnreplace" class="solr.TextField" positionIncrementGap="100"&gt;
	///   &lt;analyzer&gt;
	///     &lt;tokenizer class="solr.KeywordTokenizerFactory"/&gt;
	///     &lt;filter class="solr.PatternReplaceFilterFactory" pattern="([^a-z])" replacement=""
	///             replace="all"/&gt;
	///   &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	/// <seealso cref= PatternReplaceFilter </seealso>
	public class PatternReplaceFilterFactory : TokenFilterFactory
	{
	  internal readonly Pattern pattern;
	  internal readonly string replacement;
	  internal readonly bool replaceAll;

	  /// <summary>
	  /// Creates a new PatternReplaceFilterFactory </summary>
	  public PatternReplaceFilterFactory(IDictionary<string, string> args) : base(args)
	  {
		pattern = getPattern(args, "pattern");
		replacement = get(args, "replacement");
		replaceAll = "all".Equals(get(args, "replace", Arrays.asList("all", "first"), "all"));
		if (args.Count > 0)
		{
		  throw new System.ArgumentException("Unknown parameters: " + args);
		}
	  }

	  public override PatternReplaceFilter create(TokenStream input)
	  {
		return new PatternReplaceFilter(input, pattern, replacement, replaceAll);
	  }
	}

}