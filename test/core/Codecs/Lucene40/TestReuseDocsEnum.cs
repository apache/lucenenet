using System;
using System.Collections.Generic;

namespace Lucene.Net.Codecs.Lucene40
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
	using AtomicReader = Lucene.Net.Index.AtomicReader;
	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using DocsEnum = Lucene.Net.Index.DocsEnum;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Terms = Lucene.Net.Index.Terms;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using Directory = Lucene.Net.Store.Directory;
	using MatchNoBits = Lucene.Net.Util.Bits.MatchNoBits;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using LineFileDocs = Lucene.Net.Util.LineFileDocs;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using BeforeClass = org.junit.BeforeClass;

	// TODO: really this should be in BaseTestPF or somewhere else? useful test!
	public class TestReuseDocsEnum : LuceneTestCase
	{

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void beforeClass()
	  public static void BeforeClass()
	  {
		OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true; // explicitly instantiates ancient codec
	  }

	  public virtual void TestReuseDocsEnumNoReuse()
	  {
		Directory dir = newDirectory();
		Codec cp = TestUtil.alwaysPostingsFormat(new Lucene40RWPostingsFormat());
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setCodec(cp));
		int numdocs = atLeast(20);
		CreateRandomIndex(numdocs, writer, random());
		writer.commit();

		DirectoryReader open = DirectoryReader.open(dir);
		foreach (AtomicReaderContext ctx in open.leaves())
		{
		  AtomicReader indexReader = ctx.reader();
		  Terms terms = indexReader.terms("body");
		  TermsEnum iterator = terms.iterator(null);
		  IdentityHashMap<DocsEnum, bool?> enums = new IdentityHashMap<DocsEnum, bool?>();
		  MatchNoBits bits = new MatchNoBits(indexReader.maxDoc());
		  while ((iterator.next()) != null)
		  {
			DocsEnum docs = iterator.docs(random().nextBoolean() ? bits : new MatchNoBits(indexReader.maxDoc()), null, random().nextBoolean() ? DocsEnum.FLAG_FREQS : DocsEnum.FLAG_NONE);
			enums.put(docs, true);
		  }

		  Assert.AreEqual(terms.size(), enums.size());
		}
		IOUtils.close(writer, open, dir);
	  }

	  // tests for reuse only if bits are the same either null or the same instance
	  public virtual void TestReuseDocsEnumSameBitsOrNull()
	  {
		Directory dir = newDirectory();
		Codec cp = TestUtil.alwaysPostingsFormat(new Lucene40RWPostingsFormat());
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setCodec(cp));
		int numdocs = atLeast(20);
		CreateRandomIndex(numdocs, writer, random());
		writer.commit();

		DirectoryReader open = DirectoryReader.open(dir);
		foreach (AtomicReaderContext ctx in open.leaves())
		{
		  Terms terms = ctx.reader().terms("body");
		  TermsEnum iterator = terms.iterator(null);
		  IdentityHashMap<DocsEnum, bool?> enums = new IdentityHashMap<DocsEnum, bool?>();
		  MatchNoBits bits = new MatchNoBits(open.maxDoc());
		  DocsEnum docs = null;
		  while ((iterator.next()) != null)
		  {
			docs = iterator.docs(bits, docs, random().nextBoolean() ? DocsEnum.FLAG_FREQS : DocsEnum.FLAG_NONE);
			enums.put(docs, true);
		  }

		  Assert.AreEqual(1, enums.size());
		  enums.clear();
		  iterator = terms.iterator(null);
		  docs = null;
		  while ((iterator.next()) != null)
		  {
			docs = iterator.docs(new MatchNoBits(open.maxDoc()), docs, random().nextBoolean() ? DocsEnum.FLAG_FREQS : DocsEnum.FLAG_NONE);
			enums.put(docs, true);
		  }
		  Assert.AreEqual(terms.size(), enums.size());

		  enums.clear();
		  iterator = terms.iterator(null);
		  docs = null;
		  while ((iterator.next()) != null)
		  {
			docs = iterator.docs(null, docs, random().nextBoolean() ? DocsEnum.FLAG_FREQS : DocsEnum.FLAG_NONE);
			enums.put(docs, true);
		  }
		  Assert.AreEqual(1, enums.size());
		}
		IOUtils.close(writer, open, dir);
	  }

	  // make sure we never reuse from another reader even if it is the same field & codec etc
	  public virtual void TestReuseDocsEnumDifferentReader()
	  {
		Directory dir = newDirectory();
		Codec cp = TestUtil.alwaysPostingsFormat(new Lucene40RWPostingsFormat());
		MockAnalyzer analyzer = new MockAnalyzer(random());
		analyzer.MaxTokenLength = TestUtil.Next(random(), 1, IndexWriter.MAX_TERM_LENGTH);

		RandomIndexWriter writer = new RandomIndexWriter(random(), dir, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setCodec(cp));
		int numdocs = atLeast(20);
		CreateRandomIndex(numdocs, writer, random());
		writer.commit();

		DirectoryReader firstReader = DirectoryReader.open(dir);
		DirectoryReader secondReader = DirectoryReader.open(dir);
		IList<AtomicReaderContext> leaves = firstReader.leaves();
		IList<AtomicReaderContext> leaves2 = secondReader.leaves();

		foreach (AtomicReaderContext ctx in leaves)
		{
		  Terms terms = ctx.reader().terms("body");
		  TermsEnum iterator = terms.iterator(null);
		  IdentityHashMap<DocsEnum, bool?> enums = new IdentityHashMap<DocsEnum, bool?>();
		  MatchNoBits bits = new MatchNoBits(firstReader.maxDoc());
		  iterator = terms.iterator(null);
		  DocsEnum docs = null;
		  BytesRef term = null;
		  while ((term = iterator.next()) != null)
		  {
			docs = iterator.docs(null, RandomDocsEnum("body", term, leaves2, bits), random().nextBoolean() ? DocsEnum.FLAG_FREQS : DocsEnum.FLAG_NONE);
			enums.put(docs, true);
		  }
		  Assert.AreEqual(terms.size(), enums.size());

		  iterator = terms.iterator(null);
		  enums.clear();
		  docs = null;
		  while ((term = iterator.next()) != null)
		  {
			docs = iterator.docs(bits, RandomDocsEnum("body", term, leaves2, bits), random().nextBoolean() ? DocsEnum.FLAG_FREQS : DocsEnum.FLAG_NONE);
			enums.put(docs, true);
		  }
		  Assert.AreEqual(terms.size(), enums.size());
		}
		IOUtils.close(writer, firstReader, secondReader, dir);
	  }

	  public virtual DocsEnum RandomDocsEnum(string field, BytesRef term, IList<AtomicReaderContext> readers, Bits bits)
	  {
		if (random().Next(10) == 0)
		{
		  return null;
		}
		AtomicReader indexReader = readers[random().Next(readers.Count)].reader();
		Terms terms = indexReader.terms(field);
		if (terms == null)
		{
		  return null;
		}
		TermsEnum iterator = terms.iterator(null);
		if (iterator.seekExact(term))
		{
		  return iterator.docs(bits, null, random().nextBoolean() ? DocsEnum.FLAG_FREQS : DocsEnum.FLAG_NONE);
		}
		return null;
	  }

	  /// <summary>
	  /// populates a writer with random stuff. this must be fully reproducable with
	  /// the seed!
	  /// </summary>
	  public static void CreateRandomIndex(int numdocs, RandomIndexWriter writer, Random random)
	  {
		LineFileDocs lineFileDocs = new LineFileDocs(random);

		for (int i = 0; i < numdocs; i++)
		{
		  writer.addDocument(lineFileDocs.nextDoc());
		}

		lineFileDocs.close();
	  }

	}

}