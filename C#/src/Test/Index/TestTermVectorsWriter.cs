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
// {{Aroush-2.3.1}} remove this file from SVN
/*
using System;

using NUnit.Framework;

using StandardAnalyzer = Lucene.Net.Analysis.Standard.StandardAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Index
{
	[TestFixture]
	public class TestTermVectorsWriter
	{
		private void  InitBlock()
		{
			positions = new int[testTerms.Length][];
		}
		
		private System.String[] testTerms = new System.String[]{"this", "is", "a", "test"};
		private System.String[] testFields = new System.String[]{"f1", "f2", "f3"};
		private int[][] positions;
		private RAMDirectory dir = new RAMDirectory();
		private System.String seg = "testSegment";
		private FieldInfos fieldInfos = new FieldInfos();
		
		
		[SetUp]
        public virtual void  SetUp()
		{
            InitBlock();
			
			for (int i = 0; i < testFields.Length; i++)
			{
				fieldInfos.Add(testFields[i], true, true);
			}
			
			
			for (int i = 0; i < testTerms.Length; i++)
			{
				positions[i] = new int[5];
				for (int j = 0; j < positions[i].Length; j++)
				{
					positions[i][j] = j * 10;
				}
			}
		}
		
		[TearDown]
        public virtual void  TearDown()
		{
		}
		
		[Test]
        public virtual void  Test()
		{
			Assert.IsTrue(dir != null);
			Assert.IsTrue(positions != null);
		}
		
		/*public void testWriteNoPositions() {
		try {
		TermVectorsWriter writer = new TermVectorsWriter(dir, seg, 50);
		writer.openDocument();
		Assert.IsTrue(writer.isDocumentOpen() == true);
		writer.openField(0);
		Assert.IsTrue(writer.isFieldOpen() == true);
		for (int i = 0; i < testTerms.length; i++) {
		writer.addTerm(testTerms[i], i);
		}
		writer.closeField();
		
		writer.closeDocument();
		writer.close();
		Assert.IsTrue(writer.isDocumentOpen() == false);
		//Check to see the files were created
		Assert.IsTrue(dir.fileExists(seg + TermVectorsWriter.TVD_EXTENSION));
		Assert.IsTrue(dir.fileExists(seg + TermVectorsWriter.TVX_EXTENSION));
		//Now read it back in
		TermVectorsReader reader = new TermVectorsReader(dir, seg);
		Assert.IsTrue(reader != null);
		checkTermVector(reader, 0, 0);
		} catch (IOException e) {
		e.printStackTrace();
		Assert.IsTrue(false);
		}
		}  *
		
		[Test]
        public virtual void  TestWriter()
		{
			TermVectorsWriter writer = new TermVectorsWriter(dir, seg, fieldInfos);
			writer.OpenDocument();
			Assert.IsTrue(writer.IsDocumentOpen() == true);
			WriteField(writer, testFields[0]);
			writer.CloseDocument();
			writer.Close();
			Assert.IsTrue(writer.IsDocumentOpen() == false);
			//Check to see the files were created
			Assert.IsTrue(dir.FileExists(seg + TermVectorsWriter.TvdExtension));
			Assert.IsTrue(dir.FileExists(seg + TermVectorsWriter.TvxExtension));
			//Now read it back in
			TermVectorsReader reader = new TermVectorsReader(dir, seg, fieldInfos);
			Assert.IsTrue(reader != null);
			CheckTermVector(reader, 0, testFields[0]);
		}
		
		private void  CheckTermVector(TermVectorsReader reader, int docNum, System.String field)
		{
			TermFreqVector vector = reader.Get(docNum, field);
			Assert.IsTrue(vector != null);
			System.String[] terms = vector.GetTerms();
			Assert.IsTrue(terms != null);
			Assert.IsTrue(terms.Length == testTerms.Length);
			for (int i = 0; i < terms.Length; i++)
			{
				System.String term = terms[i];
				Assert.IsTrue(term.Equals(testTerms[i]));
			}
		}
		
		/// <summary> Test one document, multiple fields</summary>
		/// <throws>  IOException </throws>
		[Test]
        public virtual void  TestMultipleFields()
		{
			TermVectorsWriter writer = new TermVectorsWriter(dir, seg, fieldInfos);
			WriteDocument(writer, testFields.Length);
			
			writer.Close();
			
			Assert.IsTrue(writer.IsDocumentOpen() == false);
			//Check to see the files were created
			Assert.IsTrue(dir.FileExists(seg + TermVectorsWriter.TvdExtension));
			Assert.IsTrue(dir.FileExists(seg + TermVectorsWriter.TvxExtension));
			//Now read it back in
			TermVectorsReader reader = new TermVectorsReader(dir, seg, fieldInfos);
			Assert.IsTrue(reader != null);
			
			for (int j = 0; j < testFields.Length; j++)
			{
				CheckTermVector(reader, 0, testFields[j]);
			}
		}
		
		private void  WriteDocument(TermVectorsWriter writer, int numFields)
		{
			writer.OpenDocument();
			Assert.IsTrue(writer.IsDocumentOpen() == true);
			
			for (int j = 0; j < numFields; j++)
			{
				WriteField(writer, testFields[j]);
			}
			writer.CloseDocument();
			Assert.IsTrue(writer.IsDocumentOpen() == false);
		}
		
		/// <summary> </summary>
		/// <param name="writer">The writer to write to
		/// </param>
		/// <param name="f">The field name
		/// </param>
		/// <throws>  IOException </throws>
		private void  WriteField(TermVectorsWriter writer, System.String f)
		{
			writer.OpenField(f);
			Assert.IsTrue(writer.IsFieldOpen() == true);
			for (int i = 0; i < testTerms.Length; i++)
			{
				writer.AddTerm(testTerms[i], i);
			}
			writer.CloseField();
		}
		
		[Test]
		public virtual void  TestMultipleDocuments()
		{
			TermVectorsWriter writer = new TermVectorsWriter(dir, seg, fieldInfos);
			Assert.IsTrue(writer != null);
			for (int i = 0; i < 10; i++)
			{
				WriteDocument(writer, testFields.Length);
			}
			writer.Close();
			//Do some arbitrary tests
			TermVectorsReader reader = new TermVectorsReader(dir, seg, fieldInfos);
			for (int i = 0; i < 10; i++)
			{
				Assert.IsTrue(reader != null);
				CheckTermVector(reader, 5, testFields[0]);
				CheckTermVector(reader, 2, testFields[2]);
			}
		}
		
		/// <summary> Test that no NullPointerException will be raised,
		/// when adding one document with a single, empty field
		/// and term vectors enabled.
		/// </summary>
		/// <throws>  IOException </throws>
		/// <summary> 
		/// </summary>
		[Test]
        public virtual void  TestBadSegment()
		{
			dir = new RAMDirectory();
			IndexWriter ir = new IndexWriter(dir, new StandardAnalyzer(), true);
			
			Lucene.Net.Documents.Document document = new Lucene.Net.Documents.Document();
			document.Add(new Field("tvtest", "", Field.Store.NO, Field.Index.TOKENIZED, Field.TermVector.YES));
			ir.AddDocument(document);
			ir.Close();
		}
	}
}
*/