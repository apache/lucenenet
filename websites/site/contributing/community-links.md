---
uid: contributing/community-links
---

# Community Links

---

## Blog Posts

Here are some great posts from the Lucene.Net community:

### [Full Text Search with Lucene.Net](https://www.elbisch.ch/2019/05/31/full-text-search-for-database-entities-with-lucene-net/)

A very detailed how-to guide for working with Lucene.Net.

> "This is one of the best posts I have ever seen about Lucene.NET"

### Great intro posts about Lucene:

* [Analysis of Lucene - Basic Concepts](https://www.alibabacloud.com/blog/analysis-of-lucene---basic-concepts_594672)
* [Apache Lucene: free search for your website](https://www.ionos.com/digitalguide/server/configuration/apache-lucene/)

### Introductory Lucene.Net Series

An excellent introductory series from [Simone Chiaretta](http://codeclimber.net.nz/)

- [How to get started with Lucene.Net](https://codeclimber.net.nz/archive/2009/08/27/how-to-get-started-with-lucenenet/)
- [The Main Concepts](https://codeclimber.net.nz/archive/2009/08/31/lucenenet-the-main-concepts/)
- [Your First Application](https://codeclimber.net.nz/archive/2009/09/02/lucenenet-your-first-application/)
- [Dissecting Storage Documents and Fields](https://codeclimber.net.nz/archive/2009/09/04/dissecting-lucenenet-storage-documents-and-fields/)
- [Lucene - or how I stopped worrying and learned to love unstructured data](https://codeclimber.net.nz/archive/2009/09/08/lucene-or-how-i-stopped-worrying-and-learned-to/)

### Series on using Lucene.Net with Blazor

A series that covers Lucene.Net search, pagination, auto complete, faceting and highlighting in Blazor from [Thomas Beck](
https://blog.beckshome.com/). Source for all projects [available on GitHub](https://github.com/thbst16/dotnet-lucene-search) with a [live demo site](https://dotnet-lucene-search.azurewebsites.net/) available as well.

- [Lucene + Blazor, Part 1: Basic Search](https://blog.beckshome.com/2022/10/lucene-blazor-part-1-basic-search)
- [Lucene + Blazor, Part 2: Results Paging](https://blog.beckshome.com/2022/11/lucene-blazor-part-2-results-paging)
- [Lucene + Blazor, Part 3: Auto Complete](https://blog.beckshome.com/2022/11/lucene-blazor-part-3-auto-complete)
- [Lucene + Blazor, Part 4: Faceting](https://blog.beckshome.com/2022/11/lucene-blazor-part-4-faceting)
- [Lucene + Blazor, Part 5: Highlighting](https://blog.beckshome.com/2022/11/lucene-blazor-part-5-highlighting)

### Other posts

- [Lazily setting the SetMultiTermRewriteMethod](https://shazwazza.com/post/how-to-set-rewrite-method-on-queries-lazily-in-lucene/)
  - How-to guide on lazily setting the rewrite method of the query parser instead of eagerly since you may not know it is required until the query is built.
- [Spatial Search with Lucene.Net and Examine](https://shazwazza.com/post/spatial-search-with-examine-and-lucene/)
  - How-to guide on implementing geo spatial search with Lucene.Net in the context of using [Examine](https://github.com/shazwazza/examine) to manage Lucene.Net.
- [Implementing Search in Blazor WebAssembly with Lucene.Net](https://www.aaron-powell.com/posts/2019-11-29-implementing-search-in-blazor-webassembly-with-lucenenet/)
  - How-to guide on setting up Lucene.Net to work with [Blazor WebAssembly](https://docs.microsoft.com/en-gb/aspnet/core/blazor/?view=aspnetcore-3.0&WT.mc_id=aaronpowell-blog-aapowell#blazor-webassembly).

## Lucene.Net projects

Here are some great projects built with Lucene.Net:

### [Examine](https://github.com/shazwazza/examine)

Examine is a managed abstraction around Lucene.Net. It provides a fluent search API and handles all of the underlying Lucene.Net objects for you.

### [Azure Directory](https://github.com/azure-contrib/AzureDirectory)

This project allows you to create Lucene Indexes and use them in Azure.

This project implements a low level Lucene Directory object called AzureDirectory around Windows Azure BlobStorage.
