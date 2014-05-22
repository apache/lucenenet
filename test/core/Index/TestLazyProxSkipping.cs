namespace Lucene.Net.Index
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


	using Lucene.Net.Analysis;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using PhraseQuery = Lucene.Net.Search.PhraseQuery;
	using ScoreDoc = Lucene.Net.Search.ScoreDoc;
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using IndexInput = Lucene.Net.Store.IndexInput;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using RAMDirectory = Lucene.Net.Store.RAMDirectory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using TestUtil = Lucene.Net.Util.TestUtil;

	/// <summary>
	/// Tests lazy skipping on the proximity file.
	/// 
	/// </summary>
	public class TestLazyProxSkipping : LuceneTestCase
	{
		private IndexSearcher Searcher;
		private int SeeksCounter = 0;

		private string Field = "tokens";
		private string Term1 = "xx";
		private string Term2 = "yy";
		private string Term3 = "zz";

		private class SeekCountingDirectory : MockDirectoryWrapper
		{
			private readonly TestLazyProxSkipping OuterInstance;

		  public SeekCountingDirectory(TestLazyProxSkipping outerInstance, Directory @delegate) : base(random(), @delegate)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override IndexInput OpenInput(string name, IOContext context)
		  {
			IndexInput ii = base.openInput(name, context);
			if (name.EndsWith(".prx") || name.EndsWith(".pos"))
			{
			  // we decorate the proxStream with a wrapper class that allows to count the number of calls of seek()
			  ii = new SeeksCountingStream(OuterInstance, ii);
			}
			return ii;
		  }

		}

		private void CreateIndex(int numHits)
		{
			int numDocs = 500;

			Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this);
			Directory directory = new SeekCountingDirectory(this, new RAMDirectory());
			// note: test explicitly disables payloads
			IndexWriter writer = new IndexWriter(directory, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setMaxBufferedDocs(10).setMergePolicy(newLogMergePolicy(false)));

			for (int i = 0; i < numDocs; i++)
			{
				Document doc = new Document();
				string content;
				if (i % (numDocs / numHits) == 0)
				{
					// add a document that matches the query "term1 term2"
					content = this.Term1 + " " + this.Term2;
				}
				else if (i % 15 == 0)
				{
					// add a document that only contains term1
					content = this.Term1 + " " + this.Term1;
				}
				else
				{
					// add a document that contains term2 but not term 1
					content = this.Term3 + " " + this.Term2;
				}

				doc.add(newTextField(this.Field, content, Field.Store.YES));
				writer.addDocument(doc);
			}

			// make sure the index has only a single segment
			writer.forceMerge(1);
			writer.close();

		  SegmentReader reader = getOnlySegmentReader(DirectoryReader.open(directory));

		  this.Searcher = newSearcher(reader);
		}

		private class AnalyzerAnonymousInnerClassHelper : Analyzer
		{
			private readonly TestLazyProxSkipping OuterInstance;

			public AnalyzerAnonymousInnerClassHelper(TestLazyProxSkipping outerInstance)
			{
				this.OuterInstance = outerInstance;
			}

			public override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
			{
			  return new TokenStreamComponents(new MockTokenizer(reader, MockTokenizer.WHITESPACE, true));
			}
		}

		private ScoreDoc[] Search()
		{
			// create PhraseQuery "term1 term2" and search
			PhraseQuery pq = new PhraseQuery();
			pq.add(new Term(this.Field, this.Term1));
			pq.add(new Term(this.Field, this.Term2));
			return this.Searcher.search(pq, null, 1000).scoreDocs;
		}

		private void PerformTest(int numHits)
		{
			CreateIndex(numHits);
			this.SeeksCounter = 0;
			ScoreDoc[] hits = Search();
			// verify that the right number of docs was found
			Assert.AreEqual(numHits, hits.Length);

			// check if the number of calls of seek() does not exceed the number of hits
			Assert.IsTrue(this.SeeksCounter > 0);
			Assert.IsTrue("seeksCounter=" + this.SeeksCounter + " numHits=" + numHits, this.SeeksCounter <= numHits + 1);
			Searcher.IndexReader.close();
		}

		public virtual void TestLazySkipping()
		{
		  string fieldFormat = TestUtil.getPostingsFormat(this.Field);
		  assumeFalse("this test cannot run with Memory postings format", fieldFormat.Equals("Memory"));
		  assumeFalse("this test cannot run with Direct postings format", fieldFormat.Equals("Direct"));
		  assumeFalse("this test cannot run with SimpleText postings format", fieldFormat.Equals("SimpleText"));

			// test whether only the minimum amount of seeks()
			// are performed
			PerformTest(5);
			PerformTest(10);
		}

		public virtual void TestSeek()
		{
			Directory directory = newDirectory();
			IndexWriter writer = new IndexWriter(directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
			for (int i = 0; i < 10; i++)
			{
				Document doc = new Document();
				doc.add(newTextField(this.Field, "a b", Field.Store.YES));
				writer.addDocument(doc);
			}

			writer.close();
			IndexReader reader = DirectoryReader.open(directory);

			DocsAndPositionsEnum tp = MultiFields.getTermPositionsEnum(reader, MultiFields.getLiveDocs(reader), this.Field, new BytesRef("b"));

			for (int i = 0; i < 10; i++)
			{
				tp.nextDoc();
				Assert.AreEqual(tp.docID(), i);
				Assert.AreEqual(tp.nextPosition(), 1);
			}

			tp = MultiFields.getTermPositionsEnum(reader, MultiFields.getLiveDocs(reader), this.Field, new BytesRef("a"));

			for (int i = 0; i < 10; i++)
			{
				tp.nextDoc();
				Assert.AreEqual(tp.docID(), i);
				Assert.AreEqual(tp.nextPosition(), 0);
			}
			reader.close();
			directory.close();

		}


		// Simply extends IndexInput in a way that we are able to count the number
		// of invocations of seek()
		internal class SeeksCountingStream : IndexInput
		{
			private readonly TestLazyProxSkipping OuterInstance;

			  internal IndexInput Input;


			  internal SeeksCountingStream(TestLazyProxSkipping outerInstance, IndexInput input) : base("SeekCountingStream(" + input + ")")
			  {
				  this.OuterInstance = outerInstance;
				  this.Input = input;
			  }

			  public override sbyte ReadByte()
			  {
				  return this.Input.readByte();
			  }

			  public override void ReadBytes(sbyte[] b, int offset, int len)
			  {
				  this.Input.readBytes(b, offset, len);
			  }

			  public override void Close()
			  {
				  this.Input.close();
			  }

			  public override long FilePointer
			  {
				  get
				  {
					  return this.Input.FilePointer;
				  }
			  }

			  public override void Seek(long pos)
			  {
				  OuterInstance.SeeksCounter++;
				  this.Input.seek(pos);
			  }

			  public override long Length()
			  {
				  return this.Input.length();
			  }

			  public override SeeksCountingStream Clone()
			  {
				  return new SeeksCountingStream(OuterInstance, this.Input.clone());
			  }

		}
	}

}