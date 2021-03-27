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

 1. [Postings APIs](#postings)
    * [Fields](#fields)
    * [Terms](#terms)
    * [Documents](#documents)
    * [Positions](#positions)
 2. [Index Statistics](#index-statistics)
    * [Term-level](#term-statistics)
    * [Field-level](#field-statistics)
    * [Segment-level](#segment-statistics)
    * [Document-level](#document-statistics) 

## Postings APIs

#### Fields

 <xref:Lucene.Net.Index.Fields> is the initial entry point into the postings APIs, this can be obtained in several ways:

```cs
// access indexed fields for an index segment
Fields fields = reader.Fields; // access term vector fields for a specified document
Fields fields = reader.GetTermVectors(docid);
```

Fields implements .NET's `IEnumerable<T>` interface, so its easy to enumerate the list of fields:

```cs
// enumerate list of fields
foreach (string field in fields) // access the terms for this field
{
    Terms terms = fields.GetTerms(field);
} 
```

#### Terms

 <xref:Lucene.Net.Index.Terms> represents the collection of terms within a field, exposes some metadata and [statistics](#field-statistics), and an API for enumeration.

```cs
// metadata about the field
Console.WriteLine("positions? " + terms.HasPositions);
Console.WriteLine("offsets? " + terms.HasOffsets);
Console.WriteLine("payloads? " + terms.HasPayloads);
// iterate through terms
TermsEnum termsEnum = terms.GetEnumerator();
while (termsEnum.MoveNext())
{
    DoSomethingWith(termsEnum.Term); // Term is a BytesRef
}
```

<xref:Lucene.Net.Index.TermsEnum> provides an enumerator over the list of terms within a field, some [statistics](#term-statistics) about the term, and methods to access the term's [documents](#documents) and [positions](#positions).

```cs
// seek to a specific term
bool found = termsEnum.SeekExact(new BytesRef("foobar"));
if (found)
{
    // get the document frequency
    Console.WriteLine(termsEnum.DocFreq);
    // enumerate through documents
    DocsEnum docs = termsEnum.Docs(null, null);
    // enumerate through documents and positions
    DocsAndPositionsEnum docsAndPositions = termsEnum.DocsAndPositions(null, null);
}
```

#### Documents

 <xref:Lucene.Net.Index.DocsEnum> is an extension of <xref:Lucene.Net.Search.DocIdSetIterator> that iterates over the list of documents for a term, along with the term frequency within that document.

```cs
int docid;
while ((docid = docsEnum.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
{
    Console.WriteLine(docid);
    Console.WriteLine(docsEnum.Freq);
}
```

#### Positions

 <xref:Lucene.Net.Index.DocsAndPositionsEnum> is an extension of <xref:Lucene.Net.Index.DocsEnum> that additionally allows iteration of the positions a term occurred within the document, and any additional per-position information (offsets and payload)

```cs
int docid;
while ((docid = docsAndPositionsEnum.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
{
    Console.WriteLine(docid);
    int freq = docsAndPositionsEnum.Freq;
    for (int i = 0; i < freq; i++)
    {
        Console.WriteLine(docsAndPositionsEnum.NextPosition());
        Console.WriteLine(docsAndPositionsEnum.StartOffset);
        Console.WriteLine(docsAndPositionsEnum.EndOffset);
        Console.WriteLine(docsAndPositionsEnum.GetPayload());
    }
}
``` 

## Index Statistics

#### Term statistics

 * [DocFreq](xref:Lucene.Net.Index.TermsEnum#Lucene_Net_Index_TermsEnum_DocFreq): Returns the number of documents that contain at least one occurrence of the term. This statistic is always available for an indexed term. Note that it will also count deleted documents, when segments are merged the statistic is updated as those deleted documents are merged away.
 * [TotalTermFreq](xref:Lucene.Net.Index.TermsEnum#Lucene_Net_Index_TermsEnum_TotalTermFreq): Returns the number of occurrences of this term across all documents. Note that this statistic is unavailable (returns `-1`) if term frequencies were omitted from the index ([DOCS_ONLY](xref:Lucene.Net.Index.FieldInfo.IndexOptions#Lucene_Net_Index_IndexOptions_DOCS_ONLY)) for the field. Like `DocFreq`, it will also count occurrences that appear in deleted documents. 

#### Field statistics

 * [Count](xref:Lucene.Net.Index.Terms#Lucene_Net_Index_Terms_Count): Returns the number of unique terms in the field. This statistic may be unavailable (returns `-1`) for some Terms implementations such as <xref:Lucene.Net.Index.MultiTerms>, where it cannot be efficiently computed. Note that this count also includes terms that appear only in deleted documents: when segments are merged such terms are also merged away and the statistic is then updated.
 * [DocCount](xref:Lucene.Net.Index.Terms#Lucene_Net_Index_Terms_DocCount): Returns the number of documents that contain at least one occurrence of any term for this field. This can be thought of as a Field-level `DocFreq`. Like `DocFreq` it will also count deleted documents.
 * [SumDocFreq](xref:Lucene.Net.Index.Terms#Lucene_Net_Index_Terms_SumDocFreq): Returns the number of postings (term-document mappings in the inverted index) for the field. This can be thought of as the sum of [TermsEnum.DocFreq](xref:Lucene.Net.Index.TermsEnum#Lucene_Net_Index_TermsEnum_DocFreq) across all terms in the field, and like `DocFreq` it will also count postings that appear in deleted documents.
 * [SumTotalTermFreq](xref:Lucene.Net.Index.Terms#Lucene_Net_Index_Terms_SumTotalTermFreq): Returns the number of tokens for the field. This can be thought of as the sum of [TermsEnum.TotalTermFreq](xref:Lucene.Net.Index.TermsEnum#Lucene_Net_Index_TermsEnum_TotalTermFreq) across all terms in the field, and like `TotalTermFreq` it will also count occurrences that appear in deleted documents, and will be unavailable (returns `-1`) if term frequencies were omitted from the index ([DOCS_ONLY](xref:Lucene.Net.Index.FieldInfo.IndexOptions#Lucene_Net_Index_IndexOptions_DOCS_ONLY)) for the field. 

#### Segment statistics

 * [MaxDoc](xref:Lucene.Net.Index.IndexReader#Lucene_Net_Index_IndexReader_MaxDoc): Returns the number of documents (including deleted documents) in the index.
 * [NumDocs](xref:Lucene.Net.Index.IndexReader#Lucene_Net_Index_IndexReader_NumDocs): Returns the number of live documents (excluding deleted documents) in the index.
 * [NumDeletedDocs](xref:Lucene.Net.Index.IndexReader#Lucene_Net_Index_IndexReader_NumDeletedDocs): Returns the number of deleted documents in the index.
 * [Count](xref:Lucene.Net.Index.Fields#Lucene_Net_Index_Fields_Count): Returns the number of indexed fields.
 * [UniqueTermCount](xref:Lucene.Net.Index.Fields#Lucene_Net_Index_Fields_UniqueTermCount): Returns the number of indexed terms, the sum of [Count](xref:Lucene.Net.Index.Terms#Lucene_Net_Index_Terms_Count) across all fields. 

#### Document statistics

 Document statistics are available during the indexing process for an indexed field: typically a <xref:Lucene.Net.Search.Similarities.Similarity> implementation will store some of these values (possibly in a lossy way), into the normalization value for the document in its [Similarity.ComputeNorm(FieldInvertState)](xref:Lucene.Net.Search.Similarities.Similarity#Lucene_Net_Search_Similarities_Similarity_ComputeNorm_Lucene_Net_Index_FieldInvertState_) method. 

 * [Length](xref:Lucene.Net.Index.FieldInvertState#Lucene_Net_Index_FieldInvertState_Length): Returns the number of tokens for this field in the document. Note that this is just the number of times that [IncrementToken()](xref:Lucene.Net.Analysis.TokenStream#Lucene_Net_Analysis_TokenStream_IncrementToken) returned `true`, and is unrelated to the values in <xref:Lucene.Net.Analysis.TokenAttributes.PositionIncrementAttribute>.
 * [NumOverlap](xref:Lucene.Net.Index.FieldInvertState#Lucene_Net_Index_FieldInvertState_NumOverlap): Returns the number of tokens for this field in the document that had a position increment of zero. This can be used to compute a document length that discounts artificial tokens such as synonyms.
 * [Position](xref:Lucene.Net.Index.FieldInvertState#Lucene_Net_Index_FieldInvertState_Position): Returns the accumulated position value for this field in the document: computed from the values of <xref:Lucene.Net.Analysis.TokenAttributes.PositionIncrementAttribute> and including [GetPositionIncrementGap(String)](xref:Lucene.Net.Analysis.Analyzer#Lucene_Net_Analysis_Analyzer_GetPositionIncrementGap_System_String_)s across multivalued fields.
 * [Offset](xref:Lucene.Net.Index.FieldInvertState#Lucene_Net_Index_FieldInvertState_Offset): Returns the total character offset value for this field in the document: computed from the values of <xref:Lucene.Net.Analysis.TokenAttributes.OffsetAttribute> returned by [End()](xref:Lucene.Net.Analysis.TokenStream#Lucene_Net_Analysis_TokenStream_End), and including [GetOffsetGap(String)](xref:Lucene.Net.Analysis.Analyzer#Lucene_Net_Analysis_Analyzer_GetOffsetGap_System_String)s across multivalued fields.
 * [UniqueTermCount](xref:Lucene.Net.Index.FieldInvertState#Lucene_Net_Index_FieldInvertState_UniqueTermCount): Returns the number of unique terms encountered for this field in the document. 
 * [MaxTermFrequency](xref:Lucene.Net.Index.FieldInvertState#Lucene_Net_Index_FieldInvertState_MaxTermFrequency): Returns the maximum frequency across all unique terms encountered for this field in the document. 

 Additional user-supplied statistics can be added to the document as DocValues fields and accessed via [GetNumericDocValues(String)](xref:Lucene.Net.Index.AtomicReader#Lucene_Net_Index_AtomicReader_GetNumericDocValues_System_String_). 