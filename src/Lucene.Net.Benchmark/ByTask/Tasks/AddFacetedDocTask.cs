using Lucene.Net.Benchmarks.ByTask.Feeds;
using Lucene.Net.Facet;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Benchmarks.ByTask.Tasks
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
    /// Add a faceted document.
    /// </summary>
    /// <remarks>
    /// Config properties:
    /// <list type="bullet">
    ///     <item>
    ///         <term>with.facets</term>
    ///         <description>
    ///             &lt;tells whether to actually add any facets to the document| Default: true&gt;
    ///             <para/>
    ///             This config property allows to easily compare the performance of adding docs
    ///             with and without facets. Note that facets are created even when this is
    ///             <c>false</c>, just that they are not added to the document (nor to the taxonomy).
    ///         </description>
    ///     </item>
    /// </list>
    /// <para/>
    /// See <see cref="AddDocTask"/> for general document parameters and configuration.
    /// <para/>
    /// Makes use of the <see cref="FacetSource"/> in effect - see <see cref="PerfRunData"/> for
    /// facet source settings.
    /// </remarks>
    public class AddFacetedDocTask : AddDocTask
    {
        private FacetsConfig config;

        public AddFacetedDocTask(PerfRunData runData)
            : base(runData)
        {
        }

        public override void Setup()
        {
            base.Setup();
            if (config is null)
            {
                bool withFacets = RunData.Config.Get("with.facets", true);
                if (withFacets)
                {
                    FacetSource facetsSource = RunData.FacetSource;
                    config = new FacetsConfig();
                    facetsSource.Configure(config);
                }
            }
        }

        protected override string GetLogMessage(int recsCount)
        {
            if (config is null)
            {
                return base.GetLogMessage(recsCount);
            }
            return base.GetLogMessage(recsCount) + " with facets";
        }

        public override int DoLogic()
        {
            if (config != null)
            {
                IList<FacetField> facets = new JCG.List<FacetField>();
                RunData.FacetSource.GetNextFacets(facets);
                foreach (FacetField ff in facets)
                {
                    m_doc.Add(ff);
                }
                m_doc = config.Build(RunData.TaxonomyWriter, m_doc);
            }
            return base.DoLogic();
        }
    }
}
