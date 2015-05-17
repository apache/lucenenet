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
using System;
using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Analysis.Miscellaneous
{
    /// <summary>
	/// Trims leading and trailing whitespace from Tokens in the stream.
	/// <para>As of Lucene 4.4, this filter does not support updateOffsets=true anymore
	/// as it can lead to broken token streams.
	/// </para>
	/// </summary>
	public sealed class TrimFilter : TokenFilter
	{

	  internal readonly bool updateOffsets;
	  private readonly ICharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly IOffsetAttribute offsetAtt = addAttribute(typeof(OffsetAttribute));

	  /// <summary>
	  /// Create a new <seealso cref="TrimFilter"/>. </summary>
	  /// <param name="version">       the Lucene match version </param>
	  /// <param name="in">            the stream to consume </param>
	  /// <param name="updateOffsets"> whether to update offsets </param>
	  /// @deprecated Offset updates are not supported anymore as of Lucene 4.4. 
	  [Obsolete("Offset updates are not supported anymore as of Lucene 4.4.")]
	  public TrimFilter(Version version, TokenStream @in, bool updateOffsets) : base(@in)
	  {
		if (updateOffsets && version.onOrAfter(Version.LUCENE_44))
		{
		  throw new System.ArgumentException("updateOffsets=true is not supported anymore as of Lucene 4.4");
		}
		this.updateOffsets = updateOffsets;
	  }

	  /// <summary>
	  /// Create a new <seealso cref="TrimFilter"/> on top of <code>in</code>. </summary>
	  public TrimFilter(Version version, TokenStream @in) : this(version, @in, false)
	  {
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		if (!input.incrementToken())
		{
			return false;
		}

		char[] termBuffer = termAtt.buffer();
		int len = termAtt.length();
		//TODO: Is this the right behavior or should we return false?  Currently, "  ", returns true, so I think this should
		//also return true
		if (len == 0)
		{
		  return true;
		}
		int start = 0;
		int end = 0;
		int endOff = 0;

		// eat the first characters
		for (start = 0; start < len && char.IsWhiteSpace(termBuffer[start]); start++)
		{
		}
		// eat the end characters
		for (end = len; end >= start && char.IsWhiteSpace(termBuffer[end - 1]); end--)
		{
		  endOff++;
		}
		if (start > 0 || end < len)
		{
		  if (start < end)
		  {
			termAtt.copyBuffer(termBuffer, start, (end - start));
		  }
		  else
		  {
			termAtt.setEmpty();
		  }
		  if (updateOffsets && len == offsetAtt.endOffset() - offsetAtt.startOffset())
		  {
			int newStart = offsetAtt.startOffset() + start;
			int newEnd = offsetAtt.endOffset() - (start < end ? endOff:0);
			offsetAtt.setOffset(newStart, newEnd);
		  }
		}

		return true;
	  }
	}

}