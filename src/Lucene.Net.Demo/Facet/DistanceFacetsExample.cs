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

// Add NuGet References:

// Lucene.Net.Analysis.Common
// Lucene.Net.Expressions
// Lucene.Net.Facet

using J2N;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Documents;
using Lucene.Net.Expressions;
using Lucene.Net.Expressions.JS;
using Lucene.Net.Facet;
using Lucene.Net.Facet.Range;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Index;
using Lucene.Net.Queries;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Diagnostics;
using System.Globalization;

namespace Lucene.Net.Demo.Facet
{
    /// <summary>
    /// Shows simple usage of dynamic range faceting, using the
    /// expressions module to calculate distance.
    /// </summary>
    public sealed class DistanceFacetsExample : IDisposable
    {
        /// <summary>
        /// Using a constant for all functionality related to a specific index
        /// is the best strategy. This allows you to upgrade Lucene.Net first
        /// and plan the upgrade of the index binary format for a later time. 
        /// Once the index is upgraded, you simply need to update the constant 
        /// version and redeploy your application.
        /// </summary>
        private const LuceneVersion EXAMPLE_VERSION = LuceneVersion.LUCENE_48;

        internal static readonly DoubleRange ONE_KM = new DoubleRange("< 1 km", 0.0, true, 1.0, false);
        internal static readonly DoubleRange TWO_KM = new DoubleRange("< 2 km", 0.0, true, 2.0, false);
        internal static readonly DoubleRange FIVE_KM = new DoubleRange("< 5 km", 0.0, true, 5.0, false);
        internal static readonly DoubleRange TEN_KM = new DoubleRange("< 10 km", 0.0, true, 10.0, false);

        private readonly Directory indexDir = new RAMDirectory();
        private IndexSearcher searcher;
        private readonly FacetsConfig config = new FacetsConfig();

        /// <summary>The "home" latitude.</summary>
        public readonly static double ORIGIN_LATITUDE = 40.7143528;

        /// <summary>The "home" longitude.</summary>
        public readonly static double ORIGIN_LONGITUDE = -74.0059731;

        /// <summary>
        /// Radius of the Earth in KM
        /// <para/>
        /// NOTE: this is approximate, because the earth is a bit
        /// wider at the equator than the poles.  See
        /// http://en.wikipedia.org/wiki/Earth_radius
        /// </summary>
        public readonly static double EARTH_RADIUS_KM = 6371.01;

        /// <summary>Build the example index.</summary>
        public void Index()
        {
            using IndexWriter writer = new IndexWriter(indexDir, new IndexWriterConfig(EXAMPLE_VERSION,
                new WhitespaceAnalyzer(EXAMPLE_VERSION)));
            // TODO: we could index in radians instead ... saves all the conversions in GetBoundingBoxFilter

            // Add documents with latitude/longitude location:
            writer.AddDocument(new Document
                {
                    new DoubleField("latitude", 40.759011, Field.Store.NO),
                    new DoubleField("longitude", -73.9844722, Field.Store.NO)
                });

            writer.AddDocument(new Document
                {
                    new DoubleField("latitude", 40.718266, Field.Store.NO),
                    new DoubleField("longitude", -74.007819, Field.Store.NO)
                });

            writer.AddDocument(new Document
                {
                    new DoubleField("latitude", 40.7051157, Field.Store.NO),
                    new DoubleField("longitude", -74.0088305, Field.Store.NO)
                });

            // Open near-real-time searcher
            searcher = new IndexSearcher(DirectoryReader.Open(writer, true));
        }

        private static ValueSource GetDistanceValueSource()
        {
            Expression distance = JavascriptCompiler.Compile(
                string.Format(CultureInfo.InvariantCulture, "haversin({0:R},{1:R},latitude,longitude)", ORIGIN_LATITUDE, ORIGIN_LONGITUDE));

            SimpleBindings bindings = new SimpleBindings
            {
                new SortField("latitude", SortFieldType.DOUBLE),
                new SortField("longitude", SortFieldType.DOUBLE),
            };

            return distance.GetValueSource(bindings);
        }

        /// <summary>
        /// Given a latitude and longitude (in degrees) and the
        /// maximum great circle (surface of the earth) distance,
        /// returns a simple Filter bounding box to "fast match"
        /// candidates.
        /// </summary>
        public static Filter GetBoundingBoxFilter(double originLat, double originLng, double maxDistanceKM)
        {
            // Basic bounding box geo math from
            // http://JanMatuschek.de/LatitudeLongitudeBoundingCoordinates,
            // licensed under creative commons 3.0:
            // http://creativecommons.org/licenses/by/3.0

            // TODO: maybe switch to recursive prefix tree instead
            // (in lucene/spatial)?  It should be more efficient
            // since it's a 2D trie...

            // Degrees -> Radians:
            double originLatRadians = originLat.ToRadians();
            double originLngRadians = originLng.ToRadians();

            double angle = maxDistanceKM / (SloppyMath.EarthDiameter(originLat) / 2.0);

            double minLat = originLatRadians - angle;
            double maxLat = originLatRadians + angle;

            double minLng;
            double maxLng;
            if (minLat > -90.ToRadians() && maxLat < 90.ToRadians())
            {
                double delta = Math.Asin(Math.Sin(angle) / Math.Cos(originLatRadians));
                minLng = originLngRadians - delta;
                if (minLng < -180.ToRadians())
                {
                    minLng += 2 * Math.PI;
                }
                maxLng = originLngRadians + delta;
                if (maxLng > 180.ToRadians())
                {
                    maxLng -= 2 * Math.PI;
                }
            }
            else
            {
                // The query includes a pole!
                minLat = Math.Max(minLat, -90.ToRadians());
                maxLat = Math.Min(maxLat, 90.ToRadians());
                minLng = -180.ToRadians();
                maxLng = 180.ToRadians();
            }

            BooleanFilter f = new BooleanFilter
            {
                // Add latitude range filter:
                {
                    NumericRangeFilter.NewDoubleRange("latitude", minLat.ToDegrees(), maxLat.ToDegrees(), true, true),
                    Occur.MUST
                }
            };

            // Add longitude range filter:
            if (minLng > maxLng)
            {
                // The bounding box crosses the international date
                // line:
                BooleanFilter lonF = new BooleanFilter
                {
                    {
                        NumericRangeFilter.NewDoubleRange("longitude", minLng.ToDegrees(), null, true, true),
                        Occur.SHOULD
                    },
                    {
                        NumericRangeFilter.NewDoubleRange("longitude", null, maxLng.ToDegrees(), true, true),
                        Occur.SHOULD
                    }
                };
                f.Add(lonF, Occur.MUST);
            }
            else
            {
                f.Add(NumericRangeFilter.NewDoubleRange("longitude", minLng.ToDegrees(), maxLng.ToDegrees(), true, true),
                      Occur.MUST);
            }

            return f;
        }

        /// <summary>User runs a query and counts facets.</summary>
        public FacetResult Search()
        {
            FacetsCollector fc = new FacetsCollector();

            searcher.Search(new MatchAllDocsQuery(), fc);

            Facets facets = new DoubleRangeFacetCounts("field", GetDistanceValueSource(), fc,
                                                       GetBoundingBoxFilter(ORIGIN_LATITUDE, ORIGIN_LONGITUDE, 10.0),
                                                       ONE_KM,
                                                       TWO_KM,
                                                       FIVE_KM,
                                                       TEN_KM);

            return facets.GetTopChildren(10, "field");
        }

        /// <summary>User drills down on the specified range.</summary>
        public TopDocs DrillDown(DoubleRange range)
        {
            // Passing no baseQuery means we drill down on all
            // documents ("browse only"):
            DrillDownQuery q = new DrillDownQuery(null);
            ValueSource vs = GetDistanceValueSource();
            q.Add("field", range.GetFilter(GetBoundingBoxFilter(ORIGIN_LATITUDE, ORIGIN_LONGITUDE, range.Max), vs));
            DrillSideways ds = new SearchDrillSideways(searcher, config, vs);
            return ds.Search(q, 10).Hits;
        }

        private class SearchDrillSideways : DrillSideways
        {
            private readonly ValueSource vs;

            public SearchDrillSideways(IndexSearcher indexSearcher, FacetsConfig facetsConfig, ValueSource valueSource)
                : base(indexSearcher, facetsConfig, (TaxonomyReader)null)
            {
                this.vs = valueSource;
            }

            protected override Facets BuildFacetsResult(FacetsCollector drillDowns, FacetsCollector[] drillSideways, string[] drillSidewaysDims)
            {
                Debug.Assert(drillSideways.Length == 1);
                return new DoubleRangeFacetCounts("field", vs, drillSideways[0], ONE_KM, TWO_KM, FIVE_KM, TEN_KM);
            }
        }

        public void Dispose()
        {
            searcher?.IndexReader?.Dispose();
            indexDir?.Dispose();
        }

        /// <summary>Runs the search and drill-down examples and prints the results.</summary>
        public static void Main(string[] args)
        {
            using DistanceFacetsExample example = new DistanceFacetsExample();
            example.Index();

            Console.WriteLine("Distance facet counting example:");
            Console.WriteLine("-----------------------");
            Console.WriteLine(example.Search());

            Console.WriteLine("\n");
            Console.WriteLine("Distance facet drill-down example (field/< 2 km):");
            Console.WriteLine("---------------------------------------------");
            TopDocs hits = example.DrillDown(TWO_KM);
            Console.WriteLine(hits.TotalHits + " totalHits");
        }
    }
}
