namespace Lucene.Net.Search
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

    using IndexReader = Lucene.Net.Index.IndexReader;

    /// <summary>
    /// Factory class used by <see cref="SearcherManager"/> to
    /// create new <see cref="IndexSearcher"/>s. The default implementation just creates
    /// an <see cref="IndexSearcher"/> with no custom behavior:
    ///
    /// <code>
    ///     public IndexSearcher NewSearcher(IndexReader r)
    ///     {
    ///         return new IndexSearcher(r);
    ///     }
    /// </code>
    ///
    /// You can pass your own factory instead if you want custom behavior, such as:
    /// <list type="bullet">
    ///   <item><description>Setting a custom scoring model: <see cref="IndexSearcher.Similarity"/></description></item>
    ///   <item><description>Parallel per-segment search: <see cref="IndexSearcher.IndexSearcher(IndexReader, System.Threading.Tasks.TaskScheduler)"/></description></item>
    ///   <item><description>Return custom subclasses of <see cref="IndexSearcher"/> (for example that implement distributed scoring)</description></item>
    ///   <item><description>Run queries to warm your <see cref="IndexSearcher"/> before it is used. Note: when using near-realtime search
    ///       you may want to also set <see cref="Index.LiveIndexWriterConfig.MergedSegmentWarmer"/> to warm
    ///       newly merged segments in the background, outside of the reopen path.</description></item>
    /// </list>
    /// @lucene.experimental
    /// </summary>
    public class SearcherFactory
    {
        /// <summary>
        /// Returns a new <see cref="IndexSearcher"/> over the given reader.
        /// </summary>
        public virtual IndexSearcher NewSearcher(IndexReader reader)
        {
            return new IndexSearcher(reader);
        }
    }
}