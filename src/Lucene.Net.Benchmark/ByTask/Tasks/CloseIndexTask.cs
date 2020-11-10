using Lucene.Net.Index;
using Lucene.Net.Util;

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
    /// Close index writer.
    /// <para/>
    /// Other side effects: index writer object in perfRunData is nullified.
    /// <para/>
    /// Takes optional param "doWait": if false, then close(false) is called.
    /// </summary>
    public class CloseIndexTask : PerfTask
    {
        public CloseIndexTask(PerfRunData runData)
            : base(runData)
        {
        }

        private bool doWait = true;

        public override int DoLogic()
        {
            IndexWriter iw = RunData.IndexWriter;
            if (iw != null)
            {
                // If infoStream was set to output to a file, close it.
                InfoStream infoStream = iw.Config.InfoStream;
                if (infoStream != null)
                {
                    infoStream.Dispose();
                }
                iw.Dispose(doWait);
                RunData.IndexWriter = null;
            }
            return 1;
        }

        public override void SetParams(string @params)
        {
                base.SetParams(@params);
                doWait = bool.Parse(@params);
        }

        public override bool SupportsParams => true;
    }
}
