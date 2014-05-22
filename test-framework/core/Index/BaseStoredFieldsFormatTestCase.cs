using System;
using System.Collections.Generic;
using System.Threading;

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


	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Codec = Lucene.Net.Codecs.Codec;
	using StoredFieldsFormat = Lucene.Net.Codecs.StoredFieldsFormat;
	using Lucene46Codec = Lucene.Net.Codecs.lucene46.Lucene46Codec;
	using SimpleTextCodec = Lucene.Net.Codecs.simpletext.SimpleTextCodec;
	using Document = Lucene.Net.Document.Document;
	using DoubleField = Lucene.Net.Document.DoubleField;
	using Field = Lucene.Net.Document.Field;
	using Store = Lucene.Net.Document.Field.Store;
	using FieldType = Lucene.Net.Document.FieldType;
	using NumericType = Lucene.Net.Document.FieldType.NumericType;
	using FloatField = Lucene.Net.Document.FloatField;
	using IntField = Lucene.Net.Document.IntField;
	using LongField = Lucene.Net.Document.LongField;
	using StoredField = Lucene.Net.Document.StoredField;
	using StringField = Lucene.Net.Document.StringField;
	using TextField = Lucene.Net.Document.TextField;
	using FieldCache = Lucene.Net.Search.FieldCache;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using NumericRangeQuery = Lucene.Net.Search.NumericRangeQuery;
	using Query = Lucene.Net.Search.Query;
	using TermQuery = Lucene.Net.Search.TermQuery;
	using TopDocs = Lucene.Net.Search.TopDocs;
	using Directory = Lucene.Net.Store.Directory;
	using MMapDirectory = Lucene.Net.Store.MMapDirectory;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using Throttling = Lucene.Net.Store.MockDirectoryWrapper.Throttling;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using TestUtil = Lucene.Net.Util.TestUtil;

	using RandomInts = com.carrotsearch.randomizedtesting.generators.RandomInts;
	using RandomPicks = com.carrotsearch.randomizedtesting.generators.RandomPicks;

	/// <summary>
	/// Base class aiming at testing <seealso cref="StoredFieldsFormat stored fields formats"/>.
	/// To test a new format, all you need is to register a new <seealso cref="Codec"/> which
	/// uses it and extend this class and override <seealso cref="#getCodec()"/>.
	/// @lucene.experimental
	/// </summary>
	public abstract class BaseStoredFieldsFormatTestCase : BaseIndexFileFormatTestCase
	{

	  protected internal override void AddRandomFields(Document d)
	  {
		int numValues = Random().Next(3);
		for (int i = 0; i < numValues; ++i)
		{
		  d.add(new StoredField("f", TestUtil.RandomSimpleString(Random(), 100)));
		}
	  }

	  public virtual void TestRandomStoredFields()
	  {
		Directory dir = NewDirectory();
		Random rand = Random();
		RandomIndexWriter w = new RandomIndexWriter(rand, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).setMaxBufferedDocs(TestUtil.NextInt(rand, 5, 20)));
		//w.w.setNoCFSRatio(0.0);
		int docCount = AtLeast(200);
		int fieldCount = TestUtil.NextInt(rand, 1, 5);

		IList<int?> fieldIDs = new List<int?>();

		FieldType customType = new FieldType(TextField.TYPE_STORED);
		customType.Tokenized = false;
		Field idField = NewField("id", "", customType);

		for (int i = 0;i < fieldCount;i++)
		{
		  fieldIDs.Add(i);
		}

		IDictionary<string, Document> docs = new Dictionary<string, Document>();

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: build index docCount=" + docCount);
		}

		FieldType customType2 = new FieldType();
		customType2.Stored = true;
		for (int i = 0;i < docCount;i++)
		{
		  Document doc = new Document();
		  doc.add(idField);
		  string id = "" + i;
		  idField.StringValue = id;
		  docs[id] = doc;
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: add doc id=" + id);
		  }

		  foreach (int field in fieldIDs)
		  {
			string s;
			if (rand.Next(4) != 3)
			{
			  s = TestUtil.RandomUnicodeString(rand, 1000);
			  doc.add(NewField("f" + field, s, customType2));
			}
			else
			{
			  s = null;
			}
		  }
		  w.AddDocument(doc);
		  if (rand.Next(50) == 17)
		  {
			// mixup binding of field name -> Number every so often
			Collections.shuffle(fieldIDs);
		  }
		  if (rand.Next(5) == 3 && i > 0)
		  {
			string delID = "" + rand.Next(i);
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: delete doc id=" + delID);
			}
			w.DeleteDocuments(new Term("id", delID));
			docs.Remove(delID);
		  }
		}

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: " + docs.Count + " docs in index; now load fields");
		}
		if (docs.Count > 0)
		{
		  string[] idsList = docs.Keys.toArray(new string[docs.Count]);

		  for (int x = 0;x < 2;x++)
		  {
			IndexReader r = w.Reader;
			IndexSearcher s = NewSearcher(r);

			if (VERBOSE)
			{
			  Console.WriteLine("TEST: cycle x=" + x + " r=" + r);
			}

			int num = AtLeast(1000);
			for (int iter = 0;iter < num;iter++)
			{
			  string testID = idsList[rand.Next(idsList.Length)];
			  if (VERBOSE)
			  {
				Console.WriteLine("TEST: test id=" + testID);
			  }
			  TopDocs hits = s.search(new TermQuery(new Term("id", testID)), 1);
			  Assert.AreEqual(1, hits.totalHits);
			  Document doc = r.document(hits.scoreDocs[0].doc);
			  Document docExp = docs[testID];
			  for (int i = 0;i < fieldCount;i++)
			  {
				Assert.AreEqual("doc " + testID + ", field f" + fieldCount + " is wrong", docExp.get("f" + i), doc.get("f" + i));
			  }
			}
			r.close();
			w.ForceMerge(1);
		  }
		}
		w.Close();
		dir.close();
	  }

	  // LUCENE-1727: make sure doc fields are stored in order
	  public virtual void TestStoredFieldsOrder()
	  {
		Directory d = NewDirectory();
		IndexWriter w = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
		Document doc = new Document();

		FieldType customType = new FieldType();
		customType.Stored = true;
		doc.add(NewField("zzz", "a b c", customType));
		doc.add(NewField("aaa", "a b c", customType));
		doc.add(NewField("zzz", "1 2 3", customType));
		w.addDocument(doc);
		IndexReader r = w.Reader;
		Document doc2 = r.document(0);
		IEnumerator<IndexableField> it = doc2.Fields.GetEnumerator();
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.IsTrue(it.hasNext());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Field f = (Field) it.next();
		Assert.AreEqual(f.name(), "zzz");
		Assert.AreEqual(f.stringValue(), "a b c");

//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.IsTrue(it.hasNext());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		f = (Field) it.next();
		Assert.AreEqual(f.name(), "aaa");
		Assert.AreEqual(f.stringValue(), "a b c");

//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.IsTrue(it.hasNext());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		f = (Field) it.next();
		Assert.AreEqual(f.name(), "zzz");
		Assert.AreEqual(f.stringValue(), "1 2 3");
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.IsFalse(it.hasNext());
		r.close();
		w.close();
		d.close();
	  }

	  // LUCENE-1219
	  public virtual void TestBinaryFieldOffsetLength()
	  {
		Directory dir = NewDirectory();
		IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
		sbyte[] b = new sbyte[50];
		for (int i = 0;i < 50;i++)
		{
		  b[i] = (sbyte)(i + 77);
		}

		Document doc = new Document();
		Field f = new StoredField("binary", b, 10, 17);
		sbyte[] bx = f.binaryValue().bytes;
		Assert.IsTrue(bx != null);
		Assert.AreEqual(50, bx.Length);
		Assert.AreEqual(10, f.binaryValue().offset);
		Assert.AreEqual(17, f.binaryValue().length);
		doc.add(f);
		w.addDocument(doc);
		w.close();

		IndexReader ir = DirectoryReader.open(dir);
		Document doc2 = ir.document(0);
		IndexableField f2 = doc2.getField("binary");
		b = f2.binaryValue().bytes;
		Assert.IsTrue(b != null);
		Assert.AreEqual(17, b.Length, 17);
		Assert.AreEqual(87, b[0]);
		ir.close();
		dir.close();
	  }

	  public virtual void TestNumericField()
	  {
		Directory dir = NewDirectory();
		RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
		int numDocs = AtLeast(500);
		Number[] answers = new Number[numDocs];
		FieldType.NumericType[] typeAnswers = new FieldType.NumericType[numDocs];
		for (int id = 0;id < numDocs;id++)
		{
		  Document doc = new Document();
		  Field nf;
		  Field sf;
		  Number answer;
		  FieldType.NumericType typeAnswer;
		  if (Random().nextBoolean())
		  {
			// float/double
			if (Random().nextBoolean())
			{
			  float f = Random().nextFloat();
			  answer = Convert.ToSingle(f);
			  nf = new FloatField("nf", f, Field.Store.NO);
			  sf = new StoredField("nf", f);
			  typeAnswer = FieldType.NumericType.FLOAT;
			}
			else
			{
			  double d = Random().NextDouble();
			  answer = Convert.ToDouble(d);
			  nf = new DoubleField("nf", d, Field.Store.NO);
			  sf = new StoredField("nf", d);
			  typeAnswer = FieldType.NumericType.DOUBLE;
			}
		  }
		  else
		  {
			// int/long
			if (Random().nextBoolean())
			{
			  int i = Random().Next();
			  answer = Convert.ToInt32(i);
			  nf = new IntField("nf", i, Field.Store.NO);
			  sf = new StoredField("nf", i);
			  typeAnswer = FieldType.NumericType.INT;
			}
			else
			{
			  long l = Random().nextLong();
			  answer = Convert.ToInt64(l);
			  nf = new LongField("nf", l, Field.Store.NO);
			  sf = new StoredField("nf", l);
			  typeAnswer = FieldType.NumericType.LONG;
			}
		  }
		  doc.add(nf);
		  doc.add(sf);
		  answers[id] = answer;
		  typeAnswers[id] = typeAnswer;
		  FieldType ft = new FieldType(IntField.TYPE_STORED);
		  ft.NumericPrecisionStep = int.MaxValue;
		  doc.add(new IntField("id", id, ft));
		  w.AddDocument(doc);
		}
		DirectoryReader r = w.Reader;
		w.Close();

		Assert.AreEqual(numDocs, r.numDocs());

		foreach (AtomicReaderContext ctx in r.leaves())
		{
		  AtomicReader sub = ctx.reader();
		  FieldCache.Ints ids = FieldCache.DEFAULT.getInts(sub, "id", false);
		  for (int docID = 0;docID < sub.numDocs();docID++)
		  {
			Document doc = sub.document(docID);
			Field f = (Field) doc.getField("nf");
			Assert.IsTrue("got f=" + f, f is StoredField);
			Assert.AreEqual(answers[ids.get(docID)], f.numericValue());
		  }
		}
		r.close();
		dir.close();
	  }

	  public virtual void TestIndexedBit()
	  {
		Directory dir = NewDirectory();
		RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
		Document doc = new Document();
		FieldType onlyStored = new FieldType();
		onlyStored.Stored = true;
		doc.add(new Field("field", "value", onlyStored));
		doc.add(new StringField("field2", "value", Field.Store.YES));
		w.AddDocument(doc);
		IndexReader r = w.Reader;
		w.Close();
		Assert.IsFalse(r.document(0).getField("field").fieldType().indexed());
		Assert.IsTrue(r.document(0).getField("field2").fieldType().indexed());
		r.close();
		dir.close();
	  }

	  public virtual void TestReadSkip()
	  {
		Directory dir = NewDirectory();
		IndexWriterConfig iwConf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
		iwConf.MaxBufferedDocs = RandomInts.randomIntBetween(Random(), 2, 30);
		RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwConf);

		FieldType ft = new FieldType();
		ft.Stored = true;
		ft.freeze();

		string @string = TestUtil.RandomSimpleString(Random(), 50);
		sbyte[] bytes = @string.getBytes(StandardCharsets.UTF_8);
		long l = Random().nextBoolean() ? Random().Next(42) : Random().nextLong();
		int i = Random().nextBoolean() ? Random().Next(42) : Random().Next();
		float f = Random().nextFloat();
		double d = Random().NextDouble();

		IList<Field> fields = Arrays.asList(new Field("bytes", bytes, ft), new Field("string", @string, ft), new LongField("long", l, Field.Store.YES), new IntField("int", i, Field.Store.YES), new FloatField("float", f, Field.Store.YES), new DoubleField("double", d, Field.Store.YES)
	   );

		for (int k = 0; k < 100; ++k)
		{
		  Document doc = new Document();
		  foreach (Field fld in fields)
		  {
			doc.add(fld);
		  }
		  iw.w.addDocument(doc);
		}
		iw.Commit();

		DirectoryReader reader = DirectoryReader.open(dir);
		int docID = Random().Next(100);
		foreach (Field fld in fields)
		{
		  string fldName = fld.name();
		  Document sDoc = reader.document(docID, Collections.singleton(fldName));
		  IndexableField sField = sDoc.getField(fldName);
		  if (typeof(Field).Equals(fld.GetType()))
		  {
			Assert.AreEqual(fld.binaryValue(), sField.binaryValue());
			Assert.AreEqual(fld.stringValue(), sField.stringValue());
		  }
		  else
		  {
			Assert.AreEqual(fld.numericValue(), sField.numericValue());
		  }
		}
		reader.close();
		iw.Close();
		dir.close();
	  }

	  public virtual void TestEmptyDocs()
	  {
		Directory dir = NewDirectory();
		IndexWriterConfig iwConf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
		iwConf.MaxBufferedDocs = RandomInts.randomIntBetween(Random(), 2, 30);
		RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwConf);

		// make sure that the fact that documents might be empty is not a problem
		Document emptyDoc = new Document();
		int numDocs = Random().nextBoolean() ? 1 : AtLeast(1000);
		for (int i = 0; i < numDocs; ++i)
		{
		  iw.AddDocument(emptyDoc);
		}
		iw.Commit();
		DirectoryReader rd = DirectoryReader.open(dir);
		for (int i = 0; i < numDocs; ++i)
		{
		  Document doc = rd.document(i);
		  Assert.IsNotNull(doc);
		  Assert.IsTrue(doc.Fields.Empty);
		}
		rd.close();

		iw.Close();
		dir.close();
	  }

	  public virtual void TestConcurrentReads()
	  {
		Directory dir = NewDirectory();
		IndexWriterConfig iwConf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
		iwConf.MaxBufferedDocs = RandomInts.randomIntBetween(Random(), 2, 30);
		RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwConf);

		// make sure the readers are properly cloned
		Document doc = new Document();
		Field field = new StringField("fld", "", Field.Store.YES);
		doc.add(field);
		int numDocs = AtLeast(1000);
		for (int i = 0; i < numDocs; ++i)
		{
		  field.StringValue = "" + i;
		  iw.AddDocument(doc);
		}
		iw.Commit();

		DirectoryReader rd = DirectoryReader.open(dir);
		IndexSearcher searcher = new IndexSearcher(rd);
		int concurrentReads = AtLeast(5);
		int readsPerThread = AtLeast(50);
		IList<Thread> readThreads = new List<Thread>();
		AtomicReference<Exception> ex = new AtomicReference<Exception>();
		for (int i = 0; i < concurrentReads; ++i)
		{
		  readThreads.Add(new ThreadAnonymousInnerClassHelper(this, numDocs, rd, searcher, readsPerThread, ex, i));
		}
		foreach (Thread thread in readThreads)
		{
		  thread.Start();
		}
		foreach (Thread thread in readThreads)
		{
		  thread.Join();
		}
		rd.close();
		if (ex.get() != null)
		{
		  throw ex.get();
		}

		iw.Close();
		dir.close();
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly BaseStoredFieldsFormatTestCase OuterInstance;

		  private int NumDocs;
		  private DirectoryReader Rd;
		  private IndexSearcher Searcher;
		  private int ReadsPerThread;
		  private AtomicReference<Exception> Ex;
		  private int i;

		  public ThreadAnonymousInnerClassHelper(BaseStoredFieldsFormatTestCase outerInstance, int numDocs, DirectoryReader rd, IndexSearcher searcher, int readsPerThread, AtomicReference<Exception> ex, int i)
		  {
			  this.OuterInstance = outerInstance;
			  this.NumDocs = numDocs;
			  this.Rd = rd;
			  this.Searcher = searcher;
			  this.ReadsPerThread = readsPerThread;
			  this.Ex = ex;
			  this.i = i;
		  }


		  internal int[] queries;

//JAVA TO C# CONVERTER TODO TASK: Initialization blocks declared within anonymous inner classes are not converted:
	//	  {
	//		queries = new int[readsPerThread];
	//		for (int i = 0; i < queries.length; ++i)
	//		{
	//		  queries[i] = random().nextInt(numDocs);
	//		}
	//	  }

		  public override void Run()
		  {
			foreach (int q in queries)
			{
			  Query query = new TermQuery(new Term("fld", "" + q));
			  try
			  {
				TopDocs topDocs = Searcher.search(query, 1);
				if (topDocs.totalHits != 1)
				{
				  throw new IllegalStateException("Expected 1 hit, got " + topDocs.totalHits);
				}
				Document sdoc = Rd.document(topDocs.scoreDocs[0].doc);
				if (sdoc == null || sdoc.get("fld") == null)
				{
				  throw new IllegalStateException("Could not find document " + q);
				}
				if (!Convert.ToString(q).Equals(sdoc.get("fld")))
				{
				  throw new IllegalStateException("Expected " + q + ", but got " + sdoc.get("fld"));
				}
			  }
			  catch (Exception e)
			  {
				Ex.compareAndSet(null, e);
			  }
			}
		  }
	  }

	  private sbyte[] RandomByteArray(int length, int max)
	  {
		sbyte[] result = new sbyte[length];
		for (int i = 0; i < length; ++i)
		{
		  result[i] = (sbyte) Random().Next(max);
		}
		return result;
	  }

	  public virtual void TestWriteReadMerge()
	  {
		// get another codec, other than the default: so we are merging segments across different codecs
		Codec otherCodec;
		if ("SimpleText".Equals(Codec.Default.Name))
		{
		  otherCodec = new Lucene46Codec();
		}
		else
		{
		  otherCodec = new SimpleTextCodec();
		}
		Directory dir = NewDirectory();
		IndexWriterConfig iwConf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
		iwConf.MaxBufferedDocs = RandomInts.randomIntBetween(Random(), 2, 30);
		RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwConf.clone());

		int docCount = AtLeast(200);
		sbyte[][][] data = new sbyte [docCount][][];
		for (int i = 0; i < docCount; ++i)
		{
		  int fieldCount = Rarely() ? RandomInts.randomIntBetween(Random(), 1, 500) : RandomInts.randomIntBetween(Random(), 1, 5);
		  data[i] = new sbyte[fieldCount][];
		  for (int j = 0; j < fieldCount; ++j)
		  {
			int length = Rarely() ? Random().Next(1000) : Random().Next(10);
			int max = Rarely() ? 256 : 2;
			data[i][j] = RandomByteArray(length, max);
		  }
		}

		FieldType type = new FieldType(StringField.TYPE_STORED);
		type.Indexed = false;
		type.freeze();
		IntField id = new IntField("id", 0, Field.Store.YES);
		for (int i = 0; i < data.Length; ++i)
		{
		  Document doc = new Document();
		  doc.add(id);
		  id.IntValue = i;
		  for (int j = 0; j < data[i].Length; ++j)
		  {
			Field f = new Field("bytes" + j, data[i][j], type);
			doc.add(f);
		  }
		  iw.w.addDocument(doc);
		  if (Random().nextBoolean() && (i % (data.Length / 10) == 0))
		  {
			iw.w.close();
			// test merging against a non-compressing codec
			if (iwConf.Codec == otherCodec)
			{
			  iwConf.Codec = Codec.Default;
			}
			else
			{
			  iwConf.Codec = otherCodec;
			}
			iw = new RandomIndexWriter(Random(), dir, iwConf.clone());
		  }
		}

		for (int i = 0; i < 10; ++i)
		{
		  int min = Random().Next(data.Length);
		  int max = min + Random().Next(20);
		  iw.DeleteDocuments(NumericRangeQuery.newIntRange("id", min, max, true, false));
		}

		iw.ForceMerge(2); // force merges with deletions

		iw.Commit();

		DirectoryReader ir = DirectoryReader.open(dir);
		Assert.IsTrue(ir.numDocs() > 0);
		int numDocs = 0;
		for (int i = 0; i < ir.maxDoc(); ++i)
		{
		  Document doc = ir.document(i);
		  if (doc == null)
		  {
			continue;
		  }
		  ++numDocs;
		  int docId = (int)doc.getField("id").numericValue();
		  Assert.AreEqual(data[docId].Length + 1, doc.Fields.size());
		  for (int j = 0; j < data[docId].Length; ++j)
		  {
			sbyte[] arr = data[docId][j];
			BytesRef arr2Ref = doc.getBinaryValue("bytes" + j);
			sbyte[] arr2 = Arrays.copyOfRange(arr2Ref.bytes, arr2Ref.offset, arr2Ref.offset + arr2Ref.length);
			assertArrayEquals(arr, arr2);
		  }
		}
		Assert.IsTrue(ir.numDocs() <= numDocs);
		ir.close();

		iw.DeleteAll();
		iw.Commit();
		iw.ForceMerge(1);

		iw.Close();
		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Nightly public void testBigDocuments() throws java.io.IOException
	  public virtual void TestBigDocuments()
	  {
		// "big" as "much bigger than the chunk size"
		// for this test we force a FS dir
		// we can't just use newFSDirectory, because this test doesn't really index anything.
		// so if we get NRTCachingDir+SimpleText, we make massive stored fields and OOM (LUCENE-4484)
		Directory dir = new MockDirectoryWrapper(Random(), new MMapDirectory(CreateTempDir("testBigDocuments")));
		IndexWriterConfig iwConf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
		iwConf.MaxBufferedDocs = RandomInts.randomIntBetween(Random(), 2, 30);
		RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwConf);

		if (dir is MockDirectoryWrapper)
		{
		  ((MockDirectoryWrapper) dir).Throttling = MockDirectoryWrapper.Throttling.NEVER;
		}

		Document emptyDoc = new Document(); // emptyDoc
		Document bigDoc1 = new Document(); // lot of small fields
		Document bigDoc2 = new Document(); // 1 very big field

		Field idField = new StringField("id", "", Field.Store.NO);
		emptyDoc.add(idField);
		bigDoc1.add(idField);
		bigDoc2.add(idField);

		FieldType onlyStored = new FieldType(StringField.TYPE_STORED);
		onlyStored.Indexed = false;

		Field smallField = new Field("fld", RandomByteArray(Random().Next(10), 256), onlyStored);
		int numFields = RandomInts.randomIntBetween(Random(), 500000, 1000000);
		for (int i = 0; i < numFields; ++i)
		{
		  bigDoc1.add(smallField);
		}

		Field bigField = new Field("fld", RandomByteArray(RandomInts.randomIntBetween(Random(), 1000000, 5000000), 2), onlyStored);
		bigDoc2.add(bigField);

		int numDocs = AtLeast(5);
		Document[] docs = new Document[numDocs];
		for (int i = 0; i < numDocs; ++i)
		{
		  docs[i] = RandomPicks.randomFrom(Random(), Arrays.asList(emptyDoc, bigDoc1, bigDoc2));
		}
		for (int i = 0; i < numDocs; ++i)
		{
		  idField.StringValue = "" + i;
		  iw.AddDocument(docs[i]);
		  if (Random().Next(numDocs) == 0)
		  {
			iw.Commit();
		  }
		}
		iw.Commit();
		iw.ForceMerge(1); // look at what happens when big docs are merged
		DirectoryReader rd = DirectoryReader.open(dir);
		IndexSearcher searcher = new IndexSearcher(rd);
		for (int i = 0; i < numDocs; ++i)
		{
		  Query query = new TermQuery(new Term("id", "" + i));
		  TopDocs topDocs = searcher.search(query, 1);
		  Assert.AreEqual("" + i, 1, topDocs.totalHits);
		  Document doc = rd.document(topDocs.scoreDocs[0].doc);
		  Assert.IsNotNull(doc);
		  IndexableField[] fieldValues = doc.getFields("fld");
		  Assert.AreEqual(docs[i].getFields("fld").length, fieldValues.Length);
		  if (fieldValues.Length > 0)
		  {
			Assert.AreEqual(docs[i].getFields("fld")[0].binaryValue(), fieldValues[0].binaryValue());
		  }
		}
		rd.close();
		iw.Close();
		dir.close();
	  }

	  public virtual void TestBulkMergeWithDeletes()
	  {
		int numDocs = AtLeast(200);
		Directory dir = NewDirectory();
		RandomIndexWriter w = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).setMergePolicy(NoMergePolicy.COMPOUND_FILES));
		for (int i = 0; i < numDocs; ++i)
		{
		  Document doc = new Document();
		  doc.add(new StringField("id", Convert.ToString(i), Field.Store.YES));
		  doc.add(new StoredField("f", TestUtil.RandomSimpleString(Random())));
		  w.AddDocument(doc);
		}
		int deleteCount = TestUtil.NextInt(Random(), 5, numDocs);
		for (int i = 0; i < deleteCount; ++i)
		{
		  int id = Random().Next(numDocs);
		  w.DeleteDocuments(new Term("id", Convert.ToString(id)));
		}
		w.Commit();
		w.Close();
		w = new RandomIndexWriter(Random(), dir);
		w.ForceMerge(TestUtil.NextInt(Random(), 1, 3));
		w.Commit();
		w.Close();
		TestUtil.CheckIndex(dir);
		dir.close();
	  }

	}

}