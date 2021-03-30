# stempel-patch-stems

### Name

`analysis-stempel-patch-stems` - Generates patch commands from an already prepared stemmer table for the Egothor stemmer in the Lucene.Net.Analysis.Stempel project.

### Synopsis

```console
lucene analysis stempel-patch-stems <STEMMER_TABLE_FILE> [-e|--encoding] [?|-h|--help]
```

### Description

See the [Egothor project documentation](http://egothor.sourceforge.net/) for more information.

### Arguments

`STEMMER_TABLE_FILE`

The path to a file containing a stemmer table. Multiple values can be supplied separated by a space.

### Options

`?|-h|--help`

Prints out a short help for the command.

`-e|--encoding <ENCODING>`

The file encoding used by the stemmer files. If not supplied, the default value is `UTF-8`. Note this value can alternatively be supplied by setting the environment variable `egothor.stemmer.charset`.

### Example

```console
lucene analysis stempel-patch-stems X:\stemmer-data\table1.txt X:\stemmer-data\table2.txt --encoding UTF-16
```

