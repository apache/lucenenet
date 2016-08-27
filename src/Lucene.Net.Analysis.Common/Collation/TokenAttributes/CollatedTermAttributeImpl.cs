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

namespace Lucene.Net.Analysis.Collation
{
	/// <summary>
	/// Extension of <seealso cref="CharTermAttribute"/> that encodes the term
	/// text as a binary Unicode collation key instead of as UTF-8 bytes.
	/// </summary>
	public class CollatedTermAttributeImpl : CharTermAttribute
	{
		private readonly Collator collator;

		/// <summary>
		/// Create a new CollatedTermAttributeImpl </summary>
		/// <param name="collator"> Collation key generator </param>
		public CollatedTermAttributeImpl(Collator collator)
		{
			// clone in case JRE doesn't properly sync,
			// or to reduce contention in case they do
			this.collator = (Collator)collator.clone();
		}

		public override void FillBytesRef()
		{
			var bytes = this.BytesRef;
			bytes.Bytes = this.collator.getCollationKey(this.ToString()).toByteArray();
			bytes.Offset = 0;
			bytes.Length = bytes.Bytes.Length;
		}
	}
}