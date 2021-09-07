---
uid: Lucene.Net.Join
title: Lucene.Net.Join
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

This module supports index-time and query-time joins.

## Index-time joins

The index-time joining support joins while searching, where joined documents are indexed as a single document block using [IndexWriter.AddDocuments()](xref:Lucene.Net.Index.IndexWriter#Lucene_Net_Index_IndexWriter_AddDocuments_System_Collections_Generic_IEnumerable_System_Collections_Generic_IEnumerable_Lucene_Net_Index_IIndexableField___). This is useful for any normalized content (XML documents or database tables). In database terms, all rows for all joined tables matching a single row of the primary table must be indexed as a single document block, with the parent document being last in the group.

When you index in this way, the documents in your index are divided into parent documents (the last document of each block) and child documents (all others). You provide a <xref:Lucene.Net.Search.Filter> that identifies the parent documents, as Lucene does not currently record any information about doc blocks.

At search time, use <xref:Lucene.Net.Search.Join.ToParentBlockJoinQuery> to remap/join matches from any child <xref:Lucene.Net.Search.Query> (ie, a query that matches only child documents) up to the parent document space. The resulting query can then be used as a clause in any query that matches parent.

If you only care about the parent documents matching the query, you can use any collector to collect the parent hits, but if you'd also like to see which child documents match for each parent document, use the <xref:Lucene.Net.Search.Join.ToParentBlockJoinCollector> to collect the hits. Once the search is done, you retrieve a <xref:Lucene.Net.Search.Grouping.ITopGroups`1> instance from the [ToParentBlockJoinCollector.GetTopGroups()](xref:Lucene.Net.Search.Join.ToParentBlockJoinCollector#Lucene_Net_Search_Join_ToParentBlockJoinCollector_GetTopGroups_Lucene_Net_Search_Join_ToParentBlockJoinQuery_Lucene_Net_Search_Sort_System_Int32_System_Int32_System_Int32_System_Boolean_) method.

To map/join in the opposite direction, use <xref:Lucene.Net.Search.Join.ToChildBlockJoinQuery>.  This wraps
any query matching parent documents, creating the joined query
matching only child documents.

## Query-time joins

The query time joining is index term based and implemented as two pass search. The first pass collects all the terms from a `fromField` that match the `fromQuery`. The second pass returns all documents that have matching terms in a `toField` to the terms collected in the first pass. 

Query time joining has the following input:

*   `fromField`: The from field to join from.

*   `fromQuery`:  The query executed to collect the from terms. This is usually the user specified query.

*   `multipleValuesPerDocument`:  Whether the `fromField` contains more than one value per document

*   `scoreMode`:  Defines how scores are translated to the other join side. If you don't care about scoring
  use [ScoreMode.None](xref:Lucene.Net.Search.Join.ScoreMode#Lucene_Net_Search_Join_ScoreMode_None) mode. This will disable scoring and is therefore more
  efficient (requires less memory and is faster).

*   `toField`: The to field to join to

Basically the query-time joining is accessible from one static method. The user of this method supplies the method with the described input and a `IndexSearcher` where the from terms need to be collected from. The returned query can be executed with the same `IndexSearcher`, but also with another `IndexSearcher`. Example usage of the [JoinUtil.CreateJoinQuery()](xref:Lucene.Net.Search.Join.JoinUtil#Lucene_Net_Search_Join_JoinUtil_CreateJoinQuery_System_String_System_Boolean_System_String_Lucene_Net_Search_Query_Lucene_Net_Search_IndexSearcher_Lucene_Net_Search_Join_ScoreMode_): 


```cs
string fromField = "from"; // Name of the from field
bool multipleValuesPerDocument = false; // Set only yo true in the case when your fromField has multiple values per document in your index
string toField = "to"; // Name of the to field
ScoreMode scoreMode = ScoreMode.Max; // Defines how the scores are translated into the other side of the join.
Query fromQuery = new TermQuery(new Term("content", searchTerm)); // Query executed to collect from values to join to the to values

Query joinQuery = JoinUtil.CreateJoinQuery(fromField, multipleValuesPerDocument, toField, fromQuery, fromSearcher, scoreMode);
TopDocs topDocs = toSearcher.Search(joinQuery, 10); // Note: toSearcher can be the same as the fromSearcher
// Render topDocs...
```