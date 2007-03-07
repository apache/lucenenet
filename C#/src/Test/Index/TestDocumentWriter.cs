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
using Analyzer = Lucene.Net.Analysis.Analyzer;
using TokenStream = Lucene.Net.Analysis.TokenStream;
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using WhitespaceTokenizer = Lucene.Net.Analysis.WhitespaceTokenizer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using Similarity = Lucene.Net.Search.Similarity;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Index
{
	
	[TestFixture]
	public class TestDocumentWriter
	{
		private class AnonymousClassAnalyzer : Analyzer
		{
			public AnonymousClassAnalyzer(TestDocumentWriter enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(TestDocumentWriter enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestDocumentWriter enclosingInstance;
			public TestDocumentWriter Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}

            public override TokenStream TokenStream(System.String fieldName, System.IO.TextReader reader)
			{
				return new WhitespaceTokenizer(reader);
			}
			
			public override int GetPositionIncrementGap(System.String fieldName)
			{
				return 500;
			}
		}
		private RAMDirectory dir;


		[SetUp]
        public virtual void  SetUp()
		{
			dir = new RAMDirectory();
		}
		
		[TearDown]
        public virtual void  TearDown()
		{
			
		}
		
		[Test]
        public virtual void  Test()
		{
			Assert.IsTrue(dir != null);
		}
		
		[Test]
        public virtual void  TestAddDocument()
		{
			Lucene.Net.Documents.Document testDoc = new Lucene.Net.Documents.Document();
			DocHelper.SetupDoc(testDoc);
			Analyzer analyzer = new WhitespaceAnalyzer();
			Lucene.Net.Search.Similarity similarity = Lucene.Net.Search.Similarity.GetDefault();
			DocumentWriter writer = new DocumentWriter(dir, analyzer, similarity, 50);
			System.String segName = "test";
			writer.AddDocument(segName, testDoc);
			//After adding the document, we should be able to read it back in
			SegmentReader reader = SegmentReader.Get(new SegmentInfo(segName, 1, dir));
			Assert.IsTrue(reader != null);
			Lucene.Net.Documents.Document doc = reader.Document(0);
			Assert.IsTrue(doc != null);
			
			//System.out.println("Document: " + doc);
			Field[] fields = doc.GetFields("textField2");
			Assert.IsTrue(fields != null && fields.Length == 1);
			Assert.IsTrue(fields[0].StringValue().Equals(DocHelper.FIELD_2_TEXT));
			Assert.IsTrue(fields[0].IsTermVectorStored());
			
			fields = doc.GetFields("textField1");
			Assert.IsTrue(fields != null && fields.Length == 1);
			Assert.IsTrue(fields[0].StringValue().Equals(DocHelper.FIELD_1_TEXT));
			Assert.IsFalse(fields[0].IsTermVectorStored());
			
			fields = doc.GetFields("keyField");
			Assert.IsTrue(fields != null && fields.Length == 1);
			Assert.IsTrue(fields[0].StringValue().Equals(DocHelper.KEYWORD_TEXT));
			
			fields = doc.GetFields(DocHelper.NO_NORMS_KEY);
			Assert.IsTrue(fields != null && fields.Length == 1);
			Assert.IsTrue(fields[0].StringValue().Equals(DocHelper.NO_NORMS_TEXT));
			
			fields = doc.GetFields(DocHelper.TEXT_FIELD_3_KEY);
			Assert.IsTrue(fields != null && fields.Length == 1);
			Assert.IsTrue(fields[0].StringValue().Equals(DocHelper.FIELD_3_TEXT));
			
			// test that the norm file is not present if omitNorms is true
			for (int i = 0; i < reader.FieldInfos.Size(); i++)
			{
				FieldInfo fi = reader.FieldInfos.FieldInfo(i);
				if (fi.IsIndexed())
				{
					Assert.IsTrue(fi.omitNorms == !dir.FileExists(segName + ".f" + i));
				}
			}
		}
		
		[Test]
        public virtual void  TestPositionIncrementGap()
		{
			Analyzer analyzer = new AnonymousClassAnalyzer(this);
			
			Lucene.Net.Search.Similarity similarity = Lucene.Net.Search.Similarity.GetDefault();
			DocumentWriter writer = new DocumentWriter(dir, analyzer, similarity, 50);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("repeated", "repeated one", Field.Store.YES, Field.Index.TOKENIZED));
			doc.Add(new Field("repeated", "repeated two", Field.Store.YES, Field.Index.TOKENIZED));
			
			System.String segName = "test";
			writer.AddDocument(segName, doc);
			SegmentReader reader = SegmentReader.Get(new SegmentInfo(segName, 1, dir));
			
			TermPositions termPositions = reader.TermPositions(new Term("repeated", "repeated"));
			Assert.IsTrue(termPositions.Next());
			int freq = termPositions.Freq();
			Assert.AreEqual(2, freq);
			Assert.AreEqual(0, termPositions.NextPosition());
			Assert.AreEqual(502, termPositions.NextPosition());
		}
	}
}