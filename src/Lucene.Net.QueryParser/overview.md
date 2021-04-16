---
uid: Lucene.Net.QueryParser
title: Lucene.Net.QueryParser
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

Apache Lucene QueryParsers.

This module provides a number of queryparsers:

*   [Classic](#classicxreflucenenetqueryparsersclassic)

*   [Analyzing](#analyzingxreflucenenetqueryparsersanalyzing)

*   [Complex Phrase](#complex-phrasexreflucenenetqueryparserscomplexphrase)

*   [Extendable](#extendablexreflucenenetqueryparsersext)

*   [Flexible](#flexible)

*   [Surround](#surround)

*   [XML](#xmlxreflucenenetqueryparsersxml)

* * *

## [Classic](xref:Lucene.Net.QueryParsers.Classic)

A Simple Lucene QueryParser implemented with JavaCC.

## [Analyzing](xref:Lucene.Net.QueryParsers.Analyzing)

QueryParser that passes Fuzzy-, Prefix-, Range-, and WildcardQuerys through the given analyzer.

## [Complex Phrase](xref:Lucene.Net.QueryParsers.ComplexPhrase)

QueryParser which permits complex phrase query syntax eg "(john jon jonathan~) peters*"

## [Extendable](xref:Lucene.Net.QueryParsers.Ext)

Extendable QueryParser provides a simple and flexible extension mechanism by overloading query field names.

## Flexible

This project contains the new Lucene query parser implementation, which matches the syntax of the core QueryParser but offers a more modular architecture to enable customization. 

It's currently divided in 2 main namespaces:

* <xref:Lucene.Net.QueryParsers.Flexible.Core>: it contains the query parser API classes, which should be extended by query parser implementations.
* <xref:Lucene.Net.QueryParsers.Flexible.Standard>: it contains the current Lucene query parser implementation using the new query parser API. 

### Features

1.  Full support for boolean logic (not enabled)

2.  QueryNode Trees - support for several syntaxes, 
            that can be converted into similar syntax QueryNode trees.

3.  QueryNode Processors - Optimize, validate, rewrite the 
            QueryNode trees

4.  Processors Pipelines - Select your favorite Processor
		    and build a processor pipeline, to implement the features you need

5.  Config Interfaces - Allow the consumer of the Query Parser to implement
            a diff Config Handler Objects to suite their needs.

6.  Standard Builders - convert QueryNode's into several lucene 
            representations. Supported conversion is using a 2.4 compatible logic

7.  QueryNode tree's can be converted to a lucene 2.4 syntax string, using ToQueryString()                          

### Design

 This new query parser was designed to have very generic architecture, so that it can be easily used for different products with varying query syntaxes. This code is much more flexible and extensible than the Lucene query parser in 2.4.X. 

 The new query parser goal is to separate syntax and semantics of a query. E.g. 'a AND b', '+a +b', 'AND(a,b)' could be different syntaxes for the same query. It distinguishes the semantics of the different query components, e.g. whether and how to tokenize/lemmatize/normalize the different terms or which Query objects to create for the terms. It allows to write a parser with a new syntax, while reusing the underlying semantics, as quickly as possible. 

 The query parser has three layers and its core is what we call the QueryNode tree. It is a tree that initially represents the syntax of the original query, e.g. for 'a AND b': 

          AND
         /   \
        A     B

 The three layers are: 

<dl>
<dt>QueryParser</dt>
<dd>
This layer is the text parsing layer which simply transforms the
query text string into a <xref:Lucene.Net.QueryParsers.Flexible.Core.Nodes.IQueryNode> tree. Every text parser
must implement the interface <xref:Lucene.Net.QueryParsers.Flexible.Core.Parser.ISyntaxParser>.
Lucene default implementations implements it using JavaCC.
</dd>

<dt>QueryNodeProcessor</dt>
<dd>The query node processors do most of the work. It is in fact a
configurable chain of processors. Each processors can walk the tree and
modify nodes or even the tree's structure. That makes it possible to
e.g. do query optimization before the query is executed or to tokenize
terms.
</dd>

<dt>QueryBuilder</dt>
<dd>
The third layer is a configurable map of builders, which map <xref:Lucene.Net.QueryParsers.Flexible.Core.Nodes.IQueryNode> types to its specific 
builder that will transform the IQueryNode into Lucene Query object.
</dd>

</dl>

Furthermore, the query parser uses flexible configuration objects. It also uses message classes that allow to attach resource bundles. This makes it possible to translate messages, which is an important feature of a query parser. 

This design allows to develop different query syntaxes very quickly. 

### StandardQueryParser and QueryParserWrapper

The classic Lucene query parser is located under
<xref:Lucene.Net.QueryParsers.Classic>.

To make it simpler to use the new query parser 
the class <xref:Lucene.Net.QueryParsers.Flexible.Standard.StandardQueryParser> may be helpful,
specially for people that do not want to extend the Query Parser.
It uses the default Lucene query processors, text parser and builders, so
you don't need to worry about dealing with those.

<xref:Lucene.Net.QueryParsers.Flexible.Standard.StandardQueryParser> usage:


```cs
const LuceneVersion matchVersion = LuceneVersion.LUCENE_48;
StandardQueryParser qpHelper = new StandardQueryParser();
QueryConfigHandler config = qpHelper.QueryConfigHandler;
config.Set(ConfigurationKeys.ALLOW_LEADING_WILDCARD, true);
config.Set(ConfigurationKeys.ANALYZER, new WhitespaceAnalyzer(matchVersion));
Query query = qpHelper.Parse("apache AND lucene", "defaultField");
```

## Surround

A QueryParser that supports the Span family of queries as well as pre and infix notation.

It's divided in 2 main namespaces:

* <xref:Lucene.Net.QueryParsers.Surround.Parser>
* <xref:Lucene.Net.QueryParsers.Surround.Query>

## [XML](xref:Lucene.Net.QueryParsers.Xml)

A QueryParser that produces Lucene Query objects from XML documents.