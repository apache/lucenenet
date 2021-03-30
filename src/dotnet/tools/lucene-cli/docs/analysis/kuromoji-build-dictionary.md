# kuromoji-build-dictionary

### Name

`analysis-kuromoji-build-dictionary` - Generates a set of custom dictionary files for the Lucene.Net.Analysis.Kuromoji library.

### Synopsis

```console
lucene analysis kuromoji-build-dictionary <FORMAT> <INPUT_DIRECTORY> <OUTPUT_DIRECTORY> [-e|--encoding] [-n|--normalize] [?|-h|--help]
```

### Description

Generates the following set of binary files:

- CharacterDefinition.dat
- ConnectionCosts.dat
- TokenInfoDictionary$buffer.dat
- TokenInfoDictionary$fst.dat
- TokenInfoDictionary$posDict.dat
- TokenInfoDictionary$targetMap.dat
- UnknownDictionary$buffer.dat
- UnknownDictionary$posDict.dat
- UnknownDictionary$targetMap.dat

If these files are placed into a subdirectory of your application named `kuromoji-data`, they will be used automatically by Lucene.Net.Analysis.Kuromoji features such as the JapaneseAnalyzer or JapaneseTokenizer. To use an alternate directory location, put the path in an environment variable named `kuromoji.data.dir`. The files must be placed in a subdirectory of this location named `kuromoji-data`.

See [this blog post](http://mentaldetritus.blogspot.com/2013/03/compiling-custom-dictionary-for.html) for information about the dictionary format. A sample is available at [https://sourceforge.net/projects/mecab/files/mecab-ipadic/2.7.0-20070801/](https://sourceforge.net/projects/mecab/files/mecab-ipadic/2.7.0-20070801/). The [Kuromoji project documentation](https://github.com/atilika/kuromoji) may also be helpful. 

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

The file encoding used by the input files. If not supplied, the default value is `EUC-JP`.

`-n|--normalize`

Normalize the entries using normalization form KC.

### Example

```console
lucene analysis kuromoji-build-dictionary IPADIC X:\kuromoji-data X:\kuromoji-dictionary --normalize
```

