# Documentation Tools

## JavaDocToMarkdownConverter

This is a utility that is executed against the Java Lucene source to convert their documentation files into markdown documentation files that we can 
use to build the Lucene.Net documentation suite. 

This utility does a lot of edge case work to try to automatically convert as much as possible from the Java Lucene repo into something usable for our project.

The source that this is executed against is this tag: https://github.com/apache/lucene-solr/releases/tag/releases%2Flucene-solr%2F4.8.1

See docs: https://lucenenet.apache.org/contributing/documentation.html#api-docs

## LuceneDocsPlugins

This is a DocFx custom extension. It is built and executed as part of the docs build process to customize/extend some of the features in DocFx.