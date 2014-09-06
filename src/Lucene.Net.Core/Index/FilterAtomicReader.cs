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
    /// <seealso cref="#getCoreCacheKey()"/> so that <seealso cref="FieldCache"/> and
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
                reader = ((FilterAtomicReader)reader).@in;
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
            protected internal readonly Fields @in;

            /// <summary>
            /// Creates a new FilterFields. </summary>
            /// <param name="in"> the underlying Fields instance. </param>
            public FilterFields(Fields @in)
            {
                this.@in = @in;
            }

            public override IEnumerator<string> GetEnumerator()
            {
                return @in.GetEnumerator();
            }

            public override Terms Terms(string field)
            {
                return @in.Terms(field);
            }

            public override int Size()
            {
                return @in.Size();
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
            protected internal readonly Terms @in;

            /// <summary>
            /// Creates a new FilterTerms </summary>
            /// <param name="in"> the underlying Terms instance. </param>
            public FilterTerms(Terms @in)
            {
                this.@in = @in;
            }

            public override TermsEnum Iterator(TermsEnum reuse)
            {
                return @in.Iterator(reuse);
            }

            public override IComparer<BytesRef> Comparator
            {
                get
                {
                    return @in.Comparator;
                }
            }

            public override long Size()
            {
                return @in.Size();
            }

            public override long SumTotalTermFreq
            {
                get
                {
                    return @in.SumTotalTermFreq;
                }
            }

            public override long SumDocFreq
            {
                get
                {
                    return @in.SumDocFreq;
                }
            }

            public override int DocCount
            {
                get
                {
                    return @in.DocCount;
                }
            }

            public override bool HasFreqs()
            {
                return @in.HasFreqs();
            }

            public override bool HasOffsets()
            {
                return @in.HasOffsets();
            }

            public override bool HasPositions()
            {
                return @in.HasPositions();
            }

            public override bool HasPayloads()
            {
                return @in.HasPayloads();
            }
        }

        /// <summary>
        /// Base class for filtering <seealso cref="TermsEnum"/> implementations. </summary>
        public class FilterTermsEnum : TermsEnum
        {
            /// <summary>
            /// The underlying TermsEnum instance. </summary>
            protected internal readonly TermsEnum @in;

            /// <summary>
            /// Creates a new FilterTermsEnum </summary>
            /// <param name="in"> the underlying TermsEnum instance. </param>
            public FilterTermsEnum(TermsEnum @in)
            {
                this.@in = @in;
            }

            public TermsEnum TermsEnumIn_Nunit()
            {
                return @in;
            }

            public override AttributeSource Attributes()
            {
                return @in.Attributes();
            }

            public override SeekStatus SeekCeil(BytesRef text)
            {
                return @in.SeekCeil(text);
            }

            public override void SeekExact(long ord)
            {
                @in.SeekExact(ord);
            }

            public override BytesRef Next()
            {
                return @in.Next();
            }

            public override BytesRef Term()
            {
                return @in.Term();
            }

            public override long Ord()
            {
                return @in.Ord();
            }

            public override int DocFreq()
            {
                return @in.DocFreq();
            }

            public override long TotalTermFreq()
            {
                return @in.TotalTermFreq();
            }

            public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
            {
                return @in.Docs(liveDocs, reuse, flags);
            }

            public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
            {
                return @in.DocsAndPositions(liveDocs, reuse, flags);
            }

            public override IComparer<BytesRef> Comparator
            {
                get
                {
                    return @in.Comparator;
                }
            }
        }

        /// <summary>
        /// Base class for filtering <seealso cref="DocsEnum"/> implementations. </summary>
        public class FilterDocsEnum : DocsEnum
        {
            /// <summary>
            /// The underlying DocsEnum instance. </summary>
            protected internal readonly DocsEnum @in;

            /// <summary>
            /// Create a new FilterDocsEnum </summary>
            /// <param name="in"> the underlying DocsEnum instance. </param>
            public FilterDocsEnum(DocsEnum @in)
            {
                this.@in = @in;
            }

            public DocsEnum DocsEnumIn_Nunit()
            {
                return @in;
            }

            public override AttributeSource Attributes()
            {
                return @in.Attributes();
            }

            public override int DocID()
            {
                return @in.DocID();
            }

            public override int Freq()
            {
                return @in.Freq();
            }

            public override int NextDoc()
            {
                return @in.NextDoc();
            }

            public override int Advance(int target)
            {
                return @in.Advance(target);
            }

            public override long Cost()
            {
                return @in.Cost();
            }
        }

        /// <summary>
        /// Base class for filtering <seealso cref="DocsAndPositionsEnum"/> implementations. </summary>
        public class FilterDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            /// <summary>
            /// The underlying DocsAndPositionsEnum instance. </summary>
            protected internal readonly DocsAndPositionsEnum @in;

            /// <summary>
            /// Create a new FilterDocsAndPositionsEnum </summary>
            /// <param name="in"> the underlying DocsAndPositionsEnum instance. </param>
            public FilterDocsAndPositionsEnum(DocsAndPositionsEnum @in)
            {
                this.@in = @in;
            }

            public DocsAndPositionsEnum DocsEnumIn_Nunit()
            {
                return @in;
            }

            public override AttributeSource Attributes()
            {
                return @in.Attributes();
            }

            public override int DocID()
            {
                return @in.DocID();
            }

            public override int Freq()
            {
                return @in.Freq();
            }

            public override int NextDoc()
            {
                return @in.NextDoc();
            }

            public override int Advance(int target)
            {
                return @in.Advance(target);
            }

            public override int NextPosition()
            {
                return @in.NextPosition();
            }

            public override int StartOffset()
            {
                return @in.StartOffset();
            }

            public override int EndOffset()
            {
                return @in.EndOffset();
            }

            public override BytesRef Payload
            {
                get
                {
                    return @in.Payload;
                }
            }

            public override long Cost()
            {
                return @in.Cost();
            }
        }

        /// <summary>
        /// The underlying AtomicReader. </summary>
        protected internal readonly AtomicReader @in;

        /// <summary>
        /// <p>Construct a FilterAtomicReader based on the specified base reader.
        /// <p>Note that base reader is closed if this FilterAtomicReader is closed.</p> </summary>
        /// <param name="in"> specified base reader. </param>
        public FilterAtomicReader(AtomicReader @in)
            : base()
        {
            this.@in = @in;
            @in.RegisterParentReader(this);
        }

        public override Bits LiveDocs
        {
            get
            {
                EnsureOpen();
                return @in.LiveDocs;
            }
        }

        public override FieldInfos FieldInfos
        {
            get
            {
                return @in.FieldInfos;
            }
        }

        public override Fields GetTermVectors(int docID)
        {
            EnsureOpen();
            return @in.GetTermVectors(docID);
        }

        public override int NumDocs()
        {
            // Don't call ensureOpen() here (it could affect performance)
            return @in.NumDocs();
        }

        public override int MaxDoc()
        {
            // Don't call ensureOpen() here (it could affect performance)
            return @in.MaxDoc();
        }

        public override void Document(int docID, StoredFieldVisitor visitor)
        {
            EnsureOpen();
            @in.Document(docID, visitor);
        }

        protected internal override void DoClose()
        {
            @in.Dispose();
        }

        public override Fields Fields()
        {
            EnsureOpen();
            return @in.Fields();
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder("FilterAtomicReader(");
            buffer.Append(@in);
            buffer.Append(')');
            return buffer.ToString();
        }

        public override NumericDocValues GetNumericDocValues(string field)
        {
            EnsureOpen();
            return @in.GetNumericDocValues(field);
        }

        public override BinaryDocValues GetBinaryDocValues(string field)
        {
            EnsureOpen();
            return @in.GetBinaryDocValues(field);
        }

        public override SortedDocValues GetSortedDocValues(string field)
        {
            EnsureOpen();
            return @in.GetSortedDocValues(field);
        }

        public override SortedSetDocValues GetSortedSetDocValues(string field)
        {
            EnsureOpen();
            return @in.GetSortedSetDocValues(field);
        }

        public override NumericDocValues GetNormValues(string field)
        {
            EnsureOpen();
            return @in.GetNormValues(field);
        }

        public override Bits GetDocsWithField(string field)
        {
            EnsureOpen();
            return @in.GetDocsWithField(field);
        }

        public override void CheckIntegrity()
        {
            EnsureOpen();
            @in.CheckIntegrity();
        }
    }
}