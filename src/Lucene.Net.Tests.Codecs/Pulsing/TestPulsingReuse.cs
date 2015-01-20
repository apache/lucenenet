using System.Collections.Generic;

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
	using NestedPulsingPostingsFormat = org.apache.lucene.codecs.nestedpulsing.NestedPulsingPostingsFormat;
	using Document = org.apache.lucene.document.Document;
	using Field = org.apache.lucene.document.Field;
	using TextField = org.apache.lucene.document.TextField;
	using AtomicReader = org.apache.lucene.index.AtomicReader;
	using DirectoryReader = org.apache.lucene.index.DirectoryReader;
	using DocsAndPositionsEnum = org.apache.lucene.index.DocsAndPositionsEnum;
	using DocsEnum = org.apache.lucene.index.DocsEnum;
	using RandomIndexWriter = org.apache.lucene.index.RandomIndexWriter;
	using TermsEnum = org.apache.lucene.index.TermsEnum;
	using BaseDirectoryWrapper = org.apache.lucene.store.BaseDirectoryWrapper;
	using Directory = org.apache.lucene.store.Directory;
	using LuceneTestCase = org.apache.lucene.util.LuceneTestCase;
	using TestUtil = org.apache.lucene.util.TestUtil;

	/// <summary>
	/// Tests that pulsing codec reuses its enums and wrapped enums
	/// </summary>
	public class TestPulsingReuse : LuceneTestCase
	{
	  // TODO: this is a basic test. this thing is complicated, add more
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testSophisticatedReuse() throws Exception
	  public virtual void testSophisticatedReuse()
	  {
		// we always run this test with pulsing codec.
		Codec cp = TestUtil.alwaysPostingsFormat(new Pulsing41PostingsFormat(1));
		Directory dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setCodec(cp));
		Document doc = new Document();
		doc.add(new TextField("foo", "a b b c c c d e f g g h i i j j k", Field.Store.NO));
		iw.addDocument(doc);
		DirectoryReader ir = iw.Reader;
		iw.close();

		AtomicReader segment = getOnlySegmentReader(ir);
		DocsEnum reuse = null;
		IDictionary<DocsEnum, bool?> allEnums = new IdentityHashMap<DocsEnum, bool?>();
		TermsEnum te = segment.terms("foo").iterator(null);
		while (te.next() != null)
		{
		  reuse = te.docs(null, reuse, DocsEnum.FLAG_NONE);
		  allEnums[reuse] = true;
		}

		assertEquals(2, allEnums.Count);

		allEnums.Clear();
		DocsAndPositionsEnum posReuse = null;
		te = segment.terms("foo").iterator(null);
		while (te.next() != null)
		{
		  posReuse = te.docsAndPositions(null, posReuse);
		  allEnums[posReuse] = true;
		}

		assertEquals(2, allEnums.Count);

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// tests reuse with Pulsing1(Pulsing2(Standard)) </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNestedPulsing() throws Exception
	  public virtual void testNestedPulsing()
	  {
		// we always run this test with pulsing codec.
		Codec cp = TestUtil.alwaysPostingsFormat(new NestedPulsingPostingsFormat());
		BaseDirectoryWrapper dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setCodec(cp));
		Document doc = new Document();
		doc.add(new TextField("foo", "a b b c c c d e f g g g h i i j j k l l m m m", Field.Store.NO));
		// note: the reuse is imperfect, here we would have 4 enums (lost reuse when we get an enum for 'm')
		// this is because we only track the 'last' enum we reused (not all).
		// but this seems 'good enough' for now.
		iw.addDocument(doc);
		DirectoryReader ir = iw.Reader;
		iw.close();

		AtomicReader segment = getOnlySegmentReader(ir);
		DocsEnum reuse = null;
		IDictionary<DocsEnum, bool?> allEnums = new IdentityHashMap<DocsEnum, bool?>();
		TermsEnum te = segment.terms("foo").iterator(null);
		while (te.next() != null)
		{
		  reuse = te.docs(null, reuse, DocsEnum.FLAG_NONE);
		  allEnums[reuse] = true;
		}

		assertEquals(4, allEnums.Count);

		allEnums.Clear();
		DocsAndPositionsEnum posReuse = null;
		te = segment.terms("foo").iterator(null);
		while (te.next() != null)
		{
		  posReuse = te.docsAndPositions(null, posReuse);
		  allEnums[posReuse] = true;
		}

		assertEquals(4, allEnums.Count);

		ir.close();
		dir.close();
	  }
	}

}