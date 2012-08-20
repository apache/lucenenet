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
using System.Text;
using Spatial4n.Core.Context;
using Spatial4n.Core.Exceptions;
using Spatial4n.Core.Shapes;

namespace Spatial4n.Core.Exceptions
{
	public class InvalidSpatialArgument : ArgumentException
	{
		public InvalidSpatialArgument(String reason)
			: base(reason)
		{
		}
	}
}

namespace Lucene.Net.Spatial.Queries
{
	public class SpatialArgs
	{
		public static double DEFAULT_DIST_PRECISION = 0.025d;

		public SpatialOperation Operation { get; set; }

		private Shape shape;
		private double distPrecision = DEFAULT_DIST_PRECISION;

		// Useful for 'distance' calculations
		public double? Min { get; set; }
		public double? Max { get; set; }

		public SpatialArgs(SpatialOperation operation)
		{
			this.Operation = operation;
		}

		public SpatialArgs(SpatialOperation operation, Shape shape)
		{
			this.Operation = operation;
			this.shape = shape;
		}

		/// <summary>
		/// Check if the arguments make sense -- throw an exception if not
		/// </summary>
		public void Validate()
		{
			if (Operation.IsTargetNeedsArea() && !shape.HasArea())
			{
				throw new InvalidSpatialArgument(Operation + " only supports geometry with area");
			}
		}

		public override String ToString()
		{
			var str = new StringBuilder();
			str.Append(Operation.GetName()).Append('(');
			str.Append(shape.ToString());
			if (Min != null)
			{
				str.Append(" min=").Append(Min);
			}
			if (Max != null)
			{
				str.Append(" max=").Append(Max);
			}
			str.Append(" distPrec=").AppendFormat("{0:0.00}%", distPrecision / 100d);
			str.Append(')');
			return str.ToString();
		}

		//------------------------------------------------
		// Getters & Setters
		//------------------------------------------------

		/// <summary>
		/// Considers {@link SpatialOperation#BBoxWithin} in returning the shape.
		/// </summary>
		/// <returns></returns>
		public Shape GetShape()
		{
			if (shape != null && (Operation == SpatialOperation.BBoxWithin || Operation == SpatialOperation.BBoxIntersects))
				return shape.GetBoundingBox();
			return shape;
		}

		public void SetShape(Shape shape)
		{
			this.shape = shape;
		}

		/// <summary>
		/// The fraction of the distance from the center of the query shape to its nearest edge that is considered acceptable
		/// error. The algorithm for computing the distance to the nearest edge is actually a little different. It normalizes
		/// the shape to a square given it's bounding box area:
		/// <pre>sqrt(shape.bbox.area)/2</pre>
		/// And the error distance is beyond the shape such that the shape is a minimum shape.
		/// </summary>
		/// <returns></returns>
		public Double GetDistPrecision()
		{
			return distPrecision;
		}

		public void SetDistPrecision(double? distPrecision)
		{
			if (distPrecision != null)
				this.distPrecision = distPrecision.Value;
		}
	}
}
