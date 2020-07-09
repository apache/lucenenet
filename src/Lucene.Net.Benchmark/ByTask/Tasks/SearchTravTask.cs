using Lucene.Net.Benchmarks.ByTask.Feeds;
using System.Globalization;

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
    /// Search and Traverse task.
    /// </summary>
    /// <remarks>
    /// Note: This task reuses the reader if it is already open. 
    /// Otherwise a reader is opened at start and closed at the end.
    /// <para/>
    /// Takes optional param: traversal size (otherwise all results are traversed).
    /// <para/>
    /// Other side effects: counts additional 1 (record) for each traversed hit.
    /// </remarks>
    public class SearchTravTask : ReadTask
    {
        protected int m_traversalSize = int.MaxValue;

        public SearchTravTask(PerfRunData runData)
            : base(runData)
        {
        }

        public override bool WithRetrieve => false;


        public override bool WithSearch => true;

        public override bool WithTraverse => true;

        public override bool WithWarm => false;


        public override IQueryMaker GetQueryMaker()
        {
            return RunData.GetQueryMaker(this);
        }

        public override int TraversalSize => m_traversalSize;

        public override void SetParams(string @params)
        {
            base.SetParams(@params);
            m_traversalSize = (int)float.Parse(@params, CultureInfo.InvariantCulture);
        }

        /// <seealso cref="PerfTask.SupportsParams"/>
        public override bool SupportsParams => true;
    }
}
