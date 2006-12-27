/*
 * Copyright 2004 The Apache Software Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using SimpleAnalyzer = Lucene.Net.Analysis.SimpleAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using Lucene.Net.Index;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using Pattern = System.Text.RegularExpressions.Regex;
using NUnit.Framework;

namespace Lucene.Net.Search
{
	
	/// <summary> Unit tests for sorting code.
	/// 
	/// <p>Created: Feb 17, 2004 4:55:10 PM
	/// 
	/// </summary>
	/// <author>   Tim Jones (Nacimiento Software)
	/// </author>
	/// <since>   lucene 1.4
	/// </since>
	/// <version>  $Id: TestSort.java 332651 2005-11-11 21:19:02Z yonik $
	/// </version>
	
	[Serializable]
	[TestFixture]
    public class TestSort
	{
		[Serializable]
		private class AnonymousClassFilter : Filter
		{
			public AnonymousClassFilter(Lucene.Net.Search.TopDocs docs1, TestSort enclosingInstance)
			{
				InitBlock(docs1, enclosingInstance);
			}
			private void  InitBlock(Lucene.Net.Search.TopDocs docs1, TestSort enclosingInstance)
			{
				this.docs1 = docs1;
				this.enclosingInstance = enclosingInstance;
			}

            private Lucene.Net.Search.TopDocs docs1;
			private TestSort enclosingInstance;

            public TestSort Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}

            public override System.Collections.BitArray Bits(IndexReader reader)
			{
				System.Collections.BitArray bs = new System.Collections.BitArray((reader.MaxDoc() % 64 == 0?reader.MaxDoc() / 64:reader.MaxDoc() / 64 + 1) * 64);
				bs.Set(docs1.scoreDocs[0].doc, true);
				return bs;
			}
		}
		
		private Searcher full;
		private Searcher searchX;
		private Searcher searchY;
		private Query queryX;
		private Query queryY;
		private Query queryA;
		private Query queryE;
		private Query queryF;
		private Query queryG;
		private Sort sort;
		
		
		[STAThread]
		public static void  Main(System.String[] argv)
		{
			System.Runtime.Remoting.RemotingConfiguration.Configure("Lucene.Net.Search.TestSort.config");
			System.Runtime.Remoting.Channels.ChannelServices.RegisterChannel(new System.Runtime.Remoting.Channels.Http.HttpChannel(8080));
            if (argv == null || argv.Length < 1)
            {
                // NUnit.Core.TestRunner.Run(Suite());    // {{Aroush-1.9}} where is "Run" in NUnit?
            }
            else if ("server".Equals(argv[0]))
            {
                TestSort test = new TestSort();
                try
                {
                    test.StartServer();
                    System.Threading.Thread.Sleep(new System.TimeSpan((System.Int64) 10000 * 500000));
                }
                catch (System.Exception e)
                {
                    System.Console.Out.WriteLine(e);
                    System.Console.Error.WriteLine(e.StackTrace);
                }
            }

            System.Console.ReadLine();
		}
		
		public static NUnit.Framework.TestCase Suite()
		{
			return null; // return new NUnit.Core.TestSuite(typeof(TestSort)); {{Aroush-1.9}} how do you do this in NUnit?
		}
		
		
        // document data:
        // the tracer field is used to determine which document was hit
        // the contents field is used to search and sort by relevance
        // the int field to sort by int
        // the float field to sort by float
        // the string field to sort by string
        // the i18n field includes accented characters for testing locale-specific sorting
        private System.String[][] data = new System.String[][]{
                               // tracer  contents         int            float           string   custom   i18n
            new System.String[]{   "A",   "x a",           "5",           "4f",           "c",     "A-3",   "p\u00EAche"},
            new System.String[]{   "B",   "y a",           "5",           "3.4028235E38", "i",     "B-10",  "HAT"},
            new System.String[]{   "C",   "x a b c",       "2147483647",  "1.0",          "j",     "A-2",   "p\u00E9ch\u00E9"},
            new System.String[]{   "D",   "y a b c",       "-1",          "0.0f",         "a",     "C-0",   "HUT"},
            new System.String[]{   "E",   "x a b c d",     "5",           "2f",           "h",     "B-8",   "peach"},
            new System.String[]{   "F",   "y a b c d",     "2",           "3.14159f",     "g",     "B-1",   "H\u00C5T"},
            new System.String[]{   "G",   "x a b c d",     "3",           "-1.0",         "f",     "C-100", "sin"},
            new System.String[]{   "H",   "y a b c d",     "0",           "1.4E-45",      "e",     "C-88",  "H\u00D8T"},
            new System.String[]{   "I",   "x a b c d e f", "-2147483648", "1.0e+0",       "d",     "A-10",  "s\u00EDn"},
            new System.String[]{   "J",   "y a b c d e f", "4",           ".5",           "b",     "C-7",   "HOT"},
            new System.String[]{   "W",   "g",             "1",           null,           null,    null,    null},
            new System.String[]{   "X",   "g",             "1",           "0.1",          null,    null,    null},
            new System.String[]{   "Y",   "g",             "1",           "0.2",          null,    null,    null},
            new System.String[]{   "Z",   "f g",           null,          null,           null,    null,    null}};

		
		// create an index of all the documents, or just the x, or just the y documents
		private Searcher GetIndex(bool even, bool odd)
		{
			RAMDirectory indexStore = new RAMDirectory();
			IndexWriter writer = new IndexWriter(indexStore, new SimpleAnalyzer(), true);
			for (int i = 0; i < data.Length; ++i)
			{
				if (((i % 2) == 0 && even) || ((i % 2) == 1 && odd))
				{
					Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
					doc.Add(new Field("tracer", data[i][0], Field.Store.YES, Field.Index.NO));
					doc.Add(new Field("contents", data[i][1], Field.Store.NO, Field.Index.TOKENIZED));
					if (data[i][2] != null)
						doc.Add(new Field("int", data[i][2], Field.Store.NO, Field.Index.UN_TOKENIZED));
					if (data[i][3] != null)
						doc.Add(new Field("float", data[i][3], Field.Store.NO, Field.Index.UN_TOKENIZED));
					if (data[i][4] != null)
						doc.Add(new Field("string", data[i][4], Field.Store.NO, Field.Index.UN_TOKENIZED));
					if (data[i][5] != null)
						doc.Add(new Field("custom", data[i][5], Field.Store.NO, Field.Index.UN_TOKENIZED));
                    if (data[i][6] != null)
                        doc.Add(new Field("i18n", data[i][6], Field.Store.NO, Field.Index.UN_TOKENIZED));
                    doc.SetBoost(2); // produce some scores above 1.0
					writer.AddDocument(doc);
				}
			}
			writer.Optimize();
			writer.Close();
			return new IndexSearcher(indexStore);
		}
		
		private Searcher GetFullIndex()
		{
			return GetIndex(true, true);
		}
		
		private Searcher GetXIndex()
		{
			return GetIndex(true, false);
		}
		
		private Searcher GetYIndex()
		{
			return GetIndex(false, true);
		}
		
		private Searcher GetEmptyIndex()
		{
			return GetIndex(false, false);
		}
		
		[SetUp]
        public virtual void  SetUp()
		{
			full = GetFullIndex();
			searchX = GetXIndex();
			searchY = GetYIndex();
			queryX = new TermQuery(new Term("contents", "x"));
			queryY = new TermQuery(new Term("contents", "y"));
			queryA = new TermQuery(new Term("contents", "a"));
			queryE = new TermQuery(new Term("contents", "e"));
			queryF = new TermQuery(new Term("contents", "f"));
			queryG = new TermQuery(new Term("contents", "g"));
			sort = new Sort();
		}
		
		// test the sorts by score and document number
		[Test]
        public virtual void  TestBuiltInSorts()
		{
			sort = new Sort();
			AssertMatches(full, queryX, sort, "ACEGI");
			AssertMatches(full, queryY, sort, "BDFHJ");
			
			sort.SetSort(SortField.FIELD_DOC);
			AssertMatches(full, queryX, sort, "ACEGI");
			AssertMatches(full, queryY, sort, "BDFHJ");
		}
		
		// test sorts where the type of field is specified
		[Test]
        public virtual void  TestTypedSort()
		{
			sort.SetSort(new SortField[]{new SortField("int", SortField.INT), SortField.FIELD_DOC});
			AssertMatches(full, queryX, sort, "IGAEC");
			AssertMatches(full, queryY, sort, "DHFJB");
			
			sort.SetSort(new SortField[]{new SortField("float", SortField.FLOAT), SortField.FIELD_DOC});
			AssertMatches(full, queryX, sort, "GCIEA");
			AssertMatches(full, queryY, sort, "DHJFB");
			
			sort.SetSort(new SortField[]{new SortField("string", SortField.STRING), SortField.FIELD_DOC});
			AssertMatches(full, queryX, sort, "AIGEC");
			AssertMatches(full, queryY, sort, "DJHFB");
		}
		
		// test sorts when there's nothing in the index
		[Test]
        public virtual void  TestEmptyIndex()
		{
			Searcher empty = GetEmptyIndex();
			
			sort = new Sort();
			AssertMatches(empty, queryX, sort, "");
			
			sort.SetSort(SortField.FIELD_DOC);
			AssertMatches(empty, queryX, sort, "");
			
			sort.SetSort(new SortField[]{new SortField("int", SortField.INT), SortField.FIELD_DOC});
			AssertMatches(empty, queryX, sort, "");
			
			sort.SetSort(new SortField[]{new SortField("string", SortField.STRING, true), SortField.FIELD_DOC});
			AssertMatches(empty, queryX, sort, "");
			
			sort.SetSort(new SortField[]{new SortField("float", SortField.FLOAT), new SortField("string", SortField.STRING)});
			AssertMatches(empty, queryX, sort, "");
		}
		
		// test sorts where the type of field is determined dynamically
		[Test]
        public virtual void  TestAutoSort()
		{
			sort.SetSort("int");
			AssertMatches(full, queryX, sort, "IGAEC");
			AssertMatches(full, queryY, sort, "DHFJB");
			
			sort.SetSort("float");
			AssertMatches(full, queryX, sort, "GCIEA");
			AssertMatches(full, queryY, sort, "DHJFB");
			
			sort.SetSort("string");
			AssertMatches(full, queryX, sort, "AIGEC");
			AssertMatches(full, queryY, sort, "DJHFB");
		}
		
		// test sorts in reverse
		[Test]
        public virtual void  TestReverseSort()
		{
			sort.SetSort(new SortField[]{new SortField(null, SortField.SCORE, true), SortField.FIELD_DOC});
			AssertMatches(full, queryX, sort, "IEGCA");
			AssertMatches(full, queryY, sort, "JFHDB");
			
			sort.SetSort(new SortField(null, SortField.DOC, true));
			AssertMatches(full, queryX, sort, "IGECA");
			AssertMatches(full, queryY, sort, "JHFDB");
			
			sort.SetSort("int", true);
			AssertMatches(full, queryX, sort, "CAEGI");
			AssertMatches(full, queryY, sort, "BJFHD");
			
			sort.SetSort("float", true);
			AssertMatches(full, queryX, sort, "AECIG");
			AssertMatches(full, queryY, sort, "BFJHD");
			
			sort.SetSort("string", true);
			AssertMatches(full, queryX, sort, "CEGIA");
			AssertMatches(full, queryY, sort, "BFHJD");
		}
		
		// test sorting when the sort field is empty (undefined) for some of the documents
		[Test]
        public virtual void  TestEmptyFieldSort()
		{
			sort.SetSort("string");
			AssertMatches(full, queryF, sort, "ZJI");
			
			sort.SetSort("string", true);
			AssertMatches(full, queryF, sort, "IJZ");
			
			sort.SetSort("int");
			AssertMatches(full, queryF, sort, "IZJ");
			
			sort.SetSort("int", true);
			AssertMatches(full, queryF, sort, "JZI");
			
			sort.SetSort("float");
			AssertMatches(full, queryF, sort, "ZJI");
			
			// using a nonexisting field as first sort key shouldn't make a difference:
			sort.SetSort(new SortField[]{new SortField("nosuchfield", SortField.STRING), new SortField("float")});
			AssertMatches(full, queryF, sort, "ZJI");
			
			sort.SetSort("float", true);
			AssertMatches(full, queryF, sort, "IJZ");
			
			// When a field is null for both documents, the next SortField should be used.
			// Works for
			sort.SetSort(new SortField[]{new SortField("int"), new SortField("string", SortField.STRING), new SortField("float")});
			AssertMatches(full, queryG, sort, "ZWXY");
			
			// Reverse the last criterium to make sure the test didn't pass by chance
			sort.SetSort(new SortField[]{new SortField("int"), new SortField("string", SortField.STRING), new SortField("float", true)});
			AssertMatches(full, queryG, sort, "ZYXW");
			
			// Do the same for a MultiSearcher
			Searcher multiSearcher = new MultiSearcher(new Lucene.Net.Search.Searchable[]{full});
			
			sort.SetSort(new SortField[]{new SortField("int"), new SortField("string", SortField.STRING), new SortField("float")});
			AssertMatches(multiSearcher, queryG, sort, "ZWXY");
			
			sort.SetSort(new SortField[]{new SortField("int"), new SortField("string", SortField.STRING), new SortField("float", true)});
			AssertMatches(multiSearcher, queryG, sort, "ZYXW");
			// Don't close the multiSearcher. it would close the full searcher too!
			
			// Do the same for a ParallelMultiSearcher
			Searcher parallelSearcher = new ParallelMultiSearcher(new Lucene.Net.Search.Searchable[]{full});
			
			sort.SetSort(new SortField[]{new SortField("int"), new SortField("string", SortField.STRING), new SortField("float")});
			AssertMatches(parallelSearcher, queryG, sort, "ZWXY");
			
			sort.SetSort(new SortField[]{new SortField("int"), new SortField("string", SortField.STRING), new SortField("float", true)});
			AssertMatches(parallelSearcher, queryG, sort, "ZYXW");
			// Don't close the parallelSearcher. it would close the full searcher too!
		}
		
		// test sorts using a series of fields
		[Test]
        public virtual void  TestSortCombos()
		{
			sort.SetSort(new System.String[]{"int", "float"});
			AssertMatches(full, queryX, sort, "IGEAC");
			
			sort.SetSort(new SortField[]{new SortField("int", true), new SortField(null, SortField.DOC, true)});
			AssertMatches(full, queryX, sort, "CEAGI");
			
			sort.SetSort(new System.String[]{"float", "string"});
			AssertMatches(full, queryX, sort, "GICEA");
		}
		
		// test using a Locale for sorting strings
		[Test]
        public virtual void  TestLocaleSort()
		{
			sort.SetSort(new SortField[]{new SortField("string", new System.Globalization.CultureInfo("en-US"))});
			AssertMatches(full, queryX, sort, "AIGEC");
			AssertMatches(full, queryY, sort, "DJHFB");
			
			sort.SetSort(new SortField[]{new SortField("string", new System.Globalization.CultureInfo("en-US"), true)});
			AssertMatches(full, queryX, sort, "CEGIA");
			AssertMatches(full, queryY, sort, "BFHJD");
		}
		
        // test using various international locales with accented characters
        // (which sort differently depending on locale)
        [Test]
        public virtual void  TestInternationalSort()
        {
            sort.SetSort(new SortField("i18n", new System.Globalization.CultureInfo("en-US")));
            AssertMatches(full, queryY, sort, "BFJHD");     // NOTE: this is "BFJDH" in Java's version
			
            sort.SetSort(new SortField("i18n", new System.Globalization.CultureInfo("sv" + "-" + "se")));
            AssertMatches(full, queryY, sort, "BJDFH");
			
            sort.SetSort(new SortField("i18n", new System.Globalization.CultureInfo("da" + "-" + "dk")));
            AssertMatches(full, queryY, sort, "BJDHF");
			
            sort.SetSort(new SortField("i18n", new System.Globalization.CultureInfo("en-US")));
            AssertMatches(full, queryX, sort, "ECAGI");
			
            sort.SetSort(new SortField("i18n", new System.Globalization.CultureInfo("fr-FR")));
            AssertMatches(full, queryX, sort, "EACGI");
        }
		
        // Test the MultiSearcher's ability to preserve locale-sensitive ordering
        // by wrapping it around a single searcher
        [Test]
        public virtual void  TestInternationalMultiSearcherSort()
        {
            Searcher multiSearcher = new MultiSearcher(new Lucene.Net.Search.Searchable[]{full});
			
            sort.SetSort(new SortField("i18n", new System.Globalization.CultureInfo("sv" + "-" + "se")));
            AssertMatches(multiSearcher, queryY, sort, "BJDFH");
			
            sort.SetSort(new SortField("i18n", new System.Globalization.CultureInfo("en-US")));
            AssertMatches(multiSearcher, queryY, sort, "BFJHD");    // NOTE: this is "BFJDH" in Java's version
			
            sort.SetSort(new SortField("i18n", new System.Globalization.CultureInfo("da" + "-" + "dk")));
            AssertMatches(multiSearcher, queryY, sort, "BJDHF");
        }
		
        // test a custom sort function
		[Test]
        public virtual void  TestCustomSorts()
		{
			sort.SetSort(new SortField("custom", SampleComparable.GetComparatorSource()));
			AssertMatches(full, queryX, sort, "CAIEG");
			sort.SetSort(new SortField("custom", SampleComparable.GetComparatorSource(), true));
			AssertMatches(full, queryY, sort, "HJDBF");
			SortComparator custom = SampleComparable.GetComparator();
			sort.SetSort(new SortField("custom", custom));
			AssertMatches(full, queryX, sort, "CAIEG");
			sort.SetSort(new SortField("custom", custom, true));
			AssertMatches(full, queryY, sort, "HJDBF");
		}
		
		// test a variety of sorts using more than one searcher
		[Test]
        public virtual void  TestMultiSort()
		{
			MultiSearcher searcher = new MultiSearcher(new Lucene.Net.Search.Searchable[]{searchX, searchY});
			RunMultiSorts(searcher);
		}
		
		// test a variety of sorts using a parallel multisearcher
		[Test]
        public virtual void  TestParallelMultiSort()
		{
			Searcher searcher = new ParallelMultiSearcher(new Lucene.Net.Search.Searchable[]{searchX, searchY});
			RunMultiSorts(searcher);
		}
		
		// test a variety of sorts using a remote searcher
		[Test]
        public virtual void  TestRemoteSort()
		{
			Lucene.Net.Search.Searchable searcher = GetRemote();
			MultiSearcher multi = new MultiSearcher(new Lucene.Net.Search.Searchable[]{searcher});
			RunMultiSorts(multi);
		}
		
		// test custom search when remote
		[Test]
        public virtual void  TestRemoteCustomSort()
		{
			Lucene.Net.Search.Searchable searcher = GetRemote();
			MultiSearcher multi = new MultiSearcher(new Lucene.Net.Search.Searchable[]{searcher});
			sort.SetSort(new SortField("custom", SampleComparable.GetComparatorSource()));
			AssertMatches(multi, queryX, sort, "CAIEG");
			sort.SetSort(new SortField("custom", SampleComparable.GetComparatorSource(), true));
			AssertMatches(multi, queryY, sort, "HJDBF");
			SortComparator custom = SampleComparable.GetComparator();
			sort.SetSort(new SortField("custom", custom));
			AssertMatches(multi, queryX, sort, "CAIEG");
			sort.SetSort(new SortField("custom", custom, true));
			AssertMatches(multi, queryY, sort, "HJDBF");
		}
		
		// test that the relevancy scores are the same even if
		// hits are sorted
		[Test]
        public virtual void  TestNormalizedScores()
		{
			
			// capture relevancy scores
			System.Collections.Hashtable scoresX = GetScores(full.Search(queryX));
			System.Collections.Hashtable scoresY = GetScores(full.Search(queryY));
			System.Collections.Hashtable scoresA = GetScores(full.Search(queryA));
			
			// we'll test searching locally, remote and multi
			MultiSearcher remote = new MultiSearcher(new Lucene.Net.Search.Searchable[]{GetRemote()});
			MultiSearcher multi = new MultiSearcher(new Lucene.Net.Search.Searchable[]{searchX, searchY});
			
			// change sorting and make sure relevancy stays the same
			
			sort = new Sort();
			AssertSameValues(scoresX, GetScores(full.Search(queryX, sort)));
			AssertSameValues(scoresX, GetScores(remote.Search(queryX, sort)));
			AssertSameValues(scoresX, GetScores(multi.Search(queryX, sort)));
			AssertSameValues(scoresY, GetScores(full.Search(queryY, sort)));
			AssertSameValues(scoresY, GetScores(remote.Search(queryY, sort)));
			AssertSameValues(scoresY, GetScores(multi.Search(queryY, sort)));
			AssertSameValues(scoresA, GetScores(full.Search(queryA, sort)));
			AssertSameValues(scoresA, GetScores(remote.Search(queryA, sort)));
			AssertSameValues(scoresA, GetScores(multi.Search(queryA, sort)));
			
			sort.SetSort(SortField.FIELD_DOC);
			AssertSameValues(scoresX, GetScores(full.Search(queryX, sort)));
			AssertSameValues(scoresX, GetScores(remote.Search(queryX, sort)));
			AssertSameValues(scoresX, GetScores(multi.Search(queryX, sort)));
			AssertSameValues(scoresY, GetScores(full.Search(queryY, sort)));
			AssertSameValues(scoresY, GetScores(remote.Search(queryY, sort)));
			AssertSameValues(scoresY, GetScores(multi.Search(queryY, sort)));
			AssertSameValues(scoresA, GetScores(full.Search(queryA, sort)));
			AssertSameValues(scoresA, GetScores(remote.Search(queryA, sort)));
			AssertSameValues(scoresA, GetScores(multi.Search(queryA, sort)));
			
			sort.SetSort("int");
			AssertSameValues(scoresX, GetScores(full.Search(queryX, sort)));
			AssertSameValues(scoresX, GetScores(remote.Search(queryX, sort)));
			AssertSameValues(scoresX, GetScores(multi.Search(queryX, sort)));
			AssertSameValues(scoresY, GetScores(full.Search(queryY, sort)));
			AssertSameValues(scoresY, GetScores(remote.Search(queryY, sort)));
			AssertSameValues(scoresY, GetScores(multi.Search(queryY, sort)));
			AssertSameValues(scoresA, GetScores(full.Search(queryA, sort)));
			AssertSameValues(scoresA, GetScores(remote.Search(queryA, sort)));
			AssertSameValues(scoresA, GetScores(multi.Search(queryA, sort)));
			
			sort.SetSort("float");
			AssertSameValues(scoresX, GetScores(full.Search(queryX, sort)));
			AssertSameValues(scoresX, GetScores(remote.Search(queryX, sort)));
			AssertSameValues(scoresX, GetScores(multi.Search(queryX, sort)));
			AssertSameValues(scoresY, GetScores(full.Search(queryY, sort)));
			AssertSameValues(scoresY, GetScores(remote.Search(queryY, sort)));
			AssertSameValues(scoresY, GetScores(multi.Search(queryY, sort)));
			AssertSameValues(scoresA, GetScores(full.Search(queryA, sort)));
			AssertSameValues(scoresA, GetScores(remote.Search(queryA, sort)));
			AssertSameValues(scoresA, GetScores(multi.Search(queryA, sort)));
			
			sort.SetSort("string");
			AssertSameValues(scoresX, GetScores(full.Search(queryX, sort)));
			AssertSameValues(scoresX, GetScores(remote.Search(queryX, sort)));
			AssertSameValues(scoresX, GetScores(multi.Search(queryX, sort)));
			AssertSameValues(scoresY, GetScores(full.Search(queryY, sort)));
			AssertSameValues(scoresY, GetScores(remote.Search(queryY, sort)));
			AssertSameValues(scoresY, GetScores(multi.Search(queryY, sort)));
			AssertSameValues(scoresA, GetScores(full.Search(queryA, sort)));
			AssertSameValues(scoresA, GetScores(remote.Search(queryA, sort)));
			AssertSameValues(scoresA, GetScores(multi.Search(queryA, sort)));
			
			sort.SetSort(new System.String[]{"int", "float"});
			AssertSameValues(scoresX, GetScores(full.Search(queryX, sort)));
			AssertSameValues(scoresX, GetScores(remote.Search(queryX, sort)));
			AssertSameValues(scoresX, GetScores(multi.Search(queryX, sort)));
			AssertSameValues(scoresY, GetScores(full.Search(queryY, sort)));
			AssertSameValues(scoresY, GetScores(remote.Search(queryY, sort)));
			AssertSameValues(scoresY, GetScores(multi.Search(queryY, sort)));
			AssertSameValues(scoresA, GetScores(full.Search(queryA, sort)));
			AssertSameValues(scoresA, GetScores(remote.Search(queryA, sort)));
			AssertSameValues(scoresA, GetScores(multi.Search(queryA, sort)));
			
			sort.SetSort(new SortField[]{new SortField("int", true), new SortField(null, SortField.DOC, true)});
			AssertSameValues(scoresX, GetScores(full.Search(queryX, sort)));
			AssertSameValues(scoresX, GetScores(remote.Search(queryX, sort)));
			AssertSameValues(scoresX, GetScores(multi.Search(queryX, sort)));
			AssertSameValues(scoresY, GetScores(full.Search(queryY, sort)));
			AssertSameValues(scoresY, GetScores(remote.Search(queryY, sort)));
			AssertSameValues(scoresY, GetScores(multi.Search(queryY, sort)));
			AssertSameValues(scoresA, GetScores(full.Search(queryA, sort)));
			AssertSameValues(scoresA, GetScores(remote.Search(queryA, sort)));
			AssertSameValues(scoresA, GetScores(multi.Search(queryA, sort)));
			
			sort.SetSort(new System.String[]{"float", "string"});
			AssertSameValues(scoresX, GetScores(full.Search(queryX, sort)));
			AssertSameValues(scoresX, GetScores(remote.Search(queryX, sort)));
			AssertSameValues(scoresX, GetScores(multi.Search(queryX, sort)));
			AssertSameValues(scoresY, GetScores(full.Search(queryY, sort)));
			AssertSameValues(scoresY, GetScores(remote.Search(queryY, sort)));
			AssertSameValues(scoresY, GetScores(multi.Search(queryY, sort)));
			AssertSameValues(scoresA, GetScores(full.Search(queryA, sort)));
			AssertSameValues(scoresA, GetScores(remote.Search(queryA, sort)));
			AssertSameValues(scoresA, GetScores(multi.Search(queryA, sort)));
		}
		
        [Test]
		public virtual void  TestTopDocsScores()
		{
			
			// There was previously a bug in FieldSortedHitQueue.maxscore when only a single
			// doc was added.  That is what the following tests for.
			Sort sort = new Sort();
			int nDocs = 10;
			
			// try to pick a query that will result in an unnormalized
			// score greater than 1 to test for correct normalization
			TopDocs docs1 = full.Search(queryE, null, nDocs, sort);
			
			// a filter that only allows through the first hit
			Filter filt = new AnonymousClassFilter(docs1, this);
			
			TopDocs docs2 = full.Search(queryE, filt, nDocs, sort);
			
			Assert.AreEqual(docs1.scoreDocs[0].score, docs2.scoreDocs[0].score, 1e-6);
		}
		
		
		// runs a variety of sorts useful for multisearchers
		private void  RunMultiSorts(Searcher multi)
		{
			sort.SetSort(SortField.FIELD_DOC);
			AssertMatchesPattern(multi, queryA, sort, "[AB]{2}[CD]{2}[EF]{2}[GH]{2}[IJ]{2}");
			
			sort.SetSort(new SortField("int", SortField.INT));
			AssertMatchesPattern(multi, queryA, sort, "IDHFGJ[ABE]{3}C");
			
			sort.SetSort(new SortField[]{new SortField("int", SortField.INT), SortField.FIELD_DOC});
			AssertMatchesPattern(multi, queryA, sort, "IDHFGJ[AB]{2}EC");
			
			sort.SetSort("int");
			AssertMatchesPattern(multi, queryA, sort, "IDHFGJ[AB]{2}EC");
			
			sort.SetSort(new SortField[]{new SortField("float", SortField.FLOAT), SortField.FIELD_DOC});
			AssertMatchesPattern(multi, queryA, sort, "GDHJ[CI]{2}EFAB");
			
			sort.SetSort("float");
			AssertMatchesPattern(multi, queryA, sort, "GDHJ[CI]{2}EFAB");
			
			sort.SetSort("string");
			AssertMatches(multi, queryA, sort, "DJAIHGFEBC");
			
			sort.SetSort("int", true);
			AssertMatchesPattern(multi, queryA, sort, "C[AB]{2}EJGFHDI");
			
			sort.SetSort("float", true);
			AssertMatchesPattern(multi, queryA, sort, "BAFE[IC]{2}JHDG");
			
			sort.SetSort("string", true);
			AssertMatches(multi, queryA, sort, "CBEFGHIAJD");
			
			sort.SetSort(new SortField[]{new SortField("string", new System.Globalization.CultureInfo("en-US"))});
			AssertMatches(multi, queryA, sort, "DJAIHGFEBC");
			
			sort.SetSort(new SortField[]{new SortField("string", new System.Globalization.CultureInfo("en-US"), true)});
			AssertMatches(multi, queryA, sort, "CBEFGHIAJD");
			
			sort.SetSort(new System.String[]{"int", "float"});
			AssertMatches(multi, queryA, sort, "IDHFGJEABC");
			
			sort.SetSort(new System.String[]{"float", "string"});
			AssertMatches(multi, queryA, sort, "GDHJICEFAB");
			
			sort.SetSort("int");
			AssertMatches(multi, queryF, sort, "IZJ");
			
			sort.SetSort("int", true);
			AssertMatches(multi, queryF, sort, "JZI");
			
			sort.SetSort("float");
			AssertMatches(multi, queryF, sort, "ZJI");
			
			sort.SetSort("string");
			AssertMatches(multi, queryF, sort, "ZJI");
			
			sort.SetSort("string", true);
			AssertMatches(multi, queryF, sort, "IJZ");
		}
		
		// make sure the documents returned by the search match the expected list
		private void  AssertMatches(Searcher searcher, Query query, Sort sort, System.String expectedResult)
		{
			Hits result = searcher.Search(query, sort);
			System.Text.StringBuilder buff = new System.Text.StringBuilder(10);
			int n = result.Length();
			for (int i = 0; i < n; ++i)
			{
				Lucene.Net.Documents.Document doc = result.Doc(i);
				System.String[] v = doc.GetValues("tracer");
				for (int j = 0; j < v.Length; ++j)
				{
					buff.Append(v[j]);
				}
			}
			Assert.AreEqual(expectedResult, buff.ToString());
		}
		
		// make sure the documents returned by the search match the expected list pattern
		private void  AssertMatchesPattern(Searcher searcher, Query query, Sort sort, System.String pattern)
		{
			Hits result = searcher.Search(query, sort);
			System.Text.StringBuilder buff = new System.Text.StringBuilder(10);
			int n = result.Length();
			for (int i = 0; i < n; ++i)
			{
				Lucene.Net.Documents.Document doc = result.Doc(i);
				System.String[] v = doc.GetValues("tracer");
				for (int j = 0; j < v.Length; ++j)
				{
					buff.Append(v[j]);
				}
			}
			// System.out.println ("matching \""+buff+"\" against pattern \""+pattern+"\"");
            Pattern p = new System.Text.RegularExpressions.Regex(pattern);
			Assert.IsTrue(p.Match(buff.ToString()).Success);
		}
		
		private System.Collections.Hashtable GetScores(Hits hits)
		{
			System.Collections.Hashtable scoreMap = new System.Collections.Hashtable();
			int n = hits.Length();
			for (int i = 0; i < n; ++i)
			{
				Lucene.Net.Documents.Document doc = hits.Doc(i);
				System.String[] v = doc.GetValues("tracer");
				Assert.AreEqual(v.Length, 1);
				scoreMap[v[0]] = (float) hits.Score(i);
			}
			return scoreMap;
		}
		
		// make sure all the values in the maps match
		private void  AssertSameValues(System.Collections.Hashtable m1, System.Collections.Hashtable m2)
		{
			int n = m1.Count;
			int m = m2.Count;
			Assert.AreEqual(n, m);
			System.Collections.IEnumerator iter = new System.Collections.Hashtable().GetEnumerator();
			while (iter.MoveNext())
			{
				System.Object key = iter.Current;
				System.Object o1 = m1[key];
				System.Object o2 = m2[key];
				if (o1 is System.Single)
				{
					Assert.AreEqual((float) ((System.Single) o1), (float) ((System.Single) o2), 1e-6);
				}
				else
				{
					Assert.AreEqual(m1[key], m2[key]);
				}
			}
		}
		
		private Lucene.Net.Search.Searchable GetRemote()
		{
			try
			{
				return LookupRemote();
			}
			catch (System.Exception e)
			{
				StartServer();
				return LookupRemote();
			}
		}
		
		private Lucene.Net.Search.Searchable LookupRemote()
		{
			return (Lucene.Net.Search.Searchable) Activator.GetObject(typeof(Lucene.Net.Search.Searchable), "http://localhost/SortedSearchable");
		}
		
		private void  StartServer()
		{
			// construct an index
			Searcher local = GetFullIndex();
			// local.search (queryA, new Sort());
			
			// publish it
			//System.Runtime.Remoting.RemotingConfiguration reg = LocateRegistry.createRegistry(1099);
			//RemoteSearchable impl = new RemoteSearchable(local);
			//System.Runtime.Remoting.RemotingServices.Marshal(impl, SupportClass.ParseURIBind("//localhost/SortedSearchable"));
            Assert.Fail("Need to port Java to C#");     // {{Aroush-1.9}} We need to do this in C#
		}
	}
}