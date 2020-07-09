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
    /// Search task.
    /// <para/>
    /// Note: This task reuses the reader if it is already open. 
    /// Otherwise a reader is opened at start and closed at the end.
    /// </summary>
    public class SearchTask : ReadTask
    {
        public SearchTask(PerfRunData runData)
            : base(runData)
        {
        }

        public override bool WithRetrieve => false;

        public override bool WithSearch => true;

        public override bool WithTraverse => false;

        public override bool WithWarm => false;

        public override IQueryMaker GetQueryMaker()
        {
            return RunData.GetQueryMaker(this);
        }
    }
}
