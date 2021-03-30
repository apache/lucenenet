---
uid: Lucene.Net.Analysis.Cn.Smart
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

Analyzer for Simplified Chinese, which indexes words.
@lucene.experimental

Three analyzers are provided for Chinese, each of which treats Chinese text in a different way.

*   StandardAnalyzer: Index unigrams (individual Chinese characters) as a token.

*   CJKAnalyzer (in the <xref:Lucene.Net.Analysis.Cjk> namespace of <xref:Lucene.Net.Analysis.Common>): Index bigrams (overlapping groups of two adjacent Chinese characters) as tokens.

*   SmartChineseAnalyzer (in this package): Index words (attempt to segment Chinese text into words) as tokens.


Example phrase： "我是中国人"

1.  StandardAnalyzer: 我－是－中－国－人

2.  CJKAnalyzer: 我是－是中－中国－国人

3.  SmartChineseAnalyzer: 我－是－中国－人
