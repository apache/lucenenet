# delete-segments

### Name

`index-delete-segments` - Deletes segments from an index.

### Synopsis

```console
lucene index delete-segments <INDEX_DIRECTORY> <SEGMENT>[ <SEGMENT_2>...] [?|-h|--help]
```

### Description

Deletes segments from an index.

> [!WARNING]
> You can easily accidentally remove segments from your index, so be careful! Always make a backup of your index first.

### Arguments

`INDEX_DIRECTORY`

The directory of the index.

`SEGMENT`

The segments to delete, separated by a space.

### Options

`?|-h|--help`

Prints out a short help for the command.

### Example

Delete the segments named `_8c` and `_83` from the index located at `X:\category-data\`:

```console
lucene index delete-segments X:\category-data _8c _83
```
