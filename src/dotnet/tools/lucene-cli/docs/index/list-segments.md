# list-segments

### Name

`index-list-segments` - Lists segments in an index.

### Synopsis

```console
lucene index list-segments [\<INDEX_DIRECTORY>] [?|-h|--help]
```

### Description

After running this command to view segments, use [copy-segments](copy-segments.md) to copy segments from one index directory to another or [delete-segments](delete-segments.md) to remove segments from an index.

### Arguments

`INDEX_DIRECTORY`

The directory of the index. If omitted, it defaults to the current working directory.

### Options

`?|-h|--help`

Prints out a short help for the command.

### Example

List the segments in the index located at `X:\lucene-index\`:

```console
lucene index list-segments X:\lucene-index
```

