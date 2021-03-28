---
uid: Lucene.Net.ICU
title: Lucene.Net.ICU
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

This module exposes functionality from 
[ICU](http://site.icu-project.org/) to Apache Lucene. ICU4N is a .NET
library that enhances .NET's internationalization support by improving 
performance, keeping current with the Unicode Standard, and providing richer
APIs.

> [!NOTE]
> Since the .NET platform doesn't provide a BreakIterator class (or similar), the functionality that utilizes it was consolidated from Java Lucene's analyzers-icu package, <xref:Lucene.Net.Analysis.Common> and <xref:Lucene.Net.Highlighter> into this unified package.
> [!WARNING]
> While ICU4N's BreakIterator has customizable rules, its default behavior is not the same as the one in the JDK. When using any features of this package outside of the <xref:Lucene.Net.Analysis.Icu> namespace, they will behave differently than they do in Java Lucene and the rules may need some tweaking to fit your needs. See the [Break Rules](http://userguide.icu-project.org/boundaryanalysis/break-rules) ICU documentation for details on how to customize `ICU4N.Text.RuleBaseBreakIterator`.



This module exposes the following functionality: 

* [Text Analysis](xref:Lucene.Net.Analysis.Icu): For an introduction to Lucene's analysis API, see the <xref:Lucene.Net.Analysis> package documentation.

  * [Text Segmentation](xref:Lucene.Net.Analysis.Icu#text-segmentation): Tokenizes text based on 
  properties and rules defined in Unicode.

  * [Collation](xref:Lucene.Net.Analysis.Icu#collation): Compare strings according to the 
  conventions and standards of a particular language, region or country.

  * [Normalization](xref:Lucene.Net.Analysis.Icu#normalization): Converts text to a unique,
  equivalent form.

  * [Case Folding](xref:Lucene.Net.Analysis.Icu#case-folding): Removes case distinctions with
  Unicode's Default Caseless Matching algorithm.

  * [Search Term Folding](xref:Lucene.Net.Analysis.Icu#search-term-folding): Removes distinctions
  (such as accent marks) between similar characters for a loose or fuzzy search.

  * [Text Transformation](xref:Lucene.Net.Analysis.Icu#text-transformation): Transforms Unicode text in
  a context-sensitive fashion: e.g. mapping Traditional to Simplified Chinese

  * [Thai Language Analysis](xref:Lucene.Net.Analysis.Th)

* Unicode Highlighter Support

  * [Postings Highlighter](xref:Lucene.Net.Search.PostingsHighlight): Highlighter implementation that uses offsets from postings lists.

  * [Vector Highlighter](xref:Lucene.Net.Search.VectorHighlight.BreakIteratorBoundaryScanner): An implementation of IBoundaryScanner for use with the vector highlighter in the [Lucene.Net.Highlighter module](../highlighter/Lucene.Net.Search.Highlight.html).

