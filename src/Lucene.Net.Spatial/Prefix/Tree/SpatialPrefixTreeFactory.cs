using Spatial4n.Context;
using Spatial4n.Distance;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace Lucene.Net.Spatial.Prefix.Tree
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
    /// Abstract Factory for creating <see cref="SpatialPrefixTree"/> instances with useful
    /// defaults and passed on configurations defined in a <see cref="IDictionary{TKey, TValue}"/>.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public abstract class SpatialPrefixTreeFactory
    {
        private const double DEFAULT_GEO_MAX_DETAIL_KM = 0.001;//1m
        public const string PREFIX_TREE = "prefixTree";
        public const string MAX_LEVELS = "maxLevels";
        public const string MAX_DIST_ERR = "maxDistErr";

        protected IDictionary<string, string>? m_args;
        protected SpatialContext? m_ctx;
        protected int? m_maxLevels;

        /// <summary>The factory is looked up via "prefixTree" in <paramref name="args"/>, expecting "geohash" or "quad".</summary>
        /// <remarks>
        /// The factory is looked up via "prefixTree" in <paramref name="args"/>, expecting "geohash" or "quad".
        /// If its neither of these, then "geohash" is chosen for a geo context, otherwise "quad" is chosen.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> or <paramref name="ctx"/> is <c>null</c>.</exception>
        public static SpatialPrefixTree MakeSPT(IDictionary<string, string> args, SpatialContext ctx) // LUCENENET specific overload for convenience.
            => MakeSPT(args, null, ctx);

        /// <summary>The factory is looked up via "prefixTree" in <paramref name="args"/>, expecting "geohash" or "quad".</summary>
        /// <remarks>
        /// The factory is looked up via "prefixTree" in <paramref name="args"/>, expecting "geohash" or "quad".
        /// If its neither of these, then "geohash" is chosen for a geo context, otherwise "quad" is chosen.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> or <paramref name="ctx"/> is <c>null</c>.</exception>
        public static SpatialPrefixTree MakeSPT(IDictionary<string, string> args, Assembly? assembly, SpatialContext ctx)
        {
            // LUCENENET specific - added guard clauses
            if (args is null)
                throw new ArgumentNullException(nameof(args));
            if (assembly is null)
                assembly = typeof(SpatialPrefixTreeFactory).Assembly;
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));

            SpatialPrefixTreeFactory? instance;
            if (!args.TryGetValue(PREFIX_TREE, out string? cname))
            {
                cname = ctx.IsGeo ? "geohash" : "quad";
            }
            if ("geohash".Equals(cname, StringComparison.OrdinalIgnoreCase))
            {
                instance = new GeohashPrefixTreeFactory();
            }
            else if ("quad".Equals(cname, StringComparison.OrdinalIgnoreCase))
            {
                instance = new QuadPrefixTreeFactory();
            }
            else
            {
                try
                {
                    Type? c = assembly.GetType(cname) ?? Type.GetType(cname);
                    if (c is null)
                        throw RuntimeException.Create($"{cname} not found in {assembly.GetName().FullName} or by using Type.GetType(string).");// LUCENENET specific - .NET doesn't throw when the type is not found.
                    instance = (SpatialPrefixTreeFactory)Activator.CreateInstance(c)!;
                }
                catch (Exception e) when (e.IsException())
                {
                    throw RuntimeException.Create(e);
                }
            }
            instance.Init(args, ctx);
            return instance.NewSPT();
        }

        protected internal virtual void Init(IDictionary<string, string> args, SpatialContext ctx)
        {
            // LUCENENET specific - added guard clauses
            this.m_args = args ?? throw new ArgumentNullException(nameof(args));
            this.m_ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            InitMaxLevels();
        }

        protected internal virtual void InitMaxLevels()
        {
            if (m_args is null || m_ctx is null)
                throw new InvalidOperationException($"Init(IDictionary<string, string>, SpatialContext) must be called prior to calling InitMaxLevels()");

            if (m_args.TryGetValue(MAX_LEVELS, out string? mlStr))
            {
                m_maxLevels = int.Parse(mlStr, CultureInfo.InvariantCulture);
                return;
            }
            double degrees;
            if (!m_args.TryGetValue(MAX_DIST_ERR, out string? maxDetailDistStr))
            {
                if (!m_ctx.IsGeo)
                {
                    return;
                }
                //let default to max
                degrees = DistanceUtils.Dist2Degrees(DEFAULT_GEO_MAX_DETAIL_KM, DistanceUtils.EarthMeanRadiusKilometers);
            }
            else
            {
                degrees = double.Parse(maxDetailDistStr, CultureInfo.InvariantCulture);
            }
            m_maxLevels = GetLevelForDistance(degrees);
        }

        /// <summary>
        /// Calls <see cref="SpatialPrefixTree.GetLevelForDistance(double)" />.
        /// </summary>
        protected internal abstract int GetLevelForDistance(double degrees);

        protected internal abstract SpatialPrefixTree NewSPT();
    }
}
