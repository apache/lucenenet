using System;
using System.Collections.Generic;

namespace org.apache.lucene.analysis.synonym
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

	using OffsetAttribute = org.apache.lucene.analysis.tokenattributes.OffsetAttribute;
	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using TypeAttribute = org.apache.lucene.analysis.tokenattributes.TypeAttribute;
	using AttributeSource = org.apache.lucene.util.AttributeSource;


	/// <summary>
	/// SynonymFilter handles multi-token synonyms with variable position increment offsets.
	/// <para>
	/// The matched tokens from the input stream may be optionally passed through (includeOrig=true)
	/// or discarded.  If the original tokens are included, the position increments may be modified
	/// to retain absolute positions after merging with the synonym tokenstream.
	/// </para>
	/// <para>
	/// Generated synonyms will start at the same position as the first matched source token.
	/// </para>
	/// </summary>
	/// @deprecated (3.4) use <seealso cref="SynonymFilterFactory"/> instead. only for precise index backwards compatibility. this factory will be removed in Lucene 5.0 
	[Obsolete("(3.4) use <seealso cref="SynonymFilterFactory"/> instead. only for precise index backwards compatibility. this factory will be removed in Lucene 5.0")]
	internal sealed class SlowSynonymFilter : TokenFilter
	{

	  private readonly SlowSynonymMap map; // Map<String, SynonymMap>
	  private IEnumerator<AttributeSource> replacement; // iterator over generated tokens

	  public SlowSynonymFilter(TokenStream @in, SlowSynonymMap map) : base(@in)
	  {
		if (map == null)
		{
		  throw new System.ArgumentException("map is required");
		}

		this.map = map;
		// just ensuring these attributes exist...
		addAttribute(typeof(CharTermAttribute));
		addAttribute(typeof(PositionIncrementAttribute));
		addAttribute(typeof(OffsetAttribute));
		addAttribute(typeof(TypeAttribute));
	  }


	  /*
	   * Need to worry about multiple scenarios:
	   *  - need to go for the longest match
	   *    a b => foo      #shouldn't match if "a b" is followed by "c d"
	   *    a b c d => bar
	   *  - need to backtrack - retry matches for tokens already read
	   *     a b c d => foo
	   *       b c => bar
	   *     If the input stream is "a b c x", one will consume "a b c d"
	   *     trying to match the first rule... all but "a" should be
	   *     pushed back so a match may be made on "b c".
	   *  - don't try and match generated tokens (thus need separate queue)
	   *    matching is not recursive.
	   *  - handle optional generation of original tokens in all these cases,
	   *    merging token streams to preserve token positions.
	   *  - preserve original positionIncrement of first matched token
	   */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		while (true)
		{
		  // if there are any generated tokens, return them... don't try any
		  // matches against them, as we specifically don't want recursion.
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  if (replacement != null && replacement.hasNext())
		  {
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
			copy(this, replacement.next());
			return true;
		  }

		  // common case fast-path of first token not matching anything
		  AttributeSource firstTok = nextTok();
		  if (firstTok == null)
		  {
			  return false;
		  }
		  CharTermAttribute termAtt = firstTok.addAttribute(typeof(CharTermAttribute));
		  SlowSynonymMap result = map.submap != null ? map.submap.get(termAtt.buffer(), 0, termAtt.length()) : null;
		  if (result == null)
		  {
			copy(this, firstTok);
			return true;
		  }

		  // fast-path failed, clone ourselves if needed
		  if (firstTok == this)
		  {
			firstTok = cloneAttributes();
		  }
		  // OK, we matched a token, so find the longest match.

		  matched = new LinkedList<>();

		  result = match(result);

		  if (result == null)
		  {
			// no match, simply return the first token read.
			copy(this, firstTok);
			return true;
		  }

		  // reuse, or create new one each time?
		  List<AttributeSource> generated = new List<AttributeSource>(result.synonyms.Length + matched.Count + 1);

		  //
		  // there was a match... let's generate the new tokens, merging
		  // in the matched tokens (position increments need adjusting)
		  //
		  AttributeSource lastTok = matched.Count == 0 ? firstTok : matched.Last.Value;
		  bool includeOrig = result.includeOrig();

		  AttributeSource origTok = includeOrig ? firstTok : null;
		  PositionIncrementAttribute firstPosIncAtt = firstTok.addAttribute(typeof(PositionIncrementAttribute));
		  int origPos = firstPosIncAtt.PositionIncrement; // position of origTok in the original stream
		  int repPos = 0; // curr position in replacement token stream
		  int pos = 0; // current position in merged token stream

		  for (int i = 0; i < result.synonyms.Length; i++)
		  {
			Token repTok = result.synonyms[i];
			AttributeSource newTok = firstTok.cloneAttributes();
			CharTermAttribute newTermAtt = newTok.addAttribute(typeof(CharTermAttribute));
			OffsetAttribute newOffsetAtt = newTok.addAttribute(typeof(OffsetAttribute));
			PositionIncrementAttribute newPosIncAtt = newTok.addAttribute(typeof(PositionIncrementAttribute));

			OffsetAttribute lastOffsetAtt = lastTok.addAttribute(typeof(OffsetAttribute));

			newOffsetAtt.setOffset(newOffsetAtt.startOffset(), lastOffsetAtt.endOffset());
			newTermAtt.copyBuffer(repTok.buffer(), 0, repTok.length());
			repPos += repTok.PositionIncrement;
			if (i == 0) // make position of first token equal to original
			{
				repPos = origPos;
			}

			// if necessary, insert original tokens and adjust position increment
			while (origTok != null && origPos <= repPos)
			{
			  PositionIncrementAttribute origPosInc = origTok.addAttribute(typeof(PositionIncrementAttribute));
			  origPosInc.PositionIncrement = origPos - pos;
			  generated.Add(origTok);
			  pos += origPosInc.PositionIncrement;
			  origTok = matched.Count == 0 ? null : matched.RemoveFirst();
			  if (origTok != null)
			  {
				origPosInc = origTok.addAttribute(typeof(PositionIncrementAttribute));
				origPos += origPosInc.PositionIncrement;
			  }
			}

			newPosIncAtt.PositionIncrement = repPos - pos;
			generated.Add(newTok);
			pos += newPosIncAtt.PositionIncrement;
		  }

		  // finish up any leftover original tokens
		  while (origTok != null)
		  {
			PositionIncrementAttribute origPosInc = origTok.addAttribute(typeof(PositionIncrementAttribute));
			origPosInc.PositionIncrement = origPos - pos;
			generated.Add(origTok);
			pos += origPosInc.PositionIncrement;
			origTok = matched.Count == 0 ? null : matched.RemoveFirst();
			if (origTok != null)
			{
			  origPosInc = origTok.addAttribute(typeof(PositionIncrementAttribute));
			  origPos += origPosInc.PositionIncrement;
			}
		  }

		  // what if we replaced a longer sequence with a shorter one?
		  // a/0 b/5 =>  foo/0
		  // should I re-create the gap on the next buffered token?

		  replacement = generated.GetEnumerator();
		  // Now return to the top of the loop to read and return the first
		  // generated token.. The reason this is done is that we may have generated
		  // nothing at all, and may need to continue with more matching logic.
		}
	  }


	  //
	  // Defer creation of the buffer until the first time it is used to
	  // optimize short fields with no matches.
	  //
	  private LinkedList<AttributeSource> buffer;
	  private LinkedList<AttributeSource> matched;

	  private bool exhausted;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private org.apache.lucene.util.AttributeSource nextTok() throws java.io.IOException
	  private AttributeSource nextTok()
	  {
		if (buffer != null && buffer.Count > 0)
		{
		  return buffer.RemoveFirst();
		}
		else
		{
		  if (!exhausted && input.incrementToken())
		  {
			return this;
		  }
		  else
		  {
			exhausted = true;
			return null;
		  }
		}
	  }

	  private void pushTok(AttributeSource t)
	  {
		if (buffer == null)
		{
			buffer = new LinkedList<>();
		}
		buffer.AddFirst(t);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private SlowSynonymMap match(SlowSynonymMap map) throws java.io.IOException
	  private SlowSynonymMap match(SlowSynonymMap map)
	  {
		SlowSynonymMap result = null;

		if (map.submap != null)
		{
		  AttributeSource tok = nextTok();
		  if (tok != null)
		  {
			// clone ourselves.
			if (tok == this)
			{
			  tok = cloneAttributes();
			}
			// check for positionIncrement!=1?  if>1, should not match, if==0, check multiple at this level?
			CharTermAttribute termAtt = tok.getAttribute(typeof(CharTermAttribute));
			SlowSynonymMap subMap = map.submap.get(termAtt.buffer(), 0, termAtt.length());

			if (subMap != null)
			{
			  // recurse
			  result = match(subMap);
			}

			if (result != null)
			{
			  matched.AddFirst(tok);
			}
			else
			{
			  // push back unmatched token
			  pushTok(tok);
			}
		  }
		}

		// if no longer sequence matched, so if this node has synonyms, it's the match.
		if (result == null && map.synonyms != null)
		{
		  result = map;
		}

		return result;
	  }

	  private void copy(AttributeSource target, AttributeSource source)
	  {
		if (target != source)
		{
		  source.copyTo(target);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void reset() throws java.io.IOException
	  public override void reset()
	  {
		input.reset();
		replacement = null;
		exhausted = false;
	  }
	}

}