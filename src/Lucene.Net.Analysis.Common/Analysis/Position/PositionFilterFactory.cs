using System;
using System.Collections.Generic;
using TokenFilterFactory = Lucene.Net.Analysis.Util.TokenFilterFactory;

namespace org.apache.lucene.analysis.position
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
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// Factory for <seealso cref="PositionFilter"/>.
	/// Set the positionIncrement of all tokens to the "positionIncrement", except the first return token which retains its
	/// original positionIncrement value. The default positionIncrement value is zero.
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_position" class="solr.TextField" positionIncrementGap="100"&gt;
	///   &lt;analyzer&gt;
	///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
	///     &lt;filter class="solr.PositionFilterFactory" positionIncrement="0"/&gt;
	///   &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	/// <seealso cref= org.apache.lucene.analysis.position.PositionFilter
	/// @since solr 1.4 </seealso>
	/// @deprecated (4.4) 
	[Obsolete("(4.4)")]
	public class PositionFilterFactory : TokenFilterFactory
	{
	  private readonly int positionIncrement;

	  /// <summary>
	  /// Creates a new PositionFilterFactory </summary>
	  public PositionFilterFactory(IDictionary<string, string> args) : base(args)
	  {
		positionIncrement = getInt(args, "positionIncrement", 0);
		if (args.Count > 0)
		{
		  throw new System.ArgumentException("Unknown parameters: " + args);
		}
		if (luceneMatchVersion != null && luceneMatchVersion.onOrAfter(Version.LUCENE_44))
		{
		  throw new System.ArgumentException("PositionFilter is deprecated as of Lucene 4.4. You should either fix your code to not use it or use Lucene 4.3 version compatibility");
		}
	  }

	  public override PositionFilter create(TokenStream input)
	  {
		return new PositionFilter(input, positionIncrement);
	  }
	}


}