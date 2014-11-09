# Apache Lucene.Net For KLR / PCL

## INTRODUCTION

This branch is an experimental version of LUCENE.NET being built with Visual Studio 14 for PCL, portable class libraries, and the K10 / KLR (K or cloud Language Runtime for .NET.  There will be breaking change and porting to PCL will be challenging. If you're up for a challenge, please contribute. 

Apache Lucene.Net is a C# full-text search engine.  Apache Lucene.Net is not a complete application, 
but rather a code library and API that can easily be used to add search capabilities to applications.

Apache Lucene.Net is compiled against Microsoft .NET Framework 4.0

The Apache Lucene.Net web site is at:
  http://lucenenet.apache.org

## NOTICE
This branch is on hiatus as I'm currently working on extending Xunit.Net to have similar functionality to JUnit for TestRules and to enable randomization testing from carrot search.  I'm also working on making the story for running tests quickly from the command line.  

I've also started separate code that provides the same apis found in Java but .Net into a separate library in hopes that it will make it easier for others to port code from Java.  

The Mettle Project can be found here: 
* https://github.com/michaelherndon/xunit
* https://github.com/michaelherndon/mettle

- Michael

## Getting Started

### Windows Users
 * Install a copy of [Visual Studio 14](http://blogs.msdn.com/b/visualstudio/archive/2014/07/08/visual-studio-14-ctp-2-available.aspx).
 * Install [git](https://chocolatey.org/packages?q=git) <code>$ cinst git</code>
 * [Fork](https://github.com/apache/lucene.net/fork) a copy of lucene.net on github. 
 * Clone your copy <code>$ git clone https://github.com/[user]/lucene.net.git</code>
 * <code>$ cd path/to/lucene.net</code>
 * <code>$ git checkout -b pcl origin/pcl </code>
 * Open the solution and build the project. Nuget package restore should be enabled.

## Contributing Back

### Articles
Submit any articles and tutorials to the developers list, dev @ lucenenet.apache.org.

### Contributing Code For Lucene.Net 5.0 PCL Branch
 * The current branch that is being ported is master on [lucene-solr](https://github.com/apache/lucene-solr). This will change to a tag in the future. 
 * Create a __task__ issue in [Jira](https://issues.apache.org/jira/browse/LUCENENET/) 
    * Add the label __patch__ 
    * Set the affects version __Lucene.Net 5.0 PCL__ 
    * State the intended work. 
 	* Acknowledge that the code in the pull request is licensed under Apache 2.0 and the work is not copied. If it is copied, it must be under an apache 2 compatible license and the license must be in the code.
 * Follow the internal [Micrsoft code style guides](http://blogs.msdn.com/b/brada/archive/2005/01/26/361363.aspx).
 * Make sure to submit tests and document public code. Use code comments to explain decisions. 
   * If the pull request lacks tests and documentation, it will take longer for the commiters to adjust and merge the code to make it available for everyone.
   * __Contributing something is better than nothing__. Maybe another contributor will take the patch / pull request and complete it. 
 * Put the ticket number in commit messages. i.e. LUCENENET-168
 * Send a pull request with the ticket number to [github](https://github.com/apache/lucene.net).
 * See Also: [Contributor's Guide](http://www.apache.org/dev/contributors)
  
## Running Tests

 * open a command line tool
 * change the directory to the test project that you wish to run.
 * run the command <code>$ k test</code>

 ### example

 ```bash
$ cd path/to/lucene.net/tests/Lucene.Net.Core.Tests
$ k test
 ```

## Build Scripts
 * uses FAKE
 * see [build docs](docs/build.md)


## Milestone 1
 * Implement Build Scripts with [Fake](http://fsharp.github.io/FAKE/)
 	* Crossplatform Support.
 	* Easier than using XML.
 * Implement a ci server, possibly appveyor
 * Generate Code Documentation.
 * Port core, test-framework, and tests for core. 
  * 

## Notes

 * *.csproj files exist for the PCL version because the [RuntimeMetadataVersion](http://forums.asp.net/p/1996333/5735820.aspx?Re+NETPortable+profile+throws+a+warning+CS8021+No+value+for+RuntimeMetadataVersion+found+) is not found. Once this issue is resolved, those files will be removed.


