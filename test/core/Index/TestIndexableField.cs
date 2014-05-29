using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Lucene.Net.Index
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


	using Analyzer = Lucene.Net.Analysis.Analyzer;
	using TokenStream = Lucene.Net.Analysis.TokenStream;
	using Codec = Lucene.Net.Codecs.Codec;
	using Lucene3xCodec = Lucene.Net.Codecs.Lucene3x.Lucene3xCodec;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using DocValuesType = Lucene.Net.Index.FieldInfo.DocValuesType_e;
	using BooleanClause = Lucene.Net.Search.BooleanClause;
	using BooleanQuery = Lucene.Net.Search.BooleanQuery;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using TermQuery = Lucene.Net.Search.TermQuery;
	using TopDocs = Lucene.Net.Search.TopDocs;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestIndexableField : LuceneTestCase
	{

	  private class MyField : IndexableField
	  {
		  private readonly TestIndexableField OuterInstance;


		internal readonly int Counter;
		internal readonly IndexableFieldType fieldType = new IndexableFieldTypeAnonymousInnerClassHelper();

		private class IndexableFieldTypeAnonymousInnerClassHelper : IndexableFieldType
		{
			public IndexableFieldTypeAnonymousInnerClassHelper()
			{
			}

			public override bool Indexed()
			{
			  return (OuterInstance.Counter % 10) != 3;
			}

			public override bool Stored()
			{
			  return (OuterInstance.Counter & 1) == 0 || (OuterInstance.Counter % 10) == 3;
			}

			public override bool Tokenized()
			{
			  return true;
			}

			public override bool StoreTermVectors()
			{
			  return indexed() && OuterInstance.Counter % 2 == 1 && OuterInstance.Counter % 10 != 9;
			}

			public override bool StoreTermVectorOffsets()
			{
			  return storeTermVectors() && OuterInstance.Counter % 10 != 9;
			}

			public override bool StoreTermVectorPositions()
			{
			  return storeTermVectors() && OuterInstance.Counter % 10 != 9;
			}

			public override bool StoreTermVectorPayloads()
			{
			  if (Codec.Default is Lucene3xCodec)
			  {
				return false; // 3.x doesnt support
			  }
			  else
			  {
				return storeTermVectors() && OuterInstance.Counter % 10 != 9;
			  }
			}

			public override bool OmitNorms()
			{
			  return false;
			}

			public override FieldInfo.IndexOptions_e IndexOptions()
			{
			  return FieldInfo.IndexOptions_e.DOCS_AND_FREQS_AND_POSITIONS;
			}

			public override DocValuesType DocValueType()
			{
			  return null;
			}
		}

		public MyField(TestIndexableField outerInstance, int counter)
		{
			this.OuterInstance = outerInstance;
		  this.Counter = counter;
		}

		public override string Name()
		{
		  return "f" + Counter;
		}

		public override float Boost()
		{
		  return 1.0f + random().nextFloat();
		}

		public override BytesRef BinaryValue()
		{
		  if ((Counter % 10) == 3)
		  {
			sbyte[] bytes = new sbyte[10];
			for (int idx = 0;idx < bytes.Length;idx++)
			{
			  bytes[idx] = (sbyte)(Counter + idx);
			}
			return new BytesRef(bytes, 0, bytes.Length);
		  }
		  else
		  {
			return null;
		  }
		}

		public override string StringValue()
		{
		  int fieldID = Counter % 10;
		  if (fieldID != 3 && fieldID != 7)
		  {
			return "text " + Counter;
		  }
		  else
		  {
			return null;
		  }
		}

		public override Reader ReaderValue()
		{
		  if (Counter % 10 == 7)
		  {
			return new StringReader("text " + Counter);
		  }
		  else
		  {
			return null;
		  }
		}

		public override Number NumericValue()
		{
		  return null;
		}

		public override IndexableFieldType FieldType()
		{
		  return fieldType;
		}

		public override TokenStream TokenStream(Analyzer analyzer)
		{
		  return ReaderValue() != null ? analyzer.tokenStream(Name(), ReaderValue()) : analyzer.tokenStream(Name(), new StringReader(StringValue()));
		}
	  }

	  // Silly test showing how to index documents w/o using Lucene's core
	  // Document nor Field class
	  public virtual void TestArbitraryFields()
	  {

		Directory dir = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), dir);

		int NUM_DOCS = atLeast(27);
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: " + NUM_DOCS + " docs");
		}
		int[] fieldsPerDoc = new int[NUM_DOCS];
		int baseCount = 0;

		for (int docCount = 0;docCount < NUM_DOCS;docCount++)
		{
		  int fieldCount = TestUtil.Next(random(), 1, 17);
		  fieldsPerDoc[docCount] = fieldCount - 1;

		  int finalDocCount = docCount;
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: " + fieldCount + " fields in doc " + docCount);
		  }

		  int finalBaseCount = baseCount;
		  baseCount += fieldCount - 1;

		  w.addDocument(new IterableAnonymousInnerClassHelper(this, fieldCount, finalDocCount, finalBaseCount));
		}

		IndexReader r = w.Reader;
		w.close();

		IndexSearcher s = newSearcher(r);
		int counter = 0;
		for (int id = 0;id < NUM_DOCS;id++)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: verify doc id=" + id + " (" + fieldsPerDoc[id] + " fields) counter=" + counter);
		  }
		  TopDocs hits = s.search(new TermQuery(new Term("id", "" + id)), 1);
		  Assert.AreEqual(1, hits.totalHits);
		  int docID = hits.scoreDocs[0].doc;
		  Document doc = s.doc(docID);
		  int endCounter = counter + fieldsPerDoc[id];
		  while (counter < endCounter)
		  {
			string name = "f" + counter;
			int fieldID = counter % 10;

			bool stored = (counter & 1) == 0 || fieldID == 3;
			bool binary = fieldID == 3;
			bool indexed = fieldID != 3;

			string stringValue;
			if (fieldID != 3 && fieldID != 9)
			{
			  stringValue = "text " + counter;
			}
			else
			{
			  stringValue = null;
			}

			// stored:
			if (stored)
			{
			  IndexableField f = doc.getField(name);
			  Assert.IsNotNull("doc " + id + " doesn't have field f" + counter, f);
			  if (binary)
			  {
				Assert.IsNotNull("doc " + id + " doesn't have field f" + counter, f);
				BytesRef b = f.binaryValue();
				Assert.IsNotNull(b);
				Assert.AreEqual(10, b.length);
				for (int idx = 0;idx < 10;idx++)
				{
				  Assert.AreEqual((sbyte)(idx + counter), b.bytes[b.offset + idx]);
				}
			  }
			  else
			  {
				Debug.Assert(stringValue != null);
				Assert.AreEqual(stringValue, f.stringValue());
			  }
			}

			if (indexed)
			{
			  bool tv = counter % 2 == 1 && fieldID != 9;
			  if (tv)
			  {
				Terms tfv = r.getTermVectors(docID).terms(name);
				Assert.IsNotNull(tfv);
				TermsEnum termsEnum = tfv.iterator(null);
				Assert.AreEqual(new BytesRef("" + counter), termsEnum.next());
				Assert.AreEqual(1, termsEnum.totalTermFreq());
				DocsAndPositionsEnum dpEnum = termsEnum.docsAndPositions(null, null);
				Assert.IsTrue(dpEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
				Assert.AreEqual(1, dpEnum.freq());
				Assert.AreEqual(1, dpEnum.nextPosition());

				Assert.AreEqual(new BytesRef("text"), termsEnum.next());
				Assert.AreEqual(1, termsEnum.totalTermFreq());
				dpEnum = termsEnum.docsAndPositions(null, dpEnum);
				Assert.IsTrue(dpEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
				Assert.AreEqual(1, dpEnum.freq());
				Assert.AreEqual(0, dpEnum.nextPosition());

				assertNull(termsEnum.next());

				// TODO: offsets

			  }
			  else
			  {
				Fields vectors = r.getTermVectors(docID);
				Assert.IsTrue(vectors == null || vectors.terms(name) == null);
			  }

			  BooleanQuery bq = new BooleanQuery();
			  bq.add(new TermQuery(new Term("id", "" + id)), BooleanClause.Occur_e.MUST);
			  bq.add(new TermQuery(new Term(name, "text")), BooleanClause.Occur_e.MUST);
			  TopDocs hits2 = s.search(bq, 1);
			  Assert.AreEqual(1, hits2.totalHits);
			  Assert.AreEqual(docID, hits2.scoreDocs[0].doc);

			  bq = new BooleanQuery();
			  bq.add(new TermQuery(new Term("id", "" + id)), BooleanClause.Occur_e.MUST);
			  bq.add(new TermQuery(new Term(name, "" + counter)), BooleanClause.Occur_e.MUST);
			  TopDocs hits3 = s.search(bq, 1);
			  Assert.AreEqual(1, hits3.totalHits);
			  Assert.AreEqual(docID, hits3.scoreDocs[0].doc);
			}

			counter++;
		  }
		}

		r.close();
		dir.close();
	  }

	  private class IterableAnonymousInnerClassHelper : IEnumerable<IndexableField>
	  {
		  private readonly TestIndexableField OuterInstance;

		  private int FieldCount;
		  private int FinalDocCount;
		  private int FinalBaseCount;

		  public IterableAnonymousInnerClassHelper(TestIndexableField outerInstance, int fieldCount, int finalDocCount, int finalBaseCount)
		  {
			  this.OuterInstance = outerInstance;
			  this.FieldCount = fieldCount;
			  this.FinalDocCount = finalDocCount;
			  this.FinalBaseCount = finalBaseCount;
		  }

		  public virtual IEnumerator<IndexableField> GetEnumerator()
		  {
			return new IteratorAnonymousInnerClassHelper(this);
		  }

		  private class IteratorAnonymousInnerClassHelper : IEnumerator<IndexableField>
		  {
			  private readonly IterableAnonymousInnerClassHelper OuterInstance;

			  public IteratorAnonymousInnerClassHelper(IterableAnonymousInnerClassHelper outerInstance)
			  {
				  this.outerInstance = outerInstance;
			  }

			  internal int fieldUpto;

			  public virtual bool HasNext()
			  {
				return fieldUpto < OuterInstance.FieldCount;
			  }

			  public virtual IndexableField Next()
			  {
				Debug.Assert(fieldUpto < OuterInstance.FieldCount);
				if (fieldUpto == 0)
				{
				  fieldUpto = 1;
				  return newStringField("id", "" + OuterInstance.FinalDocCount, Field.Store.YES);
				}
				else
				{
				  return new MyField(OuterInstance, OuterInstance.FinalBaseCount + (fieldUpto++-1));
				}
			  }

			  public virtual void Remove()
			  {
				throw new System.NotSupportedException();
			  }
		  }
	  }
	}

}