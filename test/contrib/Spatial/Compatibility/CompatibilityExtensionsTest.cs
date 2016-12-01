using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Function;
using Lucene.Net.Spatial;
using Lucene.Net.Spatial.Queries;
using Lucene.Net.Spatial.Util;
using Lucene.Net.Spatial.Vector;
using NUnit.Framework;
using Spatial4n.Core.Context;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Contrib.Spatial.Test.Compatibility
{
    public class CompatibilityExtensionsTest : SpatialTestCase
    {
        private const string StrategyPrefix = "pointvector_";

        private const int NumDocsToCreate = 100000;

        private readonly SpatialStrategy _spatialStrategy = new PointVectorStrategy(SpatialContext.GEO, StrategyPrefix);

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            GenerateRandomDocs(new Random(DateTime.Now.Millisecond), NumDocsToCreate); //create enough docs to ensure we will be spreading the documents over multiple reader segments.
        }

        private void GenerateRandomDocs(Random rng, int numDocs)
        {
            SpatialContext ctx = _spatialStrategy.GetSpatialContext();
            base.addDocumentsAndCommit(Enumerable.Range(1, numDocs)
                .Select(a => CreateRandomDoc(a, rng, ctx)).ToList());
        }

        private Document CreateRandomDoc(int docId, Random rng, SpatialContext ctx)
        {
            Point shape = ctx.MakePoint(DistanceUtils.NormLonDEG(rng.NextDouble() * 360.0), DistanceUtils.NormLatDEG(rng.NextDouble() * 180.0));

            var doc = new Document();

            var idField = new NumericField("locationId", Field.Store.YES, true);
            idField.SetIntValue(docId);

            doc.Add(idField);

            foreach (AbstractField field in _spatialStrategy.CreateIndexableFields(shape))
            {
                doc.Add(field);
            }

            doc.Add(_spatialStrategy.CreateStoredField(shape));

            return doc;
        }


        [Test(Description = "Tests the CompatibilityExtensions ensuring that the _docsWithFieldCache is valid for indices with multiple segments")]
        public void TestCompatibilityExtensionsCacheSupportsMultipleReaderSegments()
        {
            SearchNearDoc((int)(NumDocsToCreate * 0.25), 10);
            SearchNearDoc((int)(NumDocsToCreate * 0.5), 10);
            SearchNearDoc((int)(NumDocsToCreate * 0.75), 10);

            var cache = (IDictionary)typeof(CompatibilityExtensions).GetField("_docsWithFieldCache", BindingFlags.NonPublic | BindingFlags.Static)
                .GetValue(null);


            Assert.Greater(cache.Count, 2);// there will 2 cache entries for each reader segment (prefix__x and prefix__y). We need to ensure there were more than 2 entries or there was only one segment cached and the test is void

        }

        private void SearchNearDoc(int docId, int distanceInKm)
        {
            SpatialArgs args = GetArgs(docId, distanceInKm);

            var booleanQuery = new BooleanQuery
                               {
                                   {_spatialStrategy.MakeQuery(args), Occur.MUST}
                               };



            TopDocs topDocs = indexSearcher.Search(booleanQuery, 10);

            Assert.GreaterOrEqual(topDocs.ScoreDocs.Length, 1); //Search area is centered on a doc so at least one doc should be returned
        }

        private SpatialArgs GetArgs(int docId, int distanceInKms)
        {
            Document doc = base.indexSearcher.IndexReader.Document(docId);

            SpatialContext ctx = _spatialStrategy.GetSpatialContext();

            string[] parts = doc.Get(StrategyPrefix)
                .Split(' ');

            Point pt = ctx.MakePoint(double.Parse(parts[0]),
                                     double.Parse(parts[1]));

            Circle circle = ctx.MakeCircle(pt, DistanceUtils.Dist2Degrees(distanceInKms, DistanceUtils.EARTH_MEAN_RADIUS_KM));

            var args = new SpatialArgs(SpatialOperation.Intersects, circle);

            return args;
        }


        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }
    }
}