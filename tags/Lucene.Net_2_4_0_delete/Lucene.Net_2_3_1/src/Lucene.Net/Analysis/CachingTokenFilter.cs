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
	
	/// <summary> This class can be used if the Tokens of a TokenStream
	/// are intended to be consumed more than once. It caches
	/// all Tokens locally in a List.
	/// 
	/// CachingTokenFilter implements the optional method
	/// {@link TokenStream#Reset()}, which repositions the
	/// stream to the first Token. 
	/// 
	/// </summary>
	public class CachingTokenFilter : TokenFilter
	{
		private System.Collections.IList cache;
		private System.Collections.IEnumerator iterator;
		
		public CachingTokenFilter(TokenStream input) : base(input)
		{
		}
		
		public override Token Next()
		{
			if (cache == null)
			{
				// fill cache lazily
				cache = new System.Collections.ArrayList();
				FillCache();
				iterator = cache.GetEnumerator();
			}
			
			if (!iterator.MoveNext())
			{
				// the cache is exhausted, return null
				return null;
			}
			
			return (Token) iterator.Current;
		}
		
		public override void  Reset()
		{
			if (cache != null)
			{
				iterator = cache.GetEnumerator();
			}
		}
		
		private void  FillCache()
		{
			Token token;
			while ((token = input.Next()) != null)
			{
				cache.Add(token);
			}
		}
	}
}