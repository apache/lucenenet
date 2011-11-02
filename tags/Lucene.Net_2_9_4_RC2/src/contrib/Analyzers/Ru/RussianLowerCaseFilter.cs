/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using Lucene.Net.Analysis;

namespace Lucene.Net.Analysis.Ru
{
	/// <summary>
	/// Normalizes token text to lower case, analyzing given ("russian") charset.
	/// </summary>
	public sealed class RussianLowerCaseFilter : TokenFilter
	{
		char[] charset;

		public RussianLowerCaseFilter(TokenStream _in, char[] charset) : base(_in)
		{
			this.charset = charset;
		}

		public override Token Next() 
		{
			Token t = input.Next();

			if (t == null)
				return null;

			String txt = t.TermText();

			char[] chArray = txt.ToCharArray();
			for (int i = 0; i < chArray.Length; i++)
			{
				chArray[i] = RussianCharsets.ToLowerCase(chArray[i], charset);
			}

			String newTxt = new String(chArray);
			// create new token
			Token newToken = new Token(newTxt, t.StartOffset(), t.EndOffset());

			return newToken;
		}
	}
}