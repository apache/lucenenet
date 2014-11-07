using System;

namespace org.apache.lucene.analysis.cn
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


	using StopFilter = org.apache.lucene.analysis.core.StopFilter;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// A <seealso cref="TokenFilter"/> with a stop word table.  
	/// <ul>
	/// <li>Numeric tokens are removed.
	/// <li>English tokens must be larger than 1 character.
	/// <li>One Chinese character as one Chinese word.
	/// </ul>
	/// TO DO:
	/// <ol>
	/// <li>Add Chinese stop words, such as \ue400
	/// <li>Dictionary based Chinese word extraction
	/// <li>Intelligent Chinese word extraction
	/// </ol>
	/// </summary>
	/// @deprecated (3.1) Use <seealso cref="StopFilter"/> instead, which has the same functionality.
	/// This filter will be removed in Lucene 5.0 
	[Obsolete("(3.1) Use <seealso cref="StopFilter"/> instead, which has the same functionality.")]
	public sealed class ChineseFilter : TokenFilter
	{


		// Only English now, Chinese to be added later.
		public static readonly string[] STOP_WORDS = new string[] {"and", "are", "as", "at", "be", "but", "by", "for", "if", "in", "into", "is", "it", "no", "not", "of", "on", "or", "such", "that", "the", "their", "then", "there", "these", "they", "this", "to", "was", "will", "with"};


		private CharArraySet stopTable;

		private CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));

		public ChineseFilter(TokenStream @in) : base(@in)
		{

			stopTable = new CharArraySet(Version.LUCENE_CURRENT, Arrays.asList(STOP_WORDS), false);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
		public override bool incrementToken()
		{

			while (input.incrementToken())
			{
				char[] text = termAtt.buffer();
				int termLength = termAtt.length();

			  // why not key off token type here assuming ChineseTokenizer comes first?
				if (!stopTable.contains(text, 0, termLength))
				{
					switch (char.getType(text[0]))
					{

					case char.LOWERCASE_LETTER:
					case char.UPPERCASE_LETTER:

						// English word/token should larger than 1 character.
						if (termLength > 1)
						{
							return true;
						}
						break;
					case char.OTHER_LETTER:

						// One Chinese character as one Chinese word.
						// Chinese word extraction to be added later here.

						return true;
					}

				}

			}
			return false;
		}

	}
}