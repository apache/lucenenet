/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Analysis;
using Org.Apache.Lucene.Document;
using Org.Apache.Lucene.Expressions;
using Org.Apache.Lucene.Expressions.JS;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Expressions
{
	public class TestExpressionValueSource : LuceneTestCase
	{
		internal DirectoryReader reader;

		internal Directory dir;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			dir = NewDirectory();
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			iwc.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwc);
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
			iw.ForceMerge(1);
			reader = iw.GetReader();
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
		public virtual void TestTypes()
		{
			Expression expr = JavascriptCompiler.Compile("2*popularity");
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add(new SortField("popularity", SortField.Type.LONG));
			ValueSource vs = expr.GetValueSource(bindings);
			NUnit.Framework.Assert.AreEqual(1, reader.Leaves().Count);
			AtomicReaderContext leaf = reader.Leaves()[0];
			FunctionValues values = vs.GetValues(new Dictionary<string, object>(), leaf);
			NUnit.Framework.Assert.AreEqual(10, values.DoubleVal(0), 0);
			NUnit.Framework.Assert.AreEqual(10, values.FloatVal(0), 0);
			NUnit.Framework.Assert.AreEqual(10, values.LongVal(0));
			NUnit.Framework.Assert.AreEqual(10, values.IntVal(0));
			NUnit.Framework.Assert.AreEqual(10, values.ShortVal(0));
			NUnit.Framework.Assert.AreEqual(10, values.ByteVal(0));
			NUnit.Framework.Assert.AreEqual("10.0", values.StrVal(0));
			NUnit.Framework.Assert.AreEqual(System.Convert.ToDouble(10), values.ObjectVal(0));
			NUnit.Framework.Assert.AreEqual(40, values.DoubleVal(1), 0);
			NUnit.Framework.Assert.AreEqual(40, values.FloatVal(1), 0);
			NUnit.Framework.Assert.AreEqual(40, values.LongVal(1));
			NUnit.Framework.Assert.AreEqual(40, values.IntVal(1));
			NUnit.Framework.Assert.AreEqual(40, values.ShortVal(1));
			NUnit.Framework.Assert.AreEqual(40, values.ByteVal(1));
			NUnit.Framework.Assert.AreEqual("40.0", values.StrVal(1));
			NUnit.Framework.Assert.AreEqual(System.Convert.ToDouble(40), values.ObjectVal(1));
			NUnit.Framework.Assert.AreEqual(4, values.DoubleVal(2), 0);
			NUnit.Framework.Assert.AreEqual(4, values.FloatVal(2), 0);
			NUnit.Framework.Assert.AreEqual(4, values.LongVal(2));
			NUnit.Framework.Assert.AreEqual(4, values.IntVal(2));
			NUnit.Framework.Assert.AreEqual(4, values.ShortVal(2));
			NUnit.Framework.Assert.AreEqual(4, values.ByteVal(2));
			NUnit.Framework.Assert.AreEqual("4.0", values.StrVal(2));
			NUnit.Framework.Assert.AreEqual(System.Convert.ToDouble(4), values.ObjectVal(2));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestRangeScorer()
		{
			Expression expr = JavascriptCompiler.Compile("2*popularity");
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add(new SortField("popularity", SortField.Type.LONG));
			ValueSource vs = expr.GetValueSource(bindings);
			NUnit.Framework.Assert.AreEqual(1, reader.Leaves().Count);
			AtomicReaderContext leaf = reader.Leaves()[0];
			FunctionValues values = vs.GetValues(new Dictionary<string, object>(), leaf);
			// everything
			ValueSourceScorer scorer = values.GetRangeScorer(((AtomicReader)leaf.Reader()), "4"
				, "40", true, true);
			NUnit.Framework.Assert.AreEqual(-1, scorer.DocID());
			NUnit.Framework.Assert.AreEqual(0, scorer.NextDoc());
			NUnit.Framework.Assert.AreEqual(1, scorer.NextDoc());
			NUnit.Framework.Assert.AreEqual(2, scorer.NextDoc());
			NUnit.Framework.Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, scorer.NextDoc());
			// just the first doc
			scorer = values.GetRangeScorer(((AtomicReader)leaf.Reader()), "4", "40", false, false
				);
			NUnit.Framework.Assert.AreEqual(-1, scorer.DocID());
			NUnit.Framework.Assert.AreEqual(0, scorer.NextDoc());
			NUnit.Framework.Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, scorer.NextDoc());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestEquals()
		{
			Expression expr = JavascriptCompiler.Compile("sqrt(a) + ln(b)");
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add(new SortField("a", SortField.Type.INT));
			bindings.Add(new SortField("b", SortField.Type.INT));
			ValueSource vs1 = expr.GetValueSource(bindings);
			// same instance
			NUnit.Framework.Assert.AreEqual(vs1, vs1);
			// null
			NUnit.Framework.Assert.IsFalse(vs1.Equals(null));
			// other object
			NUnit.Framework.Assert.IsFalse(vs1.Equals("foobar"));
			// same bindings and expression instances
			ValueSource vs2 = expr.GetValueSource(bindings);
			NUnit.Framework.Assert.AreEqual(vs1.GetHashCode(), vs2.GetHashCode());
			NUnit.Framework.Assert.AreEqual(vs1, vs2);
			// equiv bindings (different instance)
			SimpleBindings bindings2 = new SimpleBindings();
			bindings2.Add(new SortField("a", SortField.Type.INT));
			bindings2.Add(new SortField("b", SortField.Type.INT));
			ValueSource vs3 = expr.GetValueSource(bindings2);
			NUnit.Framework.Assert.AreEqual(vs1, vs3);
			// different bindings (same names, different types)
			SimpleBindings bindings3 = new SimpleBindings();
			bindings3.Add(new SortField("a", SortField.Type.LONG));
			bindings3.Add(new SortField("b", SortField.Type.INT));
			ValueSource vs4 = expr.GetValueSource(bindings3);
			NUnit.Framework.Assert.IsFalse(vs1.Equals(vs4));
		}
	}
}
