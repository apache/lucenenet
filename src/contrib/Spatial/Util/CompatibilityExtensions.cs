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

using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Spatial.Util
{
	public static class CompatibilityExtensions
	{
		public static void Append(this TermAttribute termAtt, string str)
		{
			termAtt.SetTermBuffer(termAtt.Term() + str); // TODO: Not optimal, but works
		}

		public static void Append(this TermAttribute termAtt, char ch)
		{
			termAtt.SetTermBuffer(termAtt.Term() + new string(new[] {ch})); // TODO: Not optimal, but works
		}
	}
}
