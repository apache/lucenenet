using System;
using System.Diagnostics;
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


	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;
	using ByteArrayDataOutput = org.apache.lucene.store.ByteArrayDataOutput;
	using BytesRef = org.apache.lucene.util.BytesRef;
	using BytesRefHash = org.apache.lucene.util.BytesRefHash;
	using CharsRef = org.apache.lucene.util.CharsRef;
	using IOUtils = org.apache.lucene.util.IOUtils;
	using IntsRef = org.apache.lucene.util.IntsRef;
	using UnicodeUtil = org.apache.lucene.util.UnicodeUtil;
	using ByteSequenceOutputs = org.apache.lucene.util.fst.ByteSequenceOutputs;
	using FST = org.apache.lucene.util.fst.FST;
	using Util = org.apache.lucene.util.fst.Util;

	/// <summary>
	/// A map of synonyms, keys and values are phrases.
	/// @lucene.experimental
	/// </summary>
	public class SynonymMap
	{
	  /// <summary>
	  /// for multiword support, you must separate words with this separator </summary>
	  public const char WORD_SEPARATOR = (char)0;
	  /// <summary>
	  /// map&lt;input word, list&lt;ord&gt;&gt; </summary>
	  public readonly FST<BytesRef> fst;
	  /// <summary>
	  /// map&lt;ord, outputword&gt; </summary>
	  public readonly BytesRefHash words;
	  /// <summary>
	  /// maxHorizontalContext: maximum context we need on the tokenstream </summary>
	  public readonly int maxHorizontalContext;

	  public SynonymMap(FST<BytesRef> fst, BytesRefHash words, int maxHorizontalContext)
	  {
		this.fst = fst;
		this.words = words;
		this.maxHorizontalContext = maxHorizontalContext;
	  }

	  /// <summary>
	  /// Builds an FSTSynonymMap.
	  /// <para>
	  /// Call add() until you have added all the mappings, then call build() to get an FSTSynonymMap
	  /// @lucene.experimental
	  /// </para>
	  /// </summary>
	  public class Builder
	  {
		internal readonly Dictionary<CharsRef, MapEntry> workingSet = new Dictionary<CharsRef, MapEntry>();
		internal readonly BytesRefHash words = new BytesRefHash();
		internal readonly BytesRef utf8Scratch = new BytesRef(8);
		internal int maxHorizontalContext;
		internal readonly bool dedup;

		/// <summary>
		/// If dedup is true then identical rules (same input,
		///  same output) will be added only once. 
		/// </summary>
		public Builder(bool dedup)
		{
		  this.dedup = dedup;
		}

		private class MapEntry
		{
		  internal bool includeOrig;
		  // we could sort for better sharing ultimately, but it could confuse people
		  internal List<int?> ords = new List<int?>();
		}

		/// <summary>
		/// Sugar: just joins the provided terms with {@link
		///  SynonymMap#WORD_SEPARATOR}.  reuse and its chars
		///  must not be null. 
		/// </summary>
		public static CharsRef join(string[] words, CharsRef reuse)
		{
		  int upto = 0;
		  char[] buffer = reuse.chars;
		  foreach (string word in words)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int wordLen = word.length();
			int wordLen = word.Length;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int needed = (0 == upto ? wordLen : 1 + upto + wordLen);
			int needed = (0 == upto ? wordLen : 1 + upto + wordLen); // Add 1 for WORD_SEPARATOR
			if (needed > buffer.Length)
			{
			  reuse.grow(needed);
			  buffer = reuse.chars;
			}
			if (upto > 0)
			{
			  buffer[upto++] = SynonymMap.WORD_SEPARATOR;
			}

			word.CopyTo(0, buffer, upto, wordLen - 0);
			upto += wordLen;
		  }
		  reuse.length = upto;
		  return reuse;
		}



		/// <summary>
		/// only used for asserting! </summary>
		internal virtual bool hasHoles(CharsRef chars)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int end = chars.offset + chars.length;
		  int end = chars.offset + chars.length;
		  for (int idx = chars.offset + 1;idx < end;idx++)
		  {
			if (chars.chars[idx] == SynonymMap.WORD_SEPARATOR && chars.chars[idx - 1] == SynonymMap.WORD_SEPARATOR)
			{
			  return true;
			}
		  }
		  if (chars.chars[chars.offset] == '\u0000')
		  {
			return true;
		  }
		  if (chars.chars[chars.offset + chars.length - 1] == '\u0000')
		  {
			return true;
		  }

		  return false;
		}

		// NOTE: while it's tempting to make this public, since
		// caller's parser likely knows the
		// numInput/numOutputWords, sneaky exceptions, much later
		// on, will result if these values are wrong; so we always
		// recompute ourselves to be safe:
		internal virtual void add(CharsRef input, int numInputWords, CharsRef output, int numOutputWords, bool includeOrig)
		{
		  // first convert to UTF-8
		  if (numInputWords <= 0)
		  {
			throw new System.ArgumentException("numInputWords must be > 0 (got " + numInputWords + ")");
		  }
		  if (input.length <= 0)
		  {
			throw new System.ArgumentException("input.length must be > 0 (got " + input.length + ")");
		  }
		  if (numOutputWords <= 0)
		  {
			throw new System.ArgumentException("numOutputWords must be > 0 (got " + numOutputWords + ")");
		  }
		  if (output.length <= 0)
		  {
			throw new System.ArgumentException("output.length must be > 0 (got " + output.length + ")");
		  }

		  Debug.Assert(!hasHoles(input), "input has holes: " + input);
		  Debug.Assert(!hasHoles(output), "output has holes: " + output);

		  //System.out.println("fmap.add input=" + input + " numInputWords=" + numInputWords + " output=" + output + " numOutputWords=" + numOutputWords);
		  UnicodeUtil.UTF16toUTF8(output.chars, output.offset, output.length, utf8Scratch);
		  // lookup in hash
		  int ord = words.add(utf8Scratch);
		  if (ord < 0)
		  {
			// already exists in our hash
			ord = (-ord) - 1;
			//System.out.println("  output=" + output + " old ord=" + ord);
		  }
		  else
		  {
			//System.out.println("  output=" + output + " new ord=" + ord);
		  }

		  MapEntry e = workingSet[input];
		  if (e == null)
		  {
			e = new MapEntry();
			workingSet[CharsRef.deepCopyOf(input)] = e; // make a copy, since we will keep around in our map
		  }

		  e.ords.Add(ord);
		  e.includeOrig |= includeOrig;
		  maxHorizontalContext = Math.Max(maxHorizontalContext, numInputWords);
		  maxHorizontalContext = Math.Max(maxHorizontalContext, numOutputWords);
		}

		internal virtual int countWords(CharsRef chars)
		{
		  int wordCount = 1;
		  int upto = chars.offset;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int limit = chars.offset + chars.length;
		  int limit = chars.offset + chars.length;
		  while (upto < limit)
		  {
			if (chars.chars[upto++] == SynonymMap.WORD_SEPARATOR)
			{
			  wordCount++;
			}
		  }
		  return wordCount;
		}

		/// <summary>
		/// Add a phrase->phrase synonym mapping.
		/// Phrases are character sequences where words are
		/// separated with character zero (U+0000).  Empty words
		/// (two U+0000s in a row) are not allowed in the input nor
		/// the output!
		/// </summary>
		/// <param name="input"> input phrase </param>
		/// <param name="output"> output phrase </param>
		/// <param name="includeOrig"> true if the original should be included </param>
		public virtual void add(CharsRef input, CharsRef output, bool includeOrig)
		{
		  add(input, countWords(input), output, countWords(output), includeOrig);
		}

		/// <summary>
		/// Builds an <seealso cref="SynonymMap"/> and returns it.
		/// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public SynonymMap build() throws java.io.IOException
		public virtual SynonymMap build()
		{
		  ByteSequenceOutputs outputs = ByteSequenceOutputs.Singleton;
		  // TODO: are we using the best sharing options?
		  org.apache.lucene.util.fst.Builder<BytesRef> builder = new org.apache.lucene.util.fst.Builder<BytesRef>(FST.INPUT_TYPE.BYTE4, outputs);

		  BytesRef scratch = new BytesRef(64);
		  ByteArrayDataOutput scratchOutput = new ByteArrayDataOutput();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Set<Integer> dedupSet;
		  HashSet<int?> dedupSet;

		  if (dedup)
		  {
			dedupSet = new HashSet<>();
		  }
		  else
		  {
			dedupSet = null;
		  }

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte[] spare = new byte[5];
		  sbyte[] spare = new sbyte[5];

		  Dictionary<CharsRef, MapEntry>.KeyCollection keys = workingSet.Keys;
		  CharsRef[] sortedKeys = keys.toArray(new CharsRef[keys.size()]);
		  Arrays.sort(sortedKeys, CharsRef.UTF16SortedAsUTF8Comparator);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.IntsRef scratchIntsRef = new org.apache.lucene.util.IntsRef();
		  IntsRef scratchIntsRef = new IntsRef();

		  //System.out.println("fmap.build");
		  for (int keyIdx = 0; keyIdx < sortedKeys.Length; keyIdx++)
		  {
			CharsRef input = sortedKeys[keyIdx];
			MapEntry output = workingSet[input];

			int numEntries = output.ords.Count;
			// output size, assume the worst case
			int estimatedSize = 5 + numEntries * 5; // numEntries + one ord for each entry

			scratch.grow(estimatedSize);
			scratchOutput.reset(scratch.bytes, scratch.offset, scratch.bytes.length);
			Debug.Assert(scratch.offset == 0);

			// now write our output data:
			int count = 0;
			for (int i = 0; i < numEntries; i++)
			{
			  if (dedupSet != null)
			  {
				// box once
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Integer ent = output.ords.get(i);
				int? ent = output.ords[i];
				if (dedupSet.Contains(ent))
				{
				  continue;
				}
				dedupSet.Add(ent);
			  }
			  scratchOutput.writeVInt(output.ords[i]);
			  count++;
			}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int pos = scratchOutput.getPosition();
			int pos = scratchOutput.Position;
			scratchOutput.writeVInt(count << 1 | (output.includeOrig ? 0 : 1));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int pos2 = scratchOutput.getPosition();
			int pos2 = scratchOutput.Position;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int vIntLen = pos2-pos;
			int vIntLen = pos2 - pos;

			// Move the count + includeOrig to the front of the byte[]:
			Array.Copy(scratch.bytes, pos, spare, 0, vIntLen);
			Array.Copy(scratch.bytes, 0, scratch.bytes, vIntLen, pos);
			Array.Copy(spare, 0, scratch.bytes, 0, vIntLen);

			if (dedupSet != null)
			{
			  dedupSet.Clear();
			}

			scratch.length = scratchOutput.Position - scratch.offset;
			//System.out.println("  add input=" + input + " output=" + scratch + " offset=" + scratch.offset + " length=" + scratch.length + " count=" + count);
			builder.add(Util.toUTF32(input, scratchIntsRef), BytesRef.deepCopyOf(scratch));
		  }

		  FST<BytesRef> fst = builder.finish();
		  return new SynonymMap(fst, words, maxHorizontalContext);
		}
	  }

	  /// <summary>
	  /// Abstraction for parsing synonym files.
	  /// 
	  /// @lucene.experimental
	  /// </summary>
	  public abstract class Parser : Builder
	  {

		internal readonly Analyzer analyzer;

		public Parser(bool dedup, Analyzer analyzer) : base(dedup)
		{
		  this.analyzer = analyzer;
		}

		/// <summary>
		/// Parse the given input, adding synonyms to the inherited <seealso cref="Builder"/>. </summary>
		/// <param name="in"> The input to parse </param>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public abstract void parse(java.io.Reader in) throws java.io.IOException, java.text.ParseException;
		public abstract void parse(Reader @in);

		/// <summary>
		/// Sugar: analyzes the text with the analyzer and
		///  separates by <seealso cref="SynonymMap#WORD_SEPARATOR"/>.
		///  reuse and its chars must not be null. 
		/// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public org.apache.lucene.util.CharsRef analyze(String text, org.apache.lucene.util.CharsRef reuse) throws java.io.IOException
		public virtual CharsRef analyze(string text, CharsRef reuse)
		{
		  IOException priorException = null;
		  TokenStream ts = analyzer.tokenStream("", text);
		  try
		  {
			CharTermAttribute termAtt = ts.addAttribute(typeof(CharTermAttribute));
			PositionIncrementAttribute posIncAtt = ts.addAttribute(typeof(PositionIncrementAttribute));
			ts.reset();
			reuse.length = 0;
			while (ts.incrementToken())
			{
			  int length = termAtt.length();
			  if (length == 0)
			  {
				throw new System.ArgumentException("term: " + text + " analyzed to a zero-length token");
			  }
			  if (posIncAtt.PositionIncrement != 1)
			  {
				throw new System.ArgumentException("term: " + text + " analyzed to a token with posinc != 1");
			  }
			  reuse.grow(reuse.length + length + 1); // current + word + separator
			  int end = reuse.offset + reuse.length;
			  if (reuse.length > 0)
			  {
				reuse.chars[end++] = SynonymMap.WORD_SEPARATOR;
				reuse.length++;
			  }
			  Array.Copy(termAtt.buffer(), 0, reuse.chars, end, length);
			  reuse.length += length;
			}
			ts.end();
		  }
		  catch (IOException e)
		  {
			priorException = e;
		  }
		  finally
		  {
			IOUtils.closeWhileHandlingException(priorException, ts);
		  }
		  if (reuse.length == 0)
		  {
			throw new System.ArgumentException("term: " + text + " was completely eliminated by analyzer");
		  }
		  return reuse;
		}
	  }

	}

}