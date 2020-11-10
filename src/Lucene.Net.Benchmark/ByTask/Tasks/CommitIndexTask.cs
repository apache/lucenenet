using Lucene.Net.Index;
using System.Collections.Generic;

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
    /// Commits the <see cref="IndexWriter"/>.
    /// </summary>
    public class CommitIndexTask : PerfTask
    {
        private IDictionary<string, string> commitUserData;

        public CommitIndexTask(PerfRunData runData)
            : base(runData)
        {
        }

        public override bool SupportsParams => true;

        public override void SetParams(string @params)
        {
            base.SetParams(@params);
            commitUserData = new Dictionary<string, string>
            {
                [OpenReaderTask.USER_DATA] = @params
            };
        }

        public override int DoLogic()
        {
            IndexWriter iw = RunData.IndexWriter;
            if (iw != null)
            {
                if (commitUserData != null)
                {
                    iw.SetCommitData(commitUserData);
                }
                iw.Commit();
            }

            return 1;
        }
    }
}
