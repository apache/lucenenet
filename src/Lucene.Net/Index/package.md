---
uid: Lucene.Net.Index
summary: *content
---


<!--
 Licensed to the Apache Software Foundation (ASF) under one or more
 contributor license agreements.  See the NOTICE file distributed with
 this work for additional information regarding copyright ownership.
 The ASF licenses this file to You under the Apache License, Version 2.0
 (the "License"); you may not use this file except in compliance with
 the License.  You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.
-->

Code to maintain and access indices.

## Table Of Contents

 1. [Postings APIs](#postings) * [Fields](#fields) * [Terms](#terms) * [Documents](#documents) * [Positions](#positions) 2. [Index Statistics](#stats) * [Term-level](#termstats) * [Field-level](#fieldstats) * [Segment-level](#segmentstats) * [Document-level](#documentstats) 

## Postings APIs

#### 
    Fields

 <xref:Lucene.Net.Index.Fields> is the initial entry point into the postings APIs, this can be obtained in several ways: // access indexed fields for an index segment Fields fields = reader.fields(); // access term vector fields for a specified document Fields fields = reader.getTermVectors(docid); Fields implements Java's Iterable interface, so its easy to enumerate the list of fields: // enumerate list of fields for (String field : fields) { // access the terms for this field Terms terms = fields.terms(field); } 

#### 
    Terms

 <xref:Lucene.Net.Index.Terms> represents the collection of terms within a field, exposes some metadata and [statistics](#fieldstats), and an API for enumeration. // metadata about the field System.out.println("positions? " + terms.hasPositions()); System.out.println("offsets? " + terms.hasOffsets()); System.out.println("payloads? " + terms.hasPayloads()); // iterate through terms TermsEnum termsEnum = terms.iterator(null); BytesRef term = null; while ((term = termsEnum.next()) != null) { doSomethingWith(termsEnum.term()); } <xref:Lucene.Net.Index.TermsEnum> provides an iterator over the list of terms within a field, some [statistics](#termstats) about the term, and methods to access the term's [documents](#documents) and [positions](#positions). // seek to a specific term boolean found = termsEnum.seekExact(new BytesRef("foobar")); if (found) { // get the document frequency System.out.println(termsEnum.docFreq()); // enumerate through documents DocsEnum docs = termsEnum.docs(null, null); // enumerate through documents and positions DocsAndPositionsEnum docsAndPositions = termsEnum.docsAndPositions(null, null); } 

#### 
    Documents

 <xref:Lucene.Net.Index.DocsEnum> is an extension of <xref:Lucene.Net.Search.DocIdSetIterator>that iterates over the list of documents for a term, along with the term frequency within that document. int docid; while ((docid = docsEnum.nextDoc()) != DocIdSetIterator.NO_MORE_DOCS) { System.out.println(docid); System.out.println(docsEnum.freq()); } 

#### 
    Positions

 <xref:Lucene.Net.Index.DocsAndPositionsEnum> is an extension of <xref:Lucene.Net.Index.DocsEnum> that additionally allows iteration of the positions a term occurred within the document, and any additional per-position information (offsets and payload) int docid; while ((docid = docsAndPositionsEnum.nextDoc()) != DocIdSetIterator.NO_MORE_DOCS) { System.out.println(docid); int freq = docsAndPositionsEnum.freq(); for (int i = 0; i < freq;="" i++)="" {="" system.out.println(docsandpositionsenum.nextposition());="" system.out.println(docsandpositionsenum.startoffset());="" system.out.println(docsandpositionsenum.endoffset());="" system.out.println(docsandpositionsenum.getpayload());="" }="" }=""> 

## Index Statistics

#### 
    Term statistics

 * [#docFreq](xref:Lucene.Net.Index.TermsEnum): Returns the number of documents that contain at least one occurrence of the term. This statistic is always available for an indexed term. Note that it will also count deleted documents, when segments are merged the statistic is updated as those deleted documents are merged away. [#totalTermFreq](xref:Lucene.Net.Index.TermsEnum): Returns the number of occurrences of this term across all documents. Note that this statistic is unavailable (returns `-1`) if term frequencies were omitted from the index ([DOCS_ONLY](xref:Lucene.Net.Index.FieldInfo.IndexOptions#methods)) for the field. Like docFreq(), it will also count occurrences that appear in deleted documents. 

#### 
    Field statistics

 * [#size](xref:Lucene.Net.Index.Terms): Returns the number of unique terms in the field. This statistic may be unavailable (returns `-1`) for some Terms implementations such as <xref:Lucene.Net.Index.MultiTerms>, where it cannot be efficiently computed. Note that this count also includes terms that appear only in deleted documents: when segments are merged such terms are also merged away and the statistic is then updated. [#getDocCount](xref:Lucene.Net.Index.Terms): Returns the number of documents that contain at least one occurrence of any term for this field. This can be thought of as a Field-level docFreq(). Like docFreq() it will also count deleted documents. [#getSumDocFreq](xref:Lucene.Net.Index.Terms): Returns the number of postings (term-document mappings in the inverted index) for the field. This can be thought of as the sum of [#docFreq](xref:Lucene.Net.Index.TermsEnum) across all terms in the field, and like docFreq() it will also count postings that appear in deleted documents. [#getSumTotalTermFreq](xref:Lucene.Net.Index.Terms): Returns the number of tokens for the field. This can be thought of as the sum of [#totalTermFreq](xref:Lucene.Net.Index.TermsEnum) across all terms in the field, and like totalTermFreq() it will also count occurrences that appear in deleted documents, and will be unavailable (returns `-1`) if term frequencies were omitted from the index ([DOCS_ONLY](xref:Lucene.Net.Index.FieldInfo.IndexOptions#methods)) for the field. 

#### 
    Segment statistics

 * [#maxDoc](xref:Lucene.Net.Index.IndexReader): Returns the number of documents (including deleted documents) in the index. [#numDocs](xref:Lucene.Net.Index.IndexReader): Returns the number of live documents (excluding deleted documents) in the index. [#numDeletedDocs](xref:Lucene.Net.Index.IndexReader): Returns the number of deleted documents in the index. [#size](xref:Lucene.Net.Index.Fields): Returns the number of indexed fields. [#getUniqueTermCount](xref:Lucene.Net.Index.Fields): Returns the number of indexed terms, the sum of [#size](xref:Lucene.Net.Index.Terms) across all fields. 

#### 
    Document statistics

 Document statistics are available during the indexing process for an indexed field: typically a <xref:Lucene.Net.Search.Similarities.Similarity> implementation will store some of these values (possibly in a lossy way), into the normalization value for the document in its [#computeNorm](xref:Lucene.Net.Search.Similarities.Similarity) method. 

 * [#getLength](xref:Lucene.Net.Index.FieldInvertState): Returns the number of tokens for this field in the document. Note that this is just the number of times that [#incrementToken](xref:Lucene.Net.Analysis.TokenStream) returned true, and is unrelated to the values in <xref:Lucene.Net.Analysis.TokenAttributes.PositionIncrementAttribute>. [#getNumOverlap](xref:Lucene.Net.Index.FieldInvertState): Returns the number of tokens for this field in the document that had a position increment of zero. This can be used to compute a document length that discounts artificial tokens such as synonyms. [#getPosition](xref:Lucene.Net.Index.FieldInvertState): Returns the accumulated position value for this field in the document: computed from the values of <xref:Lucene.Net.Analysis.TokenAttributes.PositionIncrementAttribute> and including [#getPositionIncrementGap](xref:Lucene.Net.Analysis.Analyzer)s across multivalued fields. [#getOffset](xref:Lucene.Net.Index.FieldInvertState): Returns the total character offset value for this field in the document: computed from the values of <xref:Lucene.Net.Analysis.TokenAttributes.OffsetAttribute> returned by [#end](xref:Lucene.Net.Analysis.TokenStream), and including [#getOffsetGap](xref:Lucene.Net.Analysis.Analyzer)s across multivalued fields. [#getUniqueTermCount](xref:Lucene.Net.Index.FieldInvertState): Returns the number of unique terms encountered for this field in the document. [#getMaxTermFrequency](xref:Lucene.Net.Index.FieldInvertState): Returns the maximum frequency across all unique terms encountered for this field in the document. 

 Additional user-supplied statistics can be added to the document as DocValues fields and accessed via [#getNumericDocValues](xref:Lucene.Net.Index.AtomicReader). 