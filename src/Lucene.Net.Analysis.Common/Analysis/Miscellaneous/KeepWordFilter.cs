using System;
using FilteringTokenFilter = Lucene.Net.Analysis.Util.FilteringTokenFilter;

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

namespace org.apache.lucene.analysis.miscellaneous
{

	using FilteringTokenFilter = FilteringTokenFilter;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// A TokenFilter that only keeps tokens with text contained in the
	/// required words.  This filter behaves like the inverse of StopFilter.
	/// 
	/// @since solr 1.3
	/// </summary>
	public sealed class KeepWordFilter : FilteringTokenFilter
	{
	  private readonly CharArraySet words;
	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));

	  /// @deprecated enablePositionIncrements=false is not supported anymore as of Lucene 4.4. 
	  [Obsolete("enablePositionIncrements=false is not supported anymore as of Lucene 4.4.")]
	  public KeepWordFilter(Version version, bool enablePositionIncrements, TokenStream @in, CharArraySet words) : base(version, enablePositionIncrements, @in)
	  {
		this.words = words;
	  }

	  /// <summary>
	  /// Create a new <seealso cref="KeepWordFilter"/>.
	  /// <para><b>NOTE</b>: The words set passed to this constructor will be directly
	  /// used by this filter and should not be modified.
	  /// </para>
	  /// </summary>
	  /// <param name="version"> the Lucene match version </param>
	  /// <param name="in">      the <seealso cref="TokenStream"/> to consume </param>
	  /// <param name="words">   the words to keep </param>
	  public KeepWordFilter(Version version, TokenStream @in, CharArraySet words) : base(version, @in)
	  {
		this.words = words;
	  }

	  public override bool accept()
	  {
		return words.contains(termAtt.buffer(), 0, termAtt.length());
	  }
	}

}