---
uid: Lucene.Net.Demo
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

The demo module offers simple example code to show the features of Lucene.

# Apache Lucene - Building and Installing the Basic Demo

*   [About this Document](#about-this-document)

*   [About the Demo](#about-the-demo)



*   [Indexing Files](#indexing-files)

*   [About the code](#about-the-code)

*   [Location of the source](#location-of-the-source)

*   [IndexFiles](xref:Lucene.Net.Demo.IndexFiles)

*   [Searching Files](#searching-files)

## About this Document

This document is intended as a "getting started" guide to using and running the Lucene demos. It walks you through some basic installation and configuration.

## About the Demo

The Lucene command-line demo code consists of an application that demonstrates various functionalities of Lucene and how you can add Lucene to your applications.

## Indexing Files

Once you've gotten this far you're probably itching to go. Let's __build an index!__ Assuming you've set your CLASSPATH correctly, just type:

        java org.apache.lucene.demo.IndexFiles -docs {path-to-lucene}/src

This will produce a subdirectory called <span class="codefrag">index</span>
which will contain an index of all of the Lucene source code.

To __search the index__ type:

        java org.apache.lucene.demo.SearchFiles

You'll be prompted for a query. Type in a gibberish or made up word (for example: 
"supercalifragilisticexpialidocious").
You'll see that there are no maching results in the lucene source code. 
Now try entering the word "string". That should return a whole bunch
of documents. The results will page at every tenth result and ask you whether
you want more results.

## About the code

In this section we walk through the sources behind the command-line Lucene demo: where to find them, their parts and their function. This section is intended for Java developers wishing to understand how to use Lucene in their applications.

## Location of the source

The files discussed here are linked into this documentation directly: * [IndexFiles](xref:Lucene.Net.Demo.IndexFiles): code to create a Lucene index. * [SearchFiles](xref:Lucene.Net.Demo.SearchFiles): code to search a Lucene index. 

## IndexFiles

As we discussed in the previous walk-through, the [IndexFiles](xref:Lucene.Net.Demo.IndexFiles) class creates a Lucene Index. Let's take a look at how it does this.

The <span class="codefrag">main()</span> method parses the command-line parameters, then in preparation for instantiating [IndexWriter](xref:Lucene.Net.Index.IndexWriter), opens a [Directory](xref:Lucene.Net.Store.Directory), and instantiates [StandardAnalyzer](xref:Lucene.Net.Analysis.Standard.StandardAnalyzer) and [IndexWriterConfig](xref:Lucene.Net.Index.IndexWriterConfig).

The value of the <span class="codefrag">-index</span> command-line parameter is the name of the filesystem directory where all index information should be stored. If <span class="codefrag">IndexFiles</span> is invoked with a relative path given in the <span class="codefrag">-index</span> command-line parameter, or if the <span class="codefrag">-index</span> command-line parameter is not given, causing the default relative index path "<span class="codefrag">index</span>" to be used, the index path will be created as a subdirectory of the current working directory (if it does not already exist). On some platforms, the index path may be created in a different directory (such as the user's home directory).

The <span class="codefrag">-docs</span> command-line parameter value is the location of the directory containing files to be indexed.

The <span class="codefrag">-update</span> command-line parameter tells <span class="codefrag">IndexFiles</span> not to delete the index if it already exists. When <span class="codefrag">-update</span> is not given, <span class="codefrag">IndexFiles</span> will first wipe the slate clean before indexing any documents.

Lucene [Directory](xref:Lucene.Net.Store.Directory)s are used by the <span class="codefrag">IndexWriter</span> to store information in the index. In addition to the [FSDirectory](xref:Lucene.Net.Store.FSDirectory) implementation we are using, there are several other <span class="codefrag">Directory</span> subclasses that can write to RAM, to databases, etc.

Lucene [Analyzer](xref:Lucene.Net.Analysis.Analyzer)s are processing pipelines that break up text into indexed tokens, a.k.a. terms, and optionally perform other operations on these tokens, e.g. downcasing, synonym insertion, filtering out unwanted tokens, etc. The <span class="codefrag">Analyzer</span> we are using is <span class="codefrag">StandardAnalyzer</span>, which creates tokens using the Word Break rules from the Unicode Text Segmentation algorithm specified in [Unicode Standard Annex #29](http://unicode.org/reports/tr29/); converts tokens to lowercase; and then filters out stopwords. Stopwords are common language words such as articles (a, an, the, etc.) and other tokens that may have less value for searching. It should be noted that there are different rules for every language, and you should use the proper analyzer for each. Lucene currently provides Analyzers for a number of different languages (see the javadocs under [lucene/analysis/common/src/java/org/apache/lucene/analysis](../analyzers-common/overview-summary.html)).

The <span class="codefrag">IndexWriterConfig</span> instance holds all configuration for <span class="codefrag">IndexWriter</span>. For example, we set the <span class="codefrag">OpenMode</span> to use here based on the value of the <span class="codefrag">-update</span> command-line parameter.

Looking further down in the file, after <span class="codefrag">IndexWriter</span> is instantiated, you should see the <span class="codefrag">indexDocs()</span> code. This recursive function crawls the directories and creates [Document](xref:Lucene.Net.Documents.Document) objects. The <span class="codefrag">Document</span> is simply a data object to represent the text content from the file as well as its creation time and location. These instances are added to the <span class="codefrag">IndexWriter</span>. If the <span class="codefrag">-update</span> command-line parameter is given, the <span class="codefrag">IndexWriterConfig</span> <span class="codefrag">OpenMode</span> will be set to [OpenMode.CREATE_OR_APPEND](xref:Lucene.Net.Index.IndexWriterConfig.OpenMode#methods), and rather than adding documents to the index, the <span class="codefrag">IndexWriter</span> will __update__ them in the index by attempting to find an already-indexed document with the same identifier (in our case, the file path serves as the identifier); deleting it from the index if it exists; and then adding the new document to the index.

## Searching Files

The [SearchFiles](xref:Lucene.Net.Demo.SearchFiles) class is quite simple. It primarily collaborates with an [IndexSearcher](xref:Lucene.Net.Search.IndexSearcher), [StandardAnalyzer](xref:Lucene.Net.Analysis.Standard.StandardAnalyzer), (which is used in the [IndexFiles](xref:Lucene.Net.Demo.IndexFiles) class as well) and a [QueryParser](xref:Lucene.Net.QueryParsers.Classic.QueryParser). The query parser is constructed with an analyzer used to interpret your query text in the same way the documents are interpreted: finding word boundaries, downcasing, and removing useless words like 'a', 'an' and 'the'. The <xref:Lucene.Net.Search.Query> object contains the results from the [QueryParser](xref:Lucene.Net.QueryParsers.Classic.QueryParser) which is passed to the searcher. Note that it's also possible to programmatically construct a rich <xref:Lucene.Net.Search.Query> object without using the query parser. The query parser just enables decoding the [ Lucene query syntax](../queryparser/org/apache/lucene/queryparser/classic/package-summary.html#package_description) into the corresponding [Query](xref:Lucene.Net.Search.Query) object.

<span class="codefrag">SearchFiles</span> uses the [IndexSearcher.search](xref:Lucene.Net.Search.IndexSearcher#methods) method that returns [TopDocs](xref:Lucene.Net.Search.TopDocs) with max <span class="codefrag">n</span> hits. The results are printed in pages, sorted by score (i.e. relevance).
