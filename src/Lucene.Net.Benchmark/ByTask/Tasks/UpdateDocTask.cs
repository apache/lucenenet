using Lucene.Net.Benchmarks.ByTask.Feeds;
using Lucene.Net.Documents;
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
    /// Update a document, using <see cref="IndexWriter.UpdateDocument(Term, System.Collections.Generic.IEnumerable{IIndexableField})"/>,
    /// optionally with of a certain size.
    /// <para/>
    /// Other side effects: none.
    /// <para/>
    /// Takes optional param: document size. 
    /// </summary>
    public class UpdateDocTask : PerfTask
    {
        public UpdateDocTask(PerfRunData runData)
            : base(runData)
        {
        }

        private int docSize = 0;

        // volatile data passed between setup(), doLogic(), tearDown().
        private Document doc = null;

        public override void Setup()
        {
            base.Setup();
            DocMaker docMaker = RunData.DocMaker;
            if (docSize > 0)
            {
                doc = docMaker.MakeDocument(docSize);
            }
            else
            {
                doc = docMaker.MakeDocument();
            }
        }

        public override void TearDown()
        {
            doc = null;
            base.TearDown();
        }

        public override int DoLogic()
        {
            string docID = doc.Get(DocMaker.ID_FIELD);
            if (docID is null)
            {
                throw IllegalStateException.Create("document must define the docid field");
            }
            IndexWriter iw = RunData.IndexWriter;
            iw.UpdateDocument(new Term(DocMaker.ID_FIELD, docID), doc);
            return 1;
        }

        protected override string GetLogMessage(int recsCount)
        {
            return "updated " + recsCount + " docs";
        }

        /// <summary>
        /// Set the params (docSize only)
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
