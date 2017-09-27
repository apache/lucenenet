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

---
uid: Lucene.Net.Demo
summary: *content
---

The demo module offers simple example code to show the features of Lucene.

# Apache Lucene - Building and Installing the Basic Demo

<div id="minitoc-area">

*   [About this Document](#About_this_Document)
*   [About the Demo](#About_the_Demo)
*   [Setting your CLASSPATH](#Setting_your_CLASSPATH)
*   [Indexing Files](#Indexing_Files)
*   [About the code](#About_the_code)
*   [Location of the source](#Location_of_the_source)
*   [IndexFiles](#IndexFiles)
*   [Searching Files](#Searching_Files)
</div>

## About this Document

<div class="section">

This document is intended as a "getting started" guide to using and running the Lucene demos. It walks you through some basic installation and configuration.

</div>

## About the Demo

<div class="section">

The Lucene command-line demo code consists of an application that demonstrates various functionalities of Lucene and how you can add Lucene to your applications.

</div>

## Setting your CLASSPATH

<div class="section">

First, you should [download](http://www.apache.org/dyn/closer.cgi/lucene/java/) the latest Lucene distribution and then extract it to a working directory.

You need four JARs: the Lucene JAR, the queryparser JAR, the common analysis JAR, and the Lucene demo JAR. You should see the Lucene JAR file in the core/ directory you created when you extracted the archive -- it should be named something like <span class="codefrag">lucene-core-{version}.jar</span>. You should also see files called <span class="codefrag">lucene-queryparser-{version}.jar</span>, <span class="codefrag">lucene-analyzers-common-{version}.jar</span> and <span class="codefrag">lucene-demo-{version}.jar</span> under queryparser, analysis/common/ and demo/, respectively.

Put all four of these files in your Java CLASSPATH.

</div>

## Indexing Files

<div class="section">

Once you've gotten this far you're probably itching to go. Let's **build an index!** Assuming you've set your CLASSPATH correctly, just type:

        java org.apache.lucene.demo.IndexFiles -docs {path-to-lucene}/src

This will produce a subdirectory called <span class="codefrag">index</span>
which will contain an index of all of the Lucene source code.

To **search the index** type:

        java org.apache.lucene.demo.SearchFiles

You'll be prompted for a query. Type in a gibberish or made up word (for example: 
"supercalifragilisticexpialidocious").
You'll see that there are no maching results in the lucene source code. 
Now try entering the word "string". That should return a whole bunch
of documents. The results will page at every tenth result and ask you whether
you want more results.</div>

## About the code

<div class="section">

In this section we walk through the sources behind the command-line Lucene demo: where to find them, their parts and their function. This section is intended for Java developers wishing to understand how to use Lucene in their applications.

</div>

## Location of the source

<div class="section">

The files discussed here are linked into this documentation directly: * [IndexFiles.java](https://github.com/apache/lucenenet/blob/{tag}/src/Lucene.Net.Demo/IndexFiles.cs): code to create a Lucene index. [SearchFiles.java](https://github.com/apache/lucenenet/blob/{tag}/src/Lucene.Net.Demo/SearchFiles.cs): code to search a Lucene index. 

</div>

## IndexFiles

<div class="section">

As we discussed in the previous walk-through, the [IndexFiles](https://github.com/apache/lucenenet/blob/{tag}/src/Lucene.Net.Demo/IndexFiles.cs) class creates a Lucene Index. Let's take a look at how it does this.

The <span class="codefrag">main()</span> method parses the command-line parameters, then in preparation for instantiating [](xref:Lucene.Net.Index.IndexWriter IndexWriter), opens a [](xref:Lucene.Net.Store.Directory Directory), and instantiates [](xref:Lucene.Net.Analysis.Standard.StandardAnalyzer StandardAnalyzer) and [](xref:Lucene.Net.Index.IndexWriterConfig IndexWriterConfig).

The value of the <span class="codefrag">-index</span> command-line parameter is the name of the filesystem directory where all index information should be stored. If <span class="codefrag">IndexFiles</span> is invoked with a relative path given in the <span class="codefrag">-index</span> command-line parameter, or if the <span class="codefrag">-index</span> command-line parameter is not given, causing the default relative index path "<span class="codefrag">index</span>" to be used, the index path will be created as a subdirectory of the current working directory (if it does not already exist). On some platforms, the index path may be created in a different directory (such as the user's home directory).

The <span class="codefrag">-docs</span> command-line parameter value is the location of the directory containing files to be indexed.

The <span class="codefrag">-update</span> command-line parameter tells <span class="codefrag">IndexFiles</span> not to delete the index if it already exists. When <span class="codefrag">-update</span> is not given, <span class="codefrag">IndexFiles</span> will first wipe the slate clean before indexing any documents.

Lucene [](xref:Lucene.Net.Store.Directory Directory)s are used by the <span class="codefrag">IndexWriter</span> to store information in the index. In addition to the [](xref:Lucene.Net.Store.FSDirectory FSDirectory) implementation we are using, there are several other <span class="codefrag">Directory</span> subclasses that can write to RAM, to databases, etc.

Lucene [](xref:Lucene.Net.Analysis.Analyzer Analyzer)s are processing pipelines that break up text into indexed tokens, a.k.a. terms, and optionally perform other operations on these tokens, e.g. downcasing, synonym insertion, filtering out unwanted tokens, etc. The <span class="codefrag">Analyzer</span> we are using is <span class="codefrag">StandardAnalyzer</span>, which creates tokens using the Word Break rules from the Unicode Text Segmentation algorithm specified in [Unicode Standard Annex #29](http://unicode.org/reports/tr29/); converts tokens to lowercase; and then filters out stopwords. Stopwords are common language words such as articles (a, an, the, etc.) and other tokens that may have less value for searching. It should be noted that there are different rules for every language, and you should use the proper analyzer for each. Lucene currently provides Analyzers for a number of different languages (see the javadocs under [lucene/analysis/common/src/java/org/apache/lucene/analysis](../analyzers-common/overview-summary.html)).

The <span class="codefrag">IndexWriterConfig</span> instance holds all configuration for <span class="codefrag">IndexWriter</span>. For example, we set the <span class="codefrag">OpenMode</span> to use here based on the value of the <span class="codefrag">-update</span> command-line parameter.

Looking further down in the file, after <span class="codefrag">IndexWriter</span> is instantiated, you should see the <span class="codefrag">indexDocs()</span> code. This recursive function crawls the directories and creates [](xref:Lucene.Net.Documents.Document Document) objects. The <span class="codefrag">Document</span> is simply a data object to represent the text content from the file as well as its creation time and location. These instances are added to the <span class="codefrag">IndexWriter</span>. If the <span class="codefrag">-update</span> command-line parameter is given, the <span class="codefrag">IndexWriterConfig</span> <span class="codefrag">OpenMode</span> will be set to [](xref:Lucene.Net.Index.IndexWriterConfig.OpenMode.CREATE_OR_APPEND OpenMode.CREATE_OR_APPEND), and rather than adding documents to the index, the <span class="codefrag">IndexWriter</span> will **update** them in the index by attempting to find an already-indexed document with the same identifier (in our case, the file path serves as the identifier); deleting it from the index if it exists; and then adding the new document to the index.

</div>

## Searching Files

<div class="section">

The [SearchFiles](https://github.com/apache/lucenenet/blob/{tag}/src/Lucene.Net.Demo/SearchFiles.cs) class is quite simple. It primarily collaborates with an [](xref:Lucene.Net.Search.IndexSearcher IndexSearcher), [](xref:Lucene.Net.Analysis.Standard.StandardAnalyzer StandardAnalyzer), (which is used in the [IndexFiles](https://github.com/apache/lucenenet/blob/{tag}/src/Lucene.Net.Demo/IndexFiles.cs) class as well) and a [](xref:Lucene.Net.QueryParsers.Classic.QueryParser QueryParser). The query parser is constructed with an analyzer used to interpret your query text in the same way the documents are interpreted: finding word boundaries, downcasing, and removing useless words like 'a', 'an' and 'the'. The [](xref:Lucene.Net.Search.Query) object contains the results from the [](xref:Lucene.Net.QueryParsers.Classic.QueryParser QueryParser) which is passed to the searcher. Note that it's also possible to programmatically construct a rich [](xref:Lucene.Net.Search.Query) object without using the query parser. The query parser just enables decoding the [ Lucene query syntax](../queryparser/org/apache/lucene/queryparser/classic/package-summary.html#package_description) into the corresponding [](xref:Lucene.Net.Search.Query Query) object.

<span class="codefrag">SearchFiles</span> uses the [](xref:Lucene.Net.Search.IndexSearcher.Search(Lucene.Net.Search.Query,int) IndexSearcher.Search(query,n)) method that returns [](xref:Lucene.Net.Search.TopDocs TopDocs) with max <span class="codefrag">n</span> hits. The results are printed in pages, sorted by score (i.e. relevance).

</div>