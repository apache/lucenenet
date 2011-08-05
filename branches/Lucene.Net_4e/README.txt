Apache Lucene.Net README file


INTRODUCTION

Apache Lucene.Net is a C# full-text search engine.  Apache Lucene.Net is not a complete application, but rather a code library and API that can easily be used to add search capabilities to applications.

Apache Lucene.Net is compiled against Microsoft .NET Framework 4.0

The Apache Lucene.Net web site is at:
  http://incubator.apache.org/lucene.net/

Please join the Apache Lucene.Net-User mailing list by sending a message to:
  lucene-net-user-subscribe@incubator.apache.org
  
NOTICE
	This is an experimental branch of lucene.net for a more .net idomatic port
	of lucene 4.
	
		* The contrib projects do not exist.
		* You will need to install the portable library tools and visual studio SP1
		  * http://msdn.microsoft.com/en-us/library/gg597391.aspx
		  * http://visualstudiogallery.msdn.microsoft.com/b0e0b5e9-e138-410b-ad10-00cb3caf4981/
		* Gallio is now the main test runner.
		* The code is currently strictly ms coding standard via style cop 4.5.
		  * style cop is currently ensuring there is a file header on 
		    all code files so that the apache license is placed in all files. 
	      * style cop will run whenever you build a project.
		* The project structure was changed to support nuget packages.
		* NUnit compatibility must be maintained for mono support.
		* The doc comment links to source files are to https://github.com/wickedsoftware/
		  till this branch is accepted as the real lucene.net 4 branch.


PROJECT STRUCTURE

    bin                    - the final location of the assemblies once the build scripts
                             say that the build passes and misc command files.

    build/*                - build scripts and additional solution files.
        artifacts          - disposable items that created for metrics or prep 
                             for packaging lucene.net.
        bin                - temporary location of the items created by the build process.
        scripts            - the various scripts that ci and build scripts 
                             need to be able to run.
        solutions          - where various additional solutions files will go.
        
    packages               - the location of external assemblies, think of this folder as
                             the typical lib folder, except that nuget requires it to 
                             be named packages.
        
    src/*                  - the source code of various projects including contrib projects
        Lucene.Net
        Lucene.Net.TestFramework
  
    test/*
        Lucene.Net.Test
        Lucene.Net.TestFramework.Test

    tools/*                - any executables, plugins, etc. including 
    							* ItemTemplates and ProjectTemplates



DOCUMENTATION

    Wiki
        https://cwiki.apache.org/LUCENENET/


    MSDN style API documentation. 
        It can be created using the build scripts and having sandcastle & MsBuild installed
 
    Get the source
    
        For Lucene.Net 4.0 
            svn co https://svn.apache.org/repos/asf/incubator/lucene.net/branches/lucene-net-4
            git clone https://github.com/apache/lucene.net.git
                * git checkout v4 origin/lucene-net-4
    
        For Trunk
            svn co https://svn.apache.org/repos/asf/incubator/lucene.net/trunk/
            git clone https://github.com/apache/lucene.net.git
            
    Build
        $ cd branch/build/scripts      (change the directory)
        $ build build                  (build the projects)
        $ build test                   (run the tests, uses gallio on windows, 
        									and nunit on mono)
        $ build documents              (will build docs if you have sandcastle 
        									and msbuild installed)
        $ build coverage               (will create ncover 3 code coverage 
        									if you have ncover 3 installed)
        $ bulid rules                  (will run fx-cop and put coverage into artifacts)

        * style cop will inject warnings whenever you build the project as long as you have
          style cop installed on your local dev or build machine.
MISC

The snowball stemmers were developed by Martin Porter and Richard Boulton.
  Snowball.Net/Snowball.Net/SF/Snowball


The full snowball package is available from
  http://snowball.tartarus.org/


  
Apache Lucene.Net
Copyright 2006-2011 The Apache Software Foundation

This product includes software developed by
The Apache Software Foundation (http://www.apache.org/).


