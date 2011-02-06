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
using IndexReader = Lucene.Net.Index.IndexReader;
using Term = Lucene.Net.Index.Term;
using FilteredTermEnum = Lucene.Net.Search.FilteredTermEnum;
using Pattern = System.Text.RegularExpressions.Regex;

namespace Lucene.Net.Search.Regex
{
	
	public class RegexTermEnum : FilteredTermEnum
	{
		private System.String field = "";
		private System.String pre = "";
		internal bool endEnum = false;
		private Pattern pattern;
		
		public RegexTermEnum(IndexReader reader, Term term) : base()
		{
			field = term.Field();
			System.String text = term.Text();
			
			pattern = new Pattern(text);
			
			// Find the first regex character position, to find the
			// maximum prefix to use for term enumeration
			int index = 0;
			while (index < text.Length)
			{
				char c = text[index];
				
				if (!System.Char.IsLetterOrDigit(c))
					break;
				
				index++;
			}
			
			pre = text.Substring(0, (index) - (0));
			
			SetEnum(reader.Terms(new Term(term.Field(), pre)));
		}
		
		protected internal override bool TermCompare(Term term)
		{
			if ((System.Object) field == (System.Object) term.Field())
			{
				System.String searchText = term.Text();
				if (searchText.StartsWith(pre))
				{
                    return pattern.Match(searchText).Success;
				}
			}
			endEnum = true;
			return false;
		}
		
		public override float Difference()
		{
			// TODO: adjust difference based on distance of searchTerm.text() and term().text()
			return 1.0f;
		}
		
		public override bool EndEnum()
		{
			return endEnum;
		}
		
		public override void  Close()
		{
			base.Close();
			field = null;
		}
	}
}