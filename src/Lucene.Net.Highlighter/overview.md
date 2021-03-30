---
uid: Lucene.Net.Highlighter
title: Lucene.Net.Highlighter
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

The highlight package contains classes to provide "keyword in context" features typically used to highlight search terms in the text of results pages. There are 3 main highlighters:

* <xref:Lucene.Net.Search.Highlight> - A lightweight highlighter for basic usage.

* <xref:Lucene.Net.Search.PostingsHighlight> (In the <xref:Lucene.Net.ICU> package) - Highlighter implementation that uses offsets from postings lists. This highlighter supports Unicode.

* <xref:Lucene.Net.Search.VectorHighlight> - This highlighter is fast for large docs, supports N-gram fields, multi-term highlighting, colored highlight tags, and more. There is a <xref:Lucene.Net.Search.VectorHighlight.BreakIteratorBoundaryScanner> in the <xref:Lucene.Net.ICU> package that can be added on for Unicode support.