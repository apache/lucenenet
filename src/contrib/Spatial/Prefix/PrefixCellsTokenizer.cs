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

namespace Lucene.Net.Spatial.Prefix
{
	public class PrefixCellsTokenizer : Tokenizer
	{
		private readonly TermAttribute termAtt;

		public PrefixCellsTokenizer()
		{
			termAtt = AddAttribute<TermAttribute>();
		}

		public override bool IncrementToken()
		{
			ClearAttributes();
			int length = 0;
			char[] buffer = termAtt.TermBuffer();
			while (true)
			{
				char c = (char)input.Read();
				if (c < 0) break;
				if (c == 'a' || c == 'A')
				{
					buffer[length++] = 'A';
					continue;
				}
				if (c == 'b' || c == 'B')
				{
					buffer[length++] = 'B';
					continue;
				}
				if (c == 'c' || c == 'C')
				{
					buffer[length++] = 'C';
					continue;
				}
				if (c == 'd' || c == 'D')
				{
					buffer[length++] = 'D';
					continue;
				}
				if (c == '*')
				{
					buffer[length++] = '*';
					continue;
				}
				if (c == '+')
				{
					buffer[length++] = '+';
					continue;
				}

				if (length > 0)
				{
					// Skip any other character
					break;
				}
			}

			termAtt.SetTermLength(length);
			return length > 0; // should only happen at the end
		}

		public override void End()
		{
		}

		public override void Reset(System.IO.TextReader input)
		{
			base.Reset(input);
		}
	}
}
