---
uid: Lucene.Net.QueryParsers.Flexible.Core.Nodes
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


Query nodes commonly used by query parser implementations.

## Query Nodes

The namespace <tt>Lucene.Net.QueryParsers.Flexible.Core.Nodes</tt> contains all the basic query nodes. The interface that represents a query node is <xref:Lucene.Net.QueryParsers.Flexible.Core.Nodes.IQueryNode>. 

<xref:Lucene.Net.QueryParsers.Flexible.Core.Nodes.IQueryNode>s are used by the text parser to create a syntax tree. These nodes are designed to be used by UI or other text parsers. The default Lucene text parser is <xref:Lucene.Net.QueryParsers.Flexible.Standard.Parser.StandardSyntaxParser>, it implements Lucene's standard syntax. 

<xref:Lucene.Net.QueryParsers.Flexible.Core.Nodes.IQueryNode> interface should be implemented by all query nodes, the class <xref:Lucene.Net.QueryParsers.Flexible.Core.Nodes.QueryNode> implements <xref:Lucene.Net.QueryParsers.Flexible.Core.Nodes.IQueryNode> and is extended by all current query node implementations. 

A query node tree can be printed to the a stream, and it generates a pseudo XML representation with all the nodes. 

A query node tree can also generate a query string that can be parsed back by the original text parser, at this point only the standard lucene syntax is supported. 

Grouping nodes:

* AndQueryNode - used for AND operator
* AnyQueryNode - used for ANY operator
* OrQueryNode - used for OR operator
* BooleanQueryNode - used when no operator is specified
* ModifierQueryNode - used for modifier operator
* GroupQueryNode - used for parenthesis
* BoostQueryNode - used for boost operator
* SlopQueryNode - phrase slop
* FuzzyQueryNode - fuzzy node
* TermRangeQueryNode - used for parametric field:`[low_value TO high_value]`
* ProximityQueryNode - used for proximity search
* NumericRangeQueryNode - used for numeric range search
* TokenizedPhraseQueryNode - used by tokenizers/lemmatizers/analyzers for phrases/autophrases 

 Leaf Nodes:

* FieldQueryNode - field/value node
* NumericQueryNode - used for numeric search
* PathQueryNode - <xref:Lucene.Net.QueryParsers.Flexible.Core.Nodes.IQueryNode> object used with path-like queries
* OpaqueQueryNode - Used as for part of the query that can be parsed by other parsers. schema/value
* PrefixWildcardQueryNode - non-phrase wildcard query
* QuotedFieldQUeryNode - regular phrase node
* WildcardQueryNode - non-phrase wildcard query 

 Utility Nodes:

* DeletedQueryNode - used by processors on optimizations
* MatchAllDocsQueryNode - used by processors on optimizations
* MatchNoDocsQueryNode - used by processors on optimizations
* NoTokenFoundQueryNode - used by tokenizers/lemmatizers/analyzers 