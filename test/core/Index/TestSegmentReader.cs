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


	using Document = Lucene.Net.Document.Document;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestSegmentReader : LuceneTestCase
	{
	  private Directory Dir;
	  private Document TestDoc = new Document();
	  private SegmentReader Reader = null;

	  //TODO: Setup the reader w/ multiple documents
	  public override void SetUp()
	  {
		base.setUp();
		Dir = newDirectory();
		DocHelper.setupDoc(TestDoc);
		SegmentCommitInfo info = DocHelper.writeDoc(random(), Dir, TestDoc);
		Reader = new SegmentReader(info, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, IOContext.READ);
	  }

	  public override void TearDown()
	  {
		Reader.close();
		Dir.close();
		base.tearDown();
	  }

	  public virtual void Test()
	  {
		Assert.IsTrue(Dir != null);
		Assert.IsTrue(Reader != null);
		Assert.IsTrue(DocHelper.nameValues.size() > 0);
		Assert.IsTrue(DocHelper.numFields(TestDoc) == DocHelper.all.size());
	  }

	  public virtual void TestDocument()
	  {
		Assert.IsTrue(Reader.numDocs() == 1);
		Assert.IsTrue(Reader.maxDoc() >= 1);
		Document result = Reader.document(0);
		Assert.IsTrue(result != null);
		//There are 2 unstored fields on the document that are not preserved across writing
		Assert.IsTrue(DocHelper.numFields(result) == DocHelper.numFields(TestDoc) - DocHelper.unstored.size());

		IList<IndexableField> fields = result.Fields;
		foreach (IndexableField field in fields)
		{
		  Assert.IsTrue(field != null);
		  Assert.IsTrue(DocHelper.nameValues.containsKey(field.name()));
		}
	  }

	  public virtual void TestGetFieldNameVariations()
	  {
		ICollection<string> allFieldNames = new HashSet<string>();
		ICollection<string> indexedFieldNames = new HashSet<string>();
		ICollection<string> notIndexedFieldNames = new HashSet<string>();
		ICollection<string> tvFieldNames = new HashSet<string>();
		ICollection<string> noTVFieldNames = new HashSet<string>();

		foreach (FieldInfo fieldInfo in Reader.FieldInfos)
		{
		  string name = fieldInfo.name;
		  allFieldNames.Add(name);
		  if (fieldInfo.Indexed)
		  {
			indexedFieldNames.Add(name);
		  }
		  else
		  {
			notIndexedFieldNames.Add(name);
		  }
		  if (fieldInfo.hasVectors())
		  {
			tvFieldNames.Add(name);
		  }
		  else if (fieldInfo.Indexed)
		  {
			noTVFieldNames.Add(name);
		  }
		}

		Assert.IsTrue(allFieldNames.Count == DocHelper.all.size());
		foreach (string s in allFieldNames)
		{
		  Assert.IsTrue(DocHelper.nameValues.containsKey(s) == true || s.Equals(""));
		}

		Assert.IsTrue(indexedFieldNames.Count == DocHelper.indexed.size());
		foreach (string s in indexedFieldNames)
		{
		  Assert.IsTrue(DocHelper.indexed.containsKey(s) == true || s.Equals(""));
		}

		Assert.IsTrue(notIndexedFieldNames.Count == DocHelper.unindexed.size());
		//Get all indexed fields that are storing term vectors
		Assert.IsTrue(tvFieldNames.Count == DocHelper.termvector.size());

		Assert.IsTrue(noTVFieldNames.Count == DocHelper.notermvector.size());
	  }

	  public virtual void TestTerms()
	  {
		Fields fields = MultiFields.getFields(Reader);
		foreach (string field in fields)
		{
		  Terms terms = fields.terms(field);
		  Assert.IsNotNull(terms);
		  TermsEnum termsEnum = terms.iterator(null);
		  while (termsEnum.next() != null)
		  {
			BytesRef term = termsEnum.term();
			Assert.IsTrue(term != null);
			string fieldValue = (string) DocHelper.nameValues.get(field);
			Assert.IsTrue(fieldValue.IndexOf(term.utf8ToString()) != -1);
		  }
		}

		DocsEnum termDocs = TestUtil.docs(random(), Reader, DocHelper.TEXT_FIELD_1_KEY, new BytesRef("field"), MultiFields.getLiveDocs(Reader), null, 0);
		Assert.IsTrue(termDocs.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);

		termDocs = TestUtil.docs(random(), Reader, DocHelper.NO_NORMS_KEY, new BytesRef(DocHelper.NO_NORMS_TEXT), MultiFields.getLiveDocs(Reader), null, 0);

		Assert.IsTrue(termDocs.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);


		DocsAndPositionsEnum positions = MultiFields.getTermPositionsEnum(Reader, MultiFields.getLiveDocs(Reader), DocHelper.TEXT_FIELD_1_KEY, new BytesRef("field"));
		// NOTE: prior rev of this test was failing to first
		// call next here:
		Assert.IsTrue(positions.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		Assert.IsTrue(positions.docID() == 0);
		Assert.IsTrue(positions.nextPosition() >= 0);
	  }

	  public virtual void TestNorms()
	  {
		//TODO: Not sure how these work/should be tested
	/*
	    try {
	      byte [] norms = reader.norms(DocHelper.TEXT_FIELD_1_KEY);
	      System.out.println("Norms: " + norms);
	      Assert.IsTrue(norms != null);
	    } catch (IOException e) {
	      e.printStackTrace();
	      Assert.IsTrue(false);
	    }
	*/

		CheckNorms(Reader);
	  }

	  public static void CheckNorms(AtomicReader reader)
	  {
		// test omit norms
		for (int i = 0; i < DocHelper.fields.length; i++)
		{
		  IndexableField f = DocHelper.fields[i];
		  if (f.fieldType().indexed())
		  {
			Assert.AreEqual(reader.getNormValues(f.name()) != null, !f.fieldType().omitNorms());
			Assert.AreEqual(reader.getNormValues(f.name()) != null, !DocHelper.noNorms.containsKey(f.name()));
			if (reader.getNormValues(f.name()) == null)
			{
			  // test for norms of null
			  NumericDocValues norms = MultiDocValues.getNormValues(reader, f.name());
			  assertNull(norms);
			}
		  }
		}
	  }

	  public virtual void TestTermVectors()
	  {
		Terms result = Reader.getTermVectors(0).terms(DocHelper.TEXT_FIELD_2_KEY);
		Assert.IsNotNull(result);
		Assert.AreEqual(3, result.size());
		TermsEnum termsEnum = result.iterator(null);
		while (termsEnum.next() != null)
		{
		  string term = termsEnum.term().utf8ToString();
		  int freq = (int) termsEnum.totalTermFreq();
		  Assert.IsTrue(DocHelper.FIELD_2_TEXT.IndexOf(term) != -1);
		  Assert.IsTrue(freq > 0);
		}

		Fields results = Reader.getTermVectors(0);
		Assert.IsTrue(results != null);
		Assert.AreEqual("We do not have 3 term freq vectors", 3, results.size());
	  }

	  public virtual void TestOutOfBoundsAccess()
	  {
		int numDocs = Reader.maxDoc();
		try
		{
		  Reader.document(-1);
		  Assert.Fail();
		}
		catch (System.IndexOutOfRangeException expected)
		{
		}

		try
		{
		  Reader.getTermVectors(-1);
		  Assert.Fail();
		}
		catch (System.IndexOutOfRangeException expected)
		{
		}

		try
		{
		  Reader.document(numDocs);
		  Assert.Fail();
		}
		catch (System.IndexOutOfRangeException expected)
		{
		}

		try
		{
		  Reader.getTermVectors(numDocs);
		  Assert.Fail();
		}
		catch (System.IndexOutOfRangeException expected)
		{
		}

	  }
	}

}