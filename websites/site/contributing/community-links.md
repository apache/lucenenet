---
uid: contributing/community-links
---
Community Links
===============

---------------

## Blog Posts

Here are some great posts from the Lucene.Net community:

### [Full Text Search with Lucene.Net](https://www.elbisch.ch/2019/05/31/full-text-search-for-database-entities-with-lucene-net/) 

A very detailed how-to guide for working with Lucene.Net. 

> "This is one of the best posts I have ever seen about Lucene.NET"


### Introductory Lucene.Net Series

An excellent introductory series from [Simone Chiaretta](http://codeclimber.net.nz/)

* [How to get started with Lucene.Net](http://codeclimber.net.nz/archive/2009/08/27/how-to-get-started-with-lucene.net.aspx)
* [The Main Concepts](http://codeclimber.net.nz/archive/2009/08/31/lucene.net-the-main-concepts.aspx)
* [Your First Application](http://codeclimber.net.nz/archive/2009/09/02/lucene.net-your-first-application.aspx)
* [Dissecting Storage Documents and Fields](http://codeclimber.net.nz/archive/2009/09/04/dissecting-lucene.net-storage-documents-and-fields.aspx)
* [Lucene - or how I stopped worrying and learned to love unstructured data](http://codeclimber.net.nz/archive/2009/09/08/lucene-or-how-i-stopped-worrying-and-learned-to.aspx)
* [How Subtext Lucene.Net index is structured](http://codeclimber.net.nz/archive/2009/09/10/how-subtext-lucene.net-index-is-structured.aspx)

### Other posts

* [Lazily setting the SetMultiTermRewriteMethod](https://shazwazza.com/post/how-to-set-rewrite-method-on-queries-lazily-in-lucene/)
  * How-to guide on lazily setting the rewrite method of the query parser instead of eagerly since you may not know it is required until the query is built.
* [Spatial Search with Lucene.Net and Examine](https://shazwazza.com/post/spatial-search-with-examine-and-lucene/)
  * How-to guide on implementing geo spatial search with Lucene.Net in the context of using [Examine](https://github.com/shazwazza/examine) to manage Lucene.Net.
* [Implementing Search in Blazor WebAssembly with Lucene.Net](https://www.aaron-powell.com/posts/2019-11-29-implementing-search-in-blazor-webassembly-with-lucenenet/)
  * How-to guide on setting up Lucene.Net to work with [Blazor WebAssembly](https://docs.microsoft.com/en-gb/aspnet/core/blazor/?view=aspnetcore-3.0&WT.mc_id=aaronpowell-blog-aapowell#blazor-webassembly).

## Lucene.Net projects

Here are some great projects built with Lucene.Net:

### [Examine](https://github.com/shazwazza/examine)

Examine is a managed abstraction around Lucene.Net. It provides a fluent search API and handles all of the underlying Lucene.Net objects for you. 

### [BoboBrowse.Net](https://github.com/NightOwl888/BoboBrowse.Net)

Bobo-Browse is a powerful and extensible faceted search engine library built on top of Lucene.Net. It is a C# port of the original Bobo-Browse project written in Java by John Wang.

### [Azure Directory](https://github.com/azure-contrib/AzureDirectory)

This project allows you to create Lucene Indexes and use them in Azure.

This project implements a low level Lucene Directory object called AzureDirectory around Windows Azure BlobStorage.
