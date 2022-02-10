using J2N.Text;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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
    /// Search and Traverse and Retrieve docs task using a
    /// FieldVisitor loading only the requested fields.
    /// </summary>
    /// <remarks>
    /// Note: This task reuses the reader if it is already open.
    /// Otherwise a reader is opened at start and closed at the end.
    /// <para/>
    /// Takes optional param: comma separated list of Fields to load.
    /// <para/>
    /// Other side effects: counts additional 1 (record) for each traversed hit, 
    /// and 1 more for each retrieved (non null) document.
    /// </remarks>
    public class SearchTravRetLoadFieldSelectorTask : SearchTravTask
    {
        protected ISet<string> m_fieldsToLoad;

        public SearchTravRetLoadFieldSelectorTask(PerfRunData runData)
            : base(runData)
        {
        }

        public override bool WithRetrieve => true;


        protected override Document RetrieveDoc(IndexReader ir, int id)
        {
            if (m_fieldsToLoad is null)
            {
                return ir.Document(id);
            }
            else
            {
                DocumentStoredFieldVisitor visitor = new DocumentStoredFieldVisitor(m_fieldsToLoad);
                ir.Document(id, visitor);
                return visitor.Document;
            }
        }

        public override void SetParams(string @params)
        {
            this.m_params = @params; // cannot just call super.setParams(), b/c it's params differ.
            m_fieldsToLoad = new JCG.HashSet<string>();
            for (StringTokenizer tokenizer = new StringTokenizer(@params, ","); tokenizer.MoveNext();)
            {
                string s = tokenizer.Current;
                m_fieldsToLoad.Add(s);
            }
        }


        /// <seealso cref="PerfTask.SupportsParams"/>
        public override bool SupportsParams => true;
    }
}
