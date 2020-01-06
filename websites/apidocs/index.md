---
title: Lucene.Net Docs - The documentation website for Lucene.Net
description: The documentation website for Lucene.Net
---

Apache Lucene.Net 4.8.0 Documentation
===============

---------------

Lucene is a .NET full-text search engine. Lucene.NET is not a complete application, 
but rather a code library and API that can easily be used to add search capabilities
to applications.

This is the official API documentation for <b>Apache Lucene.NET 4.8.0</b>.

## Getting Started

The following section is intended as a "getting started" guide. It has three
audiences: first-time users looking to install Apache Lucene in their
application; developers looking to modify or base the applications they develop
on Lucene; and developers looking to become involved in and contribute to the
development of Lucene. The goal is to help you "get started". It does not go into great depth
on some of the conceptual or inner details of Lucene:

* [Lucene demo, its usage, and sources](xref:Lucene.Net.Demo): Tutorial and walk-through of the command-line Lucene demo.
* [Introduction to Lucene's APIs](xref:Lucene.Net): High-level summary of the different Lucene packages.
* [Analysis overview](xref:Lucene.Net.Analysis): Introduction to Lucene's analysis API. See also the [TokenStream consumer workflow](xref:Lucene.Net.Analysis.TokenStream).

## Reference Documents

* [Changes](https://github.com/apache/lucenenet/releases/tag/Lucene.Net_4_8_0): List of changes in this release.
* System Requirements: Minimum and supported .NET versions. __TODO: Add link__
* Migration Guide: What changed in Lucene 4; how to migrate code from Lucene 3.x. __TODO: Add link__
* [File Formats](xref:Lucene.Net.Codecs.Lucene46) : Guide to the supported index format used by Lucene.  This can be customized by using [an alternate codec](xref:Lucene.Net.Codecs).
* [Search and Scoring in Lucene](xref:Lucene.Net.Search): Introduction to how Lucene scores documents.
* [Classic Scoring Formula](xref:Lucene.Net.Search.Similarities.TFIDFSimilarity): Formula of Lucene's classic [Vector Space](http://en.wikipedia.org/wiki/Vector_Space_Model) implementation. (look [here](xref:Lucene.Net.Search.Similarities) for other models)
* [Classic QueryParser Syntax](xref:Lucene.Net.QueryParsers.Classic): Overview of the Classic QueryParser's syntax and features.

## Libraries

* [Lucene.Net](xref:Lucene.Net) - Core library
* [Lucene.Net.Analysis.Common](xref:Lucene.Net.Analysis.Common) - Analyzers for indexing content in different languages and domains
* [Lucene.Net.Analysis.Kuromoji](xref:Lucene.Net.Analysis.Ja) - Japanese Morphological Analyzer
* [Lucene.Net.Analysis.Morfologik](xref:Lucene.Net.Analysis.Uk) - Analyzer for dictionary stemming, built-in Polish dictionary
* [Lucene.Net.Analysis.OpenNLP](xref:Lucene.Net.Analysis.OpenNlp) - OpenNLP Library Integration
* [Lucene.Net.Analysis.Phonetic](xref:Lucene.Net.Analysis.Phonetic) - Analyzer for indexing phonetic signatures (for sounds-alike search)
* [Lucene.Net.Analysis.SmartCn](xref:Lucene.Net.Analysis.Cn.Smart) - Analyzer for indexing Chinese
* [Lucene.Net.Analysis.Stempel](xref:Lucene.Net.Analysis.Stempel) - Analyzer for indexing Polish
* [Lucene.Net.Benchmark](xref:Lucene.Net.Benchmarks) - System for benchmarking Lucene
* [Lucene.Net.Classification](xref:Lucene.Net.Classification) - Classification module for Lucene
* [Lucene.Net.Codecs](xref:Lucene.Net.Codecs) - Lucene codecs and postings formats
* [Lucene.Net.Expressions](xref:Lucene.Net.Expressions) - Dynamically computed values to sort/facet/search on based on a pluggable grammar
* [Lucene.Net.Facet](xref:Lucene.Net.Facet) - Faceted indexing and search capabilities
* [Lucene.Net.Grouping](xref:Lucene.Net.Search.Grouping) - Collectors for grouping search results
* [Lucene.Net.Highlighter](xref:Lucene.Net.Search.Highlight) - Highlights search keywords in results
* [Lucene.Net.ICU](xref:Lucene.Net.Analysis.Icu) - Specialized ICU (International Components for Unicode) Analyzers and Highlighters
* [Lucene.Net.Join](xref:Lucene.Net.Join) - Index-time and Query-time joins for normalized content
* [Lucene.Net.Memory](xref:Lucene.Net.Index.Memory) - Single-document in-memory index implementation
* [Lucene.Net.Misc](xref:Lucene.Net.Misc) - Index tools and other miscellaneous code
* [Lucene.Net.Queries](xref:Lucene.Net.Queries) - Filters and Queries that add to core Lucene
* [Lucene.Net.QueryParser](xref:Lucene.Net.QueryParsers.Classic) - Text to Query parsers and parsing framework
* [Lucene.Net.Replicator](xref:Lucene.Net.Replicator)  Files replication utility
* [Lucene.Net.Sandbox](xref:Lucene.Net.Sandbox) - Various third party contributions and new ideas
* [Lucene.Net.Spatial](xref:Lucene.Net.Spatial) - Geospatial search
* [Lucene.Net.Suggest](xref:Lucene.Net.Search.Suggest) - Auto-suggest and Spell-checking support
* [Lucene.Net.TestFramework](xref:Lucene.Net.Testframework) - Framework for testing Lucene-based applications

### Tools

* [Lucene CLI](cli/index.html): Dotnet tool to work with Lucene indexes from the command line
* [Demo](xref:Lucene.Net.Demo): Simple example code
