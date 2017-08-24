namespace Lucene.Net.Benchmarks.ByTask.Feeds
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
    /// Represents content from a specified source, such as TREC, Reuters etc. A
    /// <see cref="ContentSource"/> is responsible for creating <see cref="DocData"/> objects for
    /// its documents to be consumed by <see cref="DocMaker"/>. It also keeps track
    /// of various statistics, such as how many documents were generated, size in
    /// bytes etc.
    /// <para/>
    /// For supported configuration parameters see <see cref="ContentItemsSource"/>.
    /// </summary>
    public abstract class ContentSource : ContentItemsSource
    {
        /// <summary>
        /// Returns the next <see cref="DocData"/> from the content source.
        /// Implementations must account for multi-threading, as multiple threads 
        /// can call this method simultaneously.
        /// </summary>
        public abstract DocData GetNextDocData(DocData docData);
    }
}
