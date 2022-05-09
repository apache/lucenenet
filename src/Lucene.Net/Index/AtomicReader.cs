using Lucene.Net.Diagnostics;
using System.Diagnostics;
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

    // javadocs
    using IBits = Lucene.Net.Util.IBits;

    /// <summary>
    /// <see cref="AtomicReader"/> is an abstract class, providing an interface for accessing an
    /// index.  Search of an index is done entirely through this abstract interface,
    /// so that any subclass which implements it is searchable. <see cref="IndexReader"/>s implemented
    /// by this subclass do not consist of several sub-readers,
    /// they are atomic. They support retrieval of stored fields, doc values, terms,
    /// and postings.
    ///
    /// <para/>For efficiency, in this API documents are often referred to via
    /// <i>document numbers</i>, non-negative integers which each name a unique
    /// document in the index.  These document numbers are ephemeral -- they may change
    /// as documents are added to and deleted from an index.  Clients should thus not
    /// rely on a given document having the same number between sessions.
    ///
    /// <para/>
    /// <b>NOTE</b>: <see cref="IndexReader"/>
    /// instances are completely thread
    /// safe, meaning multiple threads can call any of its methods,
    /// concurrently.  If your application requires external
    /// synchronization, you should <b>not</b> synchronize on the
    /// <see cref="IndexReader"/> instance; use your own
    /// (non-Lucene) objects instead.
    /// </summary>
    public abstract class AtomicReader : IndexReader
    {
        private readonly AtomicReaderContext readerContext; // LUCENENET: marked readonly

        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected AtomicReader()
            : base()
        {
            readerContext = new AtomicReaderContext(this);
        }

        public sealed override IndexReaderContext Context
        {
            get
            {
                EnsureOpen();
                return readerContext;
            }
        }

        /// <summary>
        /// LUCENENET specific propety that allows access to
        /// the context as <see cref="AtomicReaderContext"/>,
        /// which prevents the need to cast.
        /// </summary>
        public AtomicReaderContext AtomicContext
        {
            get
            {
                EnsureOpen();
                return readerContext;
            }
        }

        /// <summary>
        /// Returns true if there are norms stored for this <paramref name="field"/>.
        /// </summary>
        [Obsolete("(4.0) use FieldInfos and check FieldInfo.HasNorms for the field instead.")]
        public bool HasNorms(string field)
        {
            EnsureOpen();
            // note: using normValues(field) != null would potentially cause i/o
            FieldInfo fi = FieldInfos.FieldInfo(field);
            return fi != null && fi.HasNorms;
        }

        /// <summary>
        /// Returns <see cref="Index.Fields"/> for this reader.
        /// this property may return <c>null</c> if the reader has no
        /// postings.
        /// </summary>
        public abstract Fields Fields { get; }

        public override sealed int DocFreq(Term term)
        {
            Fields fields = Fields;
            if (fields is null)
            {
                return 0;
            }
            Terms terms = fields.GetTerms(term.Field);
            if (terms is null)
            {
                return 0;
            }
            TermsEnum termsEnum = terms.GetEnumerator();
            if (termsEnum.SeekExact(term.Bytes))
            {
                return termsEnum.DocFreq;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Returns the number of documents containing the <paramref name="term"/>. 
        /// This method returns 0 if the term or
        /// field does not exist. This method does not take into
        /// account deleted documents that have not yet been merged
        /// away.
        /// </summary>
        public override sealed long TotalTermFreq(Term term)
        {
            Fields fields = Fields;
            if (fields is null)
            {
                return 0;
            }
            Terms terms = fields.GetTerms(term.Field);
            if (terms is null)
            {
                return 0;
            }
            TermsEnum termsEnum = terms.GetEnumerator();
            if (termsEnum.SeekExact(term.Bytes))
            {
                return termsEnum.TotalTermFreq;
            }
            else
            {
                return 0;
            }
        }

        public override sealed long GetSumDocFreq(string field)
        {
            Terms terms = GetTerms(field);
            if (terms is null)
            {
                return 0;
            }
            return terms.SumDocFreq;
        }

        public override sealed int GetDocCount(string field)
        {
            Terms terms = GetTerms(field);
            if (terms is null)
            {
                return 0;
            }
            return terms.DocCount;
        }

        public override sealed long GetSumTotalTermFreq(string field)
        {
            Terms terms = GetTerms(field);
            if (terms is null)
            {
                return 0;
            }
            return terms.SumTotalTermFreq;
        }

        /// <summary>
        /// This may return <c>null</c> if the field does not exist. </summary>
        public Terms GetTerms(string field) // LUCENENET specific: Renamed from Terms()
        {
            Fields fields = Fields;
            if (fields is null)
            {
                return null;
            }
            return fields.GetTerms(field);
        }

        /// <summary>
        /// Returns <see cref="DocsEnum"/> for the specified term.
        /// This will return <c>null</c> if either the field or
        /// term does not exist.
        /// </summary>
        /// <seealso cref="TermsEnum.Docs(IBits, DocsEnum)"/>
        public DocsEnum GetTermDocsEnum(Term term) // LUCENENET specific: Renamed from TermDocsEnum()
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(term.Field != null);
                Debugging.Assert(term.Bytes != null);
            }
            Fields fields = Fields;
            if (fields != null)
            {
                Terms terms = fields.GetTerms(term.Field);
                if (terms != null)
                {
                    TermsEnum termsEnum = terms.GetEnumerator();
                    if (termsEnum.SeekExact(term.Bytes))
                    {
                        return termsEnum.Docs(LiveDocs, null);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Returns <see cref="DocsAndPositionsEnum"/> for the specified
        /// term. This will return <c>null</c> if the
        /// field or term does not exist or positions weren't indexed. </summary>
        /// <seealso cref="TermsEnum.DocsAndPositions(IBits, DocsAndPositionsEnum)"/>
        public DocsAndPositionsEnum GetTermPositionsEnum(Term term) // LUCENENET specific: Renamed from TermPositionsEnum()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(term.Field != null);
            if (Debugging.AssertsEnabled) Debugging.Assert(term.Bytes != null);
            Fields fields = Fields;
            if (fields != null)
            {
                Terms terms = fields.GetTerms(term.Field);
                if (terms != null)
                {
                    TermsEnum termsEnum = terms.GetEnumerator();
                    if (termsEnum.SeekExact(term.Bytes))
                    {
                        return termsEnum.DocsAndPositions(LiveDocs, null);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Returns <see cref="NumericDocValues"/> for this field, or
        /// null if no <see cref="NumericDocValues"/> were indexed for
        /// this field. The returned instance should only be
        /// used by a single thread.
        /// </summary>
        public abstract NumericDocValues GetNumericDocValues(string field);

        /// <summary>
        /// Returns <see cref="BinaryDocValues"/> for this field, or
        /// <c>null</c> if no <see cref="BinaryDocValues"/> were indexed for
        /// this field. The returned instance should only be
        /// used by a single thread.
        /// </summary>
        public abstract BinaryDocValues GetBinaryDocValues(string field);

        /// <summary>
        /// Returns <see cref="SortedDocValues"/> for this field, or
        /// <c>null</c> if no <see cref="SortedDocValues"/> were indexed for
        /// this field. The returned instance should only be
        /// used by a single thread.
        /// </summary>
        public abstract SortedDocValues GetSortedDocValues(string field);

        /// <summary>
        /// Returns <see cref="SortedSetDocValues"/> for this field, or
        /// <c>null</c> if no <see cref="SortedSetDocValues"/> were indexed for
        /// this field. The returned instance should only be
        /// used by a single thread.
        /// </summary>
        public abstract SortedSetDocValues GetSortedSetDocValues(string field);

        /// <summary>
        /// Returns a <see cref="IBits"/> at the size of <c>reader.MaxDoc</c>,
        /// with turned on bits for each docid that does have a value for this field,
        /// or <c>null</c> if no <see cref="DocValues"/> were indexed for this field. The
        /// returned instance should only be used by a single thread.
        /// </summary>
        public abstract IBits GetDocsWithField(string field);

        /// <summary>
        /// Returns <see cref="NumericDocValues"/> representing norms
        /// for this field, or <c>null</c> if no <see cref="NumericDocValues"/>
        /// were indexed. The returned instance should only be
        /// used by a single thread.
        /// </summary>
        public abstract NumericDocValues GetNormValues(string field);

        /// <summary>
        /// Get the <see cref="Index.FieldInfos"/> describing all fields in
        /// this reader.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public abstract FieldInfos FieldInfos { get; }

        /// <summary>
        /// Returns the <see cref="IBits"/> representing live (not
        /// deleted) docs.  A set bit indicates the doc ID has not
        /// been deleted.  If this method returns <c>null</c> it means
        /// there are no deleted documents (all documents are
        /// live).
        /// <para/>
        /// The returned instance has been safely published for
        /// use by multiple threads without additional
        /// synchronization.
        /// </summary>
        public abstract IBits LiveDocs { get; }

        /// <summary>
        /// Checks consistency of this reader.
        /// <para/>
        /// Note that this may be costly in terms of I/O, e.g.
        /// may involve computing a checksum value against large data files.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        public abstract void CheckIntegrity();
    }
}