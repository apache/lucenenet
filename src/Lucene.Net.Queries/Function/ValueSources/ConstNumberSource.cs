// Lucene version compatibility level 4.8.1

namespace Lucene.Net.Queries.Function.ValueSources
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
    /// <see cref="ConstNumberSource"/> is the base class for all constant numbers
    /// </summary>
    public abstract class ConstNumberSource : ValueSource
    {
        /// <summary>
        /// NOTE: This was getInt() in Lucene
        /// </summary>
        public abstract int Int32 { get; }

        /// <summary>
        /// NOTE: This was getLong() in Lucene
        /// </summary>
        public abstract long Int64 { get; }

        /// <summary>
        /// NOTE: This was getFloat() in Lucene
        /// </summary>
        public abstract float Single { get; }
        public abstract double Double { get; }
        //public abstract Number Number { get; }
        public abstract bool Bool { get; }
    }
}