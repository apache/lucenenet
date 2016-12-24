using Lucene.Net.Documents;
using Lucene.Net.Expressions;
using Lucene.Net.Expressions.JS;
using Lucene.Net.Index;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Tests.Expressions
{
    /// <summary>
    /// Tests some basic expressions against different queries,
    /// and fieldcache/docvalues fields against an equivalent sort.
    /// </summary>
    /// <remarks>
    /// Tests some basic expressions against different queries,
    /// and fieldcache/docvalues fields against an equivalent sort.
    /// </remarks>
    public class TestExpressionSorts : Util.LuceneTestCase
    {
        private Directory dir;

        private IndexReader reader;

        private IndexSearcher searcher;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            dir = NewDirectory();
            var iw = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            int numDocs = TestUtil.NextInt(Random(), 2049, 4000);
            for (int i = 0; i < numDocs; i++)
            {
                var document = new Document
				{
				    NewTextField("english", English.IntToEnglish(i), Field.Store.NO),
				    NewTextField("oddeven", (i%2 == 0) ? "even" : "odd", Field.Store.NO
				        ),
				    NewStringField("byte", string.Empty + (unchecked((byte) Random().Next
				        ())), Field.Store.NO),
				    NewStringField("short", string.Empty + ((short) Random().Next()), Field.Store
				        .NO),
				    new IntField("int", Random().Next(), Field.Store.NO),
				    new LongField("long", Random().NextLong(), Field.Store.NO),
				    new FloatField("float", Random().NextFloat(), Field.Store.NO),
				    new DoubleField("double", Random().NextDouble(), Field.Store.NO),
				    new NumericDocValuesField("intdocvalues", Random().Next()),
				    new FloatDocValuesField("floatdocvalues", Random().NextFloat())
				};
                iw.AddDocument(document);
            }
            reader = iw.Reader;
            iw.Dispose();
            searcher = NewSearcher(reader);
        }

        [TearDown]
        public override void TearDown()
        {
            reader.Dispose();
            dir.Dispose();
            base.TearDown();
        }

        [Test]
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
                bq.Add(new TermQuery(new Term("english", "one")), Occur.SHOULD);
                bq.Add(new TermQuery(new Term("oddeven", "even")), Occur.SHOULD);
                AssertQuery(bq, null);
                // force in order
                bq.Add(new TermQuery(new Term("english", "two")), Occur.SHOULD);
                bq.MinimumNumberShouldMatch = 2;
                AssertQuery(bq, null);
            }
        }

        
        internal virtual void AssertQuery(Query query, Filter filter)
        {
            for (int i = 0; i < 10; i++)
            {
                bool reversed = Random().NextBoolean();
                SortField[] fields =
				{ new SortField("int", SortField.Type_e.INT, reversed
				    ), new SortField("long", SortField.Type_e.LONG, reversed), new SortField("float", 
				        SortField.Type_e.FLOAT, reversed), new SortField("double", SortField.Type_e.DOUBLE, 
				            reversed), new SortField("intdocvalues", SortField.Type_e.INT, reversed), new SortField
				                ("floatdocvalues", SortField.Type_e.FLOAT, reversed), new SortField("score", SortField.Type_e.SCORE) };
                //TODO: Add Shuffle extension
                //Collections.Shuffle(Arrays.AsList(fields), Random());
                int numSorts = TestUtil.NextInt(Random(), 1, fields.Length);
                AssertQuery(query, filter, new Sort(Arrays.CopyOfRange(fields, 0, numSorts)));
            }
        }

        
        internal virtual void AssertQuery(Query query, Filter filter, Sort sort)
        {
            int size = TestUtil.NextInt(Random(), 1, searcher.IndexReader.MaxDoc / 5);
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
                    Expression expr = JavascriptCompiler.Compile(s.Field);
                    SimpleBindings simpleBindings = new SimpleBindings();
                    simpleBindings.Add(s);
                    bool reverse = s.Type == SortField.Type_e.SCORE || s.Reverse;
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
            CheckHits.CheckEqual(query, expected.ScoreDocs, actual.ScoreDocs);
            if (size < actual.TotalHits)
            {
                expected = searcher.SearchAfter(expected.ScoreDocs[size - 1], query, filter, size
                    , sort);
                actual = searcher.SearchAfter(actual.ScoreDocs[size - 1], query, filter, size, mutatedSort
                    );
                CheckHits.CheckEqual(query, expected.ScoreDocs, actual.ScoreDocs);
            }
        }
    }
}
