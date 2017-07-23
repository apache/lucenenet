# kuromoji-build-dictionary

### Name

`analysis-kuromoji-build-dictionary` - Generates a dictionary file for the JapaneseAnalyzer or JapaneseTokenizer in the Lucene.Net.Analysis.Kuromoji project.

### Synopsis

<code>dotnet lucene-cli.dll analysis kuromoji-build-dictionary <FORMAT> <INPUT_DIRECTORY> <OUTPUT_DIRECTORY> [-e|--encoding] [-n|--normalize] [?|-h|--help]</code>

### Description

See the [Kuromoji project documentation](https://github.com/atilika/kuromoji) for more information.

### Arguments

`FORMAT`

The dictionary format. Valid values are IPADIC and UNIDIC. If an invalid value is passed, IPADIC is assumed.

`INPUT_DIRECTORY`

The directory where the dictionary input files are located.

`OUTPUT_DIRECTORY`

The directory to put the dictionary output.

### Options

`?|-h|--help`

Prints out a short help for the command.

`-e|--encoding <ENCODING>`

The file encoding used by the input files. If not supplied, the default value is `UTF-8`.

`-n|--normalize`

Normalize the entries using normalization form KC.

### Example

<code>dotnet lucene-cli.dll analysis kuromoji-build-dictionary X:\kuromoji-data X:\kuromoji-dictionary --encoding UTF-16</code>

