using System;

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
	using PositionIncrementAttribute = Lucene.Net.Analysis.Tokenattributes.PositionIncrementAttribute;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using TextField = Lucene.Net.Document.TextField;
	using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using TimeUnits = Lucene.Net.Util.TimeUnits;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
	/*using Ignore = org.junit.Ignore;

	using TimeoutSuite = com.carrotsearch.randomizedtesting.annotations.TimeoutSuite;*/

	/// <summary>
	/// Test indexes ~82M docs with 52 positions each, so you get > Integer.MAX_VALUE positions
	/// @lucene.experimental
	/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs({ "SimpleText", "Memory", "Direct" }) @TimeoutSuite(millis = 4 * TimeUnits.HOUR) public class Test2BPositions extends Lucene.Net.Util.LuceneTestCase
	public class Test2BPositions : LuceneTestCase
	  // uses lots of space and takes a few minutes
	{
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ignore("Very slow. Enable manually by removing @Ignore.") public void test() throws Exception
		public virtual void Test()
		{
		BaseDirectoryWrapper dir = NewFSDirectory(CreateTempDir("2BPositions"));
		if (dir is MockDirectoryWrapper)
		{
		  ((MockDirectoryWrapper)dir).Throttling = MockDirectoryWrapper.Throttling_e.NEVER;
		}

		IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))
	   .SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH).SetRAMBufferSizeMB(256.0).SetMergeScheduler(new ConcurrentMergeScheduler()).SetMergePolicy(NewLogMergePolicy(false, 10)).SetOpenMode(IndexWriterConfig.OpenMode_e.CREATE));

		MergePolicy mp = w.Config.MergePolicy;
		if (mp is LogByteSizeMergePolicy)
		{
		 // 1 petabyte:
		 ((LogByteSizeMergePolicy) mp).MaxMergeMB = 1024 * 1024 * 1024;
		}

		Document doc = new Document();
		FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
		ft.OmitNorms = true;
		Field field = new Field("field", new MyTokenStream(), ft);
		doc.Add(field);

		int numDocs = (int.MaxValue / 26) + 1;
		for (int i = 0; i < numDocs; i++)
		{
		  w.AddDocument(doc);
		  if (VERBOSE && i % 100000 == 0)
		  {
			Console.WriteLine(i + " of " + numDocs + "...");
		  }
		}
		w.ForceMerge(1);
		w.Dispose();
		dir.Dispose();
		}

	  public sealed class MyTokenStream : TokenStream
	  {
		internal readonly CharTermAttribute TermAtt = AddAttribute<CharTermAttribute>();
		internal readonly PositionIncrementAttribute PosIncAtt = AddAttribute<PositionIncrementAttribute>();
		internal int Index;

		public override bool IncrementToken()
		{
		  if (Index < 52)
		  {
			ClearAttributes();
			TermAtt.Length = 1;
			TermAtt.Buffer()[0] = 'a';
			PosIncAtt.PositionIncrement = 1 + Index;
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