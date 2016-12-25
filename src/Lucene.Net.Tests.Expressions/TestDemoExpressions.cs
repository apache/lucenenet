using System;
using System.Text;
using Lucene.Net.Documents;
using Lucene.Net.Expressions;
using Lucene.Net.Expressions.JS;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using NUnit.Framework;
using Expression = System.Linq.Expressions.Expression;

namespace Lucene.Net.Tests.Expressions
{
	/// <summary>simple demo of using expressions</summary>
	public class TestDemoExpressions : Util.LuceneTestCase
	{
		internal IndexSearcher searcher;

		internal DirectoryReader reader;

		internal Directory dir;

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			dir = NewDirectory();
			var iw = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
			var doc = new Document
			{
			    NewStringField("id", "1", Field.Store.YES),
			    NewTextField("body", "some contents and more contents", Field.Store.NO),
			    new NumericDocValuesField("popularity", 5),
			    new DoubleField("latitude", 40.759011, Field.Store.NO),
			    new DoubleField("longitude", -73.9844722, Field.Store.NO)
			};
		    iw.AddDocument(doc);
			doc = new Document
			{
			    NewStringField("id", "2", Field.Store.YES),
			    NewTextField("body", "another document with different contents", Field.Store
			        .NO),
			    new NumericDocValuesField("popularity", 20),
			    new DoubleField("latitude", 40.718266, Field.Store.NO),
			    new DoubleField("longitude", -74.007819, Field.Store.NO)
			};
		    iw.AddDocument(doc);
			doc = new Document
			{
			    NewStringField("id", "3", Field.Store.YES),
			    NewTextField("body", "crappy contents", Field.Store.NO),
			    new NumericDocValuesField("popularity", 2),
			    new DoubleField("latitude", 40.7051157, Field.Store.NO),
			    new DoubleField("longitude", -74.0088305, Field.Store.NO)
			};
		    iw.AddDocument(doc);
			reader = iw.Reader;
			searcher = new IndexSearcher(reader);
			iw.Dispose();
		}

		[TearDown]
		public override void TearDown()
		{
			reader.Dispose();
			dir.Dispose();
			base.TearDown();
		}

		/// <summary>an example of how to rank by an expression</summary>
		[Test]
		public virtual void Test()
		{
			// compile an expression:
			var expr = JavascriptCompiler.Compile("sqrt(_score) + ln(popularity)");
			// we use SimpleBindings: which just maps variables to SortField instances
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add(new SortField("_score", SortFieldType.SCORE));
			bindings.Add(new SortField("popularity", SortFieldType.INT));
			// create a sort field and sort by it (reverse order)
			Sort sort = new Sort(expr.GetSortField(bindings, true));
			Query query = new TermQuery(new Term("body", "contents"));
			searcher.Search(query, null, 3, sort);
		}

		/// <summary>tests the returned sort values are correct</summary>
		[Test]
		public virtual void TestSortValues()
		{
			var expr = JavascriptCompiler.Compile("sqrt(_score)");
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add(new SortField("_score", SortFieldType.SCORE));
			Sort sort = new Sort(expr.GetSortField(bindings, true));
			Query query = new TermQuery(new Term("body", "contents"));
			TopFieldDocs td = searcher.Search(query, null, 3, sort, true, true);
			for (int i = 0; i < 3; i++)
			{
				FieldDoc d = (FieldDoc)td.ScoreDocs[i];
				float expected = (float)Math.Sqrt(d.Score);
				float actual = (float)((double)d.Fields[0]);
				AreEqual(expected, actual, CheckHits.ExplainToleranceDelta(expected, actual));
			}
		}

		/// <summary>tests same binding used more than once in an expression</summary>
		[Test]
		public virtual void TestTwoOfSameBinding()
		{
			var expr = JavascriptCompiler.Compile("_score + _score");
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add(new SortField("_score", SortFieldType.SCORE));
			Sort sort = new Sort(expr.GetSortField(bindings, true));
			Query query = new TermQuery(new Term("body", "contents"));
			TopFieldDocs td = searcher.Search(query, null, 3, sort, true, true);
			for (int i = 0; i < 3; i++)
			{
				FieldDoc d = (FieldDoc)td.ScoreDocs[i];
				float expected = 2 * d.Score;
				float actual = (float)((double)d.Fields[0]);
				AreEqual(expected, actual, CheckHits.ExplainToleranceDelta
					(expected, actual));
			}
		}

		/// <summary>tests expression referring to another expression</summary>
		[Test]
		public virtual void TestExpressionRefersToExpression()
		{
			var expr1 = JavascriptCompiler.Compile("_score");
			var expr2 = JavascriptCompiler.Compile("2*expr1");
			var bindings = new SimpleBindings();
			bindings.Add(new SortField("_score", SortFieldType.SCORE));
			bindings.Add("expr1", expr1);
			Sort sort = new Sort(expr2.GetSortField(bindings, true));
			Query query = new TermQuery(new Term("body", "contents"));
			TopFieldDocs td = searcher.Search(query, null, 3, sort, true, true);
			for (int i = 0; i < 3; i++)
			{
				FieldDoc d = (FieldDoc)td.ScoreDocs[i];
				float expected = 2 * d.Score;
				float actual = (float)((double)d.Fields[0]);
				AreEqual(expected, actual, CheckHits.ExplainToleranceDelta
					(expected, actual));
			}
		}

		/// <summary>tests huge amounts of variables in the expression</summary>
		[Test]
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
				bindings.Add(new SortField("x" + i, SortFieldType.SCORE));
			}
			var expr = JavascriptCompiler.Compile(sb.ToString());
			var sort = new Sort(expr.GetSortField(bindings, true));
			Query query = new TermQuery(new Term("body", "contents"));
			TopFieldDocs td = searcher.Search(query, null, 3, sort, true, true);
			for (int i_1 = 0; i_1 < 3; i_1++)
			{
				FieldDoc d = (FieldDoc)td.ScoreDocs[i_1];
				float expected = n * d.Score;
				float actual = (float)((double)d.Fields[0]);
				AreEqual(expected, actual, CheckHits.ExplainToleranceDelta(expected, actual));
			}
		}

		[Test]
		public virtual void TestDistanceSort()
		{
			var distance = JavascriptCompiler.Compile("haversin(40.7143528,-74.0059731,latitude,longitude)");
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add(new SortField("latitude", SortFieldType.DOUBLE));
			bindings.Add(new SortField("longitude", SortFieldType.DOUBLE));
			Sort sort = new Sort(distance.GetSortField(bindings, false));
			TopFieldDocs td = searcher.Search(new MatchAllDocsQuery(), null, 3, sort);
			FieldDoc d = (FieldDoc)td.ScoreDocs[0];
			AreEqual(0.4619D, (double)d.Fields[0], 1E-4);
			d = (FieldDoc)td.ScoreDocs[1];
			AreEqual(1.0546D, (double)d.Fields[0], 1E-4);
			d = (FieldDoc)td.ScoreDocs[2];
			AreEqual(5.2842D, (double)d.Fields[0], 1E-4);
		}
	}
}
