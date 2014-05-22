using System;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Perfield
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
	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Lucene46Codec = Lucene.Net.Codecs.lucene46.Lucene46Codec;
	using BinaryDocValuesField = Lucene.Net.Document.BinaryDocValuesField;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using NumericDocValuesField = Lucene.Net.Document.NumericDocValuesField;
	using BaseDocValuesFormatTestCase = Lucene.Net.Index.BaseDocValuesFormatTestCase;
	using BinaryDocValues = Lucene.Net.Index.BinaryDocValues;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using NumericDocValues = Lucene.Net.Index.NumericDocValues;
	using RandomCodec = Lucene.Net.Index.RandomCodec;
	using Term = Lucene.Net.Index.Term;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using Query = Lucene.Net.Search.Query;
	using TermQuery = Lucene.Net.Search.TermQuery;
	using TopDocs = Lucene.Net.Search.TopDocs;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using TestUtil = Lucene.Net.Util.TestUtil;

	/// <summary>
	/// Basic tests of PerFieldDocValuesFormat
	/// </summary>
	public class TestPerFieldDocValuesFormat : BaseDocValuesFormatTestCase
	{
	  private Codec Codec_Renamed;

	  public override void SetUp()
	  {
		Codec_Renamed = new RandomCodec(new Random(random().nextLong()), Collections.emptySet<string>());
		base.setUp();
	  }

	  protected internal override Codec Codec
	  {
		  get
		  {
			return Codec_Renamed;
		  }
	  }

	  protected internal override bool CodecAcceptsHugeBinaryValues(string field)
	  {
		return TestUtil.fieldSupportsHugeBinaryDocValues(field);
	  }

	  // just a simple trivial test
	  // TODO: we should come up with a test that somehow checks that segment suffix
	  // is respected by all codec apis (not just docvalues and postings)
	  public virtual void TestTwoFieldsTwoFormats()
	  {
		Analyzer analyzer = new MockAnalyzer(random());

		Directory directory = newDirectory();
		// we don't use RandomIndexWriter because it might add more docvalues than we expect !!!!1
		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		DocValuesFormat fast = DocValuesFormat.forName("Lucene45");
		DocValuesFormat slow = DocValuesFormat.forName("SimpleText");
		iwc.Codec = new Lucene46CodecAnonymousInnerClassHelper(this, fast, slow);
		IndexWriter iwriter = new IndexWriter(directory, iwc);
		Document doc = new Document();
		string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
		string text = "this is the text to be indexed. " + longTerm;
		doc.add(newTextField("fieldname", text, Field.Store.YES));
		doc.add(new NumericDocValuesField("dv1", 5));
		doc.add(new BinaryDocValuesField("dv2", new BytesRef("hello world")));
		iwriter.addDocument(doc);
		iwriter.close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		IndexSearcher isearcher = newSearcher(ireader);

		Assert.AreEqual(1, isearcher.search(new TermQuery(new Term("fieldname", longTerm)), 1).totalHits);
		Query query = new TermQuery(new Term("fieldname", "text"));
		TopDocs hits = isearcher.search(query, null, 1);
		Assert.AreEqual(1, hits.totalHits);
		BytesRef scratch = new BytesRef();
		// Iterate through the results:
		for (int i = 0; i < hits.scoreDocs.length; i++)
		{
		  Document hitDoc = isearcher.doc(hits.scoreDocs[i].doc);
		  Assert.AreEqual(text, hitDoc.get("fieldname"));
		  Debug.Assert(ireader.leaves().size() == 1);
		  NumericDocValues dv = ireader.leaves().get(0).reader().getNumericDocValues("dv1");
		  Assert.AreEqual(5, dv.get(hits.scoreDocs[i].doc));
		  BinaryDocValues dv2 = ireader.leaves().get(0).reader().getBinaryDocValues("dv2");
		  dv2.get(hits.scoreDocs[i].doc, scratch);
		  Assert.AreEqual(new BytesRef("hello world"), scratch);
		}

		ireader.close();
		directory.close();
	  }

	  private class Lucene46CodecAnonymousInnerClassHelper : Lucene46Codec
	  {
		  private readonly TestPerFieldDocValuesFormat OuterInstance;

		  private DocValuesFormat Fast;
		  private DocValuesFormat Slow;

		  public Lucene46CodecAnonymousInnerClassHelper(TestPerFieldDocValuesFormat outerInstance, DocValuesFormat fast, DocValuesFormat slow)
		  {
			  this.OuterInstance = outerInstance;
			  this.Fast = fast;
			  this.Slow = slow;
		  }

		  public override DocValuesFormat GetDocValuesFormatForField(string field)
		  {
			if ("dv1".Equals(field))
			{
			  return Fast;
			}
			else
			{
			  return Slow;
			}
		  }
	  }
	}

}