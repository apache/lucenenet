using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Expressions;
using Lucene.Net.Expressions.JS;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Store;
using NUnit.Framework;

namespace Lucene.Net.Tests.Expressions
{
	public class TestExpressionValueSource : Util.LuceneTestCase
	{
		internal DirectoryReader reader;

		internal Directory dir;

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			dir = NewDirectory();
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			iwc.SetMergePolicy(NewLogMergePolicy());
			var iw = new RandomIndexWriter(Random(), dir, iwc);
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
			iw.ForceMerge(1);
			reader = iw.Reader;
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
		public virtual void TestTypes()
		{
			Expression expr = JavascriptCompiler.Compile("2*popularity");
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add(new SortField("popularity", SortFieldType.LONG));
			ValueSource vs = expr.GetValueSource(bindings);
			AreEqual(1, reader.Leaves.Count);
			AtomicReaderContext leaf = reader.Leaves[0];
			FunctionValues values = vs.GetValues(new Dictionary<string, object>(), leaf);
			AreEqual(10, values.DoubleVal(0), 0);
			AreEqual(10, values.FloatVal(0), 0);
			AreEqual(10, values.LongVal(0));
			AreEqual(10, values.IntVal(0));
			AreEqual(10, values.ShortVal(0));
			AreEqual(10, values.ByteVal(0));
			AreEqual("10", values.StrVal(0));
			AreEqual(System.Convert.ToDouble(10), values.ObjectVal(0));
			AreEqual(40, values.DoubleVal(1), 0);
			AreEqual(40, values.FloatVal(1), 0);
			AreEqual(40, values.LongVal(1));
			AreEqual(40, values.IntVal(1));
			AreEqual(40, values.ShortVal(1));
			AreEqual(40, values.ByteVal(1));
			AreEqual("40", values.StrVal(1));
			AreEqual(System.Convert.ToDouble(40), values.ObjectVal(1));
			AreEqual(4, values.DoubleVal(2), 0);
			AreEqual(4, values.FloatVal(2), 0);
			AreEqual(4, values.LongVal(2));
			AreEqual(4, values.IntVal(2));
			AreEqual(4, values.ShortVal(2));
			AreEqual(4, values.ByteVal(2));
			AreEqual("4", values.StrVal(2));
			AreEqual(System.Convert.ToDouble(4), values.ObjectVal(2));
		}

		[Test]
		public virtual void TestRangeScorer()
		{
			Expression expr = JavascriptCompiler.Compile("2*popularity");
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add(new SortField("popularity", SortFieldType.LONG));
			ValueSource vs = expr.GetValueSource(bindings);
			AreEqual(1, reader.Leaves.Count);
			AtomicReaderContext leaf = reader.Leaves[0];
			FunctionValues values = vs.GetValues(new Dictionary<string, object>(), leaf);
			// everything
			ValueSourceScorer scorer = values.GetRangeScorer(leaf.Reader, "4"
				, "40", true, true);
			AreEqual(-1, scorer.DocID);
			AreEqual(0, scorer.NextDoc());
			AreEqual(1, scorer.NextDoc());
			AreEqual(2, scorer.NextDoc());
			AreEqual(DocIdSetIterator.NO_MORE_DOCS, scorer.NextDoc());
			// just the first doc
			scorer = values.GetRangeScorer(leaf.Reader, "4", "40", false, false);
			AreEqual(-1, scorer.DocID);
			AreEqual(0, scorer.NextDoc());
			AreEqual(DocIdSetIterator.NO_MORE_DOCS, scorer.NextDoc());
		}

		[Test]
		public virtual void TestEquals()
		{
			Expression expr = JavascriptCompiler.Compile("sqrt(a) + ln(b)");
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add(new SortField("a", SortFieldType.INT));
			bindings.Add(new SortField("b", SortFieldType.INT));
			ValueSource vs1 = expr.GetValueSource(bindings);
			// same instance
			AreEqual(vs1, vs1);
			// null
			IsFalse(vs1.Equals(null));
			// other object
			IsFalse(vs1.Equals("foobar"));
			// same bindings and expression instances
			ValueSource vs2 = expr.GetValueSource(bindings);
			AreEqual(vs1.GetHashCode(), vs2.GetHashCode());
			AreEqual(vs1, vs2);
			// equiv bindings (different instance)
			SimpleBindings bindings2 = new SimpleBindings();
			bindings2.Add(new SortField("a", SortFieldType.INT));
			bindings2.Add(new SortField("b", SortFieldType.INT));
			ValueSource vs3 = expr.GetValueSource(bindings2);
			AreEqual(vs1, vs3);
			// different bindings (same names, different types)
			SimpleBindings bindings3 = new SimpleBindings();
			bindings3.Add(new SortField("a", SortFieldType.LONG));
			bindings3.Add(new SortField("b", SortFieldType.INT));
			ValueSource vs4 = expr.GetValueSource(bindings3);
			IsFalse(vs1.Equals(vs4));
		}
	}
}
