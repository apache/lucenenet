using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Processors;

namespace Lucene.Net.QueryParsers.Flexible.Precedence.Processors
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
    /// This processor pipeline extends <see cref="StandardQueryNodeProcessorPipeline"/> and enables
    /// boolean precedence on it.
    /// <para>
    /// EXPERT: the precedence is enabled by removing <see cref="GroupQueryNodeProcessor"/> from the
    /// <see cref="StandardQueryNodeProcessorPipeline"/> and appending <see cref="BooleanModifiersQueryNodeProcessor"/>
    /// to the pipeline.
    /// </para>
    /// </summary>
    /// <seealso cref="PrecedenceQueryParser"/>
    /// <seealso cref="StandardQueryNodeProcessorPipeline"/>
    public class PrecedenceQueryNodeProcessorPipeline : StandardQueryNodeProcessorPipeline
    {
        /// <summary>
        /// <see cref="StandardQueryNodeProcessorPipeline.StandardQueryNodeProcessorPipeline(QueryConfigHandler)"/>
        /// </summary>
        public PrecedenceQueryNodeProcessorPipeline(QueryConfigHandler queryConfig)
            : base(queryConfig)
        {
            for (int i = 0; i < Count; i++)
            {
                if (this[i].GetType().Equals(typeof(BooleanQuery2ModifierNodeProcessor)))
                {
                    RemoveAt(i--);
                }
            }

            Add(new BooleanModifiersQueryNodeProcessor());
        }
    }
}
