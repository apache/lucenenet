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

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.De;
using Lucene.Net.Analysis.Standard;

namespace Lucene.Net.Analysis.Cz
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
	/// Analyzer for Czech language. Supports an external list of stopwords (words that
	/// will not be indexed at all).
	/// A default set of stopwords is used unless an alternative list is specified, the
	/// exclusion list is empty by default.
	/// 
	/// <author>Lukas Zapletal [lzap@root.cz]</author>
	/// <version>$Id: CzechAnalyzer.java,v 1.2 2003/01/22 20:54:47 ehatcher Exp $</version>
	/// </summary>
	public sealed class CzechAnalyzer : Analyzer 
	{
		/// <summary>
		/// List of typical stopwords.
		/// </summary>
		public static String[] STOP_WORDS = 
				 {
					 "a","s","k","o","i","u","v","z","dnes","cz","t\u00edmto","bude\u0161","budem",
					 "byli","jse\u0161","m\u016fj","sv\u00fdm","ta","tomto","tohle","tuto","tyto",
					 "jej","zda","pro\u010d","m\u00e1te","tato","kam","tohoto","kdo","kte\u0159\u00ed",
					 "mi","n\u00e1m","tom","tomuto","m\u00edt","nic","proto","kterou","byla",
					 "toho","proto\u017ee","asi","ho","na\u0161i","napi\u0161te","re","co\u017e","t\u00edm",
					 "tak\u017ee","sv\u00fdch","jej\u00ed","sv\u00fdmi","jste","aj","tu","tedy","teto",
					 "bylo","kde","ke","prav\u00e9","ji","nad","nejsou","\u010di","pod","t\u00e9ma",
					 "mezi","p\u0159es","ty","pak","v\u00e1m","ani","kdy\u017e","v\u0161ak","neg","jsem",
					 "tento","\u010dl\u00e1nku","\u010dl\u00e1nky","aby","jsme","p\u0159ed","pta","jejich",
					 "byl","je\u0161t\u011b","a\u017e","bez","tak\u00e9","pouze","prvn\u00ed","va\u0161e","kter\u00e1",
					 "n\u00e1s","nov\u00fd","tipy","pokud","m\u016f\u017ee","strana","jeho","sv\u00e9","jin\u00e9",
					 "zpr\u00e1vy","nov\u00e9","nen\u00ed","v\u00e1s","jen","podle","zde","u\u017e","b\u00fdt","v\u00edce",
					 "bude","ji\u017e","ne\u017e","kter\u00fd","by","kter\u00e9","co","nebo","ten","tak",
					 "m\u00e1","p\u0159i","od","po","jsou","jak","dal\u0161\u00ed","ale","si","se","ve",
					 "to","jako","za","zp\u011bt","ze","do","pro","je","na","atd","atp",
					 "jakmile","p\u0159i\u010dem\u017e","j\u00e1","on","ona","ono","oni","ony","my","vy",
					 "j\u00ed","ji","m\u011b","mne","jemu","tomu","t\u011bm","t\u011bmu","n\u011bmu","n\u011bmu\u017e",
					 "jeho\u017e","j\u00ed\u017e","jeliko\u017e","je\u017e","jako\u017e","na\u010de\u017e",
		};

		/// <summary>
		/// Contains the stopwords used with the StopFilter.
		/// </summary>
		private Hashtable stoptable = new Hashtable();

		/// <summary>
		/// Builds an analyzer.
		/// </summary>
		public CzechAnalyzer() 
		{
			stoptable = StopFilter.MakeStopSet( STOP_WORDS );
		}

		/// <summary>
		/// Builds an analyzer with the given stop words.
		/// </summary>
		public CzechAnalyzer( String[] stopwords ) 
		{
			stoptable = StopFilter.MakeStopSet( stopwords );
		}

		/// <summary>
		/// Builds an analyzer with the given stop words.
		/// </summary>
		public CzechAnalyzer( Hashtable stopwords ) 
		{
			stoptable = stopwords;
		}

		/// <summary>
		/// Builds an analyzer with the given stop words.
		/// </summary>
		public CzechAnalyzer( FileInfo stopwords ) 
		{
			stoptable = WordlistLoader.GetWordtable( stopwords );
		}

		/// <summary>
		/// Loads stopwords hash from resource stream (file, database...).
		/// </summary>
		/// <param name="wordfile">File containing the wordlist</param>
		/// <param name="encoding">Encoding used (win-1250, iso-8859-2, ...}, null for default system encoding</param>
		public void LoadStopWords( Stream wordfile, String encoding ) 
		{
			if ( wordfile == null ) 
			{
				stoptable = new Hashtable();
				return;
			}
			try 
			{
				// clear any previous table (if present)
				stoptable = new Hashtable();

				StreamReader isr;
				if (encoding == null)
					isr = new StreamReader(wordfile);
				else
					isr = new StreamReader(wordfile, Encoding.GetEncoding(encoding));

				String word;
				while ( ( word = isr.ReadLine() ) != null ) 
				{
					stoptable[word] = word;
				}

			} 
			catch ( IOException ) 
			{
				stoptable = null;
			}
		}

		/// <summary>
		/// Creates a TokenStream which tokenizes all the text in the provided Reader.
		/// </summary>
		/// <returns>
		/// A TokenStream build from a StandardTokenizer filtered with
		/// StandardFilter, StopFilter, GermanStemFilter and LowerCaseFilter
		/// </returns>
		public override TokenStream TokenStream( String fieldName, TextReader reader ) 
		{
			TokenStream result = new StandardTokenizer( reader );
			result = new StandardFilter( result );
			result = new LowerCaseFilter( result );
			result = new StopFilter( result, stoptable );
			return result;
		}
	}
}
