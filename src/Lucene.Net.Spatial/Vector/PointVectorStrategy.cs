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

using System;
using Lucene.Net.Documents;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Query;
using Lucene.Net.Spatial.Util;
using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Spatial.Vector
{
    /// <summary>
    /// Simple {@link SpatialStrategy} which represents Points in two numeric {@link DoubleField}s.
    /// 
    /// Note, currently only Points can be indexed by this Strategy.  At query time, the bounding
    /// box of the given Shape is used to create {@link NumericRangeQuery}s to efficiently
    /// find Points within the Shape.
    /// 
    /// Due to the simple use of numeric fields, this Strategy provides support for sorting by
    /// distance through {@link DistanceValueSource}
    /// </summary>
    public class PointVectorStrategy : SpatialStrategy
    {
        public static String SUFFIX_X = "__x";
        public static String SUFFIX_Y = "__y";

        private readonly String fieldNameX;
        private readonly String fieldNameY;

        public int precisionStep = 8; // same as solr default

        public PointVectorStrategy(SpatialContext ctx, String fieldNamePrefix)
            : base(ctx, fieldNamePrefix)
        {
            this.fieldNameX = fieldNamePrefix + SUFFIX_X;
            this.fieldNameY = fieldNamePrefix + SUFFIX_Y;
        }

        public int PrecisionStep
        {
            set
            {
                precisionStep = value;
                if (precisionStep <= 0 || precisionStep >= 64)
                    precisionStep = int.MaxValue;
            }
        }

        public string FieldNameX
        {
            get { return fieldNameX; }
        }

        public string FieldNameY
        {
            get { return fieldNameY; }
        }

        public override Field[] CreateIndexableFields(IShape shape)
        {
            var point = shape as IPoint;
            if (point != null)
                return CreateIndexableFields(point);

            throw new InvalidOperationException("Can only index Point, not " + shape);
        }

        public Field[] CreateIndexableFields(IPoint point)
        {
            FieldType doubleFieldType = new FieldType(DoubleField.TYPE_NOT_STORED)
                                            {
                                                NumericPrecisionStep = precisionStep
                                            };
            var f = new Field[]
                        {
                            new DoubleField(fieldNameX, point.X, doubleFieldType),
                            new DoubleField(fieldNameY, point.Y, doubleFieldType)
                        };
            return f;
        }

        public override ValueSource MakeDistanceValueSource(IPoint queryPoint, double multiplier)
        {
            return new DistanceValueSource(this, queryPoint, multiplier);
        }

        public override ConstantScoreQuery MakeQuery(SpatialArgs args)
        {
            if (!SpatialOperation.Is(args.Operation,
                                     SpatialOperation.Intersects,
                                     SpatialOperation.IsWithin))
                throw new UnsupportedSpatialOperation(args.Operation);

            IShape shape = args.Shape;
            var bbox = shape as IRectangle;
            if (bbox != null)
                return new ConstantScoreQuery(new QueryWrapperFilter(MakeWithin(bbox)));

            var circle = shape as ICircle;
            if (circle != null)
            {
                bbox = circle.BoundingBox;
                var vsf = new ValueSourceFilter(
                    new QueryWrapperFilter(MakeWithin(bbox)),
                    MakeDistanceValueSource(circle.Center),
                    0,
                    circle.Radius);
                return new ConstantScoreQuery(vsf);
            }

            throw new InvalidOperationException("Only Rectangles and Circles are currently supported, " +
                                            "found [" + shape.GetType().Name + "]"); //TODO
        }

        //TODO this is basically old code that hasn't been verified well and should probably be removed
        public Search.Query MakeQueryDistanceScore(SpatialArgs args)
        {
            // For starters, just limit the bbox
            var shape = args.Shape;
            if (!(shape is IRectangle || shape is ICircle))
                throw new InvalidOperationException("Only Rectangles and Circles are currently supported, found ["
                    + shape.GetType().Name + "]");//TODO

            IRectangle bbox = shape.BoundingBox;
            if (bbox.CrossesDateLine)
            {
                throw new InvalidOperationException("Crossing dateline not yet supported");
            }

            ValueSource valueSource = null;

            Search.Query spatial = null;
            SpatialOperation op = args.Operation;

            if (SpatialOperation.Is(op,
                SpatialOperation.BBoxWithin,
                SpatialOperation.BBoxIntersects))
            {
                spatial = MakeWithin(bbox);
            }
            else if (SpatialOperation.Is(op,
              SpatialOperation.Intersects,
              SpatialOperation.IsWithin))
            {
                spatial = MakeWithin(bbox);
                var circle = args.Shape as ICircle;
                if (circle != null)
                {
                    // Make the ValueSource
                    valueSource = MakeDistanceValueSource(shape.Center);

                    var vsf = new ValueSourceFilter(
                        new QueryWrapperFilter(spatial), valueSource, 0, circle.Radius);

                    spatial = new FilteredQuery(new MatchAllDocsQuery(), vsf);
                }
            }
            else if (op == SpatialOperation.IsDisjointTo)
            {
                spatial = MakeDisjoint(bbox);
            }

            if (spatial == null)
            {
                throw new UnsupportedSpatialOperation(args.Operation);
            }

            if (valueSource != null)
            {
                valueSource = new CachingDoubleValueSource(valueSource);
            }
            else
            {
                valueSource = MakeDistanceValueSource(shape.Center);
            }
            Search.Query spatialRankingQuery = new FunctionQuery(valueSource);
            var bq = new BooleanQuery();
            bq.Add(spatial, BooleanClause.Occur.MUST);
            bq.Add(spatialRankingQuery, BooleanClause.Occur.MUST);
            return bq;

        }

        public override Filter MakeFilter(SpatialArgs args)
        {
            //unwrap the CSQ from makeQuery
            ConstantScoreQuery csq = MakeQuery(args);
            Filter filter = csq.Filter;
            if (filter != null)
                return filter;
            else
                return new QueryWrapperFilter(csq);
        }

        /// <summary>
        /// Constructs a query to retrieve documents that fully contain the input envelope.
        /// </summary>
        /// <param name="bbox"></param>
        private Search.Query MakeWithin(IRectangle bbox)
        {
            var bq = new BooleanQuery();
            const BooleanClause.Occur MUST = BooleanClause.Occur.MUST;
            if (bbox.CrossesDateLine)
            {
                //use null as performance trick since no data will be beyond the world bounds
                bq.Add(RangeQuery(fieldNameX, null /*-180*/, bbox.MaxX), BooleanClause.Occur.SHOULD);
                bq.Add(RangeQuery(fieldNameX, bbox.MinX, null /*+180*/), BooleanClause.Occur.SHOULD);
                bq.MinimumNumberShouldMatch = 1; //must match at least one of the SHOULD
            }
            else
            {
                bq.Add(RangeQuery(fieldNameX, bbox.MinX, bbox.MaxX), MUST);
            }
            bq.Add(RangeQuery(fieldNameY, bbox.MinY, bbox.MaxY), MUST);
            return bq;
        }

        private NumericRangeQuery<Double> RangeQuery(String fieldName, double? min, double? max)
        {
            return NumericRangeQuery.NewDoubleRange(
                fieldName,
                precisionStep,
                min,
                max,
                true,
                true); //inclusive
        }

        /// <summary>
        /// Constructs a query to retrieve documents that fully contain the input envelope.
        /// </summary>
        /// <param name="bbox"></param>
        private Search.Query MakeDisjoint(IRectangle bbox)
        {
            if (bbox.CrossesDateLine)
                throw new InvalidOperationException("MakeDisjoint doesn't handle dateline cross");
            Search.Query qX = RangeQuery(fieldNameX, bbox.MinX, bbox.MaxX);
            Search.Query qY = RangeQuery(fieldNameY, bbox.MinY, bbox.MaxY);
            var bq = new BooleanQuery { { qX, BooleanClause.Occur.MUST_NOT }, { qY, BooleanClause.Occur.MUST_NOT } };
            return bq;
        }
    }
}
