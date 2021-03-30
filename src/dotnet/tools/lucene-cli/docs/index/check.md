# check

### Name

`index-check` - Checks an index for problematic segments.

### Synopsis

```console
lucene index check [<INDEX_DIRECTORY>] [-v|--verbose] [-c|--cross-check-term-vectors] [-dir|--directory-type] [-s|--segment] [?|-h|--help]
```

### Description

Basic tool to check the health of an index. 

As this tool checks every byte in the index, on a large index it can take quite a long time to run.

### Arguments

`INDEX_DIRECTORY`

The path to the directory of the index to check. If omitted, it defaults to the current working directory.

### Options

`?|-h|--help`

Prints out a short help for the command.

`-v|--verbose`

Enable verbose output.

`-c|--cross-check-term-vectors`

Cross-check term vectors.

`-dir|--directory-type <DIRECTORY_TYPE>`

The FSDirectory implementation to use. If ommitted, it defaults to the optimal FSDirectory for your OS platform.

`-s|--segment <SEGMENT>`

Only check the specified segment(s). This can be specified multiple times, to check more than one segment, eg --segment _2 --segment _a.

### Examples

Check the index located at `X:\lucenenet-index\` verbosely, scanning only the segments named `_1j_Lucene41_0` and `_2u_Lucene41_0` for problems:

```console
lucene index check X:\lucenenet-index -v -s _1j_Lucene41_0 -s _2u_Lucene41_0
```


Check the index located at `C:\taxonomy\` using the `MMapDirectory` memory-mapped directory implementation:

```console
lucene index check C:\taxonomy --directory-type MMapDirectory
```

