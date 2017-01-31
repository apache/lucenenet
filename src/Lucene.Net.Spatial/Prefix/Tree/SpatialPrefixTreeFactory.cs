using Spatial4n.Core.Context;
using Spatial4n.Core.Distance;
using System;
using System.Collections.Generic;
using System.Globalization;

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
    /// 
    /// @lucene.experimental
    /// </summary>
    public abstract class SpatialPrefixTreeFactory
    {
        private const double DEFAULT_GEO_MAX_DETAIL_KM = 0.001;//1m
        public const string PREFIX_TREE = "prefixTree";
        public const string MAX_LEVELS = "maxLevels";
        public const string MAX_DIST_ERR = "maxDistErr";

        protected IDictionary<string, string> m_args;
        protected SpatialContext m_ctx;
        protected int? m_maxLevels;

        /// <summary>The factory  is looked up via "prefixTree" in args, expecting "geohash" or "quad".</summary>
        /// <remarks>
        /// The factory  is looked up via "prefixTree" in args, expecting "geohash" or "quad".
        /// If its neither of these, then "geohash" is chosen for a geo context, otherwise "quad" is chosen.
        /// </remarks>
        public static SpatialPrefixTree MakeSPT(IDictionary<string, string> args, SpatialContext ctx)
        {
            SpatialPrefixTreeFactory instance;
            string cname;
            if (!args.TryGetValue(PREFIX_TREE, out cname))
            {
                cname = ctx.IsGeo ? "geohash" : "quad";
            }
            if ("geohash".Equals(cname, StringComparison.OrdinalIgnoreCase))
            {
                instance = new GeohashPrefixTree.Factory();
            }
            else if ("quad".Equals(cname, StringComparison.OrdinalIgnoreCase))
            {
                instance = new QuadPrefixTree.Factory();
            }
            else
            {
                try
                {
                    Type c = Type.GetType(cname);
                    instance = (SpatialPrefixTreeFactory)Activator.CreateInstance(c);
                }
                catch (Exception e)
                {
                    throw new SpatialPrefixTreeFactoryException(string.Empty, e);
                }
            }
            instance.Init(args, ctx);
            return instance.NewSPT();
        }

        protected internal virtual void Init(IDictionary<string, string> args, SpatialContext ctx)
        {
            this.m_args = args;
            this.m_ctx = ctx;
            InitMaxLevels();
        }

        protected internal virtual void InitMaxLevels()
        {
            string mlStr;
            if (m_args.TryGetValue(MAX_LEVELS, out mlStr))
            {
                m_maxLevels = int.Parse(mlStr, CultureInfo.InvariantCulture);
                return;
            }
            double degrees;
            string maxDetailDistStr;
            if (!m_args.TryGetValue(MAX_DIST_ERR, out maxDetailDistStr))
            {
                if (!m_ctx.IsGeo)
                {
                    return;
                }
                //let default to max
                degrees = DistanceUtils.Dist2Degrees(DEFAULT_GEO_MAX_DETAIL_KM, DistanceUtils.EARTH_MEAN_RADIUS_KM);
            }
            else
            {
                degrees = double.Parse(maxDetailDistStr, CultureInfo.InvariantCulture);
            }
            m_maxLevels = GetLevelForDistance(degrees);
        }

        /// <summary>
        /// Calls <see cref="SpatialPrefixTree.GetLevelForDistance(double)">SpatialPrefixTree.GetLevelForDistance(double)</see>.
        /// </summary>
        protected internal abstract int GetLevelForDistance(double degrees);

        protected internal abstract SpatialPrefixTree NewSPT();
    }

    /// <summary>
    /// LUCENENET: Exception thrown when an operation fails in
    /// SpatialPrefixTreeFactory. Replaces generic ApplicationException that is
    /// not supported on .NET Core.
    /// </summary>
    public class SpatialPrefixTreeFactoryException : Exception
    {
        public SpatialPrefixTreeFactoryException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
