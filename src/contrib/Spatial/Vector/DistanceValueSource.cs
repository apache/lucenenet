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

using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Function;
using Lucene.Net.Spatial.Util;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Shapes.Impl;

namespace Lucene.Net.Spatial.Vector
{
	/// <summary>
	/// An implementation of the Lucene ValueSource model to support spatial relevance ranking.
	/// </summary>
	public class DistanceValueSource : ValueSource
	{
		private TwoDoublesStrategy strategy;
		private readonly Point from;
		private readonly DistanceCalculator calculator;

		public DistanceValueSource(TwoDoublesStrategy strategy, Point from, DistanceCalculator calc)
		{
			this.strategy = strategy;
			this.from = from;
			this.calculator = calc;
		}

		public class DistanceDocValues : DocValues
		{
			private readonly DistanceValueSource enclosingInstance;

			private readonly double[] ptX, ptY;
			private readonly IBits validX, validY;

			public DistanceDocValues(DistanceValueSource enclosingInstance, IndexReader reader)
			{
				this.enclosingInstance = enclosingInstance;

				ptX = FieldCache_Fields.DEFAULT.GetDoubles(reader, enclosingInstance.strategy.GetFieldNameX()/*, true*/);
				ptY = FieldCache_Fields.DEFAULT.GetDoubles(reader, enclosingInstance.strategy.GetFieldNameY()/*, true*/);
				validX = FieldCache_Fields.DEFAULT.GetDocsWithField(reader, enclosingInstance.strategy.GetFieldNameX());
				validY = FieldCache_Fields.DEFAULT.GetDocsWithField(reader, enclosingInstance.strategy.GetFieldNameY());
			}

			public override float FloatVal(int doc)
			{
				return (float)DoubleVal(doc);
			}

			public override double DoubleVal(int doc)
			{
				// make sure it has minX and area
				if (validX.Get(doc) && validY.Get(doc))
				{
					return enclosingInstance.calculator.Distance(enclosingInstance.from, ptX[doc], ptY[doc]);
				}
				return 0;
			}

			public override string ToString(int doc)
			{
				return enclosingInstance.Description() + "=" + FloatVal(doc);
			}
		}

		public override DocValues GetValues(IndexReader reader)
		{
			return new DistanceDocValues(this, reader);
		}

		public override string Description()
		{
			return "DistanceValueSource(" + calculator + ")";
		}

		public override bool Equals(object o)
		{
			if (this == o) return true;

			var that = o as DistanceValueSource;
			if (that == null) return false;

			if (calculator != null ? !calculator.Equals(that.calculator) : that.calculator != null) return false;
			if (strategy != null ? !strategy.Equals(that.strategy) : that.strategy != null) return false;
			if (from != null ? !from.Equals(that.from) : that.from != null) return false;

			return true;
		}

		public override int GetHashCode()
		{
			int result = strategy != null ? strategy.GetHashCode() : 0;
			result = 31 * result + (calculator != null ? calculator.GetHashCode() : 0);
			result = 31 * result + (from != null ? from.GetHashCode() : 0);
			return result;
		}
	}
}
