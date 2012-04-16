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

using StandardAnalyzer = Lucene.Net.Analysis.Standard.StandardAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using Directory = Lucene.Net.Store.Directory;
using MockRAMDirectory = Lucene.Net.Store.MockRAMDirectory;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Index
{
	
	
    [TestFixture]
	public class TestIndexWriterMerging:LuceneTestCase
	{
		
		/// <summary> Tests that index merging (specifically AddIndexesNoOptimize()) doesn't
		/// change the index order of documents.
		/// </summary>
		[Test]
		public virtual void  TestLucene()
		{
			
			int num = 100;
			
			Directory indexA = new MockRAMDirectory();
			Directory indexB = new MockRAMDirectory();
			
			FillIndex(indexA, 0, num);
            Assert.IsFalse(VerifyIndex(indexA, 0), "Index a is invalid");
			
			FillIndex(indexB, num, num);
            Assert.IsFalse(VerifyIndex(indexB, num), "Index b is invalid");
			
			Directory merged = new MockRAMDirectory();
			
			IndexWriter writer = new IndexWriter(merged, new StandardAnalyzer(Util.Version.LUCENE_CURRENT), true, IndexWriter.MaxFieldLength.LIMITED);
			writer.MergeFactor = 2;
			
			writer.AddIndexesNoOptimize(new []{indexA, indexB});
            writer.Optimize();
			writer.Close();
			
			var fail = VerifyIndex(merged, 0);
			merged.Close();
			
			Assert.IsFalse(fail, "The merged index is invalid");
		}
		
		private bool VerifyIndex(Directory directory, int startAt)
		{
			bool fail = false;
			IndexReader reader = IndexReader.Open(directory, true);
			
			int max = reader.MaxDoc;
			for (int i = 0; i < max; i++)
			{
				Document temp = reader.Document(i);
				//System.out.println("doc "+i+"="+temp.getField("count").stringValue());
				//compare the index doc number to the value that it should be
				if (!temp.GetField("count").StringValue.Equals((i + startAt) + ""))
				{
					fail = true;
					System.Console.Out.WriteLine("Document " + (i + startAt) + " is returning document " + temp.GetField("count").StringValue);
				}
			}
			reader.Close();
			return fail;
		}
		
		private void  FillIndex(Directory dir, int start, int numDocs)
		{
			
			IndexWriter writer = new IndexWriter(dir, new StandardAnalyzer(Util.Version.LUCENE_CURRENT), true, IndexWriter.MaxFieldLength.LIMITED);
			writer.MergeFactor = 2;
			writer.SetMaxBufferedDocs(2);
			
			for (int i = start; i < (start + numDocs); i++)
			{
				Document temp = new Document();
				temp.Add(new Field("count", ("" + i), Field.Store.YES, Field.Index.NOT_ANALYZED));
				
				writer.AddDocument(temp);
			}
			writer.Close();
		}
	}
}