using Lucene.Net.Documents;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.ValueSources;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Queries;
using Spatial4n.Context;
using Spatial4n.Shapes;
using System;

namespace Lucene.Net.Spatial
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
    /// The <see cref="SpatialStrategy"/> encapsulates an approach to indexing and searching based on shapes.
    /// <para/>
    /// Different implementations will support different features. A strategy should
    /// document these common elements:
    /// <list type="bullet">
    ///     <item><description>Can it index more than one shape per field?</description></item>
    ///     <item><description>What types of shapes can be indexed?</description></item>
    ///     <item><description>What types of query shapes can be used?</description></item>
    ///     <item><description>What types of query operations are supported? This might vary per shape.</description></item>
    ///     <item><description>Does it use the <see cref="FieldCache"/>, or some other type of cache?  When?</description></item>
    /// </list>
    /// If a strategy only supports certain shapes at index or query time, then in
    /// general it will throw an exception if given an incompatible one.  It will not
    /// be coerced into compatibility.
    /// <para/>
    /// Note that a SpatialStrategy is not involved with the Lucene stored field values of shapes, which is
    /// immaterial to indexing and search.
    /// <para/>
    /// Thread-safe.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public abstract class SpatialStrategy
    {
        protected readonly SpatialContext m_ctx;
        private readonly string fieldName;

        /// <summary>
        /// Constructs the spatial strategy with its mandatory arguments.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="ctx"/> or <paramref name="fieldName"/> is <c>null</c> or <paramref name="fieldName"/> is empty.</exception>
        protected SpatialStrategy(SpatialContext ctx, string fieldName)
        {
            this.m_ctx = ctx ?? throw new ArgumentNullException(nameof(ctx), "ctx is required");// LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            if (string.IsNullOrEmpty(fieldName))
                throw new ArgumentNullException(nameof(fieldName), "fieldName is required");// LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            this.fieldName = fieldName;
        }

        public virtual SpatialContext SpatialContext => m_ctx;

        /// <summary>
        /// The name of the field or the prefix of them if there are multiple
        /// fields needed internally.
        /// </summary>
        /// <returns>Not null.</returns>
        public virtual string FieldName => fieldName;

        /// <summary>
        /// Returns the IndexableField(s) from the <paramref name="shape"/> that are to be
        /// added to the <see cref="Document"/>.  These fields
        /// are expected to be marked as indexed and not stored.
        /// <p/>
        /// Note: If you want to <i>store</i> the shape as a string for retrieval in search
        /// results, you could add it like this:
        /// <code>
        ///     document.Add(new StoredField(fieldName, ctx.ToString(shape)));
        /// </code>
        /// The particular string representation used doesn't matter to the Strategy since it
        /// doesn't use it.
        /// </summary>
        /// <param name="shape"></param>
        /// <returns>Not null nor will it have null elements.</returns>
        /// <exception cref="NotSupportedException">if given a shape incompatible with the strategy</exception>
        public abstract Field[] CreateIndexableFields(IShape shape);

        /// <summary>
        /// See <see cref="MakeDistanceValueSource(IPoint, double)"/> called with
        /// a multiplier of 1.0 (i.e. units of degrees).
        /// </summary>
        public virtual ValueSource MakeDistanceValueSource(IPoint queryPoint)
        {
            return MakeDistanceValueSource(queryPoint, 1.0);
        }

        /// <summary>
        /// Make a ValueSource returning the distance between the center of the
        /// indexed shape and <paramref name="queryPoint"/>.  If there are multiple indexed shapes
        /// then the closest one is chosen. The result is multiplied by <paramref name="multiplier"/>, which
        /// conveniently is used to get the desired units.
        /// </summary>
        public abstract ValueSource MakeDistanceValueSource(IPoint queryPoint, double multiplier);

        /// <summary>
        /// Make a Query based principally on <see cref="SpatialOperation"/>
        /// and <see cref="IShape"/> from the supplied <paramref name="args"/>.
        /// The default implementation is
        /// <code>return new ConstantScoreQuery(MakeFilter(args));</code>
        /// </summary>
        /// <exception cref="NotSupportedException">If the strategy does not support the shape in <paramref name="args"/>.</exception>
        /// <exception cref="UnsupportedSpatialOperationException">If the strategy does not support the <see cref="SpatialOperation"/> in <paramref name="args"/>.</exception>
        public virtual ConstantScoreQuery MakeQuery(SpatialArgs args)
        {
            return new ConstantScoreQuery(MakeFilter(args));
        }

        /// <summary>
        /// Make a Filter based principally on <see cref="SpatialOperation"/>
        /// and <see cref="IShape"/> from the supplied <paramref name="args"/>.
        /// <para />
        /// If a subclasses implements
        /// <see cref="MakeQuery(SpatialArgs)"/>
        /// then this method could be simply:
        /// <code>return new QueryWrapperFilter(MakeQuery(args).Query);</code>
        /// </summary>
        /// <exception cref="NotSupportedException">If the strategy does not support the shape in <paramref name="args"/>.</exception>
        /// <exception cref="UnsupportedSpatialOperationException">If the strategy does not support the <see cref="SpatialOperation"/> in <paramref name="args"/>.</exception>
        public abstract Filter MakeFilter(SpatialArgs args);

        /// <summary>
        /// Returns a ValueSource with values ranging from 1 to 0, depending inversely
        /// on the distance from <see cref="MakeDistanceValueSource(IPoint)"/>.
        /// The formula is <c>c / (d + c)</c> where 'd' is the distance and 'c' is
        /// one tenth the distance to the farthest edge from the center. Thus the
        /// scores will be 1 for indexed points at the center of the query shape and as
        /// low as ~0.1 at its furthest edges.
        /// </summary>
        public ValueSource MakeRecipDistanceValueSource(IShape queryShape)
        {
            // LUCENENET specific - added guard clause
            if (queryShape is null)
                throw new ArgumentNullException(nameof(queryShape));

            IRectangle bbox = queryShape.BoundingBox;
            double diagonalDist = m_ctx.DistanceCalculator.Distance(
                m_ctx.MakePoint(bbox.MinX, bbox.MinY), bbox.MaxX, bbox.MaxY);
            double distToEdge = diagonalDist * 0.5;
            float c = (float)distToEdge * 0.1f; //one tenth
            return new ReciprocalSingleFunction(MakeDistanceValueSource(queryShape.Center, 1.0), 1f, c, c);
        }

        public override string ToString()
        {
            return GetType().Name + " field:" + fieldName + " ctx=" + m_ctx;
        }
    }
}
