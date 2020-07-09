using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Index;

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
    /// Open an index writer.
    /// </summary>
    /// <remarks>
    /// Other side effects: index writer object in perfRunData is set.
    /// <para/>
    /// Relevant properties:
    /// <list type="bullet">
    ///     <item><term>merge.factor</term><description></description></item>
    ///     <item><term>max.buffered</term><description></description></item>
    ///     <item><term>max.field.length</term><description></description></item>
    ///     <item><term>ram.flush.mb</term><description>[default 0]</description></item>
    /// </list>
    /// <para/>
    /// Accepts a param specifying the commit point as
    /// previously saved with <see cref="CommitIndexTask"/>.  If you specify
    /// this, it rolls the index back to that commit on opening
    /// the <see cref="IndexWriter"/>.
    /// </remarks>
    public class OpenIndexTask : PerfTask
    {
        public static readonly int DEFAULT_MAX_BUFFERED = IndexWriterConfig.DEFAULT_MAX_BUFFERED_DOCS;
        public static readonly int DEFAULT_MERGE_PFACTOR = LogMergePolicy.DEFAULT_MERGE_FACTOR;
        public static readonly double DEFAULT_RAM_FLUSH_MB = (int)IndexWriterConfig.DEFAULT_RAM_BUFFER_SIZE_MB;
        private string commitUserData;

        public OpenIndexTask(PerfRunData runData)
            : base(runData)
        {
        }

        public override int DoLogic()
        {
            PerfRunData runData = RunData;
            Config config = runData.Config;
            IndexCommit ic;
            if (commitUserData != null)
            {
                ic = OpenReaderTask.FindIndexCommit(runData.Directory, commitUserData);
            }
            else
            {
                ic = null;
            }

            IndexWriter writer = CreateIndexTask.ConfigureWriter(config, runData, OpenMode.APPEND, ic);
            runData.IndexWriter = writer;
            return 1;
        }

        public override void SetParams(string @params)
        {
            base.SetParams(@params);
            if (@params != null)
            {
                // specifies which commit point to open
                commitUserData = @params;
            }
        }

        public override bool SupportsParams => true;
    }
}
