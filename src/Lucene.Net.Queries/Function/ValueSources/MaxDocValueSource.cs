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
using org.apache.lucene.queries.function;

namespace Lucene.Net.Queries.Function.ValueSources
{
    // javadocs


    /// <summary>
	/// Returns the value of <seealso cref="IndexReader#maxDoc()"/>
	/// for every document. This is the number of documents
	/// including deletions.
	/// </summary>
	public class MaxDocValueSource : ValueSource
	{
	  public virtual string name()
	  {
		return "maxdoc";
	  }

	  public override string description()
	  {
		return name() + "()";
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void CreateWeight(java.util.Map context, org.apache.lucene.search.IndexSearcher searcher) throws java.io.IOException
	  public override void CreateWeight(IDictionary context, IndexSearcher searcher)
	  {
		context["searcher"] = searcher;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.FunctionValues GetValues(java.util.Map context, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
	  {
		IndexSearcher searcher = (IndexSearcher)context["searcher"];
		return new ConstIntDocValues(searcher.IndexReader.maxDoc(), this);
	  }

	  public override bool Equals(object o)
	  {
		return this.GetType() == o.GetType();
	  }

	  public override int GetHashCode()
	  {
		return this.GetType().GetHashCode();
	  }
	}

}