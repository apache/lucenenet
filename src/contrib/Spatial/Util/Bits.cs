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

namespace Lucene.Net.Spatial.Util
{
	/// <summary>
	/// Interface for Bitset-like structures.
	/// </summary>
	public abstract class Bits
	{
		public abstract bool Get(int index);
		public abstract int Length();

		public static readonly Bits[] EMPTY_ARRAY = new Bits[0];

		/// <summary>
		/// Bits impl of the specified length with all bits set.
		/// </summary>
		public class MatchAllBits : Bits
		{
			readonly int len;

			public MatchAllBits(int len)
			{
				this.len = len;
			}

			public override bool Get(int index)
			{
				return true;
			}

			public override int Length()
			{
				return len;
			}
		}

		/// <summary>
		/// Bits impl of the specified length with no bits set. 
		/// </summary>
		public class MatchNoBits : Bits
		{
			readonly int len;

			public MatchNoBits(int len)
			{
				this.len = len;
			}

			public override bool Get(int index)
			{
				return false;
			}

			public override int Length()
			{
				return len;
			}
		}
	}
}