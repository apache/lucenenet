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

using Term = Lucene.Net.Index.Term;
using IndexReader = Lucene.Net.Index.IndexReader;
using TermEnum = Lucene.Net.Index.TermEnum;
using TermDocs = Lucene.Net.Index.TermDocs;

namespace Lucene.Net.Search
{
	
	/// <summary> </summary>
	/// <version>  $Id$
	/// </version>
	[Serializable]
	public class PrefixFilter : Filter
	{
		private class AnonymousClassPrefixGenerator : PrefixGenerator
		{
			private void  InitBlock(System.Collections.BitArray bitSet, PrefixFilter enclosingInstance)
			{
				this.bitSet = bitSet;
				this.enclosingInstance = enclosingInstance;
			}
			private System.Collections.BitArray bitSet;
			private PrefixFilter enclosingInstance;
			public PrefixFilter Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal AnonymousClassPrefixGenerator(System.Collections.BitArray bitSet, PrefixFilter enclosingInstance, Lucene.Net.Index.Term Param1):base(Param1)
			{
				InitBlock(bitSet, enclosingInstance);
			}
			public override void  HandleDoc(int doc)
			{
				bitSet.Set(doc, true);
			}
		}
		
		protected internal Term prefix;
		
		public PrefixFilter(Term prefix)
		{
			this.prefix = prefix;
		}
		
		public virtual Term GetPrefix()
		{
			return prefix;
		}
		
		public override System.Collections.BitArray Bits(IndexReader reader)
		{
			System.Collections.BitArray bitSet = new System.Collections.BitArray((reader.MaxDoc() % 64 == 0 ? reader.MaxDoc() / 64 : reader.MaxDoc() / 64 + 1) * 64);
			new AnonymousClassPrefixGenerator(bitSet, this, prefix).Generate(reader);
			return bitSet;
		}
		
		/// <summary>Prints a user-readable version of this query. </summary>
		public override System.String ToString()
		{
			System.Text.StringBuilder buffer = new System.Text.StringBuilder();
			buffer.Append("PrefixFilter(");
			buffer.Append(prefix.ToString());
			buffer.Append(")");
			return buffer.ToString();
		}
	}
	
	// keep this protected until I decide if it's a good way
	// to separate id generation from collection (or should
	// I just reuse hitcollector???)
	internal interface IdGenerator
	{
		void  Generate(IndexReader reader);
		void  HandleDoc(int doc);
	}
	
	
	abstract class PrefixGenerator : IdGenerator
	{
		protected internal Term prefix;
		
		internal PrefixGenerator(Term prefix)
		{
			this.prefix = prefix;
		}
		
		public virtual void  Generate(IndexReader reader)
		{
			TermEnum enumerator = reader.Terms(prefix);
			TermDocs termDocs = reader.TermDocs();
			
			try
			{
				
				System.String prefixText = prefix.Text();
				System.String prefixField = prefix.Field();
				do 
				{
					Term term = enumerator.Term();
					if (term != null && term.Text().StartsWith(prefixText) && (System.Object) term.Field() == (System.Object) prefixField)
					// interned comparison
					{
						termDocs.Seek(term);
						while (termDocs.Next())
						{
							HandleDoc(termDocs.Doc());
						}
					}
					else
					{
						break;
					}
				}
				while (enumerator.Next());
			}
			finally
			{
				termDocs.Close();
				enumerator.Close();
			}
		}
		public abstract void  HandleDoc(int param1);
	}
}