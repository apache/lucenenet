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

namespace Lucene.Net.Analysis
{
	
	
	/// <summary> A SinkTokenizer can be used to cache Tokens for use in an Analyzer
	/// 
	/// </summary>
	/// <seealso cref="TeeTokenFilter">
	/// 
	/// 
	/// </seealso>
	public class SinkTokenizer : Tokenizer
	{
		protected internal System.Collections.IList lst = new System.Collections.ArrayList();
		protected internal System.Collections.IEnumerator iter;
		
		public SinkTokenizer(System.Collections.IList input)
		{
			this.lst = input;
			if (this.lst == null)
				this.lst = new System.Collections.ArrayList();
		}
		
		public SinkTokenizer()
		{
			this.lst = new System.Collections.ArrayList();
		}
		
		public SinkTokenizer(int initCap)
		{
			this.lst = new System.Collections.ArrayList(initCap);
		}
		
		/// <summary> Get the tokens in the internal List.
		/// <p/>
		/// WARNING: Adding tokens to this list requires the {@link #Reset()} method to be called in order for them
		/// to be made available.  Also, this Tokenizer does nothing to protect against {@link java.util.ConcurrentModificationException}s
		/// in the case of adds happening while {@link #Next(Lucene.Net.Analysis.Token)} is being called.
		/// 
		/// </summary>
		/// <returns> A List of {@link Lucene.Net.Analysis.Token}s
		/// </returns>
		public virtual System.Collections.IList GetTokens()
		{
			return lst;
		}
		
		/// <summary> Returns the next token out of the list of cached tokens</summary>
		/// <returns> The next {@link Lucene.Net.Analysis.Token} in the Sink.
		/// </returns>
		/// <throws>  IOException </throws>
		public override Token Next()
		{
			if (iter == null)
				iter = lst.GetEnumerator();
			return iter.MoveNext() ? (Token) iter.Current : null;
		}
		
		
		
		/// <summary> Override this method to cache only certain tokens, or new tokens based
		/// on the old tokens.
		/// 
		/// </summary>
		/// <param name="t">The {@link Lucene.Net.Analysis.Token} to add to the sink
		/// </param>
		public virtual void  Add(Token t)
		{
			if (t == null)
				return ;
			lst.Add((Token) t.Clone());
		}
		
		public override void  Close()
		{
			//nothing to close
			input = null;
			lst = null;
		}
		
		/// <summary> Reset the internal data structures to the start at the front of the list of tokens.  Should be called
		/// if tokens were added to the list after an invocation of {@link #Next(Token)}
		/// </summary>
		/// <throws>  IOException </throws>
		public override void  Reset()
		{
			iter = lst.GetEnumerator();
		}
	}
}