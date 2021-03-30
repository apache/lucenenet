---
uid: Lucene.Net.Search.VectorHighlight
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

This is an another highlighter implementation.

## Features

*   fast for large docs

*   support N-gram fields

*   support phrase-unit highlighting with slops

*   support multi-term (includes wildcard, range, regexp, etc) queries

*   highlight fields need to be stored with Positions and Offsets

*   take into account query boost and/or IDF-weight to score fragments

*   support colored highlight tags

*   pluggable FragListBuilder / FieldFragList

*   pluggable FragmentsBuilder

## Algorithm

To explain the algorithm, let's use the following sample text (to be highlighted) and user query:


<table border="1">
<tr>
<td>__Sample Text__</td>
<td>Lucene is a search engine library.</td>
</tr>
<tr>
<td>__User Query__</td>
<td>Lucene^2 OR "search library"~1</td>
</tr>
</table>

The user query is a BooleanQuery that consists of TermQuery("Lucene") with boost of 2 and PhraseQuery("search library") with slop of 1.

For your convenience, here is the offsets and positions info of the sample text.

    +--------+-----------------------------------+
    |        |          1111111111222222222233333|
    |  offset|01234567890123456789012345678901234|
    +--------+-----------------------------------+
    |document|Lucene is a search engine library. |
    +--------*-----------------------------------+
    |position|0      1  2 3      4      5        |
    +--------*-----------------------------------+

### Step 1.

In Step 1, Fast Vector Highlighter generates <xref:Lucene.Net.Search.VectorHighlight.FieldQuery.QueryPhraseMap> from the user query. `QueryPhraseMap` consists of the following members:

```cs
public class QueryPhraseMap
{
    bool terminal;
    int slop;   // valid if terminal == true and phraseHighlight == true
    float boost;  // valid if terminal == true
    IDictonary<string, QueryPhraseMap> subMap;
}
```

`QueryPhraseMap` has subMap. The key of the subMap is a term text in the user query and the value is a subsequent `QueryPhraseMap`. If the query is a term (not phrase), then the subsequent `QueryPhraseMap` is marked as terminal. If the query is a phrase, then the subsequent `QueryPhraseMap` is not a terminal and it has the next term text in the phrase.

From the sample user query, the following `QueryPhraseMap` will be generated:

       QueryPhraseMap
    +--------+-+  +-------+-+
    |"Lucene"|o+->|boost=2|*|  * : terminal
    +--------+-+  +-------+-+
    
    +--------+-+  +---------+-+  +-------+------+-+
    |"search"|o+->|"library"|o+->|boost=1|slop=1|*|
    +--------+-+  +---------+-+  +-------+------+-+

### Step 2.

In Step 2, Fast Vector Highlighter generates <xref:Lucene.Net.Search.VectorHighlight.FieldTermStack>. Fast Vector Highlighter uses term vector data (must be stored [FieldType.StoreTermVectorOffsets = true](xref:Lucene.Net.Documents.FieldType#Lucene_Net_Documents_FieldType_StoreTermVectorOffsets) and [FieldType.StoreTermVectorPositions = true](xref:Lucene.Net.Documents.FieldType#Lucene_Net_Documents_FieldType_StoreTermVectorPositions)) to generate it. `FieldTermStack` keeps the terms in the user query. Therefore, in this sample case, Fast Vector Highlighter generates the following `FieldTermStack`:

       FieldTermStack
    +------------------+
    |"Lucene"(0,6,0)   |
    +------------------+
    |"search"(12,18,3) |
    +------------------+
    |"library"(26,33,5)|
    +------------------+
    where : "termText"(startOffset,endOffset,position)

### Step 3.

In Step 3, Fast Vector Highlighter generates <xref:Lucene.Net.Search.VectorHighlight.FieldPhraseList> by reference to `QueryPhraseMap` and `FieldTermStack`.

       FieldPhraseList
    +----------------+-----------------+---+
    |"Lucene"        |[(0,6)]          |w=2|
    +----------------+-----------------+---+
    |"search library"|[(12,18),(26,33)]|w=1|
    +----------------+-----------------+---+

The type of each entry is `WeightedPhraseInfo` that consists of an array of terms offsets and weight. 

### Step 4.

In Step 4, Fast Vector Highlighter creates `FieldFragList` by reference to `FieldPhraseList`. In this sample case, the following `FieldFragList` will be generated:

       FieldFragList
    +---------------------------------+
    |"Lucene"[(0,6)]                  |
    |"search library"[(12,18),(26,33)]|
    |totalBoost=3                     |
    +---------------------------------+

The calculation for each `FieldFragList.WeightedFragInfo.totalBoost` (weight)  
depends on the implementation of `FieldFragList.Add( ... )`:

```cs
public override void Add(int startOffset, int endOffset, IList<WeightedPhraseInfo> phraseInfoList)
{
	float totalBoost = 0;
	List<SubInfo> subInfos = new List<SubInfo>();
	foreach (WeightedPhraseInfo phraseInfo in phraseInfoList)
	{
		subInfos.Add(new SubInfo(phraseInfo.GetText(), phraseInfo.TermsOffsets, phraseInfo.Seqnum, phraseInfo.Boost));
		totalBoost += phraseInfo.Boost;
	}
	FragInfos.Add(new WeightedFragInfo(startOffset, endOffset, subInfos, totalBoost));
}
```

The used implementation of `FieldFragList` is noted in `BaseFragListBuilder.createFieldFragList( ... )`:

```cs
public override FieldFragList CreateFieldFragList(FieldPhraseList fieldPhraseList, int fragCharSize)
{
	return CreateFieldFragList(fieldPhraseList, new SimpleFieldFragList(fragCharSize), fragCharSize);
}
```

Currently there are basically to approaches available: 

*   `SimpleFragListBuilder using SimpleFieldFragList`: _sum-of-boosts_-approach. The totalBoost is calculated by summarizing the query-boosts per term. Per default a term is boosted by 1.0

*   `WeightedFragListBuilder using WeightedFieldFragList`: _sum-of-distinct-weights_-approach. The totalBoost is calculated by summarizing the IDF-weights of distinct terms.

Comparison of the two approaches:

<table border="1">
<caption>
	query = das alte testament (The Old Testament)
</caption>
<tr><th>Terms in fragment</th><th>sum-of-distinct-weights</th><th>sum-of-boosts</th></tr>
<tr><td>das alte testament</td><td>5.339621</td><td>3.0</td></tr>
<tr><td>das alte testament</td><td>5.339621</td><td>3.0</td></tr>
<tr><td>das testament alte</td><td>5.339621</td><td>3.0</td></tr>
<tr><td>das alte testament</td><td>5.339621</td><td>3.0</td></tr>
<tr><td>das testament</td><td>2.9455688</td><td>2.0</td></tr>
<tr><td>das alte</td><td>2.4759595</td><td>2.0</td></tr>
<tr><td>das das das das</td><td>1.5015357</td><td>4.0</td></tr>
<tr><td>das das das</td><td>1.3003681</td><td>3.0</td></tr>
<tr><td>das das</td><td>1.061746</td><td>2.0</td></tr>
<tr><td>alte</td><td>1.0</td><td>1.0</td></tr>
<tr><td>alte</td><td>1.0</td><td>1.0</td></tr>
<tr><td>das</td><td>0.7507678</td><td>1.0</td></tr>
<tr><td>das</td><td>0.7507678</td><td>1.0</td></tr>
<tr><td>das</td><td>0.7507678</td><td>1.0</td></tr>
<tr><td>das</td><td>0.7507678</td><td>1.0</td></tr>
<tr><td>das</td><td>0.7507678</td><td>1.0</td></tr>
</table>

### Step 5.

In Step 5, by using <xref:Lucene.Net.Search.VectorHighlight.FieldFragList> and the field stored data, Fast Vector Highlighter creates highlighted snippets!