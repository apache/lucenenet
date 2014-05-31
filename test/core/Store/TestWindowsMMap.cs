using System.Text;

namespace Lucene.Net.Store
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

	using Field = Lucene.Net.Document.Field;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;

	public class TestWindowsMMap : LuceneTestCase
	{

	  private const string Alphabet = "abcdefghijklmnopqrstuvwzyz";

	  public override void SetUp()
	  {
		base.setUp();
	  }

	  private string RandomToken()
	  {
		int tl = 1 + random().Next(7);
		StringBuilder sb = new StringBuilder();
		for (int cx = 0; cx < tl; cx++)
		{
		  int c = random().Next(25);
		  sb.Append(Alphabet.Substring(c, 1));
		}
		return sb.ToString();
	  }

	  private string RandomField()
	  {
		int fl = 1 + random().Next(3);
		StringBuilder fb = new StringBuilder();
		for (int fx = 0; fx < fl; fx++)
		{
		  fb.Append(RandomToken());
		  fb.Append(" ");
		}
		return fb.ToString();
	  }

	  public virtual void TestMmapIndex()
	  {
		// sometimes the directory is not cleaned by rmDir, because on Windows it
		// may take some time until the files are finally dereferenced. So clean the
		// directory up front, or otherwise new IndexWriter will fail.
		File dirPath = createTempDir("testLuceneMmap");
		RmDir(dirPath);
		MMapDirectory dir = new MMapDirectory(dirPath, null);

		// plan to add a set of useful stopwords, consider changing some of the
		// interior filters.
		MockAnalyzer analyzer = new MockAnalyzer(random());
		// TODO: something about lock timeouts and leftover locks.
		IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, analyzer)
		   .setOpenMode(IndexWriterConfig.OpenMode_e.CREATE));
		writer.commit();
		IndexReader reader = DirectoryReader.open(dir);
		IndexSearcher searcher = newSearcher(reader);

		int num = atLeast(1000);
		for (int dx = 0; dx < num; dx++)
		{
		  string f = RandomField();
		  Document doc = new Document();
		  doc.add(newTextField("data", f, Field.Store.YES));
		  writer.addDocument(doc);
		}

		reader.close();
		writer.close();
		RmDir(dirPath);
	  }

	  private void RmDir(File dir)
	  {
		if (!dir.exists())
		{
		  return;
		}
		foreach (File file in dir.listFiles())
		{
		  file.delete();
		}
		dir.delete();
	  }
	}

}