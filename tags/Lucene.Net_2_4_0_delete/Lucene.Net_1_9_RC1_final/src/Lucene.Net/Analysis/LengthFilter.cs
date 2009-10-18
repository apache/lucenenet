/*
 * Copyright 2004 The Apache Software Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
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

namespace Lucene.Net.Analysis
{
	
	/// <summary> Removes words that are too long and too short from the stream.
	/// 
	/// </summary>
	/// <author>  David Spencer
	/// </author>
	/// <version>  $Id: LengthFilter.java 347992 2005-11-21 21:41:43Z dnaber $
	/// </version>
	public sealed class LengthFilter : TokenFilter
	{
		
		internal int min;
		internal int max;
		
		/// <summary> Build a filter that removes words that are too long or too
		/// short from the text.
		/// </summary>
		public LengthFilter(TokenStream in_Renamed, int min, int max) : base(in_Renamed)
		{
			this.min = min;
			this.max = max;
		}
		
		/// <summary> Returns the next input Token whose termText() is the right len</summary>
		public override Token Next()
		{
			// return the first non-stop word found
			for (Token token = input.Next(); token != null; token = input.Next())
			{
				int len = token.TermText().Length;
				if (len >= min && len <= max)
				{
					return token;
				}
				// note: else we ignore it but should we index each part of it?
			}
			// reached EOS -- return null
			return null;
		}
	}
}