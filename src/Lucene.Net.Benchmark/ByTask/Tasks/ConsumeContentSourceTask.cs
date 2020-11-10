using Lucene.Net.Benchmarks.ByTask.Feeds;
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
    ///  Consumes a <see cref="Feeds.ContentSource"/>.
    /// </summary>
    public class ConsumeContentSourceTask : PerfTask
    {
        private readonly ContentSource source;
        private readonly DisposableThreadLocal<DocData> dd = new DisposableThreadLocal<DocData>();

        public ConsumeContentSourceTask(PerfRunData runData)
            : base(runData)
        {
            source = runData.ContentSource;
        }

        protected override string GetLogMessage(int recsCount)
        {
            return "read " + recsCount + " documents from the content source";
        }

        public override int DoLogic()
        {
            dd.Value = source.GetNextDocData(dd.Value);
            return 1;
        }

        /// <summary>
        /// Releases resources used by the <see cref="ConsumeContentSourceTask"/> and
        /// if overridden in a derived class, optionally releases unmanaged resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
        /// <c>false</c> to release only unmanaged resources.</param>

        // LUCENENET specific
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    dd.Dispose(); // LUCENENET specific - dispose dd
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}
