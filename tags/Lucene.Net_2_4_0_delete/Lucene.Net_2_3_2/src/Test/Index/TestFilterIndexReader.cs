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

//using TestRunner = junit.textui.TestRunner;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using MockRAMDirectory = Lucene.Net.Store.MockRAMDirectory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;

namespace Lucene.Net.Index
{
	
	[TestFixture]
	public class TestFilterIndexReader : LuceneTestCase
	{
		
		private class TestReader : FilterIndexReader
		{
			
			/// <summary>Filter that only permits terms containing 'e'.</summary>
			private class TestTermEnum : FilterTermEnum
			{
				public TestTermEnum(TermEnum termEnum):base(termEnum)
				{
				}
				
				/// <summary>Scan for terms containing the letter 'e'.</summary>
				public override bool Next()
				{
					while (in_Renamed.Next())
					{
						if (in_Renamed.Term().Text().IndexOf('e') != - 1)
							return true;
					}
					return false;
				}
			}
			
			/// <summary>Filter that only returns odd numbered documents. </summary>
			private class TestTermPositions : FilterTermPositions
			{
				public TestTermPositions(TermPositions in_Renamed) : base(in_Renamed)
				{
				}
				
				/// <summary>Scan for odd numbered documents. </summary>
				public override bool Next()
				{
					while (in_Renamed.Next())
					{
						if ((in_Renamed.Doc() % 2) == 1)
							return true;
					}
					return false;
				}
			}
			
			public TestReader(IndexReader reader) : base(reader)
			{
			}
			
			/// <summary>Filter terms with TestTermEnum. </summary>
			public override TermEnum Terms()
			{
				return new TestTermEnum(in_Renamed.Terms());
			}
			
			/// <summary>Filter positions with TestTermPositions. </summary>
			public override TermPositions TermPositions()
			{
				return new TestTermPositions(in_Renamed.TermPositions());
			}
		}
		
		
		/// <summary>Main for running test case by itself. </summary>
		[STAThread]
		public static void  Main(System.String[] args)
		{
			// NUnit.Core.TestRunner.Run(new NUnit.Core.TestSuite(typeof(TestIndexReader)));   // {{Aroush}} where is 'Run' in NUnit?
		}
		
		/// <summary> Tests the IndexReader.getFieldNames implementation</summary>
		/// <throws>  Exception on error </throws>
		[Test]
		public virtual void  TestFilterIndexReader_Renamed_Method()
		{
			RAMDirectory directory = new MockRAMDirectory();
			IndexWriter writer = new IndexWriter(directory, new WhitespaceAnalyzer(), true);
			
			Lucene.Net.Documents.Document d1 = new Lucene.Net.Documents.Document();
			d1.Add(new Field("default", "one two", Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(d1);
			
			Lucene.Net.Documents.Document d2 = new Lucene.Net.Documents.Document();
			d2.Add(new Field("default", "one three", Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(d2);
			
			Lucene.Net.Documents.Document d3 = new Lucene.Net.Documents.Document();
			d3.Add(new Field("default", "two four", Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(d3);
			
			writer.Close();
			
			IndexReader reader = new TestReader(IndexReader.Open(directory));
			
			Assert.IsTrue(reader.IsOptimized());
			
			TermEnum terms = reader.Terms();
			while (terms.Next())
			{
				Assert.IsTrue(terms.Term().Text().IndexOf('e') != - 1);
			}
			terms.Close();
			
			TermPositions positions = reader.TermPositions(new Term("default", "one"));
			while (positions.Next())
			{
				Assert.IsTrue((positions.Doc() % 2) == 1);
			}
			
			reader.Close();
			directory.Close();
		}
	}
}