using System.Text;

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

namespace org.apache.lucene.analysis.pattern
{

	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using OffsetAttribute = org.apache.lucene.analysis.tokenattributes.OffsetAttribute;

	/// <summary>
	/// This tokenizer uses regex pattern matching to construct distinct tokens
	/// for the input stream.  It takes two arguments:  "pattern" and "group".
	/// <p/>
	/// <ul>
	/// <li>"pattern" is the regular expression.</li>
	/// <li>"group" says which group to extract into tokens.</li>
	///  </ul>
	/// <para>
	/// group=-1 (the default) is equivalent to "split".  In this case, the tokens will
	/// be equivalent to the output from (without empty tokens):
	/// <seealso cref="String#split(java.lang.String)"/>
	/// </para>
	/// <para>
	/// Using group >= 0 selects the matching group as the token.  For example, if you have:<br/>
	/// <pre>
	///  pattern = \'([^\']+)\'
	///  group = 0
	///  input = aaa 'bbb' 'ccc'
	/// </pre>
	/// the output will be two tokens: 'bbb' and 'ccc' (including the ' marks).  With the same input
	/// but using group=1, the output would be: bbb and ccc (no ' marks)
	/// </para>
	/// <para>NOTE: This Tokenizer does not output tokens that are of zero length.</para>
	/// </summary>
	/// <seealso cref= Pattern </seealso>
	public sealed class PatternTokenizer : Tokenizer
	{

	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly OffsetAttribute offsetAtt = addAttribute(typeof(OffsetAttribute));

	  private readonly StringBuilder str = new StringBuilder();
	  private int index;

	  private readonly int group;
	  private readonly Matcher matcher;

	  /// <summary>
	  /// creates a new PatternTokenizer returning tokens from group (-1 for split functionality) </summary>
	  public PatternTokenizer(Reader input, Pattern pattern, int group) : this(AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, input, pattern, group)
	  {
	  }

	  /// <summary>
	  /// creates a new PatternTokenizer returning tokens from group (-1 for split functionality) </summary>
	  public PatternTokenizer(AttributeFactory factory, Reader input, Pattern pattern, int group) : base(factory, input)
	  {
		this.group = group;

		// Use "" instead of str so don't consume chars
		// (fillBuffer) from the input on throwing IAE below:
		matcher = pattern.matcher("");

		// confusingly group count depends ENTIRELY on the pattern but is only accessible via matcher
		if (group >= 0 && group > matcher.groupCount())
		{
		  throw new System.ArgumentException("invalid group specified: pattern only has: " + matcher.groupCount() + " capturing groups");
		}
	  }

	  public override bool incrementToken()
	  {
		if (index >= str.Length)
		{
			return false;
		}
		clearAttributes();
		if (group >= 0)
		{

		  // match a specific group
		  while (matcher.find())
		  {
			index = matcher.start(group);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int endIndex = matcher.end(group);
			int endIndex = matcher.end(group);
			if (index == endIndex)
			{
				continue;
			}
			termAtt.setEmpty().append(str, index, endIndex);
			offsetAtt.setOffset(correctOffset(index), correctOffset(endIndex));
			return true;
		  }

		  index = int.MaxValue; // mark exhausted
		  return false;

		}
		else
		{

		  // String.split() functionality
		  while (matcher.find())
		  {
			if (matcher.start() - index > 0)
			{
			  // found a non-zero-length token
			  termAtt.setEmpty().append(str, index, matcher.start());
			  offsetAtt.setOffset(correctOffset(index), correctOffset(matcher.start()));
			  index = matcher.end();
			  return true;
			}

			index = matcher.end();
		  }

		  if (str.Length - index == 0)
		  {
			index = int.MaxValue; // mark exhausted
			return false;
		  }

		  termAtt.setEmpty().append(str, index, str.Length);
		  offsetAtt.setOffset(correctOffset(index), correctOffset(str.Length));
		  index = int.MaxValue; // mark exhausted
		  return true;
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void end() throws java.io.IOException
	  public override void end()
	  {
		base.end();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int ofs = correctOffset(str.length());
		int ofs = correctOffset(str.Length);
		offsetAtt.setOffset(ofs, ofs);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void reset() throws java.io.IOException
	  public override void reset()
	  {
		base.reset();
		fillBuffer(str, input);
		matcher.reset(str);
		index = 0;
	  }

	  // TODO: we should see if we can make this tokenizer work without reading
	  // the entire document into RAM, perhaps with Matcher.hitEnd/requireEnd ?
	  internal readonly char[] buffer = new char[8192];
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void fillBuffer(StringBuilder sb, java.io.Reader input) throws java.io.IOException
	  private void fillBuffer(StringBuilder sb, Reader input)
	  {
		int len;
		sb.Length = 0;
		while ((len = input.read(buffer)) > 0)
		{
		  sb.Append(buffer, 0, len);
		}
	  }
	}

}