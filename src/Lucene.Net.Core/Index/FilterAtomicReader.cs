using System.Collections.Generic;
using System.Text;

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
    using IBits = Lucene.Net.Util.IBits;
    using BytesRef = Lucene.Net.Util.BytesRef;

    /// <summary>
    ///  A <code>FilterAtomicReader</code> contains another AtomicReader, which it
    /// uses as its basic source of data, possibly transforming the data along the
    /// way or providing additional functionality. The class
    /// <code>FilterAtomicReader</code> itself simply implements all abstract methods
    /// of <code>IndexReader</code> with versions that pass all requests to the
    /// contained index reader. Subclasses of <code>FilterAtomicReader</code> may
    /// further override some of these methods and may also provide additional
    /// methods and fields.
    /// <p><b>NOTE</b>: If you override <seealso cref="#getLiveDocs()"/>, you will likely need
    /// to override <seealso cref="#numDocs()"/> as well and vice-versa.
    /// <p><b>NOTE</b>: If this <seealso cref="FilterAtomicReader"/> does not change the
    /// content the contained reader, you could consider overriding
    /// <seealso cref="#getCoreCacheKey()"/> so that <seealso cref="IFieldCache"/> and
    /// <seealso cref="CachingWrapperFilter"/> share the same entries for this atomic reader
    /// and the wrapped one. <seealso cref="#getCombinedCoreAndDeletesKey()"/> could be
    /// overridden as well if the <seealso cref="#getLiveDocs() live docs"/> are not changed
    /// either.
    /// </summary>
    public class FilterAtomicReader : AtomicReader
    {
        /// <summary>
        /// Get the wrapped instance by <code>reader</code> as long as this reader is
        ///  an intance of <seealso cref="FilterAtomicReader"/>.
        /// </summary>
        public static AtomicReader Unwrap(AtomicReader reader)
        {
            while (reader is FilterAtomicReader)
            {
                reader = ((FilterAtomicReader)reader).m_input;
            }
            return reader;
        }

        /// <summary>
        /// Base class for filtering <seealso cref="Fields"/>
        ///  implementations.
        /// </summary>
        public class FilterFields : Fields
        {
            /// <summary>
            /// The underlying Fields instance. </summary>
            protected readonly Fields m_input;

            /// <summary>
            /// Creates a new FilterFields. </summary>
            /// <param name="input"> the underlying Fields instance. </param>
            public FilterFields(Fields input)
            {
                this.m_input = input;
            }

            public override IEnumerator<string> GetEnumerator()
            {
                return m_input.GetEnumerator();
            }

            public override Terms Terms(string field)
            {
                return m_input.Terms(field);
            }

            public override int Count
            {
                get { return m_input.Count; }
            }
        }

        /// <summary>
        /// Base class for filtering <seealso cref="Terms"/> implementations.
        /// <p><b>NOTE</b>: If the order of terms and documents is not changed, and if
        /// these terms are going to be intersected with automata, you could consider
        /// overriding <seealso cref="#intersect"/> for better performance.
        /// </summary>
        public class FilterTerms : Terms
        {
            /// <summary>
            /// The underlying Terms instance. </summary>
            protected readonly Terms m_input;

            /// <summary>
            /// Creates a new FilterTerms </summary>
            /// <param name="input"> the underlying Terms instance. </param>
            public FilterTerms(Terms input)
            {
                this.m_input = input;
            }

            public override TermsEnum Iterator(TermsEnum reuse)
            {
                return m_input.Iterator(reuse);
            }

            public override IComparer<BytesRef> Comparer
            {
                get
                {
                    return m_input.Comparer;
                }
            }

            public override long Count
            {
                get { return m_input.Count; }
            }

            public override long SumTotalTermFreq
            {
                get
                {
                    return m_input.SumTotalTermFreq;
                }
            }

            public override long SumDocFreq
            {
                get
                {
                    return m_input.SumDocFreq;
                }
            }

            public override int DocCount
            {
                get
                {
                    return m_input.DocCount;
                }
            }

            public override bool HasFreqs
            {
                get { return m_input.HasFreqs; }
            }

            public override bool HasOffsets
            {
                get { return m_input.HasOffsets; }
            }

            public override bool HasPositions
            {
                get { return m_input.HasPositions; }
            }

            public override bool HasPayloads
            {
                get { return m_input.HasPayloads; }
            }
        }

        /// <summary>
        /// Base class for filtering <seealso cref="TermsEnum"/> implementations. </summary>
        public class FilterTermsEnum : TermsEnum
        {
            /// <summary>
            /// The underlying TermsEnum instance. </summary>
            protected internal readonly TermsEnum m_input;

            /// <summary>
            /// Creates a new FilterTermsEnum </summary>
            /// <param name="input"> the underlying TermsEnum instance. </param>
            public FilterTermsEnum(TermsEnum input)
            {
                this.m_input = input;
            }

            public override AttributeSource Attributes
            {
                get { return m_input.Attributes; }
            }

            public override SeekStatus SeekCeil(BytesRef text)
            {
                return m_input.SeekCeil(text);
            }

            public override void SeekExact(long ord)
            {
                m_input.SeekExact(ord);
            }

            public override BytesRef Next()
            {
                return m_input.Next();
            }

            public override BytesRef Term
            {
                get { return m_input.Term; }
            }

            public override long Ord
            {
                get { return m_input.Ord; }
            }

            public override int DocFreq
            {
                get { return m_input.DocFreq; }
            }

            public override long TotalTermFreq
            {
                get { return m_input.TotalTermFreq; }
            }

            public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, int flags)
            {
                return m_input.Docs(liveDocs, reuse, flags);
            }

            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, int flags)
            {
                return m_input.DocsAndPositions(liveDocs, reuse, flags);
            }

            public override IComparer<BytesRef> Comparer
            {
                get
                {
                    return m_input.Comparer;
                }
            }
        }

        /// <summary>
        /// Base class for filtering <seealso cref="DocsEnum"/> implementations. </summary>
        public class FilterDocsEnum : DocsEnum
        {
            /// <summary>
            /// The underlying DocsEnum instance.
            /// </summary>
            protected internal DocsEnum m_input;

            /// <summary>
            /// Create a new FilterDocsEnum </summary>
            /// <param name="input"> the underlying DocsEnum instance. </param>
            public FilterDocsEnum(DocsEnum input)
            {
                this.m_input = input;
            }

            public override AttributeSource Attributes
            {
                get { return m_input.Attributes; }
            }

            public override int DocID
            {
                get { return m_input.DocID; }
            }

            public override int Freq
            {
                get { return m_input.Freq; }
            }

            public override int NextDoc()
            {
                return m_input.NextDoc();
            }

            public override int Advance(int target)
            {
                return m_input.Advance(target);
            }

            public override long GetCost()
            {
                return m_input.GetCost();
            }
        }

        /// <summary>
        /// Base class for filtering <seealso cref="DocsAndPositionsEnum"/> implementations. </summary>
        public class FilterDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            /// <summary>
            /// The underlying DocsAndPositionsEnum instance. </summary>
            protected internal readonly DocsAndPositionsEnum m_input;

            /// <summary>
            /// Create a new FilterDocsAndPositionsEnum </summary>
            /// <param name="input"> the underlying DocsAndPositionsEnum instance. </param>
            public FilterDocsAndPositionsEnum(DocsAndPositionsEnum input)
            {
                this.m_input = input;
            }

            public override AttributeSource Attributes
            {
                get { return m_input.Attributes; }
            }

            public override int DocID
            {
                get { return m_input.DocID; }
            }

            public override int Freq
            {
                get { return m_input.Freq; }
            }

            public override int NextDoc()
            {
                return m_input.NextDoc();
            }

            public override int Advance(int target)
            {
                return m_input.Advance(target);
            }

            public override int NextPosition()
            {
                return m_input.NextPosition();
            }

            public override int StartOffset
            {
                get { return m_input.StartOffset; }
            }

            public override int EndOffset
            {
                get { return m_input.EndOffset; }
            }

            public override BytesRef Payload
            {
                get
                {
                    return m_input.Payload;
                }
            }

            public override long GetCost()
            {
                return m_input.GetCost();
            }
        }

        /// <summary>
        /// The underlying AtomicReader. </summary>
        protected readonly AtomicReader m_input;

        /// <summary>
        /// <p>Construct a FilterAtomicReader based on the specified base reader.
        /// <p>Note that base reader is closed if this FilterAtomicReader is closed.</p> </summary>
        /// <param name="in"> specified base reader. </param>
        public FilterAtomicReader(AtomicReader input)
            : base()
        {
            this.m_input = input;
            input.RegisterParentReader(this);
        }

        public override IBits LiveDocs
        {
            get
            {
                EnsureOpen();
                return m_input.LiveDocs;
            }
        }

        public override FieldInfos FieldInfos
        {
            get
            {
                return m_input.FieldInfos;
            }
        }

        public override Fields GetTermVectors(int docID)
        {
            EnsureOpen();
            return m_input.GetTermVectors(docID);
        }

        public override int NumDocs
        {
            get
            {
                {
                    // Don't call ensureOpen() here (it could affect performance)
                    return m_input.NumDocs;
                }
            }
        }

        public override int MaxDoc
        {
            get
            {
                // Don't call ensureOpen() here (it could affect performance)
                return m_input.MaxDoc;
            }
        }

        public override void Document(int docID, StoredFieldVisitor visitor)
        {
            EnsureOpen();
            m_input.Document(docID, visitor);
        }

        protected internal override void DoClose()
        {
            m_input.Dispose();
        }

        public override Fields Fields
        {
            get
            {
                EnsureOpen();
                return m_input.Fields;
            }
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder("FilterAtomicReader(");
            buffer.Append(m_input);
            buffer.Append(')');
            return buffer.ToString();
        }

        public override NumericDocValues GetNumericDocValues(string field)
        {
            EnsureOpen();
            return m_input.GetNumericDocValues(field);
        }

        public override BinaryDocValues GetBinaryDocValues(string field)
        {
            EnsureOpen();
            return m_input.GetBinaryDocValues(field);
        }

        public override SortedDocValues GetSortedDocValues(string field)
        {
            EnsureOpen();
            return m_input.GetSortedDocValues(field);
        }

        public override SortedSetDocValues GetSortedSetDocValues(string field)
        {
            EnsureOpen();
            return m_input.GetSortedSetDocValues(field);
        }

        public override NumericDocValues GetNormValues(string field)
        {
            EnsureOpen();
            return m_input.GetNormValues(field);
        }

        public override IBits GetDocsWithField(string field)
        {
            EnsureOpen();
            return m_input.GetDocsWithField(field);
        }

        public override void CheckIntegrity()
        {
            EnsureOpen();
            m_input.CheckIntegrity();
        }
    }
}