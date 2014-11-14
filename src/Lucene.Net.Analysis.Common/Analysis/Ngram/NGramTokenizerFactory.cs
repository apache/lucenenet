using System.Collections.Generic;
using TokenizerFactory = Lucene.Net.Analysis.Util.TokenizerFactory;

namespace org.apache.lucene.analysis.ngram
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

	using TokenizerFactory = TokenizerFactory;
	using AttributeFactory = org.apache.lucene.util.AttributeSource.AttributeFactory;
	using Version = org.apache.lucene.util.Version;


	/// <summary>
	/// Factory for <seealso cref="NGramTokenizer"/>.
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_ngrm" class="solr.TextField" positionIncrementGap="100"&gt;
	///   &lt;analyzer&gt;
	///     &lt;tokenizer class="solr.NGramTokenizerFactory" minGramSize="1" maxGramSize="2"/&gt;
	///   &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	public class NGramTokenizerFactory : TokenizerFactory
	{
	  private readonly int maxGramSize;
	  private readonly int minGramSize;

	  /// <summary>
	  /// Creates a new NGramTokenizerFactory </summary>
	  public NGramTokenizerFactory(IDictionary<string, string> args) : base(args)
	  {
		minGramSize = getInt(args, "minGramSize", NGramTokenizer.DEFAULT_MIN_NGRAM_SIZE);
		maxGramSize = getInt(args, "maxGramSize", NGramTokenizer.DEFAULT_MAX_NGRAM_SIZE);
		if (args.Count > 0)
		{
		  throw new System.ArgumentException("Unknown parameters: " + args);
		}
	  }

	  /// <summary>
	  /// Creates the <seealso cref="TokenStream"/> of n-grams from the given <seealso cref="Reader"/> and <seealso cref="AttributeFactory"/>. </summary>
	  public override Tokenizer create(AttributeFactory factory, Reader input)
	  {
		if (luceneMatchVersion.onOrAfter(Version.LUCENE_44))
		{
		  return new NGramTokenizer(luceneMatchVersion, factory, input, minGramSize, maxGramSize);
		}
		else
		{
		  return new Lucene43NGramTokenizer(factory, input, minGramSize, maxGramSize);
		}
	  }
	}

}