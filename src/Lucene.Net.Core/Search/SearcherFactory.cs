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
    /// Factory class used by <seealso cref="SearcherManager"/> to
    /// create new IndexSearchers. The default implementation just creates
    /// an IndexSearcher with no custom behavior:
    ///
    /// <pre class="prettyprint">
    ///   public IndexSearcher newSearcher(IndexReader r) throws IOException {
    ///     return new IndexSearcher(r);
    ///   }
    /// </pre>
    ///
    /// You can pass your own factory instead if you want custom behavior, such as:
    /// <ul>
    ///   <li>Setting a custom scoring model: <seealso cref="IndexSearcher#setSimilarity(Similarity)"/>
    ///   <li>Parallel per-segment search: <seealso cref="IndexSearcher#IndexSearcher(IndexReader, ExecutorService)"/>
    ///   <li>Return custom subclasses of IndexSearcher (for example that implement distributed scoring)
    ///   <li>Run queries to warm your IndexSearcher before it is used. Note: when using near-realtime search
    ///       you may want to also <seealso cref="IndexWriterConfig#setMergedSegmentWarmer(IndexWriter.IndexReaderWarmer)"/> to warm
    ///       newly merged segments in the background, outside of the reopen path.
    /// </ul>
    /// @lucene.experimental
    /// </summary>
    public class SearcherFactory
    {
        /// <summary>
        /// Returns a new IndexSearcher over the given reader.
        /// </summary>
        public virtual IndexSearcher NewSearcher(IndexReader reader)
        {
            return new IndexSearcher(reader);
        }
    }
}