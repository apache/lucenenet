Lucene.NET is a full-text search engine library capable of advanced text analysis, indexing, and searching. It can be used to easily add search capabilities to applications. Lucene.NET is a C# port of the popular Java Lucene search engine framework from The Apache Software Foundation, targeting the .NET platform.

## Quick Start

### Prerequisites

- [Lucene.Net](https://www.nuget.org/packages/Lucene.Net/absoluteLatest)
- [Lucene.Net.Analysis.Common](https://www.nuget.org/packages/Lucene.Net.Analysis.Common/absoluteLatest)

#### Imports

```c#
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
```

### Create an Index and Define a Text Analyzer

```c#
// Ensures index backward compatibility
const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;

// Construct a machine-independent path for the index
var basePath = Environment.GetFolderPath(
    Environment.SpecialFolder.CommonApplicationData);
var indexPath = Path.Combine(basePath, "index");

using var dir = FSDirectory.Open(indexPath);

// Create an analyzer to process the text
var analyzer = new StandardAnalyzer(AppLuceneVersion);

// Create an index writer
var indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer);
using var writer = new IndexWriter(dir, indexConfig);
```

### Add to the Index

```c#
var source = new
{
    Name = "Kermit the Frog",
    FavoritePhrase = "The quick brown fox jumps over the lazy dog"
};
var doc = new Document
{
    // StringField indexes but doesn't tokenize
    new StringField("name",
        source.Name,
        Field.Store.YES),
    new TextField("favoritePhrase",
        source.FavoritePhrase,
        Field.Store.YES)
};

writer.AddDocument(doc);
writer.Flush(triggerMerge: false, applyAllDeletes: false);
```

### Construct a Query

```c#
// Search with a phrase
var phrase = new MultiPhraseQuery
{
    new Term("favoritePhrase", "brown"),
    new Term("favoritePhrase", "fox")
};
```

### Fetch the Results

```c#
// Re-use the writer to get real-time updates
using var reader = writer.GetReader(applyAllDeletes: true);
var searcher = new IndexSearcher(reader);
var hits = searcher.Search(phrase, 20 /* top 20 */).ScoreDocs;

// Display the output in a table
Console.WriteLine($"{"Score",10}" +
    $" {"Name",-15}" +
    $" {"Favorite Phrase",-40}");
foreach (var hit in hits)
{
    var foundDoc = searcher.Doc(hit.Doc);
    Console.WriteLine($"{hit.Score:f8}" +
        $" {foundDoc.Get("name"),-15}" +
        $" {foundDoc.Get("favoritePhrase"),-40}");
}
```

## Documentation

See the [API documentation for Lucene.NET 4.8.0](https://lucenenet.apache.org/docs/4.8.0-beta00015/).

## All Packages

- [Lucene.Net](https://www.nuget.org/packages/Lucene.Net/absoluteLatest) - Core library
- [Lucene.Net.Analysis.Common](https://www.nuget.org/packages/Lucene.Net.Analysis.Common/absoluteLatest) - Analyzers for indexing content in different languages and domains
- [Lucene.Net.Analysis.Kuromoji](https://www.nuget.org/packages/Lucene.Net.Analysis.Kuromoji/absoluteLatest) - Japanese Morphological Analyzer
- [Lucene.Net.Analysis.Morfologik](https://www.nuget.org/packages/Lucene.Net.Analysis.Morfologik/absoluteLatest) - Analyzer for dictionary stemming, built-in Polish dictionary
- [Lucene.Net.Analysis.OpenNLP](https://www.nuget.org/packages/Lucene.Net.Analysis.OpenNLP/absoluteLatest) - OpenNLP Library Integration
- [Lucene.Net.Analysis.Phonetic](https://www.nuget.org/packages/Lucene.Net.Analysis.Phonetic/absoluteLatest) - Analyzer for indexing phonetic signatures (for sounds-alike search)
- [Lucene.Net.Analysis.SmartCn](https://www.nuget.org/packages/Lucene.Net.Analysis.SmartCn/absoluteLatest) - Analyzer for indexing Chinese
- [Lucene.Net.Analysis.Stempel](https://www.nuget.org/packages/Lucene.Net.Analysis.Stempel/absoluteLatest) - Analyzer for indexing Polish
- [Lucene.Net.Benchmark](https://www.nuget.org/packages/Lucene.Net.Benchmark/) - System for benchmarking Lucene
- [Lucene.Net.Classification](https://www.nuget.org/packages/Lucene.Net.Classification/absoluteLatest) - Classification module for Lucene
- [Lucene.Net.Codecs](https://www.nuget.org/packages/Lucene.Net.Codecs/absoluteLatest) - Lucene codecs and postings formats
- [Lucene.Net.Expressions](https://www.nuget.org/packages/Lucene.Net.Expressions/absoluteLatest) - Dynamically computed values to sort/facet/search on based on a pluggable grammar
- [Lucene.Net.Facet](https://www.nuget.org/packages/Lucene.Net.Facet/absoluteLatest) - Faceted indexing and search capabilities
- [Lucene.Net.Grouping](https://www.nuget.org/packages/Lucene.Net.Grouping/absoluteLatest) - Collectors for grouping search results
- [Lucene.Net.Highlighter](https://www.nuget.org/packages/Lucene.Net.Highlighter/absoluteLatest) - Highlights search keywords in results
- [Lucene.Net.ICU](https://www.nuget.org/packages/Lucene.Net.ICU/absoluteLatest) - Specialized ICU (International Components for Unicode) Analyzers and Highlighters
- [Lucene.Net.Join](https://www.nuget.org/packages/Lucene.Net.Join/absoluteLatest) - Index-time and Query-time joins for normalized content
- [Lucene.Net.Memory](https://www.nuget.org/packages/Lucene.Net.Memory/absoluteLatest) - Single-document in-memory index implementation
- [Lucene.Net.Misc](https://www.nuget.org/packages/Lucene.Net.Misc/absoluteLatest) - Index tools and other miscellaneous code
- [Lucene.Net.Queries](https://www.nuget.org/packages/Lucene.Net.Queries/absoluteLatest) - Filters and Queries that add to core Lucene
- [Lucene.Net.QueryParser](https://www.nuget.org/packages/Lucene.Net.QueryParser/absoluteLatest) - Text to Query parsers and parsing framework
- [Lucene.Net.Replicator](https://www.nuget.org/packages/Lucene.Net.Replicator/absoluteLatest)  Files replication utility
- [Lucene.Net.Sandbox](https://www.nuget.org/packages/Lucene.Net.Sandbox/absoluteLatest) - Various third party contributions and new ideas
- [Lucene.Net.Spatial](https://www.nuget.org/packages/Lucene.Net.Spatial/absoluteLatest) - Geospatial search
- [Lucene.Net.Suggest](https://www.nuget.org/packages/Lucene.Net.Suggest/absoluteLatest) - Auto-suggest and Spell-checking support
- [Lucene.Net.TestFramework](https://www.nuget.org/packages/Lucene.Net.TestFramework/absoluteLatest) - Framework for testing Lucene-based applications

## Demos & Tools

There are several demos implemented as simple console applications that can be copied and pasted into Visual Studio or compiled on the command line in the [Lucene.Net.Demo project](https://github.com/apache/lucenenet/tree/master/src/Lucene.Net.Demo).

There is also a dotnet command line tool available on NuGet. It contains all of the demos as well as tools maintaining your Lucene.NET index, featuring operations such as splitting, merging, listing segment info, fixing, deleting segments, upgrading, etc. Always be sure to back up your index before running any commands against it!

- [Prerequisite: .NET Core 3.1 Runtime or Higher](https://dotnet.microsoft.com/en-us/download/dotnet)

```
dotnet tool install lucene-cli -g --version 4.8.0-beta00015
```

> NOTE: The version of the CLI you install should match the version of Lucene.NET you use.

Once installed, you can explore the commands and options that are available by entering the command `lucene`.

[lucene-cli Documentation](https://lucenenet.apache.org/docs/4.8.0-beta00015/cli/)