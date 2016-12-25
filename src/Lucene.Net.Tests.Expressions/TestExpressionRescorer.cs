using Lucene.Net.Documents;
using Lucene.Net.Expressions;
using Lucene.Net.Expressions.JS;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using NUnit.Framework;

namespace Lucene.Net.Tests.Expressions
{
	public class TestExpressionRescorer : Lucene.Net.Util.LuceneTestCase
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
			    new NumericDocValuesField("popularity", 5)
			};
		    iw.AddDocument(doc);
			doc = new Document
			{
			    NewStringField("id", "2", Field.Store.YES),
			    NewTextField("body", "another document with different contents", Field.Store
			        .NO),
			    new NumericDocValuesField("popularity", 20)
			};
		    iw.AddDocument(doc);
			doc = new Document
			{
			    NewStringField("id", "3", Field.Store.YES),
			    NewTextField("body", "crappy contents", Field.Store.NO),
			    new NumericDocValuesField("popularity", 2)
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

		[Test]
		public virtual void TestBasic()
		{
			// create a sort field and sort by it (reverse order)
			Query query = new TermQuery(new Term("body", "contents"));
			IndexReader r = searcher.IndexReader;
			// Just first pass query
			TopDocs hits = searcher.Search(query, 10);
			AreEqual(3, hits.TotalHits);
			AreEqual("3", r.Document(hits.ScoreDocs[0].Doc).Get("id"));
			AreEqual("1", r.Document(hits.ScoreDocs[1].Doc).Get("id"));
			AreEqual("2", r.Document(hits.ScoreDocs[2].Doc).Get("id"));
			// Now, rescore:
			Expression e = JavascriptCompiler.Compile("sqrt(_score) + ln(popularity)");
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add(new SortField("popularity", SortFieldType.INT));
			bindings.Add(new SortField("_score", SortFieldType.SCORE));
			Rescorer rescorer = e.GetRescorer(bindings);
			hits = rescorer.Rescore(searcher, hits, 10);
			AreEqual(3, hits.TotalHits);
			AreEqual("2", r.Document(hits.ScoreDocs[0].Doc).Get("id"));
			AreEqual("1", r.Document(hits.ScoreDocs[1].Doc).Get("id"));
			AreEqual("3", r.Document(hits.ScoreDocs[2].Doc).Get("id"));
			string expl = rescorer.Explain(searcher, searcher.Explain(query, hits.ScoreDocs[0].Doc), hits.ScoreDocs[0].Doc).ToString();
			// Confirm the explanation breaks out the individual
			// variables:
			IsTrue(expl.Contains("= variable \"popularity\""));
			// Confirm the explanation includes first pass details:
			IsTrue(expl.Contains("= first pass score"));
			IsTrue(expl.Contains("body:contents in"));
		}
	}
}
