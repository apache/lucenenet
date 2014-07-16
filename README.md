# Apache Lucene.Net For KLR / PCL

## INTRODUCTION

This branch is an experimental version of LUCENE.NET being built with Visual Studio 14 for PCL, portable class libraries, and the K10 / KLR (K or cloud Language Runtime for .NET.  There will be breaking change and porting to PCL will be challenging. If you're up for a challenge, please contribute. 

Apache Lucene.Net is a C# full-text search engine.  Apache Lucene.Net is not a complete application, 
but rather a code library and API that can easily be used to add search capabilities to applications.

Apache Lucene.Net is compiled against Microsoft .NET Framework 4.0

The Apache Lucene.Net web site is at:
  http://lucenenet.apache.org

## Getting Started

### Windows Users
 * Install a copy of [http://blogs.msdn.com/b/visualstudio/archive/2014/07/08/visual-studio-14-ctp-2-available.aspx](Visual Studio 14).
 * Install [https://chocolatey.org/packages?q=git](git) <code>$ cinst git</code>
 * [https://github.com/apache/lucene.net/fork](Fork) a copy of lucene.net on github. 
 * Clone your copy <code>$ git clone https://github.com/[user]/lucene.net.git</code>
 * <code>cd path/to/lucene.net</code>
 * <code>git checkout -b pcl origin/pcl </code>
 * Open the solution and build the project. Nuget package restore should be enabled.

## Contributing Back

### Articles
Submit any articles and tutorials to the developers list, dev @ lucenenet.apache.org.

### Contributing Code For Lucene.Net 5.0 PCL Branch
 * The current branch that is being ported is master on [https://github.com/apache/lucene-solr](lucene-solor). This will change to a tag in the future. 
 * Create a *task* in [https://issues.apache.org/jira/browse/LUCENENET/](Jira) 
    * Add a label *patch* 
    * Set the affects version *Lucene.Net 5.0 PCL* 
    * State the intended work. 
 	* Acknowledge that the code in the pull request is licensed under Apache 2.0 and is not copied work.
 * Follow the internal [http://blogs.msdn.com/b/brada/archive/2005/01/26/361363.aspx](MS code style guides).
 * Make sure to submit tests and document code. 
   * If the pull request lacks tests and documentation, it will take for commiters to get the pull request adjusted and merged.
   * *Contributing something is better than nothing* and maybe someone will take the patch / pull request and complete it. 
 * Put the ticket number in commit messages. i.e. LUCENENET-168
 * Send a pull request with the ticket number to [https://github.com/apache/lucene.net/](github).
 * See Also: [http://www.apache.org/dev/contributors](Contributor's Guide)
  
## Running Tests

 * open a command line tool
 * change the directory to the test project that you wish to run.
 * run the command <code>$ k test</code>

 ### example

 ```bash
$ cd path/to/lucene.net/tests/Lucene.Net.Core.Tests
$ k test
 ```


## Milestone 1
 * Implement Build Scripts with [http://fsharp.github.io/FAKE/](Fake)
 	* Crossplatform Support.
 	* Easier than using XML.
 * Implement a ci server, possibly appveyor
 * Generate Code Documentation.
 * Port core, test-framework, and tests for core. 

## Notes

 * *.csproj files exist for the PCL version because the [http://forums.asp.net/p/1996333/5735820.aspx?Re+NETPortable+profile+throws+a+warning+CS8021+No+value+for+RuntimeMetadataVersion+found+](RuntimeMetadataVersion) is not found. Once this issue is resolved, those files will be removed.


