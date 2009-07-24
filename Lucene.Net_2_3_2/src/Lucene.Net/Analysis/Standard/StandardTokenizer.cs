/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using Token = Lucene.Net.Analysis.Token;
using Tokenizer = Lucene.Net.Analysis.Tokenizer;

namespace Lucene.Net.Analysis.Standard
{
	
	/// <summary>A grammar-based tokenizer constructed with JFlex
	/// 
	/// <p> This should be a good tokenizer for most European-language documents:
	/// 
	/// <ul>
	/// <li>Splits words at punctuation characters, removing punctuation. However, a 
	/// dot that's not followed by whitespace is considered part of a token.
	/// <li>Splits words at hyphens, unless there's a number in the token, in which case
	/// the whole token is interpreted as a product number and is not split.
	/// <li>Recognizes email addresses and internet hostnames as one token.
	/// </ul>
	/// 
	/// <p>Many applications have specific tokenizer needs.  If this tokenizer does
	/// not suit your application, please consider copying this source code
	/// directory to your project and maintaining your own grammar-based tokenizer.
	/// </summary>
	
	public class StandardTokenizer : Tokenizer
	{
        private void InitBlock()
        {
            maxTokenLength = StandardAnalyzer.DEFAULT_MAX_TOKEN_LENGTH;
        }

        /// <summary>A private instance of the JFlex-constructed scanner </summary>
        private StandardTokenizerImpl scanner;

        public const int ALPHANUM = 0;
        public const int APOSTROPHE = 1;
        public const int ACRONYM = 2;
        public const int COMPANY = 3;
        public const int EMAIL = 4;
        public const int HOST = 5;
        public const int NUM = 6;
        public const int CJ = 7;

        /// <deprecated> this solves a bug where HOSTs that end with '.' are identified
        /// as ACRONYMs. It is deprecated and will be removed in the next
        /// release.
        /// </deprecated>
        public const int ACRONYM_DEP = 8;

        public static readonly System.String[] TOKEN_TYPES = new System.String[] { "<ALPHANUM>", "<APOSTROPHE>", "<ACRONYM>", "<COMPANY>", "<EMAIL>", "<HOST>", "<NUM>", "<CJ>", "<ACRONYM_DEP>" };

        /** @deprecated Please use {@link #TOKEN_TYPES} instead */
        public static readonly String[] tokenImage = TOKEN_TYPES;

		/// <summary> Specifies whether deprecated acronyms should be replaced with HOST type.
		/// This is false by default to support backward compatibility.
		/// <p/>
		/// See http://issues.apache.org/jira/browse/LUCENE-1068
		/// 
		/// </summary>
		/// <deprecated> this should be removed in the next release (3.0).
		/// </deprecated>
		private bool replaceInvalidAcronym = false;
		
		internal virtual void  SetInput(System.IO.TextReader reader)
		{
			this.input = reader;
		}
		
		private int maxTokenLength;
		
		/// <summary>Set the max allowed token length.  Any token longer
		/// than this is skipped. 
		/// </summary>
		public virtual void  SetMaxTokenLength(int length)
		{
			this.maxTokenLength = length;
		}
		
		/// <seealso cref="setMaxTokenLength">
		/// </seealso>
		public virtual int GetMaxTokenLength()
		{
			return maxTokenLength;
		}
		
		/// <summary> Creates a new instance of the {@link StandardTokenizer}. Attaches the
		/// <code>input</code> to a newly created JFlex scanner.
		/// </summary>
		public StandardTokenizer(System.IO.TextReader input)
		{
			InitBlock();
			this.input = input;
			this.scanner = new StandardTokenizerImpl(input);
		}
		
		/// <summary> Creates a new instance of the {@link Lucene.Net.Analysis.Standard.StandardTokenizer}.  Attaches
		/// the <code>input</code> to the newly created JFlex scanner.
		/// 
		/// </summary>
		/// <param name="input">The input reader
		/// </param>
		/// <param name="replaceInvalidAcronym">Set to true to replace mischaracterized acronyms with HOST.
		/// 
		/// See http://issues.apache.org/jira/browse/LUCENE-1068
		/// </param>
		public StandardTokenizer(System.IO.TextReader input, bool replaceInvalidAcronym)
		{
			InitBlock();
			this.replaceInvalidAcronym = replaceInvalidAcronym;
			this.input = input;
			this.scanner = new StandardTokenizerImpl(input);
		}
		
		/*
		* (non-Javadoc)
		*
		* @see Lucene.Net.Analysis.TokenStream#next()
		*/
		public override Token Next(Token result)
		{
			int posIncr = 1;
			
			while (true)
			{
				int tokenType = scanner.GetNextToken();
				
				if (tokenType == StandardTokenizerImpl.YYEOF)
				{
					return null;
				}
				
				if (scanner.Yylength() <= maxTokenLength)
				{
					result.Clear();
					result.SetPositionIncrement(posIncr);
					scanner.GetText(result);
					int start = scanner.Yychar();
					result.SetStartOffset(start);
					result.SetEndOffset(start + result.TermLength());
					// This 'if' should be removed in the next release. For now, it converts
					// invalid acronyms to HOST. When removed, only the 'else' part should
					// remain.
					if (tokenType == StandardTokenizerImpl.ACRONYM_DEP)
					{
						if (replaceInvalidAcronym)
						{
							result.SetType(StandardTokenizerImpl.TOKEN_TYPES[StandardTokenizerImpl.HOST]);
							result.SetTermLength(result.TermLength() - 1); // remove extra '.'
						}
						else
						{
							result.SetType(StandardTokenizerImpl.TOKEN_TYPES[StandardTokenizerImpl.ACRONYM]);
						}
					}
					else
					{
						result.SetType(StandardTokenizerImpl.TOKEN_TYPES[tokenType]);
					}
					return result;
				}
				// When we skip a too-long term, we still increment the
				// position increment
				else
					posIncr++;
			}
		}
		
		/*
		* (non-Javadoc)
		*
		* @see Lucene.Net.Analysis.TokenStream#reset()
		*/
		public override void  Reset()
		{
			base.Reset();
			scanner.Yyreset(input);
		}
		
		public override void  Reset(System.IO.TextReader reader)
		{
			input = reader;
			Reset();
		}
		
		/// <summary> Prior to https://issues.apache.org/jira/browse/LUCENE-1068, StandardTokenizer mischaracterized as acronyms tokens like www.abc.com
		/// when they should have been labeled as hosts instead.
		/// </summary>
		/// <returns> true if StandardTokenizer now returns these tokens as Hosts, otherwise false
		/// 
		/// </returns>
		/// <deprecated> Remove in 3.X and make true the only valid value
		/// </deprecated>
		public virtual bool IsReplaceInvalidAcronym()
		{
			return replaceInvalidAcronym;
		}
		
		/// <summary> </summary>
		/// <param name="replaceInvalidAcronym">Set to true to replace mischaracterized acronyms as HOST.
		/// </param>
		/// <deprecated> Remove in 3.X and make true the only valid value
		/// 
		/// See https://issues.apache.org/jira/browse/LUCENE-1068
		/// </deprecated>
		public virtual void  SetReplaceInvalidAcronym(bool replaceInvalidAcronym)
		{
			this.replaceInvalidAcronym = replaceInvalidAcronym;
		}
	}
}