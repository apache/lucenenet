using System;

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
    /// Set a performance test configuration property.
    /// A property may have a single value, or a sequence of values, separated by ":". 
    /// If a sequence of values is specified, each time a new round starts, 
    /// the next (cyclic) value is taken.
    /// <para/>
    /// Other side effects: none.
    /// <para/>
    /// Takes mandatory param: "name,value" pair. 
    /// </summary>
    /// <seealso cref="NewRoundTask"/>
    public class SetPropTask : PerfTask
    {
        public SetPropTask(PerfRunData runData)
            : base(runData)
        {
        }

        private string name;
        private string value;

        public override int DoLogic()
        {
            if (name is null || value is null)
            {
                throw new Exception(GetName() + " - undefined name or value: name=" + name + " value=" + value);
            }
            RunData.Config.Set(name, value);
            return 0;
        }

        /// <summary>
        /// Set the params (property name and value).
        /// </summary>
        /// <param name="params">Property name and value separated by ','.</param>
        public override void SetParams(string @params)
        {
            base.SetParams(@params);
            int k = @params.IndexOf(',');
            name = @params.Substring(0, k - 0).Trim();
            value = @params.Substring(k + 1).Trim();
        }

        /// <seealso cref="PerfTask.SupportsParams"/>
        public override bool SupportsParams => true;
    }
}
