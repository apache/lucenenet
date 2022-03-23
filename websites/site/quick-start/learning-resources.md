---
uid: quick-start/learning-resources
---

# Learning Resources

---


## Lucene.NET Documentation

Lucene.NET has fairly extensive documentation that can be found in the [Lucene.NET Documentation](xref:docs) section of the website.  On that page you will find links to the documentation for various release versions.  Click on the documentation link for the version of interest and you will see that it leads to a wealth of documentation.  The project is organized as a collection of sub-projects, each one corresponding ultimately to a [nuget package](https://www.nuget.org/packages/Lucene.Net/absoluteLatest).  Those projects each have documentation that typically starts with some higher level docs that then link into API or Object/Method level documentations.

Much of this documentation is compiled from the inline documentation comments from the source code and it originated from the Java Lucene code base.  In addition to porting the Lucene code from Java to C# we are also working to convert the docs from Java to C# examples as well.  Some of this work is completed and some of it remains.   


## Java Lucene Documentation

Apache [Lucene.NET 4.8](https://github.com/apache/lucenenet) is a port of Java Apache [Lucene 4.8](https://github.com/apache/lucene/tree/releases/lucene-solr%2F4.8.0).  In general we try to do a line by line port when possible.  So if you did a comparison of the code for a file in the one [GitHub repo](https://github.com/apache/lucenenet) against the corresponding file in the [other repo](https://github.com/apache/lucene/tree/releases/lucene-solr%2F4.8.0) the two should in most cases look extremely similar. However, even for files that port fairly easily you will notice that in the conversion process we ".NETify" the code.  That is we make stylistic changes to confirm to the .NET way of writing code.

So for example, in Java it's common for method names start with a lower case letter, but of course  in .NET we expect method names to start with an upper case letter.  So we change the method names to confirm to .NET conventions when porting the code. Likewise, C# has support for properties with getters and setters but Java does not.  So sometimes when converting the Java to C# a get method that is behaving like a property accessor will be turned into a C# property.

But other then that, you will find that the documentation for Java Lucene 4.X can be very useful to your learning of Lucene.NET 4.8.  Just keep in mind the ".NETifing" that we do to the code and it's pretty simple to translate in your head Java examples into the C# equivalent.  


## From the Community


### Searching Lucene.NET Issues

Another source of information about Lucene.NET is current and past GitHub issues for our repo.  By default when you go to the GitHub issues page it defaults the search box criteria to open issues only but you can easily remove the `is:open` from the search box to search all issues for the repo.  Or you can use this link to [search all issues](https://github.com/apache/lucenenet/issues?q=is%3Aissue+is%3Aopen+) in our repo.

![Search Lucene.NET Issues](https://lucenenet.apache.org/images/quick-start/learning-resources/search-lucenenet-issues.gif)


### Searching Java Lucene Issues
In general, the Java Lucene Issues database can be a good place to learn about about how features were developed, the historical issues related to features, and how issues were resolved.  One thing that can be helpful to know is that each major Lucene feature is assigned a Lucene issue number and it's often referenced using this format: LUCENE-<issue number> for example LUCENE-6001.

Java Lucene has a GitHub mirror of their repo, but they don't track issues there.  If you want to search issues for the Java Lucene project you need to search on the [apache.org issues website](https://issues.apache.org/jira/projects/LUCENE/issues/).

By default that page shows only open issues but you can click the "View all issues and filters" link in the upper right corner of the screen (see arrow below) to see and search all issues.

![Search Lucene.NET Issues](https://lucenenet.apache.org/images/quick-start/learning-resources/search-lucene-issues.gif)


### Apache Lucene.NET Email Archives
You can search the Lucene.NET dev email archives for past emails that may contain information of a topic you'd like to dig into.  This can be especially useful, for example, to research why we may have chosen a specific porting approach for some code that wasn't so easy to port.  https://lists.apache.org/list.html?dev@lucenenet.apache.org

### Java Apache Lucene Email Archives
You can search the Java Lucene dev email archives for past emails as well.  If you the feature or patch you are trying to learn about has an issue number, searching the email archives for it formatted like LUCENE-<issue number> for example LUCENE-6001 can return some really great discussion about the rational that went into defining how that feature works. https://lists.apache.org/list.html?dev@lucene.apache.org

### StackOverflow

StackOverflow is a popular programmer question and answer website.  Both Lucene and Lucene.NET both have tags on StackOverflow.

One great way to learn about Lucene.NET is to read the [most upvoted Lucene.NET questions](https://stackoverflow.com/questions/tagged/lucene.net?sort=MostVotes) StackOverflow is also a great place to ask questions about Lucene.NET>

Additionally you can read the [most upvoted Lucene questions](https://stackoverflow.com/questions/tagged/lucene.net?sort=MostVotes) on StackOverflow.  Most of these questions and answers will apply to Lucene 4.8 but be aware that some may not if they are for related to features that came after version 4.8.

> [!TIP]
> StackOverflow is a great place to post questions about Lucene.NET.  When you post questions there it's important to tag the question with both a `lucene.net` tag and a `lucene` tag if the question doesn't specifically pertain to the .NET platform.  Having both tags will often result in a faster response.

### Community Articles

In the contributing section of this website we have a list of several community articles about Lucene.NET.  You can fine them on the [Community Links](xref:contributing/community-links) page.

### Books

**[Instant Lucene.NET](https://www.amazon.com/Instant-Lucene-NET-Michael-Heydt/dp/1782165940)** - Currently this is the only book specifically about Lucene.NET.  Based on the reviews it's primarily targeted towards those just getting started with Lucene.NET.

**[Lucene 4 Cookbook](https://www.amazon.com/Lucene-4-Cookbook-Edwood-Ng/dp/1782162283/)** - The nice thing about this book is that it is specifically written for Lucene 4, so the examples are all from the era of Lucene.NET 4.X and will work great on Lucene.NET 4.8  While the code examples are in Java it's easy to convert them to C#. ie. change method names from starting with a lower case letter to an upper case letter.  Change some getter methods to properties instead.

**[Lucene In Action 2nd Edition](https://www.amazon.com/Lucene-Action-Second-Covers-Apache/dp/1933988177)** - This is a great book written by a core Lucene committer that dives deep into the inner workings of Lucene.  It's packed full of great information about Lucene. It is written for Java Lucene rather then Lucene.NET but as I have already mentioned, it's generally not a big deal to translate Lucene code samples from Java to c#.  That said, the one downside to this book is that it was written during the Lucene 3.0 era.  The largest changes in the history of Lucene came in version 4.0.  So some of the information in this book, and some of the code samples, are outdated.  But much of the information, especially at the conceptual level, hold true and is very valuable for understanding Lucene.

**[Introduction to Information Retrieval](https://nlp.stanford.edu/IR-book/information-retrieval-book.html)** - This book isn't written about Lucene per se but is a great resource on the subject of information retrieval which is the knowledgebase that underpins search software like Lucene.  This book was written by two professors at Stanford University and one from the University of Stuttgart.  The introduction in the book states "Introduction to Information Retrieval is the first textbook with a coherent treatment of classical and web information retrieval, including web search and the related areas of text classification and text clustering.  Written from a computer science perspective, it gives and up-to-date treatment of all aspects of the design and implementation of systems for gathering, indexing and searching documents..."  The content is [available online for free](https://nlp.stanford.edu/IR-book/information-retrieval-book.html) or a [hard cover version](https://www.amazon.com/Introduction-Information-Retrieval-Christopher-Manning/dp/0521865719) is available for purchase.



