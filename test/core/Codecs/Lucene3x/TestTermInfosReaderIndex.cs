using System;
using System.Collections.Generic;

namespace Lucene.Net.Codecs.Lucene3x
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
	using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using StringField = Lucene.Net.Document.StringField;
	using CorruptIndexException = Lucene.Net.Index.CorruptIndexException;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using FieldInfos = Lucene.Net.Index.FieldInfos;
	using Fields = Lucene.Net.Index.Fields;
	using IndexFileNames = Lucene.Net.Index.IndexFileNames;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using LogMergePolicy = Lucene.Net.Index.LogMergePolicy;
	using MultiFields = Lucene.Net.Index.MultiFields;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using SegmentReader = Lucene.Net.Index.SegmentReader;
	using Term = Lucene.Net.Index.Term;
	using Terms = Lucene.Net.Index.Terms;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using TermQuery = Lucene.Net.Search.TermQuery;
	using TopDocs = Lucene.Net.Search.TopDocs;
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using IndexInput = Lucene.Net.Store.IndexInput;
	using LockObtainFailedException = Lucene.Net.Store.LockObtainFailedException;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using AfterClass = org.junit.AfterClass;
	using BeforeClass = org.junit.BeforeClass;

	public class TestTermInfosReaderIndex : LuceneTestCase
	{

	  private static int NUMBER_OF_DOCUMENTS;
	  private static int NUMBER_OF_FIELDS;
	  private static TermInfosReaderIndex Index;
	  private static Directory Directory;
	  private static SegmentTermEnum TermEnum;
	  private static int IndexDivisor;
	  private static int TermIndexInterval;
	  private static IndexReader Reader;
	  private static IList<Term> SampleTerms;

	  /// <summary>
	  /// we will manually instantiate preflex-rw here </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void beforeClass() throws Exception
	  public static void BeforeClass()
	  {
		// NOTE: turn off compound file, this test will open some index files directly.
		LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
		IndexWriterConfig config = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.KEYWORD, false)).setUseCompoundFile(false);

		TermIndexInterval = config.TermIndexInterval;
		IndexDivisor = TestUtil.Next(random(), 1, 10);
		NUMBER_OF_DOCUMENTS = atLeast(100);
		NUMBER_OF_FIELDS = atLeast(Math.Max(10, 3 * TermIndexInterval * IndexDivisor / NUMBER_OF_DOCUMENTS));

		Directory = newDirectory();

		config.Codec = new PreFlexRWCodec();
		LogMergePolicy mp = newLogMergePolicy();
		// NOTE: turn off compound file, this test will open some index files directly.
		mp.NoCFSRatio = 0.0;
		config.MergePolicy = mp;


		Populate(Directory, config);

		DirectoryReader r0 = IndexReader.open(Directory);
		SegmentReader r = LuceneTestCase.getOnlySegmentReader(r0);
		string segment = r.SegmentName;
		r.close();

		FieldInfosReader infosReader = (new PreFlexRWCodec()).fieldInfosFormat().FieldInfosReader;
		FieldInfos fieldInfos = infosReader.read(Directory, segment, "", IOContext.READONCE);
		string segmentFileName = IndexFileNames.segmentFileName(segment, "", Lucene3xPostingsFormat.TERMS_INDEX_EXTENSION);
		long tiiFileLength = Directory.fileLength(segmentFileName);
		IndexInput input = Directory.openInput(segmentFileName, newIOContext(random()));
		TermEnum = new SegmentTermEnum(Directory.openInput(IndexFileNames.segmentFileName(segment, "", Lucene3xPostingsFormat.TERMS_EXTENSION), newIOContext(random())), fieldInfos, false);
		int totalIndexInterval = TermEnum.indexInterval * IndexDivisor;

		SegmentTermEnum indexEnum = new SegmentTermEnum(input, fieldInfos, true);
		Index = new TermInfosReaderIndex(indexEnum, IndexDivisor, tiiFileLength, totalIndexInterval);
		indexEnum.close();
		input.close();

		Reader = IndexReader.open(Directory);
		SampleTerms = Sample(random(),Reader,1000);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @AfterClass public static void afterClass() throws Exception
	  public static void AfterClass()
	  {
		TermEnum.close();
		Reader.close();
		Directory.close();
		TermEnum = null;
		Reader = null;
		Directory = null;
		Index = null;
		SampleTerms = null;
	  }

	  public virtual void TestSeekEnum()
	  {
		int indexPosition = 3;
		SegmentTermEnum clone = TermEnum.clone();
		Term term = FindTermThatWouldBeAtIndex(clone, indexPosition);
		SegmentTermEnum enumerator = clone;
		Index.seekEnum(enumerator, indexPosition);
		Assert.AreEqual(term, enumerator.term());
		clone.close();
	  }

	  public virtual void TestCompareTo()
	  {
		Term term = new Term("field" + random().Next(NUMBER_OF_FIELDS),Text);
		for (int i = 0; i < Index.length(); i++)
		{
		  Term t = Index.getTerm(i);
		  int compareTo = term.compareTo(t);
		  Assert.AreEqual(compareTo, Index.compareTo(term, i));
		}
	  }

	  public virtual void TestRandomSearchPerformance()
	  {
		IndexSearcher searcher = new IndexSearcher(Reader);
		foreach (Term t in SampleTerms)
		{
		  TermQuery query = new TermQuery(t);
		  TopDocs topDocs = searcher.search(query, 10);
		  Assert.IsTrue(topDocs.totalHits > 0);
		}
	  }

	  private static IList<Term> Sample(Random random, IndexReader reader, int size)
	  {
		IList<Term> sample = new List<Term>();
		Fields fields = MultiFields.getFields(reader);
		foreach (string field in fields)
		{
		  Terms terms = fields.terms(field);
		  Assert.IsNotNull(terms);
		  TermsEnum termsEnum = terms.iterator(null);
		  while (termsEnum.next() != null)
		  {
			if (sample.Count >= size)
			{
			  int pos = random.Next(size);
			  sample[pos] = new Term(field, termsEnum.term());
			}
			else
			{
			  sample.Add(new Term(field, termsEnum.term()));
			}
		  }
		}
		Collections.shuffle(sample);
		return sample;
	  }

	  private Term FindTermThatWouldBeAtIndex(SegmentTermEnum termEnum, int index)
	  {
		int termPosition = index * TermIndexInterval * IndexDivisor;
		for (int i = 0; i < termPosition; i++)
		{
		  // TODO: this test just uses random terms, so this is always possible
		  assumeTrue("ran out of terms", termEnum.next());
		}
		Term term = termEnum.term();
		// An indexed term is only written when the term after
		// it exists, so, if the number of terms is 0 mod
		// termIndexInterval, the last index term will not be
		// written; so we require a term after this term
		// as well:
		assumeTrue("ran out of terms", termEnum.next());
		return term;
	  }

	  private static void Populate(Directory directory, IndexWriterConfig config)
	  {
		RandomIndexWriter writer = new RandomIndexWriter(random(), directory, config);
		for (int i = 0; i < NUMBER_OF_DOCUMENTS; i++)
		{
		  Document document = new Document();
		  for (int f = 0; f < NUMBER_OF_FIELDS; f++)
		  {
			document.add(newStringField("field" + f, Text, Field.Store.NO));
		  }
		  writer.addDocument(document);
		}
		writer.forceMerge(1);
		writer.close();
	  }

	  private static string Text
	  {
		  get
		  {
			return Convert.ToString(random().nextLong(),char.MAX_RADIX);
		  }
	  }
	}

}