using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Documents;
using Lucene.Net.Spatial;
using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
using Spatial4n.Context;
using Spatial4n.Shapes;
using System;
using System.Collections;
using System.Collections.Generic;
using Console = Lucene.Net.Util.SystemConsole;

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
        private static readonly IDictionary<int, SpatialStrategy> spatialStrategyCache = new Dictionary<int, SpatialStrategy>(); // LUCENENET: marked readonly

        private SpatialStrategy strategy;
        private IShapeConverter shapeConverter;

        /// <summary>
        /// Looks up the <see cref="SpatialStrategy"/> from the given round --
        /// <see cref="Config.RoundNumber"/>. It's an error
        /// if it wasn't created already for this round -- when <see cref="SpatialDocMaker"/> is initialized.
        /// </summary>
        public static SpatialStrategy GetSpatialStrategy(int roundNumber)
        {
            if (!spatialStrategyCache.TryGetValue(roundNumber, out SpatialStrategy result) || result is null)
            {
                throw IllegalStateException.Create("Strategy should have been init'ed by SpatialDocMaker by now");
            }
            return result;
        }

        /// <summary>
        /// Builds a <see cref="SpatialStrategy"/> from configuration options.
        /// </summary>
        protected virtual SpatialStrategy MakeSpatialStrategy(Config config)
        {
            //A Map view of Config that prefixes keys with "spatial."
            var configMap = new DictionaryAnonymousClass(config);

            // LUCENENET: The second argument was ClassLoader in Java, which should be made into
            // Assembly in .NET. However, Spatial4n currently doesn't support it.
            // In .NET it makes more logical sense to make 2 overloads and throw ArgumentNullException
            // if the second argument is null, anyway. So no need to change this once support has been added.
            // See: https://github.com/NightOwl888/Spatial4n/issues/1
            SpatialContext ctx = SpatialContextFactory.MakeSpatialContext(configMap /*, assembly: null*/);

            //Some day the strategy might be initialized with a factory but such a factory
            // is non-existent.
            return MakeSpatialStrategy(config, configMap, ctx);
        }

        // LUCENENET specific: since this[string] is not virtual in .NET, this full implementation
        // of IDictionary<string, string> is required to override methods to get a value by key
        private sealed class DictionaryAnonymousClass : IDictionary<string, string>
        {
            private readonly Config config;
            public DictionaryAnonymousClass(Config config)
            {
                this.config = config;
            }

            public string this[string key]
            {
                get => config.Get("spatial." + key, null);
                set => throw UnsupportedOperationException.Create();
            }

            public bool TryGetValue(string key, out string value)
            {
                value = config.Get("spatial." + key, null);
                return value != null;
            }

            public bool ContainsKey(string key)
            {
                const string notSupported = "notsupported";
                var value = config.Get("spatial." + key, notSupported);
                return !value.Equals(notSupported, StringComparison.Ordinal);
            }

            #region IDictionary<string, string> members

            ICollection<string> IDictionary<string, string>.Keys => throw UnsupportedOperationException.Create();

            ICollection<string> IDictionary<string, string>.Values => throw UnsupportedOperationException.Create();

            int ICollection<KeyValuePair<string, string>>.Count => throw UnsupportedOperationException.Create();

            public bool IsReadOnly => true;

            void IDictionary<string, string>.Add(string key, string value) => throw UnsupportedOperationException.Create();
            void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> item) => throw UnsupportedOperationException.Create();
            void ICollection<KeyValuePair<string, string>>.Clear() => throw UnsupportedOperationException.Create();
            bool ICollection<KeyValuePair<string, string>>.Contains(KeyValuePair<string, string> item) => throw UnsupportedOperationException.Create();
            
            void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => throw UnsupportedOperationException.Create();
            IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator() => throw UnsupportedOperationException.Create();
            bool IDictionary<string, string>.Remove(string key) => throw UnsupportedOperationException.Create();
            bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> item) => throw UnsupportedOperationException.Create();
            
            IEnumerator IEnumerable.GetEnumerator() => throw UnsupportedOperationException.Create();

            #endregion IDictionary<string, string> members
        }

        protected virtual SpatialStrategy MakeSpatialStrategy(Config config, IDictionary<string, string> configMap,
                                                      SpatialContext ctx)
        {
            //A factory for the prefix tree grid
            SpatialPrefixTree grid = SpatialPrefixTreeFactory.MakeSPT(configMap, assembly: null, ctx);

            RecursivePrefixTreeStrategy strategy = new RecursivePrefixTreeStrategyAnonymousClass(grid, SPATIAL_FIELD, config);

            int prefixGridScanLevel = config.Get("query.spatial.prefixGridScanLevel", -4);
            if (prefixGridScanLevel < 0)
                prefixGridScanLevel = grid.MaxLevels + prefixGridScanLevel;
            strategy.PrefixGridScanLevel = prefixGridScanLevel;

            double distErrPct = config.Get("spatial.distErrPct", .025);//doc & query; a default
            strategy.DistErrPct = distErrPct;
            return strategy;
        }

        private sealed class RecursivePrefixTreeStrategyAnonymousClass : RecursivePrefixTreeStrategy
        {
            public RecursivePrefixTreeStrategyAnonymousClass(SpatialPrefixTree grid, string fieldName, Config config)
                : base(grid, fieldName)
            {
                this.m_pointsOnly = config.Get("spatial.docPointsOnly", false);
            }
        }

        public override void SetConfig(Config config, ContentSource source)
        {
            base.SetConfig(config, source);
            if (!spatialStrategyCache.TryGetValue(config.RoundNumber, out SpatialStrategy existing) || existing is null)
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

            return new ShapeConverterAnonymousClass(spatialStrategy, radiusDegrees, plusMinus, bbox);
        }

        private sealed class ShapeConverterAnonymousClass : IShapeConverter
        {
            private readonly SpatialStrategy spatialStrategy;
            private readonly double radiusDegrees;
            private readonly double plusMinus;
            private readonly bool bbox;

            public ShapeConverterAnonymousClass(SpatialStrategy spatialStrategy, double radiusDegrees, double plusMinus, bool bbox)
            {
                this.spatialStrategy = spatialStrategy;
                this.radiusDegrees = radiusDegrees;
                this.plusMinus = plusMinus;
                this.bbox = bbox;
            }

            public IShape Convert(IShape shape)
            {
                if ((radiusDegrees != 0.0 || plusMinus != 0.0) && shape is IPoint point)
                {
                    double radius = radiusDegrees;
                    if (plusMinus > 0.0)
                    {
                        Random random = new J2N.Randomizer(point.GetHashCode());//use hashCode so it's reproducibly random
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
                catch (Exception e) when (e.IsException())
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
            throw UnsupportedOperationException.Create();
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
