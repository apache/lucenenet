namespace Lucene.Net.Index
{
    using AttributeSource = Lucene.Net.Util.AttributeSource;

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

    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;

    // javadocs

    /// <summary>
    /// Iterates through the documents and term freqs.
    ///  NOTE: you must first call <seealso cref="#nextDoc"/> before using
    ///  any of the per-doc methods.
    /// </summary>
    public abstract class DocsEnum : DocIdSetIterator
    {
        /// <summary>
        /// Flag to pass to <seealso cref="TermsEnum#docs(Bits,DocsEnum,int)"/> if you don't
        /// require term frequencies in the returned enum. When passed to
        /// <seealso cref="TermsEnum#docsAndPositions(Bits,DocsAndPositionsEnum,int)"/> means
        /// that no offsets and payloads will be returned.
        /// </summary>
        public static readonly int FLAG_NONE = 0x0;

        /// <summary>
        /// Flag to pass to <seealso cref="TermsEnum#docs(Bits,DocsEnum,int)"/>
        ///  if you require term frequencies in the returned enum.
        /// </summary>
        public static readonly int FLAG_FREQS = 0x1;

        private AttributeSource Atts = null;

        /// <summary>
        /// Sole constructor. (For invocation by subclass
        ///  constructors, typically implicit.)
        /// </summary>
        protected DocsEnum()
        {
        }

        /// <summary>
        /// Returns term frequency in the current document, or 1 if the field was
        /// indexed with <seealso cref="IndexOptions#DOCS_ONLY"/>. Do not call this before
        /// <seealso cref="#nextDoc"/> is first called, nor after <seealso cref="#nextDoc"/> returns
        /// <seealso cref="DocIdSetIterator#NO_MORE_DOCS"/>.
        ///
        /// <p>
        /// <b>NOTE:</b> if the <seealso cref="DocsEnum"/> was obtain with <seealso cref="#FLAG_NONE"/>,
        /// the result of this method is undefined.
        /// </summary>
        public abstract int Freq { get; }

        /// <summary>
        /// Returns the related attributes. </summary>
        public virtual AttributeSource Attributes() // LUCENENET TODO: make property
        {
            if (Atts == null)
            {
                Atts = new AttributeSource();
            }
            return Atts;
        }
    }
}