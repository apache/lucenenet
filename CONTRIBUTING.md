# Lucene.NET Contributor's Guide

Have you found a bug or do you have an idea for a cool new enhancement? Contributing code is a great way to give something back to the open-source community. Before you dig right into the code there are a few guidelines that we need contributors to follow so that we can have a chance of keeping on top of things.

## Getting Started

- Read [Open Source Contribution Etiquette](http://tirania.org/blog/archive/2010/Dec-31.html) and [Don't "Push" Your Pull Requests](http://www.igvita.com/2011/12/19/dont-push-your-pull-requests/).
- Make sure you have a [GitHub account](https://github.com/signup/free). NOTE: Although this is a mirror of our Git repository, pull requests are accepted through GitHub.
- If you are thinking of adding a feature, we would appreciate you opening a discussion on our [developer mailing list](https://cwiki.apache.org/confluence/display/LUCENENET/Mailing+Lists) before you start writing. It could save both you and our team quite a bit of work if the code doesn't have to be rewritten to fit in with our overall objectives.
- Submit a [new issue on GitHub](https://github.com/apache/lucenenet/issues), assuming one doesn't exist already.
  - If reporting a bug, clearly describe the issue including steps to reproduce, observed behavior, and expected behavior.
  - If reporting a bug, provide source code that we can run without any alteration demonstrating the issue. Issues submitted with runnable code will be given a higher priority than those submitted without.
- If you will be submitting a [pull request](https://github.com/apache/lucenenet/pulls), fork the repository on GitHub.
  - Create a new branch with a descriptive name (tracking master) and [submit a Pull Request](https://help.github.com/articles/creating-a-pull-request/).

> **NOTE:** In the past, the Lucene.NET project used the [JIRA issue tracker](https://issues.apache.org/jira/projects/LUCENENET/issues), which has now been deprecated. However, we are keeping it active for tracking legacy issues. Please submit any new issues to GitHub.
  
## Up For Grabs

There are several [**Open Issues on GitHub**](https://github.com/apache/lucenenet/labels/up-for-grabs) that are marked `up-for-grabs` that we could use help with.

## Other Ways To Help

* Be a power beta tester. Make it your mission to track down bugs and report them to us on [GitHub](https://github.com/apache/lucenenet/issues).
* Optimizing code. During porting we have ended up with some code that is less than optimal. We could use a hand getting everything up to speed (pun intended).
* Helping update the API, or at least just providing feedback on which API changes are affecting the usability. There are several things on our radar, like integrating something like [Lucene.Net.Linq](https://github.com/themotleyfool/Lucene.Net.Linq) directly into our project, [converting the remaining public-facing iterator classes into `IEnumerator<T>`](https://issues.apache.org/jira/projects/LUCENENET/issues/LUCENENET-469?filter=allopenissues) so they can be used with foreach loops, adding extension methods to remove the need for casting, etc.
* Making demos and tutorials, blogging about Lucene.Net, etc. (and providing feedback on how we can make the API better!). If you write a helpful Lucene.Net post on your blog, be sure to let us know so we can link to it.
* Helping out with documentation. We are still trying to make the API docs easily navigable (see #206), and there are many files that are not formatted correctly (links not appearing, tables not very readable, etc). Also, we need help getting all of the Java-related documentation converted to use .NET methodologies.
* Fixing TODOs. There are several TODOs throughout the code that need to be reviewed and action taken, if necessary. Search for `LUCENENET TODO|LUCENE TO-DO` using the regular expression option in Visual Studio to find them. Do note there are a lot of TODOs left over from Java Lucene that are safe to ignore.
* Reviewing code. Pick a random section, review line by line, comparing the code against the [original Lucene 4.8.0 code](https://github.com/apache/lucene-solr/tree/releases/lucene-solr/4.8.0/lucene). Many of the bugs have been found this way, as the tests are not showing them. Let us know if you find anything suspicious on the [dev mailing list](https://cwiki.apache.org/confluence/display/LUCENENET/Mailing+Lists) or [submit a pull request](https://help.github.com/en/github/collaborating-with-issues-and-pull-requests/creating-a-pull-request) with a fix.
* Creating projects to make Lucene.Net easier to use with various .NET frameworks (ASP.NET MVC, WebApi, AspNetCore, WPF, EntityFramework, etc). In general, we would like common tasks as easy as possible to integrate into applications build on these frameworks without everyone having to write the same boilerplate code.
* Building automation tools to eliminate some of the manual work of managing the project, updating information on various web pages, creating tools to make porting/upgrading more automated, etc.

Or, if none of that interests you, join our [dev mailing list](https://cwiki.apache.org/confluence/display/LUCENENET/Mailing+Lists) and ask!

## Thank You For Your Help!

Again, thank you very much for your contribution. May the fork be with you!