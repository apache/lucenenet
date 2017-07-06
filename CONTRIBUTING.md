# Lucene.NET Contributor's Guide
Hello new contributors, thanks for getting on board!

Before anything else, please read
[The Getting Involved article at apache.org](https://cwiki.apache.org/confluence/display/LUCENENET/Getting+Involved). In
particular, we will need you to have an ICLA with Apache and to feel
comfortable with Git and GitHub.

You should also be familiar with [Open Source Contribution Etiquette](http://tirania.org/blog/archive/2010/Dec-31.html) and the practice of [Don't "Push" Your Pull Requests](http://www.igvita.com/2011/12/19/dont-push-your-pull-requests/). If you are thinking of making a change that will result in more than 25 lines of changed code, we would appreciate you opening a discussion on our [developer mailing list](https://cwiki.apache.org/confluence/display/LUCENENET/Mailing+Lists) before you start writing. It could save both you and our team quite a bit of work if the code doesn't have to be rewritten to fit in with our overall objectives. 

Start by forking [Lucene.NET on GitHub](https://github.com/apache/lucenenet). For every
contribution you are about to make, you should create a branch (tracking
master!) with some descriptive name, and [send us a Pull Request](https://help.github.com/articles/creating-a-pull-request/) once it is
ready to be reviewed and merged.

And please git rebase when pulling from origin/master instead of merging :) [More information can be found over at Atlassian](https://www.atlassian.com/git/tutorials/rewriting-history/git-rebase).

## If You are Willing to Help with Porting Code

* Please make sure nobody else is working on porting it already. We would
like to avoid doing redundant work. We ask that you communicate clearly in
this list that you are going to work on some part of the project. A PMC
member will then either approve or alert you someone else is working on
that part already.

* Use automated tools to do the basic porting work, and then start a manual
clean-up process. For automatic conversion we are using [Tangible's Java to C# Converter](http://www.tangiblesoftwaresolutions.com/Product_Details/Java_to_CSharp_Converter.html).
We have licenses to give to committers) and it proved to work quite nicely, but I also hear good things on Sharpen. [Check it out here](https://github.com/imazen/sharpen) and pick the tool you are more comfortable
with.

* Conventions & standards: not too picky at this point, but we should
definitely align with the common conventions in .NET: PascalCase and not
camelCase for method names, properties instead of getters/setters of fields
etc. I'm not going to list all the differences now but we probably want to
have such a document up in the future. For reference have a look at
Lucene.Net.Core, while not perfect it is starting to shape up the way we
want it.

* In general, prefer .NETified code over code resembling Java. Enumerators
over Iterators, yields when possible, Linq, BCL data structures and so on.
We are targeting .NET 4.5.1, use this fact. Sometimes you will have to
resort to Java-like code to ensure compatibility; it's ok. We would rather
ship fast and then iterate on improving later.

* While porting tests, we don't care about all those conventions and
.NETification. Porting tests should be reduced to a copy-paste procedure
with minimal cleaning up. We are working on tools and code
helpers to help with that, see for examples see our [Java style methods to avoid many search-replace in porting tests](https://github.com/apache/lucenenet/tree/master/src/Lucene.Net.TestFramework/JavaCompatibility), and a
[R# plugin that will help making some stuff auto-port when pasting](https://resharper-plugins.jetbrains.com/packages/ReSharper.ExJava/).

## Porting Work - Up For Grabs

Note that even though we are currently a port of Lucene 4.8.0, we recommend porting over new work from 4.8.1. We hope to begin the work of upgrading to 4.8.1 soon (let us know if interested). There are only about 100 files that changed between 4.8.0 and 4.8.1.

### Pending being ported from scratch (code + tests)

* [Lucene.Net.Replicator](https://github.com/apache/lucene-solr/tree/releases/lucene-solr/4.8.1/lucene/replicator) - See [JIRA issue 565](https://issues.apache.org/jira/browse/LUCENENET-565) (in progress)
* [Lucene.Net.Analysis.ICU](https://github.com/apache/lucene-solr/tree/releases/lucene-solr/4.8.1/lucene/analysis/icu) - See [JIRA issue 566](https://issues.apache.org/jira/browse/LUCENENET-566)
* [Lucene.Net.Analysis.Kuromoji](https://github.com/apache/lucene-solr/tree/releases/lucene-solr/4.8.1/lucene/analysis/kuromoji) - See [JIRA issue 567](https://issues.apache.org/jira/browse/LUCENENET-567)

### Pending being ported from scratch (code + tests), but have additional dependencies that also either need to be sourced from the .NET ecosystem or ported.

* [Lucene.Net.Benchmark](https://github.com/apache/lucene-solr/tree/releases/lucene-solr/4.8.1/lucene/benchmark) - See [JIRA issue 564](https://issues.apache.org/jira/browse/LUCENENET-564)
* [Lucene.Net.Analysis.Morfologik](https://github.com/apache/lucene-solr/tree/releases/lucene-solr/4.8.1/lucene/analysis/morfologik) - See [JIRA issue 568](https://issues.apache.org/jira/browse/LUCENENET-568)
* [Lucene.Net.Analysis.UIMA](https://github.com/apache/lucene-solr/tree/releases/lucene-solr/4.8.1/lucene/analysis/uima) - See [JIRA issue 570](https://issues.apache.org/jira/browse/LUCENENET-570)

### Partially Completed

* [Lucene.Net.Misc](https://github.com/apache/lucene-solr/tree/releases/lucene-solr/4.8.1/lucene/misc)
 * Missing native C++ Directory implementations for Windows and Unix/Posix along with wrapper classes to utilize them. See the [Store namespace](https://github.com/apache/lucene-solr/tree/releases/lucene-solr/4.8.1/lucene/misc/src/java/org/apache/lucene/store).
* [Lucene.Net.Sandbox](https://github.com/apache/lucene-solr/tree/releases/lucene-solr/4.8.1/lucene/sandbox)
 * Missing all of the SlowCollatedXXX classes, the RegEx namespace (+ related tests). (casing intentional to prevent naming collisions with .NET Regex class)

## If you are more into Fixing Existing Tests

We have already managed to get all of the tests green (most of the time). However, there are still a few [flaky tests](https://teamcity.jetbrains.com/project.html?projectId=LuceneNet_PortableBuilds&tab=flakyTests) that fail randomly that need to be addressed. Since tests are using randomized testing, failures are changing. But if you put a `[Repeat(number)]` attribute on the tests they will fail more often, making them a bit easier to debut.

Some of the code (in particular code in the Support namespace) has no code coverage, and porting/adding tests for those is up for grabs.


* Start by cloning Lucene.NET locally. The set VERBOSE to false and you probably may also want to set a constant seed for working locally. See <https://github.com/apache/lucenenet/blob/master/src/Lucene.Net.TestFramework/Util/LuceneTestCase.cs#L295>
and <https://github.com/apache/lucenenet/blob/master/src/Lucene.Net.TestFramework/Util/LuceneTestCase.cs#L610>

* Note that tests should be run both on .NET Framework and .NET Core. Currently, we have 2 different solutions (Lucene.Net.sln for .NET Framework and Lucene.Net.Portable.sln for .NET Core) that only run in Visual Studio 2015. We are setup to use NUnit 3.x and you will need the appropriate [test adapter](https://marketplace.visualstudio.com/items?itemName=NUnitDevelopers.NUnit3TestAdapter) for Visual Studio to detect the tests. Tests can also be run from the command line using the [dotnet test](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-test) command

* Run, debug, iterate. When you think you fixed a bug or a test, please send a PR as fast as possible. There are multiple people working in this area, and we want to make sure your contribution doesn't go stale. Any such PR should have a descriptive name and a short description of what happened and what is your solution. There are [some good past examples here](https://github.com/apache/lucenenet/pulls?q=is%3Apr+is%3Aclosed).

* If we will have comments, we will use GitHub's excellent interface and you will receive notifications also via this list.

## Other Ways To Help

* Making demos and tutorials, blogging about Lucene.Net, etc. (and providing feedback on how we can make the API better!). If you write a helpful Lucene.Net post on your blog, be sure to let us know so we can link to it.
* Helping out with documentation. We are still trying to make the API docs easily navigable (see #206), and there are many files that are not formatted correctly (links not appearing, tables not very readable, etc). Also, we need help getting all of the Java-related documentation converted to use .NET methodologies.
* Reviewing code. Pick a random section, review line by line. Many of the bugs have been found this way, as the tests are not showing them. Let us know if you find anything suspicious on the [dev mailing list](https://cwiki.apache.org/confluence/display/LUCENENET/Mailing+Lists).
* Create a new Lucene.Net web site. Our [current one](https://lucenenet.apache.org/) is ridiculously out of date and could use a mobile-friendly finish. We could probably also use a refresher on our logo. This project was on our radar for a while but somehow died. Join our [dev mailing list](https://cwiki.apache.org/confluence/display/LUCENENET/Mailing+Lists), gather requirements, and you will be well on your way.
* Optimizing code. During porting we have ended up with some code that is less than optimal. We could use a hand getting everything up to speed (pun intended).
* Helping update the API, or at least just providing feedback on what is important. There are several things on our radar, like integrating something like [Lucene.Net.Linq](https://github.com/themotleyfool/Lucene.Net.Linq) directly into our project, [converting the remaining public-facing iterator classes into `IEnumerator<T>`](https://issues.apache.org/jira/projects/LUCENENET/issues/LUCENENET-469?filter=allopenissues) so they can be used with foreach loops, adding extension methods to remove the need for casting, etc.
* Creating projects to make Lucene.Net easier to use with various .NET frameworks (ASP.NET MVC, WebApi, AspNetCore, WPF, EntityFramework, etc). In general, we would like common tasks as easy as possible to integrate into applications build on these frameworks without everyone having to write the same boilerplate code.
* Building automation tools to eliminate some of the manual work of managing the project, updating information on various web pages, creating tools to make porting/upgrading more automated, etc.
* Be a power beta tester. Make it your mission to track down bugs and report them to us on [JIRA](https://issues.apache.org/jira/issues/?jql=project%20%3D%20LUCENENET%20AND%20status%20%3D%20Open).

Also, check out the [JIRA issue tracker](https://issues.apache.org/jira/issues/?jql=project%20%3D%20LUCENENET%20AND%20status%20%3D%20Open) for any other issues that you might be interested in helping with. You can signup for a JIRA account [here](https://cwiki.apache.org/confluence/signup.action) (it just takes a minute).

Or, if none of that interests you, join our [dev mailing list](https://cwiki.apache.org/confluence/display/LUCENENET/Mailing+Lists) and ask!

## Thank You For Your Help!

Again, thank you very much for your contribution. May the fork be with you!