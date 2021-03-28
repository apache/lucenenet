---
uid: Lucene.Net.Analysis.Standard
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

 Fast, general-purpose grammar-based tokenizers. 

The <xref:Lucene.Net.Analysis.Standard> package contains three fast grammar-based tokenizers constructed with JFlex:

* <xref:Lucene.Net.Analysis.Standard.StandardTokenizer>:
  as of Lucene 3.1, implements the Word Break rules from the Unicode Text 
  Segmentation algorithm, as specified in 
  [Unicode Standard Annex #29](http://unicode.org/reports/tr29/).
  Unlike `UAX29URLEmailTokenizer`, URLs and email addresses are
  __not__ tokenized as single tokens, but are instead split up into 
  tokens according to the UAX#29 word break rules.<br/><br/>
  [StandardAnalyzer](xref:Lucene.Net.Analysis.Standard.StandardAnalyzer) includes
  [StandardTokenizer](xref:Lucene.Net.Analysis.Standard.StandardTokenizer),
  [StandardFilter](xref:Lucene.Net.Analysis.Standard.StandardFilter), 
  [LowerCaseFilter](xref:Lucene.Net.Analysis.Core.LowerCaseFilter)
  and [StopFilter](xref:Lucene.Net.Analysis.Core.StopFilter).
  When the `LuceneVersion` specified in the constructor is lower than 
  3.1, the [ClassicTokenizer](xref:Lucene.Net.Analysis.Standard.ClassicTokenizer)
  implementation is invoked.

* [ClassicTokenizer](xref:Lucene.Net.Analysis.Standard.ClassicTokenizer):
  this class was formerly (prior to Lucene 3.1) named 
  `StandardTokenizer`.  (Its tokenization rules are not
  based on the Unicode Text Segmentation algorithm.)
  [ClassicAnalyzer](xref:Lucene.Net.Analysis.Standard.ClassicAnalyzer) includes
  [ClassicTokenizer](xref:Lucene.Net.Analysis.Standard.ClassicTokenizer),
  [StandardFilter](xref:Lucene.Net.Analysis.Standard.StandardFilter), 
  [LowerCaseFilter](xref:Lucene.Net.Analysis.Core.LowerCaseFilter)
  and [StopFilter](xref:Lucene.Net.Analysis.Core.StopFilter).

* [UAX29URLEmailTokenizer](xref:Lucene.Net.Analysis.Standard.UAX29URLEmailTokenizer):
  implements the Word Break rules from the Unicode Text Segmentation
  algorithm, as specified in 
  [Unicode Standard Annex #29](http://unicode.org/reports/tr29/).
  URLs and email addresses are also tokenized according to the relevant RFCs.<br/><br/>
  [UAX29URLEmailAnalyzer](xref:Lucene.Net.Analysis.Standard.UAX29URLEmailAnalyzer) includes
  [UAX29URLEmailTokenizer](xref:Lucene.Net.Analysis.Standard.UAX29URLEmailTokenizer),
  [StandardFilter](xref:Lucene.Net.Analysis.Standard.StandardFilter),
  [LowerCaseFilter](xref:Lucene.Net.Analysis.Core.LowerCaseFilter)
  and [StopFilter](xref:Lucene.Net.Analysis.Core.StopFilter).