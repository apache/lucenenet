# extract-cfs

### Name

`index-extract-cfs` - Extracts sub-files from a `.cfs` compound file.

### Synopsis

```console
lucene index extract-cfs <CFS_FILE_NAME> [-dir|--directory-type] [?|-h|--help]
```

### Description

Extracts `.cfs` compound files (that were created using the `CompoundFileDirectory` from Lucene.Net.Misc) to the current working directory.

In order to make the extracted version of the index work, you have to copy the segments file from the compound index into the directory where the extracted files are stored.

### Arguments

`CFS_FILE_NAME`

The path to a `.cfs` compound file containing words to parse.

### Options

`?|-h|--help`

Prints out a short help for the command.

`-dir|--directory-type <DIRECTORY_TYPE>`

The FSDirectory implementation to use. If ommitted, it defaults to the optimal FSDirectory for your OS platform.

### Examples

Extract the files from the compound file at `X:\lucene-index\_81.cfs` to the current working directory:

```console
lucene index extract-cfs X:\lucene-index\_81.cfs
```


Extract the files from the compound file at `X:\lucene-index\_64.cfs` to the current working directory using the `SimpleFSDirectory` implementation:

```console
lucene index extract-cfs X:\lucene-index\_64.cfs --directory-type SimpleFSDirectory
```
