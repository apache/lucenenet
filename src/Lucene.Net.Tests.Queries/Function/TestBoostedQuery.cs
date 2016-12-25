using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.ValueSources;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Tests.Queries.Function
{
    /// <summary>
    /// Basic tests for <seealso cref="BoostedQuery"/>
    /// </summary>
    // TODO: more tests
    public class TestBoostedQuery : LuceneTestCase
    {
        internal static Directory dir;
        internal static IndexReader ir;
        internal static IndexSearcher @is;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            dir = NewDirectory();
            IndexWriterConfig iwConfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            iwConfig.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwConfig);
            Document document = new Document();
            Field idField = new StringField("id", "", Field.Store.NO);
            document.Add(idField);
            iw.AddDocument(document);
            ir = iw.Reader;
            @is = NewSearcher(ir);
            iw.Dispose();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            @is = null;
            ir.Dispose();
            ir = null;
            dir.Dispose();
            dir = null;
        }
        
        [Test]
        public virtual void TestBasic()
        {
            Query q = new MatchAllDocsQuery();
            TopDocs docs = @is.Search(q, 10);
            assertEquals(1, docs.TotalHits);
            float score = docs.ScoreDocs[0].Score;

            Query boostedQ = new BoostedQuery(q, new ConstValueSource(2.0f));
            AssertHits(boostedQ, new float[] { score * 2 });
        }


        private void AssertHits(Query q, float[] scores)
        {
            ScoreDoc[] expected = new ScoreDoc[scores.Length];
            int[] expectedDocs = new int[scores.Length];
            for (int i = 0; i < expected.Length; i++)
            {
                expectedDocs[i] = i;
                expected[i] = new ScoreDoc(i, scores[i]);
            }
            TopDocs docs = @is.Search(q, 10, new Sort(new SortField("id", SortFieldType.STRING)));
            CheckHits.DoCheckHits(Random(), q, "", @is, expectedDocs, Similarity);
            CheckHits.CheckHitsQuery(q, expected, docs.ScoreDocs, expectedDocs);
            CheckHits.CheckExplanations(q, "", @is);
        }
    }
}