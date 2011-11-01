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

namespace Lucene.Net.Analysis.Nl
{

	/* ====================================================================
	 * The Apache Software License, Version 1.1
	 *
	 * Copyright (c) 2001 The Apache Software Foundation.  All rights
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
	/// A stemmer for Dutch words. The algorithm is an implementation of
	/// the <see c="http://snowball.tartarus.org/dutch/stemmer.html">dutch stemming</see>
	/// algorithm in snowball. Snowball is a project of Martin Porter (does Porter Stemmer ring a bell?): 
	/// 
	/// @version   $Id: DutchStemmer.java,v 1.1 2004/03/09 14:55:08 otis Exp $
	/// </summary>
	/// <author>Edwin de Jonge (ejne@cbs.nl)</author>
	public class DutchStemmer
	{
		/// <summary>
		/// Buffer for the terms while stemming them. 
		/// </summary>
		private StringBuilder sb = new StringBuilder();
		private bool _removedE;
		private Hashtable _stemDict;


		private int _R1;
		private int _R2;

		/// <summary>
		/// Stemms the given term to an unique <tt>discriminator</tt>.
		/// </summary>
		/// <param name="term">The term that should be stemmed.</param>
		/// <returns>Discriminator for <tt>term</tt></returns>
		//TODO convert to internal
		public string Stem( String term )
		{
			term = term.ToLower();
			if ( !IsStemmable( term ) )
				return term;
			if (_stemDict != null && _stemDict.Contains(term))
				return _stemDict[term] as string;
			// Reset the StringBuilder.
			sb.Remove(0, sb.Length);
			sb.Insert(0, term);
			// Stemming starts here...
			Substitute(sb);
			StoreYandI(sb);
			_R1 = GetRIndex(sb, 0);
			_R1 = Math.Max(3,_R1);
			Step1(sb);
			Step2(sb);
			_R2 = GetRIndex(sb, _R1);
			Step3a(sb);
			Step3b(sb);
			Step4(sb);
			ReStoreYandI(sb);
			return sb.ToString();
		}

		private bool enEnding(StringBuilder sb)
		{
			string[] enend = new string[]{"ene","en"};
			foreach(string end in enend)
			{
				string s = sb.ToString();
				int index = s.Length - end.Length;
				if ( s.EndsWith(end) &&
					  index >= _R1 && 
					  IsValidEnEnding(sb,index-1) 
					)
				{
					sb.Remove(index, end.Length);
					UnDouble(sb,index);
					return true;
				}
			}
			return false;
		}


		private void Step1(StringBuilder sb)
		{
			if (_R1 >= sb.Length)
				return;

			string s = sb.ToString();
			int lengthR1 = sb.Length - _R1;
			int index;

			if (s.EndsWith("heden"))
			{
				sb.Replace("heden","heid", _R1, lengthR1);
				return;
			}

			if (enEnding(sb))
				return;
			
			if (s.EndsWith("se")              && 
				 (index = s.Length - 2) >= _R1  &&
				 IsValidSEnding(sb, index -1)
				)
			{
				sb.Remove(index, 2);
				return;
			} 
			if (s.EndsWith("s") && 
				(index = s.Length - 1) >= _R1  &&
				IsValidSEnding(sb, index - 1))
			{
				sb.Remove(index, 1);
			}
		}

		/// <summary>
		/// Delete suffix e if in R1 and 
		/// preceded by a non-vowel, and then undouble the ending
		/// </summary>
		/// <param name="sb">string being stemmed</param>
		private void Step2(StringBuilder sb)
		{
			_removedE = false;
			if (_R1 >= sb.Length)
				return;
			string s = sb.ToString();
			int index = s.Length - 1;
			if ( index >= _R1   && 
				 s.EndsWith("e") &&
				 !IsVowel(sb[index-1]))
			{
				sb.Remove(index,1);
				UnDouble(sb);
				_removedE = true;
			}
		}

		/// <summary>
		/// Delete "heid"
		/// </summary>
		/// <param name="sb">string being stemmed</param>
		private void Step3a(StringBuilder sb)
		{
			if (_R2 >= sb.Length)
				return;
			string s = sb.ToString();
			int index = s.Length - 4;
			if (s.EndsWith("heid")&& index >= _R2 && sb[index - 1] != 'c')
			{
				sb.Remove(index,4); //remove heid
				enEnding(sb);
			}
		}

		/// <summary>
		/// <p>A d-suffix, or derivational suffix, enables a new word, 
		/// often with a different grammatical category, or with a different 
		/// sense, to be built from another word. Whether a d-suffix can be 
		/// attached is discovered not from the rules of grammar, but by 
		/// referring to a dictionary. So in English, ness can be added to 
		/// certain adjectives to form corresponding nouns (littleness, 
		/// kindness, foolishness ...) but not to all adjectives 
		/// (not for example, to big, cruel, wise ...) d-suffixes can be 
		/// used to change meaning, often in rather exotic ways.</p>
		/// Remove "ing", "end", "ig", "lijk", "baar" and "bar"
		/// </summary>
		/// <param name="sb">string being stemmed</param>
		private void Step3b(StringBuilder sb)
		{
			if (_R2 >= sb.Length)
				return;
			string s = sb.ToString();
			int index;

			if ((s.EndsWith("end") || s.EndsWith("ing")) &&
      		 (index = s.Length - 3) >= _R2
				)
			{
				sb.Remove(index,3);
				if (sb[index - 2] == 'i' && 
					 sb[index - 1] == 'g')
				{
					if (sb[index - 3] != 'e' & index-2 >= _R2)
					{
						index -= 2;
						sb.Remove(index,2);
					}
				}
				else
				{
					UnDouble(sb,index);
				}
				return;
			}
			if ( s.EndsWith("ig")    &&
				  (index = s.Length - 2) >= _R2
				)
			{
				if (sb[index - 1] != 'e')
					sb.Remove(index, 2);
				return;
			}
			if (s.EndsWith("lijk") &&
				 (index = s.Length - 4) >= _R2
				)
			{
				sb.Remove(index, 4);
				Step2(sb);
				return;
			}
			if (s.EndsWith("baar") &&
				(index = s.Length - 4) >= _R2
				)
			{
				sb.Remove(index, 4);
				return;
			}
			if (s.EndsWith("bar")  &&
				 (index = s.Length - 3) >= _R2
				)
			{
				if (_removedE)
					sb.Remove(index, 3);
				return;
			}
		}

		/// <summary>
		/// undouble vowel 
		/// If the words ends CVD, where C is a non-vowel, D is a non-vowel other than I, and V is double a, e, o or u, remove one of the vowels from V (for example, maan -> man, brood -> brod). 
		/// </summary>
		/// <param name="sb">string being stemmed</param>
		private void Step4(StringBuilder sb)
		{
			if (sb.Length < 4)
				return;
			string end = sb.ToString(sb.Length - 4,4);
			char c = end[0];
			char v1 = end[1];
			char v2 = end[2];
			char d = end[3];
			if (v1 == v2    &&
				 d != 'I'    &&
				 v1 != 'i'    &&
				 IsVowel(v1) &&
				!IsVowel(d)  &&
				!IsVowel(c))
			{
				sb.Remove(sb.Length - 2, 1);
			}
		}

		/// <summary>
		/// Checks if a term could be stemmed.
		/// </summary>
		/// <param name="term"></param>
		/// <returns>true if, and only if, the given term consists in letters.</returns>
		private bool IsStemmable( String term )
		{
			for ( int c = 0; c < term.Length; c++ ) 
			{
				if ( !Char.IsLetter(term[c])) return false;
			}
			return true;
		}

		/// <summary>
		/// Substitute ä, ë, ï, ö, ü, á , é, í, ó, ú
		/// </summary>
		/// <param name="buffer"></param>
		private void Substitute( StringBuilder buffer )
		{
			for ( int i = 0; i < buffer.Length; i++ ) 
			{
				switch (buffer[i])
				{
					case 'ä':
					case 'á':
					{
						buffer[i] = 'a';
						break;
					}
					case 'ë':
					case 'é':
					{
						buffer[i] = 'e';
						break;
					}
					case 'ü':
					case 'ú':
					{
						buffer[i] = 'u';
						break;
					}
					case 'ï':
					case 'i':
					{
						buffer[i] = 'i';
						break;
					}
					case 'ö':
					case 'ó':
					{
						buffer[i] = 'o';
						break;
					}
				}
			}
		}

//		private bool IsValidSEnding(StringBuilder sb)
//		{
//			return  IsValidSEnding(sb,sb.Length - 1);
//		}

		private bool IsValidSEnding(StringBuilder sb, int index)
		{
			char c = sb[index];
			if (IsVowel(c) || c == 'j')
				return false;
			return true;
		}

//		private bool IsValidEnEnding(StringBuilder sb)
//		{
//			return IsValidEnEnding(sb,sb.Length - 1);
//		}

		private bool IsValidEnEnding(StringBuilder sb, int index)
		{
			char c = sb[index];
			if (IsVowel(c))
				return false;
			if (c < 3)
				return false;
			// ends with "gem"?
			if (c == 'm' && sb[index - 2] == 'g' && sb[index-1] == 'e')
				return false;
			return true;
		}

		private void UnDouble(StringBuilder sb)
		{
			UnDouble(sb, sb.Length);
		}

		private void UnDouble(StringBuilder sb, int endIndex)
		{
			string s = sb.ToString(0, endIndex);
			if (s.EndsWith("kk") || s.EndsWith("tt") || s.EndsWith("dd") || s.EndsWith("nn") || s.EndsWith("mm") || s.EndsWith("ff"))
			{
				sb.Remove(endIndex-1,1);
			}
		}

		private int GetRIndex(StringBuilder sb, int start)
		{
			if (start == 0) 
				start = 1;
			int i = start;
			for (; i < sb.Length; i++)
			{
				//first non-vowel preceded by a vowel
				if (!IsVowel(sb[i]) && IsVowel(sb[i-1]))
				{
					return i + 1;
				}
			}
			return i + 1;
		}

		private void StoreYandI(StringBuilder sb)
		{
			if (sb[0] == 'y')
				sb[0] = 'Y';
			//char c;
			int last = sb.Length - 1;
			for (int i = 1; i < last; i++)
			{
				switch (sb[i])
				{
					case 'i':
					{
						if (IsVowel(sb[i-1]) && 
							IsVowel(sb[i+1])
							)
							sb[i] = 'I';
						break;
					}
					case 'y':
					{
						if (IsVowel(sb[i-1]))
							sb[i] = 'Y';
						break;
					}
				}
			}
			if (last > 0 && sb[last]=='y' && IsVowel(sb[last-1]))
				sb[last]='Y';
		}

		private void ReStoreYandI(StringBuilder sb)
		{
			sb.Replace("I","i");
			sb.Replace("Y","y");
		}

		private bool IsVowel(char c)
		{
			switch (c)
			{
				case 'e':
				case 'a':
				case 'o':
				case 'i':
				case 'u':
				case 'y':
				case 'è':
				{
					return true;
				}
			}
			return false;
		}

		internal void SetStemDictionary(Hashtable dict)
		{
			_stemDict = dict;
		}
	}
}