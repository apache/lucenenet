using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
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


	using Lucene.Net.Analysis;
	using PayloadAttribute = Lucene.Net.Analysis.Tokenattributes.PayloadAttribute;
	using CharTermAttribute = Lucene.Net.Analysis.Tokenattributes.CharTermAttribute;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using TextField = Lucene.Net.Document.TextField;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using Bits = Lucene.Net.Util.Bits;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestPayloads : LuceneTestCase
	{

		// Simple tests to test the payloads
		public virtual void TestPayload()
		{
			BytesRef payload = new BytesRef("this is a test!");
			Assert.AreEqual("Wrong payload length.", "this is a test!".Length, payload.length);

			BytesRef clone = payload.clone();
			Assert.AreEqual(payload.length, clone.length);
			for (int i = 0; i < payload.length; i++)
			{
			  Assert.AreEqual(payload.bytes[i + payload.offset], clone.bytes[i + clone.offset]);
			}

		}

		// Tests whether the DocumentWriter and SegmentMerger correctly enable the
		// payload bit in the FieldInfo
		public virtual void TestPayloadFieldBit()
		{
			Directory ram = newDirectory();
			PayloadAnalyzer analyzer = new PayloadAnalyzer();
			IndexWriter writer = new IndexWriter(ram, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
			Document d = new Document();
			// this field won't have any payloads
			d.add(newTextField("f1", "this field has no payloads", Field.Store.NO));
			// this field will have payloads in all docs, however not for all term positions,
			// so this field is used to check if the DocumentWriter correctly enables the payloads bit
			// even if only some term positions have payloads
			d.add(newTextField("f2", "this field has payloads in all docs", Field.Store.NO));
			d.add(newTextField("f2", "this field has payloads in all docs NO PAYLOAD", Field.Store.NO));
			// this field is used to verify if the SegmentMerger enables payloads for a field if it has payloads 
			// enabled in only some documents
			d.add(newTextField("f3", "this field has payloads in some docs", Field.Store.NO));
			// only add payload data for field f2
			analyzer.SetPayloadData("f2", "somedata".getBytes(StandardCharsets.UTF_8), 0, 1);
			writer.addDocument(d);
			// flush
			writer.close();

		  SegmentReader reader = getOnlySegmentReader(DirectoryReader.open(ram));
			FieldInfos fi = reader.FieldInfos;
			Assert.IsFalse("Payload field bit should not be set.", fi.fieldInfo("f1").hasPayloads());
			Assert.IsTrue("Payload field bit should be set.", fi.fieldInfo("f2").hasPayloads());
			Assert.IsFalse("Payload field bit should not be set.", fi.fieldInfo("f3").hasPayloads());
			reader.close();

			// now we add another document which has payloads for field f3 and verify if the SegmentMerger
			// enabled payloads for that field
			analyzer = new PayloadAnalyzer(); // Clear payload state for each field
			writer = new IndexWriter(ram, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setOpenMode(OpenMode.CREATE));
			d = new Document();
			d.add(newTextField("f1", "this field has no payloads", Field.Store.NO));
			d.add(newTextField("f2", "this field has payloads in all docs", Field.Store.NO));
			d.add(newTextField("f2", "this field has payloads in all docs", Field.Store.NO));
			d.add(newTextField("f3", "this field has payloads in some docs", Field.Store.NO));
			// add payload data for field f2 and f3
			analyzer.SetPayloadData("f2", "somedata".getBytes(StandardCharsets.UTF_8), 0, 1);
			analyzer.SetPayloadData("f3", "somedata".getBytes(StandardCharsets.UTF_8), 0, 3);
			writer.addDocument(d);

			// force merge
			writer.forceMerge(1);
			// flush
			writer.close();

		  reader = getOnlySegmentReader(DirectoryReader.open(ram));
			fi = reader.FieldInfos;
			Assert.IsFalse("Payload field bit should not be set.", fi.fieldInfo("f1").hasPayloads());
			Assert.IsTrue("Payload field bit should be set.", fi.fieldInfo("f2").hasPayloads());
			Assert.IsTrue("Payload field bit should be set.", fi.fieldInfo("f3").hasPayloads());
			reader.close();
			ram.close();
		}

		// Tests if payloads are correctly stored and loaded using both RamDirectory and FSDirectory
		public virtual void TestPayloadsEncoding()
		{
			Directory dir = newDirectory();
			PerformTest(dir);
			dir.close();
		}

		// builds an index with payloads in the given Directory and performs
		// different tests to verify the payload encoding
		private void PerformTest(Directory dir)
		{
			PayloadAnalyzer analyzer = new PayloadAnalyzer();
			IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setOpenMode(OpenMode.CREATE).setMergePolicy(newLogMergePolicy()));

			// should be in sync with value in TermInfosWriter
			const int skipInterval = 16;

			const int numTerms = 5;
			const string fieldName = "f1";

			int numDocs = skipInterval + 1;
			// create content for the test documents with just a few terms
			Term[] terms = GenerateTerms(fieldName, numTerms);
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < terms.Length; i++)
			{
				sb.Append(terms[i].text());
				sb.Append(" ");
			}
			string content = sb.ToString();


			int payloadDataLength = numTerms * numDocs * 2 + numTerms * numDocs * (numDocs - 1) / 2;
			sbyte[] payloadData = GenerateRandomData(payloadDataLength);

			Document d = new Document();
			d.add(newTextField(fieldName, content, Field.Store.NO));
			// add the same document multiple times to have the same payload lengths for all
			// occurrences within two consecutive skip intervals
			int offset = 0;
			for (int i = 0; i < 2 * numDocs; i++)
			{
				analyzer = new PayloadAnalyzer(fieldName, payloadData, offset, 1);
				offset += numTerms;
				writer.addDocument(d, analyzer);
			}

			// make sure we create more than one segment to test merging
			writer.commit();

			// now we make sure to have different payload lengths next at the next skip point        
			for (int i = 0; i < numDocs; i++)
			{
				analyzer = new PayloadAnalyzer(fieldName, payloadData, offset, i);
				offset += i * numTerms;
				writer.addDocument(d, analyzer);
			}

			writer.forceMerge(1);
			// flush
			writer.close();


			/*
			 * Verify the index
			 * first we test if all payloads are stored correctly
			 */        
			IndexReader reader = DirectoryReader.open(dir);

			sbyte[] verifyPayloadData = new sbyte[payloadDataLength];
			offset = 0;
			DocsAndPositionsEnum[] tps = new DocsAndPositionsEnum[numTerms];
			for (int i = 0; i < numTerms; i++)
			{
			  tps[i] = MultiFields.getTermPositionsEnum(reader, MultiFields.getLiveDocs(reader), terms[i].field(), new BytesRef(terms[i].text()));
			}

			while (tps[0].nextDoc() != DocIdSetIterator.NO_MORE_DOCS)
			{
				for (int i = 1; i < numTerms; i++)
				{
					tps[i].nextDoc();
				}
				int freq = tps[0].freq();

				for (int i = 0; i < freq; i++)
				{
					for (int j = 0; j < numTerms; j++)
					{
						tps[j].nextPosition();
						BytesRef br = tps[j].Payload;
						if (br != null)
						{
						  Array.Copy(br.bytes, br.offset, verifyPayloadData, offset, br.length);
						  offset += br.length;
						}
					}
				}
			}

			AssertByteArrayEquals(payloadData, verifyPayloadData);

			/*
			 *  test lazy skipping
			 */        
			DocsAndPositionsEnum tp = MultiFields.getTermPositionsEnum(reader, MultiFields.getLiveDocs(reader), terms[0].field(), new BytesRef(terms[0].text()));
			tp.nextDoc();
			tp.nextPosition();
			// NOTE: prior rev of this test was failing to first
			// call next here:
			tp.nextDoc();
			// now we don't read this payload
			tp.nextPosition();
			BytesRef payload = tp.Payload;
			Assert.AreEqual("Wrong payload length.", 1, payload.length);
			Assert.AreEqual(payload.bytes[payload.offset], payloadData[numTerms]);
			tp.nextDoc();
			tp.nextPosition();

			// we don't read this payload and skip to a different document
			tp.advance(5);
			tp.nextPosition();
			payload = tp.Payload;
			Assert.AreEqual("Wrong payload length.", 1, payload.length);
			Assert.AreEqual(payload.bytes[payload.offset], payloadData[5 * numTerms]);


			/*
			 * Test different lengths at skip points
			 */
			tp = MultiFields.getTermPositionsEnum(reader, MultiFields.getLiveDocs(reader), terms[1].field(), new BytesRef(terms[1].text()));
			tp.nextDoc();
			tp.nextPosition();
			Assert.AreEqual("Wrong payload length.", 1, tp.Payload.length);
			tp.advance(skipInterval - 1);
			tp.nextPosition();
			Assert.AreEqual("Wrong payload length.", 1, tp.Payload.length);
			tp.advance(2 * skipInterval - 1);
			tp.nextPosition();
			Assert.AreEqual("Wrong payload length.", 1, tp.Payload.length);
			tp.advance(3 * skipInterval - 1);
			tp.nextPosition();
			Assert.AreEqual("Wrong payload length.", 3 * skipInterval - 2 * numDocs - 1, tp.Payload.length);

			reader.close();

			// test long payload
			analyzer = new PayloadAnalyzer();
			writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setOpenMode(OpenMode.CREATE));
			string singleTerm = "lucene";

			d = new Document();
			d.add(newTextField(fieldName, singleTerm, Field.Store.NO));
			// add a payload whose length is greater than the buffer size of BufferedIndexOutput
			payloadData = GenerateRandomData(2000);
			analyzer.SetPayloadData(fieldName, payloadData, 100, 1500);
			writer.addDocument(d);


			writer.forceMerge(1);
			// flush
			writer.close();

			reader = DirectoryReader.open(dir);
			tp = MultiFields.getTermPositionsEnum(reader, MultiFields.getLiveDocs(reader), fieldName, new BytesRef(singleTerm));
			tp.nextDoc();
			tp.nextPosition();

			BytesRef br = tp.Payload;
			verifyPayloadData = new sbyte[br.length];
			sbyte[] portion = new sbyte[1500];
			Array.Copy(payloadData, 100, portion, 0, 1500);

			AssertByteArrayEquals(portion, br.bytes, br.offset, br.length);
			reader.close();

		}

		internal static readonly Charset Utf8 = StandardCharsets.UTF_8;

		private void GenerateRandomData(sbyte[] data)
		{
		  // this test needs the random data to be valid unicode
		  string s = TestUtil.randomFixedByteLengthUnicodeString(random(), data.Length);
		  sbyte[] b = s.getBytes(Utf8);
		  Debug.Assert(b.Length == data.Length);
		  Array.Copy(b, 0, data, 0, b.Length);
		}

		private sbyte[] GenerateRandomData(int n)
		{
			sbyte[] data = new sbyte[n];
			GenerateRandomData(data);
			return data;
		}

		private Term[] GenerateTerms(string fieldName, int n)
		{
			int maxDigits = (int)(Math.Log(n) / Math.Log(10));
			Term[] terms = new Term[n];
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < n; i++)
			{
				sb.Length = 0;
				sb.Append("t");
				int zeros = maxDigits - (int)(Math.Log(i) / Math.Log(10));
				for (int j = 0; j < zeros; j++)
				{
					sb.Append("0");
				}
				sb.Append(i);
				terms[i] = new Term(fieldName, sb.ToString());
			}
			return terms;
		}


		internal virtual void AssertByteArrayEquals(sbyte[] b1, sbyte[] b2)
		{
			if (b1.Length != b2.Length)
			{
			  Assert.Fail("Byte arrays have different lengths: " + b1.Length + ", " + b2.Length);
			}

			for (int i = 0; i < b1.Length; i++)
			{
			  if (b1[i] != b2[i])
			  {
				Assert.Fail("Byte arrays different at index " + i + ": " + b1[i] + ", " + b2[i]);
			  }
			}
		}

	  internal virtual void AssertByteArrayEquals(sbyte[] b1, sbyte[] b2, int b2offset, int b2length)
	  {
			if (b1.Length != b2length)
			{
			  Assert.Fail("Byte arrays have different lengths: " + b1.Length + ", " + b2length);
			}

			for (int i = 0; i < b1.Length; i++)
			{
			  if (b1[i] != b2[b2offset + i])
			  {
				Assert.Fail("Byte arrays different at index " + i + ": " + b1[i] + ", " + b2[b2offset + i]);
			  }
			}
	  }


		/// <summary>
		/// this Analyzer uses an WhitespaceTokenizer and PayloadFilter.
		/// </summary>
		private class PayloadAnalyzer : Analyzer
		{
			internal IDictionary<string, PayloadData> FieldToData = new Dictionary<string, PayloadData>();

			public PayloadAnalyzer() : base(PER_FIELD_REUSE_STRATEGY)
			{
			}

			public PayloadAnalyzer(string field, sbyte[] data, int offset, int length) : base(PER_FIELD_REUSE_STRATEGY)
			{
				SetPayloadData(field, data, offset, length);
			}

			internal virtual void SetPayloadData(string field, sbyte[] data, int offset, int length)
			{
				FieldToData[field] = new PayloadData(data, offset, length);
			}

			public override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
			{
				PayloadData payload = FieldToData[fieldName];
				Tokenizer ts = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
				TokenStream tokenStream = (payload != null) ? new PayloadFilter(ts, payload.Data, payload.Offset, payload.Length) : ts;
				return new TokenStreamComponents(ts, tokenStream);
			}

			private class PayloadData
			{
				internal sbyte[] Data;
				internal int Offset;
				internal int Length;

				internal PayloadData(sbyte[] data, int offset, int length)
				{
					this.Data = data;
					this.Offset = offset;
					this.Length = length;
				}
			}
		}


		/// <summary>
		/// this Filter adds payloads to the tokens.
		/// </summary>
		private class PayloadFilter : TokenFilter
		{
			internal sbyte[] Data;
			internal int Length;
			internal int Offset;
			internal int StartOffset;
			internal PayloadAttribute PayloadAtt;
			internal CharTermAttribute TermAttribute;

			public PayloadFilter(TokenStream @in, sbyte[] data, int offset, int length) : base(@in)
			{
				this.Data = data;
				this.Length = length;
				this.Offset = offset;
				this.StartOffset = offset;
				PayloadAtt = addAttribute(typeof(PayloadAttribute));
				TermAttribute = addAttribute(typeof(CharTermAttribute));
			}

			public override bool IncrementToken()
			{
				bool hasNext = input.IncrementToken();
				if (!hasNext)
				{
				  return false;
				}

				// Some values of the same field are to have payloads and others not
				if (Offset + Length <= Data.Length && !TermAttribute.ToString().EndsWith("NO PAYLOAD"))
				{
				  BytesRef p = new BytesRef(Data, Offset, Length);
				  PayloadAtt.Payload = p;
				  Offset += Length;
				}
				else
				{
				  PayloadAtt.Payload = null;
				}

				return true;
			}

		  public override void Reset()
		  {
			base.reset();
			this.Offset = StartOffset;
		  }
		}

		public virtual void TestThreadSafety()
		{
			const int numThreads = 5;
			int numDocs = atLeast(50);
			ByteArrayPool pool = new ByteArrayPool(numThreads, 5);

			Directory dir = newDirectory();
			IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
			const string field = "test";

			Thread[] ingesters = new Thread[numThreads];
			for (int i = 0; i < numThreads; i++)
			{
				ingesters[i] = new ThreadAnonymousInnerClassHelper(this, numDocs, pool, writer, field);
				ingesters[i].Start();
			}

			for (int i = 0; i < numThreads; i++)
			{
			  ingesters[i].Join();
			}
			writer.close();
			IndexReader reader = DirectoryReader.open(dir);
			TermsEnum terms = MultiFields.getFields(reader).terms(field).iterator(null);
			Bits liveDocs = MultiFields.getLiveDocs(reader);
			DocsAndPositionsEnum tp = null;
			while (terms.next() != null)
			{
			  string termText = terms.term().utf8ToString();
			  tp = terms.docsAndPositions(liveDocs, tp);
			  while (tp.nextDoc() != DocIdSetIterator.NO_MORE_DOCS)
			  {
				int freq = tp.freq();
				for (int i = 0; i < freq; i++)
				{
				  tp.nextPosition();
				  BytesRef payload = tp.Payload;
				  Assert.AreEqual(termText, payload.utf8ToString());
				}
			  }
			}
			reader.close();
			dir.close();
			Assert.AreEqual(pool.Size(), numThreads);
		}

		private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
		{
			private readonly TestPayloads OuterInstance;

			private int NumDocs;
			private Lucene.Net.Index.TestPayloads.ByteArrayPool Pool;
			private IndexWriter Writer;
			private string Field;

			public ThreadAnonymousInnerClassHelper(TestPayloads outerInstance, int numDocs, Lucene.Net.Index.TestPayloads.ByteArrayPool pool, IndexWriter writer, string field)
			{
				this.OuterInstance = outerInstance;
				this.NumDocs = numDocs;
				this.Pool = pool;
				this.Writer = writer;
				this.Field = field;
			}

			public override void Run()
			{
				try
				{
					for (int j = 0; j < NumDocs; j++)
					{
						Document d = new Document();
						d.add(new TextField(Field, new PoolingPayloadTokenStream(OuterInstance, Pool)));
						Writer.addDocument(d);
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
					Console.Write(e.StackTrace);
					Assert.Fail(e.ToString());
				}
			}
		}

		private class PoolingPayloadTokenStream : TokenStream
		{
			private readonly TestPayloads OuterInstance;

			internal sbyte[] Payload;
			internal bool First;
			internal ByteArrayPool Pool;
			internal string Term;

			internal CharTermAttribute TermAtt;
			internal PayloadAttribute PayloadAtt;

			internal PoolingPayloadTokenStream(TestPayloads outerInstance, ByteArrayPool pool)
			{
				this.OuterInstance = outerInstance;
				this.Pool = pool;
				Payload = pool.Get();
				outerInstance.GenerateRandomData(Payload);
				Term = new string(Payload, 0, Payload.Length, Utf8);
				First = true;
				PayloadAtt = addAttribute(typeof(PayloadAttribute));
				TermAtt = addAttribute(typeof(CharTermAttribute));
			}

			public override bool IncrementToken()
			{
				if (!First)
				{
					return false;
				}
				First = false;
				ClearAttributes();
				TermAtt.append(Term);
				PayloadAtt.Payload = new BytesRef(Payload);
				return true;
			}

			public override void Close()
			{
				Pool.Release(Payload);
			}

		}

		private class ByteArrayPool
		{
			internal IList<sbyte[]> Pool;

			internal ByteArrayPool(int capacity, int size)
			{
				Pool = new List<>();
				for (int i = 0; i < capacity; i++)
				{
					Pool.Add(new sbyte[size]);
				}
			}

			internal virtual sbyte[] Get()
			{
				lock (this)
				{
					return Pool.Remove(0);
				}
			}

			internal virtual void Release(sbyte[] b)
			{
				lock (this)
				{
					Pool.Add(b);
				}
			}

			internal virtual int Size()
			{
				lock (this)
				{
					return Pool.Count;
				}
			}
		}

	  public virtual void TestAcrossFields()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir, new MockAnalyzer(random(), MockTokenizer.WHITESPACE, true));
		Document doc = new Document();
		doc.add(new TextField("hasMaybepayload", "here we go", Field.Store.YES));
		writer.addDocument(doc);
		writer.close();

		writer = new RandomIndexWriter(random(), dir, new MockAnalyzer(random(), MockTokenizer.WHITESPACE, true));
		doc = new Document();
		doc.add(new TextField("hasMaybepayload2", "here we go", Field.Store.YES));
		writer.addDocument(doc);
		writer.addDocument(doc);
		writer.forceMerge(1);
		writer.close();

		dir.close();
	  }

	  /// <summary>
	  /// some docs have payload att, some not </summary>
	  public virtual void TestMixupDocs()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, null);
		iwc.MergePolicy = newLogMergePolicy();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir, iwc);
		Document doc = new Document();
		Field field = new TextField("field", "", Field.Store.NO);
		TokenStream ts = new MockTokenizer(new StringReader("here we go"), MockTokenizer.WHITESPACE, true);
		Assert.IsFalse(ts.HasAttribute(typeof(PayloadAttribute)));
		field.TokenStream = ts;
		doc.add(field);
		writer.addDocument(doc);
		Token withPayload = new Token("withPayload", 0, 11);
		withPayload.Payload = new BytesRef("test");
		ts = new CannedTokenStream(withPayload);
		Assert.IsTrue(ts.HasAttribute(typeof(PayloadAttribute)));
		field.TokenStream = ts;
		writer.addDocument(doc);
		ts = new MockTokenizer(new StringReader("another"), MockTokenizer.WHITESPACE, true);
		Assert.IsFalse(ts.HasAttribute(typeof(PayloadAttribute)));
		field.TokenStream = ts;
		writer.addDocument(doc);
		DirectoryReader reader = writer.Reader;
		AtomicReader sr = SlowCompositeReaderWrapper.wrap(reader);
		DocsAndPositionsEnum de = sr.termPositionsEnum(new Term("field", "withPayload"));
		de.nextDoc();
		de.nextPosition();
		Assert.AreEqual(new BytesRef("test"), de.Payload);
		writer.close();
		reader.close();
		dir.close();
	  }

	  /// <summary>
	  /// some field instances have payload att, some not </summary>
	  public virtual void TestMixupMultiValued()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		Field field = new TextField("field", "", Field.Store.NO);
		TokenStream ts = new MockTokenizer(new StringReader("here we go"), MockTokenizer.WHITESPACE, true);
		Assert.IsFalse(ts.HasAttribute(typeof(PayloadAttribute)));
		field.TokenStream = ts;
		doc.add(field);
		Field field2 = new TextField("field", "", Field.Store.NO);
		Token withPayload = new Token("withPayload", 0, 11);
		withPayload.Payload = new BytesRef("test");
		ts = new CannedTokenStream(withPayload);
		Assert.IsTrue(ts.HasAttribute(typeof(PayloadAttribute)));
		field2.TokenStream = ts;
		doc.add(field2);
		Field field3 = new TextField("field", "", Field.Store.NO);
		ts = new MockTokenizer(new StringReader("nopayload"), MockTokenizer.WHITESPACE, true);
		Assert.IsFalse(ts.HasAttribute(typeof(PayloadAttribute)));
		field3.TokenStream = ts;
		doc.add(field3);
		writer.addDocument(doc);
		DirectoryReader reader = writer.Reader;
		SegmentReader sr = getOnlySegmentReader(reader);
		DocsAndPositionsEnum de = sr.termPositionsEnum(new Term("field", "withPayload"));
		de.nextDoc();
		de.nextPosition();
		Assert.AreEqual(new BytesRef("test"), de.Payload);
		writer.close();
		reader.close();
		dir.close();
	  }

	}

}