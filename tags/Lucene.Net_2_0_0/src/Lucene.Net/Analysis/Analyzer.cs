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
	
	/// <summary>An Analyzer builds TokenStreams, which analyze text.  It thus represents a
	/// policy for extracting index terms from text.
	/// <p>
	/// Typical implementations first build a Tokenizer, which breaks the stream of
	/// characters from the Reader into raw Tokens.  One or more TokenFilters may
	/// then be applied to the output of the Tokenizer.
	/// <p>
	/// WARNING: You must override one of the methods defined by this class in your
	/// subclass or the Analyzer will enter an infinite loop.
	/// </summary>
	public abstract class Analyzer
	{
		/// <summary>Creates a TokenStream which tokenizes all the text in the provided
		/// Reader.  Default implementation forwards to tokenStream(Reader) for 
		/// compatibility with older version.  Override to allow Analyzer to choose 
		/// strategy based on document and/or field.  Must be able to handle null
		/// field name for backward compatibility. 
		/// </summary>
		public abstract TokenStream TokenStream(System.String fieldName, System.IO.TextReader reader);
		
		
		/// <summary> Invoked before indexing a Field instance if
		/// terms have already been added to that field.  This allows custom
		/// analyzers to place an automatic position increment gap between
		/// Field instances using the same field name.  The default value
		/// position increment gap is 0.  With a 0 position increment gap and
		/// the typical default token position increment of 1, all terms in a field,
		/// including across Field instances, are in successive positions, allowing
		/// exact PhraseQuery matches, for instance, across Field instance boundaries.
		/// 
		/// </summary>
		/// <param name="fieldName">Field name being indexed.
		/// </param>
		/// <returns> position increment gap, added to the next token emitted from {@link #TokenStream(String,Reader)}
		/// </returns>
		public virtual int GetPositionIncrementGap(System.String fieldName)
		{
			return 0;
		}
	}
}