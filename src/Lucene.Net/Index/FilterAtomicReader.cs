using System;
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
    /// A <see cref="FilterAtomicReader"/> contains another <see cref="AtomicReader"/>, which it
    /// uses as its basic source of data, possibly transforming the data along the
    /// way or providing additional functionality. The class
    /// <see cref="FilterAtomicReader"/> itself simply implements all abstract methods
    /// of <see cref="IndexReader"/> with versions that pass all requests to the
    /// contained index reader. Subclasses of <see cref="FilterAtomicReader"/> may
    /// further override some of these methods and may also provide additional
    /// methods and fields.
    /// <para/><b>NOTE</b>: If you override <see cref="LiveDocs"/>, you will likely need
    /// to override <see cref="NumDocs"/> as well and vice-versa.
    /// <para/><b>NOTE</b>: If this <see cref="FilterAtomicReader"/> does not change the
    /// content the contained reader, you could consider overriding
    /// <see cref="IndexReader.CoreCacheKey"/> so that <see cref="Search.IFieldCache"/> and
    /// <see cref="Search.CachingWrapperFilter"/> share the same entries for this atomic reader
    /// and the wrapped one. <see cref="IndexReader.CombinedCoreAndDeletesKey"/> could be
    /// overridden as well if the <see cref="LiveDocs"/> are not changed
    /// either.
    /// </summary>
    public class FilterAtomicReader : AtomicReader
    {
        /// <summary>
        /// Get the wrapped instance by <paramref name="reader"/> as long as this reader is
        /// an intance of <see cref="FilterAtomicReader"/>.
        /// </summary>
        public static AtomicReader Unwrap(AtomicReader reader)
        {
            while (reader is FilterAtomicReader filterAtomicReader)
            {
                reader = filterAtomicReader.m_input;
            }
            return reader;
        }

        /// <summary>
        /// Base class for filtering <see cref="Index.Fields"/>
        /// implementations.
        /// </summary>
        public class FilterFields : Fields
        {
            /// <summary>
            /// The underlying <see cref="Index.Fields"/> instance. </summary>
            protected readonly Fields m_input;

            /// <summary>
            /// Creates a new <see cref="FilterFields"/>. </summary>
            /// <param name="input"> the underlying <see cref="Index.Fields"/> instance. </param>
            public FilterFields(Fields input)
            {
                this.m_input = input;
            }

            public override IEnumerator<string> GetEnumerator()
            {
                return m_input.GetEnumerator();
            }

            public override Terms GetTerms(string field)
            {
                return m_input.GetTerms(field);
            }

            public override int Count => m_input.Count;
        }

        /// <summary>
        /// Base class for filtering <see cref="Terms"/> implementations.
        /// <para/><b>NOTE</b>: If the order of terms and documents is not changed, and if
        /// these terms are going to be intersected with automata, you could consider
        /// overriding <see cref="Terms.Intersect"/> for better performance.
        /// </summary>
        public class FilterTerms : Terms
        {
            /// <summary>
            /// The underlying <see cref="Terms"/> instance. </summary>
            protected readonly Terms m_input;

            /// <summary>
            /// Creates a new <see cref="FilterTerms"/> </summary>
            /// <param name="input"> the underlying <see cref="Terms"/> instance. </param>
            public FilterTerms(Terms input)
            {
                this.m_input = input;
            }

            public override TermsEnum GetEnumerator() => m_input.GetEnumerator();

            public override TermsEnum GetEnumerator(TermsEnum reuse) => m_input.GetEnumerator(reuse);

            public override IComparer<BytesRef> Comparer => m_input.Comparer;

            public override long Count => m_input.Count;

            public override long SumTotalTermFreq => m_input.SumTotalTermFreq;

            public override long SumDocFreq => m_input.SumDocFreq;

            public override int DocCount => m_input.DocCount;

            public override bool HasFreqs => m_input.HasFreqs;

            public override bool HasOffsets => m_input.HasOffsets;

            public override bool HasPositions => m_input.HasPositions;

            public override bool HasPayloads => m_input.HasPayloads;
        }

        /// <summary>
        /// Base class for filtering <see cref="TermsEnum"/> implementations. </summary>
        public class FilterTermsEnum : TermsEnum
        {
            /// <summary>
            /// The underlying <see cref="TermsEnum"/> instance. </summary>
            protected internal readonly TermsEnum m_input;

            /// <summary>
            /// Creates a new <see cref="FilterTermsEnum"/> </summary>
            /// <param name="input"> the underlying <see cref="TermsEnum"/> instance. </param>
            public FilterTermsEnum(TermsEnum input)
            {
                this.m_input = input;
            }

            public override AttributeSource Attributes => m_input.Attributes;

            public override SeekStatus SeekCeil(BytesRef text)
            {
                return m_input.SeekCeil(text);
            }

            public override void SeekExact(long ord)
            {
                m_input.SeekExact(ord);
            }

            public override bool MoveNext()
            {
                if (m_input.MoveNext())
                    return m_input.Term != null;
                return false;
            }

            [Obsolete("Use MoveNext() and Term instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public override BytesRef Next()
            {
                if (MoveNext())
                    return m_input.Term;
                return null;
            }

            public override BytesRef Term => m_input.Term;

            public override long Ord => m_input.Ord;

            public override int DocFreq => m_input.DocFreq;

            public override long TotalTermFreq => m_input.TotalTermFreq;

            public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, DocsFlags flags)
            {
                return m_input.Docs(liveDocs, reuse, flags);
            }

            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags)
            {
                return m_input.DocsAndPositions(liveDocs, reuse, flags);
            }

            public override IComparer<BytesRef> Comparer => m_input.Comparer;
        }

        /// <summary>
        /// Base class for filtering <see cref="DocsEnum"/> implementations. </summary>
        public class FilterDocsEnum : DocsEnum
        {
            /// <summary>
            /// The underlying <see cref="DocsEnum"/> instance.
            /// </summary>
            protected internal DocsEnum m_input;

            /// <summary>
            /// Create a new <see cref="FilterDocsEnum"/> </summary>
            /// <param name="input"> the underlying <see cref="DocsEnum"/> instance. </param>
            public FilterDocsEnum(DocsEnum input)
            {
                this.m_input = input;
            }

            public override AttributeSource Attributes => m_input.Attributes;

            public override int DocID => m_input.DocID;

            public override int Freq => m_input.Freq;

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
        /// Base class for filtering <see cref="DocsAndPositionsEnum"/> implementations. </summary>
        public class FilterDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            /// <summary>
            /// The underlying <see cref="DocsAndPositionsEnum"/> instance. </summary>
            protected internal readonly DocsAndPositionsEnum m_input;

            /// <summary>
            /// Create a new <see cref="FilterDocsAndPositionsEnum"/> </summary>
            /// <param name="input"> the underlying <see cref="DocsAndPositionsEnum"/> instance. </param>
            public FilterDocsAndPositionsEnum(DocsAndPositionsEnum input)
            {
                this.m_input = input;
            }

            public override AttributeSource Attributes => m_input.Attributes;

            public override int DocID => m_input.DocID;

            public override int Freq => m_input.Freq;

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

            public override int StartOffset => m_input.StartOffset;

            public override int EndOffset => m_input.EndOffset;

            public override BytesRef GetPayload()
            {
                return m_input.GetPayload();
            }

            public override long GetCost()
            {
                return m_input.GetCost();
            }
        }

        /// <summary>
        /// The underlying <see cref="AtomicReader"/>. </summary>
        protected readonly AtomicReader m_input;

        /// <summary>
        /// Construct a <see cref="FilterAtomicReader"/> based on the specified base reader.
        /// <para/>
        /// Note that base reader is closed if this <see cref="FilterAtomicReader"/> is closed.
        /// </summary>
        /// <param name="input"> specified base reader. </param>
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

        public override FieldInfos FieldInfos => m_input.FieldInfos;

        public override Fields GetTermVectors(int docID)
        {
            EnsureOpen();
            return m_input.GetTermVectors(docID);
        }

        public override int NumDocs =>
            // Don't call ensureOpen() here (it could affect performance)
            m_input.NumDocs;

        public override int MaxDoc =>
            // Don't call ensureOpen() here (it could affect performance)
            m_input.MaxDoc;

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