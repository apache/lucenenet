using System.Collections.Generic;

namespace org.apache.lucene.analysis.no
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

	using TokenFilterFactory = org.apache.lucene.analysis.util.TokenFilterFactory;

//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.apache.lucene.analysis.no.NorwegianLightStemmer.BOKMAAL;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.apache.lucene.analysis.no.NorwegianLightStemmer.NYNORSK;

	/// <summary>
	/// Factory for <seealso cref="NorwegianMinimalStemFilter"/>.
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_svlgtstem" class="solr.TextField" positionIncrementGap="100"&gt;
	///   &lt;analyzer&gt;
	///     &lt;tokenizer class="solr.StandardTokenizerFactory"/&gt;
	///     &lt;filter class="solr.LowerCaseFilterFactory"/&gt;
	///     &lt;filter class="solr.NorwegianMinimalStemFilterFactory" variant="nb"/&gt;
	///   &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	public class NorwegianMinimalStemFilterFactory : TokenFilterFactory
	{

	  private readonly int flags;

	  /// <summary>
	  /// Creates a new NorwegianMinimalStemFilterFactory </summary>
	  public NorwegianMinimalStemFilterFactory(IDictionary<string, string> args) : base(args)
	  {
		string variant = get(args, "variant");
		if (variant == null || "nb".Equals(variant))
		{
		  flags = BOKMAAL;
		}
		else if ("nn".Equals(variant))
		{
		  flags = NYNORSK;
		}
		else if ("no".Equals(variant))
		{
		  flags = BOKMAAL | NYNORSK;
		}
		else
		{
		  throw new System.ArgumentException("invalid variant: " + variant);
		}
		if (args.Count > 0)
		{
		  throw new System.ArgumentException("Unknown parameters: " + args);
		}
	  }

	  public override TokenStream create(TokenStream input)
	  {
		return new NorwegianMinimalStemFilter(input, flags);
	  }
	}

}