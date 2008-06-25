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

using Payload = Lucene.Net.Index.Payload;

namespace Lucene.Net.Analysis
{
	
	/// <summary>A TokenStream enumerates the sequence of tokens, either from
	/// fields of a document or from query text.
	/// <p>
	/// This is an abstract class.  Concrete subclasses are:
	/// <ul>
	/// <li>{@link Tokenizer}, a TokenStream
	/// whose input is a Reader; and
	/// <li>{@link TokenFilter}, a TokenStream
	/// whose input is another TokenStream.
	/// </ul>
	/// NOTE: subclasses must override at least one of {@link
	/// #Next()} or {@link #Next(Token)}.
	/// </summary>
	
	public abstract class TokenStream
	{
		
		/// <summary>Returns the next token in the stream, or null at EOS.
		/// The returned Token is a "full private copy" (not
		/// re-used across calls to next()) but will be slower
		/// than calling {@link #Next(Token)} instead.. 
		/// </summary>
		public virtual Token Next()
		{
			Token result = Next(new Token());
			
			if (result != null)
			{
				Payload p = result.GetPayload();
				if (p != null)
				{
					result.SetPayload((Payload) p.Clone());
				}
			}
			
			return result;
		}
		
		/// <summary>Returns the next token in the stream, or null at EOS.
		/// When possible, the input Token should be used as the
		/// returned Token (this gives fastest tokenization
		/// performance), but this is not required and a new Token
		/// may be returned. Callers may re-use a single Token
		/// instance for successive calls to this method.
		/// <p>
		/// This implicitly defines a "contract" between 
		/// consumers (callers of this method) and 
		/// producers (implementations of this method 
		/// that are the source for tokens):
		/// <ul>
		/// <li>A consumer must fully consume the previously 
		/// returned Token before calling this method again.</li>
		/// <li>A producer must call {@link Token#Clear()}
		/// before setting the fields in it & returning it</li>
		/// </ul>
		/// Note that a {@link TokenFilter} is considered a consumer.
		/// </summary>
		/// <param name="result">a Token that may or may not be used to return
		/// </param>
		/// <returns> next token in the stream or null if end-of-stream was hit
		/// </returns>
		public virtual Token Next(Token result)
		{
			return Next();
		}
		
		/// <summary>Resets this stream to the beginning. This is an
		/// optional operation, so subclasses may or may not
		/// implement this method. Reset() is not needed for
		/// the standard indexing process. However, if the Tokens 
		/// of a TokenStream are intended to be consumed more than 
		/// once, it is necessary to implement reset(). 
		/// </summary>
		public virtual void  Reset()
		{
		}
		
		/// <summary>Releases resources associated with this stream. </summary>
		public virtual void  Close()
		{
		}
	}
}