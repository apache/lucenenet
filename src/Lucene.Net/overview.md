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
uid: Lucene.Net
summary: *content
---

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

*   **[](xref:Lucene.Net.Analysis)**
defines an abstract [](xref:Lucene.Net.Analysis.Analyzer Analyzer)
API for converting text from a {@link java.io.Reader}
into a [](xref:Lucene.Net.Analysis.TokenStream TokenStream),
an enumeration of token [](xref:Lucene.Net.Util.Attribute Attribute)s. 
A TokenStream can be composed by applying [](xref:Lucene.Net.Analysis.TokenFilter TokenFilter)s
to the output of a [](xref:Lucene.Net.Analysis.Tokenizer Tokenizer). 
Tokenizers and TokenFilters are strung together and applied with an [](xref:Lucene.Net.Analysis.Analyzer Analyzer). 
[analyzers-common](../analyzers-common/overview-summary.html) provides a number of Analyzer implementations, including 
[StopAnalyzer](../analyzers-common/org/apache/lucene/analysis/core/StopAnalyzer.html)
and the grammar-based [StandardAnalyzer](../analyzers-common/org/apache/lucene/analysis/standard/StandardAnalyzer.html).
*   **[](xref:Lucene.Net.Codecs)**
provides an abstraction over the encoding and decoding of the inverted index structure,
as well as different implementations that can be chosen depending upon application needs.

    **[](xref:Lucene.Net.Documents)**
provides a simple [](xref:Lucene.Net.Documents.Document Document)
class.  A Document is simply a set of named [](xref:Lucene.Net.Documents.Field Field)s,
whose values may be strings or instances of {@link java.io.Reader}.
*   **[](xref:Lucene.Net.Index)**
provides two primary classes: [](xref:Lucene.Net.Index.IndexWriter IndexWriter),
which creates and adds documents to indices; and [](xref:Lucene.Net.Index.IndexReader),
which accesses the data in the index.
*   **[](xref:Lucene.Net.Search)**
provides data structures to represent queries (ie [](xref:Lucene.Net.Search.TermQuery TermQuery)
for individual words, [](xref:Lucene.Net.Search.PhraseQuery PhraseQuery) 
for phrases, and [](xref:Lucene.Net.Search.BooleanQuery BooleanQuery) 
for boolean combinations of queries) and the [](xref:Lucene.Net.Search.IndexSearcher IndexSearcher)
which turns queries into [](xref:Lucene.Net.Search.TopDocs TopDocs).
A number of [QueryParser](../queryparser/overview-summary.html)s are provided for producing
query structures from strings or xml.

    **[](xref:Lucene.Net.Store)**
defines an abstract class for storing persistent data, the [](xref:Lucene.Net.Store.Directory Directory),
which is a collection of named files written by an [](xref:Lucene.Net.Store.IndexOutput IndexOutput)
and read by an [](xref:Lucene.Net.Store.IndexInput IndexInput). 
Multiple implementations are provided, including [](xref:Lucene.Net.Store.FSDirectory FSDirectory),
which uses a file system directory to store files, and [](xref:Lucene.Net.Store.RAMDirectory RAMDirectory)
which implements files as memory-resident data structures.
*   **[](xref:Lucene.Net.Util)**
contains a few handy data structures and util classes, ie [](xref:Lucene.Net.Util.OpenBitSet OpenBitSet)
and [](xref:Lucene.Net.Util.PriorityQueue PriorityQueue).
To use Lucene, an application should:

1.  Create [](xref:Lucene.Net.Documents.Document Document)s by
adding
[](xref:Lucene.Net.Documents.Field Field)s;
2.  Create an [](xref:Lucene.Net.Index.IndexWriter IndexWriter)
and add documents to it with [](xref:Lucene.Net.Index.IndexWriter.AddDocument(Iterable) addDocument());
3.  Call [QueryParser.parse()](../queryparser/org/apache/lucene/queryparser/classic/QueryParserBase.html#parse(java.lang.String))
to build a query from a string; and
4.  Create an [](xref:Lucene.Net.Search.IndexSearcher IndexSearcher)
and pass the query to its [](xref:Lucene.Net.Search.IndexSearcher.Search(Lucene.Net.Search.Query, int) search())
method.
Some simple examples of code which does this are:

*    [IndexFiles.java](../demo/src-html/org/apache/lucene/demo/IndexFiles.html) creates an
index for all the files contained in a directory.
*    [SearchFiles.java](../demo/src-html/org/apache/lucene/demo/SearchFiles.html) prompts for
queries and searches an index.
To demonstrate these, try something like:

> <tt>> **java -cp lucene-core.jar:lucene-demo.jar:lucene-analyzers-common.jar org.apache.lucene.demo.IndexFiles -index index -docs rec.food.recipes/soups**</tt>
> 
> <tt>adding rec.food.recipes/soups/abalone-chowder</tt>
> 
> <tt>  </tt>[ ... ]
> 
> <tt>> **java -cp lucene-core.jar:lucene-demo.jar:lucene-queryparser.jar:lucene-analyzers-common.jar org.apache.lucene.demo.SearchFiles**</tt>
> 
> <tt>Query: **chowder**</tt>
> 
> <tt>Searching for: chowder</tt>
> 
> <tt>34 total matching documents</tt>
> 
> <tt>1. rec.food.recipes/soups/spam-chowder</tt>
> 
> <tt>  </tt>[ ... thirty-four documents contain the word "chowder" ... ]
> 
> <tt>Query: **"clam chowder" AND Manhattan**</tt>
> 
> <tt>Searching for: +"clam chowder" +manhattan</tt>
> 
> <tt>2 total matching documents</tt>
> 
> <tt>1. rec.food.recipes/soups/clam-chowder</tt>
> 
> <tt>  </tt>[ ... two documents contain the phrase "clam chowder"
> and the word "manhattan" ... ]
> 
>     [ Note: "+" and "-" are canonical, but "AND", "OR"
> and "NOT" may be used. ]