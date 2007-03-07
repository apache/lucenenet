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
using Directory = Lucene.Net.Store.Directory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Index
{
	
	[TestFixture]
	public class TestMultiReader
	{
		private Directory dir = new RAMDirectory();
		private Lucene.Net.Documents.Document doc1 = new Lucene.Net.Documents.Document();
		private Lucene.Net.Documents.Document doc2 = new Lucene.Net.Documents.Document();
		private SegmentReader reader1;
		private SegmentReader reader2;
		private SegmentReader[] readers = new SegmentReader[2];
		private SegmentInfos sis = new SegmentInfos();
		
		// This is needed if for the test to pass and mimic what happens wiht JUnit
        // For some reason, JUnit is creating a new member variable for each sub-test
        // but NUnit is not -- who is wrong/right, I don't know.
        private void SetUpInternal()        // {{Aroush-1.9}} See note above
        {
		    dir = new RAMDirectory();
		    doc1 = new Lucene.Net.Documents.Document();
		    doc2 = new Lucene.Net.Documents.Document();
		    readers = new SegmentReader[2];
		    sis = new SegmentInfos();
        }

		[SetUp]
        public virtual void  SetUp()
		{
            SetUpInternal();    // We need this for NUnit; see note above

			DocHelper.SetupDoc(doc1);
			DocHelper.SetupDoc(doc2);
			DocHelper.WriteDoc(dir, "seg-1", doc1);
			DocHelper.WriteDoc(dir, "seg-2", doc2);
			sis.Write(dir);
			reader1 = SegmentReader.Get(new SegmentInfo("seg-1", 1, dir));
			reader2 = SegmentReader.Get(new SegmentInfo("seg-2", 1, dir));
			readers[0] = reader1;
			readers[1] = reader2;
		}
		
		[Test]
        public virtual void  Test()
		{
			Assert.IsTrue(dir != null);
			Assert.IsTrue(reader1 != null);
			Assert.IsTrue(reader2 != null);
			Assert.IsTrue(sis != null);
		}
		
		[Test]
        public virtual void  TestDocument()
		{
			sis.Read(dir);
			MultiReader reader = new MultiReader(dir, sis, false, readers);
			Assert.IsTrue(reader != null);
			Lucene.Net.Documents.Document newDoc1 = reader.Document(0);
			Assert.IsTrue(newDoc1 != null);
			Assert.IsTrue(DocHelper.NumFields(newDoc1) == DocHelper.NumFields(doc1) - DocHelper.unstored.Count);
			Lucene.Net.Documents.Document newDoc2 = reader.Document(1);
			Assert.IsTrue(newDoc2 != null);
			Assert.IsTrue(DocHelper.NumFields(newDoc2) == DocHelper.NumFields(doc2) - DocHelper.unstored.Count);
			TermFreqVector vector = reader.GetTermFreqVector(0, DocHelper.TEXT_FIELD_2_KEY);
			Assert.IsTrue(vector != null);
			TestSegmentReader.CheckNorms(reader);
		}
		
		[Test]
        public virtual void  TestUndeleteAll()
		{
			sis.Read(dir);
			MultiReader reader = new MultiReader(dir, sis, false, readers);
			Assert.IsTrue(reader != null);
			Assert.AreEqual(2, reader.NumDocs());
			reader.DeleteDocument(0);
			Assert.AreEqual(1, reader.NumDocs());
			reader.UndeleteAll();
			Assert.AreEqual(2, reader.NumDocs());
		}
		
		[Test]
		public virtual void  TestTermVectors()
		{
			MultiReader reader = new MultiReader(dir, sis, false, readers);
			Assert.IsTrue(reader != null);
		}
	}
}