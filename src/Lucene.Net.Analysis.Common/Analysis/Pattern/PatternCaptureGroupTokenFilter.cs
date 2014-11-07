using System.Diagnostics;

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

	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;
	using CharsRef = org.apache.lucene.util.CharsRef;

	/// <summary>
	/// CaptureGroup uses Java regexes to emit multiple tokens - one for each capture
	/// group in one or more patterns.
	/// 
	/// <para>
	/// For example, a pattern like:
	/// </para>
	/// 
	/// <para>
	/// <code>"(https?://([a-zA-Z\-_0-9.]+))"</code>
	/// </para>
	/// 
	/// <para>
	/// when matched against the string "http://www.foo.com/index" would return the
	/// tokens "https://www.foo.com" and "www.foo.com".
	/// </para>
	/// 
	/// <para>
	/// If none of the patterns match, or if preserveOriginal is true, the original
	/// token will be preserved.
	/// </para>
	/// <para>
	/// Each pattern is matched as often as it can be, so the pattern
	/// <code> "(...)"</code>, when matched against <code>"abcdefghi"</code> would
	/// produce <code>["abc","def","ghi"]</code>
	/// </para>
	/// <para>
	/// A camelCaseFilter could be written as:
	/// </para>
	/// <para>
	/// <code>
	///   "([A-Z]{2,})",                                 <br />
	///   "(?&lt;![A-Z])([A-Z][a-z]+)",                     <br />
	///   "(?:^|\\b|(?&lt;=[0-9_])|(?&lt;=[A-Z]{2}))([a-z]+)", <br />
	///   "([0-9]+)"
	/// </code>
	/// </para>
	/// <para>
	/// plus if <seealso cref="#preserveOriginal"/> is true, it would also return
	/// <code>"camelCaseFilter</code>
	/// </para>
	/// </summary>
	public sealed class PatternCaptureGroupTokenFilter : TokenFilter
	{

	  private readonly CharTermAttribute charTermAttr = addAttribute(typeof(CharTermAttribute));
	  private readonly PositionIncrementAttribute posAttr = addAttribute(typeof(PositionIncrementAttribute));
	  private State state;
	  private readonly Matcher[] matchers;
	  private readonly CharsRef spare = new CharsRef();
	  private readonly int[] groupCounts;
	  private readonly bool preserveOriginal;
	  private int[] currentGroup;
	  private int currentMatcher;

	  /// <param name="input">
	  ///          the input <seealso cref="TokenStream"/> </param>
	  /// <param name="preserveOriginal">
	  ///          set to true to return the original token even if one of the
	  ///          patterns matches </param>
	  /// <param name="patterns">
	  ///          an array of <seealso cref="Pattern"/> objects to match against each token </param>

	  public PatternCaptureGroupTokenFilter(TokenStream input, bool preserveOriginal, params Pattern[] patterns) : base(input)
	  {
		this.preserveOriginal = preserveOriginal;
		this.matchers = new Matcher[patterns.Length];
		this.groupCounts = new int[patterns.Length];
		this.currentGroup = new int[patterns.Length];
		for (int i = 0; i < patterns.Length; i++)
		{
		  this.matchers[i] = patterns[i].matcher("");
		  this.groupCounts[i] = this.matchers[i].groupCount();
		  this.currentGroup[i] = -1;
		}
	  }

	  private bool nextCapture()
	  {
		int min_offset = int.MaxValue;
		currentMatcher = -1;
		Matcher matcher;

		for (int i = 0; i < matchers.Length; i++)
		{
		  matcher = matchers[i];
		  if (currentGroup[i] == -1)
		  {
			currentGroup[i] = matcher.find() ? 1 : 0;
		  }
		  if (currentGroup[i] != 0)
		  {
			while (currentGroup[i] < groupCounts[i] + 1)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int start = matcher.start(currentGroup[i]);
			  int start = matcher.start(currentGroup[i]);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int end = matcher.end(currentGroup[i]);
			  int end = matcher.end(currentGroup[i]);
			  if (start == end || preserveOriginal && start == 0 && spare.length == end)
			  {
				currentGroup[i]++;
				continue;
			  }
			  if (start < min_offset)
			  {
				min_offset = start;
				currentMatcher = i;
			  }
			  break;
			}
			if (currentGroup[i] == groupCounts[i] + 1)
			{
			  currentGroup[i] = -1;
			  i--;
			}
		  }
		}
		return currentMatcher != -1;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {

		if (currentMatcher != -1 && nextCapture())
		{
		  Debug.Assert(state != null);
		  clearAttributes();
		  restoreState(state);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int start = matchers[currentMatcher].start(currentGroup[currentMatcher]);
		  int start = matchers[currentMatcher].start(currentGroup[currentMatcher]);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int end = matchers[currentMatcher].end(currentGroup[currentMatcher]);
		  int end = matchers[currentMatcher].end(currentGroup[currentMatcher]);

		  posAttr.PositionIncrement = 0;
		  charTermAttr.copyBuffer(spare.chars, start, end - start);
		  currentGroup[currentMatcher]++;
		  return true;
		}

		if (!input.incrementToken())
		{
		  return false;
		}

		char[] buffer = charTermAttr.buffer();
		int length = charTermAttr.length();
		spare.copyChars(buffer, 0, length);
		state = captureState();

		for (int i = 0; i < matchers.Length; i++)
		{
		  matchers[i].reset(spare);
		  currentGroup[i] = -1;
		}

		if (preserveOriginal)
		{
		  currentMatcher = 0;
		}
		else if (nextCapture())
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int start = matchers[currentMatcher].start(currentGroup[currentMatcher]);
		  int start = matchers[currentMatcher].start(currentGroup[currentMatcher]);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int end = matchers[currentMatcher].end(currentGroup[currentMatcher]);
		  int end = matchers[currentMatcher].end(currentGroup[currentMatcher]);

		  // if we start at 0 we can simply set the length and save the copy
		  if (start == 0)
		  {
			charTermAttr.Length = end;
		  }
		  else
		  {
			charTermAttr.copyBuffer(spare.chars, start, end - start);
		  }
		  currentGroup[currentMatcher]++;
		}
		return true;

	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void reset() throws java.io.IOException
	  public override void reset()
	  {
		base.reset();
		state = null;
		currentMatcher = -1;
	  }

	}

}