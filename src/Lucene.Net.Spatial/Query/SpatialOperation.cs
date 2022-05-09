using Spatial4n.Shapes;
using System;
using System.Collections.Generic;
using System.Globalization;
using JCG = J2N.Collections.Generic;

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
    /// A clause that compares a stored geometry to a supplied geometry. For more
    /// explanation of each operation, consider looking at the source implementation
    /// of <see cref="Evaluate(IShape, IShape)"/>.
    /// <para>
    /// See <a href="http://edndoc.esri.com/arcsde/9.1/general_topics/understand_spatial_relations.htm">
    /// ESRIs docs on spatial relations</a>
    /// </para>
    /// @lucene.experimental
    /// </summary>
    public abstract class SpatialOperation
    {
        // Private registry
        private static readonly IDictionary<string, SpatialOperation> registry = new Dictionary<string, SpatialOperation>();
        private static readonly IList<SpatialOperation> list = new JCG.List<SpatialOperation>();

        // Geometry Operations

        /// <summary>
        /// Bounding box of the *indexed* shape.
        /// </summary>
        public static readonly SpatialOperation BBoxIntersects = new BBoxIntersectsSpatialOperation();

        private sealed class BBoxIntersectsSpatialOperation : SpatialOperation
        {
            internal BBoxIntersectsSpatialOperation()
                : base("BBoxIntersects", true, false, false)
            { }

            public override bool Evaluate(IShape indexedShape, IShape queryShape)
            {
                // LUCENENET specific - added guard clauses
                if (indexedShape is null)
                    throw new ArgumentNullException(nameof(indexedShape));
                if (queryShape is null)
                    throw new ArgumentNullException(nameof(queryShape));

                return indexedShape.BoundingBox.Relate(queryShape).Intersects();
            }
        }

        /// <summary>
        /// Bounding box of the *indexed* shape.
        /// </summary>
        public static readonly SpatialOperation BBoxWithin = new BBoxWithinSpatialOperation();

        private sealed class BBoxWithinSpatialOperation : SpatialOperation
        {
            internal BBoxWithinSpatialOperation()
                : base("BBoxWithin", true, false, false)
            { }

            public override bool Evaluate(IShape indexedShape, IShape queryShape)
            {
                // LUCENENET specific - added guard clauses
                if (indexedShape is null)
                    throw new ArgumentNullException(nameof(indexedShape));
                if (queryShape is null)
                    throw new ArgumentNullException(nameof(queryShape));

                IRectangle bbox = indexedShape.BoundingBox;
                return bbox.Relate(queryShape) == SpatialRelation.Within || bbox.Equals(queryShape);
            }
        }

        public static readonly SpatialOperation Contains = new ContainsSpatialOperation();

        private sealed class ContainsSpatialOperation : SpatialOperation
        {
            internal ContainsSpatialOperation()
                : base("Contains", true, true, false)
            { }

            public override bool Evaluate(IShape indexedShape, IShape queryShape)
            {
                // LUCENENET specific - added guard clauses
                if (indexedShape is null)
                    throw new ArgumentNullException(nameof(indexedShape));
                if (queryShape is null)
                    throw new ArgumentNullException(nameof(queryShape));

                return indexedShape.HasArea && indexedShape.Relate(queryShape) == SpatialRelation.Contains || indexedShape.Equals(queryShape);
            }
        }

        public static readonly SpatialOperation Intersects = new IntersectsSpatialOperation();

        private sealed class IntersectsSpatialOperation : SpatialOperation
        {
            internal IntersectsSpatialOperation()
                : base("Intersects", true, false, false)
            { }

            public override bool Evaluate(IShape indexedShape, IShape queryShape)
            {
                // LUCENENET specific - added guard clauses
                if (indexedShape is null)
                    throw new ArgumentNullException(nameof(indexedShape));
                if (queryShape is null)
                    throw new ArgumentNullException(nameof(queryShape));

                return indexedShape.Relate(queryShape).Intersects();
            }
        }

        public static readonly SpatialOperation IsEqualTo = new IsEqualToSpatialOperation();

        private sealed class IsEqualToSpatialOperation : SpatialOperation
        {
            internal IsEqualToSpatialOperation()
                : base("IsEqualTo", false, false, false)
            { }

            public override bool Evaluate(IShape indexedShape, IShape queryShape)
            {
                // LUCENENET specific - added guard clauses
                if (indexedShape is null)
                    throw new ArgumentNullException(nameof(indexedShape));
                if (queryShape is null)
                    throw new ArgumentNullException(nameof(queryShape));

                return indexedShape.Equals(queryShape);
            }
        }

        public static readonly SpatialOperation IsDisjointTo = new IsDisjointToSpatialOperation();

        private sealed class IsDisjointToSpatialOperation : SpatialOperation
        {
            internal IsDisjointToSpatialOperation()
                : base("IsDisjointTo", false, false, false)
            { }

            public override bool Evaluate(IShape indexedShape, IShape queryShape)
            {
                // LUCENENET specific - added guard clauses
                if (indexedShape is null)
                    throw new ArgumentNullException(nameof(indexedShape));
                if (queryShape is null)
                    throw new ArgumentNullException(nameof(queryShape));

                return !indexedShape.Relate(queryShape).Intersects();
            }
        }

        public static readonly SpatialOperation IsWithin = new IsWithinSpatialOperation();

        private sealed class IsWithinSpatialOperation : SpatialOperation
        {
            internal IsWithinSpatialOperation()
                : base("IsWithin", true, false, true)
            { }

            public override bool Evaluate(IShape indexedShape, IShape queryShape)
            {
                // LUCENENET specific - added guard clauses
                if (indexedShape is null)
                    throw new ArgumentNullException(nameof(indexedShape));
                if (queryShape is null)
                    throw new ArgumentNullException(nameof(queryShape));

                return queryShape.HasArea && (indexedShape.Relate(queryShape) == SpatialRelation.Within || indexedShape.Equals(queryShape));
            }
        }

        public static readonly SpatialOperation Overlaps = new OverlapsSpatialOperation();

        private sealed class OverlapsSpatialOperation : SpatialOperation
        {
            internal OverlapsSpatialOperation()
                : base("Overlaps", true, false, true)
            { }

            public override bool Evaluate(IShape indexedShape, IShape queryShape)
            {
                // LUCENENET specific - added guard clauses
                if (indexedShape is null)
                    throw new ArgumentNullException(nameof(indexedShape));
                if (queryShape is null)
                    throw new ArgumentNullException(nameof(queryShape));

                return queryShape.HasArea && indexedShape.Relate(queryShape).Intersects();
            }
        }

        // Member variables
        private readonly bool scoreIsMeaningful;
        private readonly bool sourceNeedsArea;
        private readonly bool targetNeedsArea;
        private readonly string name;


        protected SpatialOperation(string name, bool scoreIsMeaningful, bool sourceNeedsArea, bool targetNeedsArea)
        {
            this.name = name;
            this.scoreIsMeaningful = scoreIsMeaningful;
            this.sourceNeedsArea = sourceNeedsArea;
            this.targetNeedsArea = targetNeedsArea;
            registry[name] = this;
            registry[CultureInfo.InvariantCulture.TextInfo.ToUpper(name)] = this;
            list.Add(this);
        }

        public static SpatialOperation Get(string v)
        {
            if (!registry.TryGetValue(v, out SpatialOperation? op) || op is null)
            {
                if (!registry.TryGetValue(CultureInfo.InvariantCulture.TextInfo.ToUpper(v), out op) || op is null)
                    throw new ArgumentException($"Unknown Operation: {v}", nameof(v));
            }
            return op;
        }

        public static IList<SpatialOperation> Values => list;

        public static bool Is(SpatialOperation op, params SpatialOperation[] tst)
        {
            foreach (SpatialOperation t in tst)
            {
                if (op == t)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns whether the relationship between <paramref name="indexedShape"/> and <paramref name="queryShape"/> is
        /// satisfied by this operation.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="indexedShape"/> or <paramref name="queryShape"/> is <c>null</c>.</exception>
        public abstract bool Evaluate(IShape indexedShape, IShape queryShape);

        // ================================================= Getters / Setters =============================================

        public virtual bool IsScoreMeaningful => scoreIsMeaningful;

        public virtual bool SourceNeedsArea => sourceNeedsArea;

        public virtual bool IsTargetNeedsArea => targetNeedsArea;

        public virtual string Name => name;

        public override string ToString()
        {
            return name;
        }
    }
}
