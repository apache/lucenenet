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
    /// Reset all index and input data and call gc, erase index and dir, does NOT clear statistics.
    /// <para/>
    /// This contains ResetInputs.
    /// <para/>
    /// Other side effects: writers/readers nullified, deleted, closed.
    /// Index is erased.
    /// Directory is erased.
    /// </summary>
    public class ResetSystemEraseTask : ResetSystemSoftTask
    {
        public ResetSystemEraseTask(PerfRunData runData)
            : base(runData)
        {
        }

        public override int DoLogic()
        {
            RunData.Reinit(true);
            return 0;
        }
    }
}
