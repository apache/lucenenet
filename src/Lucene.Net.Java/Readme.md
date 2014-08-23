# Lucene.Net.Java

This project is a spin off of the <strong>Support</strong> folder of Lucene.Net. There is enough low level functionality
that the standard Java library has that is not found in .NET to warrant a separate assembly.   

Creating a separate project will let the .NET community leverage the functionality 
without a required dependency to all of Lucene.Net. There is most likely similar 
functionality in IVKM.NET, however, it's JDK falls under GPL v2 which is not compatible with 
the Apache 2.0 License.  

The root namespace of the projeect is <strong>Java</strong> and not <strong>Lucene.Net.Java</strong>, so 
that its easier to convert code with the namespaces. 

There will still be differences between this library and the Java implementation. The goal is to match the Java API 
when possible while still using the correct .NET idioms.  There are also differences in runtimes that will allow for 
a simplified API with generics.  [Comparing Generics Java and C#](http://www.jprl.com/Blog/archive/development/2007/Aug-31.html)