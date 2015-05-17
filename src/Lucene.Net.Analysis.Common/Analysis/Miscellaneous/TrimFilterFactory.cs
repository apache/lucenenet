using System.Collections.Generic;
using Lucene.Net.Analysis.Miscellaneous;
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
	/// Factory for <seealso cref="TrimFilter"/>.
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_trm" class="solr.TextField" positionIncrementGap="100"&gt;
	///   &lt;analyzer&gt;
	///     &lt;tokenizer class="solr.NGramTokenizerFactory"/&gt;
	///     &lt;filter class="solr.TrimFilterFactory" /&gt;
	///   &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	/// <seealso cref= TrimFilter </seealso>
	public class TrimFilterFactory : TokenFilterFactory
	{

	  protected internal readonly bool updateOffsets;

	  /// <summary>
	  /// Creates a new TrimFilterFactory </summary>
	  public TrimFilterFactory(IDictionary<string, string> args) : base(args)
	  {
		updateOffsets = getBoolean(args, "updateOffsets", false);
		if (args.Count > 0)
		{
		  throw new System.ArgumentException("Unknown parameters: " + args);
		}
	  }

	  public override TrimFilter create(TokenStream input)
	  {
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings("deprecation") final org.apache.lucene.analysis.miscellaneous.TrimFilter filter = new org.apache.lucene.analysis.miscellaneous.TrimFilter(luceneMatchVersion, input, updateOffsets);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
		  TrimFilter filter = new TrimFilter(luceneMatchVersion, input, updateOffsets);
		return filter;
	  }
	}

}