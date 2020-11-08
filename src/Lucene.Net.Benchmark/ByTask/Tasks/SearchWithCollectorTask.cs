using Lucene.Net.Benchmarks.ByTask.Feeds;
using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Search;
using System;

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
    /// Does search w/ a custom collector
    /// </summary>
    public class SearchWithCollectorTask : SearchTask
    {
        protected string m_clnName;

        public SearchWithCollectorTask(PerfRunData runData)
            : base(runData)
        {
        }

        public override void Setup()
        {
            base.Setup();
            //check to make sure either the doc is being stored
            PerfRunData runData = RunData;
            Config config = runData.Config;
            m_clnName = config.Get("collector.class", "");
        }

        public override bool WithCollector => true;

        protected override ICollector CreateCollector()
        {
            ICollector collector; // LUCENENET: IDE0059: Remove unnecessary value assignment
            if (m_clnName.Equals("topScoreDocOrdered", StringComparison.OrdinalIgnoreCase) == true)
            {
                collector = TopScoreDocCollector.Create(NumHits, true);
            }
            else if (m_clnName.Equals("topScoreDocUnOrdered", StringComparison.OrdinalIgnoreCase) == true)
            {
                collector = TopScoreDocCollector.Create(NumHits, false);
            }
            else if (m_clnName.Length > 0)
            {
                collector = (ICollector)Activator.CreateInstance(Type.GetType(m_clnName));

            }
            else
            {
                collector = base.CreateCollector();
            }
            return collector;
        }

        public override IQueryMaker GetQueryMaker()
        {
            return RunData.GetQueryMaker(this);
        }

        public override bool WithRetrieve => false;

        public override bool WithSearch => true;

        public override bool WithTraverse => false;

        public override bool WithWarm => false;
    }
}
