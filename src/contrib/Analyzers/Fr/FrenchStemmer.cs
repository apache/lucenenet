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
using System.Text;

namespace Lucene.Net.Analysis.Fr
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
	/// A stemmer for French words. The algorithm is based on the work of
	/// Dr Martin Porter on his snowball project<br/>
	/// refer to http://snowball.sourceforge.net/french/stemmer.html<br/>
	/// (French stemming algorithm) for details
	/// 
	/// <author>Patrick Talbot (based on Gerhard Schwarz work for German)</author>
	/// <version>$Id: FrenchStemmer.java,v 1.2 2004/01/22 20:54:47 ehatcher Exp $</version>
	/// </summary>
	public class FrenchStemmer 
	{

		/// <summary>
		/// Buffer for the terms while stemming them.
		/// </summary>
		private StringBuilder sb = new StringBuilder();

		/// <summary>
		/// A temporary buffer, used to reconstruct R2
		/// </summary>
		private StringBuilder tb = new StringBuilder();

		/// <summary>
		/// Region R0 is equal to the whole buffer
		/// </summary>
		private String R0;

		/// <summary>
		/// Region RV
		/// "If the word begins with two vowels, RV is the region after the third letter,
		/// otherwise the region after the first vowel not at the beginning of the word,
		/// or the end of the word if these positions cannot be found."
		/// </summary>
		private String RV;

		/// <summary>
		/// Region R1
		/// "R1 is the region after the first non-vowel following a vowel
		/// or is the null region at the end of the word if there is no such non-vowel"
		/// </summary>
		private String R1;

		/// <summary>
		/// Region R2
		/// "R2 is the region after the first non-vowel in R1 following a vowel
		/// or is the null region at the end of the word if there is no such non-vowel"
		/// </summary>
		private String R2;


		/// <summary>
		/// Set to true if we need to perform step 2
		/// </summary>
		private bool suite;

		/// <summary>
		/// Set to true if the buffer was modified
		/// </summary>
		private bool modified;

		/// <summary>
		/// Stemms the given term to a unique <tt>discriminator</tt>.
		/// </summary>
		/// <param name="term">
		/// java.langString The term that should be stemmed
		/// </param>
		/// <returns>
		/// Discriminator for <tt>term</tt>
		/// </returns>
		protected internal String Stem( String term ) 
		{
			if ( !IsStemmable( term ) ) 
			{
				return term;
			}

			// Use lowercase for medium stemming.
			term = term.ToLower();

			// Reset the StringBuilder.
			sb.Remove( 0, sb.Length );
			sb.Append( term );

			// reset the booleans
			modified = false;
			suite = false;

			sb = TreatVowels( sb );

			SetStrings();

			Step1();

			if (!modified || suite)
			{
				if (RV != null)
				{
					suite = Step2a();
					if (!suite)
						Step2b();
				}
			}

			if (modified || suite)
				Step3();
			else
				Step4();

			Step5();

			Step6();

			return sb.ToString();
		}

		/// <summary>
		/// Sets the search region Strings<br/>
		/// it needs to be done each time the buffer was modified
		/// </summary>
		private void SetStrings() 
		{
			// set the strings
			R0 = sb.ToString();
			RV = RetrieveRV( sb );
			R1 = RetrieveR( sb );
			if ( R1 != null )
			{
				tb.Remove( 0, tb.Length );
				tb.Append( R1 );
				R2 = RetrieveR( tb );
			}
			else
				R2 = null;
		}

		/// <summary>
		/// First step of the Porter Algorithmn<br/>
		/// refer to http://snowball.sourceforge.net/french/stemmer.html for an explanation
		/// </summary>
		private void Step1( ) 
		{
			String[] suffix = { "ances", "iqUes", "ismes", "ables", "istes", "ance", "iqUe", "isme", "able", "iste" };
			DeleteFrom( R2, suffix );

			ReplaceFrom( R2, new String[] { "logies", "logie" }, "log" );
			ReplaceFrom( R2, new String[] { "usions", "utions", "usion", "ution" }, "u" );
			ReplaceFrom( R2, new String[] { "ences", "ence" }, "ent" );

			String[] search = { "atrices", "ateurs", "ations", "atrice", "ateur", "ation"};
			DeleteButSuffixFromElseReplace( R2, search, "ic",  true, R0, "iqU" );

			DeleteButSuffixFromElseReplace( R2, new String[] { "ements", "ement" }, "eus", false, R0, "eux" );
			DeleteButSuffixFrom( R2, new String[] { "ements", "ement" }, "ativ", false );
			DeleteButSuffixFrom( R2, new String[] { "ements", "ement" }, "iv", false );
			DeleteButSuffixFrom( R2, new String[] { "ements", "ement" }, "abl", false );
			DeleteButSuffixFrom( R2, new String[] { "ements", "ement" }, "iqU", false );

			DeleteFromIfTestVowelBeforeIn( R1, new String[] { "issements", "issement" }, false, R0 );
			DeleteFrom( RV, new String[] { "ements", "ement" } );

			DeleteButSuffixFromElseReplace( R2, new String[] { "ités", "ité" }, "abil", false, R0, "abl" );
			DeleteButSuffixFromElseReplace( R2, new String[] { "ités", "ité" }, "ic", false, R0, "iqU" );
			DeleteButSuffixFrom( R2, new String[] { "ités", "ité" }, "iv", true );

			String[] autre = { "ifs", "ives", "if", "ive" };
			DeleteButSuffixFromElseReplace( R2, autre, "icat", false, R0, "iqU" );
			DeleteButSuffixFromElseReplace( R2, autre, "at", true, R2, "iqU" );

			ReplaceFrom( R0, new String[] { "eaux" }, "eau" );

			ReplaceFrom( R1, new String[] { "aux" }, "al" );

			DeleteButSuffixFromElseReplace( R2, new String[] { "euses", "euse" }, "", true, R1, "eux" );

			DeleteFrom( R2, new String[] { "eux" } );

			// if one of the next steps is performed, we will need to perform step2a
			bool temp = false;
			temp = ReplaceFrom( RV, new String[] { "amment" }, "ant" );
			if (temp == true)
				suite = true;
			temp = ReplaceFrom( RV, new String[] { "emment" }, "ent" );
			if (temp == true)
				suite = true;
			temp = DeleteFromIfTestVowelBeforeIn( RV, new String[] { "ments", "ment" }, true, RV );
			if (temp == true)
				suite = true;

		}

		/// <summary>
		/// Second step (A) of the Porter Algorithmn<br/>
		/// Will be performed if nothing changed from the first step
		/// or changed were done in the amment, emment, ments or ment suffixes<br/>
		/// refer to http://snowball.sourceforge.net/french/stemmer.html for an explanation
		/// </summary>
		/// <returns>
		/// true if something changed in the StringBuilder
		/// </returns>
		private bool Step2a() 
		{
			String[] search = { "îmes", "îtes", "iraIent", "irait", "irais", "irai", "iras", "ira",
								  "irent", "iriez", "irez", "irions", "irons", "iront",
								  "issaIent", "issais", "issantes", "issante", "issants", "issant",
								  "issait", "issais", "issions", "issons", "issiez", "issez", "issent",
								  "isses", "isse", "ir", "is", "ît", "it", "ies", "ie", "i" };
			return DeleteFromIfTestVowelBeforeIn( RV, search, false, RV );
		}

		/// <summary>
		/// Second step (B) of the Porter Algorithmn<br/>
		/// Will be performed if step 2 A was performed unsuccessfully<br/>
		/// refer to http://snowball.sourceforge.net/french/stemmer.html for an explanation
		/// </summary>
		private void Step2b() 
		{
			String[] suffix = { "eraIent", "erais", "erait", "erai", "eras", "erions", "eriez",
								  "erons", "eront","erez", "èrent", "era", "ées", "iez",
								  "ée", "és", "er", "ez", "é" };
			DeleteFrom( RV, suffix );

			String[] search = { "assions", "assiez", "assent", "asses", "asse", "aIent",
								  "antes", "aIent", "Aient", "ante", "âmes", "âtes", "ants", "ant",
								  "ait", "aît", "ais", "Ait", "Aît", "Ais", "ât", "as", "ai", "Ai", "a" };
			DeleteButSuffixFrom( RV, search, "e", true );

			DeleteFrom( R2, new String[] { "ions" } );
		}

		/// <summary>
		/// Third step of the Porter Algorithmn<br/>
		/// refer to http://snowball.sourceforge.net/french/stemmer.html for an explanation
		/// </summary>
		private void Step3() 
		{
			if (sb.Length>0)
			{
				char ch = sb[ sb.Length-1];
				if (ch == 'Y')
				{
					sb[ sb.Length-1] = 'i';
					SetStrings();
				}
				else if (ch == 'ç')
				{
					sb[ sb.Length-1] = 'c';
					SetStrings();
				}
			}
		}

		/// <summary>
		/// Fourth step of the Porter Algorithmn<br/>
		/// refer to http://snowball.sourceforge.net/french/stemmer.html for an explanation
		/// </summary>
		private void Step4() 
		{
			if (sb.Length > 1)
			{
				char ch = sb[sb.Length-1];
				if (ch == 's')
				{
					char b = sb[ sb.Length-2 ];
					if (b != 'a' && b != 'i' && b != 'o' && b != 'u' && b != 'è' && b != 's')
					{
						sb.Remove( sb.Length - 1, 1);
						SetStrings();
					}
				}
			}
			bool found = DeleteFromIfPrecededIn( R2, new String[] { "ion" }, RV, "s" );
			if (!found)
				found = DeleteFromIfPrecededIn( R2, new String[] { "ion" }, RV, "t" );

			ReplaceFrom( RV, new String[] { "Ière", "ière", "Ier", "ier" }, "i" );
			DeleteFrom( RV, new String[] { "e" } );
			DeleteFromIfPrecededIn( RV, new String[] { "ë" }, R0, "gu" );
		}

		/// <summary>
		/// Fifth step of the Porter Algorithmn<br/>
		/// refer to http://snowball.sourceforge.net/french/stemmer.html for an explanation
		/// </summary>
		private void Step5() 
		{
			if (R0 != null)
			{
				if (R0.EndsWith("enn") || R0.EndsWith("onn") || R0.EndsWith("ett") || R0.EndsWith("ell") || R0.EndsWith("eill"))
				{
					sb.Remove( sb.Length - 1, 1);
					SetStrings();
				}
			}
		}

		/// <summary>
		/// Sixth (and last!) step of the Porter Algorithmn<br/>
		/// refer to http://snowball.sourceforge.net/french/stemmer.html for an explanation
		/// </summary>
		private void Step6() 
		{
			if (R0!=null && R0.Length>0)
			{
				bool seenVowel = false;
				bool seenConson = false;
				int pos = -1;
				for (int i = R0.Length-1; i > -1; i--)
				{
					char ch = R0[i];
					if (IsVowel(ch))
					{
						if (!seenVowel)
						{
							if (ch == 'é' || ch == 'è')
							{
								pos = i;
								break;
							}
						}
						seenVowel = true;
					}
					else
					{
						if (seenVowel)
							break;
						else
							seenConson = true;
					}
				}
				if (pos > -1 && seenConson && !seenVowel)
					sb[pos] = 'e';
			}
		}

		/// <summary>
		/// Delete a suffix searched in zone "source" if zone "from" contains prefix + search string
		/// </summary>
		/// <param name="source">the primary source zone for search</param>
		/// <param name="search">the strings to search for suppression</param>
		/// <param name="from">the secondary source zone for search</param>
		/// <param name="prefix">the prefix to add to the search string to test</param>
		/// <returns>true if modified</returns>
		private bool DeleteFromIfPrecededIn( String source, String[] search, String from, String prefix ) 
		{
			bool found = false;
			if (source!=null )
			{
				for (int i = 0; i < search.Length; i++) 
				{
					if ( source.EndsWith( search[i] ))
					{
						if (from!=null && from.EndsWith( prefix + search[i] ))
						{
							sb.Remove( sb.Length - search[i].Length, search[i].Length);
							found = true;
							SetStrings();
							break;
						}
					}
				}
			}
			return found;
		}

		/// <summary>
		/// Delete a suffix searched in zone "source" if the preceding letter is (or isn't) a vowel
		/// </summary>
		/// <param name="source">the primary source zone for search</param>
		/// <param name="search">the strings to search for suppression</param>
		/// <param name="vowel">true if we need a vowel before the search string</param>
		/// <param name="from">the secondary source zone for search (where vowel could be)</param>
		/// <returns>true if modified</returns>
		private bool DeleteFromIfTestVowelBeforeIn( String source, String[] search, bool vowel, String from ) 
		{
			bool found = false;
			if (source!=null && from!=null)
			{
				for (int i = 0; i < search.Length; i++) 
				{
					if ( source.EndsWith( search[i] ))
					{
						if ((search[i].Length + 1) <= from.Length)
						{
							bool test = IsVowel(sb[sb.Length-(search[i].Length+1)]);
							if (test == vowel)
							{
								sb.Remove( sb.Length - search[i].Length, search[i].Length);
								modified = true;
								found = true;
								SetStrings();
								break;
							}
						}
					}
				}
			}
			return found;
		}

		/// <summary>
		/// Delete a suffix searched in zone "source" if preceded by the prefix
		/// </summary>
		/// <param name="source">the primary source zone for search</param>
		/// <param name="search">the strings to search for suppression</param>
		/// <param name="prefix">the prefix to add to the search string to test</param>
		/// <param name="without">true if it will be deleted even without prefix found</param>
		private void DeleteButSuffixFrom( String source, String[] search, String prefix, bool without ) 
		{
			if (source!=null)
			{
				for (int i = 0; i < search.Length; i++) 
				{
					if ( source.EndsWith( prefix + search[i] ))
					{
						sb.Remove( sb.Length - (prefix.Length + search[i].Length), prefix.Length + search[i].Length);
						modified = true;
						SetStrings();
						break;
					}
					else if ( without && source.EndsWith( search[i] ))
					{
						sb.Remove( sb.Length - search[i].Length, search[i].Length);
						modified = true;
						SetStrings();
						break;
					}
				}
			}
		}

		/// <summary>
		/// Delete a suffix searched in zone "source" if preceded by prefix<br/>
		/// or replace it with the replace string if preceded by the prefix in the zone "from"<br/>
		/// or delete the suffix if specified
		/// </summary>
		/// <param name="source">the primary source zone for search</param>
		/// <param name="search">the strings to search for suppression</param>
		/// <param name="prefix">the prefix to add to the search string to test</param>
		/// <param name="without">true if it will be deleted even without prefix found</param>
		private void DeleteButSuffixFromElseReplace( String source, String[] search, String prefix, bool without, String from, String replace ) 
		{
			if (source!=null)
			{
				for (int i = 0; i < search.Length; i++) 
				{
					if ( source.EndsWith( prefix + search[i] ))
					{
						sb.Remove( sb.Length - (prefix.Length + search[i].Length), prefix.Length + search[i].Length);
						modified = true;
						SetStrings();
						break;
					}
					else if ( from!=null && from.EndsWith( prefix + search[i] ))
					{
						sb.Remove(sb.Length - (prefix.Length + search[i].Length), prefix.Length + search[i].Length);
						sb.Append( replace );
						modified = true;
						SetStrings();
						break;
					}
					else if ( without && source.EndsWith( search[i] ))
					{
						sb.Remove( sb.Length - search[i].Length, search[i].Length );
						modified = true;
						SetStrings();
						break;
					}
				}
			}
		}

		/// <summary>
		/// Replace a search string with another within the source zone
		/// </summary>
		/// <param name="source">the source zone for search</param>
		/// <param name="search">the strings to search for replacement</param>
		/// <param name="replace">the replacement string</param>
		/// <returns></returns>
		private bool ReplaceFrom( String source, String[] search, String replace ) 
		{
			bool found = false;
			if (source!=null)
			{
				for (int i = 0; i < search.Length; i++) 
				{
					if ( source.EndsWith( search[i] ))
					{
						sb.Remove(sb.Length - search[i].Length, search[i].Length);
						sb.Append( replace );
						modified = true;
						found = true;
						SetStrings();
						break;
					}
				}
			}
			return found;
		}

		/// <summary>
		/// Delete a search string within the source zone
		/// </summary>
		/// <param name="source">the source zone for search</param>
		/// <param name="suffix">the strings to search for suppression</param>
		private void DeleteFrom(String source, String[] suffix ) 
		{
			if (source!=null)
			{
				for (int i = 0; i < suffix.Length; i++) 
				{
					if (source.EndsWith( suffix[i] ))
					{
						sb.Remove( sb.Length - suffix[i].Length, suffix[i].Length);
						modified = true;
						SetStrings();
						break;
					}
				}
			}
		}

		/// <summary>
		/// Test if a char is a french vowel, including accentuated ones
		/// </summary>
		/// <param name="ch">the char to test</param>
		/// <returns>true if the char is a vowel</returns>
		private bool IsVowel(char ch) 
		{
			switch (ch)
			{
				case 'a':
				case 'e':
				case 'i':
				case 'o':
				case 'u':
				case 'y':
				case 'â':
				case 'à':
				case 'ë':
				case 'é':
				case 'ê':
				case 'è':
				case 'ï':
				case 'î':
				case 'ô':
				case 'ü':
				case 'ù':
				case 'û':
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Retrieve the "R zone" (1 or 2 depending on the buffer) and return the corresponding string<br/>
		/// "R is the region after the first non-vowel following a vowel
		/// or is the null region at the end of the word if there is no such non-vowel"<br/>
		/// </summary>
		/// <param name="buffer">the in buffer</param>
		/// <returns>the resulting string</returns>
		private String RetrieveR( StringBuilder buffer ) 
		{
			int len = buffer.Length;
			int pos = -1;
			for (int c = 0; c < len; c++) 
			{
				if (IsVowel( buffer[c]))
				{
					pos = c;
					break;
				}
			}
			if (pos > -1)
			{
				int consonne = -1;
				for (int c = pos; c < len; c++) 
				{
					if (!IsVowel(buffer[c]))
					{
						consonne = c;
						break;
					}
				}
				if (consonne > -1 && (consonne+1) < len)
					return buffer.ToString().Substring( consonne+1, len - (consonne+1) );
				else
					return null;
			}
			else
				return null;
		}

		/// <summary>
		/// Retrieve the "RV zone" from a buffer an return the corresponding string<br/>
		/// "If the word begins with two vowels, RV is the region after the third letter,
		/// otherwise the region after the first vowel not at the beginning of the word,
		/// or the end of the word if these positions cannot be found."<br/>
		/// </summary>
		/// <param name="buffer">the in buffer</param>
		/// <returns>the resulting string</returns>
		private String RetrieveRV( StringBuilder buffer ) 
		{
			int len = buffer.Length;
			if ( buffer.Length > 3)
			{
				if ( IsVowel(buffer[0]) && IsVowel(buffer[1])) 
				{
					return buffer.ToString().Substring(3,len-3);
				}
				else
				{
					int pos = 0;
					for (int c = 1; c < len; c++) 
					{
						if (IsVowel( buffer[c]))
						{
							pos = c;
							break;
						}
					}
					if ( pos+1 < len )
						return buffer.ToString().Substring( pos+1, len - (pos+1));
					else
						return null;
				}
			}
			else
				return null;
		}


		/// <summary>
		/// Turns u and i preceded AND followed by a vowel to UpperCase<br/>
		/// Turns y preceded OR followed by a vowel to UpperCase<br/>
		/// Turns u preceded by q to UpperCase<br/>
		/// </summary>
		/// <param name="buffer">the buffer to treat</param>
		/// <returns>the treated buffer</returns>
		private StringBuilder TreatVowels( StringBuilder buffer ) 
		{
			for ( int c = 0; c < buffer.Length; c++ ) 
			{
				char ch = buffer[c];

				if (c == 0) // first char
				{
					if (buffer.Length>1)
					{
						if (ch == 'y' && IsVowel(buffer[ c + 1 ]))
							buffer[c] = 'Y';
					}
				}
				else if (c == buffer.Length-1) // last char
				{
					if (ch == 'u' && buffer[c - 1] == 'q')
						buffer[c] = 'U';
					if (ch == 'y' && IsVowel(buffer[ c - 1 ]))
						buffer[c] = 'Y';
				}
				else // other cases
				{
					if (ch == 'u')
					{
						if (buffer[ c - 1] == 'q')
							buffer[ c ] = 'U';
						else if (IsVowel(buffer[c - 1]) && IsVowel(buffer[c + 1]))
							buffer[c] = 'U';
					}
					if (ch == 'i')
					{
						if (IsVowel(buffer[c - 1]) && IsVowel(buffer[ c + 1 ]))
							buffer[c] = 'I';
					}
					if (ch == 'y')
					{
						if (IsVowel(buffer[c - 1]) || IsVowel(buffer[c + 1]))
							buffer[c] = 'Y';
					}
				}
			}

			return buffer;
		}

		/// <summary>
		/// Checks a term if it can be processed correctly.
		/// </summary>
		/// <returns>true if, and only if, the given term consists in letters.</returns>
		private bool IsStemmable( String term ) 
		{
			bool upper = false;
			int first = -1;
			for ( int c = 0; c < term.Length; c++ ) 
			{
				// Discard terms that contain non-letter characters.
				if ( !Char.IsLetter( term[c] ) ) 
				{
					return false;
				}
				// Discard terms that contain multiple uppercase letters.
				if ( Char.IsUpper( term[c] ) ) 
				{
					if ( upper ) 
					{
						return false;
					}
						// First encountered uppercase letter, set flag and save
						// position.
					else 
					{
						first = c;
						upper = true;
					}
				}
			}
			// Discard the term if it contains a single uppercase letter that
			// is not starting the term.
			if ( first > 0 ) 
			{
				return false;
			}
			return true;
		}
	}
}
