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
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestOmitNorms : LuceneTestCase
	{
	  // Tests whether the DocumentWriter correctly enable the
	  // omitNorms bit in the FieldInfo
	  public virtual void TestOmitNorms()
	  {
		Directory ram = newDirectory();
		Analyzer analyzer = new MockAnalyzer(random());
		IndexWriter writer = new IndexWriter(ram, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
		Document d = new Document();

		// this field will have norms
		Field f1 = newTextField("f1", "this field has norms", Field.Store.NO);
		d.add(f1);

		// this field will NOT have norms
		FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
		customType.OmitNorms = true;
		Field f2 = newField("f2", "this field has NO norms in all docs", customType);
		d.add(f2);

		writer.addDocument(d);
		writer.forceMerge(1);
		// now we add another document which has term freq for field f2 and not for f1 and verify if the SegmentMerger
		// keep things constant
		d = new Document();

		// Reverse
		d.add(newField("f1", "this field has norms", customType));

		d.add(newTextField("f2", "this field has NO norms in all docs", Field.Store.NO));

		writer.addDocument(d);

		// force merge
		writer.forceMerge(1);
		// flush
		writer.close();

		SegmentReader reader = getOnlySegmentReader(DirectoryReader.open(ram));
		FieldInfos fi = reader.FieldInfos;
		Assert.IsTrue("OmitNorms field bit should be set.", fi.fieldInfo("f1").omitsNorms());
		Assert.IsTrue("OmitNorms field bit should be set.", fi.fieldInfo("f2").omitsNorms());

		reader.close();
		ram.close();
	  }

	  // Tests whether merging of docs that have different
	  // omitNorms for the same field works
	  public virtual void TestMixedMerge()
	  {
		Directory ram = newDirectory();
		Analyzer analyzer = new MockAnalyzer(random());
		IndexWriter writer = new IndexWriter(ram, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setMaxBufferedDocs(3).setMergePolicy(newLogMergePolicy(2)));
		Document d = new Document();

		// this field will have norms
		Field f1 = newTextField("f1", "this field has norms", Field.Store.NO);
		d.add(f1);

		// this field will NOT have norms
		FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
		customType.OmitNorms = true;
		Field f2 = newField("f2", "this field has NO norms in all docs", customType);
		d.add(f2);

		for (int i = 0; i < 30; i++)
		{
		  writer.addDocument(d);
		}

		// now we add another document which has norms for field f2 and not for f1 and verify if the SegmentMerger
		// keep things constant
		d = new Document();

		// Reverese
		d.add(newField("f1", "this field has norms", customType));

		d.add(newTextField("f2", "this field has NO norms in all docs", Field.Store.NO));

		for (int i = 0; i < 30; i++)
		{
		  writer.addDocument(d);
		}

		// force merge
		writer.forceMerge(1);
		// flush
		writer.close();

		SegmentReader reader = getOnlySegmentReader(DirectoryReader.open(ram));
		FieldInfos fi = reader.FieldInfos;
		Assert.IsTrue("OmitNorms field bit should be set.", fi.fieldInfo("f1").omitsNorms());
		Assert.IsTrue("OmitNorms field bit should be set.", fi.fieldInfo("f2").omitsNorms());

		reader.close();
		ram.close();
	  }

	  // Make sure first adding docs that do not omitNorms for
	  // field X, then adding docs that do omitNorms for that same
	  // field, 
	  public virtual void TestMixedRAM()
	  {
		Directory ram = newDirectory();
		Analyzer analyzer = new MockAnalyzer(random());
		IndexWriter writer = new IndexWriter(ram, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setMaxBufferedDocs(10).setMergePolicy(newLogMergePolicy(2)));
		Document d = new Document();

		// this field will have norms
		Field f1 = newTextField("f1", "this field has norms", Field.Store.NO);
		d.add(f1);

		// this field will NOT have norms

		FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
		customType.OmitNorms = true;
		Field f2 = newField("f2", "this field has NO norms in all docs", customType);
		d.add(f2);

		for (int i = 0; i < 5; i++)
		{
		  writer.addDocument(d);
		}

		for (int i = 0; i < 20; i++)
		{
		  writer.addDocument(d);
		}

		// force merge
		writer.forceMerge(1);

		// flush
		writer.close();

		SegmentReader reader = getOnlySegmentReader(DirectoryReader.open(ram));
		FieldInfos fi = reader.FieldInfos;
		Assert.IsTrue("OmitNorms field bit should not be set.", !fi.fieldInfo("f1").omitsNorms());
		Assert.IsTrue("OmitNorms field bit should be set.", fi.fieldInfo("f2").omitsNorms());

		reader.close();
		ram.close();
	  }

	  private void AssertNoNrm(Directory dir)
	  {
		string[] files = dir.listAll();
		for (int i = 0; i < files.Length; i++)
		{
		  // TODO: this relies upon filenames
		  Assert.IsFalse(files[i].EndsWith(".nrm") || files[i].EndsWith(".len"));
		}
	  }

	  // Verifies no *.nrm exists when all fields omit norms:
	  public virtual void TestNoNrmFile()
	  {
		Directory ram = newDirectory();
		Analyzer analyzer = new MockAnalyzer(random());
		IndexWriter writer = new IndexWriter(ram, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setMaxBufferedDocs(3).setMergePolicy(newLogMergePolicy()));
		LogMergePolicy lmp = (LogMergePolicy) writer.Config.MergePolicy;
		lmp.MergeFactor = 2;
		lmp.NoCFSRatio = 0.0;
		Document d = new Document();

		FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
		customType.OmitNorms = true;
		Field f1 = newField("f1", "this field has no norms", customType);
		d.add(f1);

		for (int i = 0; i < 30; i++)
		{
		  writer.addDocument(d);
		}

		writer.commit();

		AssertNoNrm(ram);

		// force merge
		writer.forceMerge(1);
		// flush
		writer.close();

		AssertNoNrm(ram);
		ram.close();
	  }

	  /// <summary>
	  /// Tests various combinations of omitNorms=true/false, the field not existing at all,
	  /// ensuring that only omitNorms is 'viral'.
	  /// Internally checks that MultiNorms.norms() is consistent (returns the same bytes)
	  /// as the fully merged equivalent.
	  /// </summary>
	  public virtual void TestOmitNormsCombos()
	  {
		// indexed with norms
		FieldType customType = new FieldType(TextField.TYPE_STORED);
		Field norms = new Field("foo", "a", customType);
		// indexed without norms
		FieldType customType1 = new FieldType(TextField.TYPE_STORED);
		customType1.OmitNorms = true;
		Field noNorms = new Field("foo", "a", customType1);
		// not indexed, but stored
		FieldType customType2 = new FieldType();
		customType2.Stored = true;
		Field noIndex = new Field("foo", "a", customType2);
		// not indexed but stored, omitNorms is set
		FieldType customType3 = new FieldType();
		customType3.Stored = true;
		customType3.OmitNorms = true;
		Field noNormsNoIndex = new Field("foo", "a", customType3);
		// not indexed nor stored (doesnt exist at all, we index a different field instead)
		Field emptyNorms = new Field("bar", "a", customType);

		Assert.IsNotNull(GetNorms("foo", norms, norms));
		assertNull(GetNorms("foo", norms, noNorms));
		Assert.IsNotNull(GetNorms("foo", norms, noIndex));
		Assert.IsNotNull(GetNorms("foo", norms, noNormsNoIndex));
		Assert.IsNotNull(GetNorms("foo", norms, emptyNorms));
		assertNull(GetNorms("foo", noNorms, noNorms));
		assertNull(GetNorms("foo", noNorms, noIndex));
		assertNull(GetNorms("foo", noNorms, noNormsNoIndex));
		assertNull(GetNorms("foo", noNorms, emptyNorms));
		assertNull(GetNorms("foo", noIndex, noIndex));
		assertNull(GetNorms("foo", noIndex, noNormsNoIndex));
		assertNull(GetNorms("foo", noIndex, emptyNorms));
		assertNull(GetNorms("foo", noNormsNoIndex, noNormsNoIndex));
		assertNull(GetNorms("foo", noNormsNoIndex, emptyNorms));
		assertNull(GetNorms("foo", emptyNorms, emptyNorms));
	  }

	  /// <summary>
	  /// Indexes at least 1 document with f1, and at least 1 document with f2.
	  /// returns the norms for "field".
	  /// </summary>
	  internal virtual NumericDocValues GetNorms(string field, Field f1, Field f2)
	  {
		Directory dir = newDirectory();
		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy());
		RandomIndexWriter riw = new RandomIndexWriter(random(), dir, iwc);

		// add f1
		Document d = new Document();
		d.add(f1);
		riw.addDocument(d);

		// add f2
		d = new Document();
		d.add(f2);
		riw.addDocument(d);

		// add a mix of f1's and f2's
		int numExtraDocs = TestUtil.Next(random(), 1, 1000);
		for (int i = 0; i < numExtraDocs; i++)
		{
		  d = new Document();
		  d.add(random().nextBoolean() ? f1 : f2);
		  riw.addDocument(d);
		}

		IndexReader ir1 = riw.Reader;
		// todo: generalize
		NumericDocValues norms1 = MultiDocValues.getNormValues(ir1, field);

		// fully merge and validate MultiNorms against single segment.
		riw.forceMerge(1);
		DirectoryReader ir2 = riw.Reader;
		NumericDocValues norms2 = getOnlySegmentReader(ir2).getNormValues(field);

		if (norms1 == null)
		{
		  assertNull(norms2);
		}
		else
		{
		  for (int docID = 0;docID < ir1.maxDoc();docID++)
		  {
			Assert.AreEqual(norms1.get(docID), norms2.get(docID));
		  }
		}
		ir1.close();
		ir2.close();
		riw.close();
		dir.close();
		return norms1;
	  }
	}

}