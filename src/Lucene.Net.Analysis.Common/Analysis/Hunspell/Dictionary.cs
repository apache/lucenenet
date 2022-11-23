// Lucene version compatibility level 4.10.4
using J2N;
using J2N.Collections.Generic.Extensions;
using J2N.Numerics;
using J2N.Text;
using Lucene.Net.Diagnostics;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Support.IO;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Lucene.Net.Util.Fst;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using JCG = J2N.Collections.Generic;
using Integer = J2N.Numerics.Int32;

namespace Lucene.Net.Analysis.Hunspell
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

    /// <summary>
    /// In-memory structure for the dictionary (.dic) and affix (.aff)
    /// data of a hunspell dictionary.
    /// </summary>
    public class Dictionary
    {
        private static readonly char[] NOFLAGS = Arrays.Empty<char>();

        private const string ALIAS_KEY = "AF";
        private const string MORPH_ALIAS_KEY = "AM";
        private const string PREFIX_KEY = "PFX";
        private const string SUFFIX_KEY = "SFX";
        private const string FLAG_KEY = "FLAG";
        private const string COMPLEXPREFIXES_KEY = "COMPLEXPREFIXES";
        private const string CIRCUMFIX_KEY = "CIRCUMFIX";
        private const string IGNORE_KEY = "IGNORE";
        private const string ICONV_KEY = "ICONV";
        private const string OCONV_KEY = "OCONV";
        private const string FULLSTRIP_KEY = "FULLSTRIP";
        private const string LANG_KEY = "LANG";
        private const string KEEPCASE_KEY = "KEEPCASE";
        private const string NEEDAFFIX_KEY = "NEEDAFFIX";
        private const string PSEUDOROOT_KEY = "PSEUDOROOT";
        private const string ONLYINCOMPOUND_KEY = "ONLYINCOMPOUND";

        private const string NUM_FLAG_TYPE = "num";
        private const string UTF8_FLAG_TYPE = "UTF-8";
        private const string LONG_FLAG_TYPE = "long";

        // TODO: really for suffixes we should reverse the automaton and run them backwards
        private const string PREFIX_CONDITION_REGEX_PATTERN = "{0}.*";
        private const string SUFFIX_CONDITION_REGEX_PATTERN = ".*{0}";

        internal FST<Int32sRef> prefixes;
        internal FST<Int32sRef> suffixes;

        // all condition checks used by prefixes and suffixes. these are typically re-used across
        // many affix stripping rules. so these are deduplicated, to save RAM.
        internal IList<CharacterRunAutomaton> patterns = new JCG.List<CharacterRunAutomaton>();

        // the entries in the .dic file, mapping to their set of flags.
        // the fst output is the ordinal list for flagLookup
        internal FST<Int32sRef> words;
        // the list of unique flagsets (wordforms). theoretically huge, but practically
        // small (e.g. for polish this is 756), otherwise humans wouldn't be able to deal with it either.
        internal BytesRefHash flagLookup = new BytesRefHash();

        // the list of unique strip affixes.
        internal char[] stripData;
        internal int[] stripOffsets;

        // 8 bytes per affix
        internal byte[] affixData = new byte[64];
        private int currentAffix = 0;

        private FlagParsingStrategy flagParsingStrategy = new SimpleFlagParsingStrategy(); // Default flag parsing strategy

        // AF entries
        private string[] aliases;
        private int aliasCount = 0;

        // AM entries
        private string[] morphAliases;
        private int morphAliasCount = 0;

        // st: morphological entries (either directly, or aliased from AM)
        private string[] stemExceptions = new string[8];
        private int stemExceptionCount = 0;
        // we set this during sorting, so we know to add an extra FST output.
        // when set, some words have exceptional stems, and the last entry is a pointer to stemExceptions
        internal bool hasStemExceptions;

        // LUCENENET specific - changed from DirectoryInfo to string
        private readonly string tempDir = OfflineSorter.DefaultTempDir; // TODO: make this configurable?

        internal bool ignoreCase;
        internal bool complexPrefixes;
        internal bool twoStageAffix; // if no affixes have continuation classes, no need to do 2-level affix stripping

        internal int circumfix = -1; // circumfix flag, or -1 if one is not defined
        internal int keepcase = -1;  // keepcase flag, or -1 if one is not defined
        internal int needaffix = -1; // needaffix flag, or -1 if one is not defined
        internal int onlyincompound = -1; // onlyincompound flag, or -1 if one is not defined

        // ignored characters (dictionary, affix, inputs)
        private char[] ignore;

        // FSTs used for ICONV/OCONV, output ord pointing to replacement text
        internal FST<CharsRef> iconv;
        internal FST<CharsRef> oconv;

        internal bool needsInputCleaning;
        internal bool needsOutputCleaning;

        // true if we can strip suffixes "down to nothing"
        internal bool fullStrip;

        // language declaration of the dictionary
        internal string language;
        // true if case algorithms should use alternate (Turkish/Azeri) mapping
        internal bool alternateCasing;

        // LUCENENET: Added so we can get better performance than creating the regex in every tight loop.
        private static readonly Regex whitespacePattern = new Regex("\\s+", RegexOptions.Compiled);
        private static readonly Regex leadingDigitPattern = new Regex("[^0-9]", RegexOptions.Compiled);

        /// <summary>
        /// Creates a new <see cref="Dictionary"/> containing the information read from the provided <see cref="Stream"/>s to hunspell affix
        /// and dictionary files.
        /// You have to dispose the provided <see cref="Stream"/>s yourself.
        /// </summary>
        /// <param name="affix"> <see cref="Stream"/> for reading the hunspell affix file (won't be disposed). </param>
        /// <param name="dictionary"> <see cref="Stream"/> for reading the hunspell dictionary file (won't be disposed). </param>
        /// <exception cref="IOException"> Can be thrown while reading from the <see cref="Stream"/>s </exception>
        /// <exception cref="Exception"> Can be thrown if the content of the files does not meet expected formats </exception>
        public Dictionary(Stream affix, Stream dictionary) 
            : this(affix, new JCG.List<Stream>() { dictionary }, false)
        {
        }

        /// <summary>
        /// Creates a new <see cref="Dictionary"/> containing the information read from the provided <see cref="Stream"/>s to hunspell affix
        /// and dictionary files.
        /// You have to dispose the provided <see cref="Stream"/>s yourself.
        /// </summary>
        /// <param name="affix"> <see cref="Stream"/> for reading the hunspell affix file (won't be disposed). </param>
        /// <param name="dictionaries"> <see cref="Stream"/> for reading the hunspell dictionary files (won't be disposed). </param>
        /// <param name="ignoreCase"> ignore case? </param>
        /// <exception cref="IOException"> Can be thrown while reading from the <see cref="Stream"/>s </exception>
        /// <exception cref="Exception"> Can be thrown if the content of the files does not meet expected formats </exception>
        public Dictionary(Stream affix, IList<Stream> dictionaries, bool ignoreCase)
        {
            this.ignoreCase = ignoreCase;
            this.needsInputCleaning = ignoreCase;
            this.needsOutputCleaning = false; // set if we have an OCONV
            flagLookup.Add(new BytesRef()); // no flags -> ord 0

            FileStream aff = FileSupport.CreateTempFileAsStream("affix", "aff", tempDir);
            try
            {
                // copy contents of affix stream to temp file
                affix.CopyTo(aff);
                aff.Position = 0; // LUCENENET specific - seek to the beginning of the file so we don't need to reopen

                // pass 1: get encoding
                string encoding = GetDictionaryEncoding(aff);
                aff.Position = 0; // LUCENENET specific - seek to the beginning of the file so we don't need to reopen

                // pass 2: parse affixes
                Encoding decoder = GetSystemEncoding(encoding);
                ReadAffixFile(aff, decoder);

                // read dictionary entries
                Int32SequenceOutputs o = Int32SequenceOutputs.Singleton;
                Builder<Int32sRef> b = new Builder<Int32sRef>(FST.INPUT_TYPE.BYTE4, o);
                ReadDictionaryFiles(dictionaries, decoder, b);
                words = b.Finish();
                aliases = null; // no longer needed
                morphAliases = null; // no longer needed
            }
            finally
            {
                aff.Dispose();
            }
        }

        // only for testing
        internal virtual Int32sRef LookupWord(char[] word, int offset, int length)
        {
            return Lookup(words, word, offset, length);
        }

        // only for testing
        internal virtual Int32sRef LookupPrefix(char[] word, int offset, int length)
        {
            return Lookup(prefixes, word, offset, length);
        }

        /// <summary>
        /// Looks up HunspellAffix suffixes that have an append that matches the <see cref="string"/> created from the given <see cref="char"/> array, offset and length
        /// </summary>
        /// <param name="word"> <see cref="char"/> array to generate the <see cref="string"/> from </param>
        /// <param name="offset"> Offset in the char array that the <see cref="string"/> starts at </param>
        /// <param name="length"> Length from the offset that the <see cref="string"/> is </param>
        /// <returns> List of HunspellAffix suffixes with an append that matches the <see cref="string"/>, or <c>null</c> if none are found </returns>
        internal virtual Int32sRef LookupSuffix(char[] word, int offset, int length)
        {
            return Lookup(suffixes, word, offset, length);
        }

        internal virtual Int32sRef Lookup(FST<Int32sRef> fst, char[] word, int offset, int length)
        {
            if (fst is null)
            {
                return null;
            }
            FST.BytesReader bytesReader = fst.GetBytesReader();
            FST.Arc<Int32sRef> arc = fst.GetFirstArc(new FST.Arc<Int32sRef>());
            // Accumulate output as we go
            Int32sRef NO_OUTPUT = fst.Outputs.NoOutput;
            Int32sRef output = NO_OUTPUT;

            int l = offset + length;
            try
            {
                for (int i = offset, cp = 0; i < l; i += Character.CharCount(cp))
                {
                    cp = Character.CodePointAt(word, i, l);
                    if (fst.FindTargetArc(cp, arc, arc, bytesReader) is null)
                    {
                        return null;
                    }
                    else if (arc.Output != NO_OUTPUT)
                    {
                        output = fst.Outputs.Add(output, arc.Output);
                    }
                }
                if (fst.FindTargetArc(FST.END_LABEL, arc, arc, bytesReader) is null)
                {
                    return null;
                }
                else if (arc.Output != NO_OUTPUT)
                {
                    return fst.Outputs.Add(output, arc.Output);
                }
                else
                {
                    return output;
                }
            }
            catch (Exception bogus) when (bogus.IsIOException())
            {
                throw RuntimeException.Create(bogus);
            }
        }

        /// <summary>
        /// Reads the affix file through the provided <see cref="Stream"/>, building up the prefix and suffix maps
        /// </summary>
        /// <param name="affixStream"> <see cref="Stream"/> to read the content of the affix file from </param>
        /// <param name="decoder"> <see cref="Encoding"/> to decode the content of the file </param>
        /// <exception cref="IOException"> Can be thrown while reading from the InputStream </exception>
        private void ReadAffixFile(Stream affixStream, Encoding decoder)
        {
            var prefixes = new JCG.SortedDictionary<string, IList<int>>(StringComparer.Ordinal);
            var suffixes = new JCG.SortedDictionary<string, IList<int>>(StringComparer.Ordinal);
            IDictionary<string, int> seenPatterns = new JCG.Dictionary<string, int>
            {
                // zero condition -> 0 ord
                [".*"] = 0
            };
            patterns.Add(null);

            // zero strip -> 0 ord
            IDictionary<string, int> seenStrips = new JCG.LinkedDictionary<string, int>
            {
                [""] = 0
            };

            var reader = new StreamReader(affixStream, decoder);
            string line; // LUCENENET: Removed unnecessary null assignment
            int lineNumber = 0;
            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;
                // ignore any BOM marker on first line
                if (lineNumber == 1 && line.StartsWith("\uFEFF", StringComparison.Ordinal))
                {
                    line = line.Substring(1);
                }
                if (line.StartsWith(ALIAS_KEY, StringComparison.Ordinal))
                {
                    ParseAlias(line);
                }
                else if (line.StartsWith(MORPH_ALIAS_KEY, StringComparison.Ordinal))
                {
                    ParseMorphAlias(line);
                }
                else if (line.StartsWith(PREFIX_KEY, StringComparison.Ordinal))
                {
                    ParseAffix(prefixes, line, reader, PREFIX_CONDITION_REGEX_PATTERN, seenPatterns, seenStrips);
                }
                else if (line.StartsWith(SUFFIX_KEY, StringComparison.Ordinal))
                {
                    ParseAffix(suffixes, line, reader, SUFFIX_CONDITION_REGEX_PATTERN, seenPatterns, seenStrips);
                }
                else if (line.StartsWith(FLAG_KEY, StringComparison.Ordinal))
                {
                    // Assume that the FLAG line comes before any prefix or suffixes
                    // Store the strategy so it can be used when parsing the dic file
                    flagParsingStrategy = GetFlagParsingStrategy(line);
                }
                else if (line.Equals(COMPLEXPREFIXES_KEY, StringComparison.Ordinal))
                {
                    complexPrefixes = true; // 2-stage prefix+1-stage suffix instead of 2-stage suffix+1-stage prefix
                }
                else if (line.StartsWith(CIRCUMFIX_KEY, StringComparison.Ordinal))
                {
                    string[] parts = whitespacePattern.Split(line).TrimEnd();
                    if (parts.Length != 2)
                    {
                        throw new ParseException("Illegal CIRCUMFIX declaration", lineNumber);
                    }
                    circumfix = flagParsingStrategy.ParseFlag(parts[1]);
                }
                else if (line.StartsWith(KEEPCASE_KEY, StringComparison.Ordinal))
                {
                    string[] parts = whitespacePattern.Split(line).TrimEnd();
                    if (parts.Length != 2)
                    {
                        throw new ParseException("Illegal KEEPCASE declaration", lineNumber);
                    }
                    keepcase = flagParsingStrategy.ParseFlag(parts[1]);
                }
                else if (line.StartsWith(NEEDAFFIX_KEY, StringComparison.Ordinal) || line.StartsWith(PSEUDOROOT_KEY, StringComparison.Ordinal))
                {
                    string[] parts = whitespacePattern.Split(line).TrimEnd();
                    if (parts.Length != 2)
                    {
                        throw new ParseException("Illegal NEEDAFFIX declaration", lineNumber);
                    }
                    needaffix = flagParsingStrategy.ParseFlag(parts[1]);
                }
                else if (line.StartsWith(ONLYINCOMPOUND_KEY, StringComparison.Ordinal))
                {
                    string[] parts = whitespacePattern.Split(line).TrimEnd();
                    if (parts.Length != 2)
                    {
                        throw new ParseException("Illegal ONLYINCOMPOUND declaration", lineNumber);
                    }
                    onlyincompound = flagParsingStrategy.ParseFlag(parts[1]);
                }
                else if (line.StartsWith(IGNORE_KEY, StringComparison.Ordinal))
                {
                    string[] parts = whitespacePattern.Split(line).TrimEnd();
                    if (parts.Length != 2)
                    {
                        throw new ParseException("Illegal IGNORE declaration", lineNumber);
                    }
                    ignore = parts[1].ToCharArray();
                    Array.Sort(ignore);
                    needsInputCleaning = true;
                }
                else if (line.StartsWith(ICONV_KEY, StringComparison.Ordinal) || line.StartsWith(OCONV_KEY, StringComparison.Ordinal))
                {
                    string[] parts = whitespacePattern.Split(line).TrimEnd();
                    string type = parts[0];
                    if (parts.Length != 2)
                    {
                        throw new ParseException(string.Format("Illegal {0} declaration", type), lineNumber);
                    }
                    int num = Integer.Parse(parts[1], radix: 10); // LUCENENET: specify radix 10 to make this culture invariant
                    FST<CharsRef> res = ParseConversions(reader, num, lineNumber); // LUCENENET: Pass linenumber so we can throw it
                    if (type.Equals("ICONV", StringComparison.Ordinal))
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
                else if (line.StartsWith(FULLSTRIP_KEY, StringComparison.Ordinal))
                {
                    fullStrip = true;
                }
                else if (line.StartsWith(LANG_KEY, StringComparison.Ordinal))
                {
                    language = line.Substring(LANG_KEY.Length).Trim();
                    alternateCasing = "tr_TR".Equals(language, StringComparison.Ordinal) || "az_AZ".Equals(language, StringComparison.Ordinal);
                }
            }

            this.prefixes = AffixFST(prefixes);
            this.suffixes = AffixFST(suffixes);

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
            if (Debugging.AssertsEnabled) Debugging.Assert(currentIndex == seenStrips.Count);
            stripOffsets[currentIndex] = currentOffset;
        }

        private static FST<Int32sRef> AffixFST(JCG.SortedDictionary<string, IList<int>> affixes) // LUCENENET: CA1822: Mark members as static
        {
            Int32SequenceOutputs outputs = Int32SequenceOutputs.Singleton;
            Builder<Int32sRef> builder = new Builder<Int32sRef>(FST.INPUT_TYPE.BYTE4, outputs);

            Int32sRef scratch = new Int32sRef();
            foreach (KeyValuePair<string, IList<int>> entry in affixes)
            {
                Lucene.Net.Util.Fst.Util.ToUTF32(entry.Key, scratch);
                IList<int> entries = entry.Value;
                Int32sRef output = new Int32sRef(entries.Count);
                foreach (int c in entries)
                {
                    output.Int32s[output.Length++] = c;
                }
                builder.Add(scratch, output);
            }
            return builder.Finish();
        }

        internal static string EscapeDash(string re)
        {
            // we have to be careful, even though dash doesn't have a special meaning,
            // some dictionaries already escape it (e.g. pt_PT), so we don't want to nullify it
            StringBuilder escaped = new StringBuilder();
            for (int i = 0; i < re.Length; i++)
            {
                char c = re[i];
                if (c == '-')
                {
                    escaped.Append("\\-");
                }
                else
                {
                    escaped.Append(c);
                    if (c == '\\' && i + 1 < re.Length)
                    {
                        escaped.Append(re[i + 1]);
                        i++;
                    }
                }
            }
            return escaped.ToString();
        }

        /// <summary>
        /// Parses a specific affix rule putting the result into the provided affix map
        /// </summary>
        /// <param name="affixes"> <see cref="JCG.SortedDictionary{TKey, TValue}"/> where the result of the parsing will be put </param>
        /// <param name="header"> Header line of the affix rule </param>
        /// <param name="reader"> <see cref="TextReader"/> to read the content of the rule from </param>
        /// <param name="conditionPattern"> <see cref="string.Format(string, object[])"/> pattern to be used to generate the condition regex
        ///                         pattern </param>
        /// <param name="seenPatterns"> map from condition -> index of patterns, for deduplication. </param>
        /// <param name="seenStrips"></param>
        /// <exception cref="IOException"> Can be thrown while reading the rule </exception>
        private void ParseAffix(JCG.SortedDictionary<string, IList<int>> affixes,
                    string header,
                    TextReader reader,
                    string conditionPattern,
                    IDictionary<string, int> seenPatterns,
                    IDictionary<string, int> seenStrips)
        {
            BytesRef scratch = new BytesRef();
            StringBuilder sb = new StringBuilder();
            string[] args = whitespacePattern.Split(header).TrimEnd();

            bool crossProduct = args[2].Equals("Y", StringComparison.Ordinal);
            bool isSuffix = conditionPattern == SUFFIX_CONDITION_REGEX_PATTERN;

            int numLines = Integer.Parse(args[3], radix: 10); // LUCENENET: specify radix 10 to make this culture invariant
            affixData = ArrayUtil.Grow(affixData, (currentAffix << 3) + (numLines << 3));
            ByteArrayDataOutput affixWriter = new ByteArrayDataOutput(affixData, currentAffix << 3, numLines << 3);

            for (int i = 0; i < numLines; i++)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(affixWriter.Position == currentAffix << 3);
                string line = reader.ReadLine();
                string[] ruleArgs = whitespacePattern.Split(line).TrimEnd();

                // from the manpage: PFX flag stripping prefix [condition [morphological_fields...]]
                // condition is optional
                if (ruleArgs.Length < 4)
                {
                    throw new ParseException("The affix file contains a rule with less than four elements: " + line, 0  /*, reader.LineNumber */);// LUCENENET TODO: LineNumberReader
                }

                char flag = flagParsingStrategy.ParseFlag(ruleArgs[1]);
                string strip = ruleArgs[2].Equals("0", StringComparison.Ordinal) ? "" : ruleArgs[2];
                string affixArg = ruleArgs[3];
                char[] appendFlags = null;

                // first: parse continuation classes out of affix
                int flagSep = affixArg.LastIndexOf('/');
                if (flagSep != -1)
                {
                    string flagPart = affixArg.Substring(flagSep + 1);
                    affixArg = affixArg.Substring(0, flagSep - 0);

                    if (aliasCount > 0)
                    {
                        flagPart = GetAliasValue(Integer.Parse(flagPart, radix: 10)); // LUCENENET: specify radix 10 to make this culture invariant
                    }

                    appendFlags = flagParsingStrategy.ParseFlags(flagPart);
                    Array.Sort(appendFlags);
                    twoStageAffix = true;
                }
                // zero affix -> empty string
                if ("0".Equals(affixArg, StringComparison.Ordinal))
                {
                    affixArg = "";
                }

                string condition = ruleArgs.Length > 4 ? ruleArgs[4] : ".";
                // at least the gascon affix file has this issue
                if (condition.StartsWith("[", StringComparison.Ordinal) && condition.IndexOf(']') == -1)
                {
                    condition = condition + "]";
                }
                // "dash hasn't got special meaning" (we must escape it)
                if (condition.IndexOf('-') >= 0)
                {
                    condition = EscapeDash(condition);
                }

                string regex;
                if (".".Equals(condition, StringComparison.Ordinal))
                {
                    regex = ".*"; // Zero condition is indicated by dot
                }
                else if (condition.Equals(strip, StringComparison.Ordinal))
                {
                    regex = ".*"; // TODO: optimize this better:
                                  // if we remove 'strip' from condition, we don't have to append 'strip' to check it...!
                                  // but this is complicated...
                }
                else
                {
                    regex = string.Format(CultureInfo.InvariantCulture, conditionPattern, condition);
                }

                // deduplicate patterns
                if (!seenPatterns.TryGetValue(regex, out int patternIndex))
                {
                    patternIndex = patterns.Count;
                    if (patternIndex > short.MaxValue)
                    {
                        throw UnsupportedOperationException.Create("Too many patterns, please report this to dev@lucene.apache.org");
                    }
                    seenPatterns[regex] = patternIndex;
                    CharacterRunAutomaton pattern = new CharacterRunAutomaton((new RegExp(regex, RegExpSyntax.NONE)).ToAutomaton());
                    patterns.Add(pattern);
                }

                if (!seenStrips.TryGetValue(strip, out int stripOrd))
                {
                    stripOrd = seenStrips.Count;
                    seenStrips[strip] = stripOrd;
                    if (stripOrd > char.MaxValue)
                    {
                        throw UnsupportedOperationException.Create("Too many unique strips, please report this to dev@lucene.apache.org");
                    }
                }

                if (appendFlags is null)
                {
                    appendFlags = NOFLAGS;
                }

                EncodeFlags(scratch, appendFlags);
                int appendFlagsOrd = flagLookup.Add(scratch);
                if (appendFlagsOrd < 0)
                {
                    // already exists in our hash
                    appendFlagsOrd = (-appendFlagsOrd) - 1;
                }
                else if (appendFlagsOrd > short.MaxValue)
                {
                    // this limit is probably flexible, but its a good sanity check too
                    throw UnsupportedOperationException.Create("Too many unique append flags, please report this to dev@lucene.apache.org");
                }

                affixWriter.WriteInt16((short)flag);
                affixWriter.WriteInt16((short)stripOrd);
                // encode crossProduct into patternIndex
                int patternOrd = (int)patternIndex << 1 | (crossProduct ? 1 : 0);
                affixWriter.WriteInt16((short)patternOrd);
                affixWriter.WriteInt16((short)appendFlagsOrd);

                if (needsInputCleaning)
                {
                    string cleaned = CleanInput(affixArg, sb);
                    affixArg = cleaned.ToString();
                }

                if (isSuffix)
                {
                    affixArg = new StringBuilder(affixArg).Reverse().ToString();
                }

                if (!affixes.TryGetValue(affixArg, out IList<int> list) || list is null)
                {
                    affixes[affixArg] = list = new JCG.List<int>();
                }

                list.Add(currentAffix);
                currentAffix++;
            }
        }

        private static FST<CharsRef> ParseConversions(TextReader reader, int num, int lineNumber) // LUCENENET: CA1822: Mark members as static
        {
            IDictionary<string, string> mappings = new JCG.SortedDictionary<string, string>(StringComparer.Ordinal);

            for (int i = 0; i < num; i++)
            {
                string line = reader.ReadLine();
                string[] parts = whitespacePattern.Split(line).TrimEnd();
                if (parts.Length != 3)
                {
                    throw new ParseException("invalid syntax: " + line, lineNumber /*, reader.LineNumber */); // LUCENENET TODO: LineNumberReader
                }
                if (mappings.Put(parts[1], parts[2]) != null)
                {
                    throw IllegalStateException.Create("duplicate mapping specified for: " + parts[1]);
                }
            }

            Outputs<CharsRef> outputs = CharSequenceOutputs.Singleton;
            Builder<CharsRef> builder = new Builder<CharsRef>(FST.INPUT_TYPE.BYTE2, outputs);
            Int32sRef scratchInts = new Int32sRef();
            foreach (KeyValuePair<string, string> entry in mappings)
            {
                Lucene.Net.Util.Fst.Util.ToUTF16(entry.Key, scratchInts);
                builder.Add(scratchInts, new CharsRef(entry.Value));
            }

            return builder.Finish();
        }

        /// <summary>
        /// pattern accepts optional BOM + SET + any whitespace </summary>
        internal static readonly Regex ENCODING_PATTERN = new Regex("^(\u00EF\u00BB\u00BF)?SET\\s+", RegexOptions.Compiled);

        /// <summary>
        /// Parses the encoding specified in the affix file readable through the provided <see cref="Stream"/>
        /// </summary>
        /// <param name="affix"> <see cref="Stream"/> for reading the affix file </param>
        /// <returns> Encoding specified in the affix file </returns>
        /// <exception cref="IOException"> Can be thrown while reading from the <see cref="Stream"/> </exception>
        /// <exception cref="Exception"> Thrown if the first non-empty non-comment line read from the file does not adhere to the format <c>SET &lt;encoding&gt;</c></exception>
        internal static string GetDictionaryEncoding(Stream affix)
        {
            StringBuilder encoding = new StringBuilder();
            for (;;)
            {
                encoding.Length = 0;
                int ch;
                while ((ch = affix.ReadByte()) > 0)
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
                Match matcher = ENCODING_PATTERN.Match(encoding.ToString());
                if (matcher.Success)
                {
                    int last = matcher.Index + matcher.Length;
                    return encoding.ToString(last, encoding.Length - last).Trim();
                }
            }
        }

        internal static readonly IDictionary<string, string> CHARSET_ALIASES = LoadCharsetAliases();
        private static IDictionary<string, string> LoadCharsetAliases() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
            return new Dictionary<string, string>
            {
                ["microsoft-cp1251"] = "windows-1251",
                ["TIS620-2533"] = "TIS-620"
            }.AsReadOnly();
        }

        /// <summary>
        /// Retrieves the <see cref="Encoding"/> for the given encoding.  Note, This isn't perfect as I think ISCII-DEVANAGARI and
        /// MICROSOFT-CP1251 etc are allowed...
        /// </summary>
        /// <param name="encoding"> Encoding to retrieve the <see cref="Encoding"/> instance for </param>
        /// <returns> <see cref="Encoding"/> for the given encoding <see cref="string"/> </returns>
        // LUCENENET NOTE: This was getJavaEncoding in Lucene
        private static Encoding GetSystemEncoding(string encoding) // LUCENENET: CA1822: Mark members as static
        {
            if (string.IsNullOrEmpty(encoding))
            {
                return Encoding.UTF8;
            }
            if ("ISO8859-14".Equals(encoding, StringComparison.OrdinalIgnoreCase))
            {
                return new ISO8859_14Encoding();
            }
            // .NET doesn't recognize the encoding without a dash between ISO and the number
            // https://msdn.microsoft.com/en-us/library/system.text.encodinginfo.getencoding(v=vs.110).aspx
            if (encoding.Length > 3 && encoding.StartsWith("ISO", StringComparison.OrdinalIgnoreCase) && 
                encoding[3] != '-')
            {
                encoding = "iso-" + encoding.Substring(3);
            }
            // Special case - for codepage 1250-1258, we need to change to 
            // windows-1251, etc.
            else if (windowsCodePagePattern.IsMatch(encoding))
            {
                encoding = "windows-" + windowsCodePagePattern.Match(encoding).Groups[1].Value;
            }
            // Special case - for Thai we need to switch to windows-874
            else if (thaiCodePagePattern.IsMatch(encoding))
            {
                encoding = "windows-874";
            }

            return Encoding.GetEncoding(encoding);
        }

        private static readonly Regex windowsCodePagePattern = new Regex("^(?:microsoft-)?cp-?(125[0-8])$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex thaiCodePagePattern = new Regex("^tis-?620(?:-?2533)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);


        /// <summary>
        /// Determines the appropriate <see cref="FlagParsingStrategy"/> based on the FLAG definition line taken from the affix file
        /// </summary>
        /// <param name="flagLine"> Line containing the flag information </param>
        /// <returns> <see cref="FlagParsingStrategy"/> that handles parsing flags in the way specified in the FLAG definition </returns>
        internal static FlagParsingStrategy GetFlagParsingStrategy(string flagLine)
        {
            string[] parts = whitespacePattern.Split(flagLine).TrimEnd();
            if (parts.Length != 2)
            {
                throw new ArgumentException("Illegal FLAG specification: " + flagLine);
            }
            string flagType = parts[1];

            if (NUM_FLAG_TYPE.Equals(flagType, StringComparison.Ordinal))
            {
                return new NumFlagParsingStrategy();
            }
            else if (UTF8_FLAG_TYPE.Equals(flagType, StringComparison.Ordinal))
            {
                return new SimpleFlagParsingStrategy();
            }
            else if (LONG_FLAG_TYPE.Equals(flagType, StringComparison.Ordinal))
            {
                return new DoubleASCIIFlagParsingStrategy();
            }

            throw new ArgumentException("Unknown flag type: " + flagType);
        }

        internal const char FLAG_SEPARATOR = (char)0x1f; // flag separator after escaping
        internal const char MORPH_SEPARATOR = (char)0x1e; // separator for boundary of entry (may be followed by morph data)

        internal virtual string UnescapeEntry(string entry)
        {
            StringBuilder sb = new StringBuilder();
            int end = MorphBoundary(entry);
            for (int i = 0; i < end; i++)
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
                else if (ch == MORPH_SEPARATOR || ch == FLAG_SEPARATOR)
                {
                    // BINARY EXECUTABLES EMBEDDED IN ZULU DICTIONARIES!!!!!!!
                }
                else
                {
                    sb.Append(ch);
                }
            }
            sb.Append(MORPH_SEPARATOR);
            if (end < entry.Length)
            {
                for (int i = end; i < entry.Length; i++)
                {
                    char c = entry[i];
                    if (c == FLAG_SEPARATOR || c == MORPH_SEPARATOR)
                    {
                        // BINARY EXECUTABLES EMBEDDED IN ZULU DICTIONARIES!!!!!!!
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }
            return sb.ToString();
        }

        internal static int MorphBoundary(string line)
        {
            int end = IndexOfSpaceOrTab(line, 0);
            if (end == -1)
            {
                return line.Length;
            }
            while (end >= 0 && end < line.Length)
            {
                if (line[end] == '\t' ||
                    end + 3 < line.Length &&
                    Character.IsLetter(line[end + 1]) &&
                    Character.IsLetter(line[end + 2]) &&
                    line[end + 3] == ':')
                {
                    break;
                }
                end = IndexOfSpaceOrTab(line, end + 1);
            }
            if (end == -1)
            {
                return line.Length;
            }
            return end;
        }

        internal static int IndexOfSpaceOrTab(string text, int start)
        {
            int pos1 = text.IndexOf('\t', start);
            int pos2 = text.IndexOf(' ', start);
            if (pos1 >= 0 && pos2 >= 0)
            {
                return Math.Min(pos1, pos2);
            }
            else
            {
                return Math.Max(pos1, pos2);
            }
        }

        /// <summary>
        /// Reads the dictionary file through the provided <see cref="Stream"/>s, building up the words map
        /// </summary>
        /// <param name="dictionaries"> <see cref="Stream"/>s to read the dictionary file through </param>
        /// <param name="decoder"> <see cref="Encoding"/> used to decode the contents of the file </param>
        /// <param name="words"></param>
        /// <exception cref="IOException"> Can be thrown while reading from the file </exception>
        private void ReadDictionaryFiles(IList<Stream> dictionaries, Encoding decoder, Builder<Int32sRef> words)
        {
            BytesRef flagsScratch = new BytesRef();
            Int32sRef scratchInts = new Int32sRef();

            StringBuilder sb = new StringBuilder();

            using FileStream unsorted = FileSupport.CreateTempFileAsStream("unsorted", "dat", tempDir);
            using (OfflineSorter.ByteSequencesWriter writer = new OfflineSorter.ByteSequencesWriter(unsorted, leaveOpen: true))
            {
                foreach (Stream dictionary in dictionaries)
                {
                    var lines = new StreamReader(dictionary, decoder);
                    string line = lines.ReadLine(); // first line is number of entries (approximately, sometimes)

                    while ((line = lines.ReadLine()) != null)
                    {
                        // wild and unpredictable code comment rules
                        if (line == string.Empty || line[0] == '/' || line[0] == '#' || line[0] == '\t')
                        {
                            continue;
                        }
                        line = UnescapeEntry(line);
                        // if we havent seen any stem exceptions, try to parse one
                        if (hasStemExceptions == false)
                        {
                            int morphStart = line.IndexOf(MORPH_SEPARATOR);
                            if (morphStart >= 0 && morphStart < line.Length)
                            {
                                hasStemExceptions = ParseStemException(line.Substring(morphStart + 1)) != null;
                            }
                        }
                        if (needsInputCleaning)
                        {
                            int flagSep = line.LastIndexOf(FLAG_SEPARATOR);
                            if (flagSep == -1)
                            {
                                flagSep = line.IndexOf(MORPH_SEPARATOR);
                            }
                            if (flagSep == -1)
                            {
                                string cleansed = CleanInput(line, sb);
                                writer.Write(cleansed.ToString().GetBytes(Encoding.UTF8));
                            }
                            else
                            {
                                string text = line.Substring(0, flagSep - 0);
                                string cleansed = CleanInput(text, sb);
                                if (cleansed != sb.ToString())
                                {
                                    sb.Length = 0;
                                    sb.Append(cleansed);
                                }
#if FEATURE_STRINGBUILDER_APPEND_READONLYSPAN
                                sb.Append(line.AsSpan(flagSep));
#else
                                sb.Append(line.Substring(flagSep));
#endif
                                writer.Write(sb.ToString().GetBytes(Encoding.UTF8));
                            }
                        }
                        else
                        {
                            writer.Write(line.GetBytes(Encoding.UTF8));
                        }
                    }
                }
            }

            // LUCENENET: Reset the position to the beginning of the stream so we don't have to reopen the file
            unsorted.Position = 0;

            using FileStream sorted = FileSupport.CreateTempFileAsStream("sorted", "dat", tempDir);

            OfflineSorter sorter = new OfflineSorter(Comparer<BytesRef>.Create((o1, o2) =>
            {
                BytesRef scratch1 = new BytesRef();
                BytesRef scratch2 = new BytesRef();

                scratch1.Bytes = o1.Bytes;
                scratch1.Offset = o1.Offset;
                scratch1.Length = o1.Length;

                for (int i = scratch1.Length - 1; i >= 0; i--)
                {
                    if (scratch1.Bytes[scratch1.Offset + i] == FLAG_SEPARATOR || scratch1.Bytes[scratch1.Offset + i] == MORPH_SEPARATOR)
                    {
                        scratch1.Length = i;
                        break;
                    }
                }

                scratch2.Bytes = o2.Bytes;
                scratch2.Offset = o2.Offset;
                scratch2.Length = o2.Length;

                for (int i = scratch2.Length - 1; i >= 0; i--)
                {
                    if (scratch2.Bytes[scratch2.Offset + i] == FLAG_SEPARATOR || scratch2.Bytes[scratch2.Offset + i] == MORPH_SEPARATOR)
                    {
                        scratch2.Length = i;
                        break;
                    }
                }

                int cmp = scratch1.CompareTo(scratch2);
                if (cmp == 0)
                {
                    // tie break on whole row
                    return o1.CompareTo(o2);
                }
                else
                {
                    return cmp;
                }
            }));
            sorter.Sort(unsorted, sorted);
            // LUCENENET specific - we are using the FileOptions.DeleteOnClose FileStream option to delete the file when it is disposed.

            using (OfflineSorter.ByteSequencesReader reader = new OfflineSorter.ByteSequencesReader(sorted))
            {
                BytesRef scratchLine = new BytesRef();

                // TODO: the flags themselves can be double-chars (long) or also numeric
                // either way the trick is to encode them as char... but they must be parsed differently

                string currentEntry = null;
                Int32sRef currentOrds = new Int32sRef();

                string line2;
                while (reader.Read(scratchLine))
                {
                    line2 = scratchLine.Utf8ToString();
                    string entry;
                    char[] wordForm;
                    int end;

                    int flagSep = line2.IndexOf(FLAG_SEPARATOR);
                    if (flagSep == -1)
                    {
                        wordForm = NOFLAGS;
                        end = line2.IndexOf(MORPH_SEPARATOR);
                        entry = line2.Substring(0, end);
                    }
                    else
                    {
                        end = line2.IndexOf(MORPH_SEPARATOR);
                        string flagPart = line2.Substring(flagSep + 1, end - (flagSep + 1));
                        if (aliasCount > 0)
                        {
                            flagPart = GetAliasValue(Integer.Parse(flagPart, radix: 10)); // LUCENENET: specify radix 10 to make this culture invariant
                        }

                        wordForm = flagParsingStrategy.ParseFlags(flagPart);
                        Array.Sort(wordForm);
                        entry = line2.Substring(0, flagSep - 0);
                    }
                    // we possibly have morphological data
                    int stemExceptionID = 0;
                    if (hasStemExceptions && end + 1 < line2.Length)
                    {
                        string stemException = ParseStemException(line2.Substring(end + 1));
                        if (stemException != null)
                        {
                            if (stemExceptionCount == stemExceptions.Length)
                            {
                                int newSize = ArrayUtil.Oversize(stemExceptionCount + 1, RamUsageEstimator.NUM_BYTES_OBJECT_REF);
                                stemExceptions = Arrays.CopyOf(stemExceptions, newSize);
                            }
                            stemExceptionID = stemExceptionCount + 1; // we use '0' to indicate no exception for the form
                            stemExceptions[stemExceptionCount++] = stemException;
                        }
                    }

                    // LUCENENET NOTE: CompareToOrdinal is an extension method that works similarly to
                    // Java's String.compareTo method.
                    int cmp = currentEntry is null ? 1 : entry.CompareToOrdinal(currentEntry);
                    if (cmp < 0)
                    {
                        throw new ArgumentException("out of order: " + entry + " < " + currentEntry);
                    }
                    else
                    {
                        EncodeFlags(flagsScratch, wordForm);
                        int ord = flagLookup.Add(flagsScratch);
                        if (ord < 0)
                        {
                            // already exists in our hash
                            ord = (-ord) - 1;
                        }
                        // finalize current entry, and switch "current" if necessary
                        if (cmp > 0 && currentEntry != null)
                        {
                            Lucene.Net.Util.Fst.Util.ToUTF32(currentEntry, scratchInts);
                            words.Add(scratchInts, currentOrds);
                        }
                        // swap current
                        if (cmp > 0 || currentEntry is null)
                        {
                            currentEntry = entry;
                            currentOrds = new Int32sRef(); // must be this way
                        }
                        if (hasStemExceptions)
                        {
                            currentOrds.Grow(currentOrds.Length + 2);
                            currentOrds.Int32s[currentOrds.Length++] = ord;
                            currentOrds.Int32s[currentOrds.Length++] = stemExceptionID;
                        }
                        else
                        {
                            currentOrds.Grow(currentOrds.Length + 1);
                            currentOrds.Int32s[currentOrds.Length++] = ord;
                        }
                    }
                }

                // finalize last entry
                Lucene.Net.Util.Fst.Util.ToUTF32(currentEntry, scratchInts);
                words.Add(scratchInts, currentOrds);
            }
            // LUCENENET specific - we are using the FileOptions.DeleteOnClose FileStream option to delete the file when it is disposed.
        }

        internal static char[] DecodeFlags(BytesRef b)
        {
            if (b.Length == 0)
            {
                return CharsRef.EMPTY_CHARS;
            }
            int len = b.Length.TripleShift(1);
            char[] flags = new char[len];
            int upto = 0;
            int end = b.Offset + b.Length;
            for (int i = b.Offset; i < end; i += 2)
            {
                flags[upto++] = (char)((b.Bytes[i] << 8) | (b.Bytes[i + 1] & 0xff));
            }
            return flags;
        }

        internal static void EncodeFlags(BytesRef b, char[] flags)
        {
            int len = flags.Length << 1;
            b.Grow(len);
            b.Length = len;
            int upto = b.Offset;
            for (int i = 0; i < flags.Length; i++)
            {
                int flag = flags[i];
                b.Bytes[upto++] = (byte)((flag >> 8) & 0xff);
                b.Bytes[upto++] = (byte)(flag & 0xff);
            }
        }

        private void ParseAlias(string line)
        {
            string[] ruleArgs = whitespacePattern.Split(line).TrimEnd();
            if (aliases is null)
            {
                //first line should be the aliases count
                int count = Integer.Parse(ruleArgs[1], radix: 10); // LUCENENET: specify radix 10 to make this culture invariant
                aliases = new string[count];
            }
            else
            {
                // an alias can map to no flags
                string aliasValue = ruleArgs.Length == 1 ? "" : ruleArgs[1];
                aliases[aliasCount++] = aliasValue;
            }
        }

        private string GetAliasValue(int id)
        {
            try
            {
                return aliases[id - 1];
            }
            catch (Exception ex) when (ex.IsIndexOutOfBoundsException())
            {
                throw new ArgumentException("Bad flag alias number:" + id, ex);
            }
        }

        internal string GetStemException(int id)
        {
            return stemExceptions[id - 1];
        }

        private void ParseMorphAlias(string line)
        {
            if (morphAliases is null)
            {
                //first line should be the aliases count
                int count = Integer.Parse(line.Substring(3), radix: 10); // LUCENENET: specify radix 10 to make this culture invariant
                morphAliases = new string[count];
            }
            else
            {
                string arg = line.Substring(2); // leave the space
                morphAliases[morphAliasCount++] = arg;
            }
        }

        private string ParseStemException(string morphData)
        {
            // first see if its an alias
            if (morphAliasCount > 0)
            {
                if (int.TryParse(morphData.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int alias))
                {
                    morphData = morphAliases[alias - 1];
                } // else fine
            }
            // try to parse morph entry
            int index = morphData.IndexOf(" st:", StringComparison.Ordinal);
            if (index < 0)
            {
                index = morphData.IndexOf("\tst:", StringComparison.Ordinal);
            }
            if (index >= 0)
            {
                int endIndex = IndexOfSpaceOrTab(morphData, index + 1);
                if (endIndex < 0)
                {
                    endIndex = morphData.Length;
                }
                return morphData.Substring(index + 4, endIndex - (index + 4));
            }
            return null;
        }

        /// <summary>
        /// Abstraction of the process of parsing flags taken from the affix and dic files
        /// </summary>
        internal abstract class FlagParsingStrategy
        {
            /// <summary>
            /// Parses the given <see cref="string"/> into a single flag
            /// </summary>
            /// <param name="rawFlag"> <see cref="string"/> to parse into a flag </param>
            /// <returns> Parsed flag </returns>
            internal virtual char ParseFlag(string rawFlag)
            {
                char[] flags = ParseFlags(rawFlag);
                if (flags.Length != 1)
                {
                    throw new ArgumentException("expected only one flag, got: " + rawFlag);
                }
                return flags[0];
            }

            /// <summary>
            /// Parses the given <see cref="string"/> into multiple flags
            /// </summary>
            /// <param name="rawFlags"> <see cref="string"/> to parse into flags </param>
            /// <returns> Parsed flags </returns>
            internal abstract char[] ParseFlags(string rawFlags);
        }

        /// <summary>
        /// Simple implementation of <see cref="FlagParsingStrategy"/> that treats the chars in each <see cref="string"/> as a individual flags.
        /// Can be used with both the ASCII and UTF-8 flag types.
        /// </summary>
        private class SimpleFlagParsingStrategy : FlagParsingStrategy
        {
            internal override char[] ParseFlags(string rawFlags)
            {
                return rawFlags.ToCharArray();
            }
        }

        /// <summary>
        /// Implementation of <see cref="FlagParsingStrategy"/> that assumes each flag is encoded in its numerical form.  In the case
        /// of multiple flags, each number is separated by a comma.
        /// </summary>
        private class NumFlagParsingStrategy : FlagParsingStrategy
        {
            internal override char[] ParseFlags(string rawFlags)
            {
                string[] rawFlagParts = rawFlags.Trim().Split(',').TrimEnd();
                char[] flags = new char[rawFlagParts.Length];
                int upto = 0;

                for (int i = 0; i < rawFlagParts.Length; i++)
                {
                    // note, removing the trailing X/leading I for nepali... what is the rule here?! 
                    string replacement = leadingDigitPattern.Replace(rawFlagParts[i], "");
                    // note, ignoring empty flags (this happens in danish, for example)
                    if (replacement.Length == 0)
                    {
                        continue;
                    }
                    flags[upto++] = (char)Integer.Parse(replacement, radix: 10); // LUCENENET: specify radix 10 to make this culture invariant
                }

                if (upto < flags.Length)
                {
                    flags = Arrays.CopyOf(flags, upto);
                }
                return flags;
            }
        }

        /// <summary>
        /// Implementation of <see cref="FlagParsingStrategy"/> that assumes each flag is encoded as two ASCII characters whose codes
        /// must be combined into a single character.
        /// </summary>
        private class DoubleASCIIFlagParsingStrategy : FlagParsingStrategy
        {
            internal override char[] ParseFlags(string rawFlags)
            {
                if (rawFlags.Length == 0)
                {
                    return Arrays.Empty<char>(); // LUCENENET: Optimized char[] creation
                }

                StringBuilder builder = new StringBuilder();
                if (rawFlags.Length % 2 == 1)
                {
                    throw new ArgumentException("Invalid flags (should be even number of characters): " + rawFlags);
                }
                for (int i = 0; i < rawFlags.Length; i += 2)
                {
                    char f1 = rawFlags[i];
                    char f2 = rawFlags[i + 1];
                    if (f1 >= 256 || f2 >= 256)
                    {
                        throw new ArgumentException("Invalid flags (LONG flags must be double ASCII): " + rawFlags);
                    }
                    char combined = (char)(f1 << 8 | f2);
                    builder.Append(combined);
                }

                char[] flags = new char[builder.Length];
                builder.CopyTo(0, flags, 0, builder.Length);
                return flags;
            }
        }

        internal static bool HasFlag(char[] flags, char flag)
        {
            return Array.BinarySearch(flags, flag) >= 0;
        }

        internal virtual string CleanInput(string input, StringBuilder reuse)
        {
            reuse.Length = 0;

            for (int i = 0; i < input.Length; i++)
            {
                char ch = input[i];

                if (ignore != null && Array.BinarySearch(ignore, ch) >= 0)
                {
                    continue;
                }

                if (ignoreCase && iconv is null)
                {
                    // if we have no input conversion mappings, do this on-the-fly
                    ch = CaseFold(ch);
                }

                reuse.Append(ch);
            }

            if (iconv != null)
            {
                try
                {
                    ApplyMappings(iconv, reuse);
                }
                catch (Exception bogus) when (bogus.IsIOException())
                {
                    throw RuntimeException.Create(bogus);
                }
                if (ignoreCase)
                {
                    for (int i = 0; i < reuse.Length; i++)
                    {
                        reuse[i] = CaseFold(reuse[i]);
                    }
                }
            }

            return reuse.ToString();
        }

        // folds single character (according to LANG if present)
        internal char CaseFold(char c)
        {
            if (alternateCasing)
            {
                if (c == 'I')
                {
                    return 'ı';
                }
                else if (c == 'İ')
                {
                    return 'i';
                }
                else
                {
                    return char.ToLowerInvariant(c);
                }
            }
            else
            {
                return char.ToLowerInvariant(c);
            }
        }

        // TODO: this could be more efficient!
        internal static void ApplyMappings(FST<CharsRef> fst, StringBuilder sb)
        {
            FST.BytesReader bytesReader = fst.GetBytesReader();
            FST.Arc<CharsRef> firstArc = fst.GetFirstArc(new FST.Arc<CharsRef>());
            CharsRef NO_OUTPUT = fst.Outputs.NoOutput;

            // temporary stuff
            FST.Arc<CharsRef> arc = new FST.Arc<CharsRef>();
            int longestMatch;
            CharsRef longestOutput;

            for (int i = 0; i < sb.Length; i++)
            {
                arc.CopyFrom(firstArc);
                CharsRef output = NO_OUTPUT;
                longestMatch = -1;
                longestOutput = null;

                for (int j = i; j < sb.Length; j++)
                {
                    char ch = sb[j];
                    if (fst.FindTargetArc(ch, arc, arc, bytesReader) is null)
                    {
                        break;
                    }
                    else
                    {
                        output = fst.Outputs.Add(output, arc.Output);
                    }
                    if (arc.IsFinal)
                    {
                        longestOutput = fst.Outputs.Add(output, arc.NextFinalOutput);
                        longestMatch = j;
                    }
                }

                if (longestMatch >= 0)
                {
                    sb.Remove(i, longestMatch + 1 - i);
                    sb.Insert(i, longestOutput);
                    i += (longestOutput.Length - 1);
                }
            }
        }
    }
}