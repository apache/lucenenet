/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Document;
using Org.Apache.Lucene.Expressions;
using Org.Apache.Lucene.Expressions.JS;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Expressions
{
	public class TestExpressionRescorer : LuceneTestCase
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
			Org.Apache.Lucene.Document.Document doc = new Org.Apache.Lucene.Document.Document
				();
			doc.Add(NewStringField("id", "1", Field.Store.YES));
			doc.Add(NewTextField("body", "some contents and more contents", Field.Store.NO));
			doc.Add(new NumericDocValuesField("popularity", 5));
			iw.AddDocument(doc);
			doc = new Org.Apache.Lucene.Document.Document();
			doc.Add(NewStringField("id", "2", Field.Store.YES));
			doc.Add(NewTextField("body", "another document with different contents", Field.Store
				.NO));
			doc.Add(new NumericDocValuesField("popularity", 20));
			iw.AddDocument(doc);
			doc = new Org.Apache.Lucene.Document.Document();
			doc.Add(NewStringField("id", "3", Field.Store.YES));
			doc.Add(NewTextField("body", "crappy contents", Field.Store.NO));
			doc.Add(new NumericDocValuesField("popularity", 2));
			iw.AddDocument(doc);
			reader = iw.GetReader();
			searcher = new IndexSearcher(reader);
			iw.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader.Close();
			dir.Close();
			base.TearDown();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBasic()
		{
			// create a sort field and sort by it (reverse order)
			Query query = new TermQuery(new Term("body", "contents"));
			IndexReader r = searcher.GetIndexReader();
			// Just first pass query
			TopDocs hits = searcher.Search(query, 10);
			NUnit.Framework.Assert.AreEqual(3, hits.totalHits);
			NUnit.Framework.Assert.AreEqual("3", r.Document(hits.scoreDocs[0].doc).Get("id"));
			NUnit.Framework.Assert.AreEqual("1", r.Document(hits.scoreDocs[1].doc).Get("id"));
			NUnit.Framework.Assert.AreEqual("2", r.Document(hits.scoreDocs[2].doc).Get("id"));
			// Now, rescore:
			Expression e = JavascriptCompiler.Compile("sqrt(_score) + ln(popularity)");
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add(new SortField("popularity", SortField.Type.INT));
			bindings.Add(new SortField("_score", SortField.Type.SCORE));
			Rescorer rescorer = e.GetRescorer(bindings);
			hits = rescorer.Rescore(searcher, hits, 10);
			NUnit.Framework.Assert.AreEqual(3, hits.totalHits);
			NUnit.Framework.Assert.AreEqual("2", r.Document(hits.scoreDocs[0].doc).Get("id"));
			NUnit.Framework.Assert.AreEqual("1", r.Document(hits.scoreDocs[1].doc).Get("id"));
			NUnit.Framework.Assert.AreEqual("3", r.Document(hits.scoreDocs[2].doc).Get("id"));
			string expl = rescorer.Explain(searcher, searcher.Explain(query, hits.scoreDocs[0
				].doc), hits.scoreDocs[0].doc).ToString();
			// Confirm the explanation breaks out the individual
			// variables:
			NUnit.Framework.Assert.IsTrue(expl.Contains("= variable \"popularity\""));
			// Confirm the explanation includes first pass details:
			NUnit.Framework.Assert.IsTrue(expl.Contains("= first pass score"));
			NUnit.Framework.Assert.IsTrue(expl.Contains("body:contents in"));
		}
	}
}
