
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

Code to search indices.

## Table Of Contents

 1. [Search Basics](#search) 2. [The Query Classes](#query) 3. [Scoring: Introduction](#scoring) 4. [Scoring: Basics](#scoringBasics) 5. [Changing the Scoring](#changingScoring) 6. [Appendix: Search Algorithm](#algorithm) 

## Search Basics

 Lucene offers a wide variety of [](xref:Lucene.Net.Search.Query) implementations, most of which are in this package, its subpackages ([](xref:Lucene.Net.Search.Spans spans), [](xref:Lucene.Net.Search.Payloads payloads)), or the [queries module]({@docRoot}/../queries/overview-summary.html). These implementations can be combined in a wide variety of ways to provide complex querying capabilities along with information about where matches took place in the document collection. The [Query Classes](#query) section below highlights some of the more important Query classes. For details on implementing your own Query class, see [Custom Queries -- Expert Level](#customQueriesExpert) below. 

 To perform a search, applications usually call [](xref:Lucene.Net.Search.IndexSearcher.Search(Query,int)) or [](xref:Lucene.Net.Search.IndexSearcher.Search(Query,Filter,int)). 

 Once a Query has been created and submitted to the [](xref:Lucene.Net.Search.IndexSearcher IndexSearcher), the scoring process begins. After some infrastructure setup, control finally passes to the [](xref:Lucene.Net.Search.Weight Weight) implementation and its [](xref:Lucene.Net.Search.Scorer Scorer) or [](xref:Lucene.Net.Search.BulkScorer BulkScore) instances. See the [Algorithm](#algorithm) section for more notes on the process. 

    <!-- TODO: this page over-links the same things too many times -->

## Query Classes

#### 
    [](xref:Lucene.Net.Search.TermQuery TermQuery)

Of the various implementations of [](xref:Lucene.Net.Search.Query Query), the [](xref:Lucene.Net.Search.TermQuery TermQuery) is the easiest to understand and the most often used in applications. A [](xref:Lucene.Net.Search.TermQuery TermQuery) matches all the documents that contain the specified [](xref:Lucene.Net.Index.Term Term), which is a word that occurs in a certain [](xref:Lucene.Net.Documents.Field Field). Thus, a [](xref:Lucene.Net.Search.TermQuery TermQuery) identifies and scores all [](xref:Lucene.Net.Documents.Document Document)s that have a [](xref:Lucene.Net.Documents.Field Field) with the specified string in it. Constructing a [](xref:Lucene.Net.Search.TermQuery TermQuery) is as simple as: TermQuery tq = new TermQuery(new Term("fieldName", "term")); In this example, the [](xref:Lucene.Net.Search.Query Query) identifies all [](xref:Lucene.Net.Documents.Document Document)s that have the [](xref:Lucene.Net.Documents.Field Field) named <tt>"fieldName"</tt> containing the word <tt>"term"</tt>. 

#### 
    [](xref:Lucene.Net.Search.BooleanQuery BooleanQuery)

Things start to get interesting when one combines multiple [](xref:Lucene.Net.Search.TermQuery TermQuery) instances into a [](xref:Lucene.Net.Search.BooleanQuery BooleanQuery). A [](xref:Lucene.Net.Search.BooleanQuery BooleanQuery) contains multiple [](xref:Lucene.Net.Search.BooleanClause BooleanClause)s, where each clause contains a sub-query ([](xref:Lucene.Net.Search.Query Query) instance) and an operator (from [](xref:Lucene.Net.Search.BooleanClause.Occur BooleanClause.Occur)) describing how that sub-query is combined with the other clauses: 1. <p>[](xref:Lucene.Net.Search.BooleanClause.Occur.SHOULD SHOULD) — Use this operator when a clause can occur in the result set, but is not required. If a query is made up of all SHOULD clauses, then every document in the result set matches at least one of these clauses.</p> 2. <p>[](xref:Lucene.Net.Search.BooleanClause.Occur.MUST MUST) — Use this operator when a clause is required to occur in the result set. Every document in the result set will match all such clauses.</p> 3. <p>[](xref:Lucene.Net.Search.BooleanClause.Occur.MUST_NOT MUST NOT) — Use this operator when a clause must not occur in the result set. No document in the result set will match any such clauses.</p> Boolean queries are constructed by adding two or more [](xref:Lucene.Net.Search.BooleanClause BooleanClause) instances. If too many clauses are added, a [](xref:Lucene.Net.Search.BooleanQuery.TooManyClauses TooManyClauses) exception will be thrown during searching. This most often occurs when a [](xref:Lucene.Net.Search.Query Query) is rewritten into a [](xref:Lucene.Net.Search.BooleanQuery BooleanQuery) with many [](xref:Lucene.Net.Search.TermQuery TermQuery) clauses, for example by [](xref:Lucene.Net.Search.WildcardQuery WildcardQuery). The default setting for the maximum number of clauses 1024, but this can be changed via the static method [](xref:Lucene.Net.Search.BooleanQuery.SetMaxClauseCount(int)). 

#### Phrases

Another common search is to find documents containing certain phrases. This
    is handled three different ways:

1.  

[](xref:Lucene.Net.Search.PhraseQuery PhraseQuery) — Matches a sequence of [](xref:Lucene.Net.Index.Term Term)s. [](xref:Lucene.Net.Search.PhraseQuery PhraseQuery) uses a slop factor to determine how many positions may occur between any two terms in the phrase and still be considered a match. The slop is 0 by default, meaning the phrase must match exactly.

2.  

[](xref:Lucene.Net.Search.MultiPhraseQuery MultiPhraseQuery) — A more general form of PhraseQuery that accepts multiple Terms for a position in the phrase. For example, this can be used to perform phrase queries that also incorporate synonyms. 3. <p>[](xref:Lucene.Net.Search.Spans.SpanNearQuery SpanNearQuery) — Matches a sequence of other [](xref:Lucene.Net.Search.Spans.SpanQuery SpanQuery) instances. [](xref:Lucene.Net.Search.Spans.SpanNearQuery SpanNearQuery) allows for much more complicated phrase queries since it is constructed from other [](xref:Lucene.Net.Search.Spans.SpanQuery SpanQuery) instances, instead of only [](xref:Lucene.Net.Search.TermQuery TermQuery) instances.</p> 

#### 
    [](xref:Lucene.Net.Search.TermRangeQuery TermRangeQuery)

The [](xref:Lucene.Net.Search.TermRangeQuery TermRangeQuery) matches all documents that occur in the exclusive range of a lower [](xref:Lucene.Net.Index.Term Term) and an upper [](xref:Lucene.Net.Index.Term Term) according to [](xref:Lucene.Net.Index.TermsEnum.GetComparator TermsEnum.GetComparator()). It is not intended for numerical ranges; use [](xref:Lucene.Net.Search.NumericRangeQuery NumericRangeQuery) instead. For example, one could find all documents that have terms beginning with the letters <tt>a</tt> through <tt>c</tt>. 

#### 
    [](xref:Lucene.Net.Search.NumericRangeQuery NumericRangeQuery)

The [](xref:Lucene.Net.Search.NumericRangeQuery NumericRangeQuery) matches all documents that occur in a numeric range. For NumericRangeQuery to work, you must index the values using a one of the numeric fields ([](xref:Lucene.Net.Documents.IntField IntField), [](xref:Lucene.Net.Documents.LongField LongField), [](xref:Lucene.Net.Documents.FloatField FloatField), or [](xref:Lucene.Net.Documents.DoubleField DoubleField)). 

#### 
    [](xref:Lucene.Net.Search.PrefixQuery PrefixQuery),
    [](xref:Lucene.Net.Search.WildcardQuery WildcardQuery),
    [](xref:Lucene.Net.Search.RegexpQuery RegexpQuery)

While the [](xref:Lucene.Net.Search.PrefixQuery PrefixQuery) has a different implementation, it is essentially a special case of the [](xref:Lucene.Net.Search.WildcardQuery WildcardQuery). The [](xref:Lucene.Net.Search.PrefixQuery PrefixQuery) allows an application to identify all documents with terms that begin with a certain string. The [](xref:Lucene.Net.Search.WildcardQuery WildcardQuery) generalizes this by allowing for the use of <tt>*</tt> (matches 0 or more characters) and <tt>?</tt> (matches exactly one character) wildcards. Note that the [](xref:Lucene.Net.Search.WildcardQuery WildcardQuery) can be quite slow. Also note that [](xref:Lucene.Net.Search.WildcardQuery WildcardQuery) should not start with <tt>*</tt> and <tt>?</tt>, as these are extremely slow. Some QueryParsers may not allow this by default, but provide a `setAllowLeadingWildcard` method to remove that protection. The [](xref:Lucene.Net.Search.RegexpQuery RegexpQuery) is even more general than WildcardQuery, allowing an application to identify all documents with terms that match a regular expression pattern. 

#### 
    [](xref:Lucene.Net.Search.FuzzyQuery FuzzyQuery)

A [](xref:Lucene.Net.Search.FuzzyQuery FuzzyQuery) matches documents that contain terms similar to the specified term. Similarity is determined using [Levenshtein (edit) distance](http://en.wikipedia.org/wiki/Levenshtein). This type of query can be useful when accounting for spelling variations in the collection. 

## Scoring — Introduction

Lucene scoring is the heart of why we all love Lucene. It is blazingly fast and it hides almost all of the complexity from the user. In a nutshell, it works. At least, that is, until it doesn't work, or doesn't work as one would expect it to work. Then we are left digging into Lucene internals or asking for help on [java-user@lucene.apache.org](mailto:java-user@lucene.apache.org) to figure out why a document with five of our query terms scores lower than a different document with only one of the query terms. 

While this document won't answer your specific scoring issues, it will, hopefully, point you to the places that can help you figure out the *what* and *why* of Lucene scoring. 

Lucene scoring supports a number of pluggable information retrieval [models](http://en.wikipedia.org/wiki/Information_retrieval#Model_types), including: * [Vector Space Model (VSM)](http://en.wikipedia.org/wiki/Vector_Space_Model) * [Probablistic Models](http://en.wikipedia.org/wiki/Probabilistic_relevance_model) such as [Okapi BM25](http://en.wikipedia.org/wiki/Probabilistic_relevance_model_(BM25)) and [DFR](http://en.wikipedia.org/wiki/Divergence-from-randomness_model) * [Language models](http://en.wikipedia.org/wiki/Language_model) These models can be plugged in via the [](xref:Lucene.Net.Search.Similarities Similarity API), and offer extension hooks and parameters for tuning. In general, Lucene first finds the documents that need to be scored based on boolean logic in the Query specification, and then ranks this subset of matching documents via the retrieval model. For some valuable references on VSM and IR in general refer to [Lucene Wiki IR references](http://wiki.apache.org/lucene-java/InformationRetrieval). 

The rest of this document will cover [Scoring basics](#scoringBasics) and explain how to change your [](xref:Lucene.Net.Search.Similarities.Similarity Similarity). Next, it will cover ways you can customize the lucene internals in [Custom Queries -- Expert Level](#customQueriesExpert), which gives details on implementing your own [](xref:Lucene.Net.Search.Query Query) class and related functionality. Finally, we will finish up with some reference material in the [Appendix](#algorithm). 

## Scoring — Basics

Scoring is very much dependent on the way documents are indexed, so it is important to understand 
   indexing. (see [Lucene overview]({@docRoot}/overview-summary.html#overview_description) 
   before continuing on with this section) Be sure to use the useful
   [](xref:Lucene.Net.Search.IndexSearcher.Explain(Lucene.Net.Search.Query, int) IndexSearcher.Explain(Query, doc))
   to understand how the score for a certain matching document was
   computed.

Generally, the Query determines which documents match (a binary decision), while the Similarity determines how to assign scores to the matching documents. 

#### Fields and Documents

In Lucene, the objects we are scoring are [](xref:Lucene.Net.Documents.Document Document)s. A Document is a collection of [](xref:Lucene.Net.Documents.Field Field)s. Each Field has [](xref:Lucene.Net.Documents.FieldType semantics) about how it is created and stored ([](xref:Lucene.Net.Documents.FieldType.Tokenized() tokenized), [](xref:Lucene.Net.Documents.FieldType.Stored() stored), etc). It is important to note that Lucene scoring works on Fields and then combines the results to return Documents. This is important because two Documents with the exact same content, but one having the content in two Fields and the other in one Field may return different scores for the same query due to length normalization. 

#### Score Boosting

Lucene allows influencing search results by "boosting" at different times: * **Index-time boost** by calling [](xref:Lucene.Net.Documents.Field.SetBoost(float) Field.SetBoost()) before a document is added to the index. * **Query-time boost** by setting a boost on a query clause, calling [](xref:Lucene.Net.Search.Query.SetBoost(float) Query.SetBoost()). 

Indexing time boosts are pre-processed for storage efficiency and written to storage for a field as follows: * All boosts of that field (i.e. all boosts under the same field name in that doc) are multiplied. * The boost is then encoded into a normalization value by the Similarity object at index-time: [](xref:Lucene.Net.Search.Similarities.Similarity.ComputeNorm computeNorm()). The actual encoding depends upon the Similarity implementation, but note that most use a lossy encoding (such as multiplying the boost with document length or similar, packed into a single byte!). * Decoding of any index-time normalization values and integration into the document's score is also performed at search time by the Similarity. 

## Changing Scoring — Similarity

 Changing [](xref:Lucene.Net.Search.Similarities.Similarity Similarity) is an easy way to influence scoring, this is done at index-time with [](xref:Lucene.Net.Index.IndexWriterConfig.SetSimilarity(Lucene.Net.Search.Similarities.Similarity) IndexWriterConfig.SetSimilarity(Similarity)) and at query-time with [](xref:Lucene.Net.Search.IndexSearcher.SetSimilarity(Lucene.Net.Search.Similarities.Similarity) IndexSearcher.SetSimilarity(Similarity)). Be sure to use the same Similarity at query-time as at index-time (so that norms are encoded/decoded correctly); Lucene makes no effort to verify this. 

 You can influence scoring by configuring a different built-in Similarity implementation, or by tweaking its parameters, subclassing it to override behavior. Some implementations also offer a modular API which you can extend by plugging in a different component (e.g. term frequency normalizer). 

 Finally, you can extend the low level [](xref:Lucene.Net.Search.Similarities.Similarity Similarity) directly to implement a new retrieval model, or to use external scoring factors particular to your application. For example, a custom Similarity can access per-document values via [](xref:Lucene.Net.Search.FieldCache FieldCache) or [](xref:Lucene.Net.Index.NumericDocValues) and integrate them into the score. 

 See the [](xref:Lucene.Net.Search.Similarities) package documentation for information on the built-in available scoring models and extending or changing Similarity. 

## Custom Queries — Expert Level

Custom queries are an expert level task, so tread carefully and be prepared to share your code if you want help. 

With the warning out of the way, it is possible to change a lot more than just the Similarity when it comes to matching and scoring in Lucene. Lucene's search is a complex mechanism that is grounded by <span>three main classes</span>: 1. [](xref:Lucene.Net.Search.Query Query) — The abstract object representation of the user's information need. 2. [](xref:Lucene.Net.Search.Weight Weight) — The internal interface representation of the user's Query, so that Query objects may be reused. This is global (across all segments of the index) and generally will require global statistics (such as docFreq for a given term across all segments). 3. [](xref:Lucene.Net.Search.Scorer Scorer) — An abstract class containing common functionality for scoring. Provides both scoring and explanation capabilities. This is created per-segment. 4. [](xref:Lucene.Net.Search.BulkScorer BulkScorer) — An abstract class that scores a range of documents. A default implementation simply iterates through the hits from [](xref:Lucene.Net.Search.Scorer Scorer), but some queries such as [](xref:Lucene.Net.Search.BooleanQuery BooleanQuery) have more efficient implementations. Details on each of these classes, and their children, can be found in the subsections below. 

#### The Query Class

In some sense, the [](xref:Lucene.Net.Search.Query Query) class is where it all begins. Without a Query, there would be nothing to score. Furthermore, the Query class is the catalyst for the other scoring classes as it is often responsible for creating them or coordinating the functionality between them. The [](xref:Lucene.Net.Search.Query Query) class has several methods that are important for derived classes: 1. [](xref:Lucene.Net.Search.Query.CreateWeight(IndexSearcher) createWeight(IndexSearcher searcher)) — A [](xref:Lucene.Net.Search.Weight Weight) is the internal representation of the Query, so each Query implementation must provide an implementation of Weight. See the subsection on [The Weight Interface](#weightClass) below for details on implementing the Weight interface. 2. [](xref:Lucene.Net.Search.Query.Rewrite(IndexReader) rewrite(IndexReader reader)) — Rewrites queries into primitive queries. Primitive queries are: [](xref:Lucene.Net.Search.TermQuery TermQuery), [](xref:Lucene.Net.Search.BooleanQuery BooleanQuery), <span>and other queries that implement [](xref:Lucene.Net.Search.Query.CreateWeight(IndexSearcher) createWeight(IndexSearcher searcher))</span> 

#### The Weight Interface

The [](xref:Lucene.Net.Search.Weight Weight) interface provides an internal representation of the Query so that it can be reused. Any [](xref:Lucene.Net.Search.IndexSearcher IndexSearcher) dependent state should be stored in the Weight implementation, not in the Query class. The interface defines five methods that must be implemented: 1. [](xref:Lucene.Net.Search.Weight.GetQuery getQuery()) — Pointer to the Query that this Weight represents. 2. [](xref:Lucene.Net.Search.Weight.GetValueForNormalization() getValueForNormalization()) — A weight can return a floating point value to indicate its magnitude for query normalization. Typically a weight such as TermWeight that scores via a [](xref:Lucene.Net.Search.Similarities.Similarity Similarity) will just defer to the Similarity's implementation: [](xref:Lucene.Net.Search.Similarities.Similarity.SimWeight.GetValueForNormalization SimWeight.getValueForNormalization()). For example, with [](xref:Lucene.Net.Search.Similarities.TFIDFSimilarity Lucene's classic vector-space formula), this is implemented as the sum of squared weights: `` 3. [](xref:Lucene.Net.Search.Weight.Normalize(float,float) normalize(float norm, float topLevelBoost)) — Performs query normalization: * `topLevelBoost`: A query-boost factor from any wrapping queries that should be multiplied into every document's score. For example, a TermQuery that is wrapped within a BooleanQuery with a boost of `5` would receive this value at this time. This allows the TermQuery (the leaf node in this case) to compute this up-front a single time (e.g. by multiplying into the IDF), rather than for every document. * `norm`: Passes in a a normalization factor which may allow for comparing scores between queries. Typically a weight such as TermWeight that scores via a [](xref:Lucene.Net.Search.Similarities.Similarity Similarity) will just defer to the Similarity's implementation: [](xref:Lucene.Net.Search.Similarities.Similarity.SimWeight.Normalize SimWeight.normalize(float,float)). 4. [](xref:Lucene.Net.Search.Weight.Scorer(Lucene.Net.Index.AtomicReaderContext, Lucene.Net.Util.Bits) scorer(AtomicReaderContext context, Bits acceptDocs)) — Construct a new [](xref:Lucene.Net.Search.Scorer Scorer) for this Weight. See [The Scorer Class](#scorerClass) below for help defining a Scorer. As the name implies, the Scorer is responsible for doing the actual scoring of documents given the Query. 5. [](xref:Lucene.Net.Search.Weight.BulkScorer(Lucene.Net.Index.AtomicReaderContext, boolean, Lucene.Net.Util.Bits) scorer(AtomicReaderContext context, boolean scoreDocsInOrder, Bits acceptDocs)) — Construct a new [](xref:Lucene.Net.Search.BulkScorer BulkScorer) for this Weight. See [The BulkScorer Class](#bulkScorerClass) below for help defining a BulkScorer. This is an optional method, and most queries do not implement it. 6. [](xref:Lucene.Net.Search.Weight.Explain(Lucene.Net.Index.AtomicReaderContext, int) explain(AtomicReaderContext context, int doc)) — Provide a means for explaining why a given document was scored the way it was. Typically a weight such as TermWeight that scores via a [](xref:Lucene.Net.Search.Similarities.Similarity Similarity) will make use of the Similarity's implementation: [](xref:Lucene.Net.Search.Similarities.Similarity.SimScorer.Explain(int, Explanation) SimScorer.explain(int doc, Explanation freq)). 

#### The Scorer Class

The [](xref:Lucene.Net.Search.Scorer Scorer) abstract class provides common scoring functionality for all Scorer implementations and is the heart of the Lucene scoring process. The Scorer defines the following abstract (some of them are not yet abstract, but will be in future versions and should be considered as such now) methods which must be implemented (some of them inherited from [](xref:Lucene.Net.Search.DocIdSetIterator DocIdSetIterator)): 1. [](xref:Lucene.Net.Search.Scorer.NextDoc nextDoc()) — Advances to the next document that matches this Query, returning true if and only if there is another document that matches. 2. [](xref:Lucene.Net.Search.Scorer.DocID docID()) — Returns the id of the [](xref:Lucene.Net.Documents.Document Document) that contains the match. 3. [](xref:Lucene.Net.Search.Scorer.Score score()) — Return the score of the current document. This value can be determined in any appropriate way for an application. For instance, the [](xref:Lucene.Net.Search.TermScorer TermScorer) simply defers to the configured Similarity: [](xref:Lucene.Net.Search.Similarities.Similarity.SimScorer.Score(int, float) SimScorer.Score(int doc, float freq)). 4. [](xref:Lucene.Net.Search.Scorer.Freq freq()) — Returns the number of matches for the current document. This value can be determined in any appropriate way for an application. For instance, the [](xref:Lucene.Net.Search.TermScorer TermScorer) simply defers to the term frequency from the inverted index: [](xref:Lucene.Net.Index.DocsEnum.Freq DocsEnum.Freq()). 5. [](xref:Lucene.Net.Search.Scorer.Advance advance()) — Skip ahead in the document matches to the document whose id is greater than or equal to the passed in value. In many instances, advance can be implemented more efficiently than simply looping through all the matching documents until the target document is identified. 6. [](xref:Lucene.Net.Search.Scorer.GetChildren getChildren()) — Returns any child subscorers underneath this scorer. This allows for users to navigate the scorer hierarchy and receive more fine-grained details on the scoring process. 

#### The BulkScorer Class

The [](xref:Lucene.Net.Search.BulkScorer BulkScorer) scores a range of documents. There is only one abstract method: 1. [](xref:Lucene.Net.Search.BulkScorer.Score(Lucene.Net.Search.Collector,int) score(Collector,int)) — Score all documents up to but not including the specified max document. 

#### Why would I want to add my own Query?

In a nutshell, you want to add your own custom Query implementation when you think that Lucene's aren't appropriate for the task that you want to do. You might be doing some cutting edge research or you need more information back out of Lucene (similar to Doug adding SpanQuery functionality).

## Appendix: Search Algorithm

This section is mostly notes on stepping through the Scoring process and serves as fertilizer for the earlier sections.

In the typical search application, a [](xref:Lucene.Net.Search.Query Query) is passed to the [](xref:Lucene.Net.Search.IndexSearcher IndexSearcher), beginning the scoring process.

Once inside the IndexSearcher, a [](xref:Lucene.Net.Search.Collector Collector) is used for the scoring and sorting of the search results. These important objects are involved in a search: 1. The [](xref:Lucene.Net.Search.Weight Weight) object of the Query. The Weight object is an internal representation of the Query that allows the Query to be reused by the IndexSearcher. 2. The IndexSearcher that initiated the call. 3. A [](xref:Lucene.Net.Search.Filter Filter) for limiting the result set. Note, the Filter may be null. 4. A [](xref:Lucene.Net.Search.Sort Sort) object for specifying how to sort the results if the standard score-based sort method is not desired. 

Assuming we are not sorting (since sorting doesn't affect the raw Lucene score), we call one of the search methods of the IndexSearcher, passing in the [](xref:Lucene.Net.Search.Weight Weight) object created by [](xref:Lucene.Net.Search.IndexSearcher.CreateNormalizedWeight(Lucene.Net.Search.Query) IndexSearcher.CreateNormalizedWeight(Query)), [](xref:Lucene.Net.Search.Filter Filter) and the number of results we want. This method returns a [](xref:Lucene.Net.Search.TopDocs TopDocs) object, which is an internal collection of search results. The IndexSearcher creates a [](xref:Lucene.Net.Search.TopScoreDocCollector TopScoreDocCollector) and passes it along with the Weight, Filter to another expert search method (for more on the [](xref:Lucene.Net.Search.Collector Collector) mechanism, see [](xref:Lucene.Net.Search.IndexSearcher IndexSearcher)). The TopScoreDocCollector uses a [](xref:Lucene.Net.Util.PriorityQueue PriorityQueue) to collect the top results for the search. 

If a Filter is being used, some initial setup is done to determine which docs to include. Otherwise, we ask the Weight for a [](xref:Lucene.Net.Search.Scorer Scorer) for each [](xref:Lucene.Net.Index.IndexReader IndexReader) segment and proceed by calling [](xref:Lucene.Net.Search.BulkScorer.Score(Lucene.Net.Search.Collector) BulkScorer.Score(Collector)). 

At last, we are actually going to score some documents. The score method takes in the Collector (most likely the TopScoreDocCollector or TopFieldCollector) and does its business.Of course, here is where things get involved. The [](xref:Lucene.Net.Search.Scorer Scorer) that is returned by the [](xref:Lucene.Net.Search.Weight Weight) object depends on what type of Query was submitted. In most real world applications with multiple query terms, the [](xref:Lucene.Net.Search.Scorer Scorer) is going to be a `BooleanScorer2` created from [](xref:Lucene.Net.Search.BooleanQuery.BooleanWeight BooleanWeight) (see the section on [custom queries](#customQueriesExpert) for info on changing this). 

Assuming a BooleanScorer2, we first initialize the Coordinator, which is used to apply the coord() factor. We then get a internal Scorer based on the required, optional and prohibited parts of the query. Using this internal Scorer, the BooleanScorer2 then proceeds into a while loop based on the [](xref:Lucene.Net.Search.Scorer.NextDoc Scorer.NextDoc()) method. The nextDoc() method advances to the next document matching the query. This is an abstract method in the Scorer class and is thus overridden by all derived implementations. If you have a simple OR query your internal Scorer is most likely a DisjunctionSumScorer, which essentially combines the scorers from the sub scorers of the OR'd terms.