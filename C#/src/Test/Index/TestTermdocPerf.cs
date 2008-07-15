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
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
using Analyzer = Lucene.Net.Analysis.Analyzer;
using Token = Lucene.Net.Analysis.Token;
using TokenStream = Lucene.Net.Analysis.TokenStream;

namespace Lucene.Net.Index
{
	
	/// <author>  yonik
	/// </author>
	/// <version>  $Id$
	/// </version>
	
	class RepeatingTokenStream : TokenStream
	{
		public int num;
		internal Token t;
		
		public RepeatingTokenStream(System.String val)
		{
			t = new Token(val, 0, val.Length);
		}
		
		public override Token Next()
		{
			return --num < 0?null:t;
		}
	}
	
	[TestFixture]
	public class TestTermdocPerf : LuceneTestCase
	{
		private class AnonymousClassAnalyzer:Analyzer
		{
			public AnonymousClassAnalyzer(System.Random random, float percentDocs, Lucene.Net.Index.RepeatingTokenStream ts, int maxTF, TestTermdocPerf enclosingInstance)
			{
				InitBlock(random, percentDocs, ts, maxTF, enclosingInstance);
			}
			private void  InitBlock(System.Random random, float percentDocs, Lucene.Net.Index.RepeatingTokenStream ts, int maxTF, TestTermdocPerf enclosingInstance)
			{
				this.random = random;
				this.percentDocs = percentDocs;
				this.ts = ts;
				this.maxTF = maxTF;
				this.enclosingInstance = enclosingInstance;
			}
			private System.Random random;
			private float percentDocs;
			private Lucene.Net.Index.RepeatingTokenStream ts;
			private int maxTF;
			private TestTermdocPerf enclosingInstance;
			public TestTermdocPerf Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			public override TokenStream TokenStream(System.String fieldName, System.IO.TextReader reader)
			{
				if ((float) random.NextDouble() < percentDocs)
					ts.num = random.Next(maxTF) + 1;
				else
					ts.num = 0;
				return ts;
			}
		}
		
		internal virtual void  AddDocs(Directory dir, int ndocs, System.String field, System.String val, int maxTF, float percentDocs)
		{
			System.Random random = new System.Random((System.Int32) 0);
			RepeatingTokenStream ts = new RepeatingTokenStream(val);
			
			Analyzer analyzer = new AnonymousClassAnalyzer(random, percentDocs, ts, maxTF, this);
			
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field(field, val, Field.Store.NO, Field.Index.NO_NORMS));
			IndexWriter writer = new IndexWriter(dir, analyzer, true);
			writer.SetMaxBufferedDocs(100);
			writer.SetMergeFactor(100);
			
			for (int i = 0; i < ndocs; i++)
			{
				writer.AddDocument(doc);
			}
			
			writer.Optimize();
			writer.Close();
		}
		
		
		public virtual int DoTest(int iter, int ndocs, int maxTF, float percentDocs)
		{
			Directory dir = new RAMDirectory();
			
			long start = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
			AddDocs(dir, ndocs, "foo", "val", maxTF, percentDocs);
			long end = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
			System.Console.Out.WriteLine("milliseconds for creation of " + ndocs + " docs = " + (end - start));
			
			IndexReader reader = IndexReader.Open(dir);
			TermEnum tenum = reader.Terms(new Term("foo", "val"));
			TermDocs tdocs = reader.TermDocs();
			
			start = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
			
			int ret = 0;
			for (int i = 0; i < iter; i++)
			{
				tdocs.Seek(tenum);
				while (tdocs.Next())
				{
					ret += tdocs.Doc();
				}
			}
			
			end = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
			System.Console.Out.WriteLine("milliseconds for " + iter + " TermDocs iteration: " + (end - start));
			
			return ret;
		}
		
		[Test]
		public virtual void  TestTermDocPerf()
		{
			// performance test for 10% of documents containing a term
			// DoTest(100000, 10000,3,.1f);
		}
	}
}