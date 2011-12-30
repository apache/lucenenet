Apache Lucene.Net README file


INTRODUCTION
-------------
Apache Lucene.Net is a C# full-text search engine.  Apache Lucene.Net is not a complete application, 
but rather a code library and API that can easily be used to add search capabilities to applications.

Apache Lucene.Net is compiled against Microsoft .NET Framework 4.0

The Apache Lucene.Net web site is at:
  http://incubator.apache.org/lucene.net/

Please join the Apache Lucene.Net-User mailing list by sending a message to:
  lucene-net-user-subscribe@incubator.apache.org


FILES
---------------
build/scripts
  Build scripts
  
build/*
  Visual Studio solution files
 
src/Contrib
  Contributed code whihc extends and enhances Apahce Lucene.Net, but is not part of the core library
  
src/core
  The Lucene source code.

src/Demo
  Some example code.

test/*
  nUnit tests for Lucene.Net and Contrib projects

DOCUMENTATION
---------------------
MSDN style API documentation for Apache Lucene.Net exists.  Those can be found at this site:
  http://incubator.apache.org/lucene.net/docs/2.9.4/Index.html
  
  or 
  
  http://incubator.apache.org/lucene.net/docs/2.9.4/Lucene.Net.chm
  
ADDITIONAL LIBRARIES
-----------------------------
There are a number of additional libraries that various parts of Lucene.Net may depend. These are not 
included in the source distribution

These libraries can be found at:
	https://svn.apache.org/repos/asf/incubator/lucene.net/tags/Lucene.Net_2_9_4_RC3/lib/
	
Libraries:
	- Gallio 3.2.750
	- ICSharpCode
	- Nuget
	- NUnit.org
	- StyleCop 4.5