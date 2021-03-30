# list-cfs

### Name

`index-list-cfs` - Lists sub-files from a `.cfs` compound file.

### Synopsis

```console
lucene index list-cfs <CFS_FILE_NAME> [-dir|--directory-type] [?|-h|--help]
```

### Description

Prints the filename and size of each file within a given `.cfs` compound file. The .cfs compound file format is created using the CompoundFileDirectory from Lucene.Net.Misc.

### Arguments

`CFS_FILE_NAME`

The `.cfs` compound file containing words to parse.

### Options

`?|-h|--help`

Prints out a short help for the command.

`-dir|--directory-type <DIRECTORY_TYPE>`

The `FSDirectory` implementation to use. If omitted, defaults to the optimal `FSDirectory` for your OS platform.

### Example

Lists the files within the `X:\categories\_53.cfs` compound file using the `NIOFSDirectory` directory implementation:

```console
lucene index list-cfs X:\categories\_53.cfs -dir NIOFSDirectory
```

