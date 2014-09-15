using System;
using System.Collections;

namespace org.apache.lucene.queries.function.valuesource
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

	using org.apache.lucene.index;
	using FloatDocValues = org.apache.lucene.queries.function.docvalues.FloatDocValues;
	using DocIdSetIterator = org.apache.lucene.search.DocIdSetIterator;
	using IndexSearcher = org.apache.lucene.search.IndexSearcher;
	using TFIDFSimilarity = org.apache.lucene.search.similarities.TFIDFSimilarity;
	using BytesRef = org.apache.lucene.util.BytesRef;


	/// <summary>
	/// Function that returns <seealso cref="TFIDFSimilarity#tf(float)"/>
	/// for every document.
	/// <para>
	/// Note that the configured Similarity for the field must be
	/// a subclass of <seealso cref="TFIDFSimilarity"/>
	/// @lucene.internal 
	/// </para>
	/// </summary>
	public class TFValueSource : TermFreqValueSource
	{
	  public TFValueSource(string field, string val, string indexedField, BytesRef indexedBytes) : base(field, val, indexedField, indexedBytes)
	  {
	  }

	  public override string name()
	  {
		return "tf";
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.FunctionValues getValues(java.util.Map context, AtomicReaderContext readerContext) throws java.io.IOException
	  public override FunctionValues getValues(IDictionary context, AtomicReaderContext readerContext)
	  {
		Fields fields = readerContext.reader().fields();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Terms terms = fields.terms(indexedField);
		Terms terms = fields.terms(indexedField);
		IndexSearcher searcher = (IndexSearcher)context["searcher"];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.search.similarities.TFIDFSimilarity similarity = IDFValueSource.asTFIDF(searcher.getSimilarity(), indexedField);
		TFIDFSimilarity similarity = IDFValueSource.asTFIDF(searcher.Similarity, indexedField);
		if (similarity == null)
		{
		  throw new System.NotSupportedException("requires a TFIDFSimilarity (such as DefaultSimilarity)");
		}

		return new FloatDocValuesAnonymousInnerClassHelper(this, this, terms, similarity);
	  }

	  private class FloatDocValuesAnonymousInnerClassHelper : FloatDocValues
	  {
		  private readonly TFValueSource outerInstance;

		  private Terms terms;
		  private TFIDFSimilarity similarity;

		  public FloatDocValuesAnonymousInnerClassHelper(TFValueSource outerInstance, org.apache.lucene.queries.function.valuesource.TFValueSource this, Terms terms, TFIDFSimilarity similarity) : base(this)
		  {
			  this.outerInstance = outerInstance;
			  this.terms = terms;
			  this.similarity = similarity;
			  lastDocRequested = -1;
		  }

		  internal DocsEnum docs;
		  internal int atDoc;
		  internal int lastDocRequested;

//JAVA TO C# CONVERTER TODO TASK: Initialization blocks declared within anonymous inner classes are not converted:
	//	  {
	//		  reset();
	//	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void reset() throws java.io.IOException
		  public virtual void reset()
		  {
			// no one should call us for deleted docs?

			if (terms != null)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final TermsEnum termsEnum = terms.iterator(null);
			  TermsEnum termsEnum = terms.iterator(null);
			  if (termsEnum.seekExact(outerInstance.indexedBytes))
			  {
				docs = termsEnum.docs(null, null);
			  }
			  else
			  {
				docs = null;
			  }
			}
			else
			{
			  docs = null;
			}

			if (docs == null)
			{
			  docs = new DocsEnumAnonymousInnerClassHelper(this);
			}
			atDoc = -1;
		  }

		  private class DocsEnumAnonymousInnerClassHelper : DocsEnum
		  {
			  private readonly FloatDocValuesAnonymousInnerClassHelper outerInstance;

			  public DocsEnumAnonymousInnerClassHelper(FloatDocValuesAnonymousInnerClassHelper outerInstance)
			  {
				  this.outerInstance = outerInstance;
			  }

			  public override int freq()
			  {
				return 0;
			  }

			  public override int docID()
			  {
				return DocIdSetIterator.NO_MORE_DOCS;
			  }

			  public override int nextDoc()
			  {
				return DocIdSetIterator.NO_MORE_DOCS;
			  }

			  public override int advance(int target)
			  {
				return DocIdSetIterator.NO_MORE_DOCS;
			  }

			  public override long cost()
			  {
				return 0;
			  }
		  }

		  public override float floatVal(int doc)
		  {
			try
			{
			  if (doc < lastDocRequested)
			  {
				// out-of-order access.... reset
				reset();
			  }
			  lastDocRequested = doc;

			  if (atDoc < doc)
			  {
				atDoc = docs.advance(doc);
			  }

			  if (atDoc > doc)
			  {
				// term doesn't match this document... either because we hit the
				// end, or because the next doc is after this doc.
				return similarity.tf(0);
			  }

			  // a match!
			  return similarity.tf(docs.freq());
			}
			catch (IOException e)
			{
			  throw new Exception("caught exception in function " + outerInstance.description() + " : doc=" + doc, e);
			}
		  }
	  }
	}

}