using System;
using System.Linq.Expressions;
using System.Text;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;

namespace Lucene.Net.Tests.Expressions
{
	/// <summary>simple demo of using expressions</summary>
	public class TestDemoExpressions : Util.LuceneTestCase
	{
		internal IndexSearcher searcher;

		internal DirectoryReader reader;

		internal Directory dir;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			Document doc = new Document
				();
			doc.Add(NewStringField("id", "1", Field.Store.YES));
			doc.Add(NewTextField("body", "some contents and more contents", Field.Store.NO));
			doc.Add(new NumericDocValuesField("popularity", 5));
			doc.Add(new DoubleField("latitude", 40.759011, Field.Store.NO));
			doc.Add(new DoubleField("longitude", -73.9844722, Field.Store.NO));
			iw.AddDocument(doc);
			doc = new Document();
			doc.Add(NewStringField("id", "2", Field.Store.YES));
			doc.Add(NewTextField("body", "another document with different contents", Field.Store
				.NO));
			doc.Add(new NumericDocValuesField("popularity", 20));
			doc.Add(new DoubleField("latitude", 40.718266, Field.Store.NO));
			doc.Add(new DoubleField("longitude", -74.007819, Field.Store.NO));
			iw.AddDocument(doc);
			doc = new Document();
			doc.Add(NewStringField("id", "3", Field.Store.YES));
			doc.Add(NewTextField("body", "crappy contents", Field.Store.NO));
			doc.Add(new NumericDocValuesField("popularity", 2));
			doc.Add(new DoubleField("latitude", 40.7051157, Field.Store.NO));
			doc.Add(new DoubleField("longitude", -74.0088305, Field.Store.NO));
			iw.AddDocument(doc);
			reader = iw.Reader;
			searcher = new IndexSearcher(reader);
			iw.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader.Close();
			dir.Close();
			base.TearDown();
		}

		/// <summary>an example of how to rank by an expression</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void Test()
		{
			// compile an expression:
			Expression expr = JavascriptCompiler.Compile("sqrt(_score) + ln(popularity)");
			// we use SimpleBindings: which just maps variables to SortField instances
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add(new SortField("_score", SortField.Type.SCORE));
			bindings.Add(new SortField("popularity", SortField.Type.INT));
			// create a sort field and sort by it (reverse order)
			Sort sort = new Sort(expr.GetSortField(bindings, true));
			Query query = new TermQuery(new Term("body", "contents"));
			searcher.Search(query, null, 3, sort);
		}

		/// <summary>tests the returned sort values are correct</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSortValues()
		{
			Expression expr = JavascriptCompiler.Compile("sqrt(_score)");
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add(new SortField("_score", SortField.Type.SCORE));
			Sort sort = new Sort(expr.GetSortField(bindings, true));
			Query query = new TermQuery(new Term("body", "contents"));
			TopFieldDocs td = searcher.Search(query, null, 3, sort, true, true);
			for (int i = 0; i < 3; i++)
			{
				FieldDoc d = (FieldDoc)td.scoreDocs[i];
				float expected = (float)Math.Sqrt(d.score);
				float actual = ((double)d.fields[0]);
				NUnit.Framework.Assert.AreEqual(expected, actual, CheckHits.ExplainToleranceDelta
					(expected, actual));
			}
		}

		/// <summary>tests same binding used more than once in an expression</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestTwoOfSameBinding()
		{
			Expression expr = JavascriptCompiler.Compile("_score + _score");
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add(new SortField("_score", SortField.Type.SCORE));
			Sort sort = new Sort(expr.GetSortField(bindings, true));
			Query query = new TermQuery(new Term("body", "contents"));
			TopFieldDocs td = searcher.Search(query, null, 3, sort, true, true);
			for (int i = 0; i < 3; i++)
			{
				FieldDoc d = (FieldDoc)td.scoreDocs[i];
				float expected = 2 * d.score;
				float actual = ((double)d.fields[0]);
				NUnit.Framework.Assert.AreEqual(expected, actual, CheckHits.ExplainToleranceDelta
					(expected, actual));
			}
		}

		/// <summary>tests expression referring to another expression</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestExpressionRefersToExpression()
		{
			Expression expr1 = JavascriptCompiler.Compile("_score");
			Expression expr2 = JavascriptCompiler.Compile("2*expr1");
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add(new SortField("_score", SortField.Type.SCORE));
			bindings.Add("expr1", expr1);
			Sort sort = new Sort(expr2.GetSortField(bindings, true));
			Query query = new TermQuery(new Term("body", "contents"));
			TopFieldDocs td = searcher.Search(query, null, 3, sort, true, true);
			for (int i = 0; i < 3; i++)
			{
				FieldDoc d = (FieldDoc)td.scoreDocs[i];
				float expected = 2 * d.score;
				float actual = ((double)d.fields[0]);
				NUnit.Framework.Assert.AreEqual(expected, actual, CheckHits.ExplainToleranceDelta
					(expected, actual));
			}
		}

		/// <summary>tests huge amounts of variables in the expression</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestLotsOfBindings()
		{
			DoTestLotsOfBindings(byte.MaxValue - 1);
			DoTestLotsOfBindings(byte.MaxValue);
			DoTestLotsOfBindings(byte.MaxValue + 1);
		}

		// TODO: ideally we'd test > Short.MAX_VALUE too, but compilation is currently recursive.
		// so if we want to test such huge expressions, we need to instead change parser to use an explicit Stack
		/// <exception cref="System.Exception"></exception>
		private void DoTestLotsOfBindings(int n)
		{
			SimpleBindings bindings = new SimpleBindings();
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < n; i++)
			{
				if (i > 0)
				{
					sb.Append("+");
				}
				sb.Append("x" + i);
				bindings.Add(new SortField("x" + i, SortField.Type.SCORE));
			}
			Expression expr = JavascriptCompiler.Compile(sb.ToString());
			Sort sort = new Sort(expr.GetSortField(bindings, true));
			Query query = new TermQuery(new Term("body", "contents"));
			TopFieldDocs td = searcher.Search(query, null, 3, sort, true, true);
			for (int i_1 = 0; i_1 < 3; i_1++)
			{
				FieldDoc d = (FieldDoc)td.scoreDocs[i_1];
				float expected = n * d.score;
				float actual = ((double)d.fields[0]);
				NUnit.Framework.Assert.AreEqual(expected, actual, CheckHits.ExplainToleranceDelta
					(expected, actual));
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDistanceSort()
		{
			Expression distance = JavascriptCompiler.Compile("haversin(40.7143528,-74.0059731,latitude,longitude)"
				);
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add(new SortField("latitude", SortField.Type.DOUBLE));
			bindings.Add(new SortField("longitude", SortField.Type.DOUBLE));
			Sort sort = new Sort(distance.GetSortField(bindings, false));
			TopFieldDocs td = searcher.Search(new MatchAllDocsQuery(), null, 3, sort);
			FieldDoc d = (FieldDoc)td.scoreDocs[0];
			NUnit.Framework.Assert.AreEqual(0.4619D, (double)d.fields[0], 1E-4);
			d = (FieldDoc)td.scoreDocs[1];
			NUnit.Framework.Assert.AreEqual(1.0546D, (double)d.fields[0], 1E-4);
			d = (FieldDoc)td.scoreDocs[2];
			NUnit.Framework.Assert.AreEqual(5.2842D, (double)d.fields[0], 1E-4);
		}
	}
}
