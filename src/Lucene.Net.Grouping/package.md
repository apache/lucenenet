---
uid: Lucene.Net.Grouping
title: Lucene.Net.Grouping
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

This module enables search result grouping with Lucene.NET, where hits with the same value in the specified single-valued group field are grouped together. For example, if you group by the `author` field, then all documents with the same value in the `author` field fall into a single group.

Grouping requires a number of inputs:

*   `groupField`: this is the field used for grouping.
      For example, if you use the `author` field then each
      group has all books by the same author.  Documents that don't
      have this field are grouped under a single group with
      a `null` group value.

*   `groupSort`: how the groups are sorted.  For sorting
      purposes, each group is "represented" by the highest-sorted
      document according to the `groupSort` within it.  For
      example, if you specify "price" (ascending) then the first group
      is the one with the lowest price book within it.  Or if you
      specify relevance group sort, then the first group is the one
      containing the highest scoring book.

*   `topNGroups`: how many top groups to keep.  For
      example, 10 means the top 10 groups are computed.

*   `groupOffset`: which "slice" of top groups you want to
      retrieve.  For example, 3 means you'll get 7 groups back
      (assuming `topNGroups` is 10).  This is useful for
      paging, where you might show 5 groups per page.

*   `withinGroupSort`: how the documents within each group
      are sorted.  This can be different from the group sort.

*   `maxDocsPerGroup`: how many top documents within each
      group to keep.

*   `withinGroupOffset`: which "slice" of top
      documents you want to retrieve from each group.

The implementation is two-pass: the first pass (<xref:Lucene.Net.Search.Grouping.Terms.TermFirstPassGroupingCollector>) gathers the top groups, and the second pass (<xref:Lucene.Net.Search.Grouping.Terms.TermSecondPassGroupingCollector>) gathers documents within those groups. If the search is costly to run you may want to use the <xref:Lucene.Net.Search.CachingCollector> class, which caches hits and can (quickly) replay them for the second pass. This way you only run the query once, but you pay a RAM cost to (briefly) hold all hits. Results are returned as a <xref:Lucene.Net.Search.Grouping.TopGroups> instance.

 This module abstracts away what defines group and how it is collected. All grouping collectors are abstract and have currently term based implementations. One can implement collectors that for example group on multiple fields. 

Known limitations:

*   For the two-pass grouping search, the group field must be a
    single-valued indexed field (or indexed as a <xref:Lucene.Net.Documents.SortedDocValuesField>).
    <xref:Lucene.Net.Search.FieldCache> is used to load the <xref:Lucene.Net.Index.SortedDocValues> for this field.

*   Although Solr support grouping by function and this module has abstraction of what a group is, there are currently only
    implementations for grouping based on terms.

*   Sharding is not directly supported, though is not too
    difficult, if you can merge the top groups and top documents per
    group yourself.

Typical usage for the generic two-pass grouping search looks like this using the grouping convenience utility (optionally using caching for the second pass search):

```cs
GroupingSearch groupingSearch = new GroupingSearch("author");
groupingSearch.SetGroupSort(groupSort);
groupingSearch.SetFillSortFields(fillFields);

if (useCache)
{
    // Sets cache in MB
    groupingSearch.SetCachingInMB(maxCacheRAMMB: 4.0, cacheScores: true);
}

if (requiredTotalGroupCount)
{
    groupingSearch.SetAllGroups(true);
}

TermQuery query = new TermQuery(new Term("content", searchTerm));
TopGroups<BytesRef> result = groupingSearch.Search(indexSearcher, query, groupOffset, groupLimit);

// Render groupsResult...
if (requiredTotalGroupCount)
{
    // If null, the value is not computed
    int? totalGroupCount = result.TotalGroupCount;
}
```

To use the single-pass `BlockGroupingCollector`, first, at indexing time, you must ensure all docs in each group are added as a block, and you have some way to find the last document of each group. One simple way to do this is to add a marker binary field:

```cs
// Create Documents from your source:
List<Document> oneGroup = ...;

Field groupEndField = new StringField("groupEnd", "x", Field.Store.NO);
oneGroup[oneGroup.Count - 1].Add(groupEndField);

// You can also use writer.UpdateDocuments(); just be sure you
// replace an entire previous doc block with this new one.  For
// example, each group could have a "groupID" field, with the same
// value for all docs in this group:
writer.AddDocuments(oneGroup);
```

Then, at search time, do this up front:

```cs
// Set this once in your app & save away for reusing across all queries:
Filter groupEndDocs = new CachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("groupEnd", "x"))));
```

Finally, do this per search:

```cs
// Per search:
BlockGroupingCollector c = new BlockGroupingCollector(groupSort, groupOffset + topNGroups, needsScores, groupEndDocs);
s.Search(new TermQuery(new Term("content", searchTerm)), c);
TopGroups<object> groupsResult = c.GetTopGroups(withinGroupSort, groupOffset, docOffset, docOffset + docsPerGroup, fillFields);

// Render groupsResult...
```

Or alternatively use the `GroupingSearch` convenience utility:

```cs
// Per search:
GroupingSearch groupingSearch = new GroupingSearch(groupEndDocs);
groupingSearch.SetGroupSort(groupSort);
groupingSearch.SetIncludeScores(needsScores);
TermQuery query = new TermQuery(new Term("content", searchTerm));
TopGroups<object> groupsResult = groupingSearch.Search(indexSearcher, query, groupOffset, groupLimit);

// Render groupsResult...
```

Note that the `groupValue` of each `GroupDocs`
will be `null`, so if you need to present this value you'll
have to separately retrieve it (for example using stored
fields, `FieldCache`, etc.).

Another collector is the `TermAllGroupHeadsCollector` that can be used to retrieve all most relevant documents per group. Also known as group heads. This can be useful in situations when one wants to compute group based facets / statistics on the complete query result. The collector can be executed during the first or second phase. This collector can also be used with the `GroupingSearch` convenience utility, but when if one only wants to compute the most relevant documents per group it is better to just use the collector as done here below.

```cs
AbstractAllGroupHeadsCollector c = TermAllGroupHeadsCollector.Create(groupField, sortWithinGroup);
s.Search(new TermQuery(new Term("content", searchTerm)), c);
// Return all group heads as int array
int[] groupHeadsArray = c.RetrieveGroupHeads();
// Return all group heads as FixedBitSet.
int maxDoc = s.MaxDoc;
FixedBitSet groupHeadsBitSet = c.RetrieveGroupHeads(maxDoc);
```

For each of the above collector types there is also a variant that works with `ValueSource` instead of of fields. Concretely this means that these variants can work with functions. These variants are slower than there term based counter parts. These implementations are located in the `Lucene.Net.Search.Grouping.Function` package, but can also be used with the `GroupingSearch` convenience utility 