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
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using SimpleAnalyzer = Lucene.Net.Analysis.SimpleAnalyzer;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Search
{
	
	/// <summary> Tests that the index is cached on the searcher side of things.
	/// NOTE: This is copied from TestRemoteSearchable since it already had a remote index set up.
	/// </summary>
	/// <author>  Matt Ericson
	/// </author>
	[TestFixture]
	public class TestRemoteCachingWrapperFilter : LuceneTestCase
	{
		private static System.Runtime.Remoting.Channels.Http.HttpChannel httpChannel;
		private static int port;
		private static bool serverStarted;

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			Random rnd = new Random((int) (DateTime.Now.Ticks & 0x7fffffff));
			port = rnd.Next(System.Net.IPEndPoint.MinPort, System.Net.IPEndPoint.MaxPort);
			httpChannel = new System.Runtime.Remoting.Channels.Http.HttpChannel(port);
			if (!serverStarted)
				StartServer();
		}

		[TearDown]
		public override void TearDown()
		{
			try
			{
				System.Runtime.Remoting.Channels.ChannelServices.UnregisterChannel(httpChannel);
			}
			catch
			{
			}
            
			httpChannel = null;
			base.TearDown();
		}

		private static Lucene.Net.Search.Searchable GetRemote()
		{
			return LookupRemote();
		}

		private static Lucene.Net.Search.Searchable LookupRemote()
		{
			return (Lucene.Net.Search.Searchable)Activator.GetObject(typeof(Lucene.Net.Search.Searchable), string.Format("http://localhost:{0}/Searchable", port));
		}

		private static void StartServer()
		{
			if (serverStarted)
			{
				return;
			}

			// construct an index
			RAMDirectory indexStore = new RAMDirectory();
			IndexWriter writer = new IndexWriter(indexStore, new SimpleAnalyzer(), true);

			Document doc = new Document();
			doc.Add(new Field("test", "test text", Field.Store.YES, Field.Index.TOKENIZED));
			doc.Add(new Field("type", "A", Field.Store.YES, Field.Index.TOKENIZED));
			doc.Add(new Field("other", "other test text", Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(doc);

			//Need a second document to search for
			doc = new Document();
			doc.Add(new Field("test", "test text", Field.Store.YES, Field.Index.TOKENIZED));
			doc.Add(new Field("type", "B", Field.Store.YES, Field.Index.TOKENIZED));
			doc.Add(new Field("other", "other test text", Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(doc);

			writer.Optimize();
			writer.Close();

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

			// publish it
			Lucene.Net.Search.Searchable local = new IndexSearcher(indexStore);
			RemoteSearchable impl = new RemoteSearchable(local);
			System.Runtime.Remoting.RemotingServices.Marshal(impl, "Searchable");
			serverStarted = true;
		}

		
		//private static Lucene.Net.Search.Searchable GetRemote()
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
		
		//private static Lucene.Net.Search.Searchable LookupRemote()
		//{
		//    return (Lucene.Net.Search.Searchable) Activator.GetObject(typeof(Lucene.Net.Search.Searchable), "http://" + "//localhost/Searchable");
		//}
		
		//private static void  StartServer()
		//{
		//    // construct an index
		//    RAMDirectory indexStore = new RAMDirectory();
		//    IndexWriter writer = new IndexWriter(indexStore, new SimpleAnalyzer(), true);
		//    Document doc = new Document();
		//    doc.Add(new Field("test", "test text", Field.Store.YES, Field.Index.TOKENIZED));
		//    doc.Add(new Field("type", "A", Field.Store.YES, Field.Index.TOKENIZED));
		//    doc.Add(new Field("other", "other test text", Field.Store.YES, Field.Index.TOKENIZED));
		//    writer.AddDocument(doc);
		//    //Need a second document to search for
		//    doc = new Document();
		//    doc.Add(new Field("test", "test text", Field.Store.YES, Field.Index.TOKENIZED));
		//    doc.Add(new Field("type", "B", Field.Store.YES, Field.Index.TOKENIZED));
		//    doc.Add(new Field("other", "other test text", Field.Store.YES, Field.Index.TOKENIZED));
		//    writer.AddDocument(doc);
		//    writer.Optimize();
		//    writer.Close();
			
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

		//    // publish it
		//    Lucene.Net.Search.Searchable local = new IndexSearcher(indexStore);
		//    RemoteSearchable impl = new RemoteSearchable(local);
		//    System.Runtime.Remoting.RemotingServices.Marshal(impl, "Searchable");
		//}
		
		private static void  Search(Query query, Filter filter, int hitNumber, System.String typeValue)
		{
			Lucene.Net.Search.Searchable[] searchables = new Lucene.Net.Search.Searchable[]{GetRemote()};
			Searcher searcher = new MultiSearcher(searchables);
			Hits result = searcher.Search(query, filter);
			Assert.AreEqual(1, result.Length());
			Document document = result.Doc(hitNumber);
			Assert.IsTrue(document != null, "document is null and it shouldn't be");
			Assert.AreEqual(typeValue, document.Get("type"));
			Assert.IsTrue(document.GetFields().Count == 3, "document.getFields() Size: " + document.GetFields().Count + " is not: " + 3);
		}
		
		
		[Test]
		public virtual void  TestTermRemoteFilter()
		{
			CachingWrapperFilterHelper cwfh = new CachingWrapperFilterHelper(new QueryFilter(new TermQuery(new Term("type", "a"))));
			
			// This is what we are fixing - if one uses a CachingWrapperFilter(Helper) it will never 
			// cache the filter on the remote site
			cwfh.SetShouldHaveCache(false);
			Search(new TermQuery(new Term("test", "test")), cwfh, 0, "A");
			cwfh.SetShouldHaveCache(false);
			Search(new TermQuery(new Term("test", "test")), cwfh, 0, "A");
			
			// This is how we fix caching - we wrap a Filter in the RemoteCachingWrapperFilter(Handler - for testing)
			// to cache the Filter on the searcher (remote) side
			RemoteCachingWrapperFilterHelper rcwfh = new RemoteCachingWrapperFilterHelper(cwfh, false);
			Search(new TermQuery(new Term("test", "test")), rcwfh, 0, "A");
			
			// 2nd time we do the search, we should be using the cached Filter
			rcwfh.ShouldHaveCache(true);
			Search(new TermQuery(new Term("test", "test")), rcwfh, 0, "A");
			
			// assert that we get the same cached Filter, even if we create a new instance of RemoteCachingWrapperFilter(Helper)
			// this should pass because the Filter parameters are the same, and the cache uses Filter's hashCode() as cache keys,
			// and Filters' hashCode() builds on Filter parameters, not the Filter instance itself
			rcwfh = new RemoteCachingWrapperFilterHelper(new QueryFilter(new TermQuery(new Term("type", "a"))), false);
			rcwfh.ShouldHaveCache(false);
			Search(new TermQuery(new Term("test", "test")), rcwfh, 0, "A");
			
			rcwfh = new RemoteCachingWrapperFilterHelper(new QueryFilter(new TermQuery(new Term("type", "a"))), false);
			rcwfh.ShouldHaveCache(true);
			Search(new TermQuery(new Term("test", "test")), rcwfh, 0, "A");
			
			// assert that we get a non-cached version of the Filter because this is a new Query (type:b)
			rcwfh = new RemoteCachingWrapperFilterHelper(new QueryFilter(new TermQuery(new Term("type", "b"))), false);
			rcwfh.ShouldHaveCache(false);
			Search(new TermQuery(new Term("type", "b")), rcwfh, 0, "B");
		}
	}
}