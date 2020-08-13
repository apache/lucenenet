using Lucene.Net.Diagnostics;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using System;
using System.Collections.Generic;

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

    /// <summary>
    /// Wraps a <see cref="Index.Fields"/> but with additional asserts
    /// </summary>
    public class AssertingFields : FilterAtomicReader.FilterFields
    {
        public AssertingFields(Fields input)
            : base(input)
        { }

        public override IEnumerator<string> GetEnumerator()
        {
            IEnumerator<string> iterator = base.GetEnumerator();
            Debugging.Assert(() => iterator != null);
            return iterator;
        }

        public override Terms GetTerms(string field)
        {
            Terms terms = base.GetTerms(field);
            return terms == null ? null : new AssertingTerms(terms);
        }
    }

    /// <summary>
    /// Wraps a <see cref="Terms"/> but with additional asserts
    /// </summary>
    public class AssertingTerms : FilterAtomicReader.FilterTerms
    {
        public AssertingTerms(Terms input)
            : base(input)
        { }

        public override TermsEnum Intersect(CompiledAutomaton automaton, BytesRef bytes)
        {
            TermsEnum termsEnum = m_input.Intersect(automaton, bytes);
            Debugging.Assert(() => termsEnum != null);
            Debugging.Assert(() => bytes == null || bytes.IsValid());
            return new AssertingAtomicReader.AssertingTermsEnum(termsEnum);
        }

        public override TermsEnum GetIterator(TermsEnum reuse)
        {
            // TODO: should we give this thing a random to be super-evil,
            // and randomly *not* unwrap?
            if (reuse is AssertingAtomicReader.AssertingTermsEnum)
            {
                reuse = ((AssertingAtomicReader.AssertingTermsEnum)reuse).m_input;
            }
            TermsEnum termsEnum = base.GetIterator(reuse);
            Debugging.Assert(() => termsEnum != null);
            return new AssertingAtomicReader.AssertingTermsEnum(termsEnum);
        }
    }

    internal enum DocsEnumState
    {
        START,
        ITERATING,
        FINISHED
    }

    /// <summary>
    /// Wraps a <see cref="DocsEnum"/> with additional checks </summary>
    public class AssertingDocsEnum : FilterAtomicReader.FilterDocsEnum
    {
        private DocsEnumState state = DocsEnumState.START;
        private int doc;

        public AssertingDocsEnum(DocsEnum @in)
            : this(@in, true)
        { }

        public AssertingDocsEnum(DocsEnum @in, bool failOnUnsupportedDocID)
            : base(@in)
        {
            try
            {
                int docid = @in.DocID;
                Debugging.Assert(() => docid == -1, () => @in.GetType() + ": invalid initial doc id: " + docid);
            }
            catch (NotSupportedException /*e*/)
            {
                if (failOnUnsupportedDocID)
                {
                    throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                }
            }
            doc = -1;
        }

        public override int NextDoc()
        {
            Debugging.Assert(() => state != DocsEnumState.FINISHED, () => "NextDoc() called after NO_MORE_DOCS");
            int nextDoc = base.NextDoc();
            Debugging.Assert(() => nextDoc > doc, () => "backwards NextDoc from " + doc + " to " + nextDoc + " " + m_input);
            if (nextDoc == DocIdSetIterator.NO_MORE_DOCS)
            {
                state = DocsEnumState.FINISHED;
            }
            else
            {
                state = DocsEnumState.ITERATING;
            }
            Debugging.Assert(() => base.DocID == nextDoc);
            return doc = nextDoc;
        }

        public override int Advance(int target)
        {
            Debugging.Assert(() => state != DocsEnumState.FINISHED, () => "Advance() called after NO_MORE_DOCS");
            Debugging.Assert(() => target > doc, () => "target must be > DocID, got " + target + " <= " + doc);
            int advanced = base.Advance(target);
            Debugging.Assert(() => advanced >= target, () => "backwards advance from: " + target + " to: " + advanced);
            if (advanced == DocIdSetIterator.NO_MORE_DOCS)
            {
                state = DocsEnumState.FINISHED;
            }
            else
            {
                state = DocsEnumState.ITERATING;
            }
            Debugging.Assert(() => base.DocID == advanced);
            return doc = advanced;
        }

        public override int DocID
        {
            get
            {
                Debugging.Assert(() => doc == base.DocID, () => " invalid DocID in " + m_input.GetType() + " " + base.DocID + " instead of " + doc);
                return doc;
            }
        }

        public override int Freq
        {
            get
            {
                Debugging.Assert(() => state != DocsEnumState.START, () => "Freq called before NextDoc()/Advance()");
                Debugging.Assert(() => state != DocsEnumState.FINISHED, () => "Freq called after NO_MORE_DOCS");
                int freq = base.Freq;
                Debugging.Assert(() => freq > 0);
                return freq;
            }
        }
    }

    /// <summary>
    /// Wraps a <see cref="NumericDocValues"/> but with additional asserts </summary>
    public class AssertingNumericDocValues : NumericDocValues
    {
        private readonly NumericDocValues @in;
        private readonly int maxDoc;

        public AssertingNumericDocValues(NumericDocValues @in, int maxDoc)
        {
            this.@in = @in;
            this.maxDoc = maxDoc;
        }

        public override long Get(int docID)
        {
            Debugging.Assert(() => docID >= 0 && docID < maxDoc);
            return @in.Get(docID);
        }
    }

    /// <summary>
    /// Wraps a <see cref="BinaryDocValues"/> but with additional asserts </summary>
    public class AssertingBinaryDocValues : BinaryDocValues
    {
        private readonly BinaryDocValues @in;
        private readonly int maxDoc;

        public AssertingBinaryDocValues(BinaryDocValues @in, int maxDoc)
        {
            this.@in = @in;
            this.maxDoc = maxDoc;
        }

        public override void Get(int docID, BytesRef result)
        {
            Debugging.Assert(() => docID >= 0 && docID < maxDoc);
            Debugging.Assert(result.IsValid);
            @in.Get(docID, result);
            Debugging.Assert(result.IsValid);
        }
    }

    /// <summary>
    /// Wraps a <see cref="SortedDocValues"/> but with additional asserts </summary>
    public class AssertingSortedDocValues : SortedDocValues
    {
        private readonly SortedDocValues @in;
        private readonly int maxDoc;
        private readonly int valueCount;

        public AssertingSortedDocValues(SortedDocValues @in, int maxDoc)
        {
            this.@in = @in;
            this.maxDoc = maxDoc;
            this.valueCount = @in.ValueCount;
            Debugging.Assert(() => valueCount >= 0 && valueCount <= maxDoc);
        }

        public override int GetOrd(int docID)
        {
            Debugging.Assert(() => docID >= 0 && docID < maxDoc);
            int ord = @in.GetOrd(docID);
            Debugging.Assert(() => ord >= -1 && ord < valueCount);
            return ord;
        }

        public override void LookupOrd(int ord, BytesRef result)
        {
            Debugging.Assert(() => ord >= 0 && ord < valueCount);
            Debugging.Assert(result.IsValid);
            @in.LookupOrd(ord, result);
            Debugging.Assert(result.IsValid);
        }

        public override int ValueCount
        {
            get
            {
                int valueCount = @in.ValueCount;
                Debugging.Assert(() => valueCount == this.valueCount); // should not change
                return valueCount;
            }
        }

        public override void Get(int docID, BytesRef result)
        {
            Debugging.Assert(() => docID >= 0 && docID < maxDoc);
            Debugging.Assert(result.IsValid);
            @in.Get(docID, result);
            Debugging.Assert(result.IsValid);
        }

        public override int LookupTerm(BytesRef key)
        {
            Debugging.Assert(key.IsValid);
            int result = @in.LookupTerm(key);
            Debugging.Assert(() => result < valueCount);
            Debugging.Assert(key.IsValid);
            return result;
        }
    }

    /// <summary>
    /// Wraps a <see cref="SortedSetDocValues"/> but with additional asserts </summary>
    public class AssertingSortedSetDocValues : SortedSetDocValues
    {
        private readonly SortedSetDocValues @in;
        private readonly int maxDoc;
        private readonly long valueCount;
        private long lastOrd = NO_MORE_ORDS;

        public AssertingSortedSetDocValues(SortedSetDocValues @in, int maxDoc)
        {
            this.@in = @in;
            this.maxDoc = maxDoc;
            this.valueCount = @in.ValueCount;
            Debugging.Assert(() => valueCount >= 0);
        }

        public override long NextOrd()
        {
            Debugging.Assert(() => lastOrd != NO_MORE_ORDS);
            long ord = @in.NextOrd();
            Debugging.Assert(() => ord < valueCount);
            Debugging.Assert(() => ord == NO_MORE_ORDS || ord > lastOrd);
            lastOrd = ord;
            return ord;
        }

        public override void SetDocument(int docID)
        {
            Debugging.Assert(() => docID >= 0 && docID < maxDoc, () => "docid=" + docID + ",maxDoc=" + maxDoc);
            @in.SetDocument(docID);
            lastOrd = -2;
        }

        public override void LookupOrd(long ord, BytesRef result)
        {
            Debugging.Assert(() => ord >= 0 && ord < valueCount);
            Debugging.Assert(result.IsValid);
            @in.LookupOrd(ord, result);
            Debugging.Assert(result.IsValid);
        }

        public override long ValueCount
        {
            get
            {
                long valueCount = @in.ValueCount;
                Debugging.Assert(() => valueCount == this.valueCount); // should not change
                return valueCount;
            }
        }

        public override long LookupTerm(BytesRef key)
        {
            Debugging.Assert(key.IsValid);
            long result = @in.LookupTerm(key);
            Debugging.Assert(() => result < valueCount);
            Debugging.Assert(key.IsValid);
            return result;
        }
    }

    /// <summary>
    /// Wraps a <see cref="IBits"/> but with additional asserts </summary>
    public class AssertingBits : IBits
    {
        internal readonly IBits @in;

        public AssertingBits(IBits @in)
        {
            this.@in = @in;
        }

        public virtual bool Get(int index)
        {
            Debugging.Assert(() => index >= 0 && index < Length);
            return @in.Get(index);
        }

        public virtual int Length => @in.Length;
    }

    /// <summary>
    /// A <see cref="FilterAtomicReader"/> that can be used to apply
    /// additional checks for tests.
    /// </summary>
    public class AssertingAtomicReader : FilterAtomicReader
    {
        public AssertingAtomicReader(AtomicReader @in)
            : base(@in)
        {
            // check some basic reader sanity
            Debugging.Assert(() => @in.MaxDoc >= 0);
            Debugging.Assert(() => @in.NumDocs <= @in.MaxDoc);
            Debugging.Assert(() => @in.NumDeletedDocs + @in.NumDocs == @in.MaxDoc);
            Debugging.Assert(() => !@in.HasDeletions || @in.NumDeletedDocs > 0 && @in.NumDocs < @in.MaxDoc);
        }

        public override Fields Fields
        {
            get
            {
                Fields fields = base.Fields;
                return fields == null ? null : new AssertingFields(fields);
            }
        }

        public override Fields GetTermVectors(int docID)
        {
            Fields fields = base.GetTermVectors(docID);
            return fields == null ? null : new AssertingFields(fields);
        }

        // LUCENENET specific - de-nested AssertingFields

        // LUCENENET specific - de-nested AssertingTerms

        // LUCENENET specific - de-nested AssertingTermsEnum

        internal class AssertingTermsEnum : FilterTermsEnum
        {
            private enum State
            {
                INITIAL,
                POSITIONED,
                UNPOSITIONED
            }

            private State state = State.INITIAL;

            public AssertingTermsEnum(TermsEnum @in)
                : base(@in)
            { }

            public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, DocsFlags flags)
            {
                Debugging.Assert(() => state == State.POSITIONED, () => "Docs(...) called on unpositioned TermsEnum");

                // TODO: should we give this thing a random to be super-evil,
                // and randomly *not* unwrap?
                if (reuse is AssertingDocsEnum)
                {
                    reuse = ((AssertingDocsEnum)reuse).m_input;
                }
                DocsEnum docs = base.Docs(liveDocs, reuse, flags);
                return docs == null ? null : new AssertingDocsEnum(docs);
            }

            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags)
            {
                Debugging.Assert(() => state == State.POSITIONED, () => "DocsAndPositions(...) called on unpositioned TermsEnum");

                // TODO: should we give this thing a random to be super-evil,
                // and randomly *not* unwrap?
                if (reuse is AssertingDocsAndPositionsEnum)
                {
                    reuse = ((AssertingDocsAndPositionsEnum)reuse).m_input;
                }
                DocsAndPositionsEnum docs = base.DocsAndPositions(liveDocs, reuse, flags);
                return docs == null ? null : new AssertingDocsAndPositionsEnum(docs);
            }

            // TODO: we should separately track if we are 'at the end' ?
            // someone should not call next() after it returns null!!!!
            public override BytesRef Next()
            {
                Debugging.Assert(() => state == State.INITIAL || state == State.POSITIONED, () => "Next() called on unpositioned TermsEnum");
                BytesRef result = base.Next();
                if (result == null)
                {
                    state = State.UNPOSITIONED;
                }
                else
                {
                    Debugging.Assert(result.IsValid);
                    state = State.POSITIONED;
                }
                return result;
            }

            public override long Ord
            {
                get
                {
                    Debugging.Assert(() => state == State.POSITIONED, () => "Ord called on unpositioned TermsEnum");
                    return base.Ord;
                }
            }

            public override int DocFreq
            {
                get
                {
                    Debugging.Assert(() => state == State.POSITIONED, () => "DocFreq called on unpositioned TermsEnum");
                    return base.DocFreq;
                }
            }

            public override long TotalTermFreq
            {
                get
                {
                    Debugging.Assert(() => state == State.POSITIONED, () => "TotalTermFreq called on unpositioned TermsEnum");
                    return base.TotalTermFreq;
                }
            }

            public override BytesRef Term
            {
                get
                {
                    Debugging.Assert(() => state == State.POSITIONED, () => "Term called on unpositioned TermsEnum");
                    BytesRef ret = base.Term;
                    Debugging.Assert(() => ret == null || ret.IsValid());
                    return ret;
                }
            }

            public override void SeekExact(long ord)
            {
                base.SeekExact(ord);
                state = State.POSITIONED;
            }

            public override SeekStatus SeekCeil(BytesRef term)
            {
                Debugging.Assert(term.IsValid);
                SeekStatus result = base.SeekCeil(term);
                if (result == SeekStatus.END)
                {
                    state = State.UNPOSITIONED;
                }
                else
                {
                    state = State.POSITIONED;
                }
                return result;
            }

            public override bool SeekExact(BytesRef text)
            {
                Debugging.Assert(text.IsValid);
                if (base.SeekExact(text))
                {
                    state = State.POSITIONED;
                    return true;
                }
                else
                {
                    state = State.UNPOSITIONED;
                    return false;
                }
            }

            public override TermState GetTermState()
            {
                Debugging.Assert(() => state == State.POSITIONED, () => "GetTermState() called on unpositioned TermsEnum");
                return base.GetTermState();
            }

            public override void SeekExact(BytesRef term, TermState state)
            {
                Debugging.Assert(term.IsValid);
                base.SeekExact(term, state);
                this.state = State.POSITIONED;
            }
        }

        // LUCENENET specific - de-nested DocsEnumState

        // LUCENENET specific - de-nested AssertingDocsEnum

        internal class AssertingDocsAndPositionsEnum : FilterDocsAndPositionsEnum
        {
            private DocsEnumState state = DocsEnumState.START;
            private int positionMax = 0;
            private int positionCount = 0;
            private int doc;

            public AssertingDocsAndPositionsEnum(DocsAndPositionsEnum @in)
                : base(@in)
            {
                int docid = @in.DocID;
                Debugging.Assert(() => docid == -1, () => "invalid initial doc id: " + docid);
                doc = -1;
            }

            public override int NextDoc()
            {
                Debugging.Assert(() => state != DocsEnumState.FINISHED, () => "NextDoc() called after NO_MORE_DOCS");
                int nextDoc = base.NextDoc();
                Debugging.Assert(() => nextDoc > doc, () => "backwards nextDoc from " + doc + " to " + nextDoc);
                positionCount = 0;
                if (nextDoc == DocIdSetIterator.NO_MORE_DOCS)
                {
                    state = DocsEnumState.FINISHED;
                    positionMax = 0;
                }
                else
                {
                    state = DocsEnumState.ITERATING;
                    positionMax = base.Freq;
                }
                Debugging.Assert(() => base.DocID == nextDoc);
                return doc = nextDoc;
            }

            public override int Advance(int target)
            {
                Debugging.Assert(() => state != DocsEnumState.FINISHED, () => "Advance() called after NO_MORE_DOCS");
                Debugging.Assert(() => target > doc, () => "target must be > DocID, got " + target + " <= " + doc);
                int advanced = base.Advance(target);
                Debugging.Assert(() => advanced >= target, () => "backwards advance from: " + target + " to: " + advanced);
                positionCount = 0;
                if (advanced == DocIdSetIterator.NO_MORE_DOCS)
                {
                    state = DocsEnumState.FINISHED;
                    positionMax = 0;
                }
                else
                {
                    state = DocsEnumState.ITERATING;
                    positionMax = base.Freq;
                }
                Debugging.Assert(() => base.DocID == advanced);
                return doc = advanced;
            }

            public override int DocID
            {
                get
                {
                    Debugging.Assert(() => doc == base.DocID, () => " invalid DocID in " + m_input.GetType() + " " + base.DocID + " instead of " + doc);
                    return doc;
                }
            }

            public override int Freq
            {
                get
                {
                    Debugging.Assert(() => state != DocsEnumState.START, () => "Freq called before NextDoc()/Advance()");
                    Debugging.Assert(() => state != DocsEnumState.FINISHED, () => "Freq called after NO_MORE_DOCS");
                    int freq = base.Freq;
                    Debugging.Assert(() => freq > 0);
                    return freq;
                }
            }

            public override int NextPosition()
            {
                Debugging.Assert(() => state != DocsEnumState.START, () => "NextPosition() called before NextDoc()/Advance()");
                Debugging.Assert(() => state != DocsEnumState.FINISHED, () => "NextPosition() called after NO_MORE_DOCS");
                Debugging.Assert(() => positionCount < positionMax, () => "NextPosition() called more than Freq times!");
                int position = base.NextPosition();
                Debugging.Assert(() => position >= 0 || position == -1, () => "invalid position: " + position);
                positionCount++;
                return position;
            }

            public override int StartOffset
            {
                get
                {
                    Debugging.Assert(() => state != DocsEnumState.START, () => "StartOffset called before NextDoc()/Advance()");
                    Debugging.Assert(() => state != DocsEnumState.FINISHED, () => "StartOffset called after NO_MORE_DOCS");
                    Debugging.Assert(() => positionCount > 0, () => "StartOffset called before NextPosition()!");
                    return base.StartOffset;
                }
            }

            public override int EndOffset
            {
                get
                {
                    Debugging.Assert(() => state != DocsEnumState.START, () => "EndOffset called before NextDoc()/Advance()");
                    Debugging.Assert(() => state != DocsEnumState.FINISHED, () => "EndOffset called after NO_MORE_DOCS");
                    Debugging.Assert(() => positionCount > 0, () => "EndOffset called before NextPosition()!");
                    return base.EndOffset;
                }
            }

            public override BytesRef GetPayload()
            {
                Debugging.Assert(() => state != DocsEnumState.START, () => "GetPayload() called before NextDoc()/Advance()");
                Debugging.Assert(() => state != DocsEnumState.FINISHED, () => "GetPayload() called after NO_MORE_DOCS");
                Debugging.Assert(() => positionCount > 0, () => "GetPayload() called before NextPosition()!");
                BytesRef payload = base.GetPayload();
                Debugging.Assert(() => payload == null || payload.IsValid() && payload.Length > 0, () => "GetPayload() returned payload with invalid length!");
                return payload;
            }
        }

        // LUCENENET specific - de-nested AssertingNumericDocValues

        // LUCENENET specific - de-nested AssertingBinaryDocValues

        // LUCENENET specific - de-nested AssertingSortedDocValues

        // LUCENENET specific - de-nested AssertingSortedSetDocValues


        public override NumericDocValues GetNumericDocValues(string field)
        {
            NumericDocValues dv = base.GetNumericDocValues(field);
            FieldInfo fi = FieldInfos.FieldInfo(field);
            if (dv != null)
            {
                Debugging.Assert(() => fi != null);
                Debugging.Assert(() => fi.DocValuesType == DocValuesType.NUMERIC);
                return new AssertingNumericDocValues(dv, MaxDoc);
            }
            else
            {
                Debugging.Assert(() => fi == null || fi.DocValuesType != DocValuesType.NUMERIC);
                return null;
            }
        }

        public override BinaryDocValues GetBinaryDocValues(string field)
        {
            BinaryDocValues dv = base.GetBinaryDocValues(field);
            FieldInfo fi = FieldInfos.FieldInfo(field);
            if (dv != null)
            {
                Debugging.Assert(() => fi != null);
                Debugging.Assert(() => fi.DocValuesType == DocValuesType.BINARY);
                return new AssertingBinaryDocValues(dv, MaxDoc);
            }
            else
            {
                Debugging.Assert(() => fi == null || fi.DocValuesType != DocValuesType.BINARY);
                return null;
            }
        }

        public override SortedDocValues GetSortedDocValues(string field)
        {
            SortedDocValues dv = base.GetSortedDocValues(field);
            FieldInfo fi = FieldInfos.FieldInfo(field);
            if (dv != null)
            {
                Debugging.Assert(() => fi != null);
                Debugging.Assert(() => fi.DocValuesType == DocValuesType.SORTED);
                return new AssertingSortedDocValues(dv, MaxDoc);
            }
            else
            {
                Debugging.Assert(() => fi == null || fi.DocValuesType != DocValuesType.SORTED);
                return null;
            }
        }

        public override SortedSetDocValues GetSortedSetDocValues(string field)
        {
            SortedSetDocValues dv = base.GetSortedSetDocValues(field);
            FieldInfo fi = FieldInfos.FieldInfo(field);
            if (dv != null)
            {
                Debugging.Assert(() => fi != null);
                Debugging.Assert(() => fi.DocValuesType == DocValuesType.SORTED_SET);
                return new AssertingSortedSetDocValues(dv, MaxDoc);
            }
            else
            {
                Debugging.Assert(() => fi == null || fi.DocValuesType != DocValuesType.SORTED_SET);
                return null;
            }
        }

        public override NumericDocValues GetNormValues(string field)
        {
            NumericDocValues dv = base.GetNormValues(field);
            FieldInfo fi = FieldInfos.FieldInfo(field);
            if (dv != null)
            {
                Debugging.Assert(() => fi != null);
                Debugging.Assert(() => fi.HasNorms);
                return new AssertingNumericDocValues(dv, MaxDoc);
            }
            else
            {
                Debugging.Assert(() => fi == null || fi.HasNorms == false);
                return null;
            }
        }

        // LUCENENET specific - de-nested AssertingBits

        public override IBits LiveDocs
        {
            get
            {
                IBits liveDocs = base.LiveDocs;
                if (liveDocs != null)
                {
                    Debugging.Assert(() => MaxDoc == liveDocs.Length);
                    liveDocs = new AssertingBits(liveDocs);
                }
                else
                {
                    Debugging.Assert(() => MaxDoc == NumDocs);
                    Debugging.Assert(() => !HasDeletions);
                }
                return liveDocs;
            }
        }

        public override IBits GetDocsWithField(string field)
        {
            IBits docsWithField = base.GetDocsWithField(field);
            FieldInfo fi = FieldInfos.FieldInfo(field);
            if (docsWithField != null)
            {
                Debugging.Assert(() => fi != null);
                Debugging.Assert(() => fi.HasDocValues);
                Debugging.Assert(() => MaxDoc == docsWithField.Length);
                docsWithField = new AssertingBits(docsWithField);
            }
            else
            {
                Debugging.Assert(() => fi == null || fi.HasDocValues == false);
            }
            return docsWithField;
        }

        // this is the same hack as FCInvisible
        public override object CoreCacheKey => cacheKey;

        public override object CombinedCoreAndDeletesKey => cacheKey;


        private readonly object cacheKey = new object();
    }
}