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
using System.Collections;
using Lucene.Net.Search.Similarities;
using org.apache.lucene.queries.function;

namespace Lucene.Net.Queries.Function.ValueSources
{
    /// <summary>
	/// Function that returns <seealso cref="TFIDFSimilarity #idf(long, long)"/>
	/// for every document.
	/// <para>
	/// Note that the configured Similarity for the field must be
	/// a subclass of <seealso cref="TFIDFSimilarity"/>
	/// @lucene.internal 
	/// </para>
	/// </summary>
	public class IDFValueSource : DocFreqValueSource
	{
	  public IDFValueSource(string field, string val, string indexedField, BytesRef indexedBytes) : base(field, val, indexedField, indexedBytes)
	  {
	  }

	  public override string name()
	  {
		return "idf";
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.FunctionValues GetValues(java.util.Map context, AtomicReaderContext readerContext) throws java.io.IOException
	  public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
	  {
		IndexSearcher searcher = (IndexSearcher)context["searcher"];
		TFIDFSimilarity sim = asTFIDF(searcher.Similarity, field);
		if (sim == null)
		{
		  throw new System.NotSupportedException("requires a TFIDFSimilarity (such as DefaultSimilarity)");
		}
		int docfreq = searcher.IndexReader.docFreq(new Term(indexedField, indexedBytes));
		float idf = sim.idf(docfreq, searcher.IndexReader.maxDoc());
		return new ConstDoubleDocValues(idf, this);
	  }

	  // tries extra hard to cast the sim to TFIDFSimilarity
	  internal static TFIDFSimilarity AsTFIDF(Similarity sim, string field)
	  {
		while (sim is PerFieldSimilarityWrapper)
		{
		  sim = ((PerFieldSimilarityWrapper)sim).get(field);
		}
		if (sim is TFIDFSimilarity)
		{
		  return (TFIDFSimilarity)sim;
		}
		else
		{
		  return null;
		}
	  }
	}


}