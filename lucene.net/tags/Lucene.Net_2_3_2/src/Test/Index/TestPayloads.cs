/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using NUnit.Framework;

using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using Directory = Lucene.Net.Store.Directory;
using FSDirectory = Lucene.Net.Store.FSDirectory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
using Analyzer = Lucene.Net.Analysis.Analyzer;
using Token = Lucene.Net.Analysis.Token;
using TokenFilter = Lucene.Net.Analysis.TokenFilter;
using TokenStream = Lucene.Net.Analysis.TokenStream;
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using WhitespaceTokenizer = Lucene.Net.Analysis.WhitespaceTokenizer;

namespace Lucene.Net.Index
{
	
	
	[TestFixture]
	public class TestPayloads : LuceneTestCase
	{
		private class AnonymousClassThread : SupportClass.ThreadClass
		{
			public AnonymousClassThread(int numDocs, System.String field, Lucene.Net.Index.TestPayloads.ByteArrayPool pool, Lucene.Net.Index.IndexWriter writer, TestPayloads enclosingInstance)
			{
				InitBlock(numDocs, field, pool, writer, enclosingInstance);
			}
			private void  InitBlock(int numDocs, System.String field, Lucene.Net.Index.TestPayloads.ByteArrayPool pool, Lucene.Net.Index.IndexWriter writer, TestPayloads enclosingInstance)
			{
				this.numDocs = numDocs;
				this.field = field;
				this.pool = pool;
				this.writer = writer;
				this.enclosingInstance = enclosingInstance;
			}
			private int numDocs;
			private System.String field;
			private Lucene.Net.Index.TestPayloads.ByteArrayPool pool;
			private Lucene.Net.Index.IndexWriter writer;
			private TestPayloads enclosingInstance;
			public TestPayloads Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			override public void  Run()
			{
				try
				{
					for (int j = 0; j < numDocs; j++)
					{
						Document d = new Document();
						d.Add(new Field(field, new PoolingPayloadTokenStream(pool)));
						writer.AddDocument(d);
					}
				}
				catch (System.Exception e)
				{
					System.Console.Error.WriteLine(e.StackTrace);
					Assert.Fail(e.ToString());
				}
			}
		}
		
		// Simple tests to test the Payload class
		[Test]
		public virtual void  TestPayload()
		{
			byte[] testData = System.Text.Encoding.UTF8.GetBytes("This is a test!");
			Payload payload = new Payload(testData);
			Assert.AreEqual(testData.Length, payload.Length(), "Wrong payload length.");
			
			// test copyTo()
			byte[] target = new byte[testData.Length - 1];
			try
			{
				payload.CopyTo(target, 0);
				Assert.Fail("Expected exception not thrown");
			}
			catch (System.Exception)
			{
				// expected exception
			}
			
			target = new byte[testData.Length + 3];
			payload.CopyTo(target, 3);
			
			for (int i = 0; i < testData.Length; i++)
			{
				Assert.AreEqual(testData[i], target[i + 3]);
			}
			
			
			// test toByteArray()
			target = payload.ToByteArray();
			AssertByteArrayEquals(testData, target);
			
			// test byteAt()
			for (int i = 0; i < testData.Length; i++)
			{
				Assert.AreEqual(payload.ByteAt(i), testData[i]);
			}
			
			try
			{
				payload.ByteAt(testData.Length + 1);
				Assert.Fail("Expected exception not thrown");
			}
			catch (System.Exception)
			{
				// expected exception
			}
			
			Payload clone = (Payload) payload.Clone();
			Assert.AreEqual(payload.Length(), clone.Length());
			for (int i = 0; i < payload.Length(); i++)
			{
				Assert.AreEqual(payload.ByteAt(i), clone.ByteAt(i));
			}
		}
		
		// Tests whether the DocumentWriter and SegmentMerger correctly enable the
		// payload bit in the FieldInfo
		[Test]
		public virtual void  TestPayloadFieldBit()
		{
			Directory ram = new RAMDirectory();
			PayloadAnalyzer analyzer = new PayloadAnalyzer();
			IndexWriter writer = new IndexWriter(ram, analyzer, true);
			Document d = new Document();
			// this field won't have any payloads
			d.Add(new Field("f1", "This field has no payloads", Field.Store.NO, Field.Index.TOKENIZED));
			// this field will have payloads in all docs, however not for all term positions,
			// so this field is used to check if the DocumentWriter correctly enables the payloads bit
			// even if only some term positions have payloads
			d.Add(new Field("f2", "This field has payloads in all docs", Field.Store.NO, Field.Index.TOKENIZED));
			d.Add(new Field("f2", "This field has payloads in all docs", Field.Store.NO, Field.Index.TOKENIZED));
			// this field is used to verify if the SegmentMerger enables payloads for a field if it has payloads 
			// enabled in only some documents
			d.Add(new Field("f3", "This field has payloads in some docs", Field.Store.NO, Field.Index.TOKENIZED));
			// only add payload data for field f2
			analyzer.SetPayloadData("f2", 1, System.Text.Encoding.UTF8.GetBytes("somedata"), 0, 1);
			writer.AddDocument(d);
			// flush
			writer.Close();
			
			// only one segment in the index, so we can cast to SegmentReader
			SegmentReader reader = (SegmentReader) IndexReader.Open(ram);
			FieldInfos fi = reader.FieldInfos();
			Assert.IsFalse(fi.FieldInfo("f1").StorePayloads_ForNUnitTest, "Payload field bit should not be set.");
			Assert.IsTrue(fi.FieldInfo("f2").StorePayloads_ForNUnitTest, "Payload field bit should be set.");
			Assert.IsFalse(fi.FieldInfo("f3").StorePayloads_ForNUnitTest, "Payload field bit should not be set.");
			reader.Close();
			
			// now we add another document which has payloads for field f3 and verify if the SegmentMerger
			// enabled payloads for that field
			writer = new IndexWriter(ram, analyzer, true);
			d = new Document();
			d.Add(new Field("f1", "This field has no payloads", Field.Store.NO, Field.Index.TOKENIZED));
			d.Add(new Field("f2", "This field has payloads in all docs", Field.Store.NO, Field.Index.TOKENIZED));
			d.Add(new Field("f2", "This field has payloads in all docs", Field.Store.NO, Field.Index.TOKENIZED));
			d.Add(new Field("f3", "This field has payloads in some docs", Field.Store.NO, Field.Index.TOKENIZED));
			// add payload data for field f2 and f3
			analyzer.SetPayloadData("f2", System.Text.Encoding.UTF8.GetBytes("somedata"), 0, 1);
			analyzer.SetPayloadData("f3", System.Text.Encoding.UTF8.GetBytes("somedata"), 0, 3);
			writer.AddDocument(d);
			// force merge
			writer.Optimize();
			// flush
			writer.Close();
			
			// only one segment in the index, so we can cast to SegmentReader
			reader = (SegmentReader) IndexReader.Open(ram);
			fi = reader.FieldInfos();
			Assert.IsFalse(fi.FieldInfo("f1").StorePayloads_ForNUnitTest, "Payload field bit should not be set.");
			Assert.IsTrue(fi.FieldInfo("f2").StorePayloads_ForNUnitTest, "Payload field bit should be set.");
			Assert.IsTrue(fi.FieldInfo("f3").StorePayloads_ForNUnitTest, "Payload field bit should be set.");
			reader.Close();
		}
		
		// Tests if payloads are correctly stored and loaded using both RamDirectory and FSDirectory
		[Test]
		public virtual void  TestPayloadsEncoding()
		{
			// first perform the test using a RAMDirectory
			Directory dir = new RAMDirectory();
			PerformTest(dir);
			
			// now use a FSDirectory and repeat same test
			System.String dirName = "test_payloads";
			dir = FSDirectory.GetDirectory(dirName);
			PerformTest(dir);
			RmDir(dirName);
		}
		
		// builds an index with payloads in the given Directory and performs
		// different tests to verify the payload encoding
		private void  PerformTest(Directory dir)
		{
			PayloadAnalyzer analyzer = new PayloadAnalyzer();
			IndexWriter writer = new IndexWriter(dir, analyzer, true);
			
			// should be in sync with value in TermInfosWriter
			int skipInterval = 16;
			
			int numTerms = 5;
			System.String fieldName = "f1";
			
			int numDocs = skipInterval + 1;
			// create content for the test documents with just a few terms
			Term[] terms = GenerateTerms(fieldName, numTerms);
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			for (int i = 0; i < terms.Length; i++)
			{
				sb.Append(terms[i].text_ForNUnitTest);
				sb.Append(" ");
			}
			System.String content = sb.ToString();
			
			
			int payloadDataLength = numTerms * numDocs * 2 + numTerms * numDocs * (numDocs - 1) / 2;
			byte[] payloadData = GenerateRandomData(payloadDataLength);
			
			Document d = new Document();
			d.Add(new Field(fieldName, content, Field.Store.NO, Field.Index.TOKENIZED));
			// add the same document multiple times to have the same payload lengths for all
			// occurrences within two consecutive skip intervals
			int offset = 0;
			for (int i = 0; i < 2 * numDocs; i++)
			{
				analyzer.SetPayloadData(fieldName, payloadData, offset, 1);
				offset += numTerms;
				writer.AddDocument(d);
			}
			
			// make sure we create more than one segment to test merging
			writer.Flush();
			
			// now we make sure to have different payload lengths next at the next skip point        
			for (int i = 0; i < numDocs; i++)
			{
				analyzer.SetPayloadData(fieldName, payloadData, offset, i);
				offset += i * numTerms;
				writer.AddDocument(d);
			}
			
			writer.Optimize();
			// flush
			writer.Close();
			
			
			/*
			* Verify the index
			* first we test if all payloads are stored correctly
			*/
			IndexReader reader = IndexReader.Open(dir);
			
			byte[] verifyPayloadData = new byte[payloadDataLength];
			offset = 0;
			TermPositions[] tps = new TermPositions[numTerms];
			for (int i = 0; i < numTerms; i++)
			{
				tps[i] = reader.TermPositions(terms[i]);
			}
			
			while (tps[0].Next())
			{
				for (int i = 1; i < numTerms; i++)
				{
					tps[i].Next();
				}
				int freq = tps[0].Freq();
				
				for (int i = 0; i < freq; i++)
				{
					for (int j = 0; j < numTerms; j++)
					{
						tps[j].NextPosition();
						tps[j].GetPayload(verifyPayloadData, offset);
						offset += tps[j].GetPayloadLength();
					}
				}
			}
			
			for (int i = 0; i < numTerms; i++)
			{
				tps[i].Close();
			}
			
			AssertByteArrayEquals(payloadData, verifyPayloadData);
			
			/*
			*  test lazy skipping
			*/
			TermPositions tp = reader.TermPositions(terms[0]);
			tp.Next();
			tp.NextPosition();
			// now we don't read this payload
			tp.NextPosition();
			Assert.AreEqual(1, tp.GetPayloadLength(), "Wrong payload length.");
			byte[] payload = tp.GetPayload(null, 0);
			Assert.AreEqual(payload[0], payloadData[numTerms]);
			tp.NextPosition();
			
			// we don't read this payload and skip to a different document
			tp.SkipTo(5);
			tp.NextPosition();
			Assert.AreEqual(1, tp.GetPayloadLength(), "Wrong payload length.");
			payload = tp.GetPayload(null, 0);
			Assert.AreEqual(payload[0], payloadData[5 * numTerms]);
			
			
			/*
			* Test different lengths at skip points
			*/
			tp.Seek(terms[1]);
			tp.Next();
			tp.NextPosition();
			Assert.AreEqual(1, tp.GetPayloadLength(), "Wrong payload length.");
			tp.SkipTo(skipInterval - 1);
			tp.NextPosition();
			Assert.AreEqual(1, tp.GetPayloadLength(), "Wrong payload length.");
			tp.SkipTo(2 * skipInterval - 1);
			tp.NextPosition();
			Assert.AreEqual(1, tp.GetPayloadLength(), "Wrong payload length.");
			tp.SkipTo(3 * skipInterval - 1);
			tp.NextPosition();
			Assert.AreEqual(3 * skipInterval - 2 * numDocs - 1, tp.GetPayloadLength(), "Wrong payload length.");
			
			/*
			* Test multiple call of getPayload()
			*/
			tp.GetPayload(null, 0);
			try
			{
				// it is forbidden to call getPayload() more than once
				// without calling nextPosition()
				tp.GetPayload(null, 0);
				Assert.Fail("Expected exception not thrown");
			}
			catch (System.Exception)
			{
				// expected exception
			}
			
			reader.Close();
			
			// test long payload
			analyzer = new PayloadAnalyzer();
			writer = new IndexWriter(dir, analyzer, true);
			System.String singleTerm = "lucene";
			
			d = new Document();
			d.Add(new Field(fieldName, singleTerm, Field.Store.NO, Field.Index.TOKENIZED));
			// add a payload whose length is greater than the buffer size of BufferedIndexOutput
			payloadData = GenerateRandomData(2000);
			analyzer.SetPayloadData(fieldName, payloadData, 100, 1500);
			writer.AddDocument(d);
			
			
			writer.Optimize();
			// flush
			writer.Close();
			
			reader = IndexReader.Open(dir);
			tp = reader.TermPositions(new Term(fieldName, singleTerm));
			tp.Next();
			tp.NextPosition();
			
			verifyPayloadData = new byte[tp.GetPayloadLength()];
			tp.GetPayload(verifyPayloadData, 0);
			byte[] portion = new byte[1500];
			Array.Copy(payloadData, 100, portion, 0, 1500);
			
			AssertByteArrayEquals(portion, verifyPayloadData);
			reader.Close();
		}
		
		private static System.Random rnd = new System.Random();
		
		private static void  GenerateRandomData(byte[] data)
		{
			rnd.NextBytes(data);
		}
		
		private static byte[] GenerateRandomData(int n)
		{
			byte[] data = new byte[n];
			GenerateRandomData(data);
			return data;
		}
		
		private Term[] GenerateTerms(System.String fieldName, int n)
		{
			int maxDigits = (int) (System.Math.Log(n) / System.Math.Log(10));
			Term[] terms = new Term[n];
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			for (int i = 0; i < n; i++)
			{
				sb.Length = 0;
				sb.Append("t");
				int zeros = maxDigits - (int) (System.Math.Log(i) / System.Math.Log(10));
				for (int j = 0; j < zeros; j++)
				{
					sb.Append("0");
				}
				sb.Append(i);
				terms[i] = new Term(fieldName, sb.ToString());
			}
			return terms;
		}
		
		
		private void  RmDir(System.String dir)
		{
			System.IO.FileInfo fileDir = new System.IO.FileInfo(dir);
			bool tmpBool;
			if (System.IO.File.Exists(fileDir.FullName))
				tmpBool = true;
			else
				tmpBool = System.IO.Directory.Exists(fileDir.FullName);
			if (tmpBool)
			{
				System.IO.FileInfo[] files = SupportClass.FileSupport.GetFiles(fileDir);
				if (files != null)
				{
					for (int i = 0; i < files.Length; i++)
					{
						bool tmpBool2;
						if (System.IO.File.Exists(files[i].FullName))
						{
							System.IO.File.Delete(files[i].FullName);
							tmpBool2 = true;
						}
						else if (System.IO.Directory.Exists(files[i].FullName))
						{
							System.IO.Directory.Delete(files[i].FullName);
							tmpBool2 = true;
						}
						else
							tmpBool2 = false;
						bool generatedAux = tmpBool2;
					}
				}
				bool tmpBool3;
				if (System.IO.File.Exists(fileDir.FullName))
				{
					System.IO.File.Delete(fileDir.FullName);
					tmpBool3 = true;
				}
				else if (System.IO.Directory.Exists(fileDir.FullName))
				{
					System.IO.Directory.Delete(fileDir.FullName);
					tmpBool3 = true;
				}
				else
					tmpBool3 = false;
				bool generatedAux2 = tmpBool3;
			}
		}
		
		
		
		internal virtual void  AssertByteArrayEquals(byte[] b1, byte[] b2)
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
		
		
		/// <summary> This Analyzer uses an WhitespaceTokenizer and PayloadFilter.</summary>
		private class PayloadAnalyzer : Analyzer
		{
			internal System.Collections.IDictionary fieldToData = new System.Collections.Hashtable();
			
			internal virtual void  SetPayloadData(System.String field, byte[] data, int offset, int length)
			{
				fieldToData[field] = new PayloadData(0, data, offset, length);
			}
			
			internal virtual void  SetPayloadData(System.String field, int numFieldInstancesToSkip, byte[] data, int offset, int length)
			{
				fieldToData[field] = new PayloadData(numFieldInstancesToSkip, data, offset, length);
			}
			
			public override TokenStream TokenStream(System.String fieldName, System.IO.TextReader reader)
			{
				PayloadData payload = (PayloadData) fieldToData[fieldName];
				TokenStream ts = new WhitespaceTokenizer(reader);
				if (payload != null)
				{
					if (payload.numFieldInstancesToSkip == 0)
					{
						ts = new PayloadFilter(ts, payload.data, payload.offset, payload.length);
					}
					else
					{
						payload.numFieldInstancesToSkip--;
					}
				}
				return ts;
			}
			
			private class PayloadData
			{
				internal byte[] data;
				internal int offset;
				internal int length;
				internal int numFieldInstancesToSkip;
				
				internal PayloadData(int skip, byte[] data, int offset, int length)
				{
					numFieldInstancesToSkip = skip;
					this.data = data;
					this.offset = offset;
					this.length = length;
				}
			}
		}
		
		
		/// <summary> This Filter adds payloads to the tokens.</summary>
		private class PayloadFilter : TokenFilter
		{
			private byte[] data;
			private int length;
			private int offset;
			internal Payload payload = new Payload();
			
			public PayloadFilter(TokenStream in_Renamed, byte[] data, int offset, int length):base(in_Renamed)
			{
				this.data = data;
				this.length = length;
				this.offset = offset;
			}
			
			public override Token Next(Token token)
			{
				token = input.Next(token);
				if (token != null)
				{
					if (offset + length <= data.Length)
					{
						Payload p = null;
						if (p == null)
						{
							p = new Payload();
							token.SetPayload(p);
						}
						p.SetData(data, offset, length);
						offset += length;
					}
					else
					{
						token.SetPayload(null);
					}
				}
				
				return token;
			}
		}
		
		[Test]
		public virtual void  TestThreadSafety()
		{
			int numThreads = 5;
			int numDocs = 50;
			ByteArrayPool pool = new ByteArrayPool(numThreads, 5);
			
			Directory dir = new RAMDirectory();
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer());
			System.String field = "test";
			
			SupportClass.ThreadClass[] ingesters = new SupportClass.ThreadClass[numThreads];
			for (int i = 0; i < numThreads; i++)
			{
				ingesters[i] = new AnonymousClassThread(numDocs, field, pool, writer, this);
				ingesters[i].Start();
			}
			
			for (int i = 0; i < numThreads; i++)
			{
				try
				{
					ingesters[i].Join();
				}
				catch (System.Threading.ThreadInterruptedException)
				{
				}
			}
			writer.Close();
			IndexReader reader = IndexReader.Open(dir);
			TermEnum terms = reader.Terms();
			while (terms.Next())
			{
				TermPositions tp = reader.TermPositions(terms.Term());
				while (tp.Next())
				{
					int freq = tp.Freq();
					for (int i = 0; i < freq; i++)
					{
						tp.NextPosition();
						System.String s = System.Text.Encoding.UTF8.GetString(tp.GetPayload(new byte[5], 0));
						Assert.AreEqual(s, terms.Term().text_ForNUnitTest);
					}
				}
				tp.Close();
			}
			terms.Close();
			reader.Close();
			
			Assert.AreEqual(pool.Size(), numThreads);
		}
		
		private class PoolingPayloadTokenStream : TokenStream
		{
			private byte[] payload;
			private bool first;
			private ByteArrayPool pool;
			
			internal PoolingPayloadTokenStream(ByteArrayPool pool)
			{
				this.pool = pool;
				payload = pool.Get();
				Lucene.Net.Index.TestPayloads.GenerateRandomData(payload);
				first = true;
			}
			
			public override Token Next()
			{
				if (!first)
					return null;
				Token t = new Token(System.Text.Encoding.UTF8.GetString(payload), 0, 0);
				t.SetPayload(new Payload(payload));
				return t;
			}
			
			public override void  Close()
			{
				pool.Release(payload);
			}
		}
		
		internal class ByteArrayPool
		{
			private System.Collections.IList pool;
			
			internal ByteArrayPool(int capacity, int size)
			{
				pool = new System.Collections.ArrayList();
				for (int i = 0; i < capacity; i++)
				{
					pool.Add(new byte[size]);
				}
			}
			
			internal virtual byte[] Get()
			{
				lock (this)
				{
					System.Object tempObject;
					tempObject = pool[0];
					pool.RemoveAt(0);
					return (byte[]) tempObject;
				}
			}
			
			internal virtual void  Release(byte[] b)
			{
				lock (this)
				{
					pool.Add(b);
				}
			}
			
			internal virtual int Size()
			{
				lock (this)
				{
					return pool.Count;
				}
			}
		}
	}
}