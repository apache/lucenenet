using System;

namespace org.apache.lucene.analysis.snowball
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

	using LowerCaseFilter = org.apache.lucene.analysis.core.LowerCaseFilter;
	using KeywordAttribute = org.apache.lucene.analysis.tokenattributes.KeywordAttribute;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using TurkishLowerCaseFilter = org.apache.lucene.analysis.tr.TurkishLowerCaseFilter; // javadoc @link
	using SnowballProgram = org.tartarus.snowball.SnowballProgram;

	/// <summary>
	/// A filter that stems words using a Snowball-generated stemmer.
	/// 
	/// Available stemmers are listed in <seealso cref="org.tartarus.snowball.ext"/>.
	/// <para><b>NOTE</b>: SnowballFilter expects lowercased text.
	/// <ul>
	///  <li>For the Turkish language, see <seealso cref="TurkishLowerCaseFilter"/>.
	///  <li>For other languages, see <seealso cref="LowerCaseFilter"/>.
	/// </ul>
	/// </para>
	/// 
	/// <para>
	/// Note: This filter is aware of the <seealso cref="KeywordAttribute"/>. To prevent
	/// certain terms from being passed to the stemmer
	/// <seealso cref="KeywordAttribute#isKeyword()"/> should be set to <code>true</code>
	/// in a previous <seealso cref="TokenStream"/>.
	/// 
	/// Note: For including the original term as well as the stemmed version, see
	/// <seealso cref="org.apache.lucene.analysis.miscellaneous.KeywordRepeatFilterFactory"/>
	/// </para>
	/// 
	/// 
	/// </summary>
	public sealed class SnowballFilter : TokenFilter
	{

	  private readonly SnowballProgram stemmer;

	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly KeywordAttribute keywordAttr = addAttribute(typeof(KeywordAttribute));

	  public SnowballFilter(TokenStream input, SnowballProgram stemmer) : base(input)
	  {
		this.stemmer = stemmer;
	  }

	  /// <summary>
	  /// Construct the named stemming filter.
	  /// 
	  /// Available stemmers are listed in <seealso cref="org.tartarus.snowball.ext"/>.
	  /// The name of a stemmer is the part of the class name before "Stemmer",
	  /// e.g., the stemmer in <seealso cref="org.tartarus.snowball.ext.EnglishStemmer"/> is named "English".
	  /// </summary>
	  /// <param name="in"> the input tokens to stem </param>
	  /// <param name="name"> the name of a stemmer </param>
	  public SnowballFilter(TokenStream @in, string name) : base(@in)
	  {
		//Class.forName is frowned upon in place of the ResourceLoader but in this case,
		// the factory will use the other constructor so that the program is already loaded.
		try
		{
		  Type stemClass = Type.GetType("org.tartarus.snowball.ext." + name + "Stemmer").asSubclass(typeof(SnowballProgram));
		  stemmer = stemClass.newInstance();
		}
		catch (Exception e)
		{
		  throw new System.ArgumentException("Invalid stemmer class specified: " + name, e);
		}
	  }

	  /// <summary>
	  /// Returns the next input Token, after being stemmed </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public final boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		if (input.incrementToken())
		{
		  if (!keywordAttr.Keyword)
		  {
			char[] termBuffer = termAtt.buffer();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int length = termAtt.length();
			int length = termAtt.length();
			stemmer.setCurrent(termBuffer, length);
			stemmer.stem();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final char finalTerm[] = stemmer.getCurrentBuffer();
			char[] finalTerm = stemmer.CurrentBuffer;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int newLength = stemmer.getCurrentBufferLength();
			int newLength = stemmer.CurrentBufferLength;
			if (finalTerm != termBuffer)
			{
			  termAtt.copyBuffer(finalTerm, 0, newLength);
			}
			else
			{
			  termAtt.Length = newLength;
			}
		  }
		  return true;
		}
		else
		{
		  return false;
		}
	  }
	}

}