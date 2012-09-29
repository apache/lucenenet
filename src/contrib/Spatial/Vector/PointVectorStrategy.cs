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
using Lucene.Net.Search;
using Lucene.Net.Search.Function;
using Lucene.Net.Spatial.Queries;
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

		public void SetPrecisionStep(int p)
		{
			precisionStep = p;
			if (precisionStep <= 0 || precisionStep >= 64)
				precisionStep = int.MaxValue;
		}

		public string GetFieldNameX()
		{
			return fieldNameX;
		}

		public string GetFieldNameY()
		{
			return fieldNameY;
		}

		public override AbstractField[] CreateIndexableFields(Shape shape)
		{
		    var point = shape as Point;
		    if (point != null)
		        return CreateIndexableFields(point);

		    throw new InvalidOperationException("Can only index Point, not " + shape);
		}

        public AbstractField[] CreateIndexableFields(Point point)
        {
				var f = new AbstractField[2];

				var f0 = new NumericField(fieldNameX, precisionStep, Field.Store.NO, true)
				         	{OmitNorms = true, OmitTermFreqAndPositions = true};
				f0.SetDoubleValue(point.GetX());
				f[0] = f0;

				var f1 = new NumericField(fieldNameY, precisionStep, Field.Store.NO, true)
				         	{OmitNorms = true, OmitTermFreqAndPositions = true};
				f1.SetDoubleValue(point.GetY());
				f[1] = f1;

				return f;
		}

		public override ValueSource MakeDistanceValueSource(Point queryPoint)
		{
            return new DistanceValueSource(this, queryPoint);
		}

        public override ConstantScoreQuery MakeQuery(SpatialArgs args)
        {
            if (!SpatialOperation.Is(args.Operation,
                                     SpatialOperation.Intersects,
                                     SpatialOperation.IsWithin))
                throw new UnsupportedSpatialOperation(args.Operation);

            Shape shape = args.Shape;
            var bbox = shape as Rectangle;
            if (bbox != null)
                return new ConstantScoreQuery(new QueryWrapperFilter(MakeWithin(bbox)));

            var circle = shape as Circle;
            if (circle != null)
            {
                bbox = circle.GetBoundingBox();
                var vsf = new ValueSourceFilter(
                    new QueryWrapperFilter(MakeWithin(bbox)),
                    MakeDistanceValueSource(circle.GetCenter()),
                    0,
                    circle.GetRadius());
                return new ConstantScoreQuery(vsf);
            }
            
            throw new InvalidOperationException("Only Rectangles and Circles are currently supported, " +
                                            "found [" + shape.GetType().Name + "]"); //TODO
        }

	    //TODO this is basically old code that hasn't been verified well and should probably be removed
        public Query MakeQueryDistanceScore(SpatialArgs args)
        {
	        // For starters, just limit the bbox
			var shape = args.Shape;
			if (!(shape is Rectangle || shape is Circle))
				throw new InvalidOperationException("Only Rectangles and Circles are currently supported, found ["
					+ shape.GetType().Name + "]");//TODO

			Rectangle bbox = shape.GetBoundingBox();
			if (bbox.GetCrossesDateLine())
			{
				throw new InvalidOperationException("Crossing dateline not yet supported");
			}

			ValueSource valueSource = null;

			Query spatial = null;
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
				var circle = args.Shape as Circle;
				if (circle != null)
				{
					// Make the ValueSource
                    valueSource = MakeDistanceValueSource(shape.GetCenter());

					var vsf = new ValueSourceFilter(
						new QueryWrapperFilter(spatial), valueSource, 0, circle.GetRadius());

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
                valueSource = MakeDistanceValueSource(shape.GetCenter());
			}
			Query spatialRankingQuery = new FunctionQuery(valueSource);
			var bq = new BooleanQuery();
			bq.Add(spatial, Occur.MUST);
			bq.Add(spatialRankingQuery, Occur.MUST);
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
        private Query MakeWithin(Rectangle bbox)
	    {
	        var bq = new BooleanQuery();
	        const Occur MUST = Occur.MUST;
	        if (bbox.GetCrossesDateLine())
	        {
	            //use null as performance trick since no data will be beyond the world bounds
	            bq.Add(RangeQuery(fieldNameX, null /*-180*/, bbox.GetMaxX()), Occur.SHOULD);
	            bq.Add(RangeQuery(fieldNameX, bbox.GetMinX(), null /*+180*/), Occur.SHOULD);
	            bq.MinimumNumberShouldMatch = 1; //must match at least one of the SHOULD
	        }
	        else
	        {
	            bq.Add(RangeQuery(fieldNameX, bbox.GetMinX(), bbox.GetMaxX()), MUST);
	        }
	        bq.Add(RangeQuery(fieldNameY, bbox.GetMinY(), bbox.GetMaxY()), MUST);
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
        private Query MakeDisjoint(Rectangle bbox)
	    {
	        if (bbox.GetCrossesDateLine())
	            throw new InvalidOperationException("MakeDisjoint doesn't handle dateline cross");
	        Query qX = RangeQuery(fieldNameX, bbox.GetMinX(), bbox.GetMaxX());
	        Query qY = RangeQuery(fieldNameY, bbox.GetMinY(), bbox.GetMaxY());
	        var bq = new BooleanQuery {{qX, Occur.MUST_NOT}, {qY, Occur.MUST_NOT}};
	        return bq;
	    }
	}
}
