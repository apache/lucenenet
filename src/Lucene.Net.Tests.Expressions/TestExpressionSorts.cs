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
	/// <summary>
	/// Tests some basic expressions against different queries,
	/// and fieldcache/docvalues fields against an equivalent sort.
	/// </summary>
	/// <remarks>
	/// Tests some basic expressions against different queries,
	/// and fieldcache/docvalues fields against an equivalent sort.
	/// </remarks>
	public class TestExpressionSorts : LuceneTestCase
	{
		private Directory dir;

		private IndexReader reader;

		private IndexSearcher searcher;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			int numDocs = TestUtil.NextInt(Random(), 2049, 4000);
			for (int i = 0; i < numDocs; i++)
			{
				Org.Apache.Lucene.Document.Document document = new Org.Apache.Lucene.Document.Document
					();
				document.Add(NewTextField("english", English.IntToEnglish(i), Field.Store.NO));
				document.Add(NewTextField("oddeven", (i % 2 == 0) ? "even" : "odd", Field.Store.NO
					));
				document.Add(NewStringField("byte", string.Empty + (unchecked((byte)Random().Next
					())), Field.Store.NO));
				document.Add(NewStringField("short", string.Empty + ((short)Random().Next()), Field.Store
					.NO));
				document.Add(new IntField("int", Random().Next(), Field.Store.NO));
				document.Add(new LongField("long", Random().NextLong(), Field.Store.NO));
				document.Add(new FloatField("float", Random().NextFloat(), Field.Store.NO));
				document.Add(new DoubleField("double", Random().NextDouble(), Field.Store.NO));
				document.Add(new NumericDocValuesField("intdocvalues", Random().Next()));
				document.Add(new FloatDocValuesField("floatdocvalues", Random().NextFloat()));
				iw.AddDocument(document);
			}
			reader = iw.GetReader();
			iw.Close();
			searcher = NewSearcher(reader);
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader.Close();
			dir.Close();
			base.TearDown();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestQueries()
		{
			int n = AtLeast(4);
			for (int i = 0; i < n; i++)
			{
				Filter odd = new QueryWrapperFilter(new TermQuery(new Term("oddeven", "odd")));
				AssertQuery(new MatchAllDocsQuery(), null);
				AssertQuery(new TermQuery(new Term("english", "one")), null);
				AssertQuery(new MatchAllDocsQuery(), odd);
				AssertQuery(new TermQuery(new Term("english", "four")), odd);
				BooleanQuery bq = new BooleanQuery();
				bq.Add(new TermQuery(new Term("english", "one")), BooleanClause.Occur.SHOULD);
				bq.Add(new TermQuery(new Term("oddeven", "even")), BooleanClause.Occur.SHOULD);
				AssertQuery(bq, null);
				// force in order
				bq.Add(new TermQuery(new Term("english", "two")), BooleanClause.Occur.SHOULD);
				bq.SetMinimumNumberShouldMatch(2);
				AssertQuery(bq, null);
			}
		}

		/// <exception cref="System.Exception"></exception>
		internal virtual void AssertQuery(Query query, Filter filter)
		{
			for (int i = 0; i < 10; i++)
			{
				bool reversed = Random().NextBoolean();
				SortField[] fields = new SortField[] { new SortField("int", SortField.Type.INT, reversed
					), new SortField("long", SortField.Type.LONG, reversed), new SortField("float", 
					SortField.Type.FLOAT, reversed), new SortField("double", SortField.Type.DOUBLE, 
					reversed), new SortField("intdocvalues", SortField.Type.INT, reversed), new SortField
					("floatdocvalues", SortField.Type.FLOAT, reversed), new SortField("score", SortField.Type
					.SCORE) };
				Collections.Shuffle(Arrays.AsList(fields), Random());
				int numSorts = TestUtil.NextInt(Random(), 1, fields.Length);
				AssertQuery(query, filter, new Sort(Arrays.CopyOfRange(fields, 0, numSorts)));
			}
		}

		/// <exception cref="System.Exception"></exception>
		internal virtual void AssertQuery(Query query, Filter filter, Sort sort)
		{
			int size = TestUtil.NextInt(Random(), 1, searcher.GetIndexReader().MaxDoc() / 5);
			TopDocs expected = searcher.Search(query, filter, size, sort, Random().NextBoolean
				(), Random().NextBoolean());
			// make our actual sort, mutating original by replacing some of the 
			// sortfields with equivalent expressions
			SortField[] original = sort.GetSort();
			SortField[] mutated = new SortField[original.Length];
			for (int i = 0; i < mutated.Length; i++)
			{
				if (Random().Next(3) > 0)
				{
					SortField s = original[i];
					Expression expr = JavascriptCompiler.Compile(s.GetField());
					SimpleBindings simpleBindings = new SimpleBindings();
					simpleBindings.Add(s);
					bool reverse = s.GetType() == SortField.Type.SCORE || s.GetReverse();
					mutated[i] = expr.GetSortField(simpleBindings, reverse);
				}
				else
				{
					mutated[i] = original[i];
				}
			}
			Sort mutatedSort = new Sort(mutated);
			TopDocs actual = searcher.Search(query, filter, size, mutatedSort, Random().NextBoolean
				(), Random().NextBoolean());
			CheckHits.CheckEqual(query, expected.scoreDocs, actual.scoreDocs);
			if (size < actual.totalHits)
			{
				expected = searcher.SearchAfter(expected.scoreDocs[size - 1], query, filter, size
					, sort);
				actual = searcher.SearchAfter(actual.scoreDocs[size - 1], query, filter, size, mutatedSort
					);
				CheckHits.CheckEqual(query, expected.scoreDocs, actual.scoreDocs);
			}
		}
	}
}
