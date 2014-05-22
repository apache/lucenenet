using System.Collections.Generic;

namespace Lucene.Net.Search.Similarities
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


	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using TextField = Lucene.Net.Document.TextField;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using SpanOrQuery = Lucene.Net.Search.Spans.SpanOrQuery;
	using SpanTermQuery = Lucene.Net.Search.Spans.SpanTermQuery;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	/// <summary>
	/// Tests against all the similarities we have
	/// </summary>
	public class TestSimilarity2 : LuceneTestCase
	{
	  internal IList<Similarity> Sims;

	  public override void SetUp()
	  {
		base.setUp();
		Sims = new List<>();
		Sims.Add(new DefaultSimilarity());
		Sims.Add(new BM25Similarity());
		// TODO: not great that we dup this all with TestSimilarityBase
		foreach (BasicModel basicModel in TestSimilaritybase.BASIC_MODELS)
		{
		  foreach (AfterEffect afterEffect in TestSimilaritybase.AFTER_EFFECTS)
		  {
			foreach (Normalization normalization in TestSimilaritybase.NORMALIZATIONS)
			{
			  Sims.Add(new DFRSimilarity(basicModel, afterEffect, normalization));
			}
		  }
		}
		foreach (Distribution distribution in TestSimilaritybase.DISTRIBUTIONS)
		{
		  foreach (Lambda lambda in TestSimilaritybase.LAMBDAS)
		  {
			foreach (Normalization normalization in TestSimilaritybase.NORMALIZATIONS)
			{
			  Sims.Add(new IBSimilarity(distribution, lambda, normalization));
			}
		  }
		}
		Sims.Add(new LMDirichletSimilarity());
		Sims.Add(new LMJelinekMercerSimilarity(0.1f));
		Sims.Add(new LMJelinekMercerSimilarity(0.7f));
	  }

	  /// <summary>
	  /// because of stupid things like querynorm, its possible we computeStats on a field that doesnt exist at all
	  ///  test this against a totally empty index, to make sure sims handle it
	  /// </summary>
	  public virtual void TestEmptyIndex()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir);
		IndexReader ir = iw.Reader;
		iw.close();
		IndexSearcher @is = newSearcher(ir);

		foreach (Similarity sim in Sims)
		{
		  @is.Similarity = sim;
		  Assert.AreEqual(0, @is.search(new TermQuery(new Term("foo", "bar")), 10).totalHits);
		}
		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// similar to the above, but ORs the query with a real field </summary>
	  public virtual void TestEmptyField()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(newTextField("foo", "bar", Field.Store.NO));
		iw.addDocument(doc);
		IndexReader ir = iw.Reader;
		iw.close();
		IndexSearcher @is = newSearcher(ir);

		foreach (Similarity sim in Sims)
		{
		  @is.Similarity = sim;
		  BooleanQuery query = new BooleanQuery(true);
		  query.add(new TermQuery(new Term("foo", "bar")), BooleanClause.Occur.SHOULD);
		  query.add(new TermQuery(new Term("bar", "baz")), BooleanClause.Occur.SHOULD);
		  Assert.AreEqual(1, @is.search(query, 10).totalHits);
		}
		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// similar to the above, however the field exists, but we query with a term that doesnt exist too </summary>
	  public virtual void TestEmptyTerm()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(newTextField("foo", "bar", Field.Store.NO));
		iw.addDocument(doc);
		IndexReader ir = iw.Reader;
		iw.close();
		IndexSearcher @is = newSearcher(ir);

		foreach (Similarity sim in Sims)
		{
		  @is.Similarity = sim;
		  BooleanQuery query = new BooleanQuery(true);
		  query.add(new TermQuery(new Term("foo", "bar")), BooleanClause.Occur.SHOULD);
		  query.add(new TermQuery(new Term("foo", "baz")), BooleanClause.Occur.SHOULD);
		  Assert.AreEqual(1, @is.search(query, 10).totalHits);
		}
		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// make sure we can retrieve when norms are disabled </summary>
	  public virtual void TestNoNorms()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
		ft.OmitNorms = true;
		ft.freeze();
		doc.add(newField("foo", "bar", ft));
		iw.addDocument(doc);
		IndexReader ir = iw.Reader;
		iw.close();
		IndexSearcher @is = newSearcher(ir);

		foreach (Similarity sim in Sims)
		{
		  @is.Similarity = sim;
		  BooleanQuery query = new BooleanQuery(true);
		  query.add(new TermQuery(new Term("foo", "bar")), BooleanClause.Occur.SHOULD);
		  Assert.AreEqual(1, @is.search(query, 10).totalHits);
		}
		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// make sure all sims work if TF is omitted </summary>
	  public virtual void TestOmitTF()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
		ft.IndexOptions = IndexOptions.DOCS_ONLY;
		ft.freeze();
		Field f = newField("foo", "bar", ft);
		doc.add(f);
		iw.addDocument(doc);
		IndexReader ir = iw.Reader;
		iw.close();
		IndexSearcher @is = newSearcher(ir);

		foreach (Similarity sim in Sims)
		{
		  @is.Similarity = sim;
		  BooleanQuery query = new BooleanQuery(true);
		  query.add(new TermQuery(new Term("foo", "bar")), BooleanClause.Occur.SHOULD);
		  Assert.AreEqual(1, @is.search(query, 10).totalHits);
		}
		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// make sure all sims work if TF and norms is omitted </summary>
	  public virtual void TestOmitTFAndNorms()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
		ft.IndexOptions = IndexOptions.DOCS_ONLY;
		ft.OmitNorms = true;
		ft.freeze();
		Field f = newField("foo", "bar", ft);
		doc.add(f);
		iw.addDocument(doc);
		IndexReader ir = iw.Reader;
		iw.close();
		IndexSearcher @is = newSearcher(ir);

		foreach (Similarity sim in Sims)
		{
		  @is.Similarity = sim;
		  BooleanQuery query = new BooleanQuery(true);
		  query.add(new TermQuery(new Term("foo", "bar")), BooleanClause.Occur.SHOULD);
		  Assert.AreEqual(1, @is.search(query, 10).totalHits);
		}
		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// make sure all sims work with spanOR(termX, termY) where termY does not exist </summary>
	  public virtual void TestCrazySpans()
	  {
		// The problem: "normal" lucene queries create scorers, returning null if terms dont exist
		// this means they never score a term that does not exist.
		// however with spans, there is only one scorer for the whole hierarchy:
		// inner queries are not real queries, their boosts are ignored, etc.
		Directory dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
		doc.add(newField("foo", "bar", ft));
		iw.addDocument(doc);
		IndexReader ir = iw.Reader;
		iw.close();
		IndexSearcher @is = newSearcher(ir);

		foreach (Similarity sim in Sims)
		{
		  @is.Similarity = sim;
		  SpanTermQuery s1 = new SpanTermQuery(new Term("foo", "bar"));
		  SpanTermQuery s2 = new SpanTermQuery(new Term("foo", "baz"));
		  Query query = new SpanOrQuery(s1, s2);
		  TopDocs td = @is.search(query, 10);
		  Assert.AreEqual(1, td.totalHits);
		  float score = td.scoreDocs[0].score;
		  Assert.IsTrue(score >= 0.0f);
		  Assert.IsFalse("inf score for " + sim, float.IsInfinity(score));
		}
		ir.close();
		dir.close();
	  }
	}

}