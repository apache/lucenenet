using System.Diagnostics;
using System.Collections.Generic;

namespace Lucene.Net.Index
{


	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using CompiledAutomaton = Lucene.Net.Util.Automaton.CompiledAutomaton;

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
	/// A <seealso cref="FilterAtomicReader"/> that can be used to apply
	/// additional checks for tests.
	/// </summary>
	public class AssertingAtomicReader : FilterAtomicReader
	{

	  public AssertingAtomicReader(AtomicReader @in) : base(@in)
	  {
		// check some basic reader sanity
		Debug.Assert(@in.maxDoc() >= 0);
		Debug.Assert(@in.numDocs() <= @in.maxDoc());
		Debug.Assert(@in.numDeletedDocs() + @in.numDocs() == @in.maxDoc());
		Debug.Assert(!@in.hasDeletions() || @in.numDeletedDocs() > 0 && @in.numDocs() < @in.maxDoc());
	  }

	  public override Fields Fields()
	  {
		Fields fields = base.fields();
		return fields == null ? null : new AssertingFields(fields);
	  }

	  public override Fields GetTermVectors(int docID)
	  {
		Fields fields = base.getTermVectors(docID);
		return fields == null ? null : new AssertingFields(fields);
	  }

	  /// <summary>
	  /// Wraps a Fields but with additional asserts
	  /// </summary>
	  public class AssertingFields : FilterFields
	  {
		public AssertingFields(Fields @in) : base(@in)
		{
		}

		public override IEnumerator<string> Iterator()
		{
		  IEnumerator<string> iterator = base.GetEnumerator();
		  Debug.Assert(iterator != null);
		  return iterator;
		}

		public override Terms Terms(string field)
		{
		  Terms terms = base.terms(field);
		  return terms == null ? null : new AssertingTerms(terms);
		}
	  }

	  /// <summary>
	  /// Wraps a Terms but with additional asserts
	  /// </summary>
	  public class AssertingTerms : FilterTerms
	  {
		public AssertingTerms(Terms @in) : base(@in)
		{
		}

		public override TermsEnum Intersect(CompiledAutomaton automaton, BytesRef bytes)
		{
		  TermsEnum termsEnum = @in.intersect(automaton, bytes);
		  Debug.Assert(termsEnum != null);
		  Debug.Assert(bytes == null || bytes.Valid);
		  return new AssertingTermsEnum(termsEnum);
		}

		public override TermsEnum Iterator(TermsEnum reuse)
		{
		  // TODO: should we give this thing a random to be super-evil,
		  // and randomly *not* unwrap?
		  if (reuse is AssertingTermsEnum)
		  {
			reuse = ((AssertingTermsEnum) reuse).@in;
		  }
		  TermsEnum termsEnum = base.iterator(reuse);
		  Debug.Assert(termsEnum != null);
		  return new AssertingTermsEnum(termsEnum);
		}
	  }

	  internal class AssertingTermsEnum : FilterTermsEnum
	  {
		private enum State
		{
			INITIAL,
			POSITIONED,
			UNPOSITIONED
		}
		internal State State = State.INITIAL;

		public AssertingTermsEnum(TermsEnum @in) : base(@in)
		{
		}

		public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
		{
		  Debug.Assert(State == State.POSITIONED, "docs(...) called on unpositioned TermsEnum");

		  // TODO: should we give this thing a random to be super-evil,
		  // and randomly *not* unwrap?
		  if (reuse is AssertingDocsEnum)
		  {
			reuse = ((AssertingDocsEnum) reuse).@in;
		  }
		  DocsEnum docs = base.docs(liveDocs, reuse, flags);
		  return docs == null ? null : new AssertingDocsEnum(docs);
		}

		public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
		{
		  Debug.Assert(State == State.POSITIONED, "docsAndPositions(...) called on unpositioned TermsEnum");

		  // TODO: should we give this thing a random to be super-evil,
		  // and randomly *not* unwrap?
		  if (reuse is AssertingDocsAndPositionsEnum)
		  {
			reuse = ((AssertingDocsAndPositionsEnum) reuse).@in;
		  }
		  DocsAndPositionsEnum docs = base.docsAndPositions(liveDocs, reuse, flags);
		  return docs == null ? null : new AssertingDocsAndPositionsEnum(docs);
		}

		// TODO: we should separately track if we are 'at the end' ?
		// someone should not call next() after it returns null!!!!
		public override BytesRef Next()
		{
		  Debug.Assert(State == State.INITIAL || State == State.POSITIONED, "next() called on unpositioned TermsEnum");
		  BytesRef result = base.next();
		  if (result == null)
		  {
			State = State.UNPOSITIONED;
		  }
		  else
		  {
			Debug.Assert(result.Valid);
			State = State.POSITIONED;
		  }
		  return result;
		}

		public override long Ord()
		{
		  Debug.Assert(State == State.POSITIONED, "ord() called on unpositioned TermsEnum");
		  return base.ord();
		}

		public override int DocFreq()
		{
		  Debug.Assert(State == State.POSITIONED, "docFreq() called on unpositioned TermsEnum");
		  return base.docFreq();
		}

		public override long TotalTermFreq()
		{
		  Debug.Assert(State == State.POSITIONED, "totalTermFreq() called on unpositioned TermsEnum");
		  return base.totalTermFreq();
		}

		public override BytesRef Term()
		{
		  Debug.Assert(State == State.POSITIONED, "term() called on unpositioned TermsEnum");
		  BytesRef ret = base.term();
		  Debug.Assert(ret == null || ret.Valid);
		  return ret;
		}

		public override void SeekExact(long ord)
		{
		  base.seekExact(ord);
		  State = State.POSITIONED;
		}

		public override SeekStatus SeekCeil(BytesRef term)
		{
		  Debug.Assert(term.Valid);
		  SeekStatus result = base.seekCeil(term);
		  if (result == SeekStatus.END)
		  {
			State = State.UNPOSITIONED;
		  }
		  else
		  {
			State = State.POSITIONED;
		  }
		  return result;
		}

		public override bool SeekExact(BytesRef text)
		{
		  Debug.Assert(text.Valid);
		  if (base.seekExact(text))
		  {
			State = State.POSITIONED;
			return true;
		  }
		  else
		  {
			State = State.UNPOSITIONED;
			return false;
		  }
		}

		public override TermState TermState()
		{
		  Debug.Assert(State == State.POSITIONED, "termState() called on unpositioned TermsEnum");
		  return base.termState();
		}

		public override void SeekExact(BytesRef term, TermState state)
		{
		  Debug.Assert(term.Valid);
		  base.seekExact(term, state);
		  this.State = State.POSITIONED;
		}
	  }

	  internal enum DocsEnumState
	  {
		  START,
		  ITERATING,
		  FINISHED
	  }

	  /// <summary>
	  /// Wraps a docsenum with additional checks </summary>
	  public class AssertingDocsEnum : FilterDocsEnum
	  {
		internal DocsEnumState State = DocsEnumState.START;
		internal int Doc;

		public AssertingDocsEnum(DocsEnum @in) : this(@in, true)
		{
		}

		public AssertingDocsEnum(DocsEnum @in, bool failOnUnsupportedDocID) : base(@in)
		{
		  try
		  {
			int docid = @in.docID();
			Debug.Assert(docid == -1, @in.GetType() + ": invalid initial doc id: " + docid);
		  }
		  catch (System.NotSupportedException e)
		  {
			if (failOnUnsupportedDocID)
			{
			  throw e;
			}
		  }
		  Doc = -1;
		}

		public override int NextDoc()
		{
		  Debug.Assert(State != DocsEnumState.FINISHED, "nextDoc() called after NO_MORE_DOCS");
		  int nextDoc = base.nextDoc();
		  Debug.Assert(nextDoc > Doc, "backwards nextDoc from " + Doc + " to " + nextDoc + " " + @in);
		  if (nextDoc == DocIdSetIterator.NO_MORE_DOCS)
		  {
			State = DocsEnumState.FINISHED;
		  }
		  else
		  {
			State = DocsEnumState.ITERATING;
		  }
		  Debug.Assert(base.docID() == nextDoc);
		  return Doc = nextDoc;
		}

		public override int Advance(int target)
		{
		  Debug.Assert(State != DocsEnumState.FINISHED, "advance() called after NO_MORE_DOCS");
		  Debug.Assert(target > Doc, "target must be > docID(), got " + target + " <= " + Doc);
		  int advanced = base.advance(target);
		  Debug.Assert(advanced >= target, "backwards advance from: " + target + " to: " + advanced);
		  if (advanced == DocIdSetIterator.NO_MORE_DOCS)
		  {
			State = DocsEnumState.FINISHED;
		  }
		  else
		  {
			State = DocsEnumState.ITERATING;
		  }
		  Debug.Assert(base.docID() == advanced);
		  return Doc = advanced;
		}

		public override int DocID()
		{
		  Debug.Assert(Doc == base.docID(), " invalid docID() in " + @in.GetType() + " " + base.docID() + " instead of " + Doc);
		  return Doc;
		}

		public override int Freq()
		{
		  Debug.Assert(State != DocsEnumState.START, "freq() called before nextDoc()/advance()");
		  Debug.Assert(State != DocsEnumState.FINISHED, "freq() called after NO_MORE_DOCS");
		  int freq = base.freq();
		  Debug.Assert(freq > 0);
		  return freq;
		}
	  }

	  internal class AssertingDocsAndPositionsEnum : FilterDocsAndPositionsEnum
	  {
		internal DocsEnumState State = DocsEnumState.START;
		internal int PositionMax = 0;
		internal int PositionCount = 0;
		internal int Doc;

		public AssertingDocsAndPositionsEnum(DocsAndPositionsEnum @in) : base(@in)
		{
		  int docid = @in.docID();
		  Debug.Assert(docid == -1, "invalid initial doc id: " + docid);
		  Doc = -1;
		}

		public override int NextDoc()
		{
		  Debug.Assert(State != DocsEnumState.FINISHED, "nextDoc() called after NO_MORE_DOCS");
		  int nextDoc = base.nextDoc();
		  Debug.Assert(nextDoc > Doc, "backwards nextDoc from " + Doc + " to " + nextDoc);
		  PositionCount = 0;
		  if (nextDoc == DocIdSetIterator.NO_MORE_DOCS)
		  {
			State = DocsEnumState.FINISHED;
			PositionMax = 0;
		  }
		  else
		  {
			State = DocsEnumState.ITERATING;
			PositionMax = base.freq();
		  }
		  Debug.Assert(base.docID() == nextDoc);
		  return Doc = nextDoc;
		}

		public override int Advance(int target)
		{
		  Debug.Assert(State != DocsEnumState.FINISHED, "advance() called after NO_MORE_DOCS");
		  Debug.Assert(target > Doc, "target must be > docID(), got " + target + " <= " + Doc);
		  int advanced = base.advance(target);
		  Debug.Assert(advanced >= target, "backwards advance from: " + target + " to: " + advanced);
		  PositionCount = 0;
		  if (advanced == DocIdSetIterator.NO_MORE_DOCS)
		  {
			State = DocsEnumState.FINISHED;
			PositionMax = 0;
		  }
		  else
		  {
			State = DocsEnumState.ITERATING;
			PositionMax = base.freq();
		  }
		  Debug.Assert(base.docID() == advanced);
		  return Doc = advanced;
		}

		public override int DocID()
		{
		  Debug.Assert(Doc == base.docID(), " invalid docID() in " + @in.GetType() + " " + base.docID() + " instead of " + Doc);
		  return Doc;
		}

		public override int Freq()
		{
		  Debug.Assert(State != DocsEnumState.START, "freq() called before nextDoc()/advance()");
		  Debug.Assert(State != DocsEnumState.FINISHED, "freq() called after NO_MORE_DOCS");
		  int freq = base.freq();
		  Debug.Assert(freq > 0);
		  return freq;
		}

		public override int NextPosition()
		{
		  Debug.Assert(State != DocsEnumState.START, "nextPosition() called before nextDoc()/advance()");
		  Debug.Assert(State != DocsEnumState.FINISHED, "nextPosition() called after NO_MORE_DOCS");
		  Debug.Assert(PositionCount < PositionMax, "nextPosition() called more than freq() times!");
		  int position = base.nextPosition();
		  Debug.Assert(position >= 0 || position == -1, "invalid position: " + position);
		  PositionCount++;
		  return position;
		}

		public override int StartOffset()
		{
		  Debug.Assert(State != DocsEnumState.START, "StartOffset() called before nextDoc()/advance()");
		  Debug.Assert(State != DocsEnumState.FINISHED, "StartOffset() called after NO_MORE_DOCS");
		  Debug.Assert(PositionCount > 0, "StartOffset() called before nextPosition()!");
		  return base.StartOffset();
		}

		public override int EndOffset()
		{
		  Debug.Assert(State != DocsEnumState.START, "EndOffset() called before nextDoc()/advance()");
		  Debug.Assert(State != DocsEnumState.FINISHED, "EndOffset() called after NO_MORE_DOCS");
		  Debug.Assert(PositionCount > 0, "EndOffset() called before nextPosition()!");
		  return base.EndOffset();
		}

		public override BytesRef Payload
		{
			get
			{
			  Debug.Assert(State != DocsEnumState.START, "getPayload() called before nextDoc()/advance()");
			  Debug.Assert(State != DocsEnumState.FINISHED, "getPayload() called after NO_MORE_DOCS");
			  Debug.Assert(PositionCount > 0, "getPayload() called before nextPosition()!");
			  BytesRef payload = base.Payload;
			  Debug.Assert(payload == null || payload.Valid && payload.length > 0, "getPayload() returned payload with invalid length!");
			  return payload;
			}
		}
	  }

	  /// <summary>
	  /// Wraps a NumericDocValues but with additional asserts </summary>
	  public class AssertingNumericDocValues : NumericDocValues
	  {
		internal readonly NumericDocValues @in;
		internal readonly int MaxDoc;

		public AssertingNumericDocValues(NumericDocValues @in, int maxDoc)
		{
		  this.@in = @in;
		  this.MaxDoc = maxDoc;
		}

		public override long Get(int docID)
		{
		  Debug.Assert(docID >= 0 && docID < MaxDoc);
		  return @in.get(docID);
		}
	  }

	  /// <summary>
	  /// Wraps a BinaryDocValues but with additional asserts </summary>
	  public class AssertingBinaryDocValues : BinaryDocValues
	  {
		internal readonly BinaryDocValues @in;
		internal readonly int MaxDoc;

		public AssertingBinaryDocValues(BinaryDocValues @in, int maxDoc)
		{
		  this.@in = @in;
		  this.MaxDoc = maxDoc;
		}

		public override void Get(int docID, BytesRef result)
		{
		  Debug.Assert(docID >= 0 && docID < MaxDoc);
		  Debug.Assert(result.Valid);
		  @in.get(docID, result);
		  Debug.Assert(result.Valid);
		}
	  }

	  /// <summary>
	  /// Wraps a SortedDocValues but with additional asserts </summary>
	  public class AssertingSortedDocValues : SortedDocValues
	  {
		internal readonly SortedDocValues @in;
		internal readonly int MaxDoc;
		internal readonly int ValueCount_Renamed;

		public AssertingSortedDocValues(SortedDocValues @in, int maxDoc)
		{
		  this.@in = @in;
		  this.MaxDoc = maxDoc;
		  this.ValueCount_Renamed = @in.ValueCount;
		  Debug.Assert(ValueCount_Renamed >= 0 && ValueCount_Renamed <= maxDoc);
		}

		public override int GetOrd(int docID)
		{
		  Debug.Assert(docID >= 0 && docID < MaxDoc);
		  int ord = @in.getOrd(docID);
		  Debug.Assert(ord >= -1 && ord < ValueCount_Renamed);
		  return ord;
		}

		public override void LookupOrd(int ord, BytesRef result)
		{
		  Debug.Assert(ord >= 0 && ord < ValueCount_Renamed);
		  Debug.Assert(result.Valid);
		  @in.lookupOrd(ord, result);
		  Debug.Assert(result.Valid);
		}

		public override int ValueCount
		{
			get
			{
			  int valueCount = @in.ValueCount;
			  Debug.Assert(valueCount == this.ValueCount_Renamed); // should not change
			  return valueCount;
			}
		}

		public override void Get(int docID, BytesRef result)
		{
		  Debug.Assert(docID >= 0 && docID < MaxDoc);
		  Debug.Assert(result.Valid);
		  @in.get(docID, result);
		  Debug.Assert(result.Valid);
		}

		public override int LookupTerm(BytesRef key)
		{
		  Debug.Assert(key.Valid);
		  int result = @in.lookupTerm(key);
		  Debug.Assert(result < ValueCount_Renamed);
		  Debug.Assert(key.Valid);
		  return result;
		}
	  }

	  /// <summary>
	  /// Wraps a SortedSetDocValues but with additional asserts </summary>
	  public class AssertingSortedSetDocValues : SortedSetDocValues
	  {
		internal readonly SortedSetDocValues @in;
		internal readonly int MaxDoc;
		internal readonly long ValueCount_Renamed;
		internal long LastOrd = NO_MORE_ORDS;

		public AssertingSortedSetDocValues(SortedSetDocValues @in, int maxDoc)
		{
		  this.@in = @in;
		  this.MaxDoc = maxDoc;
		  this.ValueCount_Renamed = @in.ValueCount;
		  Debug.Assert(ValueCount_Renamed >= 0);
		}

		public override long NextOrd()
		{
		  Debug.Assert(LastOrd != NO_MORE_ORDS);
		  long ord = @in.nextOrd();
		  Debug.Assert(ord < ValueCount_Renamed);
		  Debug.Assert(ord == NO_MORE_ORDS || ord > LastOrd);
		  LastOrd = ord;
		  return ord;
		}

		public override int Document
		{
			set
			{
			  Debug.Assert(value >= 0 && value < MaxDoc, "docid=" + value + ",maxDoc=" + MaxDoc);
			  @in.Document = value;
			  LastOrd = -2;
			}
		}

		public override void LookupOrd(long ord, BytesRef result)
		{
		  Debug.Assert(ord >= 0 && ord < ValueCount_Renamed);
		  Debug.Assert(result.Valid);
		  @in.lookupOrd(ord, result);
		  Debug.Assert(result.Valid);
		}

		public override long ValueCount
		{
			get
			{
			  long valueCount = @in.ValueCount;
			  Debug.Assert(valueCount == this.ValueCount_Renamed); // should not change
			  return valueCount;
			}
		}

		public override long LookupTerm(BytesRef key)
		{
		  Debug.Assert(key.Valid);
		  long result = @in.lookupTerm(key);
		  Debug.Assert(result < ValueCount_Renamed);
		  Debug.Assert(key.Valid);
		  return result;
		}
	  }

	  public override NumericDocValues GetNumericDocValues(string field)
	  {
		NumericDocValues dv = base.getNumericDocValues(field);
		FieldInfo fi = FieldInfos.fieldInfo(field);
		if (dv != null)
		{
		  Debug.Assert(fi != null);
		  Debug.Assert(fi.DocValuesType_e == FieldInfo.DocValuesType_e.NUMERIC);
		  return new AssertingNumericDocValues(dv, maxDoc());
		}
		else
		{
		  Debug.Assert(fi == null || fi.DocValuesType_e != FieldInfo.DocValuesType_e.NUMERIC);
		  return null;
		}
	  }

	  public override BinaryDocValues GetBinaryDocValues(string field)
	  {
		BinaryDocValues dv = base.getBinaryDocValues(field);
		FieldInfo fi = FieldInfos.fieldInfo(field);
		if (dv != null)
		{
		  Debug.Assert(fi != null);
		  Debug.Assert(fi.DocValuesType_e == FieldInfo.DocValuesType_e.BINARY);
		  return new AssertingBinaryDocValues(dv, maxDoc());
		}
		else
		{
		  Debug.Assert(fi == null || fi.DocValuesType_e != FieldInfo.DocValuesType_e.BINARY);
		  return null;
		}
	  }

	  public override SortedDocValues GetSortedDocValues(string field)
	  {
		SortedDocValues dv = base.getSortedDocValues(field);
		FieldInfo fi = FieldInfos.fieldInfo(field);
		if (dv != null)
		{
		  Debug.Assert(fi != null);
		  Debug.Assert(fi.DocValuesType_e == FieldInfo.DocValuesType_e.SORTED);
		  return new AssertingSortedDocValues(dv, maxDoc());
		}
		else
		{
		  Debug.Assert(fi == null || fi.DocValuesType_e != FieldInfo.DocValuesType_e.SORTED);
		  return null;
		}
	  }

	  public override SortedSetDocValues GetSortedSetDocValues(string field)
	  {
		SortedSetDocValues dv = base.getSortedSetDocValues(field);
		FieldInfo fi = FieldInfos.fieldInfo(field);
		if (dv != null)
		{
		  Debug.Assert(fi != null);
		  Debug.Assert(fi.DocValuesType_e == FieldInfo.DocValuesType_e.SORTED_SET);
		  return new AssertingSortedSetDocValues(dv, maxDoc());
		}
		else
		{
		  Debug.Assert(fi == null || fi.DocValuesType_e != FieldInfo.DocValuesType_e.SORTED_SET);
		  return null;
		}
	  }

	  public override NumericDocValues GetNormValues(string field)
	  {
		NumericDocValues dv = base.getNormValues(field);
		FieldInfo fi = FieldInfos.fieldInfo(field);
		if (dv != null)
		{
		  Debug.Assert(fi != null);
		  Debug.Assert(fi.hasNorms());
		  return new AssertingNumericDocValues(dv, maxDoc());
		}
		else
		{
		  Debug.Assert(fi == null || fi.hasNorms() == false);
		  return null;
		}
	  }

	  /// <summary>
	  /// Wraps a Bits but with additional asserts </summary>
	  public class AssertingBits : Bits
	  {
		internal readonly Bits @in;

		public AssertingBits(Bits @in)
		{
		  this.@in = @in;
		}

		public override bool Get(int index)
		{
		  Debug.Assert(index >= 0 && index < Length());
		  return @in.get(index);
		}

		public override int Length()
		{
		  return @in.length();
		}
	  }

	  public override Bits LiveDocs
	  {
		  get
		  {
			Bits liveDocs = base.LiveDocs;
			if (liveDocs != null)
			{
			  Debug.Assert(maxDoc() == liveDocs.length());
			  liveDocs = new AssertingBits(liveDocs);
			}
			else
			{
			  Debug.Assert(maxDoc() == numDocs());
			  Debug.Assert(!hasDeletions());
			}
			return liveDocs;
		  }
	  }

	  public override Bits GetDocsWithField(string field)
	  {
		Bits docsWithField = base.getDocsWithField(field);
		FieldInfo fi = FieldInfos.fieldInfo(field);
		if (docsWithField != null)
		{
		  Debug.Assert(fi != null);
		  Debug.Assert(fi.hasDocValues());
		  Debug.Assert(maxDoc() == docsWithField.length());
		  docsWithField = new AssertingBits(docsWithField);
		}
		else
		{
		  Debug.Assert(fi == null || fi.hasDocValues() == false);
		}
		return docsWithField;
	  }

	  // this is the same hack as FCInvisible
	  public override object CoreCacheKey
	  {
		  get
		  {
			return CacheKey;
		  }
	  }

	  public override object CombinedCoreAndDeletesKey
	  {
		  get
		  {
			return CacheKey;
		  }
	  }

	  private readonly object CacheKey = new object();
	}
}