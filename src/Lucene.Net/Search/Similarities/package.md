---
uid: Lucene.Net.Search.Similarities
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

This package contains the various ranking models that can be used in Lucene. The
abstract class <xref:Lucene.Net.Search.Similarities.Similarity> serves
as the base for ranking functions. For searching, users can employ the models
already implemented or create their own by extending one of the classes in this
package.

## Table Of Contents

1. [Summary of the Ranking Methods](#summary-of-the-ranking-methods)
2. [Changing the Similarity](#changing-similarity) 

## Summary of the Ranking Methods

<xref:Lucene.Net.Search.Similarities.DefaultSimilarity> is the original Lucene scoring function. It is based on a highly optimized [Vector Space Model](http://en.wikipedia.org/wiki/Vector_Space_Model). For more information, see <xref:Lucene.Net.Search.Similarities.TFIDFSimilarity>.

<xref:Lucene.Net.Search.Similarities.BM25Similarity> is an optimized implementation of the successful Okapi BM25 model.

<xref:Lucene.Net.Search.Similarities.SimilarityBase> provides a basic implementation of the Similarity contract and exposes a highly simplified interface, which makes it an ideal starting point for new ranking functions. Lucene ships the following methods built on <xref:Lucene.Net.Search.Similarities.SimilarityBase>:

<a id="framework"></a>

* Amati and Rijsbergen's [DFR](xref:Lucene.Net.Search.Similarities.DFRSimilarity) framework;
* Clinchant and Gaussier's [Information-based models](xref:Lucene.Net.Search.Similarities.IBSimilarity) for IR;
* The implementation of two [language models](xref:Lucene.Net.Search.Similarities.LMSimilarity) from Zhai and Lafferty's paper.

Since <xref:Lucene.Net.Search.Similarities.SimilarityBase> is not optimized to the same extent as <xref:Lucene.Net.Search.Similarities.DefaultSimilarity> and <xref:Lucene.Net.Search.Similarities.BM25Similarity>, a difference in performance is to be expected when using the methods listed above. However, optimizations can always be implemented in subclasses; see [below](#changing-similarity).

## Changing Similarity

Chances are the available Similarities are sufficient for all your searching needs. However, in some applications it may be necessary to customize your <xref:Lucene.Net.Search.Similarities.Similarity> implementation. For instance, some applications do not need to distinguish between shorter and longer documents (see [a "fair" similarity](http://www.gossamer-threads.com/lists/lucene/java-user/38967#38967)).

To change <xref:Lucene.Net.Search.Similarities.Similarity>, one must do so for both indexing and searching, and the changes must happen before either of these actions take place. Although in theory there is nothing stopping you from changing mid-stream, it just isn't well-defined what is going to happen. 

To make this change, implement your own <xref:Lucene.Net.Search.Similarities.Similarity> (likely you'll want to simply subclass an existing method, be it <xref:Lucene.Net.Search.Similarities.DefaultSimilarity> or a descendant of <xref:Lucene.Net.Search.Similarities.SimilarityBase>), and then register the new class by setting [IndexWriterConfig.Similarity](xref:Lucene.Net.Index.IndexWriterConfig#Lucene_Net_Index_IndexWriterConfig_Similarity) before indexing and [IndexSearcher.Similarity](xref:Lucene.Net.Search.IndexSearcher#Lucene_Net_Search_IndexSearcher_Similarity) before searching. 

### Extending [SimilarityBase](xref:Lucene.Net.Search.Similarities.SimilarityBase)

The easiest way to quickly implement a new ranking method is to extend <xref:Lucene.Net.Search.Similarities.SimilarityBase>, which provides basic implementations for the low level . Subclasses are only required to implement the [Float)](xref:Lucene.Net.Search.Similarities.SimilarityBase#methods) and [#toString()](xref:Lucene.Net.Search.Similarities.SimilarityBase) methods.

Another option is to extend one of the [frameworks](#framework) based on <xref:Lucene.Net.Search.Similarities.SimilarityBase>. These Similarities are implemented modularly, e.g. <xref:Lucene.Net.Search.Similarities.DFRSimilarity> delegates computation of the three parts of its formula to the classes <xref:Lucene.Net.Search.Similarities.BasicModel>, <xref:Lucene.Net.Search.Similarities.AfterEffect> and <xref:Lucene.Net.Search.Similarities.Normalization>. Instead of subclassing the Similarity, one can simply introduce a new basic model and tell <xref:Lucene.Net.Search.Similarities.DFRSimilarity> to use it.

### Changing [DefaultSimilarity](xref:Lucene.Net.Search.Similarities.DefaultSimilarity)

If you are interested in use cases for changing your similarity, see the Lucene users's mailing list at [Overriding Similarity](http://www.gossamer-threads.com/lists/lucene/java-user/39125). In summary, here are a few use cases:

1. The [SweetSpotSimilarity](../../misc/Lucene.Net.Misc.SweetSpotSimilarity.html) gives small increases as the frequency increases a small amount and then greater increases when you hit the "sweet spot", i.e. where you think the frequency of terms is more significant.
2. Overriding Tf — In some applications, it doesn't matter what the score of a document is as long as a matching term occurs. In these cases people have overridden Similarity to return 1 from the Tf() method.
3. Changing Length Normalization — By overriding [State)](xref:Lucene.Net.Search.Similarities.Similarity#methods), it is possible to discount how the length of a field contributes to a score. In <xref:Lucene.Net.Search.Similarities.DefaultSimilarity>, lengthNorm = 1 / (numTerms in field)^0.5, but if one changes this to be 1 / (numTerms in field), all fields will be treated ["fairly"](http://www.gossamer-threads.com/lists/lucene/java-user/38967#38967).

In general, Chris Hostetter sums it up best in saying (from [the Lucene users's mailing list](http://www.gossamer-threads.com/lists/lucene/java-user/39125#39125)): 

> [One would override the Similarity in] ... any situation where you know more about your data then just that it's "text" is a situation where it *might* make sense to to override your Similarity method.