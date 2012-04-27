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
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Search.Function;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Spatial.Util
{
	/// <summary>
	/// An implementation of the Lucene ValueSource model to support spatial relevance ranking.
	/// </summary>
	public class CachedDistanceValueSource : ValueSource
	{
		private readonly ShapeFieldCacheProvider<Point> provider;
		private readonly DistanceCalculator calculator;
		private readonly Point from;

		public CachedDistanceValueSource(Point from, DistanceCalculator calc, ShapeFieldCacheProvider<Point> provider)
		{
			this.from = from;
			this.provider = provider;
			this.calculator = calc;
		}

		public override DocValues GetValues(IndexReader reader)
		{
			ShapeFieldCache<Point> cache = provider.GetCache(reader);

			//return new FunctionValues() {
			//  @Override
			//  public float floatVal(int doc) {
			//    return (float) doubleVal(doc);
			//  }

			//  @Override
			//  public double doubleVal(int doc) {
			//    IList<Point> vals = cache.getShapes( doc );
			//    if( vals != null ) {
			//      double v = calculator.distance(from, vals.get(0));
			//      for( int i=1; i<vals.size(); i++ ) {
			//        v = Math.min(v, calculator.distance(from, vals.get(i)));
			//      }
			//      return v;
			//    }
			//    return Double.NaN; // ?? maybe max?
			//  }

			//  @Override
			//  public String toString(int doc) {
			//    return description() + "=" + floatVal(doc);
			//  }
			//};

		}

		public override string Description()
		{
			return "DistanceValueSource(" + calculator + ")";
		}

		public override bool Equals(object o)
		{
			if (this == o) return true;

			var that = o as CachedDistanceValueSource;

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
