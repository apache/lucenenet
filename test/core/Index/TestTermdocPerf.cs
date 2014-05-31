using System;

namespace Lucene.Net.Index
{

	/// <summary>
	/// Copyright 2006 The Apache Software Foundation
	/// 
	/// Licensed under the Apache License, Version 2.0 (the "License");
	/// you may not use this file except in compliance with the License.
	/// You may obtain a copy of the License at
	/// 
	///     http://www.apache.org/licenses/LICENSE-2.0
	/// 
	/// Unless required by applicable law or agreed to in writing, software
	/// distributed under the License is distributed on an "AS IS" BASIS,
	/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	/// See the License for the specific language governing permissions and
	/// limitations under the License.
	/// </summary>



	using Analyzer = Lucene.Net.Analysis.Analyzer;
	using Tokenizer = Lucene.Net.Analysis.Tokenizer;
	using CharTermAttribute = Lucene.Net.Analysis.Tokenattributes.CharTermAttribute;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	internal class RepeatingTokenizer : Tokenizer
	{

	  private readonly Random Random;
	  private readonly float PercentDocs;
	  private readonly int MaxTF;
	  private int Num;
	  internal CharTermAttribute TermAtt;
	  internal string Value;

	   public RepeatingTokenizer(Reader reader, string val, Random random, float percentDocs, int maxTF) : base(reader)
	   {
		 this.Value = val;
		 this.Random = random;
		 this.PercentDocs = percentDocs;
		 this.MaxTF = maxTF;
		 this.TermAtt = addAttribute(typeof(CharTermAttribute));
	   }

	   public override bool IncrementToken()
	   {
		 Num--;
		 if (Num >= 0)
		 {
		   ClearAttributes();
		   TermAtt.append(Value);
		   return true;
		 }
		 return false;
	   }

	  public override void Reset()
	  {
		base.reset();
		if (Random.nextFloat() < PercentDocs)
		{
		  Num = Random.Next(MaxTF) + 1;
		}
		else
		{
		  Num = 0;
		}
	  }
	}


	public class TestTermdocPerf : LuceneTestCase
	{

	  internal virtual void AddDocs(Random random, Directory dir, int ndocs, string field, string val, int maxTF, float percentDocs)
	  {

		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this, random, val, maxTF, percentDocs);

		Document doc = new Document();

		doc.add(newStringField(field, val, Field.Store.NO));
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setOpenMode(OpenMode.CREATE).setMaxBufferedDocs(100).setMergePolicy(newLogMergePolicy(100)));

		for (int i = 0; i < ndocs; i++)
		{
		  writer.addDocument(doc);
		}

		writer.forceMerge(1);
		writer.close();
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestTermdocPerf OuterInstance;

		  private Random Random;
		  private string Val;
		  private int MaxTF;
		  private float PercentDocs;

		  public AnalyzerAnonymousInnerClassHelper(TestTermdocPerf outerInstance, Random random, string val, int maxTF, float percentDocs)
		  {
			  this.OuterInstance = outerInstance;
			  this.Random = random;
			  this.Val = val;
			  this.MaxTF = maxTF;
			  this.PercentDocs = percentDocs;
		  }

		  public override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		  {
			return new TokenStreamComponents(new RepeatingTokenizer(reader, Val, Random, PercentDocs, MaxTF));
		  }
	  }


	  public virtual int DoTest(int iter, int ndocs, int maxTF, float percentDocs)
	  {
		Directory dir = newDirectory();

		long start = System.currentTimeMillis();
		AddDocs(random(), dir, ndocs, "foo", "val", maxTF, percentDocs);
		long end = System.currentTimeMillis();
		if (VERBOSE)
		{
			Console.WriteLine("milliseconds for creation of " + ndocs + " docs = " + (end - start));
		}

		IndexReader reader = DirectoryReader.open(dir);

		TermsEnum tenum = MultiFields.getTerms(reader, "foo").iterator(null);

		start = System.currentTimeMillis();

		int ret = 0;
		DocsEnum tdocs = null;
		Random random = new Random(random().nextLong());
		for (int i = 0; i < iter; i++)
		{
		  tenum.seekCeil(new BytesRef("val"));
		  tdocs = TestUtil.docs(random, tenum, MultiFields.getLiveDocs(reader), tdocs, DocsEnum.FLAG_NONE);
		  while (tdocs.nextDoc() != DocIdSetIterator.NO_MORE_DOCS)
		  {
			ret += tdocs.docID();
		  }
		}

		end = System.currentTimeMillis();
		if (VERBOSE)
		{
			Console.WriteLine("milliseconds for " + iter + " TermDocs iteration: " + (end - start));
		}

		return ret;
	  }

	  public virtual void TestTermDocPerf()
	  {
		// performance test for 10% of documents containing a term
		// doTest(100000, 10000,3,.1f);
	  }


	}

}