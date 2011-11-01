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
using System.Text;
using System.Collections;
using System.Globalization;

using Lucene.Net.Analysis;

namespace Lucene.Net.Analysis.Cn
{
	/* ====================================================================
	 * The Apache Software License, Version 1.1
	 *
	 * Copyright (c) 2004 The Apache Software Foundation.  All rights
	 * reserved.
	 *
	 * Redistribution and use in source and binary forms, with or without
	 * modification, are permitted provided that the following conditions
	 * are met:
	 *
	 * 1. Redistributions of source code must retain the above copyright
	 *    notice, this list of conditions and the following disclaimer.
	 *
	 * 2. Redistributions in binary form must reproduce the above copyright
	 *    notice, this list of conditions and the following disclaimer in
	 *    the documentation and/or other materials provided with the
	 *    distribution.
	 *
	 * 3. The end-user documentation included with the redistribution,
	 *    if any, must include the following acknowledgment:
	 *       "This product includes software developed by the
	 *        Apache Software Foundation (http://www.apache.org/)."
	 *    Alternately, this acknowledgment may appear in the software itself,
	 *    if and wherever such third-party acknowledgments normally appear.
	 *
	 * 4. The names "Apache" and "Apache Software Foundation" and
	 *    "Apache Lucene" must not be used to endorse or promote products
	 *    derived from this software without prior written permission. For
	 *    written permission, please contact apache@apache.org.
	 *
	 * 5. Products derived from this software may not be called "Apache",
	 *    "Apache Lucene", nor may "Apache" appear in their name, without
	 *    prior written permission of the Apache Software Foundation.
	 *
	 * THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED
	 * WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
	 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
	 * DISCLAIMED.  IN NO EVENT SHALL THE APACHE SOFTWARE FOUNDATION OR
	 * ITS CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
	 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
	 * LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF
	 * USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
	 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
	 * OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT
	 * OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
	 * SUCH DAMAGE.
	 * ====================================================================
	 *
	 * This software consists of voluntary contributions made by many
	 * individuals on behalf of the Apache Software Foundation.  For more
	 * information on the Apache Software Foundation, please see
	 * <http://www.apache.org/>.
	 */

	/// <summary>
	/// Title: ChineseTokenizer
	/// Description: Extract tokens from the Stream using Character.getType()
	///              Rule: A Chinese character as a single token
	/// Copyright:   Copyright (c) 2001
	/// Company:
	/// 
	/// The difference between thr ChineseTokenizer and the
	/// CJKTokenizer (id=23545) is that they have different
	/// token parsing logic.
	/// 
	/// Let me use an example. If having a Chinese text
	/// "C1C2C3C4" to be indexed, the tokens returned from the
	/// ChineseTokenizer are C1, C2, C3, C4. And the tokens
	/// returned from the CJKTokenizer are C1C2, C2C3, C3C4.
	/// 
	/// Therefore the index the CJKTokenizer created is much
	/// larger.
	/// 
	/// The problem is that when searching for C1, C1C2, C1C3,
	/// C4C2, C1C2C3 ... the ChineseTokenizer works, but the
	/// CJKTokenizer will not work.
	/// <author>Yiyi Sun</author>
	/// <version>$Id: ChineseTokenizer.java, v 1.4 2003/03/02 13:56:03 otis Exp $</version>
	/// </summary>
	public sealed class ChineseTokenizer : Tokenizer 
	{


		public ChineseTokenizer(TextReader _in) 
		{
			input = _in;
		}

		private int offset = 0, bufferIndex=0, dataLen=0;
		private static int MAX_WORD_LEN = 255;
		private static int IO_BUFFER_SIZE = 1024;
		private char[] buffer = new char[MAX_WORD_LEN];
		private char[] ioBuffer = new char[IO_BUFFER_SIZE];

		private int length;
		private int start;

		private void Push(char c) 
		{

			if (length == 0) start = offset-1;            // start of token
			buffer[length++] = Char.ToLower(c);  // buffer it

		}

		private Token Flush() 
		{

			if (length > 0) 
			{
				//System.out.println(new String(buffer, 0, length));
				return new Token(new String(buffer, 0, length), start, start+length);
			}
			else
				return null;
		}

		public override Token Next()
		{

			length = 0;
			start = offset;


			while (true) 
			{

				char c;
				offset++;

				if (bufferIndex >= dataLen) 
				{
					dataLen = input.Read(ioBuffer, 0, ioBuffer.Length);
					bufferIndex = 0;
				};

				if (dataLen == 0) return Flush();
				else
					c = ioBuffer[bufferIndex++];


				switch(Char.GetUnicodeCategory(c)) 
				{

					case UnicodeCategory.DecimalDigitNumber:
					case UnicodeCategory.LowercaseLetter:
					case UnicodeCategory.UppercaseLetter:
						Push(c);
						if (length == MAX_WORD_LEN) return Flush();
						break;

					case UnicodeCategory.OtherLetter:
						if (length>0) 
						{
							bufferIndex--;
							return Flush();
						}
						Push(c);
						return Flush();

					default:
						if (length>0) return Flush();
						break;
				}
			}

		}
	}
}
