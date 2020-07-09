using Lucene.Net.Benchmarks.ByTask.Feeds;

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
    /// Warm reader task: retrieve all reader documents.
    /// </summary>
    /// <remarks>
    /// Note: This task reuses the reader if it is already open. 
    /// Otherwise a reader is opened at start and closed at the end.
    /// <para/>
    /// Other side effects: counts additional 1 (record) for each 
    /// retrieved (non null) document.
    /// </remarks>
    public class WarmTask : ReadTask
    {
        public WarmTask(PerfRunData runData)
            : base(runData)
        {
        }

        public override bool WithRetrieve => false;

        public override bool WithSearch => false;

        public override bool WithTraverse => false;

        public override bool WithWarm => true;

        public override IQueryMaker GetQueryMaker()
        {
            return null; // not required for this task.
        }
    }
}
