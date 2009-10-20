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

using Lucene.Net.Documents;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using SimpleAnalyzer = Lucene.Net.Analysis.SimpleAnalyzer;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Search
{
	
	/// <version>  $Id: TestRemoteSearchable.java 583534 2007-10-10 16:46:35Z mikemccand $
	/// </version>
	[TestFixture]
	public class TestRemoteSearchable : LuceneTestCase
	{
		private static readonly string RemoteTypeName = typeof(RemoteSearchable).Name;
		private static System.Runtime.Remoting.Channels.Http.HttpChannel httpChannel;
		private static bool serverStarted;

		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			if (!serverStarted) //should always evaluate to true
			{
				httpChannel = new System.Runtime.Remoting.Channels.Http.HttpChannel(0);
				StartServer();
			}
		}

		[TestFixtureTearDown]
		public void FixtureTeardown()
		{
			try
			{
				System.Runtime.Remoting.Channels.ChannelServices.UnregisterChannel(httpChannel);
			}
			catch
			{
			}

			httpChannel = null;
		}

		private static Lucene.Net.Search.Searchable GetRemote()
		{
			return LookupRemote();
		}

		private static Lucene.Net.Search.Searchable LookupRemote()
		{
			return (Lucene.Net.Search.Searchable)Activator.GetObject(typeof(Lucene.Net.Search.Searchable), httpChannel.GetUrlsForUri(RemoteTypeName)[0]);
		}

		public static void StartServer()
		{
			if (serverStarted)
			{
				return;
			}

			try
			{
				System.Runtime.Remoting.Channels.ChannelServices.RegisterChannel(httpChannel, false);
			}
			catch (System.Net.Sockets.SocketException ex)
			{
				if (ex.ErrorCode == 10048)
					return;     // EADDRINUSE?
				throw ex;
			}

			// construct an index
			RAMDirectory indexStore = new RAMDirectory();
			IndexWriter writer = new IndexWriter(indexStore, new SimpleAnalyzer(), true, IndexWriter.MaxFieldLength.LIMITED);

			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("test", "test text", Field.Store.YES, Field.Index.ANALYZED));
			doc.Add(new Field("other", "other test text", Field.Store.YES, Field.Index.ANALYZED));
			writer.AddDocument(doc);

			writer.Optimize();
			writer.Close();

			// publish it
			Lucene.Net.Search.Searchable local = new IndexSearcher(indexStore);
			RemoteSearchable impl = new RemoteSearchable(local);
			System.Runtime.Remoting.RemotingServices.Marshal(impl, RemoteTypeName);
			serverStarted = true;
		}
		
		//private Lucene.Net.Search.Searchable GetRemote()
		//{
		//    try
		//    {
		//        return LookupRemote();
		//    }
		//    catch (System.Exception)
		//    {
		//        StartServer();
		//        return LookupRemote();
		//    }
		//}
		
		//private  Lucene.Net.Search.Searchable LookupRemote()
		//{
		//    return (Lucene.Net.Search.Searchable) Activator.GetObject(typeof(Lucene.Net.Search.Searchable), @"http://localhost:1099/Searchable");
		//}
		
		//[SetUp]
		//public void StartServer()
		//{
		//    try
		//    {
		//        System.Runtime.Remoting.Channels.ChannelServices.RegisterChannel(new System.Runtime.Remoting.Channels.Http.HttpChannel(1099), false);
		//    }
		//    catch (System.Net.Sockets.SocketException ex)
		//    {
		//        if (ex.ErrorCode == 10048)
		//            return;     // EADDRINUSE?
		//        throw ex;
		//    }

		//    // construct an index
		//    RAMDirectory indexStore = new RAMDirectory();
		//    IndexWriter writer = new IndexWriter(indexStore, new SimpleAnalyzer(), true);
		//    Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
		//    doc.Add(new Field("test", "test text", Field.Store.YES, Field.Index.ANALYZED));
		//    doc.Add(new Field("other", "other test text", Field.Store.YES, Field.Index.ANALYZED));
		//    writer.AddDocument(doc);
		//    writer.Optimize();
		//    writer.Close();
			
		//    // publish it
		//    Lucene.Net.Search.Searchable local = new IndexSearcher(indexStore);
		//    RemoteSearchable impl = new RemoteSearchable(local);
		//    System.Runtime.Remoting.RemotingServices.Marshal(impl, "Searchable");
		//}
		
		private void  Search(Query query)
		{
			// try to search the published index
			Lucene.Net.Search.Searchable[] searchables = new Lucene.Net.Search.Searchable[]{GetRemote()};
			Searcher searcher = new MultiSearcher(searchables);
            ScoreDoc[] result = searcher.Search(query, null, 1000).scoreDocs;
			
			Assert.AreEqual(1, result.Length);
			Document document = searcher.Doc(result[0].doc);
			Assert.IsTrue(document != null, "document is null and it shouldn't be");
			Assert.AreEqual(document.Get("test"), "test text");
			Assert.IsTrue(document.GetFields().Count == 2, "document.getFields() Size: " + document.GetFields().Count + " is not: " + 2);
			System.Collections.Hashtable ftl = new System.Collections.Hashtable();
			ftl.Add("other", "other");
			FieldSelector fs = new SetBasedFieldSelector(ftl, new System.Collections.Hashtable());
			document = searcher.Doc(0, fs);
			Assert.IsTrue(document != null, "document is null and it shouldn't be");
			Assert.IsTrue(document.GetFields().Count == 1, "document.getFields() Size: " + document.GetFields().Count + " is not: " + 1);
			fs = new MapFieldSelector(new System.String[]{"other"});
			document = searcher.Doc(0, fs);
			Assert.IsTrue(document != null, "document is null and it shouldn't be");
			Assert.IsTrue(document.GetFields().Count == 1, "document.getFields() Size: " + document.GetFields().Count + " is not: " + 1);
		}
		
		[Test]
		public virtual void  TestTermQuery()
		{
			Search(new TermQuery(new Term("test", "test")));
		}
		
		[Test]
		public virtual void  TestBooleanQuery()
		{
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term("test", "test")), BooleanClause.Occur.MUST);
			Search(query);
		}
		
		[Test]
		public virtual void  TestPhraseQuery()
		{
			PhraseQuery query = new PhraseQuery();
			query.Add(new Term("test", "test"));
			query.Add(new Term("test", "text"));
			Search(query);
		}
		
		// Tests bug fix at http://nagoya.apache.org/bugzilla/show_bug.cgi?id=20290
		[Test]
		public virtual void  TestQueryFilter()
		{
			// try to search the published index
			Lucene.Net.Search.Searchable[] searchables = new Lucene.Net.Search.Searchable[]{GetRemote()};
			Searcher searcher = new MultiSearcher(searchables);
			ScoreDoc[] hits = searcher.Search(new TermQuery(new Term("test", "text")), new QueryWrapperFilter(new TermQuery(new Term("test", "test"))), 1000).scoreDocs;
			Assert.AreEqual(1, hits.Length);
            ScoreDoc[] nohits = searcher.Search(new TermQuery(new Term("test", "text")), new QueryWrapperFilter(new TermQuery(new Term("test", "non-existent-term"))), 1000).scoreDocs;
			Assert.AreEqual(0, nohits.Length);
		}
		
		[Test]
		public virtual void  TestConstantScoreQuery()
		{
			// try to search the published index
			Lucene.Net.Search.Searchable[] searchables = new Lucene.Net.Search.Searchable[]{GetRemote()};
			Searcher searcher = new MultiSearcher(searchables);
            ScoreDoc[] hits = searcher.Search(new ConstantScoreQuery(new QueryWrapperFilter(new TermQuery(new Term("test", "test")))), 1000).scoreDocs;
			Assert.AreEqual(1, hits.Length);
		}
	}
}