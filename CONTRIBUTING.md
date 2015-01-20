Lucene.NET Contributor's Guide
---
Hello new contributors, thanks for getting on board!

Before anything else, please read
<https://cwiki.apache.org/confluence/display/LUCENENET/Getting+Involved>. In
particular, we will need you to have an ICLA with Apache and to feel
comfortable with Git and GitHub.

Start by forking <https://github.com/apache/lucenenet> on github. For every
contribution you are about to make, you should create a branch (tracking
master!) with some descriptive name, and send us a Pull Request once it is
ready to be reviewed and merged.

And please git rebase
<https://www.atlassian.com/git/tutorials/rewriting-history/git-rebase> when
pulling from origin/master instead of merging :)

If You are Willing to Help with Porting Code
---

* Please make sure nobody else is working on porting it already. We would
like to avoid doing redundant work. We ask that you communicate clearly in
this list that you are going to work on some part of the project. A PMC
member will then either approve or alert you someone else is working on
that part already.

* Use automated tools to do the basic porting work, and then start a manual
clean-up process. For automatic conversion we are using
<http://www.tangiblesoftwaresolutions.com/Product_Details/Java_to_CSharp_Converter.html>
(we have licenses to give to committers) and it proved to work quite
nicely, but I also hear good things on Sharpen. Check this version out:
https://github.com/imazen/sharpen. Pick the tool you are more comfortable
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
with minimal cleaning up procedure. We are working on tools and code
helpers to help with that, see for examples see:
<https://github.com/apache/lucenenet/tree/master/src/Lucene.Net.TestFramework/JavaCompatibility>
(Java style methods to avoid many search-replace in porting tests), and a
R# plugin that will help making some stuff auto-port when pasting
<https://resharper-plugins.jetbrains.com/packages/ReSharper.ExJava/>

Code that is currently pending being ported from scratch (+ tests) == up
for grabs:

<https://github.com/apache/lucene-solr/tree/lucene_solr_4_8_0/lucene/queryparser>
<https://github.com/apache/lucene-solr/tree/lucene_solr_4_8_0/lucene/join>
<https://github.com/apache/lucenenet/tree/master/src/Lucene.Net.Classification>
(missing tests)
<https://github.com/apache/lucenenet/tree/master/src/Lucene.Net.Queries>
(missing tests)
The spatial module (the situation there is a bit subtle, if you are
interested let me know)
More analysis modules:
<https://github.com/apache/lucene-solr/tree/trunk/lucene/analysis> (common is
already mid-porting, see below)
<https://github.com/apache/lucenenet/tree/master/src/Lucene.Net.Misc>
(missing tests)
<https://github.com/apache/lucenenet/tree/master/src/Lucene.Net.Memory>
(missing tests)

Code that is ported and now pending manual cleanup, and then once its
compiling porting of its tests == up for grabs:

<https://github.com/apache/lucenenet/tree/master/src/Lucene.Net.Analysis.Common>
<https://github.com/apache/lucenenet/tree/master/src/Lucene.Net.Suggest>
(depends on Analysis.Common)

The rest is pretty much under control already

If you are more into Fixing Existing Tests
---

* First we want to get all the Core tests green. We are about 200 (out of
~2300) short of that. Since tests are using randomized testing, failures
are changing.

* Start by cloning Lucene.NET locally. The set VERBOSE to false and you
probably may also want to set a constant seed for working locally. See
<https://github.com/apache/lucenenet/blob/master/src/Lucene.Net.TestFramework/Util/LuceneTestCase.cs#L295>
and
<https://github.com/apache/lucenenet/blob/master/src/Lucene.Net.TestFramework/Util/LuceneTestCase.cs#L610>

* Run, debug, iterate. When you think you fixed a bug or a test, please
send a PR as fast as possible. There are multiple people working in this
area, and we want to make sure your contribution doesn't go stale. Any such
PR should have a descriptive name and a short description of what happened
and what is your solution. There are some good past examples here:
<https://github.com/apache/lucenenet/pulls?q=is%3Apr+is%3Aclosed>

* If we will have comments, we will use github's excellent interface and
you will receive notifications also via this list.

* Once we got all core tests passing reliably, we will go to testing the
other sub-projects. Some are already up for grabs if you want to have a
stab at them.

Other types of help
---

We will definitely need more help (like normalizing tabs/spaces, license
headers, automating stuff, etc) but we are not there yet!

Thank You!
---

Again, thank you very much for your contribution. May the fork be with you!