using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Lucene.Net.Documents;
using Lucene.Net.Search;
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
    public class FixedBitSetTest : SpatialTestCase
    {
        private const string StrategyPrefix = "pointvector_";

        private const int NumDocsToCreate = 100;

        private readonly SpatialStrategy _spatialStrategy = new PointVectorStrategy(SpatialContext.GEO, StrategyPrefix);

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            GenerateRandomDocs(new Random(DateTime.Now.Millisecond), NumDocsToCreate, 0.8); //make sure that some documents do not have spatial fields and some do. This makes the caches use FixedBitSet rather than MatchAllBits or MatchNoBits.
        }

        private void GenerateRandomDocs(Random rng, int numDocs, double percentageWithSpatialFields)
        {
            SpatialContext ctx = _spatialStrategy.GetSpatialContext();
            base.addDocumentsAndCommit(Enumerable.Range(1, numDocs)
                .Select(a => CreateRandomDoc(a, rng, ctx, percentageWithSpatialFields)).ToList());
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="docId"></param>
        /// <param name="rng"></param>
        /// <param name="ctx"></param>
        /// <param name="percentageWithSpatialFields">ensures that some documents are missing spatial fields. This forces the cache to use FixedBitSet rather than MatchAllBits or MatchNoBits</param>
        /// <returns></returns>
        private Document CreateRandomDoc(int docId, Random rng, SpatialContext ctx, double percentageWithSpatialFields)
        {
            var doc = new Document();

            var idField = new NumericField("locationId", Field.Store.YES, true);
            idField.SetIntValue(docId);

            doc.Add(idField);

            if (rng.NextDouble() > percentageWithSpatialFields)
            {
                return doc;
            }

            Point shape = ctx.MakePoint(DistanceUtils.NormLonDEG(rng.NextDouble() * 360.0), DistanceUtils.NormLatDEG(rng.NextDouble() * 180.0));

            foreach (AbstractField field in _spatialStrategy.CreateIndexableFields(shape))
            {
                doc.Add(field);
            }

            doc.Add(_spatialStrategy.CreateStoredField(shape));

            return doc;
        }


        [Test(Description = "Tests that each index in a FixedBitSet is valid")]
        public void TestFixedBitSet()
        {
            SearchNearDoc((int)(NumDocsToCreate * 0.25), 10); //populate caches

            //reflect to get the cache instance - saves changing the api
            var cache = (IDictionary)typeof(CompatibilityExtensions).GetField("_docsWithFieldCache", BindingFlags.NonPublic | BindingFlags.Static)
                .GetValue(null);

            foreach (IBits ib in cache.Values.Cast<IBits>())//ensure each IBits in the cache is a FixedBitSet
            {
                Assert.IsInstanceOf(typeof(FixedBitSet), ib);
            }

            for (var i = 0; i <= indexSearcher.MaxDoc; i++)//ensure each docId is accessible in the FixedBitSet
            {
                SearchNearDoc(i, 10);
            }

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
            Document doc;
            int index = docId;
            //we may land at a document that has no spatial field. In which case we keep increasing the index until we find one that does have a spatial field.
            do
            {
                doc = base.indexSearcher.IndexReader.Document(index % indexSearcher.MaxDoc);
                index++;
            } while (doc.GetField(StrategyPrefix) == null);

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