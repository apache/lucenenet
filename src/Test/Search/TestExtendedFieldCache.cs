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
using IndexReader = Lucene.Net.Index.IndexReader;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using English = Lucene.Net.Util.English;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Search
{
	
	[TestFixture]
	public class TestExtendedFieldCache : LuceneTestCase
	{
		protected internal IndexReader reader;
		private const int NUM_DOCS = 1000;
		
		[SetUp]
		public override void  SetUp()
		{
			base.SetUp();
			RAMDirectory directory = new RAMDirectory();
			IndexWriter writer = new IndexWriter(directory, new WhitespaceAnalyzer(), true);
			long theLong = System.Int64.MaxValue;
			double theDouble = System.Double.MaxValue;
			for (int i = 0; i < NUM_DOCS; i++)
			{
				Document doc = new Document();
				doc.Add(new Field("theLong", System.Convert.ToString(theLong--), Field.Store.NO, Field.Index.UN_TOKENIZED));
				//doc.Add(new Field("theDouble", System.Convert.ToString(theDouble--), Field.Store.NO, Field.Index.UN_TOKENIZED));
				doc.Add(new Field("theDouble", (theDouble--).ToString("R"), Field.Store.NO, Field.Index.UN_TOKENIZED));
				doc.Add(new Field("text", English.IntToEnglish(i), Field.Store.NO, Field.Index.TOKENIZED));
				writer.AddDocument(doc);
			}
			writer.Close();
			reader = IndexReader.Open(directory);
		}
		
		
		[Test]
		public virtual void  Test()
		{
			ExtendedFieldCache cache = new ExtendedFieldCacheImpl();
			double[] doubles = cache.GetDoubles(reader, "theDouble");
			Assert.IsTrue(doubles.Length == NUM_DOCS, "doubles Size: " + doubles.Length + " is not: " + NUM_DOCS);
			for (int i = 0; i < doubles.Length; i++)
			{
				Assert.IsTrue(doubles[i] == (System.Double.MaxValue - i), doubles[i] + " does not equal: " + (System.Double.MaxValue - i));
			}
			long[] longs = cache.GetLongs(reader, "theLong");
			Assert.IsTrue(longs.Length == NUM_DOCS, "longs Size: " + longs.Length + " is not: " + NUM_DOCS);
			for (int i = 0; i < longs.Length; i++)
			{
				Assert.IsTrue(longs[i] == (System.Int64.MaxValue - i), longs[i] + " does not equal: " + (System.Int64.MaxValue - i));
			}
		}
	}
}