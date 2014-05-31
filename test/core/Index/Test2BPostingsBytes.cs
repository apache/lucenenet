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

	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using TokenStream = Lucene.Net.Analysis.TokenStream;
	using CharTermAttribute = Lucene.Net.Analysis.Tokenattributes.CharTermAttribute;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using TextField = Lucene.Net.Document.TextField;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions_e;
	using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using TimeUnits = Lucene.Net.Util.TimeUnits;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
	using Ignore = org.junit.Ignore;

	using TimeoutSuite = com.carrotsearch.randomizedtesting.annotations.TimeoutSuite;

	/// <summary>
	/// Test indexes 2B docs with 65k freqs each, 
	/// so you get > Integer.MAX_VALUE postings data for the term
	/// @lucene.experimental
	/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs({ "SimpleText", "Memory", "Direct", "Lucene3x" }) @TimeoutSuite(millis = 4 * TimeUnits.HOUR) public class Test2BPostingsBytes extends Lucene.Net.Util.LuceneTestCase
	public class Test2BPostingsBytes : LuceneTestCase
	// disable Lucene3x: older lucene formats always had this issue.
	  // @Absurd @Ignore takes ~20GB-30GB of space and 10 minutes.
	  // with some codecs needs more heap space as well.
	{
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ignore("Very slow. Enable manually by removing @Ignore.") public void test() throws Exception
		public virtual void Test()
		{
		BaseDirectoryWrapper dir = newFSDirectory(createTempDir("2BPostingsBytes1"));
		if (dir is MockDirectoryWrapper)
		{
		  ((MockDirectoryWrapper)dir).Throttling = MockDirectoryWrapper.Throttling.NEVER;
		}

		IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))
	   .setMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH).setRAMBufferSizeMB(256.0).setMergeScheduler(new ConcurrentMergeScheduler()).setMergePolicy(newLogMergePolicy(false, 10)).setOpenMode(IndexWriterConfig.OpenMode_e.CREATE));

		MergePolicy mp = w.Config.MergePolicy;
		if (mp is LogByteSizeMergePolicy)
		{
		 // 1 petabyte:
		 ((LogByteSizeMergePolicy) mp).MaxMergeMB = 1024 * 1024 * 1024;
		}

		Document doc = new Document();
		FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
		ft.IndexOptions = IndexOptions.DOCS_AND_FREQS;
		ft.OmitNorms = true;
		MyTokenStream tokenStream = new MyTokenStream();
		Field field = new Field("field", tokenStream, ft);
		doc.add(field);

		const int numDocs = 1000;
		for (int i = 0; i < numDocs; i++)
		{
		  if (i % 2 == 1) // trick blockPF's little optimization
		  {
			tokenStream.n = 65536;
		  }
		  else
		  {
			tokenStream.n = 65537;
		  }
		  w.addDocument(doc);
		}
		w.forceMerge(1);
		w.close();

		DirectoryReader oneThousand = DirectoryReader.open(dir);
		IndexReader[] subReaders = new IndexReader[1000];
		Arrays.fill(subReaders, oneThousand);
		MultiReader mr = new MultiReader(subReaders);
		BaseDirectoryWrapper dir2 = newFSDirectory(createTempDir("2BPostingsBytes2"));
		if (dir2 is MockDirectoryWrapper)
		{
		  ((MockDirectoryWrapper)dir2).Throttling = MockDirectoryWrapper.Throttling.NEVER;
		}
		IndexWriter w2 = new IndexWriter(dir2, new IndexWriterConfig(TEST_VERSION_CURRENT, null));
		w2.addIndexes(mr);
		w2.forceMerge(1);
		w2.close();
		oneThousand.close();

		DirectoryReader oneMillion = DirectoryReader.open(dir2);
		subReaders = new IndexReader[2000];
		Arrays.fill(subReaders, oneMillion);
		mr = new MultiReader(subReaders);
		BaseDirectoryWrapper dir3 = newFSDirectory(createTempDir("2BPostingsBytes3"));
		if (dir3 is MockDirectoryWrapper)
		{
		  ((MockDirectoryWrapper)dir3).Throttling = MockDirectoryWrapper.Throttling.NEVER;
		}
		IndexWriter w3 = new IndexWriter(dir3, new IndexWriterConfig(TEST_VERSION_CURRENT, null));
		w3.addIndexes(mr);
		w3.forceMerge(1);
		w3.close();
		oneMillion.close();

		dir.close();
		dir2.close();
		dir3.close();
		}

	  public sealed class MyTokenStream : TokenStream
	  {
		internal readonly CharTermAttribute TermAtt = addAttribute(typeof(CharTermAttribute));
		internal int Index;
		internal int n;

		public override bool IncrementToken()
		{
		  if (Index < n)
		  {
			ClearAttributes();
			TermAtt.buffer()[0] = 'a';
			TermAtt.Length = 1;
			Index++;
			return true;
		  }
		  return false;
		}

		public override void Reset()
		{
		  Index = 0;
		}
	  }
	}

}