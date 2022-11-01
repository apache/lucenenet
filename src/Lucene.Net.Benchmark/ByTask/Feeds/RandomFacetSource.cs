using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Facet;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Lucene.Net.Benchmarks.ByTask.Feeds
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
    /// Simple implementation of a random facet source.
    /// </summary>
    /// <remarks>
    /// Supports the following parameters:
    /// <list type="bullet">
    ///     <item><term>rand.seed</term><description>defines the seed to initialize <see cref="Random"/> with (default: <b>13</b>).</description></item>
    ///     <item><term>max.doc.facet.dims</term><description>Max number of random dimensions to create (default: <b>5</b>); 
    ///         actual number of dimensions would be anything between 1 and that number.</description></item>
    ///     <item><term>max.doc.facets</term><description>maximal #facets per doc (default: <b>10</b>).
    ///         Actual number of facets in a certain doc would be anything between 1 and that number.
    ///     </description></item>
    ///     <item><term>max.facet.depth</term><description>maximal #components in a facet (default:
    ///         <b>3</b>). Actual number of components in a certain facet would be anything
    ///         between 1 and that number.
    ///     </description></item>
    /// </list>
    /// </remarks>
    public class RandomFacetSource : FacetSource
    {
        private Random random;
        private int maxDocFacets;
        private int maxFacetDepth;
        private int maxDims;
        private int maxValue; // = maxDocFacets * maxFacetDepth;

        public override void GetNextFacets(IList<FacetField> facets)
        {
            facets.Clear();
            int numFacets = 1 + random.Next(maxDocFacets); // at least one facet to each doc
            for (int i = 0; i < numFacets; i++)
            {
                int depth;
                if (maxFacetDepth == 2)
                {
                    depth = 2;
                }
                else
                {
                    depth = 2 + random.Next(maxFacetDepth - 2); // depth < 2 is not useful
                }

                string dim = random.Next(maxDims).ToString(CultureInfo.InvariantCulture);
                string[] components = new string[depth - 1];
                for (int k = 0; k < depth - 1; k++)
                {
                    components[k] = random.Next(maxValue).ToString(CultureInfo.InvariantCulture);
                    AddItem();
                }
                FacetField ff = new FacetField(dim, components);
                facets.Add(ff);
                AddBytes(ff.ToString().Length); // very rough approximation
            }
        }

        public override void Configure(FacetsConfig config)
        {
            for (int i = 0; i < maxDims; i++)
            {
                config.SetHierarchical(i.ToString(CultureInfo.InvariantCulture), true);
                config.SetMultiValued(i.ToString(CultureInfo.InvariantCulture), true);
            }
        }

        protected override void Dispose(bool disposing)
        {
            // nothing to do here
        }

        public override void SetConfig(Config config)
        {
            base.SetConfig(config);
            random = new J2N.Randomizer(config.Get("rand.seed", 13));
            maxDocFacets = config.Get("max.doc.facets", 10);
            maxDims = config.Get("max.doc.facets.dims", 5);
            maxFacetDepth = config.Get("max.facet.depth", 3);
            if (maxFacetDepth < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(config), "max.facet.depth must be at least 2; got: " + maxFacetDepth);
            }
            maxValue = maxDocFacets * maxFacetDepth;
        }
    }
}
