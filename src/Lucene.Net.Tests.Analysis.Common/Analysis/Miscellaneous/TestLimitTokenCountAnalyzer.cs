using System.Text;

namespace org.apache.lucene.analysis.miscellaneous
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

	using Document = org.apache.lucene.document.Document;
	using Field = org.apache.lucene.document.Field;
	using DirectoryReader = org.apache.lucene.index.DirectoryReader;
	using IndexReader = org.apache.lucene.index.IndexReader;
	using IndexWriter = org.apache.lucene.index.IndexWriter;
	using IndexWriterConfig = org.apache.lucene.index.IndexWriterConfig;
	using Term = org.apache.lucene.index.Term;
	using Directory = org.apache.lucene.store.Directory;
	using TestUtil = org.apache.lucene.util.TestUtil;

	public class TestLimitTokenCountAnalyzer : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testLimitTokenCountAnalyzer() throws java.io.IOException
	  public virtual void testLimitTokenCountAnalyzer()
	  {
		foreach (bool consumeAll in new bool[] {true, false})
		{
		  MockAnalyzer mock = new MockAnalyzer(random());

		  // if we are consuming all tokens, we can use the checks, 
		  // otherwise we can't
		  mock.EnableChecks = consumeAll;
		  Analyzer a = new LimitTokenCountAnalyzer(mock, 2, consumeAll);

		  // dont use assertAnalyzesTo here, as the end offset is not the end of the string (unless consumeAll is true, in which case its correct)!
		  assertTokenStreamContents(a.tokenStream("dummy", "1  2     3  4  5"), new string[] {"1", "2"}, new int[] {0, 3}, new int[] {1, 4}, consumeAll ? 16 : null);
		  assertTokenStreamContents(a.tokenStream("dummy", "1 2 3 4 5"), new string[] {"1", "2"}, new int[] {0, 2}, new int[] {1, 3}, consumeAll ? 9 : null);

		  // less than the limit, ensure we behave correctly
		  assertTokenStreamContents(a.tokenStream("dummy", "1  "), new string[] {"1"}, new int[] {0}, new int[] {1}, consumeAll ? 3 : null);

		  // equal to limit
		  assertTokenStreamContents(a.tokenStream("dummy", "1  2  "), new string[] {"1", "2"}, new int[] {0, 3}, new int[] {1, 4}, consumeAll ? 6 : null);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testLimitTokenCountIndexWriter() throws java.io.IOException
	  public virtual void testLimitTokenCountIndexWriter()
	  {

		foreach (bool consumeAll in new bool[] {true, false})
		{
		  Directory dir = newDirectory();
		  int limit = TestUtil.Next(random(), 50, 101000);
		  MockAnalyzer mock = new MockAnalyzer(random());

		  // if we are consuming all tokens, we can use the checks, 
		  // otherwise we can't
		  mock.EnableChecks = consumeAll;
		  Analyzer a = new LimitTokenCountAnalyzer(mock, limit, consumeAll);

		  IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, a));

		  Document doc = new Document();
		  StringBuilder b = new StringBuilder();
		  for (int i = 1;i < limit;i++)
		  {
			b.Append(" a");
		  }
		  b.Append(" x");
		  b.Append(" z");
		  doc.add(newTextField("field", b.ToString(), Field.Store.NO));
		  writer.addDocument(doc);
		  writer.close();

		  IndexReader reader = DirectoryReader.open(dir);
		  Term t = new Term("field", "x");
		  assertEquals(1, reader.docFreq(t));
		  t = new Term("field", "z");
		  assertEquals(0, reader.docFreq(t));
		  reader.close();
		  dir.close();
		}
	  }

	}

}