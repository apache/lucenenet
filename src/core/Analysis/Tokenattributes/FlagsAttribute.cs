namespace Lucene.Net.Analysis.Tokenattributes
{

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

	using Attribute = Lucene.Net.Util.Attribute;

	/// <summary>
	/// this attribute can be used to pass different flags down the <seealso cref="Tokenizer"/> chain,
	/// e.g. from one TokenFilter to another one. 
	/// <p>
	/// this is completely distinct from <seealso cref="TypeAttribute"/>, although they do share similar purposes.
	/// The flags can be used to encode information about the token for use by other 
	/// <seealso cref="Lucene.Net.Analysis.TokenFilter"/>s.
	/// @lucene.experimental While we think this is here to stay, we may want to change it to be a long.
	/// </summary>
	public interface FlagsAttribute : Attribute
	{
	  /// <summary>
	  /// Get the bitset for any bits that have been set. </summary>
	  /// <returns> The bits </returns>
	  /// <seealso cref= #getFlags() </seealso>
	  int Flags {get;set;}

	}

}