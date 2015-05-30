using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;

namespace Lucene.Net.Analysis.Miscellaneous
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
    /// <summary>
	/// Provides the ability to override any <seealso cref="KeywordAttribute"/> aware stemmer
	/// with custom dictionary-based stemming.
	/// </summary>
	public sealed class StemmerOverrideFilter : TokenFilter
	{
	  private readonly StemmerOverrideMap stemmerOverrideMap;

	  private readonly ICharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly IKeywordAttribute keywordAtt = addAttribute(typeof(KeywordAttribute));
	  private readonly FST.BytesReader fstReader;
	  private readonly FST.Arc<BytesRef> scratchArc = new FST.Arc<BytesRef>();
	  private readonly CharsRef spare = new CharsRef();

	  /// <summary>
	  /// Create a new StemmerOverrideFilter, performing dictionary-based stemming
	  /// with the provided <code>dictionary</code>.
	  /// <para>
	  /// Any dictionary-stemmed terms will be marked with <seealso cref="KeywordAttribute"/>
	  /// so that they will not be stemmed with stemmers down the chain.
	  /// </para>
	  /// </summary>
	  public StemmerOverrideFilter(TokenStream input, StemmerOverrideMap stemmerOverrideMap) : base(input)
	  {
		this.stemmerOverrideMap = stemmerOverrideMap;
		fstReader = stemmerOverrideMap.BytesReader;
	  }

	  public override bool IncrementToken()
	  {
		if (input.IncrementToken())
		{
		  if (fstReader == null)
		  {
			// No overrides
			return true;
		  }
		  if (!keywordAtt.Keyword) // don't muck with already-keyworded terms
		  {
			BytesRef stem = stemmerOverrideMap.get(termAtt.Buffer(), termAtt.Length, scratchArc, fstReader);
			if (stem != null)
			{
			  char[] buffer = spare.chars = termAtt.Buffer();
			  UnicodeUtil.UTF8toUTF16(stem.Bytes, stem.Offset, stem.Length, spare);
			  if (spare.chars != buffer)
			  {
				termAtt.copyBuffer(spare.chars, spare.offset, spare.length);
			  }
			  termAtt.Length = spare.length;
			  keywordAtt.Keyword = true;
			}
		  }
		  return true;
		}
		else
		{
		  return false;
		}
	  }

	  /// <summary>
	  /// A read-only 4-byte FST backed map that allows fast case-insensitive key
	  /// value lookups for <seealso cref="StemmerOverrideFilter"/>
	  /// </summary>
	  // TODO maybe we can generalize this and reuse this map somehow?
	  public sealed class StemmerOverrideMap
	  {
		internal readonly FST<BytesRef> fst;
		internal readonly bool ignoreCase;

		/// <summary>
		/// Creates a new <seealso cref="StemmerOverrideMap"/> </summary>
		/// <param name="fst"> the fst to lookup the overrides </param>
		/// <param name="ignoreCase"> if the keys case should be ingored </param>
		public StemmerOverrideMap(FST<BytesRef> fst, bool ignoreCase)
		{
		  this.fst = fst;
		  this.ignoreCase = ignoreCase;
		}

		/// <summary>
		/// Returns a <seealso cref="BytesReader"/> to pass to the <seealso cref="#get(char[], int, FST.Arc, FST.BytesReader)"/> method.
		/// </summary>
		public FST.BytesReader BytesReader
		{
			get
			{
			  if (fst == null)
			  {
				return null;
			  }
			  else
			  {
				return fst.BytesReader;
			  }
			}
		}

		/// <summary>
		/// Returns the value mapped to the given key or <code>null</code> if the key is not in the FST dictionary.
		/// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public org.apache.lucene.util.BytesRef get(char[] buffer, int bufferLen, org.apache.lucene.util.fst.FST.Arc<org.apache.lucene.util.BytesRef> scratchArc, org.apache.lucene.util.fst.FST.BytesReader fstReader) throws java.io.IOException
		public BytesRef get(char[] buffer, int bufferLen, FST.Arc<BytesRef> scratchArc, FST.BytesReader fstReader)
		{
		  BytesRef pendingOutput = fst.outputs.NoOutput;
		  BytesRef matchOutput = null;
		  int bufUpto = 0;
		  fst.getFirstArc(scratchArc);
		  while (bufUpto < bufferLen)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int codePoint = Character.codePointAt(buffer, bufUpto, bufferLen);
			int codePoint = char.codePointAt(buffer, bufUpto, bufferLen);
			if (fst.findTargetArc(ignoreCase ? char.ToLower(codePoint) : codePoint, scratchArc, scratchArc, fstReader) == null)
			{
			  return null;
			}
			pendingOutput = fst.outputs.add(pendingOutput, scratchArc.output);
			bufUpto += char.charCount(codePoint);
		  }
		  if (scratchArc.Final)
		  {
			matchOutput = fst.outputs.add(pendingOutput, scratchArc.nextFinalOutput);
		  }
		  return matchOutput;
		}

	  }
	  /// <summary>
	  /// This builder builds an <seealso cref="FST"/> for the <seealso cref="StemmerOverrideFilter"/>
	  /// </summary>
	  public class Builder
	  {
		internal readonly BytesRefHash hash = new BytesRefHash();
		internal readonly BytesRef spare = new BytesRef();
		internal readonly List<CharSequence> outputValues = new List<CharSequence>();
		internal readonly bool ignoreCase;
		internal readonly CharsRef charsSpare = new CharsRef();

		/// <summary>
		/// Creates a new <seealso cref="Builder"/> with ignoreCase set to <code>false</code> 
		/// </summary>
		public Builder() : this(false)
		{
		}

		/// <summary>
		/// Creates a new <seealso cref="Builder"/> </summary>
		/// <param name="ignoreCase"> if the input case should be ignored. </param>
		public Builder(bool ignoreCase)
		{
		  this.ignoreCase = ignoreCase;
		}

		/// <summary>
		/// Adds an input string and it's stemmer override output to this builder.
		/// </summary>
		/// <param name="input"> the input char sequence </param>
		/// <param name="output"> the stemmer override output char sequence </param>
		/// <returns> <code>false</code> iff the input has already been added to this builder otherwise <code>true</code>. </returns>
		public virtual bool add(ICharSequence input, ICharSequence output)
		{
		  int length = input.length();
		  if (ignoreCase)
		  {
			// convert on the fly to lowercase
			charsSpare.grow(length);
			char[] buffer = charsSpare.chars;
			for (int i = 0; i < length;)
			{
				i += char.toChars(char.ToLower(char.codePointAt(input, i)), buffer, i);
			}
			UnicodeUtil.UTF16toUTF8(buffer, 0, length, spare);
		  }
		  else
		  {
			UnicodeUtil.UTF16toUTF8(input, 0, length, spare);
		  }
		  if (hash.add(spare) >= 0)
		  {
			outputValues.Add(output);
			return true;
		  }
		  return false;
		}

		/// <summary>
		/// Returns an <seealso cref="StemmerOverrideMap"/> to be used with the <seealso cref="StemmerOverrideFilter"/> </summary>
		/// <returns> an <seealso cref="StemmerOverrideMap"/> to be used with the <seealso cref="StemmerOverrideFilter"/> </returns>
		/// <exception cref="IOException"> if an <seealso cref="IOException"/> occurs; </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public StemmerOverrideMap build() throws java.io.IOException
		public virtual StemmerOverrideMap build()
		{
		  ByteSequenceOutputs outputs = ByteSequenceOutputs.Singleton;
		  org.apache.lucene.util.fst.Builder<BytesRef> builder = new org.apache.lucene.util.fst.Builder<BytesRef>(FST.INPUT_TYPE.BYTE4, outputs);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] sort = hash.sort(org.apache.lucene.util.BytesRef.getUTF8SortedAsUnicodeComparator());
		  int[] sort = hash.sort(BytesRef.UTF8SortedAsUnicodeComparator);
		  IntsRef intsSpare = new IntsRef();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int size = hash.size();
		  int size = hash.size();
		  for (int i = 0; i < size; i++)
		  {
			int id = sort[i];
			BytesRef bytesRef = hash.get(id, spare);
			UnicodeUtil.UTF8toUTF32(bytesRef, intsSpare);
			builder.add(intsSpare, new BytesRef(outputValues[id]));
		  }
		  return new StemmerOverrideMap(builder.finish(), ignoreCase);
		}

	  }
	}

}