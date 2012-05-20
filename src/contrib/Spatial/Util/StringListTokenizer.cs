/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Spatial.Util
{
	/// <summary>
	/// Put a list of strings directly into the token stream
	/// </summary>
	public class StringListTokenizer : TokenStream
	{
		private TermAttribute termAtt;

		private readonly IEnumerable<String> tokens;
		private IEnumerator<String> iter = null;

		public StringListTokenizer(IEnumerable<String> tokens)
		{
			this.tokens = tokens;
			Init();
		}

		private void Init()
		{
			termAtt = AddAttribute<TermAttribute>();
		}

		public override bool IncrementToken()
		{
			if (iter.MoveNext())
			{
				ClearAttributes();
				var t = iter.Current;
				termAtt.Append(t);
				return true;
			}
			return false;
		}

		public override void Reset()
		{
			base.Reset();
			iter = tokens.GetEnumerator();
		}

		protected override void Dispose(bool disposing)
		{
		}
	}
}
