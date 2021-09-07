using Lucene.Net.Index;
using System;
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
    /// Runs forceMerge on the index.
    /// <para/>
    /// Other side effects: none.
    /// </summary>
    public class ForceMergeTask : PerfTask
    {
        public ForceMergeTask(PerfRunData runData)
            : base(runData)
        {
        }

        private int maxNumSegments = -1;

        public override int DoLogic()
        {
            if (maxNumSegments == -1)
            {
                throw IllegalStateException.Create("required argument (maxNumSegments) was not specified");
            }
            IndexWriter iw = RunData.IndexWriter;
            iw.ForceMerge(maxNumSegments);
            //System.out.println("forceMerge called");
            return 1;
        }

        public override void SetParams(string @params)
        {
            base.SetParams(@params);
            maxNumSegments = (int)double.Parse(@params, CultureInfo.InvariantCulture);
        }

        public override bool SupportsParams => true;
    }
}
