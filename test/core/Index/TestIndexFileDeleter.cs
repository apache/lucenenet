using System;
using System.Collections.Generic;

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
	using Field = Lucene.Net.Document.Field;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
	using Directory = Lucene.Net.Store.Directory;
	using IndexInput = Lucene.Net.Store.IndexInput;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	/*
	  Verify we can read the pre-2.1 file format, do searches
	  against it, and add documents to it.
	*/

	public class TestIndexFileDeleter : LuceneTestCase
	{

	  public virtual void TestDeleteLeftoverFiles()
	  {
		Directory dir = newDirectory();
		if (dir is MockDirectoryWrapper)
		{
		  ((MockDirectoryWrapper)dir).PreventDoubleWrite = false;
		}

		MergePolicy mergePolicy = newLogMergePolicy(true, 10);

		// this test expects all of its segments to be in CFS
		mergePolicy.NoCFSRatio = 1.0;
		mergePolicy.MaxCFSSegmentSizeMB = double.PositiveInfinity;

		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(10).setMergePolicy(mergePolicy).setUseCompoundFile(true));

		int i;
		for (i = 0;i < 35;i++)
		{
		  AddDoc(writer, i);
		}
		writer.Config.MergePolicy.NoCFSRatio = 0.0;
		writer.Config.UseCompoundFile = false;
		for (;i < 45;i++)
		{
		  AddDoc(writer, i);
		}
		writer.close();

		// Delete one doc so we get a .del file:
		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(NoMergePolicy.NO_COMPOUND_FILES).setUseCompoundFile(true));
		Term searchTerm = new Term("id", "7");
		writer.deleteDocuments(searchTerm);
		writer.close();

		// Now, artificially create an extra .del file & extra
		// .s0 file:
		string[] files = dir.listAll();

		/*
		for(int j=0;j<files.length;j++) {
		  System.out.println(j + ": " + files[j]);
		}
		*/

		// TODO: fix this test better
		string ext = Codec.Default.Name.Equals("SimpleText") ? ".liv" : ".del";

		// Create a bogus separate del file for a
		// segment that already has a separate del file: 
		CopyFile(dir, "_0_1" + ext, "_0_2" + ext);

		// Create a bogus separate del file for a
		// segment that does not yet have a separate del file:
		CopyFile(dir, "_0_1" + ext, "_1_1" + ext);

		// Create a bogus separate del file for a
		// non-existent segment:
		CopyFile(dir, "_0_1" + ext, "_188_1" + ext);

		// Create a bogus segment file:
		CopyFile(dir, "_0.cfs", "_188.cfs");

		// Create a bogus fnm file when the CFS already exists:
		CopyFile(dir, "_0.cfs", "_0.fnm");

		// Create some old segments file:
		CopyFile(dir, "segments_2", "segments");
		CopyFile(dir, "segments_2", "segments_1");

		// Create a bogus cfs file shadowing a non-cfs segment:

		// TODO: assert is bogus (relies upon codec-specific filenames)
		Assert.IsTrue(slowFileExists(dir, "_3.fdt") || slowFileExists(dir, "_3.fld"));
		Assert.IsTrue(!slowFileExists(dir, "_3.cfs"));
		CopyFile(dir, "_1.cfs", "_3.cfs");

		string[] filesPre = dir.listAll();

		// Open & close a writer: it should delete the above 4
		// files and nothing more:
		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND));
		writer.close();

		string[] files2 = dir.listAll();
		dir.close();

		Array.Sort(files);
		Array.Sort(files2);

		Set<string> dif = DifFiles(files, files2);

		if (!Array.Equals(files, files2))
		{
		  Assert.Fail("IndexFileDeleter failed to delete unreferenced extra files: should have deleted " + (filesPre.Length - files.Length) + " files but only deleted " + (filesPre.Length - files2.Length) + "; expected files:\n    " + AsString(files) + "\n  actual files:\n    " + AsString(files2) + "\ndiff: " + dif);
		}
	  }

	  private static Set<string> DifFiles(string[] files1, string[] files2)
	  {
		Set<string> set1 = new HashSet<string>();
		Set<string> set2 = new HashSet<string>();
		Set<string> extra = new HashSet<string>();

		for (int x = 0; x < files1.Length; x++)
		{
		  set1.add(files1[x]);
		}
		for (int x = 0; x < files2.Length; x++)
		{
		  set2.add(files2[x]);
		}
		IEnumerator<string> i1 = set1.GetEnumerator();
		while (i1.MoveNext())
		{
		  string o = i1.Current;
		  if (!set2.contains(o))
		  {
			extra.add(o);
		  }
		}
		IEnumerator<string> i2 = set2.GetEnumerator();
		while (i2.MoveNext())
		{
		  string o = i2.Current;
		  if (!set1.contains(o))
		  {
			extra.add(o);
		  }
		}
		return extra;
	  }

	  private string AsString(string[] l)
	  {
		string s = "";
		for (int i = 0;i < l.Length;i++)
		{
		  if (i > 0)
		  {
			s += "\n    ";
		  }
		  s += l[i];
		}
		return s;
	  }

	  public virtual void CopyFile(Directory dir, string src, string dest)
	  {
		IndexInput @in = dir.openInput(src, newIOContext(random()));
		IndexOutput @out = dir.createOutput(dest, newIOContext(random()));
		sbyte[] b = new sbyte[1024];
		long remainder = @in.length();
		while (remainder > 0)
		{
		  int len = (int) Math.Min(b.Length, remainder);
		  @in.readBytes(b, 0, len);
		  @out.writeBytes(b, len);
		  remainder -= len;
		}
		@in.close();
		@out.close();
	  }

	  private void AddDoc(IndexWriter writer, int id)
	  {
		Document doc = new Document();
		doc.add(newTextField("content", "aaa", Field.Store.NO));
		doc.add(newStringField("id", Convert.ToString(id), Field.Store.NO));
		writer.addDocument(doc);
	  }
	}

}