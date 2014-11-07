using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace org.apache.lucene.analysis.hunspell
{

	/*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements.  See the NOTICE file distributed with
	 * this work for additional information regarding copyright ownership.
	 * The ASF licenses this file to You under the Apache License, Version 2.0
	 * (the "License"); you may not use this file except in compliance with
	 * the License.  You may obtain a copy of the License at
	 *
	 *     http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */

	using ByteArrayDataOutput = org.apache.lucene.store.ByteArrayDataOutput;
	using ArrayUtil = org.apache.lucene.util.ArrayUtil;
	using BytesRef = org.apache.lucene.util.BytesRef;
	using BytesRefHash = org.apache.lucene.util.BytesRefHash;
	using CharsRef = org.apache.lucene.util.CharsRef;
	using IOUtils = org.apache.lucene.util.IOUtils;
	using IntsRef = org.apache.lucene.util.IntsRef;
	using OfflineSorter = org.apache.lucene.util.OfflineSorter;
	using ByteSequencesReader = org.apache.lucene.util.OfflineSorter.ByteSequencesReader;
	using ByteSequencesWriter = org.apache.lucene.util.OfflineSorter.ByteSequencesWriter;
	using CharacterRunAutomaton = org.apache.lucene.util.automaton.CharacterRunAutomaton;
	using RegExp = org.apache.lucene.util.automaton.RegExp;
	using Builder = org.apache.lucene.util.fst.Builder;
	using CharSequenceOutputs = org.apache.lucene.util.fst.CharSequenceOutputs;
	using FST = org.apache.lucene.util.fst.FST;
	using IntSequenceOutputs = org.apache.lucene.util.fst.IntSequenceOutputs;
	using Outputs = org.apache.lucene.util.fst.Outputs;
	using Util = org.apache.lucene.util.fst.Util;


	/// <summary>
	/// In-memory structure for the dictionary (.dic) and affix (.aff)
	/// data of a hunspell dictionary.
	/// </summary>
	public class Dictionary
	{

	  internal static readonly char[] NOFLAGS = new char[0];

	  private const string ALIAS_KEY = "AF";
	  private const string PREFIX_KEY = "PFX";
	  private const string SUFFIX_KEY = "SFX";
	  private const string FLAG_KEY = "FLAG";
	  private const string COMPLEXPREFIXES_KEY = "COMPLEXPREFIXES";
	  private const string CIRCUMFIX_KEY = "CIRCUMFIX";
	  private const string IGNORE_KEY = "IGNORE";
	  private const string ICONV_KEY = "ICONV";
	  private const string OCONV_KEY = "OCONV";

	  private const string NUM_FLAG_TYPE = "num";
	  private const string UTF8_FLAG_TYPE = "UTF-8";
	  private const string LONG_FLAG_TYPE = "long";

	  // TODO: really for suffixes we should reverse the automaton and run them backwards
	  private const string PREFIX_CONDITION_REGEX_PATTERN = "%s.*";
	  private const string SUFFIX_CONDITION_REGEX_PATTERN = ".*%s";

	  internal FST<IntsRef> prefixes;
	  internal FST<IntsRef> suffixes;

	  // all condition checks used by prefixes and suffixes. these are typically re-used across
	  // many affix stripping rules. so these are deduplicated, to save RAM.
	  internal List<CharacterRunAutomaton> patterns = new List<CharacterRunAutomaton>();

	  // the entries in the .dic file, mapping to their set of flags.
	  // the fst output is the ordinal list for flagLookup
	  internal FST<IntsRef> words;
	  // the list of unique flagsets (wordforms). theoretically huge, but practically
	  // small (e.g. for polish this is 756), otherwise humans wouldn't be able to deal with it either.
	  internal BytesRefHash flagLookup = new BytesRefHash();

	  // the list of unique strip affixes.
	  internal char[] stripData;
	  internal int[] stripOffsets;

	  // 8 bytes per affix
	  internal sbyte[] affixData = new sbyte[64];
	  private int currentAffix = 0;

	  private FlagParsingStrategy flagParsingStrategy = new SimpleFlagParsingStrategy(); // Default flag parsing strategy

	  private string[] aliases;
	  private int aliasCount = 0;

	  private readonly File tempDir = OfflineSorter.defaultTempDir(); // TODO: make this configurable?

	  internal bool ignoreCase;
	  internal bool complexPrefixes;
	  internal bool twoStageAffix; // if no affixes have continuation classes, no need to do 2-level affix stripping

	  internal int circumfix = -1; // circumfix flag, or -1 if one is not defined

	  // ignored characters (dictionary, affix, inputs)
	  private char[] ignore;

	  // FSTs used for ICONV/OCONV, output ord pointing to replacement text
	  internal FST<CharsRef> iconv;
	  internal FST<CharsRef> oconv;

	  internal bool needsInputCleaning;
	  internal bool needsOutputCleaning;

	  /// <summary>
	  /// Creates a new Dictionary containing the information read from the provided InputStreams to hunspell affix
	  /// and dictionary files.
	  /// You have to close the provided InputStreams yourself.
	  /// </summary>
	  /// <param name="affix"> InputStream for reading the hunspell affix file (won't be closed). </param>
	  /// <param name="dictionary"> InputStream for reading the hunspell dictionary file (won't be closed). </param>
	  /// <exception cref="IOException"> Can be thrown while reading from the InputStreams </exception>
	  /// <exception cref="ParseException"> Can be thrown if the content of the files does not meet expected formats </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public Dictionary(java.io.InputStream affix, java.io.InputStream dictionary) throws java.io.IOException, java.text.ParseException
	  public Dictionary(InputStream affix, InputStream dictionary) : this(affix, Collections.singletonList(dictionary), false)
	  {
	  }

	  /// <summary>
	  /// Creates a new Dictionary containing the information read from the provided InputStreams to hunspell affix
	  /// and dictionary files.
	  /// You have to close the provided InputStreams yourself.
	  /// </summary>
	  /// <param name="affix"> InputStream for reading the hunspell affix file (won't be closed). </param>
	  /// <param name="dictionaries"> InputStream for reading the hunspell dictionary files (won't be closed). </param>
	  /// <exception cref="IOException"> Can be thrown while reading from the InputStreams </exception>
	  /// <exception cref="ParseException"> Can be thrown if the content of the files does not meet expected formats </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public Dictionary(java.io.InputStream affix, java.util.List<java.io.InputStream> dictionaries, boolean ignoreCase) throws java.io.IOException, java.text.ParseException
	  public Dictionary(InputStream affix, IList<InputStream> dictionaries, bool ignoreCase)
	  {
		this.ignoreCase = ignoreCase;
		this.needsInputCleaning = ignoreCase;
		this.needsOutputCleaning = false; // set if we have an OCONV
		flagLookup.add(new BytesRef()); // no flags -> ord 0

		File aff = File.createTempFile("affix", "aff", tempDir);
		OutputStream @out = new BufferedOutputStream(new FileOutputStream(aff));
		InputStream aff1 = null;
		InputStream aff2 = null;
		try
		{
		  // copy contents of affix stream to temp file
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte [] buffer = new byte [1024 * 8];
		  sbyte[] buffer = new sbyte [1024 * 8];
		  int len;
		  while ((len = affix.read(buffer)) > 0)
		  {
			@out.write(buffer, 0, len);
		  }
		  @out.close();

		  // pass 1: get encoding
		  aff1 = new BufferedInputStream(new FileInputStream(aff));
		  string encoding = getDictionaryEncoding(aff1);

		  // pass 2: parse affixes
		  CharsetDecoder decoder = getJavaEncoding(encoding);
		  aff2 = new BufferedInputStream(new FileInputStream(aff));
		  readAffixFile(aff2, decoder);

		  // read dictionary entries
		  IntSequenceOutputs o = IntSequenceOutputs.Singleton;
		  Builder<IntsRef> b = new Builder<IntsRef>(FST.INPUT_TYPE.BYTE4, o);
		  readDictionaryFiles(dictionaries, decoder, b);
		  words = b.finish();
		  aliases = null; // no longer needed
		}
		finally
		{
		  IOUtils.closeWhileHandlingException(@out, aff1, aff2);
		  aff.delete();
		}
	  }

	  /// <summary>
	  /// Looks up Hunspell word forms from the dictionary
	  /// </summary>
	  internal virtual IntsRef lookupWord(char[] word, int offset, int length)
	  {
		return lookup(words, word, offset, length);
	  }

	  /// <summary>
	  /// Looks up HunspellAffix prefixes that have an append that matches the String created from the given char array, offset and length
	  /// </summary>
	  /// <param name="word"> Char array to generate the String from </param>
	  /// <param name="offset"> Offset in the char array that the String starts at </param>
	  /// <param name="length"> Length from the offset that the String is </param>
	  /// <returns> List of HunspellAffix prefixes with an append that matches the String, or {@code null} if none are found </returns>
	  internal virtual IntsRef lookupPrefix(char[] word, int offset, int length)
	  {
		return lookup(prefixes, word, offset, length);
	  }

	  /// <summary>
	  /// Looks up HunspellAffix suffixes that have an append that matches the String created from the given char array, offset and length
	  /// </summary>
	  /// <param name="word"> Char array to generate the String from </param>
	  /// <param name="offset"> Offset in the char array that the String starts at </param>
	  /// <param name="length"> Length from the offset that the String is </param>
	  /// <returns> List of HunspellAffix suffixes with an append that matches the String, or {@code null} if none are found </returns>
	  internal virtual IntsRef lookupSuffix(char[] word, int offset, int length)
	  {
		return lookup(suffixes, word, offset, length);
	  }

	  // TODO: this is pretty stupid, considering how the stemming algorithm works
	  // we can speed it up to be significantly faster!
	  internal virtual IntsRef lookup(FST<IntsRef> fst, char[] word, int offset, int length)
	  {
		if (fst == null)
		{
		  return null;
		}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.fst.FST.BytesReader bytesReader = fst.getBytesReader();
		FST.BytesReader bytesReader = fst.BytesReader;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.fst.FST.Arc<org.apache.lucene.util.IntsRef> arc = fst.getFirstArc(new org.apache.lucene.util.fst.FST.Arc<org.apache.lucene.util.IntsRef>());
		FST.Arc<IntsRef> arc = fst.getFirstArc(new FST.Arc<IntsRef>());
		// Accumulate output as we go
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.IntsRef NO_OUTPUT = fst.outputs.getNoOutput();
		IntsRef NO_OUTPUT = fst.outputs.NoOutput;
		IntsRef output = NO_OUTPUT;

		int l = offset + length;
		try
		{
		  for (int i = offset, cp = 0; i < l; i += char.charCount(cp))
		  {
			cp = char.codePointAt(word, i, l);
			if (fst.findTargetArc(cp, arc, arc, bytesReader) == null)
			{
			  return null;
			}
			else if (arc.output != NO_OUTPUT)
			{
			  output = fst.outputs.add(output, arc.output);
			}
		  }
		  if (fst.findTargetArc(FST.END_LABEL, arc, arc, bytesReader) == null)
		  {
			return null;
		  }
		  else if (arc.output != NO_OUTPUT)
		  {
			return fst.outputs.add(output, arc.output);
		  }
		  else
		  {
			return output;
		  }
		}
		catch (IOException bogus)
		{
		  throw new Exception(bogus);
		}
	  }

	  /// <summary>
	  /// Reads the affix file through the provided InputStream, building up the prefix and suffix maps
	  /// </summary>
	  /// <param name="affixStream"> InputStream to read the content of the affix file from </param>
	  /// <param name="decoder"> CharsetDecoder to decode the content of the file </param>
	  /// <exception cref="IOException"> Can be thrown while reading from the InputStream </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void readAffixFile(java.io.InputStream affixStream, java.nio.charset.CharsetDecoder decoder) throws java.io.IOException, java.text.ParseException
	  private void readAffixFile(InputStream affixStream, CharsetDecoder decoder)
	  {
		SortedDictionary<string, IList<char?>> prefixes = new SortedDictionary<string, IList<char?>>();
		SortedDictionary<string, IList<char?>> suffixes = new SortedDictionary<string, IList<char?>>();
		IDictionary<string, int?> seenPatterns = new Dictionary<string, int?>();

		// zero condition -> 0 ord
		seenPatterns[".*"] = 0;
		patterns.Add(null);

		// zero strip -> 0 ord
		IDictionary<string, int?> seenStrips = new LinkedHashMap<string, int?>();
		seenStrips[""] = 0;

		LineNumberReader reader = new LineNumberReader(new InputStreamReader(affixStream, decoder));
		string line = null;
		while ((line = reader.readLine()) != null)
		{
		  // ignore any BOM marker on first line
		  if (reader.LineNumber == 1 && line.StartsWith("\uFEFF", StringComparison.Ordinal))
		  {
			line = line.Substring(1);
		  }
		  if (line.StartsWith(ALIAS_KEY, StringComparison.Ordinal))
		  {
			parseAlias(line);
		  }
		  else if (line.StartsWith(PREFIX_KEY, StringComparison.Ordinal))
		  {
			parseAffix(prefixes, line, reader, PREFIX_CONDITION_REGEX_PATTERN, seenPatterns, seenStrips);
		  }
		  else if (line.StartsWith(SUFFIX_KEY, StringComparison.Ordinal))
		  {
			parseAffix(suffixes, line, reader, SUFFIX_CONDITION_REGEX_PATTERN, seenPatterns, seenStrips);
		  }
		  else if (line.StartsWith(FLAG_KEY, StringComparison.Ordinal))
		  {
			// Assume that the FLAG line comes before any prefix or suffixes
			// Store the strategy so it can be used when parsing the dic file
			flagParsingStrategy = getFlagParsingStrategy(line);
		  }
		  else if (line.Equals(COMPLEXPREFIXES_KEY))
		  {
			complexPrefixes = true; // 2-stage prefix+1-stage suffix instead of 2-stage suffix+1-stage prefix
		  }
		  else if (line.StartsWith(CIRCUMFIX_KEY, StringComparison.Ordinal))
		  {
			string[] parts = line.Split("\\s+", true);
			if (parts.Length != 2)
			{
			  throw new ParseException("Illegal CIRCUMFIX declaration", reader.LineNumber);
			}
			circumfix = flagParsingStrategy.parseFlag(parts[1]);
		  }
		  else if (line.StartsWith(IGNORE_KEY, StringComparison.Ordinal))
		  {
			string[] parts = line.Split("\\s+", true);
			if (parts.Length != 2)
			{
			  throw new ParseException("Illegal IGNORE declaration", reader.LineNumber);
			}
			ignore = parts[1].ToCharArray();
			Arrays.sort(ignore);
			needsInputCleaning = true;
		  }
		  else if (line.StartsWith(ICONV_KEY, StringComparison.Ordinal) || line.StartsWith(OCONV_KEY, StringComparison.Ordinal))
		  {
			string[] parts = line.Split("\\s+", true);
			string type = parts[0];
			if (parts.Length != 2)
			{
			  throw new ParseException("Illegal " + type + " declaration", reader.LineNumber);
			}
			int num = int.Parse(parts[1]);
			FST<CharsRef> res = parseConversions(reader, num);
			if (type.Equals("ICONV"))
			{
			  iconv = res;
			  needsInputCleaning |= iconv != null;
			}
			else
			{
			  oconv = res;
			  needsOutputCleaning |= oconv != null;
			}
		  }
		}

		this.prefixes = affixFST(prefixes);
		this.suffixes = affixFST(suffixes);

		int totalChars = 0;
		foreach (string strip in seenStrips.Keys)
		{
		  totalChars += strip.Length;
		}
		stripData = new char[totalChars];
		stripOffsets = new int[seenStrips.Count + 1];
		int currentOffset = 0;
		int currentIndex = 0;
		foreach (string strip in seenStrips.Keys)
		{
		  stripOffsets[currentIndex++] = currentOffset;
		  strip.CopyTo(0, stripData, currentOffset, strip.Length - 0);
		  currentOffset += strip.Length;
		}
		Debug.Assert(currentIndex == seenStrips.Count);
		stripOffsets[currentIndex] = currentOffset;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private org.apache.lucene.util.fst.FST<org.apache.lucene.util.IntsRef> affixFST(java.util.TreeMap<String,java.util.List<Character>> affixes) throws java.io.IOException
	  private FST<IntsRef> affixFST(SortedDictionary<string, IList<char?>> affixes)
	  {
		IntSequenceOutputs outputs = IntSequenceOutputs.Singleton;
		Builder<IntsRef> builder = new Builder<IntsRef>(FST.INPUT_TYPE.BYTE4, outputs);

		IntsRef scratch = new IntsRef();
		foreach (KeyValuePair<string, IList<char?>> entry in affixes.SetOfKeyValuePairs())
		{
		  Util.toUTF32(entry.Key, scratch);
		  IList<char?> entries = entry.Value;
		  IntsRef output = new IntsRef(entries.Count);
		  foreach (char? c in entries)
		  {
			output.ints[output.length++] = c;
		  }
		  builder.add(scratch, output);
		}
		return builder.finish();
	  }

	  /// <summary>
	  /// Parses a specific affix rule putting the result into the provided affix map
	  /// </summary>
	  /// <param name="affixes"> Map where the result of the parsing will be put </param>
	  /// <param name="header"> Header line of the affix rule </param>
	  /// <param name="reader"> BufferedReader to read the content of the rule from </param>
	  /// <param name="conditionPattern"> <seealso cref="String#format(String, Object...)"/> pattern to be used to generate the condition regex
	  ///                         pattern </param>
	  /// <param name="seenPatterns"> map from condition -> index of patterns, for deduplication. </param>
	  /// <exception cref="IOException"> Can be thrown while reading the rule </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void parseAffix(java.util.TreeMap<String,java.util.List<Character>> affixes, String header, java.io.LineNumberReader reader, String conditionPattern, java.util.Map<String,Integer> seenPatterns, java.util.Map<String,Integer> seenStrips) throws java.io.IOException, java.text.ParseException
	  private void parseAffix(SortedDictionary<string, IList<char?>> affixes, string header, LineNumberReader reader, string conditionPattern, IDictionary<string, int?> seenPatterns, IDictionary<string, int?> seenStrips)
	  {

		BytesRef scratch = new BytesRef();
		StringBuilder sb = new StringBuilder();
		string[] args = header.Split("\\s+", true);

		bool crossProduct = args[2].Equals("Y");

		int numLines = int.Parse(args[3]);
		affixData = ArrayUtil.grow(affixData, (currentAffix << 3) + (numLines << 3));
		ByteArrayDataOutput affixWriter = new ByteArrayDataOutput(affixData, currentAffix << 3, numLines << 3);

		for (int i = 0; i < numLines; i++)
		{
		  Debug.Assert(affixWriter.Position == currentAffix << 3);
		  string line = reader.readLine();
		  string[] ruleArgs = line.Split("\\s+", true);

		  // from the manpage: PFX flag stripping prefix [condition [morphological_fields...]]
		  // condition is optional
		  if (ruleArgs.Length < 4)
		  {
			  throw new ParseException("The affix file contains a rule with less than four elements: " + line, reader.LineNumber);
		  }

		  char flag = flagParsingStrategy.parseFlag(ruleArgs[1]);
		  string strip = ruleArgs[2].Equals("0") ? "" : ruleArgs[2];
		  string affixArg = ruleArgs[3];
		  char[] appendFlags = null;

		  int flagSep = affixArg.LastIndexOf('/');
		  if (flagSep != -1)
		  {
			string flagPart = affixArg.Substring(flagSep + 1);
			affixArg = affixArg.Substring(0, flagSep);

			if (aliasCount > 0)
			{
			  flagPart = getAliasValue(int.Parse(flagPart));
			}

			appendFlags = flagParsingStrategy.parseFlags(flagPart);
			Arrays.sort(appendFlags);
			twoStageAffix = true;
		  }

		  // TODO: add test and fix zero-affix handling!

		  string condition = ruleArgs.Length > 4 ? ruleArgs[4] : ".";
		  // at least the gascon affix file has this issue
		  if (condition.StartsWith("[", StringComparison.Ordinal) && !condition.EndsWith("]", StringComparison.Ordinal))
		  {
			condition = condition + "]";
		  }
		  // "dash hasn't got special meaning" (we must escape it)
		  if (condition.IndexOf('-') >= 0)
		  {
			condition = condition.Replace("-", "\\-");
		  }

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String regex;
		  string regex;
		  if (".".Equals(condition))
		  {
			regex = ".*"; // Zero condition is indicated by dot
		  }
		  else if (condition.Equals(strip))
		  {
			regex = ".*"; // TODO: optimize this better:
						  // if we remove 'strip' from condition, we don't have to append 'strip' to check it...!
						  // but this is complicated...
		  }
		  else
		  {
			regex = string.format(Locale.ROOT, conditionPattern, condition);
		  }

		  // deduplicate patterns
		  int? patternIndex = seenPatterns[regex];
		  if (patternIndex == null)
		  {
			patternIndex = patterns.Count;
			if (patternIndex > short.MaxValue)
			{
			  throw new System.NotSupportedException("Too many patterns, please report this to dev@lucene.apache.org");
			}
			seenPatterns[regex] = patternIndex;
			CharacterRunAutomaton pattern = new CharacterRunAutomaton((new RegExp(regex, RegExp.NONE)).toAutomaton());
			patterns.Add(pattern);
		  }

		  int? stripOrd = seenStrips[strip];
		  if (stripOrd == null)
		  {
			stripOrd = seenStrips.Count;
			seenStrips[strip] = stripOrd;
			if (stripOrd > Char.MaxValue)
			{
			  throw new System.NotSupportedException("Too many unique strips, please report this to dev@lucene.apache.org");
			}
		  }

		  if (appendFlags == null)
		  {
			appendFlags = NOFLAGS;
		  }

		  encodeFlags(scratch, appendFlags);
		  int appendFlagsOrd = flagLookup.add(scratch);
		  if (appendFlagsOrd < 0)
		  {
			// already exists in our hash
			appendFlagsOrd = (-appendFlagsOrd) - 1;
		  }
		  else if (appendFlagsOrd > short.MaxValue)
		  {
			// this limit is probably flexible, but its a good sanity check too
			throw new System.NotSupportedException("Too many unique append flags, please report this to dev@lucene.apache.org");
		  }

		  affixWriter.writeShort((short)flag);
		  affixWriter.writeShort((int)(short)stripOrd);
		  // encode crossProduct into patternIndex
		  int patternOrd = (int)patternIndex << 1 | (crossProduct ? 1 : 0);
		  affixWriter.writeShort((short)patternOrd);
		  affixWriter.writeShort((short)appendFlagsOrd);

		  if (needsInputCleaning)
		  {
			CharSequence cleaned = cleanInput(affixArg, sb);
			affixArg = cleaned.ToString();
		  }

		  IList<char?> list = affixes[affixArg];
		  if (list == null)
		  {
			list = new List<>();
			affixes[affixArg] = list;
		  }

		  list.Add((char)currentAffix);
		  currentAffix++;
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private org.apache.lucene.util.fst.FST<org.apache.lucene.util.CharsRef> parseConversions(java.io.LineNumberReader reader, int num) throws java.io.IOException, java.text.ParseException
	  private FST<CharsRef> parseConversions(LineNumberReader reader, int num)
	  {
		IDictionary<string, string> mappings = new SortedDictionary<string, string>();

		for (int i = 0; i < num; i++)
		{
		  string line = reader.readLine();
		  string[] parts = line.Split("\\s+", true);
		  if (parts.Length != 3)
		  {
			throw new ParseException("invalid syntax: " + line, reader.LineNumber);
		  }
		  if (mappings.put(parts[1], parts[2]) != null)
		  {
			throw new System.InvalidOperationException("duplicate mapping specified for: " + parts[1]);
		  }
		}

		Outputs<CharsRef> outputs = CharSequenceOutputs.Singleton;
		Builder<CharsRef> builder = new Builder<CharsRef>(FST.INPUT_TYPE.BYTE2, outputs);
		IntsRef scratchInts = new IntsRef();
		foreach (KeyValuePair<string, string> entry in mappings.SetOfKeyValuePairs())
		{
		  Util.toUTF16(entry.Key, scratchInts);
		  builder.add(scratchInts, new CharsRef(entry.Value));
		}

		return builder.finish();
	  }

	  /// <summary>
	  /// pattern accepts optional BOM + SET + any whitespace </summary>
	  internal static readonly Pattern ENCODING_PATTERN = Pattern.compile("^(\u00EF\u00BB\u00BF)?SET\\s+");

	  /// <summary>
	  /// Parses the encoding specified in the affix file readable through the provided InputStream
	  /// </summary>
	  /// <param name="affix"> InputStream for reading the affix file </param>
	  /// <returns> Encoding specified in the affix file </returns>
	  /// <exception cref="IOException"> Can be thrown while reading from the InputStream </exception>
	  /// <exception cref="ParseException"> Thrown if the first non-empty non-comment line read from the file does not adhere to the format {@code SET <encoding>} </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: static String getDictionaryEncoding(java.io.InputStream affix) throws java.io.IOException, java.text.ParseException
	  internal static string getDictionaryEncoding(InputStream affix)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final StringBuilder encoding = new StringBuilder();
		StringBuilder encoding = new StringBuilder();
		for (;;)
		{
		  encoding.Length = 0;
		  int ch;
		  while ((ch = affix.read()) >= 0)
		  {
			if (ch == '\n')
			{
			  break;
			}
			if (ch != '\r')
			{
			  encoding.Append((char)ch);
			}
		  }
		  if (encoding.Length == 0 || encoding[0] == '#' || encoding.ToString().Trim().Length == 0)
		  {
			  // this test only at the end as ineffective but would allow lines only containing spaces:
			if (ch < 0)
			{
			  throw new ParseException("Unexpected end of affix file.", 0);
			}
			continue;
		  }
		  Matcher matcher = ENCODING_PATTERN.matcher(encoding);
		  if (matcher.find())
		  {
			int last = matcher.end();
			return encoding.Substring(last).Trim();
		  }
		}
	  }

	  internal static readonly IDictionary<string, string> CHARSET_ALIASES;
	  static Dictionary()
	  {
		IDictionary<string, string> m = new Dictionary<string, string>();
		m["microsoft-cp1251"] = "windows-1251";
		m["TIS620-2533"] = "TIS-620";
		CHARSET_ALIASES = Collections.unmodifiableMap(m);
	  }

	  /// <summary>
	  /// Retrieves the CharsetDecoder for the given encoding.  Note, This isn't perfect as I think ISCII-DEVANAGARI and
	  /// MICROSOFT-CP1251 etc are allowed...
	  /// </summary>
	  /// <param name="encoding"> Encoding to retrieve the CharsetDecoder for </param>
	  /// <returns> CharSetDecoder for the given encoding </returns>
	  private CharsetDecoder getJavaEncoding(string encoding)
	  {
		if ("ISO8859-14".Equals(encoding))
		{
		  return new ISO8859_14Decoder();
		}
		string canon = CHARSET_ALIASES[encoding];
		if (canon != null)
		{
		  encoding = canon;
		}
		Charset charset = Charset.forName(encoding);
		return charset.newDecoder().onMalformedInput(CodingErrorAction.REPLACE);
	  }

	  /// <summary>
	  /// Determines the appropriate <seealso cref="FlagParsingStrategy"/> based on the FLAG definition line taken from the affix file
	  /// </summary>
	  /// <param name="flagLine"> Line containing the flag information </param>
	  /// <returns> FlagParsingStrategy that handles parsing flags in the way specified in the FLAG definition </returns>
	  internal static FlagParsingStrategy getFlagParsingStrategy(string flagLine)
	  {
		string[] parts = flagLine.Split("\\s+", true);
		if (parts.Length != 2)
		{
		  throw new System.ArgumentException("Illegal FLAG specification: " + flagLine);
		}
		string flagType = parts[1];

		if (NUM_FLAG_TYPE.Equals(flagType))
		{
		  return new NumFlagParsingStrategy();
		}
		else if (UTF8_FLAG_TYPE.Equals(flagType))
		{
		  return new SimpleFlagParsingStrategy();
		}
		else if (LONG_FLAG_TYPE.Equals(flagType))
		{
		  return new DoubleASCIIFlagParsingStrategy();
		}

		throw new System.ArgumentException("Unknown flag type: " + flagType);
	  }

	  internal readonly char FLAG_SEPARATOR = (char)0x1f; // flag separator after escaping

	  internal virtual string unescapeEntry(string entry)
	  {
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < entry.Length; i++)
		{
		  char ch = entry[i];
		  if (ch == '\\' && i + 1 < entry.Length)
		  {
			sb.Append(entry[i + 1]);
			i++;
		  }
		  else if (ch == '/')
		  {
			sb.Append(FLAG_SEPARATOR);
		  }
		  else
		  {
			sb.Append(ch);
		  }
		}
		return sb.ToString();
	  }

	  /// <summary>
	  /// Reads the dictionary file through the provided InputStreams, building up the words map
	  /// </summary>
	  /// <param name="dictionaries"> InputStreams to read the dictionary file through </param>
	  /// <param name="decoder"> CharsetDecoder used to decode the contents of the file </param>
	  /// <exception cref="IOException"> Can be thrown while reading from the file </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void readDictionaryFiles(java.util.List<java.io.InputStream> dictionaries, java.nio.charset.CharsetDecoder decoder, org.apache.lucene.util.fst.Builder<org.apache.lucene.util.IntsRef> words) throws java.io.IOException
	  private void readDictionaryFiles(IList<InputStream> dictionaries, CharsetDecoder decoder, Builder<IntsRef> words)
	  {
		BytesRef flagsScratch = new BytesRef();
		IntsRef scratchInts = new IntsRef();

		StringBuilder sb = new StringBuilder();

		File unsorted = File.createTempFile("unsorted", "dat", tempDir);
		OfflineSorter.ByteSequencesWriter writer = new OfflineSorter.ByteSequencesWriter(unsorted);
		bool success = false;
		try
		{
		  foreach (InputStream dictionary in dictionaries)
		  {
			BufferedReader lines = new BufferedReader(new InputStreamReader(dictionary, decoder));
			string line = lines.readLine(); // first line is number of entries (approximately, sometimes)

			while ((line = lines.readLine()) != null)
			{
			  line = unescapeEntry(line);
			  if (needsInputCleaning)
			  {
				int flagSep = line.LastIndexOf(FLAG_SEPARATOR);
				if (flagSep == -1)
				{
				  CharSequence cleansed = cleanInput(line, sb);
				  writer.write(cleansed.ToString().GetBytes(StandardCharsets.UTF_8));
				}
				else
				{
				  string text = line.Substring(0, flagSep);
				  CharSequence cleansed = cleanInput(text, sb);
				  if (cleansed != sb)
				  {
					sb.Length = 0;
					sb.Append(cleansed);
				  }
				  sb.Append(line.Substring(flagSep));
				  writer.write(sb.ToString().GetBytes(StandardCharsets.UTF_8));
				}
			  }
			  else
			  {
				writer.write(line.GetBytes(StandardCharsets.UTF_8));
			  }
			}
		  }
		  success = true;
		}
		finally
		{
		  if (success)
		  {
			IOUtils.close(writer);
		  }
		  else
		  {
			IOUtils.closeWhileHandlingException(writer);
		  }
		}
		File sorted = File.createTempFile("sorted", "dat", tempDir);

		OfflineSorter sorter = new OfflineSorter(new ComparatorAnonymousInnerClassHelper(this));
		sorter.sort(unsorted, sorted);
		unsorted.delete();

		OfflineSorter.ByteSequencesReader reader = new OfflineSorter.ByteSequencesReader(sorted);
		BytesRef scratchLine = new BytesRef();

		// TODO: the flags themselves can be double-chars (long) or also numeric
		// either way the trick is to encode them as char... but they must be parsed differently

		string currentEntry = null;
		IntsRef currentOrds = new IntsRef();

		string line;
		while (reader.read(scratchLine))
		{
		  line = scratchLine.utf8ToString();
		  string entry;
		  char[] wordForm;

		  int flagSep = line.LastIndexOf(FLAG_SEPARATOR);
		  if (flagSep == -1)
		  {
			wordForm = NOFLAGS;
			entry = line;
		  }
		  else
		  {
			// note, there can be comments (morph description) after a flag.
			// we should really look for any whitespace: currently just tab and space
			int end = line.IndexOf('\t', flagSep);
			if (end == -1)
			{
			  end = line.Length;
			}
			int end2 = line.IndexOf(' ', flagSep);
			if (end2 == -1)
			{
			  end2 = line.Length;
			}
			end = Math.Min(end, end2);

			string flagPart = StringHelperClass.SubstringSpecial(line, flagSep + 1, end);
			if (aliasCount > 0)
			{
			  flagPart = getAliasValue(int.Parse(flagPart));
			}

			wordForm = flagParsingStrategy.parseFlags(flagPart);
			Arrays.sort(wordForm);
			entry = line.Substring(0, flagSep);
		  }

		  int cmp = currentEntry == null ? 1 : entry.CompareTo(currentEntry);
		  if (cmp < 0)
		  {
			throw new System.ArgumentException("out of order: " + entry + " < " + currentEntry);
		  }
		  else
		  {
			encodeFlags(flagsScratch, wordForm);
			int ord = flagLookup.add(flagsScratch);
			if (ord < 0)
			{
			  // already exists in our hash
			  ord = (-ord) - 1;
			}
			// finalize current entry, and switch "current" if necessary
			if (cmp > 0 && currentEntry != null)
			{
			  Util.toUTF32(currentEntry, scratchInts);
			  words.add(scratchInts, currentOrds);
			}
			// swap current
			if (cmp > 0 || currentEntry == null)
			{
			  currentEntry = entry;
			  currentOrds = new IntsRef(); // must be this way
			}
			currentOrds.grow(currentOrds.length + 1);
			currentOrds.ints[currentOrds.length++] = ord;
		  }
		}

		// finalize last entry
		Util.toUTF32(currentEntry, scratchInts);
		words.add(scratchInts, currentOrds);

		reader.close();
		sorted.delete();
	  }

	  private class ComparatorAnonymousInnerClassHelper : IComparer<BytesRef>
	  {
		  private readonly Dictionary outerInstance;

		  public ComparatorAnonymousInnerClassHelper(Dictionary outerInstance)
		  {
			  this.outerInstance = outerInstance;
			  scratch1 = new BytesRef();
			  scratch2 = new BytesRef();
		  }

		  internal BytesRef scratch1;
		  internal BytesRef scratch2;

		  public virtual int Compare(BytesRef o1, BytesRef o2)
		  {
			scratch1.bytes = o1.bytes;
			scratch1.offset = o1.offset;
			scratch1.length = o1.length;

			for (int i = scratch1.length - 1; i >= 0; i--)
			{
			  if (scratch1.bytes[scratch1.offset + i] == outerInstance.FLAG_SEPARATOR)
			  {
				scratch1.length = i;
				break;
			  }
			}

			scratch2.bytes = o2.bytes;
			scratch2.offset = o2.offset;
			scratch2.length = o2.length;

			for (int i = scratch2.length - 1; i >= 0; i--)
			{
			  if (scratch2.bytes[scratch2.offset + i] == outerInstance.FLAG_SEPARATOR)
			  {
				scratch2.length = i;
				break;
			  }
			}

			int cmp = scratch1.compareTo(scratch2);
			if (cmp == 0)
			{
			  // tie break on whole row
			  return o1.compareTo(o2);
			}
			else
			{
			  return cmp;
			}
		  }
	  }

	  internal static char[] decodeFlags(BytesRef b)
	  {
		if (b.length == 0)
		{
		  return CharsRef.EMPTY_CHARS;
		}
		int len = (int)((uint)b.length >> 1);
		char[] flags = new char[len];
		int upto = 0;
		int end = b.offset + b.length;
		for (int i = b.offset; i < end; i += 2)
		{
		  flags[upto++] = (char)((b.bytes[i] << 8) | (b.bytes[i + 1] & 0xff));
		}
		return flags;
	  }

	  internal static void encodeFlags(BytesRef b, char[] flags)
	  {
		int len = flags.Length << 1;
		b.grow(len);
		b.length = len;
		int upto = b.offset;
		for (int i = 0; i < flags.Length; i++)
		{
		  int flag = flags[i];
		  b.bytes[upto++] = unchecked((sbyte)((flag >> 8) & 0xff));
		  b.bytes[upto++] = unchecked((sbyte)(flag & 0xff));
		}
	  }

	  private void parseAlias(string line)
	  {
		string[] ruleArgs = line.Split("\\s+", true);
		if (aliases == null)
		{
		  //first line should be the aliases count
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int count = Integer.parseInt(ruleArgs[1]);
		  int count = int.Parse(ruleArgs[1]);
		  aliases = new string[count];
		}
		else
		{
		  // an alias can map to no flags
		  string aliasValue = ruleArgs.Length == 1 ? "" : ruleArgs[1];
		  aliases[aliasCount++] = aliasValue;
		}
	  }

	  private string getAliasValue(int id)
	  {
		try
		{
		  return aliases[id - 1];
		}
		catch (System.IndexOutOfRangeException ex)
		{
		  throw new System.ArgumentException("Bad flag alias number:" + id, ex);
		}
	  }

	  /// <summary>
	  /// Abstraction of the process of parsing flags taken from the affix and dic files
	  /// </summary>
	  internal abstract class FlagParsingStrategy
	  {

		/// <summary>
		/// Parses the given String into a single flag
		/// </summary>
		/// <param name="rawFlag"> String to parse into a flag </param>
		/// <returns> Parsed flag </returns>
		internal virtual char parseFlag(string rawFlag)
		{
		  char[] flags = parseFlags(rawFlag);
		  if (flags.Length != 1)
		  {
			throw new System.ArgumentException("expected only one flag, got: " + rawFlag);
		  }
		  return flags[0];
		}

		/// <summary>
		/// Parses the given String into multiple flags
		/// </summary>
		/// <param name="rawFlags"> String to parse into flags </param>
		/// <returns> Parsed flags </returns>
		internal abstract char[] parseFlags(string rawFlags);
	  }

	  /// <summary>
	  /// Simple implementation of <seealso cref="FlagParsingStrategy"/> that treats the chars in each String as a individual flags.
	  /// Can be used with both the ASCII and UTF-8 flag types.
	  /// </summary>
	  private class SimpleFlagParsingStrategy : FlagParsingStrategy
	  {
		public override char[] parseFlags(string rawFlags)
		{
		  return rawFlags.ToCharArray();
		}
	  }

	  /// <summary>
	  /// Implementation of <seealso cref="FlagParsingStrategy"/> that assumes each flag is encoded in its numerical form.  In the case
	  /// of multiple flags, each number is separated by a comma.
	  /// </summary>
	  private class NumFlagParsingStrategy : FlagParsingStrategy
	  {
		public override char[] parseFlags(string rawFlags)
		{
		  string[] rawFlagParts = rawFlags.Trim().Split(",", true);
		  char[] flags = new char[rawFlagParts.Length];
		  int upto = 0;

		  for (int i = 0; i < rawFlagParts.Length; i++)
		  {
			// note, removing the trailing X/leading I for nepali... what is the rule here?! 
			string replacement = rawFlagParts[i].replaceAll("[^0-9]", "");
			// note, ignoring empty flags (this happens in danish, for example)
			if (replacement.Length == 0)
			{
			  continue;
			}
			flags[upto++] = (char) int.Parse(replacement);
		  }

		  if (upto < flags.Length)
		  {
			flags = Arrays.copyOf(flags, upto);
		  }
		  return flags;
		}
	  }

	  /// <summary>
	  /// Implementation of <seealso cref="FlagParsingStrategy"/> that assumes each flag is encoded as two ASCII characters whose codes
	  /// must be combined into a single character.
	  /// 
	  /// TODO (rmuir) test
	  /// </summary>
	  private class DoubleASCIIFlagParsingStrategy : FlagParsingStrategy
	  {

		public override char[] parseFlags(string rawFlags)
		{
		  if (rawFlags.Length == 0)
		  {
			return new char[0];
		  }

		  StringBuilder builder = new StringBuilder();
		  if (rawFlags.Length % 2 == 1)
		  {
			throw new System.ArgumentException("Invalid flags (should be even number of characters): " + rawFlags);
		  }
		  for (int i = 0; i < rawFlags.Length; i += 2)
		  {
			char cookedFlag = (char)((int) rawFlags[i] + (int) rawFlags[i + 1]);
			builder.Append(cookedFlag);
		  }

		  char[] flags = new char[builder.Length];
		  builder.getChars(0, builder.Length, flags, 0);
		  return flags;
		}
	  }

	  internal static bool hasFlag(char[] flags, char flag)
	  {
		return Arrays.binarySearch(flags, flag) >= 0;
	  }

	  internal virtual CharSequence cleanInput(CharSequence input, StringBuilder reuse)
	  {
		reuse.Length = 0;

		for (int i = 0; i < input.length(); i++)
		{
		  char ch = input.charAt(i);

		  if (ignore != null && Arrays.binarySearch(ignore, ch) >= 0)
		  {
			continue;
		  }

		  if (ignoreCase && iconv == null)
		  {
			// if we have no input conversion mappings, do this on-the-fly
			ch = char.ToLower(ch);
		  }

		  reuse.Append(ch);
		}

		if (iconv != null)
		{
		  try
		  {
			applyMappings(iconv, reuse);
		  }
		  catch (IOException bogus)
		  {
			throw new Exception(bogus);
		  }
		  if (ignoreCase)
		  {
			for (int i = 0; i < reuse.Length; i++)
			{
			  reuse[i] = char.ToLower(reuse[i]);
			}
		  }
		}

		return reuse;
	  }

	  // TODO: this could be more efficient!
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: static void applyMappings(org.apache.lucene.util.fst.FST<org.apache.lucene.util.CharsRef> fst, StringBuilder sb) throws java.io.IOException
	  internal static void applyMappings(FST<CharsRef> fst, StringBuilder sb)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.fst.FST.BytesReader bytesReader = fst.getBytesReader();
		FST.BytesReader bytesReader = fst.BytesReader;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.fst.FST.Arc<org.apache.lucene.util.CharsRef> firstArc = fst.getFirstArc(new org.apache.lucene.util.fst.FST.Arc<org.apache.lucene.util.CharsRef>());
		FST.Arc<CharsRef> firstArc = fst.getFirstArc(new FST.Arc<CharsRef>());
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.CharsRef NO_OUTPUT = fst.outputs.getNoOutput();
		CharsRef NO_OUTPUT = fst.outputs.NoOutput;

		// temporary stuff
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.fst.FST.Arc<org.apache.lucene.util.CharsRef> arc = new org.apache.lucene.util.fst.FST.Arc<>();
		FST.Arc<CharsRef> arc = new FST.Arc<CharsRef>();
		int longestMatch;
		CharsRef longestOutput;

		for (int i = 0; i < sb.Length; i++)
		{
		  arc.copyFrom(firstArc);
		  CharsRef output = NO_OUTPUT;
		  longestMatch = -1;
		  longestOutput = null;

		  for (int j = i; j < sb.Length; j++)
		  {
			char ch = sb[j];
			if (fst.findTargetArc(ch, arc, arc, bytesReader) == null)
			{
			  break;
			}
			else
			{
			  output = fst.outputs.add(output, arc.output);
			}
			if (arc.Final)
			{
			  longestOutput = fst.outputs.add(output, arc.nextFinalOutput);
			  longestMatch = j;
			}
		  }

		  if (longestMatch >= 0)
		  {
			sb.Remove(i, longestMatch + 1 - i);
			sb.Insert(i, longestOutput);
			i += (longestOutput.length - 1);
		  }
		}
	  }
	}

}