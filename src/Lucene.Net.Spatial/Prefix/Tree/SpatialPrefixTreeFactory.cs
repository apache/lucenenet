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
    /// Abstract Factory for creating
    /// <see cref="SpatialPrefixTree">SpatialPrefixTree</see>
    /// instances with useful
    /// defaults and passed on configurations defined in a Map.
    /// </summary>
    /// <lucene.experimental></lucene.experimental>
    public abstract class SpatialPrefixTreeFactory
    {
        private const double DefaultGeoMaxDetailKm = 0.001;//1m
        public const string PrefixTree = "prefixTree";
        public const string MaxLevels = "maxLevels";
        public const string MaxDistErr = "maxDistErr";

        protected internal IDictionary<string, string> args;
        protected internal SpatialContext ctx;
        protected internal int? maxLevels;

        /// <summary>The factory  is looked up via "prefixTree" in args, expecting "geohash" or "quad".
        /// 	</summary>
        /// <remarks>
        /// The factory  is looked up via "prefixTree" in args, expecting "geohash" or "quad".
        /// If its neither of these, then "geohash" is chosen for a geo context, otherwise "quad" is chosen.
        /// </remarks>
        public static SpatialPrefixTree MakeSPT(IDictionary<string, string> args, SpatialContext ctx)
        {
            SpatialPrefixTreeFactory instance;
            string cname;
            if (!args.TryGetValue(PrefixTree, out cname))
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
                    throw new ApplicationException(string.Empty, e);
                }
            }
            instance.Init(args, ctx);
            return instance.NewSPT();
        }

        protected internal virtual void Init(IDictionary<string, string> args, SpatialContext ctx)
        {
            this.args = args;
            this.ctx = ctx;
            InitMaxLevels();
        }

        protected internal virtual void InitMaxLevels()
        {
            string mlStr;
            if (args.TryGetValue(MaxLevels, out mlStr))
            {
                maxLevels = int.Parse(mlStr, CultureInfo.InvariantCulture);
                return;
            }
            double degrees;
            string maxDetailDistStr;
            if (!args.TryGetValue(MaxDistErr, out maxDetailDistStr))
            {
                if (!ctx.IsGeo)
                {
                    return;
                }
                //let default to max
                degrees = DistanceUtils.Dist2Degrees(DefaultGeoMaxDetailKm, DistanceUtils.EARTH_MEAN_RADIUS_KM);
            }
            else
            {
                degrees = double.Parse(maxDetailDistStr, CultureInfo.InvariantCulture);
            }
            maxLevels = GetLevelForDistance(degrees);
        }

        /// <summary>
        /// Calls
        /// <see cref="SpatialPrefixTree.GetLevelForDistance(double)">SpatialPrefixTree.GetLevelForDistance(double)</see>.
        /// </summary>
        protected internal abstract int GetLevelForDistance(double degrees);

        protected internal abstract SpatialPrefixTree NewSPT();
    }
}
