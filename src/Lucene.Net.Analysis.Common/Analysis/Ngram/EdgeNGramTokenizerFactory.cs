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
	/// Creates new instances of <seealso cref="EdgeNGramTokenizer"/>.
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_edgngrm" class="solr.TextField" positionIncrementGap="100"&gt;
	///   &lt;analyzer&gt;
	///     &lt;tokenizer class="solr.EdgeNGramTokenizerFactory" minGramSize="1" maxGramSize="1"/&gt;
	///   &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	public class EdgeNGramTokenizerFactory : TokenizerFactory
	{
	  private readonly int maxGramSize;
	  private readonly int minGramSize;
	  private readonly string side;

	  /// <summary>
	  /// Creates a new EdgeNGramTokenizerFactory </summary>
	  public EdgeNGramTokenizerFactory(IDictionary<string, string> args) : base(args)
	  {
		minGramSize = getInt(args, "minGramSize", EdgeNGramTokenizer.DEFAULT_MIN_GRAM_SIZE);
		maxGramSize = getInt(args, "maxGramSize", EdgeNGramTokenizer.DEFAULT_MAX_GRAM_SIZE);
		side = get(args, "side", EdgeNGramTokenFilter.Side.FRONT.Label);
		if (args.Count > 0)
		{
		  throw new System.ArgumentException("Unknown parameters: " + args);
		}
	  }

	  public override Tokenizer create(AttributeFactory factory, Reader input)
	  {
		if (luceneMatchVersion.onOrAfter(Version.LUCENE_44))
		{
		  if (!EdgeNGramTokenFilter.Side.FRONT.Label.Equals(side))
		  {
			throw new System.ArgumentException(typeof(EdgeNGramTokenizer).SimpleName + " does not support backward n-grams as of Lucene 4.4");
		  }
		  return new EdgeNGramTokenizer(luceneMatchVersion, input, minGramSize, maxGramSize);
		}
		else
		{
		  return new Lucene43EdgeNGramTokenizer(luceneMatchVersion, input, side, minGramSize, maxGramSize);
		}
	  }
	}

}