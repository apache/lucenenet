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

	using TokenFilterFactory = TokenFilterFactory;

	/// <summary>
	/// Factory for <seealso cref="LimitTokenPositionFilter"/>. 
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_limit_pos" class="solr.TextField" positionIncrementGap="100"&gt;
	///   &lt;analyzer&gt;
	///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
	///     &lt;filter class="solr.LimitTokenPositionFilterFactory" maxTokenPosition="3" consumeAllTokens="false" /&gt;
	///   &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// <para>
	/// The {@code consumeAllTokens} property is optional and defaults to {@code false}.  
	/// See <seealso cref="LimitTokenPositionFilter"/> for an explanation of its use.
	/// </para>
	/// </summary>
	public class LimitTokenPositionFilterFactory : TokenFilterFactory
	{

	  public const string MAX_TOKEN_POSITION_KEY = "maxTokenPosition";
	  public const string CONSUME_ALL_TOKENS_KEY = "consumeAllTokens";
	  internal readonly int maxTokenPosition;
	  internal readonly bool consumeAllTokens;

	  /// <summary>
	  /// Creates a new LimitTokenPositionFilterFactory </summary>
	  public LimitTokenPositionFilterFactory(IDictionary<string, string> args) : base(args)
	  {
		maxTokenPosition = requireInt(args, MAX_TOKEN_POSITION_KEY);
		consumeAllTokens = getBoolean(args, CONSUME_ALL_TOKENS_KEY, false);
		if (args.Count > 0)
		{
		  throw new System.ArgumentException("Unknown parameters: " + args);
		}
	  }

	  public override TokenStream create(TokenStream input)
	  {
		return new LimitTokenPositionFilter(input, maxTokenPosition, consumeAllTokens);
	  }

	}

}