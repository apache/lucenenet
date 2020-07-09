using Lucene.Net.Search;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Config
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
    /// Configuration parameters for <see cref="FuzzyQuery"/>s
    /// </summary>
    public class FuzzyConfig
    {
        private int prefixLength = FuzzyQuery.DefaultPrefixLength;

#pragma warning disable 612, 618
        private float minSimilarity = FuzzyQuery.DefaultMinSimilarity;
#pragma warning restore 612, 618

        public FuzzyConfig() { }

        public virtual int PrefixLength
        {
            get => prefixLength;
            set => this.prefixLength = value;
        }

        public virtual float MinSimilarity
        {
            get => minSimilarity;
            set => this.minSimilarity = value;
        }
    }
}
