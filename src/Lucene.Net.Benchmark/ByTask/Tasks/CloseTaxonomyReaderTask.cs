using Lucene.Net.Facet.Taxonomy;
using Console = Lucene.Net.Util.SystemConsole;

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
    /// Close taxonomy reader.
    /// <para/>
    /// Other side effects: taxonomy reader in perfRunData is nullified.
    /// </summary>
    public class CloseTaxonomyReaderTask : PerfTask
    {
        public CloseTaxonomyReaderTask(PerfRunData runData)
            : base(runData)
        {
        }

        public override int DoLogic()
        {
            TaxonomyReader taxoReader = RunData.GetTaxonomyReader();
            RunData.SetTaxonomyReader(null);
            if (taxoReader.RefCount != 1)
            {
                Console.WriteLine("WARNING: CloseTaxonomyReader: reference count is currently " + taxoReader.RefCount);
            }
            taxoReader.Dispose();
            return 1;
        }
    }
}
