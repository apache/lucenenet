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

Apache Lucene.NET is a high-performance, full-featured text search engine library. Here's a simple example how to use Lucene.NET for indexing and searching (using NUnit to check if the results are what we expect):

```cs
Analyzer analyzer = new StandardAnalyzer(Version.LUCENE_CURRENT);

// Store the index in memory:
Directory directory = new RAMDirectory();
// To store an index on disk, use this instead:
// Construct a machine-independent path for the index
//var basePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
//var indexPath = Path.Combine(basePath, "index");
//Directory directory = FSDirectory.Open(indexPath);
IndexWriterConfig config = new IndexWriterConfig(LuceneVersion.LUCENE_CURRENT, analyzer);
using IndexWriter iwriter = new IndexWriter(directory, config);
Document doc = new Document();
String text = "This is the text to be indexed.";
doc.Add(new Field("fieldname", text, TextField.TYPE_STORED));
iwriter.AddDocument(doc);
iwriter.Dispose();

// Now search the index:
using DirectoryReader ireader = DirectoryReader.Open(directory);
IndexSearcher isearcher = new IndexSearcher(ireader);
// Parse a simple query that searches for "text":
QueryParser parser = new QueryParser(LuceneVersion.LUCENE_CURRENT, "fieldname", analyzer);
Query query = parser.Parse("text");
ScoreDoc[] hits = isearcher.Search(query, null, 1000).ScoreDocs;
Assert.AreEqual(1, hits.Length);
// Iterate through the results:
for (int i = 0; i < hits.Length; i++)
{
	Document hitDoc = isearcher.Doc(hits[i].Doc);
	Assert.AreEqual("This is the text to be indexed.", hitDoc.Get("fieldname"));
}
```

The Lucene API is divided into several packages:

*   __<xref:Lucene.Net.Analysis>__
defines an abstract [Analyzer](xref:Lucene.Net.Analysis.Analyzer)
API for converting text from a [System.Text.TextReader](https://docs.microsoft.com/en-us/dotnet/api/system.io.textreader)
into a [TokenStream](xref:Lucene.Net.Analysis.TokenStream),
an enumeration of token [Attribute](xref:Lucene.Net.Util.Attribute)s. 
A TokenStream can be composed by applying [TokenFilter](xref:Lucene.Net.Analysis.TokenFilter)s
to the output of a [Tokenizer](xref:Lucene.Net.Analysis.Tokenizer). 
Tokenizers and TokenFilters are strung together and applied with an [Analyzer](xref:Lucene.Net.Analysis.Analyzer). 
[Lucene.Net.Analysis.Common](../analysis-common/overview.html) provides a number of Analyzer implementations, including 
[StopAnalyzer](../analysis-common/Lucene.Net.Analysis.Core.StopAnalyzer.html)
and the grammar-based [StandardAnalyzer](../analysis-common/Lucene.Net.Analysis.Standard.StandardAnalyzer.html).

*   __<xref:Lucene.Net.Codecs>__
provides an abstraction over the encoding and decoding of the inverted index structure,
as well as different implementations that can be chosen depending upon application needs.

*   __<xref:Lucene.Net.Documents>__
provides a simple [Document](xref:Lucene.Net.Documents.Document)
class.  A Document is simply a set of named [Field](xref:Lucene.Net.Documents.Field)s,
whose values may be strings or instances of [System.Text.TextReader](https://docs.microsoft.com/en-us/dotnet/api/system.io.textreader).

*   __<xref:Lucene.Net.Index>__
provides two primary classes: [IndexWriter](xref:Lucene.Net.Index.IndexWriter),
which creates and adds documents to indices; and [IndexReader](xref:Lucene.Net.Index.IndexReader),
which accesses the data in the index.

*   __<xref:Lucene.Net.Search>__
provides data structures to represent queries (ie [TermQuery](xref:Lucene.Net.Search.TermQuery)
for individual words, [PhraseQuery](xref:Lucene.Net.Search.PhraseQuery) 
for phrases, and [BooleanQuery](xref:Lucene.Net.Search.BooleanQuery) 
for boolean combinations of queries) and the [IndexSearcher](xref:Lucene.Net.Search.IndexSearcher)
which turns queries into [TopDocs](xref:Lucene.Net.Search.TopDocs).
A number of [QueryParser](../queryparser/overview.html)s are provided for producing
query structures from strings or XML.

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
adding [Field](xref:Lucene.Net.Documents.Field)s;

2.  Create an [IndexWriter](xref:Lucene.Net.Index.IndexWriter)
and add documents to it with [AddDocument()](xref:Lucene.Net.Index.IndexWriter#Lucene_Net_Index_IndexWriter_AddDocument_System_Collections_Generic_IEnumerable_Lucene_Net_Index_IIndexableField__Lucene_Net_Analysis_Analyzer_);

3.  Call [QueryParser.Parse()](../queryparser/Lucene.Net.QueryParsers.Classic.QueryParserBase.html#Lucene_Net_QueryParsers_Classic_QueryParserBase_Parse_System_String_)
to build a query from a string; and

4.  Create an [IndexSearcher](xref:Lucene.Net.Search.IndexSearcher)
and pass the query to its [Search()](xref:Lucene.Net.Search.IndexSearcher#Lucene_Net_Search_IndexSearcher_Search_Lucene_Net_Search_Query_System_Int32_)
method.

Some simple examples of code which does this are:

*    [IndexFiles.cs](../demo/Lucene.Net.Demo.IndexFiles.html) creates an
index for all the files contained in a directory.

*    [SearchFiles.cs](../demo/Lucene.Net.Demo.SearchFiles.html) prompts for
queries and searches an index.

> [!TIP]
> These demos can be run and code viewed/exported using the [lucene-cli](../../cli/index.html) dotnet tool.

To demonstrate this, try something like:

```console
> dotnet demo index-files index rec.food.recipies/soups
adding rec.food.recipes/soups/abalone-chowder
[...]

> dotnet demo search-files index
Query: chowder
Searching for: chowder
34 total matching documents
1. rec.food.recipes/soups/spam-chowder
  [ ... thirty-four documents contain the word "chowder" ... ]

Query: "clam chowder" AND Manhattan
Searching for: +"clam chowder" +manhattan
2 total matching documents
1. rec.food.recipes/soups/clam-chowder
  [ ... two documents contain the phrase "clam chowder" and the word "manhattan" ... ]
  [ Note: "+" and "-" are canonical, but "AND", "OR" and "NOT" may be used. ]
```

