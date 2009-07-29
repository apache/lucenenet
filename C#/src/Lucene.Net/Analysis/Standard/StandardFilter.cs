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
using TokenFilter = Lucene.Net.Analysis.TokenFilter;
using TokenStream = Lucene.Net.Analysis.TokenStream;

namespace Lucene.Net.Analysis.Standard
{
	
	/// <summary>Normalizes tokens extracted with {@link StandardTokenizer}. </summary>
	public sealed class StandardFilter:TokenFilter
	{
        private static readonly System.String APOSTROPHE_TYPE;
        private static readonly System.String ACRONYM_TYPE;

        static StandardFilter()
        {
            APOSTROPHE_TYPE = StandardTokenizerImpl.TOKEN_TYPES[StandardTokenizerImpl.APOSTROPHE];
            ACRONYM_TYPE = StandardTokenizerImpl.TOKEN_TYPES[StandardTokenizerImpl.ACRONYM];
        }
		
        /// <summary>Construct filtering <i>in</i>. </summary>
        public StandardFilter(TokenStream in_Renamed)
            : base(in_Renamed)
        {
        }

        /// <summary>Returns the next token in the stream, or null at EOS.
		/// <p>Removes <tt>'s</tt> from the end of words.
		/// <p>Removes dots from acronyms.
		/// </summary>
		public override Token Next(/* in */ Token reusableToken)
		{
            System.Diagnostics.Debug.Assert(reusableToken != null);
			Token nextToken = input.Next(reusableToken);
			
			if (nextToken == null)
				return null;
			
			char[] buffer = nextToken.TermBuffer();
			int bufferLength = nextToken.TermLength();
			System.String type = nextToken.Type();
			
			if (type == APOSTROPHE_TYPE && 
                bufferLength >= 2 &&
                buffer[bufferLength - 2] == '\'' &&
                (buffer[bufferLength - 1] == 's' || buffer[bufferLength - 1] == 'S'))
			{
				// Strip last 2 characters off
				nextToken.SetTermLength(bufferLength - 2);
			}
			else if (type == ACRONYM_TYPE)
			{
				// remove dots
				int upto = 0;
				for (int i = 0; i < bufferLength; i++)
				{
					char c = buffer[i];
					if (c != '.')
						buffer[upto++] = c;
				}
				nextToken.SetTermLength(upto);
			}
			
			return nextToken;
		}
	}
}