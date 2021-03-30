---
uid: Lucene.Net.QueryParsers.Surround.Query
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

This package contains <xref:Lucene.Net.QueryParsers.Surround.Query.SrndQuery> and its subclasses.

The parser in the <xref:Lucene.Net.QueryParsers.Surround.Parser> namespace normally generates a SrndQuery.

For searching an <xref:Lucene.Net.Search.Query> is provided by the [SrndQuery.MakeLuceneQueryField()](xref:Lucene.Net.QueryParsers.Surround.Query.SrndQuery#Lucene_Net_QueryParsers_Surround_Query_SrndQuery_MakeLuceneQueryField_System_String_Lucene_Net_QueryParsers_Surround_Query_BasicQueryFactory_) method. For this, TermQuery, BooleanQuery and SpanQuery are used from Lucene.