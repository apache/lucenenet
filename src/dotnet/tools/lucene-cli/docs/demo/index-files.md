# index-files

### Name

`demo-index-files` - Index all files under a directory.

### Synopsis

```console
lucene demo index-files <INDEX_DIRECTORY> <SOURCE_DIRECTORY> [-u|--update] [?|-h|--help]
lucene demo index-files [-src|--view-source-code] [-out|--output-source-code]
```

### Description

This demo can be used to learn how to build a Lucene.Net index. After the index has been built, you can run the [search-files demo](search-files.md) to run queries against it.

### Arguments

`INDEX_DIRECTORY`

The directory of the index.

`SOURCE_DIRECTORY`

The source directory containing the files to index. This directory will be analyzed recursively.

### Options

`?|-h|--help`

Prints out a short help for the command.

`-u|--update`

Adds new documents to an existing index. If not supplied, any existing index in the `INDEX_DIRECTORY` will be overwritten.

`-src|--view-source-code`

Prints the source code to the console. Use `SPACE` or `n` to move to the next page of text, `ENTER` to scroll to the next line of text, `q` or `x` to quit.

`-out|--output-source-code <DIRECTORY>`

Outputs the source code to the specified directory.

### Example

Indexes the contents of `C:\Users\BGates\Documents\` and places the Lucene.Net index in `X:\test-index\`.

```console
lucene demo index-files X:\test-index C:\Users\BGates\Documents
```

