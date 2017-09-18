using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Documents;
using Lucene.Net.Spatial;
using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes;
using System;
using System.Collections.Generic;
using Console = Lucene.Net.Support.SystemConsole;

namespace Lucene.Net.Benchmarks.ByTask.Feeds
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

    /// <summary>
    /// Indexes spatial data according to a configured <see cref="SpatialStrategy"/> with optional
    /// shape transformation via a configured <see cref="IShapeConverter"/>. The converter can turn points into
    /// circles and bounding boxes, in order to vary the type of indexing performance tests.
    /// Unless it's subclass-ed to do otherwise, this class configures a <see cref="SpatialContext"/>,
    /// <see cref="SpatialPrefixTree"/>, and <see cref="RecursivePrefixTreeStrategy"/>. The Strategy is made
    /// available to a query maker via the static method <see cref="GetSpatialStrategy(int)"/>.
    /// See spatial.alg for a listing of spatial parameters, in particular those starting with "spatial."
    /// and "doc.spatial".
    /// </summary>
    public class SpatialDocMaker : DocMaker
    {
        public static readonly string SPATIAL_FIELD = "spatial";

        //cache spatialStrategy by round number
        private static IDictionary<int, SpatialStrategy> spatialStrategyCache = new Dictionary<int, SpatialStrategy>();

        private SpatialStrategy strategy;
        private IShapeConverter shapeConverter;

        /// <summary>
        /// Looks up the <see cref="SpatialStrategy"/> from the given round --
        /// <see cref="Config.RoundNumber"/>. It's an error
        /// if it wasn't created already for this round -- when <see cref="SpatialDocMaker"/> is initialized.
        /// </summary>
        public static SpatialStrategy GetSpatialStrategy(int roundNumber)
        {
            SpatialStrategy result;
            if (!spatialStrategyCache.TryGetValue(roundNumber, out result) || result == null)
            {
                throw new InvalidOperationException("Strategy should have been init'ed by SpatialDocMaker by now");
            }
            return result;
        }

        /// <summary>
        /// Builds a <see cref="SpatialStrategy"/> from configuration options.
        /// </summary>
        protected virtual SpatialStrategy MakeSpatialStrategy(Config config)
        {
            //A Map view of Config that prefixes keys with "spatial."
            var configMap = new DictionaryAnonymousHelper(config);

            SpatialContext ctx = SpatialContextFactory.MakeSpatialContext(configMap /*, null*/); // LUCENENET TODO: What is this extra param?

            //Some day the strategy might be initialized with a factory but such a factory
            // is non-existent.
            return MakeSpatialStrategy(config, configMap, ctx);
        }

        private class DictionaryAnonymousHelper : Dictionary<string, string>
        {
            private readonly Config config;
            public DictionaryAnonymousHelper(Config config)
            {
                this.config = config;
            }

            // LUCENENET TODO: EntrySet not supported. Should we throw on GetEnumerator()?

            new public string this[string key]
            {
                get { return config.Get("spatial." + key, null); }
            }
        }

        protected virtual SpatialStrategy MakeSpatialStrategy(Config config, IDictionary<string, string> configMap,
                                                      SpatialContext ctx)
        {
            //A factory for the prefix tree grid
            SpatialPrefixTree grid = SpatialPrefixTreeFactory.MakeSPT(configMap, /*null,*/ ctx); // LUCENENET TODO: What is this extra param?

            RecursivePrefixTreeStrategy strategy = new RecursivePrefixTreeStrategyAnonymousHelper(grid, SPATIAL_FIELD, config);

            int prefixGridScanLevel = config.Get("query.spatial.prefixGridScanLevel", -4);
            if (prefixGridScanLevel < 0)
                prefixGridScanLevel = grid.MaxLevels + prefixGridScanLevel;
            strategy.PrefixGridScanLevel = prefixGridScanLevel;

            double distErrPct = config.Get("spatial.distErrPct", .025);//doc & query; a default
            strategy.DistErrPct = distErrPct;
            return strategy;
        }

        private class RecursivePrefixTreeStrategyAnonymousHelper : RecursivePrefixTreeStrategy
        {
            public RecursivePrefixTreeStrategyAnonymousHelper(SpatialPrefixTree grid, string fieldName, Config config)
                : base(grid, fieldName)
            {
                this.m_pointsOnly = config.Get("spatial.docPointsOnly", false);
            }
        }

        public override void SetConfig(Config config, ContentSource source)
        {
            base.SetConfig(config, source);
            SpatialStrategy existing;
            if (!spatialStrategyCache.TryGetValue(config.RoundNumber, out existing) || existing == null)
            {
                //new round; we need to re-initialize
                strategy = MakeSpatialStrategy(config);
                spatialStrategyCache[config.RoundNumber] = strategy;
                //TODO remove previous round config?
                shapeConverter = MakeShapeConverter(strategy, config, "doc.spatial.");
                Console.WriteLine("Spatial Strategy: " + strategy);
            }
        }

        /// <summary>
        /// Optionally converts points to circles, and optionally bbox'es result.
        /// </summary>
        public static IShapeConverter MakeShapeConverter(SpatialStrategy spatialStrategy,
                                                        Config config, string configKeyPrefix)
        {
            //by default does no conversion
            double radiusDegrees = config.Get(configKeyPrefix + "radiusDegrees", 0.0);
            double plusMinus = config.Get(configKeyPrefix + "radiusDegreesRandPlusMinus", 0.0);
            bool bbox = config.Get(configKeyPrefix + "bbox", false);

            return new ShapeConverterAnonymousHelper(spatialStrategy, radiusDegrees, plusMinus, bbox);
        }

        private class ShapeConverterAnonymousHelper : IShapeConverter
        {
            private readonly SpatialStrategy spatialStrategy;
            private readonly double radiusDegrees;
            private readonly double plusMinus;
            private readonly bool bbox;

            public ShapeConverterAnonymousHelper(SpatialStrategy spatialStrategy, double radiusDegrees, double plusMinus, bool bbox)
            {
                this.spatialStrategy = spatialStrategy;
                this.radiusDegrees = radiusDegrees;
                this.plusMinus = plusMinus;
                this.bbox = bbox;
            }

            public IShape Convert(IShape shape)
            {
                if (shape is IPoint && (radiusDegrees != 0.0 || plusMinus != 0.0))
                {
                    IPoint point = (IPoint)shape;
                    double radius = radiusDegrees;
                    if (plusMinus > 0.0)
                    {
                        Random random = new Random(point.GetHashCode());//use hashCode so it's reproducibly random
                        radius += random.NextDouble() * 2 * plusMinus - plusMinus;
                        radius = Math.Abs(radius);//can happen if configured plusMinus > radiusDegrees
                    }
                    shape = spatialStrategy.SpatialContext.MakeCircle(point, radius);
                }
                if (bbox)
                {
                    shape = shape.BoundingBox;
                }
                return shape;
            }
        }

        // LUCENENET specific: de-nested IShapeConverter

        public override Document MakeDocument()
        {

            DocState docState = GetDocState();

            Document doc = base.MakeDocument();

            // Set SPATIAL_FIELD from body
            DocData docData = docState.docData;
            //   makeDocument() resets docState.getBody() so we can't look there; look in Document
            string shapeStr = doc.GetField(DocMaker.BODY_FIELD).GetStringValue();
            IShape shape = MakeShapeFromString(strategy, docData.Name, shapeStr);
            if (shape != null)
            {
                shape = shapeConverter.Convert(shape);
                //index
                foreach (Field f in strategy.CreateIndexableFields(shape))
                {
                    doc.Add(f);
                }
            }

            return doc;
        }

        public static IShape MakeShapeFromString(SpatialStrategy strategy, string name, string shapeStr)
        {
            if (shapeStr != null && shapeStr.Length > 0)
            {
                try
                {
                    return strategy.SpatialContext.ReadShapeFromWkt(shapeStr);
                }
                catch (Exception e)
                {//InvalidShapeException TODO
                    Console.Error.WriteLine("Shape " + name + " wasn't parseable: " + e + "  (skipping it)");
                    return null;
                }
            }
            return null;
        }

        public override Document MakeDocument(int size)
        {
            //TODO consider abusing the 'size' notion to number of shapes per document
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts one shape to another. Created by
    /// <see cref="SpatialDocMaker.MakeShapeConverter(SpatialStrategy, Config, string)"/>.
    /// </summary>
    public interface IShapeConverter
    {
        IShape Convert(IShape shape);
    }
}
