namespace Lucene.Net.Search
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

	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using TextField = Lucene.Net.Document.TextField;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
	using Fields = Lucene.Net.Index.Fields;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Terms = Lucene.Net.Index.Terms;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using Directory = Lucene.Net.Store.Directory;
	using English = Lucene.Net.Util.English;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using AfterClass = org.junit.AfterClass;
	using BeforeClass = org.junit.BeforeClass;

	public class TestTermVectors : LuceneTestCase
	{
	  private static IndexReader Reader;
	  private static Directory Directory;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void beforeClass() throws Exception
	  public static void BeforeClass()
	  {
		Directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), Directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.SIMPLE, true)).setMergePolicy(newLogMergePolicy()));
		//writer.setNoCFSRatio(1.0);
		//writer.infoStream = System.out;
		for (int i = 0; i < 1000; i++)
		{
		  Document doc = new Document();
		  FieldType ft = new FieldType(TextField.TYPE_STORED);
		  int mod3 = i % 3;
		  int mod2 = i % 2;
		  if (mod2 == 0 && mod3 == 0)
		  {
			ft.StoreTermVectors = true;
			ft.StoreTermVectorOffsets = true;
			ft.StoreTermVectorPositions = true;
		  }
		  else if (mod2 == 0)
		  {
			ft.StoreTermVectors = true;
			ft.StoreTermVectorPositions = true;
		  }
		  else if (mod3 == 0)
		  {
			ft.StoreTermVectors = true;
			ft.StoreTermVectorOffsets = true;
		  }
		  else
		  {
			ft.StoreTermVectors = true;
		  }
		  doc.add(new Field("field", English.intToEnglish(i), ft));
		  //test no term vectors too
		  doc.add(new TextField("noTV", English.intToEnglish(i), Field.Store.YES));
		  writer.addDocument(doc);
		}
		Reader = writer.Reader;
		writer.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @AfterClass public static void afterClass() throws Exception
	  public static void AfterClass()
	  {
		Reader.close();
		Directory.close();
		Reader = null;
		Directory = null;
	  }

	  // In a single doc, for the same field, mix the term
	  // vectors up
	  public virtual void TestMixedVectrosVectors()
	  {
		RandomIndexWriter writer = new RandomIndexWriter(random(), Directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.SIMPLE, true)).setOpenMode(OpenMode.CREATE));
		Document doc = new Document();

		FieldType ft2 = new FieldType(TextField.TYPE_STORED);
		ft2.StoreTermVectors = true;

		FieldType ft3 = new FieldType(TextField.TYPE_STORED);
		ft3.StoreTermVectors = true;
		ft3.StoreTermVectorPositions = true;

		FieldType ft4 = new FieldType(TextField.TYPE_STORED);
		ft4.StoreTermVectors = true;
		ft4.StoreTermVectorOffsets = true;

		FieldType ft5 = new FieldType(TextField.TYPE_STORED);
		ft5.StoreTermVectors = true;
		ft5.StoreTermVectorOffsets = true;
		ft5.StoreTermVectorPositions = true;

		doc.add(newTextField("field", "one", Field.Store.YES));
		doc.add(newField("field", "one", ft2));
		doc.add(newField("field", "one", ft3));
		doc.add(newField("field", "one", ft4));
		doc.add(newField("field", "one", ft5));
		writer.addDocument(doc);
		IndexReader reader = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(reader);

		Query query = new TermQuery(new Term("field", "one"));
		ScoreDoc[] hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(1, hits.Length);

		Fields vectors = searcher.reader.getTermVectors(hits[0].doc);
		Assert.IsNotNull(vectors);
		Assert.AreEqual(1, vectors.size());
		Terms vector = vectors.terms("field");
		Assert.IsNotNull(vector);
		Assert.AreEqual(1, vector.size());
		TermsEnum termsEnum = vector.iterator(null);
		Assert.IsNotNull(termsEnum.next());
		Assert.AreEqual("one", termsEnum.term().utf8ToString());
		Assert.AreEqual(5, termsEnum.totalTermFreq());
		DocsAndPositionsEnum dpEnum = termsEnum.docsAndPositions(null, null);
		Assert.IsNotNull(dpEnum);
		Assert.IsTrue(dpEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(5, dpEnum.freq());
		for (int i = 0;i < 5;i++)
		{
		  Assert.AreEqual(i, dpEnum.nextPosition());
		}

		dpEnum = termsEnum.docsAndPositions(null, dpEnum);
		Assert.IsNotNull(dpEnum);
		Assert.IsTrue(dpEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(5, dpEnum.freq());
		for (int i = 0;i < 5;i++)
		{
		  dpEnum.nextPosition();
		  Assert.AreEqual(4 * i, dpEnum.StartOffset());
		  Assert.AreEqual(4 * i + 3, dpEnum.EndOffset());
		}
		reader.close();
	  }

	  private IndexWriter CreateWriter(Directory dir)
	  {
		return new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2));
	  }

	  private void CreateDir(Directory dir)
	  {
		IndexWriter writer = CreateWriter(dir);
		writer.addDocument(CreateDoc());
		writer.close();
	  }

	  private Document CreateDoc()
	  {
		Document doc = new Document();
		FieldType ft = new FieldType(TextField.TYPE_STORED);
		ft.StoreTermVectors = true;
		ft.StoreTermVectorOffsets = true;
		ft.StoreTermVectorPositions = true;
		doc.add(newField("c", "aaa", ft));
		return doc;
	  }

	  private void VerifyIndex(Directory dir)
	  {
		IndexReader r = DirectoryReader.open(dir);
		int numDocs = r.numDocs();
		for (int i = 0; i < numDocs; i++)
		{
		  Assert.IsNotNull("term vectors should not have been null for document " + i, r.getTermVectors(i).terms("c"));
		}
		r.close();
	  }

	  public virtual void TestFullMergeAddDocs()
	  {
		Directory target = newDirectory();
		IndexWriter writer = CreateWriter(target);
		// with maxBufferedDocs=2, this results in two segments, so that forceMerge
		// actually does something.
		for (int i = 0; i < 4; i++)
		{
		  writer.addDocument(CreateDoc());
		}
		writer.forceMerge(1);
		writer.close();

		VerifyIndex(target);
		target.close();
	  }

	  public virtual void TestFullMergeAddIndexesDir()
	  {
		Directory[] input = new Directory[] {newDirectory(), newDirectory()};
		Directory target = newDirectory();

		foreach (Directory dir in input)
		{
		  CreateDir(dir);
		}

		IndexWriter writer = CreateWriter(target);
		writer.addIndexes(input);
		writer.forceMerge(1);
		writer.close();

		VerifyIndex(target);

		IOUtils.close(target, input[0], input[1]);
	  }

	  public virtual void TestFullMergeAddIndexesReader()
	  {
		Directory[] input = new Directory[] {newDirectory(), newDirectory()};
		Directory target = newDirectory();

		foreach (Directory dir in input)
		{
		  CreateDir(dir);
		}

		IndexWriter writer = CreateWriter(target);
		foreach (Directory dir in input)
		{
		  IndexReader r = DirectoryReader.open(dir);
		  writer.addIndexes(r);
		  r.close();
		}
		writer.forceMerge(1);
		writer.close();

		VerifyIndex(target);
		IOUtils.close(target, input[0], input[1]);
	  }

	}

}