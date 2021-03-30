# list-taxonomy-stats

### Name

`index-list-taxonomy-stats` - Displays the taxonomy statistical information for a taxonomy index.

### Synopsis

```console
lucene index list-taxonomy-stats [<INDEX_DIRECTORY>] [-tree|--show-tree] [?|-h|--help]
```

### Description

Prints how many ords are under each dimension.

### Arguments

`INDEX_DIRECTORY`

The directory of the index. If omitted, it defaults to the current working directory.

> [!NOTE] 
> This directory must be a facet taxonomy directory for the command to succeed.

### Options

`?|-h|--help`

Prints out a short help for the command.

`-tree|--show-tree`

Recursively lists all descendant nodes.

### Example

List the taxonomy statistics from the index located at `X:\category-taxonomy-index\`, viewing all descendant nodes:

```console
lucene index list-taxonomy-stats X:\category-taxonomy-index -tree
```

