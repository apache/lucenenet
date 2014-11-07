using System;

namespace org.apache.lucene.analysis.@in
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

	using CharTokenizer = org.apache.lucene.analysis.util.CharTokenizer;
	using StandardTokenizer = org.apache.lucene.analysis.standard.StandardTokenizer; // javadocs
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// Simple Tokenizer for text in Indian Languages. </summary>
	/// @deprecated (3.6) Use <seealso cref="StandardTokenizer"/> instead. 
	[Obsolete("(3.6) Use <seealso cref="StandardTokenizer"/> instead.")]
	public sealed class IndicTokenizer : CharTokenizer
	{

	  public IndicTokenizer(Version matchVersion, AttributeFactory factory, Reader input) : base(matchVersion, factory, input)
	  {
	  }

	  public IndicTokenizer(Version matchVersion, Reader input) : base(matchVersion, input)
	  {
	  }

	  protected internal override bool isTokenChar(int c)
	  {
		return char.IsLetter(c) || char.getType(c) == char.NON_SPACING_MARK || char.getType(c) == char.FORMAT || char.getType(c) == char.COMBINING_SPACING_MARK;
	  }
	}

}