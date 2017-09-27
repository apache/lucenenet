# extract-reuters

### Name

`benchmark-extract-reuters` - Splits Reuters SGML documents into simple text files containing: Title, Date, Dateline, Body.

### Synopsis

<code>dotnet lucene-cli.dll benchmark extract-reuters [?|-h|--help]</code>

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

<code>dotnet lucene-cli.dll benchmark extract-reuters z:\input z:\output</code>
