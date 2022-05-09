using System;

namespace Lucene.Net.Index
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

    using AttributeSource = Lucene.Net.Util.AttributeSource;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;

    // LUCENENET specific - converted constants from DocsEnum
    // into a flags enum.
    [Flags]
    public enum DocsFlags
    {
        /// <summary>
        /// Flag to pass to <see cref="TermsEnum.Docs(Util.IBits, DocsEnum, DocsFlags)"/> if you don't
        /// require term frequencies in the returned enum.
        /// </summary>
        NONE = 0x0,

        /// <summary>
        /// Flag to pass to <see cref="TermsEnum.Docs(Util.IBits, DocsEnum, DocsFlags)"/>
        /// if you require term frequencies in the returned enum.
        /// </summary>
        FREQS = 0x1
    }

    /// <summary>
    /// Iterates through the documents and term freqs.
    /// NOTE: you must first call <see cref="DocIdSetIterator.NextDoc()"/> before using
    /// any of the per-doc methods.
    /// </summary>
    public abstract class DocsEnum : DocIdSetIterator
    {
        // LUCENENET specific - made flags into their own [Flags] enum named DocsFlags and de-nested from this type

        private AttributeSource atts = null;

        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected DocsEnum()
        {
        }

        /// <summary>
        /// Returns term frequency in the current document, or 1 if the field was
        /// indexed with <see cref="IndexOptions.DOCS_ONLY"/>. Do not call this before
        /// <see cref="DocIdSetIterator.NextDoc()"/> is first called, nor after <see cref="DocIdSetIterator.NextDoc()"/> returns
        /// <see cref="DocIdSetIterator.NO_MORE_DOCS"/>.
        ///
        /// <para/>
        /// <b>NOTE:</b> if the <see cref="DocsEnum"/> was obtain with <see cref="DocsFlags.NONE"/>,
        /// the result of this method is undefined.
        /// </summary>
        public abstract int Freq { get; }

        /// <summary>
        /// Returns the related attributes. </summary>
        public virtual AttributeSource Attributes
        {
            get
            {
                if (atts is null)
                {
                    atts = new AttributeSource();
                }
                return atts;
            }
        }
    }
}