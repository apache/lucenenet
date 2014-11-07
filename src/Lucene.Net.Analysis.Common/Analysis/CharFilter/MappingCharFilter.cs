using System;
using System.Diagnostics;
using System.Collections.Generic;
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
using Lucene.Net.Analysis.CharFilter;

namespace org.apache.lucene.analysis.charfilter
{


	using RollingCharBuffer = org.apache.lucene.analysis.util.RollingCharBuffer;
	using CharsRef = org.apache.lucene.util.CharsRef;
	using CharSequenceOutputs = org.apache.lucene.util.fst.CharSequenceOutputs;
	using FST = org.apache.lucene.util.fst.FST;
	using Outputs = org.apache.lucene.util.fst.Outputs;

	/// <summary>
	/// Simplistic <seealso cref="CharFilter"/> that applies the mappings
	/// contained in a <seealso cref="NormalizeCharMap"/> to the character
	/// stream, and correcting the resulting changes to the
	/// offsets.  Matching is greedy (longest pattern matching at
	/// a given point wins).  Replacement is allowed to be the
	/// empty string.
	/// </summary>

	public class MappingCharFilter : BaseCharFilter
	{

	  private readonly Outputs<CharsRef> outputs = CharSequenceOutputs.Singleton;
	  private readonly FST<CharsRef> map;
	  private readonly FST.BytesReader fstReader;
	  private readonly RollingCharBuffer buffer = new RollingCharBuffer();
	  private readonly FST.Arc<CharsRef> scratchArc = new FST.Arc<CharsRef>();
	  private readonly IDictionary<char?, FST.Arc<CharsRef>> cachedRootArcs;

	  private CharsRef replacement;
	  private int replacementPointer;
	  private int inputOff;

	  /// <summary>
	  /// Default constructor that takes a <seealso cref="Reader"/>. </summary>
	  public MappingCharFilter(NormalizeCharMap normMap, Reader @in) : base(@in)
	  {
		buffer.reset(@in);

		map = normMap.map;
		cachedRootArcs = normMap.cachedRootArcs;

		if (map != null)
		{
		  fstReader = map.BytesReader;
		}
		else
		{
		  fstReader = null;
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void reset() throws java.io.IOException
	  public override void reset()
	  {
		input.reset();
		buffer.reset(input);
		replacement = null;
		inputOff = 0;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int read() throws java.io.IOException
	  public override int read()
	  {

		//System.out.println("\nread");
		while (true)
		{

		  if (replacement != null && replacementPointer < replacement.length)
		  {
			//System.out.println("  return repl[" + replacementPointer + "]=" + replacement.chars[replacement.offset + replacementPointer]);
			return replacement.chars[replacement.offset + replacementPointer++];
		  }

		  // TODO: a more efficient approach would be Aho/Corasick's
		  // algorithm
		  // (http://en.wikipedia.org/wiki/Aho%E2%80%93Corasick_string_matching_algorithm)
		  // or this generalizatio: www.cis.uni-muenchen.de/people/Schulz/Pub/dictle5.ps
		  //
		  // I think this would be (almost?) equivalent to 1) adding
		  // epsilon arcs from all final nodes back to the init
		  // node in the FST, 2) adding a .* (skip any char)
		  // loop on the initial node, and 3) determinizing
		  // that.  Then we would not have to restart matching
		  // at each position.

		  int lastMatchLen = -1;
		  CharsRef lastMatch = null;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int firstCH = buffer.get(inputOff);
		  int firstCH = buffer.get(inputOff);
		  if (firstCH != -1)
		  {
			FST.Arc<CharsRef> arc = cachedRootArcs[Convert.ToChar((char) firstCH)];
			if (arc != null)
			{
			  if (!FST.targetHasArcs(arc))
			  {
				// Fast pass for single character match:
				Debug.Assert(arc.Final);
				lastMatchLen = 1;
				lastMatch = arc.output;
			  }
			  else
			  {
				int lookahead = 0;
				CharsRef output = arc.output;
				while (true)
				{
				  lookahead++;

				  if (arc.Final)
				  {
					// Match! (to node is final)
					lastMatchLen = lookahead;
					lastMatch = outputs.add(output, arc.nextFinalOutput);
					// Greedy: keep searching to see if there's a
					// longer match...
				  }

				  if (!FST.targetHasArcs(arc))
				  {
					break;
				  }

				  int ch = buffer.get(inputOff + lookahead);
				  if (ch == -1)
				  {
					break;
				  }
				  if ((arc = map.findTargetArc(ch, arc, scratchArc, fstReader)) == null)
				  {
					// Dead end
					break;
				  }
				  output = outputs.add(output, arc.output);
				}
			  }
			}
		  }

		  if (lastMatch != null)
		  {
			inputOff += lastMatchLen;
			//System.out.println("  match!  len=" + lastMatchLen + " repl=" + lastMatch);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int diff = lastMatchLen - lastMatch.length;
			int diff = lastMatchLen - lastMatch.length;

			if (diff != 0)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int prevCumulativeDiff = getLastCumulativeDiff();
			  int prevCumulativeDiff = LastCumulativeDiff;
			  if (diff > 0)
			  {
				// Replacement is shorter than matched input:
				addOffCorrectMap(inputOff - diff - prevCumulativeDiff, prevCumulativeDiff + diff);
			  }
			  else
			  {
				// Replacement is longer than matched input: remap
				// the "extra" chars all back to the same input
				// offset:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int outputStart = inputOff - prevCumulativeDiff;
				int outputStart = inputOff - prevCumulativeDiff;
				for (int extraIDX = 0;extraIDX < -diff;extraIDX++)
				{
				  addOffCorrectMap(outputStart + extraIDX, prevCumulativeDiff - extraIDX - 1);
				}
			  }
			}

			replacement = lastMatch;
			replacementPointer = 0;

		  }
		  else
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int ret = buffer.get(inputOff);
			int ret = buffer.get(inputOff);
			if (ret != -1)
			{
			  inputOff++;
			  buffer.freeBefore(inputOff);
			}
			return ret;
		  }
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int read(char[] cbuf, int off, int len) throws java.io.IOException
	  public override int read(char[] cbuf, int off, int len)
	  {
		int numRead = 0;
		for (int i = off; i < off + len; i++)
		{
		  int c = read();
		  if (c == -1)
		  {
			  break;
		  }
		  cbuf[i] = (char) c;
		  numRead++;
		}

		return numRead == 0 ? - 1 : numRead;
	  }
	}

}