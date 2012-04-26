using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Tier;
using Lucene.Net.Spatial.Tier.Projectors;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Contrib.Spatial.Test
{
	[TestFixture]
	public class Various
	{
		private Directory _directory;
		private IndexSearcher _searcher;
		private IndexWriter _writer;
		private readonly List<CartesianTierPlotter> _ctps = new List<CartesianTierPlotter>();
		private readonly IProjector _projector = new SinusoidalProjector();

		private const string LatField = "lat";
		private const string LngField = "lng";

		//[SetUp]
		protected void SetUp()
		{
			_directory = new RAMDirectory();
			_writer = new IndexWriter(_directory, new WhitespaceAnalyzer(), true, IndexWriter.MaxFieldLength.UNLIMITED);
			SetUpPlotter(2, 15);
		}

		private void AddData(IndexWriter writer)
		{
			AddPoint(writer, "Within radius", 55.6880508001, 13.5717346673);
			AddPoint(writer, "Within radius", 55.6821978456, 13.6076183965);
			AddPoint(writer, "Within radius", 55.673251569, 13.5946697607);
			AddPoint(writer, "Close but not in radius", 55.8634157297, 13.5497731987);
			AddPoint(writer, "Faar away", 40.7137578228, -74.0126901936);
		}

		private void SetUpPlotter(int @base, int top)
		{

			for (; @base <= top; @base++)
			{
				_ctps.Add(new CartesianTierPlotter(@base, _projector, CartesianTierPlotter.DefaltFieldPrefix));
			}
		}

		private void AddPoint(IndexWriter writer, String name, double lat, double lng)
		{
			Document doc = new Document();
			
			doc.Add(new Field("name", name, Field.Store.YES, Field.Index.ANALYZED));

			// convert the lat / long to lucene fields
			doc.Add(new Field(LatField, NumericUtils.DoubleToPrefixCoded(lat), Field.Store.YES, Field.Index.NOT_ANALYZED));
			doc.Add(new Field(LngField, NumericUtils.DoubleToPrefixCoded(lng), Field.Store.YES, Field.Index.NOT_ANALYZED));

			// add a default meta field to make searching all documents easy 
			doc.Add(new Field("metafile", "doc", Field.Store.YES, Field.Index.ANALYZED));

			int ctpsize = _ctps.Count;
			for (int i = 0; i < ctpsize; i++)
			{
				CartesianTierPlotter ctp = _ctps[i];
				var boxId = ctp.GetTierBoxId(lat, lng);
				doc.Add(new Field(ctp.GetTierFieldName(),
								  NumericUtils.DoubleToPrefixCoded(boxId),
								  Field.Store.YES,
								  Field.Index.NOT_ANALYZED_NO_NORMS));
			}
			writer.AddDocument(doc);
		}

		[Test]
		public void RadiusOf15Something()
		{
			SetUp();

			// Origin
			double _lat = 45.829507799999988;
			double _lng = -73.800524699999983;

			//Locations            
			AddPoint(_writer, "The location doc we are after", _lat, _lng);

			_writer.Commit();
			_writer.Close();

			_searcher = new IndexSearcher(_directory, true);

			ExecuteSearch(45.831909, -73.810322, 150000 * 0.000621, 1);
			ExecuteSearch(45.831909, -73.810322, 15000 * 0.000621, 1);
			ExecuteSearch(45.831909, -73.810322, 1500 * 0.000621, 1);

			_searcher.Close();
			_directory.Close();
		}

		private void ExecuteSearch(double lat, double lng, double radius, int expectedResults)
		{
			// create a distance query
			var dq = new DistanceQueryBuilder(lat, lng, radius, LatField, LngField, CartesianTierPlotter.DefaltFieldPrefix, true);

			Console.WriteLine(dq);
			//create a term query to search against all documents
			Query tq = new TermQuery(new Term("metafile", "doc"));

			var dsort = new DistanceFieldComparatorSource(dq.DistanceFilter);
			Sort sort = new Sort(new SortField("foo", dsort, false));

			// Perform the search, using the term query, the distance filter, and the
			// distance sort
			TopDocs hits = _searcher.Search(tq, dq.Filter, 10, sort);
			int results = hits.TotalHits;
			ScoreDoc[] scoreDocs = hits.ScoreDocs;

			// Get a list of distances
			Dictionary<int, Double> distances = dq.DistanceFilter.Distances;

			Console.WriteLine("Distance Filter filtered: " + distances.Count);
			Console.WriteLine("Results: " + results);

			Assert.AreEqual(expectedResults, distances.Count); // fixed a store of only needed distances
			Assert.AreEqual(expectedResults, results);
		}

		[Test]
		public void LUCENENET462()
		{
			SetUp();
			AddData(_writer);
	
			// Origin
			double _lat = 42.350153;
			double _lng = -71.061667;

			//Locations            
			AddPoint(_writer, "Location 1", 42.0, -71.0); //24 miles away from origin
			AddPoint(_writer, "Location 2", 42.35, -71.06); //less than a mile

			_writer.Commit();
			_writer.Close();

			_searcher = new IndexSearcher(_directory, true);

			//const double miles = 53.8; // Correct. Returns 2 Locations.
			const double miles = 52; // Incorrect. Returns 1 Location.

			Console.WriteLine("LUCENENET462");
			// create a distance query
			var dq = new DistanceQueryBuilder(_lat, _lng, miles, LatField, LngField, CartesianTierPlotter.DefaltFieldPrefix, true);

			Console.WriteLine(dq);
			//create a term query to search against all documents
			Query tq = new TermQuery(new Term("metafile", "doc"));

			var dsort = new DistanceFieldComparatorSource(dq.DistanceFilter);
			Sort sort = new Sort(new SortField("foo", dsort, false));

			// Perform the search, using the term query, the distance filter, and the
			// distance sort
			TopDocs hits = _searcher.Search(tq, dq.Filter, 1000, sort);
			int results = hits.TotalHits;
			ScoreDoc[] scoreDocs = hits.ScoreDocs;

			// Get a list of distances
			Dictionary<int, Double> distances = dq.DistanceFilter.Distances;

			Console.WriteLine("Distance Filter filtered: " + distances.Count);
			Console.WriteLine("Results: " + results);
			Console.WriteLine("=============================");
			Console.WriteLine("Distances should be 2 " + distances.Count);
			Console.WriteLine("Results should be 2 " + results);

			Assert.AreEqual(2, distances.Count); // fixed a store of only needed distances
			Assert.AreEqual(2, results);

			_directory.Close();
		}
	}
}
