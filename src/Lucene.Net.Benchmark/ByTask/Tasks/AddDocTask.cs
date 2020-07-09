using Lucene.Net.Benchmarks.ByTask.Feeds;
using Lucene.Net.Documents;
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
    /// Add a document, optionally of a certain size.
    /// <para/>
    /// Other side effects: none.
    /// <para/>
    /// Takes optional param: document size.
    /// </summary>
    public class AddDocTask : PerfTask
    {
        public AddDocTask(PerfRunData runData)
            : base(runData)
        {
        }

        private int docSize = 0;

        /// <summary>
        /// Volatile data passed between <see cref="Setup()"/>, <see cref="DoLogic()"/>, <see cref="TearDown()"/>.
        /// The doc is created at <see cref="Setup()"/> and added at <see cref="DoLogic()"/>. 
        /// </summary>
        protected Document m_doc = null;

        public override void Setup()
        {
            base.Setup();
            DocMaker docMaker = RunData.DocMaker;
            if (docSize > 0)
            {
                m_doc = docMaker.MakeDocument(docSize);
            }
            else
            {
                m_doc = docMaker.MakeDocument();
            }
        }

        public override void TearDown()
        {
            m_doc = null;
            base.TearDown();
        }

        protected override string GetLogMessage(int recsCount)
        {
            return string.Format(CultureInfo.InvariantCulture, "added {0:N9} docs", recsCount);
        }

        public override int DoLogic()
        {
            RunData.IndexWriter.AddDocument(m_doc);
            return 1;
        }

        /// <summary>
        /// Set the params (docSize only).
        /// </summary>
        /// <param name="params">docSize, or 0 for no limit.</param>
        public override void SetParams(string @params)
        {
            base.SetParams(@params);
            docSize = (int)float.Parse(@params, CultureInfo.InvariantCulture);
        }

        /// <seealso cref="PerfTask.SupportsParams"/>
        public override bool SupportsParams => true;
    }
}
