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
    using NUnit.Framework;

	public class TestSegmentReader : LuceneTestCase
	{
	  private Directory Dir;
	  private Document TestDoc = new Document();
	  private SegmentReader Reader = null;

	  //TODO: Setup the reader w/ multiple documents
	  public override void SetUp()
	  {
		base.SetUp();
		Dir = NewDirectory();
		DocHelper.SetupDoc(TestDoc);
		SegmentCommitInfo info = DocHelper.WriteDoc(Random(), Dir, TestDoc);
		Reader = new SegmentReader(info, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, IOContext.READ);
	  }

	  public override void TearDown()
	  {
		Reader.Dispose();
		Dir.Dispose();
		base.TearDown();
	  }

	  public virtual void Test()
	  {
		Assert.IsTrue(Dir != null);
		Assert.IsTrue(Reader != null);
		Assert.IsTrue(DocHelper.nameValues.Size() > 0);
		Assert.IsTrue(DocHelper.NumFields(TestDoc) == DocHelper.all.Size());
	  }

	  public virtual void TestDocument()
	  {
		Assert.IsTrue(Reader.NumDocs() == 1);
		Assert.IsTrue(Reader.MaxDoc() >= 1);
		Document result = Reader.Document(0);
		Assert.IsTrue(result != null);
		//There are 2 unstored fields on the document that are not preserved across writing
		Assert.IsTrue(DocHelper.NumFields(result) == DocHelper.NumFields(TestDoc) - DocHelper.unstored.Size());

		IList<IndexableField> fields = result.Fields;
		foreach (IndexableField field in fields)
		{
		  Assert.IsTrue(field != null);
		  Assert.IsTrue(DocHelper.nameValues.containsKey(field.Name()));
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
		  string name = fieldInfo.Name;
		  allFieldNames.Add(name);
		  if (fieldInfo.Indexed)
		  {
			indexedFieldNames.Add(name);
		  }
		  else
		  {
			notIndexedFieldNames.Add(name);
		  }
		  if (fieldInfo.HasVectors())
		  {
			tvFieldNames.Add(name);
		  }
		  else if (fieldInfo.Indexed)
		  {
			noTVFieldNames.Add(name);
		  }
		}

		Assert.IsTrue(allFieldNames.Count == DocHelper.all.Size());
		foreach (string s in allFieldNames)
		{
		  Assert.IsTrue(DocHelper.nameValues.containsKey(s) == true || s.Equals(""));
		}

		Assert.IsTrue(indexedFieldNames.Count == DocHelper.indexed.Size());
		foreach (string s in indexedFieldNames)
		{
		  Assert.IsTrue(DocHelper.indexed.containsKey(s) == true || s.Equals(""));
		}

		Assert.IsTrue(notIndexedFieldNames.Count == DocHelper.unindexed.Size());
		//Get all indexed fields that are storing term vectors
		Assert.IsTrue(tvFieldNames.Count == DocHelper.termvector.Size());

		Assert.IsTrue(noTVFieldNames.Count == DocHelper.notermvector.Size());
	  }

	  public virtual void TestTerms()
	  {
		Fields fields = MultiFields.GetFields(Reader);
		foreach (string field in fields)
		{
		  Terms terms = fields.Terms(field);
		  Assert.IsNotNull(terms);
		  TermsEnum termsEnum = terms.Iterator(null);
		  while (termsEnum.Next() != null)
		  {
			BytesRef term = termsEnum.Term();
			Assert.IsTrue(term != null);
			string fieldValue = (string) DocHelper.nameValues.Get(field);
			Assert.IsTrue(fieldValue.IndexOf(term.Utf8ToString()) != -1);
		  }
		}

		DocsEnum termDocs = TestUtil.Docs(Random(), Reader, DocHelper.TEXT_FIELD_1_KEY, new BytesRef("field"), MultiFields.GetLiveDocs(Reader), null, 0);
		Assert.IsTrue(termDocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);

		termDocs = TestUtil.Docs(Random(), Reader, DocHelper.NO_NORMS_KEY, new BytesRef(DocHelper.NO_NORMS_TEXT), MultiFields.GetLiveDocs(Reader), null, 0);

		Assert.IsTrue(termDocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);


		DocsAndPositionsEnum positions = MultiFields.GetTermPositionsEnum(Reader, MultiFields.GetLiveDocs(Reader), DocHelper.TEXT_FIELD_1_KEY, new BytesRef("field"));
		// NOTE: prior rev of this test was failing to first
		// call next here:
		Assert.IsTrue(positions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		Assert.IsTrue(positions.DocID() == 0);
		Assert.IsTrue(positions.NextPosition() >= 0);
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
		for (int i = 0; i < DocHelper.fields.Length; i++)
		{
		  IndexableField f = DocHelper.fields[i];
		  if (f.FieldType().indexed())
		  {
			Assert.AreEqual(reader.GetNormValues(f.Name()) != null, !f.FieldType().OmitsNorms());
			Assert.AreEqual(reader.GetNormValues(f.Name()) != null, !DocHelper.noNorms.containsKey(f.Name()));
			if (reader.GetNormValues(f.Name()) == null)
			{
			  // test for norms of null
			  NumericDocValues norms = MultiDocValues.GetNormValues(reader, f.Name());
			  Assert.IsNull(norms);
			}
		  }
		}
	  }

	  public virtual void TestTermVectors()
	  {
		Terms result = Reader.GetTermVectors(0).Terms(DocHelper.TEXT_FIELD_2_KEY);
		Assert.IsNotNull(result);
		Assert.AreEqual(3, result.Size());
		TermsEnum termsEnum = result.Iterator(null);
		while (termsEnum.Next() != null)
		{
		  string term = termsEnum.Term().Utf8ToString();
		  int freq = (int) termsEnum.TotalTermFreq();
		  Assert.IsTrue(DocHelper.FIELD_2_TEXT.IndexOf(term) != -1);
		  Assert.IsTrue(freq > 0);
		}

		Fields results = Reader.GetTermVectors(0);
		Assert.IsTrue(results != null);
		Assert.AreEqual(3, results.Size(), "We do not have 3 term freq vectors");
	  }

	  public virtual void TestOutOfBoundsAccess()
	  {
		int numDocs = Reader.MaxDoc();
		try
		{
		  Reader.Document(-1);
		  Assert.Fail();
		}
		catch (System.IndexOutOfRangeException expected)
		{
		}

		try
		{
		  Reader.GetTermVectors(-1);
		  Assert.Fail();
		}
		catch (System.IndexOutOfRangeException expected)
		{
		}

		try
		{
		  Reader.Document(numDocs);
		  Assert.Fail();
		}
		catch (System.IndexOutOfRangeException expected)
		{
		}

		try
		{
		  Reader.GetTermVectors(numDocs);
		  Assert.Fail();
		}
		catch (System.IndexOutOfRangeException expected)
		{
		}

	  }
	}

}