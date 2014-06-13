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
	using Codec = Lucene.Net.Codecs.Codec;
	using Document = Lucene.Net.Document.Document;
	using NumericDocValuesField = Lucene.Net.Document.NumericDocValuesField;
	using SortedSetDocValuesField = Lucene.Net.Document.SortedSetDocValuesField;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LineFileDocs = Lucene.Net.Util.LineFileDocs;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	/// <summary>
	/// Compares one codec against another
	/// </summary>
	public class TestDuelingCodecs : LuceneTestCase
	{
	  private Directory LeftDir;
	  private IndexReader LeftReader;
	  private Codec LeftCodec;

	  private Directory RightDir;
	  private IndexReader RightReader;
	  private Codec RightCodec;

	  private string Info; // for debugging

	  public override void SetUp()
	  {
		base.SetUp();

		// for now its SimpleText vs Lucene46(random postings format)
		// as this gives the best overall coverage. when we have more
		// codecs we should probably pick 2 from Codec.availableCodecs()

		LeftCodec = Codec.forName("SimpleText");
		RightCodec = new RandomCodec(Random());

		LeftDir = NewDirectory();
		RightDir = NewDirectory();

		long seed = Random().nextLong();

		// must use same seed because of random payloads, etc
		int maxTermLength = TestUtil.NextInt(Random(), 1, IndexWriter.MAX_TERM_LENGTH);
		MockAnalyzer leftAnalyzer = new MockAnalyzer(new Random(seed));
		leftAnalyzer.MaxTokenLength = maxTermLength;
		MockAnalyzer rightAnalyzer = new MockAnalyzer(new Random(seed));
		rightAnalyzer.MaxTokenLength = maxTermLength;

		// but these can be different
		// TODO: this turns this into a really big test of Multi*, is that what we want?
		IndexWriterConfig leftConfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, leftAnalyzer);
		leftConfig.Codec = LeftCodec;
		// preserve docids
		leftConfig.MergePolicy = NewLogMergePolicy();

		IndexWriterConfig rightConfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, rightAnalyzer);
		rightConfig.Codec = RightCodec;
		// preserve docids
		rightConfig.MergePolicy = NewLogMergePolicy();

		// must use same seed because of random docvalues fields, etc
		RandomIndexWriter leftWriter = new RandomIndexWriter(new Random(seed), LeftDir, leftConfig);
		RandomIndexWriter rightWriter = new RandomIndexWriter(new Random(seed), RightDir, rightConfig);

		int numdocs = AtLeast(100);
		CreateRandomIndex(numdocs, leftWriter, seed);
		CreateRandomIndex(numdocs, rightWriter, seed);

		LeftReader = MaybeWrapReader(leftWriter.Reader);
        leftWriter.Close();
		RightReader = MaybeWrapReader(rightWriter.Reader);
        rightWriter.Close();

		// check that our readers are valid
		TestUtil.CheckReader(LeftReader);
		TestUtil.CheckReader(RightReader);

		Info = "left: " + LeftCodec.ToString() + " / right: " + RightCodec.ToString();
	  }

	  public override void TearDown()
	  {
		if (LeftReader != null)
		{
		  LeftReader.Dispose();
		}
		if (RightReader != null)
		{
		  RightReader.Dispose();
		}

		if (LeftDir != null)
		{
		  LeftDir.Dispose();
		}
		if (RightDir != null)
		{
		  RightDir.Dispose();
		}

		base.TearDown();
	  }

	  /// <summary>
	  /// populates a writer with random stuff. this must be fully reproducable with the seed!
	  /// </summary>
	  public static void CreateRandomIndex(int numdocs, RandomIndexWriter writer, long seed)
	  {
		Random random = new Random(seed);
		// primary source for our data is from linefiledocs, its realistic.
		LineFileDocs lineFileDocs = new LineFileDocs(random);

		// TODO: we should add other fields that use things like docs&freqs but omit positions,
		// because linefiledocs doesn't cover all the possibilities.
		for (int i = 0; i < numdocs; i++)
		{
		  Document document = lineFileDocs.NextDoc();
		  // grab the title and add some SortedSet instances for fun
		  string title = document.Get("titleTokenized");
		  string[] split = title.Split("\\s+", true);
		  foreach (string trash in split)
		  {
			document.Add(new SortedSetDocValuesField("sortedset", new BytesRef(trash)));
		  }
		  // add a numeric dv field sometimes
		  document.removeFields("sparsenumeric");
		  if (random.Next(4) == 2)
		  {
			document.Add(new NumericDocValuesField("sparsenumeric", random.Next()));
		  }
		  writer.AddDocument(document);
		}

        lineFileDocs.Close();
	  }

	  /// <summary>
	  /// checks the two indexes are equivalent
	  /// </summary>
	  public virtual void TestEquals()
	  {
		AssertReaderEquals(Info, LeftReader, RightReader);
	  }

	}

}