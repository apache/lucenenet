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
using Lucene.Net.Index;
using Lucene.Net.Search.Function;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Spatial.Util
{
	/// <summary>
	/// An implementation of the Lucene ValueSource model to support spatial relevance ranking.
	/// </summary>
	public class ShapeFieldCacheDistanceValueSource : ValueSource
	{
		private readonly ShapeFieldCacheProvider<Point> provider;
		private readonly DistanceCalculator calculator;
		private readonly Point from;

		public ShapeFieldCacheDistanceValueSource(Point from, DistanceCalculator calc, ShapeFieldCacheProvider<Point> provider)
		{
			this.from = from;
			this.provider = provider;
			this.calculator = calc;
		}

		public class CachedDistanceDocValues : DocValues
		{
			private readonly ShapeFieldCacheDistanceValueSource enclosingInstance;
			private readonly ShapeFieldCache<Point> cache;

			public CachedDistanceDocValues(ShapeFieldCache<Point> cache, ShapeFieldCacheDistanceValueSource enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
				this.cache = cache;
			}

			public override float FloatVal(int doc)
			{
				return (float)DoubleVal(doc);
			}

			public override double DoubleVal(int doc)
			{
				var vals = cache.GetShapes(doc);
				if (vals != null)
				{
					double v = enclosingInstance.calculator.Distance(enclosingInstance.from, vals[0]);
					for (int i = 1; i < vals.Count; i++)
					{
						v = Math.Min(v, enclosingInstance.calculator.Distance(enclosingInstance.from, vals[i]));
					}
					return v;
				}
				return Double.NaN; // ?? maybe max?
			}

			public override string ToString(int doc)
			{
				return enclosingInstance.Description() + "=" + FloatVal(doc);
			}
		}

		public override DocValues GetValues(IndexReader reader)
		{
			ShapeFieldCache<Point> cache = provider.GetCache(reader);
			return new CachedDistanceDocValues(cache, this);
		}

		public override string Description()
		{
			return "DistanceValueSource(" + calculator + ")";
		}

		public override bool Equals(object o)
		{
			if (this == o) return true;

			var that = o as ShapeFieldCacheDistanceValueSource;

			if (that == null) return false;
			if (calculator != null ? !calculator.Equals(that.calculator) : that.calculator != null) return false;
			if (from != null ? !from.Equals(that.from) : that.from != null) return false;

			return true;
		}

		public override int GetHashCode()
		{
			var result = calculator != null ? calculator.GetHashCode() : 0;
			result = 31 * result + (from != null ? from.GetHashCode() : 0);
			return result;
		}
	}
}
