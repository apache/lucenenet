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
using System.IO;
using Lucene.Net.Analysis;

namespace Lucene.Net.Analysis.Ru
{
	/// <summary>
	/// A RussianLetterTokenizer is a tokenizer that extends LetterTokenizer by additionally looking up letters
	/// in a given "russian charset". The problem with LeterTokenizer is that it uses Character.isLetter() method,
	/// which doesn't know how to detect letters in encodings like CP1252 and KOI8
	/// (well-known problems with 0xD7 and 0xF7 chars)
	/// </summary>
	public class RussianLetterTokenizer : CharTokenizer
	{
		/// <summary>
		/// Construct a new LetterTokenizer.
		/// </summary>
		private char[] charset;

		public RussianLetterTokenizer(TextReader _in, char[] charset) : base(_in)
		{
			this.charset = charset;
		}

		/// <summary>
		/// Collects only characters which satisfy Char.IsLetter(char).
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		protected override bool IsTokenChar(char c)
		{
			if (Char.IsLetter(c))
				return true;
			for (int i = 0; i < charset.Length; i++)
			{
				if (c == charset[i])
					return true;
			}
			return false;
		}
	}
}