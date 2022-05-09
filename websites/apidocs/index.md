---
title: Lucene.Net Docs - The documentation website for Lucene.NET
description: The documentation website for Lucene.NET
---

Apache Lucene.Net <EnvVar:LuceneNetVersion> Documentation
===============

---------------

Lucene.NET is a _.NET full-text search engine_. Lucene.NET is not a complete application, 
but rather a code library and API that can easily be used to add search capabilities
to applications.

This is the official API documentation for __Apache Lucene.NET <EnvVar:LuceneNetVersion>__.

## Getting Started

The following section is intended as a "getting started" guide. It has three
audiences: first-time users looking to install Apache Lucene in their
application; developers looking to modify or base the applications they develop
on Lucene; and developers looking to become involved in and contribute to the
development of Lucene. The goal is to help you "get started". It does not go into great depth
on some of the conceptual or inner details of Lucene:

- [Lucene demo, its usage, and sources](xref:Lucene.Net.Demo): Tutorial and walk-through of the command-line Lucene demo.
- [Introduction to Lucene's APIs](xref:Lucene.Net): High-level summary of the different Lucene packages.
- [Analysis overview](xref:Lucene.Net.Analysis): Introduction to Lucene's analysis API. See also the [TokenStream consumer workflow](xref:Lucene.Net.Analysis.TokenStream).

## Reference Documents

- [Changes](https://github.com/apache/lucenenet/releases/tag/[EnvVar:LuceneNetReleaseTag]): List of changes in this release.
<!-- - System Requirements: Minimum and supported .NET versions. LUCENENT TODO: Add link -->
- [Migration Guide](xref:Lucene.Net.Migration.Guide): What changed in Lucene 4; how to migrate code from Lucene 3.x.
- [Source Stepping](xref:source-stepping): How to use the Visual Studio debugger to step into Lucene.NET source code.
- [File Formats](xref:Lucene.Net.Codecs.Lucene46): Guide to the supported index format used by Lucene. This can be customized by using [an alternate codec](xref:Lucene.Net.Codecs).
- [Search and Scoring in Lucene](xref:Lucene.Net.Search): Introduction to how Lucene scores documents.
- [Classic Scoring Formula](xref:Lucene.Net.Search.Similarities.TFIDFSimilarity): Formula of Lucene's classic [Vector Space](http://en.wikipedia.org/wiki/Vector_Space_Model) implementation. (look [here](xref:Lucene.Net.Search.Similarities) for other models)
- [Classic QueryParser Syntax](xref:Lucene.Net.QueryParsers.Classic): Overview of the Classic QueryParser's syntax and features.

## Libraries

- <xref:Lucene.Net> - Core library
- <xref:Lucene.Net.Analysis.Common> - Analyzers for indexing content in different languages and domains
- [Lucene.Net.Analysis.Kuromoji](xref:Lucene.Net.Analysis.Ja) - Japanese Morphological Analyzer
- <xref:Lucene.Net.Analysis.Morfologik> - Analyzer for dictionary stemming, built-in Polish dictionary
- [Lucene.Net.Analysis.OpenNLP](xref:Lucene.Net.Analysis.OpenNlp) - OpenNLP Library Integration
- <xref:Lucene.Net.Analysis.Phonetic> - Analyzer for indexing phonetic signatures (for sounds-alike search)
- [Lucene.Net.Analysis.SmartCn](xref:Lucene.Net.Analysis.Cn.Smart) - Analyzer for indexing Chinese
- <xref:Lucene.Net.Analysis.Stempel> - Analyzer for indexing Polish
- [Lucene.Net.Benchmark](xref:Lucene.Net.Benchmarks) - System for benchmarking Lucene
- <xref:Lucene.Net.Classification> - Classification module for Lucene
- [Lucene.Net.Codecs](api/codecs/overview.html) - Lucene codecs and postings formats
- [Lucene.Net.Expressions](xref:Lucene.Net.Expressions) - Dynamically computed values to sort/facet/search on based on a pluggable grammar
- [Lucene.Net.Facet](xref:Lucene.Net.Facet) - Faceted indexing and search capabilities
- <xref:Lucene.Net.Grouping> - Collectors for grouping search results
- <xref:Lucene.Net.Highlighter> - Highlights search keywords in results
- <xref:Lucene.Net.ICU> - Specialized ICU (International Components for Unicode) Analyzers and Highlighters
- <xref:Lucene.Net.Join> - Index-time and Query-time joins for normalized content
- [Lucene.Net.Memory](xref:Lucene.Net.Index.Memory) - Single-document in-memory index implementation
- <xref:Lucene.Net.Misc> - Index tools and other miscellaneous code
- <xref:Lucene.Net.Queries> - Filters and Queries that add to core Lucene
- <xref:Lucene.Net.QueryParser> - Text to Query parsers and parsing framework
- <xref:Lucene.Net.Replicator> - Files replication utility
- <xref:Lucene.Net.Sandbox> - Various third party contributions and new ideas
- [Lucene.Net.Spatial](xref:Lucene.Net.Spatial) - Geospatial search
- <xref:Lucene.Net.Suggest> - Auto-suggest and Spell-checking support
- <xref:Lucene.Net.TestFramework> - Framework for testing Lucene-based applications

### Tools

- [Lucene CLI](cli/index.html): Dotnet tool to work with Lucene indexes from the command line
- [Demo](xref:Lucene.Net.Demo): Simple example code
