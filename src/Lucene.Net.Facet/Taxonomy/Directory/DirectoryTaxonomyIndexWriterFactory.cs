using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Util;

namespace Lucene.Net.Facet.Taxonomy.Directory
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

    using Directory = Lucene.Net.Store.Directory;

    /// <summary>
    /// This class offers some hooks for extending classes to control the
    /// <see cref="IndexWriter"/> instance that is used by <see cref="DirectoryTaxonomyWriter"/>.
    /// <para/>
    /// This class is specific to Lucene.NET to allow to customize the <see cref="IndexWriter"/> instance
    /// and its configuration before it is used by <see cref="DirectoryTaxonomyWriter"/>.
    /// </summary>
    public class DirectoryTaxonomyIndexWriterFactory
    {
        public static DirectoryTaxonomyIndexWriterFactory Default { get; } = new DirectoryTaxonomyIndexWriterFactory();
        
        /// <summary>
        /// Open internal index writer, which contains the taxonomy data.
        /// <para/>
        /// Extensions may provide their own <see cref="IndexWriter"/> implementation or instance. 
        /// <para/>
        /// <b>NOTE:</b> the instance this method returns will be disposed upon calling
        /// to <see cref="DirectoryTaxonomyWriter.Dispose()"/>.
        /// <para/>
        /// <b>NOTE:</b> the merge policy in effect must not merge none adjacent segments. See
        /// comment in <see cref="CreateIndexWriterConfig(OpenMode)"/> for the logic behind this.
        /// </summary>
        /// <seealso cref="CreateIndexWriterConfig(OpenMode)"/>
        /// <param name="directory">
        ///          the <see cref="Store.Directory"/> on top of which an <see cref="IndexWriter"/>
        ///          should be opened. </param>
        /// <param name="config">
        ///          configuration for the internal index writer. </param>
        public virtual IndexWriter OpenIndexWriter(Directory directory, IndexWriterConfig config)
        {
            return new IndexWriter(directory, config);
        }

        /// <summary>
        /// Create the <see cref="IndexWriterConfig"/> that would be used for opening the internal index writer.
        /// <para/>
        /// Extensions can configure the <see cref="IndexWriter"/> as they see fit,
        /// including setting a <see cref="Index.MergeScheduler"/>, or
        /// <see cref="Index.IndexDeletionPolicy"/>, different RAM size
        /// etc.
        /// <para/>
        /// <b>NOTE:</b> internal docids of the configured index must not be altered.
        /// For that, categories are never deleted from the taxonomy index.
        /// In addition, merge policy in effect must not merge none adjacent segments.
        /// </summary>
        /// <seealso cref="OpenIndexWriter(Directory, IndexWriterConfig)"/>
        /// <param name="openMode"> see <see cref="OpenMode"/> </param>
        public virtual IndexWriterConfig CreateIndexWriterConfig(OpenMode openMode)
        {
            // TODO: should we use a more optimized Codec, e.g. Pulsing (or write custom)?
            // The taxonomy has a unique structure, where each term is associated with one document

            // :Post-Release-Update-Version.LUCENE_XY:
            // Make sure we use a MergePolicy which always merges adjacent segments and thus
            // keeps the doc IDs ordered as well (this is crucial for the taxonomy index).
            return (new IndexWriterConfig(LuceneVersion.LUCENE_48, null)).SetOpenMode(openMode).SetMergePolicy(new LogByteSizeMergePolicy());
        }
    }
}