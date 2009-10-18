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

using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using Hits = Lucene.Net.Search.Hits;
using IndexSearcher = Lucene.Net.Search.IndexSearcher;
using PhraseQuery = Lucene.Net.Search.PhraseQuery;
using Searcher = Lucene.Net.Search.Searcher;
using Directory = Lucene.Net.Store.Directory;
using IndexInput = Lucene.Net.Store.IndexInput;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Index
{
	
	/// <summary> Tests lazy skipping on the proximity file.
	/// 
	/// </summary>
	[TestFixture]
    public class TestLazyProxSkipping
	{
		private Searcher searcher;
		private int seeksCounter = 0;
		
		private System.String field = "tokens";
		private System.String term1 = "xx";
		private System.String term2 = "yy";
		private System.String term3 = "zz";
		
		private void  CreateIndex(int numHits)
		{
			int numDocs = 500;
			
			Directory directory = new RAMDirectory();
			IndexWriter writer = new IndexWriter(directory, new WhitespaceAnalyzer(), true);
			
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
				System.String content;
				if (i % (numDocs / numHits) == 0)
				{
					// add a document that matches the query "term1 term2"
					content = this.term1 + " " + this.term2;
				}
				else if (i % 15 == 0)
				{
					// add a document that only contains term1
					content = this.term1 + " " + this.term1;
				}
				else
				{
					// add a document that contains term2 but not term 1
					content = this.term3 + " " + this.term2;
				}
				
				doc.Add(new Field(this.field, content, Field.Store.YES, Field.Index.TOKENIZED));
				writer.AddDocument(doc);
			}
			
			// make sure the index has only a single segment
			writer.Optimize();
			writer.Close();
			
			// the index is a single segment, thus IndexReader.open() returns an instance of SegmentReader
			SegmentReader reader = (SegmentReader) IndexReader.Open(directory);
			
			// we decorate the proxStream with a wrapper class that allows to count the number of calls of seek()
			reader.ProxStream = new SeeksCountingStream(this, reader.ProxStream);
			
			this.searcher = new IndexSearcher(reader);
		}
		
		private Hits Search()
		{
			// create PhraseQuery "term1 term2" and search
			PhraseQuery pq = new PhraseQuery();
			pq.Add(new Term(this.field, this.term1));
			pq.Add(new Term(this.field, this.term2));
			return this.searcher.Search(pq);
		}
		
		private void  PerformTest(int numHits)
		{
			CreateIndex(numHits);
			this.seeksCounter = 0;
			Hits hits = Search();
			// verify that the right number of docs was found
			Assert.AreEqual(numHits, hits.Length());
			
			// check if the number of calls of seek() does not exceed the number of hits
			Assert.AreEqual(numHits, this.seeksCounter);
		}
		
        [Test]
		public virtual void  TestLazySkipping()
		{
			// test whether only the minimum amount of seeks() are performed
			PerformTest(5);
			PerformTest(10);
		}
		
		
		// Simply extends IndexInput in a way that we are able to count the number
		// of invocations of seek()
		internal class SeeksCountingStream : IndexInput, System.ICloneable
		{
			private void  InitBlock(TestLazyProxSkipping enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestLazyProxSkipping enclosingInstance;
			public TestLazyProxSkipping Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			private IndexInput input;
			
			
			internal SeeksCountingStream(TestLazyProxSkipping enclosingInstance, IndexInput input)
			{
				InitBlock(enclosingInstance);
				this.input = input;
			}
			
			public override byte ReadByte()
			{
				return this.input.ReadByte();
			}
			
			public override void  ReadBytes(byte[] b, int offset, int len)
			{
				this.input.ReadBytes(b, offset, len);
			}
			
			public override void  Close()
			{
				this.input.Close();
			}
			
			public override long GetFilePointer()
			{
				return this.input.GetFilePointer();
			}
			
			public override void  Seek(long pos)
			{
				Enclosing_Instance.seeksCounter++;
				this.input.Seek(pos);
			}
			
			public override long Length()
			{
				return this.input.Length();
			}
			
			public override System.Object Clone()
			{
				return new SeeksCountingStream(enclosingInstance, (IndexInput) this.input.Clone());
			}
		}
	}
}