# Lucene.NET Contributor's Guide
Hello new contributors, thanks for getting on board!

Before anything else, please read
[The Getting Involved article at apache.org](https://cwiki.apache.org/confluence/display/LUCENENET/Getting+Involved). In
particular, we will need you to have an ICLA with Apache and to feel
comfortable with Git and GitHub.

You should also be familiar with [Open Source Contribution Etiquette](http://tirania.org/blog/archive/2010/Dec-31.html) and the practice of [Don't "Push" Your Pull Requests](http://www.igvita.com/2011/12/19/dont-push-your-pull-requests/). If you are thinking of making a change that will result in more than 25 lines of changed code, we would appreciate you opening a discussion on our [developer mailing list](https://cwiki.apache.org/confluence/display/LUCENENET/Mailing+Lists) before you start writing. It could save both you and our team quite a bit of work if the code doesn't have to be rewritten to fit in with our overall objectives. 

Start by forking [Lucene.NET on GitHub](https://github.com/apache/lucenenet). For every
contribution you are about to make, you should create a branch (tracking
master!) with some descriptive name, and send us a Pull Request once it is
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

### Documentation Comments == up for grabs:

1. Lucene.Net.Core (project)
   1. Codecs (namespace)
   2. Support (namespace)
   3. Util.Automaton (namespace)
   4. Util.Mutable (namespace)
   5. Util.Packed (namespace)
2. Lucene.Net.Codecs (project)

See [Documenting Lucene.Net](https://cwiki.apache.org/confluence/display/LUCENENET/Documenting+Lucene.Net) for instructions. 

> While it is assumed that the documentation comments for the other projects are finished, they could probably all use a review. Also be sure to check the comments against [Lucene 4.8.0](https://github.com/apache/lucene-solr/tree/releases/lucene-solr/4.8.0/lucene) to ensure they are correct and complete!

### Code that is currently pending being ported from scratch (+ tests) == up for grabs:

* [Lucene.Net.Demo](https://github.com/apache/lucene-solr/tree/releases/lucene-solr/4.8.0/lucene/demo) (might be a good learning experience)
* [Lucene.Net.Replicator](https://github.com/apache/lucene-solr/tree/releases/lucene-solr/4.8.0/lucene/replicator)
* [Lucene.Net.Analysis.ICU](https://github.com/apache/lucene-solr/tree/releases/lucene-solr/4.8.0/lucene/analysis/icu) (note that we will be putting this functionality into the Lucene.Net.ICU package)
* [Lucene.Net.Analysis.Kuromoji](https://github.com/apache/lucene-solr/tree/releases/lucene-solr/4.8.0/lucene/analysis/kuromoji)
* [Lucene.Net.Analysis.SmartCn](https://github.com/apache/lucene-solr/tree/releases/lucene-solr/4.8.0/lucene/analysis/smartcn)

There are a few other specialized Analysis packages ([Morfologik](https://github.com/apache/lucene-solr/tree/releases/lucene-solr/4.8.0/lucene/analysis/morfologik), [Phonetic](https://github.com/apache/lucene-solr/tree/releases/lucene-solr/4.8.0/lucene/analysis/phonetic), [UIMA](https://github.com/apache/lucene-solr/tree/releases/lucene-solr/4.8.0/lucene/analysis/uima)) that have dependencies that would also need to be ported if they don't exist in .NET yet.

There are several command-line utilities for tasks such as maintaining indexes that just need to be put into a console application and "usage" documentation updated for them to be useful (which might be helpful for those who don't want to install Java to run such utilities from the Lucene project). See the [JIRA Issues](https://issues.apache.org/jira/issues/?jql=project%20%3D%20LUCENENET%20AND%20status%20%3D%20Open%20AND%20text%20~%20%22CLI%22) for the current list.

The [Lucene.Net.Misc](https://github.com/apache/lucene-solr/tree/releases/lucene-solr/4.8.0/lucene/misc) project has some native C++ directories for Windows and Unix/Posix along with wrapper classes to utilize them (in the Store namespace) that are not yet ported, and the [Lucene.Net.Sandbox](https://github.com/apache/lucene-solr/tree/releases/lucene-solr/4.8.0/lucene/sandbox) project is still incomplete.

## If you are more into Fixing Existing Tests

We have already managed to get all of the tests green (most of the time). However, there are still a few [flaky tests](https://teamcity.jetbrains.com/project.html?projectId=LuceneNet_PortableBuilds&tab=flakyTests) that fail randomly that need to be addressed. Since tests are using randomized testing, failures are changing.

Some of the code (in particular code in the Support namespace) has no code coverage, and porting/adding tests for those is up for grabs.


* Start by cloning Lucene.NET locally. The set VERBOSE to false and you
probably may also want to set a constant seed for working locally. See
<https://github.com/apache/lucenenet/blob/master/src/Lucene.Net.TestFramework/Util/LuceneTestCase.cs#L295>
and
<https://github.com/apache/lucenenet/blob/master/src/Lucene.Net.TestFramework/Util/LuceneTestCase.cs#L610>

* Note that tests should be run both on .NET Framework and .NET Core. Currently, we have 2 different solutions (Lucene.Net.sln for .NET Framework and Lucene.Net.Portable.sln for .NET Core) that only run in Visual Studio 2015 and onwards. We are setup to use NUnit 3.x and you will need the appropriate [test adapter](https://marketplace.visualstudio.com/items?itemName=NUnitDevelopers.NUnit3TestAdapter) for Visual Studio to detect the tests. Tests can also be run from the command line using the [dotnet test]() command

* Run, debug, iterate. When you think you fixed a bug or a test, please
send a PR as fast as possible. There are multiple people working in this
area, and we want to make sure your contribution doesn't go stale. Any such
PR should have a descriptive name and a short description of what happened
and what is your solution. There are [some good past examples here](https://github.com/apache/lucenenet/pulls?q=is%3Apr+is%3Aclosed).

* If we will have comments, we will use github's excellent interface and
you will receive notifications also via this list.

## Other types of help

We will definitely need more help (like optimizing code, normalizing tabs/spaces, license headers, automating stuff, etc) but we are not there yet!

Also, check out the [JIRA issue tracker](https://issues.apache.org/jira/browse/LUCENENET-586?jql=project%20%3D%20LUCENENET%20AND%20status%20%3D%20Open%20AND%20assignee%20in%20(EMPTY)) for any other issues that you might be interested in helping with. You can signup for a JIRA account [here](https://cwiki.apache.org/confluence/signup.action) (it just takes a minute).

## Thank You!

Again, thank you very much for your contribution. May the fork be with you!