# find-quality-queries

### Name

`benchmark-find-quality-queries` - Suggests quality queries based on index contents. Used for making quality test benchmarks.

### Synopsis

<code>dotnet lucene-cli.dll benchmark find-quality-queries [?|-h|--help]</code>

### Arguments

`INDEX_DIRECTORY`

Path to the index.

### Options

`?|-h|--help`

Prints out a short help for the command.

### Example

Finds quality queries on the `c:\lucene-index` index directory.

<code>dotnet lucene-cli.dll benchmark find-quality-queries c:\lucene-index</code>
