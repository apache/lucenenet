// Lucene version compatibility level 4.8.1

namespace Lucene.Net.Facet.Taxonomy
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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using Int32sRef = Lucene.Net.Util.Int32sRef;

    /// <summary>
    /// Provides per-document ordinals. 
    /// </summary>
    public abstract class OrdinalsReader
    {
        /// <summary>
        /// Returns ordinals for documents in one segment.
        /// </summary>
        public abstract class OrdinalsSegmentReader
        {
            /// <summary>
            /// Get the ordinals for this document. The <paramref name="ordinals"/>.<see cref="Int32sRef.Offset"/>
            /// must always be 0! 
            /// </summary>
            public abstract void Get(int doc, Int32sRef ordinals);

            /// <summary>
            /// Default constructor. 
            /// </summary>
            protected OrdinalsSegmentReader() // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            {
            }
        }

        /// <summary>
        /// Default constructor. 
        /// </summary>
        protected OrdinalsReader() // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
        }

        /// <summary>
        /// Set current atomic reader. 
        /// </summary>
        public abstract OrdinalsSegmentReader GetReader(AtomicReaderContext context);

        /// <summary>
        /// Returns the indexed field name this <see cref="OrdinalsReader"/>
        /// is reading from. 
        /// </summary>
        public abstract string IndexFieldName { get; }
    }
}