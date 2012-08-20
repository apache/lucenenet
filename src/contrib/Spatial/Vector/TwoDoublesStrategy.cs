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
using Spatial4n.Core.Exceptions;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Spatial.Vector
{
	public class TwoDoublesStrategy : SpatialStrategy
	{
		public static String SUFFIX_X = "__x";
		public static String SUFFIX_Y = "__y";

		private readonly String fieldNameX;
		private readonly String fieldNameY;

		public int precisionStep = 8; // same as solr default

		public TwoDoublesStrategy(SpatialContext ctx, String fieldNamePrefix)
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

		public override bool IsPolyField()
		{
			return true;
		}

		public override AbstractField[] CreateFields(Shape shape)
		{
			var point = shape as Point;
			if (point != null)
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
			if (!ignoreIncompatibleGeometry)
			{
				throw new ArgumentException("TwoDoublesStrategy can not index: " + shape);
			}
			return new AbstractField[0]; // nothing (solr does not support null) 
		}

		public override Field CreateField(Shape shape)
		{
			throw new InvalidOperationException("Point is poly field");
		}

		public override ValueSource MakeValueSource(SpatialArgs args)
		{
			Point p = args.GetShape().GetCenter();
			return new DistanceValueSource(this, p, ctx.GetDistCalc());
		}

		public override Query MakeQuery(SpatialArgs args)
		{
			// For starters, just limit the bbox
			var shape = args.GetShape();
			if (!(shape is Rectangle || shape is Circle))
				throw new InvalidShapeException("Only Rectangles and Circles are currently supported, found ["
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
				var circle = args.GetShape() as Circle;
				if (circle != null)
				{
					// Make the ValueSource
					valueSource = MakeValueSource(args);

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
				valueSource = MakeValueSource(args);
			}
			Query spatialRankingQuery = new FunctionQuery(valueSource);
			var bq = new BooleanQuery();
			bq.Add(spatial, Occur.MUST);
			bq.Add(spatialRankingQuery, Occur.MUST);
			return bq;

		}

		public override Filter MakeFilter(SpatialArgs args)
		{
			var circle = args.GetShape() as Circle;
			if (circle != null)
			{
				if (SpatialOperation.Is(args.Operation,
					SpatialOperation.Intersects,
					SpatialOperation.IsWithin))
				{
					Query bbox = MakeWithin(circle.GetBoundingBox());

					// Make the ValueSource
					ValueSource valueSource = MakeValueSource(args);

					return new ValueSourceFilter(
						new QueryWrapperFilter(bbox), valueSource, 0, circle.GetRadius());
				}
			}
			return new QueryWrapperFilter(MakeQuery(args));
		}

		/// <summary>
		/// Constructs a query to retrieve documents that fully contain the input envelope.
		/// </summary>
		/// <param name="bbox"></param>
		/// <param name="fieldInfo"></param>
		/// <returns>The spatial query</returns>
		private Query MakeWithin(Rectangle bbox)
		{
			Query qX = NumericRangeQuery.NewDoubleRange(
			  fieldNameX,
			  precisionStep,
			  bbox.GetMinX(),
			  bbox.GetMaxX(),
			  true,
			  true);
			Query qY = NumericRangeQuery.NewDoubleRange(
			  fieldNameY,
			  precisionStep,
			  bbox.GetMinY(),
			  bbox.GetMaxY(),
			  true,
			  true);

			var bq = new BooleanQuery {{qX, Occur.MUST}, {qY, Occur.MUST}};
			return bq;
		}

		/// <summary>
		/// Constructs a query to retrieve documents that fully contain the input envelope.
		/// </summary>
		/// <param name="bbox"></param>
		/// <param name="fieldInfo"></param>
		/// <returns>The spatial query</returns>
		Query MakeDisjoint(Rectangle bbox)
		{
			Query qX = NumericRangeQuery.NewDoubleRange(
			  fieldNameX,
			  precisionStep,
			  bbox.GetMinX(),
			  bbox.GetMaxX(),
			  true,
			  true);
			Query qY = NumericRangeQuery.NewDoubleRange(
			  fieldNameY,
			  precisionStep,
			  bbox.GetMinY(),
			  bbox.GetMaxY(),
			  true,
			  true);

			var bq = new BooleanQuery {{qX, Occur.MUST_NOT}, {qY, Occur.MUST_NOT}};
			return bq;
		}

	}
}
