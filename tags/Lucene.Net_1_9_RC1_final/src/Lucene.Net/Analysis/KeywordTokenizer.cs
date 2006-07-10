/*
 * Copyright 2004-2005 The Apache Software Foundation
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
	
	/// <summary> Emits the entire input as a single token.</summary>
	public class KeywordTokenizer : Tokenizer
	{
		
		private const int DEFAULT_BUFFER_SIZE = 256;
		
		private bool done;
		private char[] buffer;
		
		public KeywordTokenizer(System.IO.TextReader input) : this(input, DEFAULT_BUFFER_SIZE)
		{
		}
		
		public KeywordTokenizer(System.IO.TextReader input, int bufferSize) : base(input)
		{
			this.buffer = new char[bufferSize];
			this.done = false;
		}
		
		public override Token Next()
		{
			if (!done)
			{
				done = true;
				System.Text.StringBuilder buffer = new System.Text.StringBuilder();
				int length;
				while (true)
				{
					length = input.Read((System.Char[]) this.buffer, 0, this.buffer.Length);
					if (length <= 0)
						break;
					
					buffer.Append(this.buffer, 0, length);
				}
				System.String text = buffer.ToString();
				return new Token(text, 0, text.Length);
			}
			return null;
		}
	}
}