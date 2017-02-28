using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Expressions.JS;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Collections.Generic;

namespace Lucene.Net.Expressions
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

    [SuppressCodecs("Lucene3x")]
	public class TestExpressionValueSource : LuceneTestCase
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
			bindings.Add(new SortField("popularity", SortFieldType.INT64));
			ValueSource vs = expr.GetValueSource(bindings);
			AreEqual(1, reader.Leaves.Count);
			AtomicReaderContext leaf = reader.Leaves[0];
			FunctionValues values = vs.GetValues(new Dictionary<string, object>(), leaf);
			AreEqual(10, values.DoubleVal(0), 0);
			AreEqual(10, values.SingleVal(0), 0);
			AreEqual(10, values.Int64Val(0));
			AreEqual(10, values.Int32Val(0));
			AreEqual(10, values.Int16Val(0));
			AreEqual(10, values.ByteVal(0));
			AreEqual("10", values.StrVal(0));
			AreEqual(System.Convert.ToDouble(10), values.ObjectVal(0));
			AreEqual(40, values.DoubleVal(1), 0);
			AreEqual(40, values.SingleVal(1), 0);
			AreEqual(40, values.Int64Val(1));
			AreEqual(40, values.Int32Val(1));
			AreEqual(40, values.Int16Val(1));
			AreEqual(40, values.ByteVal(1));
			AreEqual("40", values.StrVal(1));
			AreEqual(System.Convert.ToDouble(40), values.ObjectVal(1));
			AreEqual(4, values.DoubleVal(2), 0);
			AreEqual(4, values.SingleVal(2), 0);
			AreEqual(4, values.Int64Val(2));
			AreEqual(4, values.Int32Val(2));
			AreEqual(4, values.Int16Val(2));
			AreEqual(4, values.ByteVal(2));
			AreEqual("4", values.StrVal(2));
			AreEqual(System.Convert.ToDouble(4), values.ObjectVal(2));
		}

		[Test]
		public virtual void TestRangeScorer()
		{
			Expression expr = JavascriptCompiler.Compile("2*popularity");
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add(new SortField("popularity", SortFieldType.INT64));
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
			bindings.Add(new SortField("a", SortFieldType.INT32));
			bindings.Add(new SortField("b", SortFieldType.INT32));
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
			bindings2.Add(new SortField("a", SortFieldType.INT32));
			bindings2.Add(new SortField("b", SortFieldType.INT32));
			ValueSource vs3 = expr.GetValueSource(bindings2);
			AreEqual(vs1, vs3);
			// different bindings (same names, different types)
			SimpleBindings bindings3 = new SimpleBindings();
			bindings3.Add(new SortField("a", SortFieldType.INT64));
			bindings3.Add(new SortField("b", SortFieldType.INT32));
			ValueSource vs4 = expr.GetValueSource(bindings3);
			IsFalse(vs1.Equals(vs4));
		}
	}
}
