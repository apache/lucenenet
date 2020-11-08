using J2N.Text;
using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.IO;

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
    /// Open an index reader.
    /// <para/>
    /// Other side effects: index reader object in perfRunData is set.
    /// <para/>
    /// Optional params commitUserData eg. OpenReader(false,commit1)
    /// </summary>
    public class OpenReaderTask : PerfTask
    {
        public static readonly string USER_DATA = "userData";
        private string commitUserData = null;

        public OpenReaderTask(PerfRunData runData)
            : base(runData)
        {
        }

        public override int DoLogic()
        {
            Store.Directory dir = RunData.Directory;
            DirectoryReader r; // LUCENENET: IDE0059: Remove unnecessary value assignment
            if (commitUserData != null)
            {
                r = DirectoryReader.Open(OpenReaderTask.FindIndexCommit(dir, commitUserData));
            }
            else
            {
                r = DirectoryReader.Open(dir);
            }
            RunData.SetIndexReader(r);
            // We transfer reference to the run data
            r.DecRef();
            return 1;
        }

        public override void SetParams(string @params)
        {
            base.SetParams(@params);
            if (@params != null)
            {
                string[] split = @params.Split(',').TrimEnd();
                if (split.Length > 0)
                {
                    commitUserData = split[0];
                }
            }
        }

        public override bool SupportsParams => true;

        public static IndexCommit FindIndexCommit(Store.Directory dir, string userData)
        {
            IList<IndexCommit> commits = DirectoryReader.ListCommits(dir);
            foreach (IndexCommit ic in commits)
            {
                IDictionary<string, string> map = ic.UserData;
                string ud = null;
                if (map != null)
                {
                    //ud = map.get(USER_DATA);
                    map.TryGetValue(USER_DATA, out ud);
                }
                if (ud != null && ud.Equals(userData, StringComparison.Ordinal))
                {
                    return ic;
                }
            }

            throw new IOException("index does not contain commit with userData: " + userData);
        }
    }
}
