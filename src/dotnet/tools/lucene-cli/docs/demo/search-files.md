# search-files

### Name

`demo-search-files` - Simple command-line based search demo.

### Synopsis

```console
lucene demo search-files <INDEX_DIRECTORY> [-f|--field] [-r|--repeat] [-qf|--queries-file] [-q|--query] [--raw] [-p|--page-size] [?|-h|--help]
lucene demo search-files [-src|--view-source-code] [-out|--output-source-code]
```

### Description

Run the [index-files demo](index-files.md) first to generate an index to search.

> NOTE: To run queries interactively in the console, omit both the `--queries-file` and the `--query` arguments.

### Arguments

`INDEX_DIRECTORY`

The directory of the index that has previously been created using the [index-files demo](index-files.md).

### Options

`?|-h|--help`

Prints out a short help for the command.

`-f|--field <FIELD>`

The index field to use in the search. If not supplied, defaults to `contents`.

`-r|--repeat <NUMBER>`

Repeat the search and time as a benchmark.

`-qf|--queries-file <PATH>`

A file containing the queries to perform.

`-q|--query <QUERY>`

A query to perform.

`--raw`

Output raw format.

`-p|--page-size <NUMBER>`

Hits per page to display.

`-src|--view-source-code`

Prints the source code to the console. Use `SPACE` or `n` to move to the next page of text, `ENTER` to scroll to the next line of text, `q` or `x` to quit.

`-out|--output-source-code <DIRECTORY>`

Outputs the source code to the specified directory.

### Examples

Search the index located in the `X:\test-index` directory interactively, showing 15 results per page in raw format:

```console
lucene demo search-files X:\test-index -p 15 --raw
```

Run the query "foobar" against the "path" field in the index located in the `X:\test-index` directory:

```console
lucene demo search-files X:\test-index --field path --query foobar
```
