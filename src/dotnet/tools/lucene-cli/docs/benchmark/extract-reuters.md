# extract-reuters

### Name

`benchmark-extract-reuters` - Splits Reuters SGML documents into simple text files containing: Title, Date, Dateline, Body.

### Synopsis

```console
lucene benchmark extract-reuters <INPUT_DIRECTORY> <OUTPUT_DIRECTORY> [?|-h|--help]
```

### Arguments

`INPUT_DIRECTORY`

Path to Reuters SGML files.

`OUTPUT_DIRECTORY`

Path to a directory where the output files will be written.

### Options

`?|-h|--help`

Prints out a short help for the command.

### Example

Extracts the reuters SGML files in the `z:\input` directory and places the content in the `z:\output` directory.

```console
lucene benchmark extract-reuters z:\input z:\output
```
