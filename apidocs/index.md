Apache Lucene.NET&trade; 4.8.0 Documentation
===============

---------------
<div>
<a href="https://lucenenet.apache.org/">
    <img src="https://raw.githubusercontent.com/apache/lucenenet/master/branding/logo/lucene-net-icon-128.png" title="Apache.NET Lucene Logo" alt="Lucene.NET">
</a>
</div>

<p>Lucene is a .NET full-text search engine. Lucene.NET is not a complete application, 
        but rather a code library and API that can easily be used to add search capabilities
        to applications.</p>
<p>
This is the official documentation for <b>Apache Lucene.NET 4.8.0</b>. Additional documentation is available <a href="docs/index.html">here</a>.
</p>

## Getting Started

<p>The following section is intended as a "getting started" guide. It has three
        audiences: first-time users looking to install Apache Lucene in their
        application; developers looking to modify or base the applications they develop
        on Lucene; and developers looking to become involved in and contribute to the
        development of Lucene. The goal is to help you "get started". It does not go into great depth
        on some of the conceptual or inner details of Lucene:</p>
<ul>
<li>
<a href="demo/overview-summary.html#overview_description">Lucene demo, its usage, and sources</a>:
        Tutorial and walk-through of the command-line Lucene demo.</li>
<li>
<a href="core/overview-summary.html#overview_description">Introduction to Lucene's APIs</a>:
        High-level summary of the different Lucene packages. </li>
<li>
<a href="core/org/apache/lucene/analysis/package-summary.html#package_description">Analysis overview</a>:
        Introduction to Lucene's analysis API.  See also the
        <a href="core/org/apache/lucene/analysis/TokenStream.html">TokenStream consumer workflow</a>.</li>
</ul>

## Reference Documents

<ul>
<li>
<a href="changes/Changes.html">Changes</a>: List of changes in this release.</li>
<li>
<a href="SYSTEM_REQUIREMENTS.html">System Requirements</a>: Minimum and supported Java versions.</li>
<li>
<a href="MIGRATE.html">Migration Guide</a>: What changed in Lucene 4; how to migrate code from Lucene 3.x.</li>
<li>
<a href="JRE_VERSION_MIGRATION.html">JRE Version Migration</a>: Information about upgrading between major JRE versions.</li>
<li>
<a href="core/org/apache/lucene/codecs/lucene46/package-summary.html#package_description">File Formats</a>: Guide to the supported index format used by Lucene.  This can be customized by using <a href="core/org/apache/lucene/codecs/package-summary.html#package_description">an alternate codec</a>.</li>
<li>
<a href="core/org/apache/lucene/search/package-summary.html#package_description">Search and Scoring in Lucene</a>: Introduction to how Lucene scores documents.</li>
<li>
<a href="core/org/apache/lucene/search/similarities/TFIDFSimilarity.html">Classic Scoring Formula</a>: Formula of Lucene's classic <a href="http://en.wikipedia.org/wiki/Vector_Space_Model">Vector Space</a> implementation. (look <a href="core/org/apache/lucene/search/similarities/package-summary.html#package_description">here</a> for other models)</li>
<li>
<a href="queryparser/org/apache/lucene/queryparser/classic/package-summary.html#package_description">Classic QueryParser Syntax</a>: Overview of the Classic QueryParser's syntax and features.</li>
</ul>

## API .NET Docs

<ul>
<li style="font-size:larger; margin-bottom:.5em;">
<b><a href="api/index.html">core</a>: </b>Lucene core library</li>
<li>
<b><a href="analyzers-common/index.html">analyzers-common</a>: </b>Analyzers for indexing content in different languages and domains.</li>
<li>
<b><a href="analyzers-icu/index.html">analyzers-icu</a>: </b>Analysis integration with ICU (International Components for Unicode).</li>
<li>
<b><a href="analyzers-kuromoji/index.html">analyzers-kuromoji</a>: </b>Japanese Morphological Analyzer</li>
<li>
<b><a href="analyzers-morfologik/index.html">analyzers-morfologik</a>: </b>Analyzer for indexing Polish</li>
<li>
<b><a href="analyzers-phonetic/index.html">analyzers-phonetic</a>: </b>Analyzer for indexing phonetic signatures (for sounds-alike search)</li>
<li>
<b><a href="analyzers-smartcn/index.html">analyzers-smartcn</a>: </b>Analyzer for indexing Chinese</li>
<li>
<b><a href="analyzers-stempel/index.html">analyzers-stempel</a>: </b>Analyzer for indexing Polish</li>
<li>
<b><a href="analyzers-uima/index.html">analyzers-uima</a>: </b>Analysis integration with Apache UIMA</li>
<li>
<b><a href="benchmark/index.html">benchmark</a>: </b>System for benchmarking Lucene</li>
<li>
<b><a href="classification/index.html">classification</a>: </b>Classification module for Lucene</li>
<li>
<b><a href="codecs/index.html">codecs</a>: </b>Lucene codecs and postings formats.</li>
<li>
<b><a href="demo/index.html">demo</a>: </b>Simple example code</li>
<li>
<b><a href="expressions/index.html">expressions</a>: </b>Dynamically computed values to sort/facet/search on based on a pluggable grammar.</li>
<li>
<b><a href="facet/index.html">facet</a>: </b>Faceted indexing and search capabilities</li>
<li>
<b><a href="grouping/index.html">grouping</a>: </b>Collectors for grouping search results.</li>
<li>
<b><a href="highlighter/index.html">highlighter</a>: </b>Highlights search keywords in results</li>
<li>
<b><a href="join/index.html">join</a>: </b>Index-time and Query-time joins for normalized content</li>
<li>
<b><a href="memory/index.html">memory</a>: </b>Single-document in-memory index implementation</li>
<li>
<b><a href="misc/index.html">misc</a>: </b>Index tools and other miscellaneous code</li>
<li>
<b><a href="queries/index.html">queries</a>: </b>Filters and Queries that add to core Lucene</li>
<li>
<b><a href="queryparser/index.html">queryparser</a>: </b>Query parsers and parsing framework</li>
<li>
<b><a href="replicator/index.html">replicator</a>: </b>Files replication utility</li>
<li>
<b><a href="sandbox/index.html">sandbox</a>: </b>Various third party contributions and new ideas</li>
<li>
<b><a href="spatial/index.html">spatial</a>: </b>Geospatial search</li>
<li>
<b><a href="suggest/index.html">suggest</a>: </b>Auto-suggest and Spellchecking support</li>
<li>
<b><a href="test-framework/index.html">test-framework</a>: </b>Framework for testing Lucene-based applications</li>
</ul>