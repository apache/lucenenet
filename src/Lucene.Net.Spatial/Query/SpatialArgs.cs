using Spatial4n.Context;
using Spatial4n.Shapes;
using System;

namespace Lucene.Net.Spatial.Queries
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
    /// Principally holds the query <see cref="IShape"/> and the <see cref="SpatialOperation"/>.
    /// It's used as an argument to some methods on <see cref="SpatialStrategy"/>.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class SpatialArgs
    {
        public static readonly double DEFAULT_DISTERRPCT = 0.025d;

        private SpatialOperation operation;
        private IShape shape;
        private double? distErrPct;
        private double? distErr;

        public SpatialArgs(SpatialOperation operation, IShape shape)
        {
            // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            this.operation = operation ?? throw new ArgumentNullException(nameof(operation), "operation and shape are required");
            this.shape = shape ?? throw new ArgumentNullException(nameof(shape), "operation and shape are required");
        }

        /// <summary>
        /// Computes the distance given a shape and the <paramref name="distErrPct"/>.  The
        /// algorithm is the fraction of the distance from the center of the query
        /// shape to its furthest bounding box corner.
        /// </summary>
        /// <param name="shape">Mandatory.</param>
        /// <param name="distErrPct">0 to 0.5</param>
        /// <param name="ctx">Mandatory</param>
        /// <returns>A distance (in degrees).</returns>
        public static double CalcDistanceFromErrPct(IShape shape, double distErrPct, SpatialContext ctx)
        {
            // LUCENENET: Added null guard clauses
            if (shape is null)
                throw new ArgumentNullException(nameof(shape));
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));

            if (distErrPct < 0 || distErrPct > 0.5)
            {
                throw new ArgumentOutOfRangeException(nameof(distErrPct), $"distErrPct {distErrPct} must be between [0 to 0.5]", nameof(distErrPct));// LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if (distErrPct == 0 || shape is IPoint)
            {
                return 0;
            }
            IRectangle bbox = shape.BoundingBox;

            //Compute the distance from the center to a corner.  Because the distance
            // to a bottom corner vs a top corner can vary in a geospatial scenario,
            // take the closest one (greater precision).
            IPoint ctr = bbox.Center;
            double y = (ctr.Y >= 0 ? bbox.MaxY : bbox.MinY);
            double diagonalDist = ctx.DistanceCalculator.Distance(ctr, bbox.MaxX, y);
            return diagonalDist * distErrPct;
        }

        /// <summary>
        /// Gets the error distance that specifies how precise the query shape is. This
        /// looks at <see cref="DistErr"/>, <see cref="DistErrPct"/>, and 
        /// <paramref name="defaultDistErrPct"/>.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="defaultDistErrPct">0 to 0.5</param>
        /// <returns>&gt;= 0</returns>
        public virtual double ResolveDistErr(SpatialContext ctx, double defaultDistErrPct)
        {
            if (DistErr != null)
                return DistErr.Value;
            // LUCENENET specific - added guard clause
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));
            double distErrPct = (this.distErrPct ?? defaultDistErrPct);
            return CalcDistanceFromErrPct(Shape, distErrPct, ctx);
        }

        /// <summary>
        /// Check if the arguments make sense -- throw an exception if not
        /// </summary>
        public virtual void Validate()
        {
            if (Operation.IsTargetNeedsArea && !Shape.HasArea)
            {
                throw new ArgumentException(Operation + " only supports geometry with area");
            }

            if (DistErr != null && DistErrPct != null)
            {
                throw new ArgumentException("Only DistErr or DistErrPct can be specified.");
            }
        }

        public override string ToString()
        {
            return SpatialArgsParser.WriteSpatialArgs(this);
        }

        //------------------------------------------------
        // Getters & Setters
        //------------------------------------------------

        public virtual SpatialOperation Operation
        {
            get => operation;
            set => operation = value ?? throw new ArgumentNullException(nameof(Operation)); // LUCENENET specific - added guard clause
        }

        public virtual IShape Shape
        {
            get => shape;
            set => shape = value ?? throw new ArgumentNullException(nameof(Shape)); // LUCENENET specific - added guard clause
        }

        /// <summary>
        /// A measure of acceptable error of the shape as a fraction. This effectively
        /// inflates the size of the shape but should not shrink it.
        /// </summary>
        /// <returns>0 to 0.5</returns>
        public virtual double? DistErrPct
        {
            get => distErrPct;
            set
            {
                if (value != null)
                {
                    distErrPct = value;
                }
            }
        }

        /// <summary>
        /// The acceptable error of the shape.  This effectively inflates the
        /// size of the shape but should not shrink it.
        /// </summary>
        /// <returns>&gt;= 0</returns>
        public virtual double? DistErr
        {
            get => distErr;
            set => distErr = value;
        }
    }
}
