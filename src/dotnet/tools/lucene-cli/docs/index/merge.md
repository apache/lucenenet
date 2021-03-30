# merge

### Name

`index-merge` - Merges multiple indexes into a single index.

### Synopsis

```console
lucene index merge <OUTPUT_DIRECTORY> <INPUT_DIRECTORY> <INPUT_DIRECTORY_2>[ <INPUT_DIRECTORY_N>...] [?|-h|--help]
```

### Description

Merges the the input index directories into a combined index at the output directory path.

### Arguments

`OUTPUT_DIRECTORY`

The output directory to merge the input indexes into.

`INPUT_DIRECTORY, INPUT_DIRECTORY_2, INPUT_DIRECTORY_N`

Two or more input index directories, separated by a space.

### Options

`?|-h|--help`

Prints out a short help for the command.

### Example

Merge the indexes `C:\product-index1` and `C:\product-index2` into an index located at `X:\merged-index`:

```console
lucene index merge X:\merged-index C:\product-index1 C:\product-index2
```

