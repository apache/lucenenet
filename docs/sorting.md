#Sorting

The system libraries in Java and C# have differences in the default sorting mechanism for their respective frameworks.

[Array.Sort](http://msdn.microsoft.com/en-us/library/kwx6zbd4\(v=vs.110\).aspx)in C# uses [Quick Sort](http://algs4.cs.princeton.edu/23quicksort) as the default algorithm for soring.

[Array.sort](http://docs.oracle.com/javase/7/docs/api/java/util/Arrays.html#sort(T\[\],%20java.util.Comparator) in in Java uses the [Tim Sort](http://svn.python.org/projects/python/trunk/Objects/listsort.txt) as the default sorting algorithm as of Java 7.

The differences in sorting methods could account for the discrepencies between running ported tests for the various sorting algorithms in Lucene core.

The base sorter test class uses Array.sort in order to test the ordinal order of elements in the array. Because of the differences of sorting alogrithm in the languages, this may break the tests that test for the oridinal positions.