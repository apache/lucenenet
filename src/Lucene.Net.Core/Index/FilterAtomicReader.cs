using System.Collections.Generic;
using System.Text;
using Lucene.Net.Search;

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
    using Bits = Lucene.Net.Util.Bits;
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
                reader = ((FilterAtomicReader)reader).input;
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
            protected readonly Fields input;

            /// <summary>
            /// Creates a new FilterFields. </summary>
            /// <param name="input"> the underlying Fields instance. </param>
            public FilterFields(Fields input)
            {
                this.input = input;
            }

            public override IEnumerator<string> GetEnumerator()
            {
                return input.GetEnumerator();
            }

            public override Terms Terms(string field)
            {
                return input.Terms(field);
            }

            public override int Size
            {
                get { return input.Size; }
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
            protected readonly Terms input;

            /// <summary>
            /// Creates a new FilterTerms </summary>
            /// <param name="input"> the underlying Terms instance. </param>
            public FilterTerms(Terms input)
            {
                this.input = input;
            }

            public override TermsEnum Iterator(TermsEnum reuse)
            {
                return input.Iterator(reuse);
            }

            public override IComparer<BytesRef> Comparator
            {
                get
                {
                    return input.Comparator;
                }
            }

            public override long Size
            {
                get { return input.Size; }
            }

            public override long SumTotalTermFreq
            {
                get
                {
                    return input.SumTotalTermFreq;
                }
            }

            public override long SumDocFreq
            {
                get
                {
                    return input.SumDocFreq;
                }
            }

            public override int DocCount
            {
                get
                {
                    return input.DocCount;
                }
            }

            public override bool HasFreqs
            {
                get { return input.HasFreqs; }
            }

            public override bool HasOffsets
            {
                get { return input.HasOffsets; }
            }

            public override bool HasPositions
            {
                get { return input.HasPositions; }
            }

            public override bool HasPayloads
            {
                get { return input.HasPayloads; }
            }
        }

        /// <summary>
        /// Base class for filtering <seealso cref="TermsEnum"/> implementations. </summary>
        public class FilterTermsEnum : TermsEnum
        {
            /// <summary>
            /// The underlying TermsEnum instance. </summary>
            protected internal readonly TermsEnum input;

            /// <summary>
            /// Creates a new FilterTermsEnum </summary>
            /// <param name="input"> the underlying TermsEnum instance. </param>
            public FilterTermsEnum(TermsEnum input)
            {
                this.input = input;
            }

            public override AttributeSource Attributes
            {
                get { return input.Attributes; }
            }

            public override SeekStatus SeekCeil(BytesRef text)
            {
                return input.SeekCeil(text);
            }

            public override void SeekExact(long ord)
            {
                input.SeekExact(ord);
            }

            public override BytesRef Next()
            {
                return input.Next();
            }

            public override BytesRef Term()
            {
                return input.Term();
            }

            public override long Ord()
            {
                return input.Ord();
            }

            public override int DocFreq()
            {
                return input.DocFreq();
            }

            public override long TotalTermFreq()
            {
                return input.TotalTermFreq();
            }

            public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
            {
                return input.Docs(liveDocs, reuse, flags);
            }

            public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
            {
                return input.DocsAndPositions(liveDocs, reuse, flags);
            }

            public override IComparer<BytesRef> Comparator
            {
                get
                {
                    return input.Comparator;
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
            protected internal DocsEnum input;

            /// <summary>
            /// Create a new FilterDocsEnum </summary>
            /// <param name="input"> the underlying DocsEnum instance. </param>
            public FilterDocsEnum(DocsEnum input)
            {
                this.input = input;
            }

            public override AttributeSource Attributes
            {
                get { return input.Attributes; }
            }

            public override int DocID()
            {
                return input.DocID();
            }

            public override int Freq
            {
                get { return input.Freq; }
            }

            public override int NextDoc()
            {
                return input.NextDoc();
            }

            public override int Advance(int target)
            {
                return input.Advance(target);
            }

            public override long Cost()
            {
                return input.Cost();
            }
        }

        /// <summary>
        /// Base class for filtering <seealso cref="DocsAndPositionsEnum"/> implementations. </summary>
        public class FilterDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            /// <summary>
            /// The underlying DocsAndPositionsEnum instance. </summary>
            protected internal readonly DocsAndPositionsEnum input;

            /// <summary>
            /// Create a new FilterDocsAndPositionsEnum </summary>
            /// <param name="input"> the underlying DocsAndPositionsEnum instance. </param>
            public FilterDocsAndPositionsEnum(DocsAndPositionsEnum input)
            {
                this.input = input;
            }

            public override AttributeSource Attributes
            {
                get { return input.Attributes; }
            }

            public override int DocID()
            {
                return input.DocID();
            }

            public override int Freq
            {
                get { return input.Freq; }
            }

            public override int NextDoc()
            {
                return input.NextDoc();
            }

            public override int Advance(int target)
            {
                return input.Advance(target);
            }

            public override int NextPosition()
            {
                return input.NextPosition();
            }

            public override int StartOffset
            {
                get { return input.StartOffset; }
            }

            public override int EndOffset
            {
                get { return input.EndOffset; }
            }

            public override BytesRef Payload
            {
                get
                {
                    return input.Payload;
                }
            }

            public override long Cost()
            {
                return input.Cost();
            }
        }

        /// <summary>
        /// The underlying AtomicReader. </summary>
        protected internal readonly AtomicReader input;

        /// <summary>
        /// <p>Construct a FilterAtomicReader based on the specified base reader.
        /// <p>Note that base reader is closed if this FilterAtomicReader is closed.</p> </summary>
        /// <param name="in"> specified base reader. </param>
        public FilterAtomicReader(AtomicReader input)
            : base()
        {
            this.input = input;
            input.RegisterParentReader(this);
        }

        public override Bits LiveDocs
        {
            get
            {
                EnsureOpen();
                return input.LiveDocs;
            }
        }

        public override FieldInfos FieldInfos
        {
            get
            {
                return input.FieldInfos;
            }
        }

        public override Fields GetTermVectors(int docID)
        {
            EnsureOpen();
            return input.GetTermVectors(docID);
        }

        public override int NumDocs
        {
            get
            {
                {
                    // Don't call ensureOpen() here (it could affect performance)
                    return input.NumDocs;
                }
            }
        }

        public override int MaxDoc
        {
            get
            {
                // Don't call ensureOpen() here (it could affect performance)
                return input.MaxDoc;
            }
        }

        public override void Document(int docID, StoredFieldVisitor visitor)
        {
            EnsureOpen();
            input.Document(docID, visitor);
        }

        protected internal override void DoClose()
        {
            input.Dispose();
        }

        public override Fields Fields
        {
            get
            {
                EnsureOpen();
                return input.Fields;
            }
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder("FilterAtomicReader(");
            buffer.Append(input);
            buffer.Append(')');
            return buffer.ToString();
        }

        public override NumericDocValues GetNumericDocValues(string field)
        {
            EnsureOpen();
            return input.GetNumericDocValues(field);
        }

        public override BinaryDocValues GetBinaryDocValues(string field)
        {
            EnsureOpen();
            return input.GetBinaryDocValues(field);
        }

        public override SortedDocValues GetSortedDocValues(string field)
        {
            EnsureOpen();
            return input.GetSortedDocValues(field);
        }

        public override SortedSetDocValues GetSortedSetDocValues(string field)
        {
            EnsureOpen();
            return input.GetSortedSetDocValues(field);
        }

        public override NumericDocValues GetNormValues(string field)
        {
            EnsureOpen();
            return input.GetNormValues(field);
        }

        public override Bits GetDocsWithField(string field)
        {
            EnsureOpen();
            return input.GetDocsWithField(field);
        }

        public override void CheckIntegrity()
        {
            EnsureOpen();
            input.CheckIntegrity();
        }
    }
}