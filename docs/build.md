# Building Lucene.NET

* open command line or terminal
* <code>$ cd path/to/lucene.net</code>
* Windows
    * <code>$ build </code> "Default" is implied
    * <code>$ build Restore</code> restores packages from nuget
* Linux
     * <code>$ sh build.sh </code> "Default" is implied
     * <code>$ sh build.sh Restore </code> restores the packages from nuget. 

## List of Targets

 * __Build:Core__ builds the project found in ./src/Lucene.Net.Core/
 * __Build:TestingFramework__ builds the project found in ./test/Lucene.Net.TestingFramework/
 * __Build:Core:Tests__ builds the project found in ./test/Lucene.Net.Core.Tests/
 * __Clean__ create/cleans the build directory.
 * __Default__ runs the default build pipeline.
 * __Restore__ pulls / restores packages from nuget.
 * __Test:Core__ runs the tests found in ./test/Lucene.Net.Core.Tests

[Back](../README.md)