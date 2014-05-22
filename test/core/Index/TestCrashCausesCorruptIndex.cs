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
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using TermQuery = Lucene.Net.Search.TermQuery;
	using TopDocs = Lucene.Net.Search.TopDocs;
	using Directory = Lucene.Net.Store.Directory;
	using FSDirectory = Lucene.Net.Store.FSDirectory;
	using FilterDirectory = Lucene.Net.Store.FilterDirectory;
	using IOContext = Lucene.Net.Store.IOContext;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestCrashCausesCorruptIndex : LuceneTestCase
	{

	  internal File Path;

	  /// <summary>
	  /// LUCENE-3627: this test fails.
	  /// </summary>
	  public virtual void TestCrashCorruptsIndexing()
	  {
		Path = createTempDir("testCrashCorruptsIndexing");

		IndexAndCrashOnCreateOutputSegments2();

		SearchForFleas(2);

		IndexAfterRestart();

		SearchForFleas(3);
	  }

	  /// <summary>
	  /// index 1 document and commit.
	  /// prepare for crashing.
	  /// index 1 more document, and upon commit, creation of segments_2 will crash.
	  /// </summary>
	  private void IndexAndCrashOnCreateOutputSegments2()
	  {
		Directory realDirectory = FSDirectory.open(Path);
		CrashAfterCreateOutput crashAfterCreateOutput = new CrashAfterCreateOutput(realDirectory);

		// NOTE: cannot use RandomIndexWriter because it
		// sometimes commits:
		IndexWriter indexWriter = new IndexWriter(crashAfterCreateOutput, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

		indexWriter.addDocument(Document);
		// writes segments_1:
		indexWriter.commit();

		crashAfterCreateOutput.CrashAfterCreateOutput = "segments_2";
		indexWriter.addDocument(Document);
		try
		{
		  // tries to write segments_2 but hits fake exc:
		  indexWriter.commit();
		  Assert.Fail("should have hit CrashingException");
		}
		catch (CrashingException e)
		{
		  // expected
		}
		// writes segments_3
		indexWriter.close();
		Assert.IsFalse(slowFileExists(realDirectory, "segments_2"));
		crashAfterCreateOutput.close();
	  }

	  /// <summary>
	  /// Attempts to index another 1 document.
	  /// </summary>
	  private void IndexAfterRestart()
	  {
		Directory realDirectory = newFSDirectory(Path);

		// LUCENE-3627 (before the fix): this line fails because
		// it doesn't know what to do with the created but empty
		// segments_2 file
		IndexWriter indexWriter = new IndexWriter(realDirectory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

		// currently the test fails above.
		// however, to test the fix, the following lines should pass as well.
		indexWriter.addDocument(Document);
		indexWriter.close();
		Assert.IsFalse(slowFileExists(realDirectory, "segments_2"));
		realDirectory.close();
	  }

	  /// <summary>
	  /// Run an example search.
	  /// </summary>
	  private void SearchForFleas(int expectedTotalHits)
	  {
		Directory realDirectory = newFSDirectory(Path);
		IndexReader indexReader = DirectoryReader.open(realDirectory);
		IndexSearcher indexSearcher = newSearcher(indexReader);
		TopDocs topDocs = indexSearcher.search(new TermQuery(new Term(TEXT_FIELD, "fleas")), 10);
		Assert.IsNotNull(topDocs);
		Assert.AreEqual(expectedTotalHits, topDocs.totalHits);
		indexReader.close();
		realDirectory.close();
	  }

	  private const string TEXT_FIELD = "text";

	  /// <summary>
	  /// Gets a document with content "my dog has fleas".
	  /// </summary>
	  private Document Document
	  {
		  get
		  {
			Document document = new Document();
			document.add(newTextField(TEXT_FIELD, "my dog has fleas", Field.Store.NO));
			return document;
		  }
	  }

	  /// <summary>
	  /// The marker RuntimeException that we use in lieu of an
	  /// actual machine crash.
	  /// </summary>
	  private class CrashingException : Exception
	  {
		public CrashingException(string msg) : base(msg)
		{
		}
	  }

	  /// <summary>
	  /// this test class provides direct access to "simulating" a crash right after 
	  /// realDirectory.createOutput(..) has been called on a certain specified name.
	  /// </summary>
	  private class CrashAfterCreateOutput : FilterDirectory
	  {

		internal string CrashAfterCreateOutput_Renamed;

		public CrashAfterCreateOutput(Directory realDirectory) : base(realDirectory)
		{
		  LockFactory = realDirectory.LockFactory;
		}

		public virtual string CrashAfterCreateOutput
		{
			set
			{
			  this.CrashAfterCreateOutput_Renamed = value;
			}
		}

		public override IndexOutput CreateOutput(string name, IOContext cxt)
		{
		  IndexOutput indexOutput = @in.createOutput(name, cxt);
		  if (null != CrashAfterCreateOutput_Renamed && name.Equals(CrashAfterCreateOutput_Renamed))
		  {
			// CRASH!
			indexOutput.close();
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: now crash");
			  (new Exception()).printStackTrace(System.out);
			}
			throw new CrashingException("crashAfterCreateOutput " + CrashAfterCreateOutput_Renamed);
		  }
		  return indexOutput;
		}

	  }

	}

}