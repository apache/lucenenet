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
	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using TextField = Lucene.Net.Document.TextField;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	/// 
	/// <summary>
	/// @lucene.experimental
	/// </summary>
	public class TestOmitPositions : LuceneTestCase
	{

	  public virtual void TestBasic()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
		ft.IndexOptions = IndexOptions.DOCS_AND_FREQS;
		Field f = newField("foo", "this is a test test", ft);
		doc.add(f);
		for (int i = 0; i < 100; i++)
		{
		  w.addDocument(doc);
		}

		IndexReader reader = w.Reader;
		w.close();

		assertNull(MultiFields.getTermPositionsEnum(reader, null, "foo", new BytesRef("test")));

		DocsEnum de = TestUtil.docs(random(), reader, "foo", new BytesRef("test"), null, null, DocsEnum.FLAG_FREQS);
		while (de.nextDoc() != DocIdSetIterator.NO_MORE_DOCS)
		{
		  Assert.AreEqual(2, de.freq());
		}

		reader.close();
		dir.close();
	  }

	  // Tests whether the DocumentWriter correctly enable the
	  // omitTermFreqAndPositions bit in the FieldInfo
	  public virtual void TestPositions()
	  {
		Directory ram = newDirectory();
		Analyzer analyzer = new MockAnalyzer(random());
		IndexWriter writer = new IndexWriter(ram, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
		Document d = new Document();

		// f1,f2,f3: docs only
		FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
		ft.IndexOptions = IndexOptions.DOCS_ONLY;

		Field f1 = newField("f1", "this field has docs only", ft);
		d.add(f1);

		Field f2 = newField("f2", "this field has docs only", ft);
		d.add(f2);

		Field f3 = newField("f3", "this field has docs only", ft);
		d.add(f3);

		FieldType ft2 = new FieldType(TextField.TYPE_NOT_STORED);
		ft2.IndexOptions = IndexOptions.DOCS_AND_FREQS;

		// f4,f5,f6 docs and freqs
		Field f4 = newField("f4", "this field has docs and freqs", ft2);
		d.add(f4);

		Field f5 = newField("f5", "this field has docs and freqs", ft2);
		d.add(f5);

		Field f6 = newField("f6", "this field has docs and freqs", ft2);
		d.add(f6);

		FieldType ft3 = new FieldType(TextField.TYPE_NOT_STORED);
		ft3.IndexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;

		// f7,f8,f9 docs/freqs/positions
		Field f7 = newField("f7", "this field has docs and freqs and positions", ft3);
		d.add(f7);

		Field f8 = newField("f8", "this field has docs and freqs and positions", ft3);
		d.add(f8);

		Field f9 = newField("f9", "this field has docs and freqs and positions", ft3);
		d.add(f9);

		writer.addDocument(d);
		writer.forceMerge(1);

		// now we add another document which has docs-only for f1, f4, f7, docs/freqs for f2, f5, f8, 
		// and docs/freqs/positions for f3, f6, f9
		d = new Document();

		// f1,f4,f7: docs only
		f1 = newField("f1", "this field has docs only", ft);
		d.add(f1);

		f4 = newField("f4", "this field has docs only", ft);
		d.add(f4);

		f7 = newField("f7", "this field has docs only", ft);
		d.add(f7);

		// f2, f5, f8: docs and freqs
		f2 = newField("f2", "this field has docs and freqs", ft2);
		d.add(f2);

		f5 = newField("f5", "this field has docs and freqs", ft2);
		d.add(f5);

		f8 = newField("f8", "this field has docs and freqs", ft2);
		d.add(f8);

		// f3, f6, f9: docs and freqs and positions
		f3 = newField("f3", "this field has docs and freqs and positions", ft3);
		d.add(f3);

		f6 = newField("f6", "this field has docs and freqs and positions", ft3);
		d.add(f6);

		f9 = newField("f9", "this field has docs and freqs and positions", ft3);
		d.add(f9);

		writer.addDocument(d);

		// force merge
		writer.forceMerge(1);
		// flush
		writer.close();

		SegmentReader reader = getOnlySegmentReader(DirectoryReader.open(ram));
		FieldInfos fi = reader.FieldInfos;
		// docs + docs = docs
		Assert.AreEqual(IndexOptions.DOCS_ONLY, fi.fieldInfo("f1").IndexOptions);
		// docs + docs/freqs = docs
		Assert.AreEqual(IndexOptions.DOCS_ONLY, fi.fieldInfo("f2").IndexOptions);
		// docs + docs/freqs/pos = docs
		Assert.AreEqual(IndexOptions.DOCS_ONLY, fi.fieldInfo("f3").IndexOptions);
		// docs/freqs + docs = docs
		Assert.AreEqual(IndexOptions.DOCS_ONLY, fi.fieldInfo("f4").IndexOptions);
		// docs/freqs + docs/freqs = docs/freqs
		Assert.AreEqual(IndexOptions.DOCS_AND_FREQS, fi.fieldInfo("f5").IndexOptions);
		// docs/freqs + docs/freqs/pos = docs/freqs
		Assert.AreEqual(IndexOptions.DOCS_AND_FREQS, fi.fieldInfo("f6").IndexOptions);
		// docs/freqs/pos + docs = docs
		Assert.AreEqual(IndexOptions.DOCS_ONLY, fi.fieldInfo("f7").IndexOptions);
		// docs/freqs/pos + docs/freqs = docs/freqs
		Assert.AreEqual(IndexOptions.DOCS_AND_FREQS, fi.fieldInfo("f8").IndexOptions);
		// docs/freqs/pos + docs/freqs/pos = docs/freqs/pos
		Assert.AreEqual(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS, fi.fieldInfo("f9").IndexOptions);

		reader.close();
		ram.close();
	  }

	  private void AssertNoPrx(Directory dir)
	  {
		string[] files = dir.listAll();
		for (int i = 0;i < files.Length;i++)
		{
		  Assert.IsFalse(files[i].EndsWith(".prx"));
		  Assert.IsFalse(files[i].EndsWith(".pos"));
		}
	  }

	  // Verifies no *.prx exists when all fields omit term positions:
	  public virtual void TestNoPrxFile()
	  {
		Directory ram = newDirectory();
		Analyzer analyzer = new MockAnalyzer(random());
		IndexWriter writer = new IndexWriter(ram, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setMaxBufferedDocs(3).setMergePolicy(newLogMergePolicy()));
		LogMergePolicy lmp = (LogMergePolicy) writer.Config.MergePolicy;
		lmp.MergeFactor = 2;
		lmp.NoCFSRatio = 0.0;
		Document d = new Document();

		FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
		ft.IndexOptions = IndexOptions.DOCS_AND_FREQS;
		Field f1 = newField("f1", "this field has term freqs", ft);
		d.add(f1);

		for (int i = 0;i < 30;i++)
		{
		  writer.addDocument(d);
		}

		writer.commit();

		AssertNoPrx(ram);

		// now add some documents with positions, and check there is no prox after optimization
		d = new Document();
		f1 = newTextField("f1", "this field has positions", Field.Store.NO);
		d.add(f1);

		for (int i = 0;i < 30;i++)
		{
		  writer.addDocument(d);
		}

		// force merge
		writer.forceMerge(1);
		// flush
		writer.close();

		AssertNoPrx(ram);
		ram.close();
	  }

	  /// <summary>
	  /// make sure we downgrade positions and payloads correctly </summary>
	  public virtual void TestMixing()
	  {
		// no positions
		FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
		ft.IndexOptions = IndexOptions.DOCS_AND_FREQS;

		Directory dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir);

		for (int i = 0; i < 20; i++)
		{
		  Document doc = new Document();
		  if (i < 19 && random().nextBoolean())
		  {
			for (int j = 0; j < 50; j++)
			{
			  doc.add(new TextField("foo", "i have positions", Field.Store.NO));
			}
		  }
		  else
		  {
			for (int j = 0; j < 50; j++)
			{
			  doc.add(new Field("foo", "i have no positions", ft));
			}
		  }
		  iw.addDocument(doc);
		  iw.commit();
		}

		if (random().nextBoolean())
		{
		  iw.forceMerge(1);
		}

		DirectoryReader ir = iw.Reader;
		FieldInfos fis = MultiFields.getMergedFieldInfos(ir);
		Assert.AreEqual(IndexOptions.DOCS_AND_FREQS, fis.fieldInfo("foo").IndexOptions);
		Assert.IsFalse(fis.fieldInfo("foo").hasPayloads());
		iw.close();
		ir.close();
		dir.close(); // checkindex
	  }
	}

}