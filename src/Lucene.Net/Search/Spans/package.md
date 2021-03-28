---
uid: Lucene.Net.Search.Spans
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

The calculus of spans.

A span is a `<doc,startPosition,endPosition>` tuple.

The following span query operators are implemented:

* A [SpanTermQuery](xref:Lucene.Net.Search.Spans.SpanTermQuery) matches all spans containing a particular [Term](xref:Lucene.Net.Index.Term).
* A [SpanNearQuery](xref:Lucene.Net.Search.Spans.SpanNearQuery) matches spans which occur near one another, and can be used to implement things like phrase search (when constructed from <xref:Lucene.Net.Search.Spans.SpanTermQuery>s) and inter-phrase proximity (when constructed from other <xref:Lucene.Net.Search.Spans.SpanNearQuery>s).
* A [SpanOrQuery](xref:Lucene.Net.Search.Spans.SpanOrQuery) merges spans from a number of other <xref:Lucene.Net.Search.Spans.SpanQuery>s.
* A [SpanNotQuery](xref:Lucene.Net.Search.Spans.SpanNotQuery) removes spans matching one [SpanQuery](xref:Lucene.Net.Search.Spans.SpanQuery) which overlap (or comes near) another. This can be used, e.g., to implement within-paragraph search.
* A [SpanFirstQuery](xref:Lucene.Net.Search.Spans.SpanFirstQuery) matches spans matching `q` whose end position is less than `n`. This can be used to constrain matches to the first part of the document.
* A [SpanPositionRangeQuery](xref:Lucene.Net.Search.Spans.SpanPositionRangeQuery) is a more general form of SpanFirstQuery that can constrain matches to arbitrary portions of the document. In all cases, output spans are minimally inclusive. In other words, a span formed by matching a span in x and y starts at the lesser of the two starts and ends at the greater of the two ends. 

For example, a span query which matches "John Kerry" within ten
words of "George Bush" within the first 100 words of the document
could be constructed with:

```cs
SpanQuery john   = new SpanTermQuery(new Term("content", "john"));
SpanQuery kerry  = new SpanTermQuery(new Term("content", "kerry"));
SpanQuery george = new SpanTermQuery(new Term("content", "george"));
SpanQuery bush   = new SpanTermQuery(new Term("content", "bush"));
    
SpanQuery johnKerry =
    new SpanNearQuery(new SpanQuery[] { john, kerry }, 0, true);
    
SpanQuery georgeBush =
   new SpanNearQuery(new SpanQuery[] { george, bush }, 0, true);
    
SpanQuery johnKerryNearGeorgeBush =
   new SpanNearQuery(new SpanQuery[] { johnKerry, georgeBush }, 10, false);
    
SpanQuery johnKerryNearGeorgeBushAtStart =
   new SpanFirstQuery(johnKerryNearGeorgeBush, 100);
```

Span queries may be freely intermixed with other Lucene queries.
So, for example, the above query can be restricted to documents which
also use the word "iraq" with:

```cs
Query query = new BooleanQuery
{
    johnKerryNearGeorgeBushAtStart,
    new TermQuery("content", "iraq")
};
```