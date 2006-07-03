/*
 * Copyright 2004 The Apache Software Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
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
using Similarity = Lucene.Net.Search.Similarity;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Index
{
	
	[TestFixture]
	public class TestFieldsReader
	{
		private RAMDirectory dir = new RAMDirectory();
		private Lucene.Net.Documents.Document testDoc = new Lucene.Net.Documents.Document();
		private FieldInfos fieldInfos = null;
		
		
		[SetUp]
        public virtual void  SetUp()
		{
			fieldInfos = new FieldInfos();
			DocHelper.SetupDoc(testDoc);
			fieldInfos.Add(testDoc);
			DocumentWriter writer = new DocumentWriter(dir, new WhitespaceAnalyzer(), Similarity.GetDefault(), 50);
			Assert.IsTrue(writer != null);
			writer.AddDocument("test", testDoc);
		}
		
		[Test]
        public virtual void  Test()
		{
			Assert.IsTrue(dir != null);
			Assert.IsTrue(fieldInfos != null);
			FieldsReader reader = new FieldsReader(dir, "test", fieldInfos);
			Assert.IsTrue(reader != null);
			Assert.IsTrue(reader.Size() == 1);
			Lucene.Net.Documents.Document doc = reader.Doc(0);
			Assert.IsTrue(doc != null);
			Assert.IsTrue(doc.GetField("textField1") != null);
			
			Field field = doc.GetField("textField2");
			Assert.IsTrue(field != null);
			Assert.IsTrue(field.IsTermVectorStored() == true);
			
			Assert.IsTrue(field.IsStoreOffsetWithTermVector() == true);
			Assert.IsTrue(field.IsStorePositionWithTermVector() == true);
			Assert.IsTrue(field.GetOmitNorms() == false);
			
			field = doc.GetField("textField3");
			Assert.IsTrue(field != null);
			Assert.IsTrue(field.IsTermVectorStored() == false);
			Assert.IsTrue(field.IsStoreOffsetWithTermVector() == false);
			Assert.IsTrue(field.IsStorePositionWithTermVector() == false);
			Assert.IsTrue(field.GetOmitNorms() == true);
			
			
			reader.Close();
		}
	}
}