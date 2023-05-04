using Lucene.Net.Analysis;
using Lucene.Net.Codecs.Lucene46;
using Lucene.Net.Index;
using Lucene.Net.Index.Sorter;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Suggest.Analyzing
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
    /// Generic interface that can be used to customize the index writer to
    /// be used by <see cref="AnalyzingInfixSuggester"/>.
    /// <para/>
    /// This class is specific to Lucene.NET, where factory classes are used to allow customization
    /// as opposed to making virtual method calls from the constructor
    /// </summary>
    public interface IAnalyzingInfixSuggesterIndexWriterConfigFactory
    {
        IndexWriterConfig Get(LuceneVersion matchVersion, Analyzer indexAnalyzer, OpenMode openMode);
    }
    
    /// <summary>
    /// Default <see cref="IndexWriterConfig"/> factory for <see cref="AnalyzingInfixSuggester"/>.
    /// <para/>
    /// </summary>
    public class AnalyzingInfixSuggesterIndexWriterConfigFactory : IAnalyzingInfixSuggesterIndexWriterConfigFactory
    {
        private Sort sort;

        /// <summary>
        /// Creates a new config factory that uses the given <see cref="Sort"/> in the sorting merge policy
        /// </summary>
        public AnalyzingInfixSuggesterIndexWriterConfigFactory(Sort sort)
        {
            this.sort = sort;
        }
        
        /// <summary>
        /// Override this to customize index settings, e.g. which
        /// codec to use. 
        /// </summary>
        public virtual IndexWriterConfig Get(LuceneVersion matchVersion, Analyzer indexAnalyzer, OpenMode openMode)
        {
            IndexWriterConfig iwc = new IndexWriterConfig(matchVersion, indexAnalyzer)
            {
                Codec = new Lucene46Codec(),
                OpenMode = openMode
            };

            // This way all merged segments will be sorted at
            // merge time, allow for per-segment early termination
            // when those segments are searched:
            iwc.MergePolicy = new SortingMergePolicy(iwc.MergePolicy, sort);

            return iwc;
        }
    }
}