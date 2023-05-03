using Lucene.Net.Index;
    
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
    /// <see cref="IndexReader"/> instance that is used by <see cref="DirectoryTaxonomyReader"/>.
    /// <para/>
    /// This class is specific to Lucene.NET to allow to customize the <see cref="DirectoryReader"/> instance
    /// before it is used by <see cref="DirectoryTaxonomyReader"/>.
    /// </summary>
    public class DirectoryTaxonomyIndexReaderFactory
    {
        public static DirectoryTaxonomyIndexReaderFactory Default { get; } = new DirectoryTaxonomyIndexReaderFactory();
        
        /// <summary>
        /// Open the <see cref="DirectoryReader"/> from this <see cref="Directory"/>. 
        /// </summary>
        public virtual DirectoryReader OpenIndexReader(Directory directory)
        {
            // LUCENENET specific - added null check
            if (directory is null) throw new System.ArgumentNullException(nameof(directory));
            return DirectoryReader.Open(directory);
        }

        /// <summary>
        /// Open the <see cref="DirectoryReader"/> from this <see cref="IndexWriter"/>. 
        /// </summary>
        public virtual DirectoryReader OpenIndexReader(IndexWriter writer)
        {
            // LUCENENET specific - added null check
            if (writer is null) throw new System.ArgumentNullException(nameof(writer));
            return DirectoryReader.Open(writer, applyAllDeletes: false);
        }
    }
}