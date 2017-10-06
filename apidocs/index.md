Apache Lucene.NET&trade; 4.8.0 Documentation
===============

---------------


<div>
<a href="https://lucenenet.apache.org/">
    <img src="https://raw.githubusercontent.com/apache/lucenenet/master/branding/logo/lucene-net-icon-128.png" title="Apache.NET Lucene Logo" alt="Lucene.NET">
</a>
<br/>
</div>

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
* [System Requirements](SYSTEM_REQUIREMENTS.html): Minimum and supported .NET versions.
* [Migration Guide](MIGRATE.html): What changed in Lucene 4; how to migrate code from Lucene 3.x.
* [File Formats](xref:Lucene.​Net.​Codecs.​Lucene46): Guide to the supported index format used by Lucene.  This can be customized by using [an alternate codec](xref:Lucene.Net.Codecs).
* [Search and Scoring in Lucene](xref:Lucene.Net.Search): Introduction to how Lucene scores documents.
* [Classic Scoring Formula](xref:Lucene.Net.Search.Similarities.TFIDFSimilarity): Formula of Lucene's classic [Vector Space](http://en.wikipedia.org/wiki/Vector_Space_Model) implementation. (look [here](xref:Lucene.Net.Search.Similarities) for other models)
* [Classic QueryParser Syntax](xref:Lucene.Net.QueryParsers.Classic): Overview of the Classic QueryParser's syntax and features.