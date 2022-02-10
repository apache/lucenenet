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
            if (Debugging.AssertsEnabled) Debugging.Assert(iterator != null);
            return iterator;
        }

        public override Terms GetTerms(string field)
        {
            Terms terms = base.GetTerms(field);
            return terms is null ? null : new AssertingTerms(terms);
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
            if (Debugging.AssertsEnabled) Debugging.Assert(termsEnum != null);
            if (Debugging.AssertsEnabled) Debugging.Assert(bytes is null || bytes.IsValid());
            return new AssertingAtomicReader.AssertingTermsEnum(termsEnum);
        }

        public override TermsEnum GetEnumerator()
        {
            var termsEnum = base.GetEnumerator();
            if (Debugging.AssertsEnabled) Debugging.Assert(termsEnum != null);
            return new AssertingAtomicReader.AssertingTermsEnum(termsEnum);
        }

        public override TermsEnum GetEnumerator(TermsEnum reuse)
        {
            // TODO: should we give this thing a random to be super-evil,
            // and randomly *not* unwrap?
            if (!(reuse is null) && reuse is AssertingAtomicReader.AssertingTermsEnum reusable)
            {
                reuse = reusable.m_input;
            }
            TermsEnum termsEnum = base.GetEnumerator(reuse);
            if (Debugging.AssertsEnabled) Debugging.Assert(termsEnum != null);
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
                if (Debugging.AssertsEnabled) Debugging.Assert(docid == -1, "{0}: invalid initial doc id: {1}", @in.GetType(), docid);
            }
            catch (Exception e) when (e.IsUnsupportedOperationException())
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
            if (Debugging.AssertsEnabled) Debugging.Assert(state != DocsEnumState.FINISHED, "NextDoc() called after NO_MORE_DOCS");
            int nextDoc = base.NextDoc();
            if (Debugging.AssertsEnabled) Debugging.Assert(nextDoc > doc, "backwards NextDoc from {0} to {1} {2}", doc, nextDoc, m_input);
            if (nextDoc == DocIdSetIterator.NO_MORE_DOCS)
            {
                state = DocsEnumState.FINISHED;
            }
            else
            {
                state = DocsEnumState.ITERATING;
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(base.DocID == nextDoc);
            return doc = nextDoc;
        }

        public override int Advance(int target)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(state != DocsEnumState.FINISHED, "Advance() called after NO_MORE_DOCS");
            if (Debugging.AssertsEnabled) Debugging.Assert(target > doc,"target must be > DocID, got {0} <= {1}", target, doc);
            int advanced = base.Advance(target);
            if (Debugging.AssertsEnabled) Debugging.Assert(advanced >= target,"backwards advance from: {0} to: {1}", target, advanced);
            if (advanced == DocIdSetIterator.NO_MORE_DOCS)
            {
                state = DocsEnumState.FINISHED;
            }
            else
            {
                state = DocsEnumState.ITERATING;
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(base.DocID == advanced);
            return doc = advanced;
        }

        public override int DocID
        {
            get
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(doc == base.DocID, " invalid DocID in {0} {1} instead of {2}", m_input.GetType(), base.DocID, doc);
                return doc;
            }
        }

        public override int Freq
        {
            get
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(state != DocsEnumState.START, "Freq called before NextDoc()/Advance()");
                if (Debugging.AssertsEnabled) Debugging.Assert(state != DocsEnumState.FINISHED, "Freq called after NO_MORE_DOCS");
                int freq = base.Freq;
                if (Debugging.AssertsEnabled) Debugging.Assert(freq > 0);
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
            if (Debugging.AssertsEnabled) Debugging.Assert(docID >= 0 && docID < maxDoc);
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
            if (Debugging.AssertsEnabled) Debugging.Assert(docID >= 0 && docID < maxDoc);
            if (Debugging.AssertsEnabled) Debugging.Assert(result.IsValid());
            @in.Get(docID, result);
            if (Debugging.AssertsEnabled) Debugging.Assert(result.IsValid());
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
            if (Debugging.AssertsEnabled) Debugging.Assert(valueCount >= 0 && valueCount <= maxDoc);
        }

        public override int GetOrd(int docID)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(docID >= 0 && docID < maxDoc);
            int ord = @in.GetOrd(docID);
            if (Debugging.AssertsEnabled) Debugging.Assert(ord >= -1 && ord < valueCount);
            return ord;
        }

        public override void LookupOrd(int ord, BytesRef result)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(ord >= 0 && ord < valueCount);
            if (Debugging.AssertsEnabled) Debugging.Assert(result.IsValid());
            @in.LookupOrd(ord, result);
            if (Debugging.AssertsEnabled) Debugging.Assert(result.IsValid());
        }

        public override int ValueCount
        {
            get
            {
                int valueCount = @in.ValueCount;
                if (Debugging.AssertsEnabled) Debugging.Assert(valueCount == this.valueCount); // should not change
                return valueCount;
            }
        }

        public override void Get(int docID, BytesRef result)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(docID >= 0 && docID < maxDoc);
            if (Debugging.AssertsEnabled) Debugging.Assert(result.IsValid());
            @in.Get(docID, result);
            if (Debugging.AssertsEnabled) Debugging.Assert(result.IsValid());
        }

        public override int LookupTerm(BytesRef key)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(key.IsValid());
            int result = @in.LookupTerm(key);
            if (Debugging.AssertsEnabled) Debugging.Assert(result < valueCount);
            if (Debugging.AssertsEnabled) Debugging.Assert(key.IsValid());
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
            if (Debugging.AssertsEnabled) Debugging.Assert(valueCount >= 0);
        }

        public override long NextOrd()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(lastOrd != NO_MORE_ORDS);
            long ord = @in.NextOrd();
            if (Debugging.AssertsEnabled) Debugging.Assert(ord < valueCount);
            if (Debugging.AssertsEnabled) Debugging.Assert(ord == NO_MORE_ORDS || ord > lastOrd);
            lastOrd = ord;
            return ord;
        }

        public override void SetDocument(int docID)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(docID >= 0 && docID < maxDoc,"docid={0},maxDoc={1}", docID, maxDoc);
            @in.SetDocument(docID);
            lastOrd = -2;
        }

        public override void LookupOrd(long ord, BytesRef result)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(ord >= 0 && ord < valueCount);
            if (Debugging.AssertsEnabled) Debugging.Assert(result.IsValid());
            @in.LookupOrd(ord, result);
            if (Debugging.AssertsEnabled) Debugging.Assert(result.IsValid());
        }

        public override long ValueCount
        {
            get
            {
                long valueCount = @in.ValueCount;
                if (Debugging.AssertsEnabled) Debugging.Assert(valueCount == this.valueCount); // should not change
                return valueCount;
            }
        }

        public override long LookupTerm(BytesRef key)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(key.IsValid());
            long result = @in.LookupTerm(key);
            if (Debugging.AssertsEnabled) Debugging.Assert(result < valueCount);
            if (Debugging.AssertsEnabled) Debugging.Assert(key.IsValid());
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
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < Length);
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
            if (Debugging.AssertsEnabled) Debugging.Assert(@in.MaxDoc >= 0);
            if (Debugging.AssertsEnabled) Debugging.Assert(@in.NumDocs <= @in.MaxDoc);
            if (Debugging.AssertsEnabled) Debugging.Assert(@in.NumDeletedDocs + @in.NumDocs == @in.MaxDoc);
            if (Debugging.AssertsEnabled) Debugging.Assert(!@in.HasDeletions || @in.NumDeletedDocs > 0 && @in.NumDocs < @in.MaxDoc);
        }

        public override Fields Fields
        {
            get
            {
                Fields fields = base.Fields;
                return fields is null ? null : new AssertingFields(fields);
            }
        }

        public override Fields GetTermVectors(int docID)
        {
            Fields fields = base.GetTermVectors(docID);
            return fields is null ? null : new AssertingFields(fields);
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
                if (Debugging.AssertsEnabled) Debugging.Assert(state == State.POSITIONED, "Docs(...) called on unpositioned TermsEnum");

                // TODO: should we give this thing a random to be super-evil,
                // and randomly *not* unwrap?
                if (reuse is AssertingDocsEnum assertingDocsEnum)
                {
                    reuse = assertingDocsEnum.m_input;
                }
                DocsEnum docs = base.Docs(liveDocs, reuse, flags);
                return docs is null ? null : new AssertingDocsEnum(docs);
            }

            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(state == State.POSITIONED, "DocsAndPositions(...) called on unpositioned TermsEnum");

                // TODO: should we give this thing a random to be super-evil,
                // and randomly *not* unwrap?
                if (reuse is AssertingDocsAndPositionsEnum assertingDocsAndPositionsEnum)
                {
                    reuse = assertingDocsAndPositionsEnum.m_input;
                }
                DocsAndPositionsEnum docs = base.DocsAndPositions(liveDocs, reuse, flags);
                return docs is null ? null : new AssertingDocsAndPositionsEnum(docs);
            }

            public override bool MoveNext()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(state == State.INITIAL || state == State.POSITIONED, "MoveNext() called on unpositioned TermsEnum");
                if (!base.MoveNext())
                {
                    state = State.UNPOSITIONED;
                    return false;
                }
                else
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(base.Term.IsValid());
                    state = State.POSITIONED;
                    return base.Term != null;
                }
            }

            // TODO: we should separately track if we are 'at the end' ?
            // someone should not call next() after it returns null!!!!
            [Obsolete("Use MoveNext() and Term instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public override BytesRef Next()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(state == State.INITIAL || state == State.POSITIONED, "Next() called on unpositioned TermsEnum");
                if (MoveNext())
                    return base.Term;
                return null;
            }

            public override long Ord
            {
                get
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(state == State.POSITIONED, "Ord called on unpositioned TermsEnum");
                    return base.Ord;
                }
            }

            public override int DocFreq
            {
                get
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(state == State.POSITIONED, "DocFreq called on unpositioned TermsEnum");
                    return base.DocFreq;
                }
            }

            public override long TotalTermFreq
            {
                get
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(state == State.POSITIONED, "TotalTermFreq called on unpositioned TermsEnum");
                    return base.TotalTermFreq;
                }
            }

            public override BytesRef Term
            {
                get
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(state == State.POSITIONED, "Term called on unpositioned TermsEnum");
                    BytesRef ret = base.Term;
                    if (Debugging.AssertsEnabled) Debugging.Assert(ret is null || ret.IsValid());
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
                if (Debugging.AssertsEnabled) Debugging.Assert(term.IsValid());
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
                if (Debugging.AssertsEnabled) Debugging.Assert(text.IsValid());
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
                if (Debugging.AssertsEnabled) Debugging.Assert(state == State.POSITIONED, "GetTermState() called on unpositioned TermsEnum");
                return base.GetTermState();
            }

            public override void SeekExact(BytesRef term, TermState state)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(term.IsValid());
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
                if (Debugging.AssertsEnabled) Debugging.Assert(docid == -1,"invalid initial doc id: {0}", docid);
                doc = -1;
            }

            public override int NextDoc()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(state != DocsEnumState.FINISHED, "NextDoc() called after NO_MORE_DOCS");
                int nextDoc = base.NextDoc();
                if (Debugging.AssertsEnabled) Debugging.Assert(nextDoc > doc,"backwards nextDoc from {0} to {1}", doc, nextDoc);
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
                if (Debugging.AssertsEnabled) Debugging.Assert(base.DocID == nextDoc);
                return doc = nextDoc;
            }

            public override int Advance(int target)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(state != DocsEnumState.FINISHED, "Advance() called after NO_MORE_DOCS");
                if (Debugging.AssertsEnabled) Debugging.Assert(target > doc,"target must be > DocID, got {0} <= {1}", target, doc);
                int advanced = base.Advance(target);
                if (Debugging.AssertsEnabled) Debugging.Assert(advanced >= target,"backwards advance from: {0} to: {1}", target, advanced);
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
                if (Debugging.AssertsEnabled) Debugging.Assert(base.DocID == advanced);
                return doc = advanced;
            }

            public override int DocID
            {
                get
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(doc == base.DocID, " invalid DocID in {0} {1} instead of {2}", m_input.GetType(), base.DocID, doc);
                    return doc;
                }
            }

            public override int Freq
            {
                get
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(state != DocsEnumState.START, "Freq called before NextDoc()/Advance()");
                    if (Debugging.AssertsEnabled) Debugging.Assert(state != DocsEnumState.FINISHED, "Freq called after NO_MORE_DOCS");
                    int freq = base.Freq;
                    if (Debugging.AssertsEnabled) Debugging.Assert(freq > 0);
                    return freq;
                }
            }

            public override int NextPosition()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(state != DocsEnumState.START, "NextPosition() called before NextDoc()/Advance()");
                if (Debugging.AssertsEnabled) Debugging.Assert(state != DocsEnumState.FINISHED, "NextPosition() called after NO_MORE_DOCS");
                if (Debugging.AssertsEnabled) Debugging.Assert(positionCount < positionMax, "NextPosition() called more than Freq times!");
                int position = base.NextPosition();
                if (Debugging.AssertsEnabled) Debugging.Assert(position >= 0 || position == -1,"invalid position: {0}", position);
                positionCount++;
                return position;
            }

            public override int StartOffset
            {
                get
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(state != DocsEnumState.START, "StartOffset called before NextDoc()/Advance()");
                    if (Debugging.AssertsEnabled) Debugging.Assert(state != DocsEnumState.FINISHED, "StartOffset called after NO_MORE_DOCS");
                    if (Debugging.AssertsEnabled) Debugging.Assert(positionCount > 0, "StartOffset called before NextPosition()!");
                    return base.StartOffset;
                }
            }

            public override int EndOffset
            {
                get
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(state != DocsEnumState.START, "EndOffset called before NextDoc()/Advance()");
                    if (Debugging.AssertsEnabled) Debugging.Assert(state != DocsEnumState.FINISHED, "EndOffset called after NO_MORE_DOCS");
                    if (Debugging.AssertsEnabled) Debugging.Assert(positionCount > 0, "EndOffset called before NextPosition()!");
                    return base.EndOffset;
                }
            }

            public override BytesRef GetPayload()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(state != DocsEnumState.START, "GetPayload() called before NextDoc()/Advance()");
                if (Debugging.AssertsEnabled) Debugging.Assert(state != DocsEnumState.FINISHED, "GetPayload() called after NO_MORE_DOCS");
                if (Debugging.AssertsEnabled) Debugging.Assert(positionCount > 0, "GetPayload() called before NextPosition()!");
                BytesRef payload = base.GetPayload();
                if (Debugging.AssertsEnabled) Debugging.Assert(payload is null || payload.IsValid() && payload.Length > 0, "GetPayload() returned payload with invalid length!");
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
            FieldInfo fi = base.FieldInfos.FieldInfo(field);
            if (dv != null)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(fi != null);
                if (Debugging.AssertsEnabled) Debugging.Assert(fi.DocValuesType == DocValuesType.NUMERIC);
                return new AssertingNumericDocValues(dv, MaxDoc);
            }
            else
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(fi is null || fi.DocValuesType != DocValuesType.NUMERIC);
                return null;
            }
        }

        public override BinaryDocValues GetBinaryDocValues(string field)
        {
            BinaryDocValues dv = base.GetBinaryDocValues(field);
            FieldInfo fi = base.FieldInfos.FieldInfo(field);
            if (dv != null)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(fi != null);
                if (Debugging.AssertsEnabled) Debugging.Assert(fi.DocValuesType == DocValuesType.BINARY);
                return new AssertingBinaryDocValues(dv, MaxDoc);
            }
            else
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(fi is null || fi.DocValuesType != DocValuesType.BINARY);
                return null;
            }
        }

        public override SortedDocValues GetSortedDocValues(string field)
        {
            SortedDocValues dv = base.GetSortedDocValues(field);
            FieldInfo fi = base.FieldInfos.FieldInfo(field);
            if (dv != null)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(fi != null);
                if (Debugging.AssertsEnabled) Debugging.Assert(fi.DocValuesType == DocValuesType.SORTED);
                return new AssertingSortedDocValues(dv, MaxDoc);
            }
            else
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(fi is null || fi.DocValuesType != DocValuesType.SORTED);
                return null;
            }
        }

        public override SortedSetDocValues GetSortedSetDocValues(string field)
        {
            SortedSetDocValues dv = base.GetSortedSetDocValues(field);
            FieldInfo fi = base.FieldInfos.FieldInfo(field);
            if (dv != null)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(fi != null);
                if (Debugging.AssertsEnabled) Debugging.Assert(fi.DocValuesType == DocValuesType.SORTED_SET);
                return new AssertingSortedSetDocValues(dv, MaxDoc);
            }
            else
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(fi is null || fi.DocValuesType != DocValuesType.SORTED_SET);
                return null;
            }
        }

        public override NumericDocValues GetNormValues(string field)
        {
            NumericDocValues dv = base.GetNormValues(field);
            FieldInfo fi = base.FieldInfos.FieldInfo(field);
            if (dv != null)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(fi != null);
                if (Debugging.AssertsEnabled) Debugging.Assert(fi.HasNorms);
                return new AssertingNumericDocValues(dv, MaxDoc);
            }
            else
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(fi is null || fi.HasNorms == false);
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
                    if (Debugging.AssertsEnabled) Debugging.Assert(MaxDoc == liveDocs.Length);
                    liveDocs = new AssertingBits(liveDocs);
                }
                else
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(MaxDoc == NumDocs);
                    if (Debugging.AssertsEnabled) Debugging.Assert(!HasDeletions);
                }
                return liveDocs;
            }
        }

        public override IBits GetDocsWithField(string field)
        {
            IBits docsWithField = base.GetDocsWithField(field);
            FieldInfo fi = base.FieldInfos.FieldInfo(field);
            if (docsWithField != null)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(fi != null);
                if (Debugging.AssertsEnabled) Debugging.Assert(fi.HasDocValues);
                if (Debugging.AssertsEnabled) Debugging.Assert(MaxDoc == docsWithField.Length);
                docsWithField = new AssertingBits(docsWithField);
            }
            else
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(fi is null || fi.HasDocValues == false);
            }
            return docsWithField;
        }

        // this is the same hack as FCInvisible
        public override object CoreCacheKey => cacheKey;

        public override object CombinedCoreAndDeletesKey => cacheKey;


        private readonly object cacheKey = new object();
    }
}