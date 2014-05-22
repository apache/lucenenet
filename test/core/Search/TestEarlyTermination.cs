namespace Lucene.Net.Search
{

	/*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements.  See the NOTICE file distributed with
	 * this work for additional information regarding copyright ownership.
	 * The ASF licenses this file to You under the Apache License, Version 2.0
	 * (the "License"); you may not use this file except in compliance with
	 * the License.  You may obtain a copy of the License at
	 *
	 *     http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */

	using Document = Lucene.Net.Document.Document;
	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	public class TestEarlyTermination : LuceneTestCase
	{

	  internal Directory Dir;
	  internal RandomIndexWriter Writer;

	  public override void SetUp()
	  {
		base.setUp();
		Dir = newDirectory();
		Writer = new RandomIndexWriter(random(), Dir);
		int numDocs = atLeast(100);
		for (int i = 0; i < numDocs; i++)
		{
		  Writer.addDocument(new Document());
		  if (rarely())
		  {
			Writer.commit();
		  }
		}
	  }

	  public override void TearDown()
	  {
		base.tearDown();
		Writer.close();
		Dir.close();
	  }

	  public virtual void TestEarlyTermination()
	  {
		int iters = atLeast(5);
		IndexReader reader = Writer.Reader;

		for (int i = 0; i < iters; ++i)
		{
		  IndexSearcher searcher = newSearcher(reader);
		  Collector collector = new CollectorAnonymousInnerClassHelper(this);

		  searcher.search(new MatchAllDocsQuery(), collector);
		}
		reader.close();
	  }

	  private class CollectorAnonymousInnerClassHelper : Collector
	  {
		  private readonly TestEarlyTermination OuterInstance;

		  public CollectorAnonymousInnerClassHelper(TestEarlyTermination outerInstance)
		  {
			  this.OuterInstance = outerInstance;
			  outOfOrder = random().nextBoolean();
			  collectionTerminated = true;
		  }


		  internal readonly bool outOfOrder;
		  internal bool collectionTerminated;

		  public override Scorer Scorer
		  {
			  set
			  {
			  }
		  }
		  public override void Collect(int doc)
		  {
			Assert.IsFalse(collectionTerminated);
			if (rarely())
			{
			  collectionTerminated = true;
			  throw new CollectionTerminatedException();
			}
		  }

		  public override AtomicReaderContext NextReader
		  {
			  set
			  {
				if (random().nextBoolean())
				{
				  collectionTerminated = true;
				  throw new CollectionTerminatedException();
				}
				else
				{
				  collectionTerminated = false;
				}
			  }
		  }

		  public override bool AcceptsDocsOutOfOrder()
		  {
			return outOfOrder;
		  }

	  }

	}

}