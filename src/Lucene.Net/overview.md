---
uid: Lucene.Net
title: Lucene.Net
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

Apache Lucene is a high-performance, full-featured text search engine library. Here's a simple example how to use Lucene for indexing and searching (using JUnit to check if the results are what we expect):

<!-- =   Java2Html Converter 5.0 [2006-03-04] by Markus Gebhard  markus@jave.de   = -->

        Analyzer analyzer = new StandardAnalyzer(Version.LUCENE_CURRENT);
    
    // Store the index in memory:
        Directory directory = new RAMDirectory();
        // To store an index on disk, use this instead:
        //Directory directory = FSDirectory.open("/tmp/testindex");
        IndexWriterConfig config = new IndexWriterConfig(Version.LUCENE_CURRENT, analyzer);
        IndexWriter iwriter = new IndexWriter(directory, config);
        Document doc = new Document();
        String text = "This is the text to be indexed.";
        doc.add(new Field("fieldname", text, TextField.TYPE_STORED));
        iwriter.addDocument(doc);
        iwriter.close();

        // Now search the index:
        DirectoryReader ireader = DirectoryReader.open(directory);
        IndexSearcher isearcher = new IndexSearcher(ireader);
        // Parse a simple query that searches for "text":
        QueryParser parser = new QueryParser(Version.LUCENE_CURRENT, "fieldname", analyzer);
        Query query = parser.parse("text");
        ScoreDoc[] hits = isearcher.search(query, null, 1000).scoreDocs;
        assertEquals(1, hits.length);
        // Iterate through the results:
        for (int i = 0; i < hits.length;="" i++)="" {="" document="" hitdoc="isearcher.doc(hits[i].doc);" assertequals("this="" is="" the="" text="" to="" be="" indexed.",="" hitdoc.get("fieldname"));="" }="" ireader.close();="">

The Lucene API is divided into several packages:

*   __<xref:Lucene.Net.Analysis>__
defines an abstract [Analyzer](xref:Lucene.Net.Analysis.Analyzer)
API for converting text from a {@link java.io.Reader}
into a [TokenStream](xref:Lucene.Net.Analysis.TokenStream),
an enumeration of token [Attribute](xref:Lucene.Net.Util.Attribute)s. 
A TokenStream can be composed by applying [TokenFilter](xref:Lucene.Net.Analysis.TokenFilter)s
to the output of a [Tokenizer](xref:Lucene.Net.Analysis.Tokenizer). 
Tokenizers and TokenFilters are strung together and applied with an [Analyzer](xref:Lucene.Net.Analysis.Analyzer). 
[analyzers-common](../analyzers-common/overview-summary.html) provides a number of Analyzer implementations, including 
[StopAnalyzer](../analyzers-common/org/apache/lucene/analysis/core/StopAnalyzer.html)
and the grammar-based [StandardAnalyzer](../analyzers-common/org/apache/lucene/analysis/standard/StandardAnalyzer.html).


*   __<xref:Lucene.Net.Codecs>__
provides an abstraction over the encoding and decoding of the inverted index structure,
as well as different implementations that can be chosen depending upon application needs.


*   __<xref:Lucene.Net.Documents>__
provides a simple [Document](xref:Lucene.Net.Documents.Document)
class.  A Document is simply a set of named [Field](xref:Lucene.Net.Documents.Field)s,
whose values may be strings or instances of {@link java.io.Reader}.


*   __<xref:Lucene.Net.Index>__
provides two primary classes: [IndexWriter](xref:Lucene.Net.Index.IndexWriter),
which creates and adds documents to indices; and <xref:Lucene.Net.Index.IndexReader>,
which accesses the data in the index.


*   __<xref:Lucene.Net.Search>__
provides data structures to represent queries (ie [TermQuery](xref:Lucene.Net.Search.TermQuery)
for individual words, [PhraseQuery](xref:Lucene.Net.Search.PhraseQuery) 
for phrases, and [BooleanQuery](xref:Lucene.Net.Search.BooleanQuery) 
for boolean combinations of queries) and the [IndexSearcher](xref:Lucene.Net.Search.IndexSearcher)
which turns queries into [TopDocs](xref:Lucene.Net.Search.TopDocs).
A number of [QueryParser](../queryparser/overview-summary.html)s are provided for producing
query structures from strings or xml.


*   __<xref:Lucene.Net.Store>__
defines an abstract class for storing persistent data, the [Directory](xref:Lucene.Net.Store.Directory),
which is a collection of named files written by an [IndexOutput](xref:Lucene.Net.Store.IndexOutput)
and read by an [IndexInput](xref:Lucene.Net.Store.IndexInput). 
Multiple implementations are provided, including [FSDirectory](xref:Lucene.Net.Store.FSDirectory),
which uses a file system directory to store files, and [RAMDirectory](xref:Lucene.Net.Store.RAMDirectory)
which implements files as memory-resident data structures.


*   __<xref:Lucene.Net.Util>__
contains a few handy data structures and util classes, ie [OpenBitSet](xref:Lucene.Net.Util.OpenBitSet)
and [PriorityQueue](xref:Lucene.Net.Util.PriorityQueue).

To use Lucene, an application should:

1.  Create [Document](xref:Lucene.Net.Documents.Document)s by
adding
[Field](xref:Lucene.Net.Documents.Field)s;


2.  Create an [IndexWriter](xref:Lucene.Net.Index.IndexWriter)
and add documents to it with [AddDocument](xref:Lucene.Net.Index.IndexWriter#methods);


3.  Call [QueryParser.parse()](../queryparser/org/apache/lucene/queryparser/classic/QueryParserBase.html#parse(java.lang.String))
to build a query from a string; and


4.  Create an [IndexSearcher](xref:Lucene.Net.Search.IndexSearcher)
and pass the query to its [Search](xref:Lucene.Net.Search.IndexSearcher#methods)
method.

Some simple examples of code which does this are:

*    [IndexFiles.java](../demo/src-html/org/apache/lucene/demo/IndexFiles.html) creates an
index for all the files contained in a directory.


*    [SearchFiles.java](../demo/src-html/org/apache/lucene/demo/SearchFiles.html) prompts for
queries and searches an index.
