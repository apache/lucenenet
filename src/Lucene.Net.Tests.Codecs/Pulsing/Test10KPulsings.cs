using System.Text;

namespace org.apache.lucene.codecs.pulsing
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


	using MockAnalyzer = org.apache.lucene.analysis.MockAnalyzer;
	using Document = org.apache.lucene.document.Document;
	using Field = org.apache.lucene.document.Field;
	using FieldType = org.apache.lucene.document.FieldType;
	using TextField = org.apache.lucene.document.TextField;
	using DocsEnum = org.apache.lucene.index.DocsEnum;
	using IndexOptions = org.apache.lucene.index.FieldInfo.IndexOptions;
	using IndexReader = org.apache.lucene.index.IndexReader;
	using MultiFields = org.apache.lucene.index.MultiFields;
	using RandomIndexWriter = org.apache.lucene.index.RandomIndexWriter;
	using TermsEnum = org.apache.lucene.index.TermsEnum;
	using DocIdSetIterator = org.apache.lucene.search.DocIdSetIterator;
	using BaseDirectoryWrapper = org.apache.lucene.store.BaseDirectoryWrapper;
	using LuceneTestCase = org.apache.lucene.util.LuceneTestCase;
	using TestUtil = org.apache.lucene.util.TestUtil;

	/// <summary>
	/// Pulses 10k terms/docs, 
	/// originally designed to find JRE bugs (https://issues.apache.org/jira/browse/LUCENE-3335)
	/// 
	/// @lucene.experimental
	/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @LuceneTestCase.Nightly public class Test10KPulsings extends org.apache.lucene.util.LuceneTestCase
	public class Test10KPulsings : LuceneTestCase
	{
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test10kPulsed() throws Exception
	  public virtual void test10kPulsed()
	  {
		// we always run this test with pulsing codec.
		Codec cp = TestUtil.alwaysPostingsFormat(new Pulsing41PostingsFormat(1));

		File f = createTempDir("10kpulsed");
		BaseDirectoryWrapper dir = newFSDirectory(f);
		dir.CheckIndexOnClose = false; // we do this ourselves explicitly
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setCodec(cp));

		Document document = new Document();
		FieldType ft = new FieldType(TextField.TYPE_STORED);

		switch (TestUtil.Next(random(), 0, 2))
		{
		  case 0:
			  ft.IndexOptions = IndexOptions.DOCS_ONLY;
			  break;
		  case 1:
			  ft.IndexOptions = IndexOptions.DOCS_AND_FREQS;
			  break;
		  default:
			  ft.IndexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
			  break;
		}

		Field field = newField("field", "", ft);
		document.add(field);

		NumberFormat df = new DecimalFormat("00000", new DecimalFormatSymbols(Locale.ROOT));

		for (int i = 0; i < 10050; i++)
		{
		  field.StringValue = df.format(i);
		  iw.addDocument(document);
		}

		IndexReader ir = iw.Reader;
		iw.close();

		TermsEnum te = MultiFields.getTerms(ir, "field").iterator(null);
		DocsEnum de = null;

		for (int i = 0; i < 10050; i++)
		{
		  string expected = df.format(i);
		  assertEquals(expected, te.next().utf8ToString());
		  de = TestUtil.docs(random(), te, null, de, DocsEnum.FLAG_NONE);
		  assertTrue(de.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		  assertEquals(DocIdSetIterator.NO_MORE_DOCS, de.nextDoc());
		}
		ir.close();

		TestUtil.checkIndex(dir);
		dir.close();
	  }

	  /// <summary>
	  /// a variant, that uses pulsing, but uses a high TF to force pass thru to the underlying codec
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test10kNotPulsed() throws Exception
	  public virtual void test10kNotPulsed()
	  {
		// we always run this test with pulsing codec.
		int freqCutoff = TestUtil.Next(random(), 1, 10);
		Codec cp = TestUtil.alwaysPostingsFormat(new Pulsing41PostingsFormat(freqCutoff));

		File f = createTempDir("10knotpulsed");
		BaseDirectoryWrapper dir = newFSDirectory(f);
		dir.CheckIndexOnClose = false; // we do this ourselves explicitly
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setCodec(cp));

		Document document = new Document();
		FieldType ft = new FieldType(TextField.TYPE_STORED);

		switch (TestUtil.Next(random(), 0, 2))
		{
		  case 0:
			  ft.IndexOptions = IndexOptions.DOCS_ONLY;
			  break;
		  case 1:
			  ft.IndexOptions = IndexOptions.DOCS_AND_FREQS;
			  break;
		  default:
			  ft.IndexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
			  break;
		}

		Field field = newField("field", "", ft);
		document.add(field);

		NumberFormat df = new DecimalFormat("00000", new DecimalFormatSymbols(Locale.ROOT));

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int freq = freqCutoff + 1;
		int freq = freqCutoff + 1;

		for (int i = 0; i < 10050; i++)
		{
		  StringBuilder sb = new StringBuilder();
		  for (int j = 0; j < freq; j++)
		  {
			sb.Append(df.format(i));
			sb.Append(' '); // whitespace
		  }
		  field.StringValue = sb.ToString();
		  iw.addDocument(document);
		}

		IndexReader ir = iw.Reader;
		iw.close();

		TermsEnum te = MultiFields.getTerms(ir, "field").iterator(null);
		DocsEnum de = null;

		for (int i = 0; i < 10050; i++)
		{
		  string expected = df.format(i);
		  assertEquals(expected, te.next().utf8ToString());
		  de = TestUtil.docs(random(), te, null, de, DocsEnum.FLAG_NONE);
		  assertTrue(de.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		  assertEquals(DocIdSetIterator.NO_MORE_DOCS, de.nextDoc());
		}
		ir.close();

		TestUtil.checkIndex(dir);
		dir.close();
	  }
	}

}