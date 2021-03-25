// Lucene version compatibility level 4.8.1
namespace Lucene.Net.Analysis.Util
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
    /// Add to any analysis factory component to allow returning an
    /// analysis component factory for use with partial terms in prefix queries,
    /// wildcard queries, range query endpoints, regex queries, etc.
    /// 
    /// @lucene.experimental
    /// </summary>
    public interface IMultiTermAwareComponent
    {
        /// <summary>
        /// Returns an analysis component to handle analysis if multi-term queries.
        /// The returned component must be a <see cref="TokenizerFactory"/>, <see cref="TokenFilterFactory"/> or <see cref="CharFilterFactory"/>.
        /// </summary>
        AbstractAnalysisFactory GetMultiTermComponent(); // LUCENENET NOTE: Some implementations return new instance, so not a good candidate for a property.
    }
}