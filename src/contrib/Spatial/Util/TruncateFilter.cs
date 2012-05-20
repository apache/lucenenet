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

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Spatial.Util
{
	public class TruncateFilter : TokenFilter
	{
		private readonly int maxTokenLength;

		// TODO using TermAttribute for now since CharTermAttribute is not available
		// TODO in 3.1+ https://issues.apache.org/jira/browse/LUCENE-2302
		private readonly TermAttribute termAttr;

		public TruncateFilter(TokenStream input, int maxTokenLength)
			: base(input)
		{
			termAttr = AddAttribute<TermAttribute>();
			this.maxTokenLength = maxTokenLength;
		}

		public override bool IncrementToken()
		{
			if (!input.IncrementToken())
			{
				return false;
			}

			if (termAttr.TermLength() > maxTokenLength)
			{
				termAttr.SetTermLength(maxTokenLength);
			}
			return true;
		}
	}
}
