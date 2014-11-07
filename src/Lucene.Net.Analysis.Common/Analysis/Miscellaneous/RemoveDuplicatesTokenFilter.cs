using System;

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

	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// A TokenFilter which filters out Tokens at the same position and Term text as the previous token in the stream.
	/// </summary>
	public sealed class RemoveDuplicatesTokenFilter : TokenFilter
	{

	  private readonly CharTermAttribute termAttribute = addAttribute(typeof(CharTermAttribute));
	  private readonly PositionIncrementAttribute posIncAttribute = addAttribute(typeof(PositionIncrementAttribute));

	  // use a fixed version, as we don't care about case sensitivity.
	  private readonly CharArraySet previous = new CharArraySet(Version.LUCENE_31, 8, false);

	  /// <summary>
	  /// Creates a new RemoveDuplicatesTokenFilter
	  /// </summary>
	  /// <param name="in"> TokenStream that will be filtered </param>
	  public RemoveDuplicatesTokenFilter(TokenStream @in) : base(@in)
	  {
	  }

	  /// <summary>
	  /// {@inheritDoc}
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		while (input.incrementToken())
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final char term[] = termAttribute.buffer();
		  char[] term = termAttribute.buffer();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int length = termAttribute.length();
		  int length = termAttribute.length();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int posIncrement = posIncAttribute.getPositionIncrement();
		  int posIncrement = posIncAttribute.PositionIncrement;

		  if (posIncrement > 0)
		  {
			previous.clear();
		  }

		  bool duplicate = (posIncrement == 0 && previous.contains(term, 0, length));

		  // clone the term, and add to the set of seen terms.
		  char[] saved = new char[length];
		  Array.Copy(term, 0, saved, 0, length);
		  previous.add(saved);

		  if (!duplicate)
		  {
			return true;
		  }
		}
		return false;
	  }

	  /// <summary>
	  /// {@inheritDoc}
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void reset() throws java.io.IOException
	  public override void reset()
	  {
		base.reset();
		previous.clear();
	  }
	}

}