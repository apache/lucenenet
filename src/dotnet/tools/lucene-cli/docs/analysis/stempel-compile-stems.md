# stempel-compile-stems

### Name

`analysis-stempel-compile-stems` - Compiles a stemmer table for the Egothor stemmer in the Lucene.Net.Analysis.Stempel project.

### Synopsis

```console
lucene analysis stempel-compile-stems <STEMMING_ALGORITHM> <STEMMER_TABLE_FILE> [-e|--encoding] [?|-h|--help]
```

### Description

See the [Egothor project documentation](http://egothor.sourceforge.net/) for more information.

### Arguments

`STEMMING_ALGORITHM`

The name of the desired stemming algorithm to use. Possible values are `Multi` (which changes the stemmer to use the  MultiTrie2 rather than a Trie class to store its data) or `0` which instructs the stemmer to store the original data. Any other supplied value will use the default algorithm. See the [Egothor project documentation](http://egothor.sourceforge.net/) for more information.

`STEMMER_TABLE_FILE`

The path to a file containing a stemmer table. Multiple values can be supplied separated by a space.

### Options

`?|-h|--help`

Prints out a short help for the command.

`-e|--encoding <ENCODING>`

The file encoding used by the stemmer files. If not supplied, the default value is `UTF-8`. Note this value can alternatively be supplied by setting the environment variable `egothor.stemmer.charset`.

### Example

```console
lucene analysis stempel-compile-stems test X:\stemmer-data\table1.txt X:\stemmer-data\table2.txt
```
