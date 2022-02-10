using J2N.Text;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Codecs.Lucene3x
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IBits = Lucene.Net.Util.IBits;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOptions = Lucene.Net.Index.IndexOptions;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;
    using Term = Lucene.Net.Index.Term;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using UnicodeUtil = Lucene.Net.Util.UnicodeUtil;

    /// <summary>
    /// Exposes flex API on a pre-flex index, as a codec.
    /// <para/>
    /// @lucene.experimental 
    /// </summary>
    [Obsolete("(4.0)")]
    internal class Lucene3xFields : FieldsProducer
    {
#pragma warning disable CA1802 // Use literals where appropriate
        private static readonly bool DEBUG_SURROGATES = false;
#pragma warning restore CA1802 // Use literals where appropriate

        public TermInfosReader Tis { get; set; }
        public TermInfosReader TisNoIndex { get; private set; }

        public IndexInput FreqStream { get; private set; }
        public IndexInput ProxStream { get; private set; }
        private readonly FieldInfos fieldInfos;
        //private readonly SegmentInfo si; // LUCENENET: Never read

        // LUCENENET specific: Use StringComparer.Ordinal to get the same ordering as Java
        internal readonly IDictionary<string, FieldInfo> fields = new JCG.SortedDictionary<string, FieldInfo>(StringComparer.Ordinal);
        internal readonly IDictionary<string, Terms> preTerms = new Dictionary<string, Terms>();
        //private readonly Directory dir; // LUCENENET: Never read
        //private readonly IOContext context; // LUCENENET: Never read
        //private Directory cfsReader; // LUCENENET NOTE: cfsReader not used

        public Lucene3xFields(Directory dir, FieldInfos fieldInfos, SegmentInfo info, IOContext context, int indexDivisor)
        {
            //si = info; // LUCENENET: Never read

            // NOTE: we must always load terms index, even for
            // "sequential" scan during merging, because what is
            // sequential to merger may not be to TermInfosReader
            // since we do the surrogates dance:
            if (indexDivisor < 0)
            {
                indexDivisor = -indexDivisor;
            }

            bool success = false;
            try
            {
                var r = new TermInfosReader(dir, info.Name, fieldInfos, context, indexDivisor);
                if (indexDivisor == -1)
                {
                    TisNoIndex = r;
                }
                else
                {
                    TisNoIndex = null;
                    Tis = r;
                }
                //this.context = context; // LUCENENET: Never read
                this.fieldInfos = fieldInfos;

                // make sure that all index files have been read or are kept open
                // so that if an index update removes them we'll still have them
                FreqStream = dir.OpenInput(IndexFileNames.SegmentFileName(info.Name, "", Lucene3xPostingsFormat.FREQ_EXTENSION), context);
                bool anyProx = false;
                foreach (FieldInfo fi in fieldInfos)
                {
                    if (fi.IsIndexed)
                    {
                        fields[fi.Name] = fi;
                        preTerms[fi.Name] = new PreTerms(this, fi);
                        if (fi.IndexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                        {
                            anyProx = true;
                        }
                    }
                }

                if (anyProx)
                {
                    ProxStream = dir.OpenInput(IndexFileNames.SegmentFileName(info.Name, "", Lucene3xPostingsFormat.PROX_EXTENSION), context);
                }
                else
                {
                    ProxStream = null;
                }
                success = true;
            }
            finally
            {
                // With lock-less commits, it's entirely possible (and
                // fine) to hit a FileNotFound exception above. In
                // this case, we want to explicitly close any subset
                // of things that were opened so that we don't have to
                // wait for a GC to do so.
                if (!success)
                {
                    Dispose();
                }
            }
            //this.dir = dir; // LUCENENET: Never read
        }

        // If this returns, we do the surrogates dance so that the
        // terms are sorted by unicode sort order.  this should be
        // true when segments are used for "normal" searching;
        // it's only false during testing, to create a pre-flex
        // index, using the test-only PreFlexRW.
        protected virtual bool SortTermsByUnicode => true;

        public override IEnumerator<string> GetEnumerator()
        {
            return fields.Keys.GetEnumerator();
        }

        public override Terms GetTerms(string field)
        {
            preTerms.TryGetValue(field, out Terms result);
            return result;
        }

        public override int Count
        {
            get
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(preTerms.Count == fields.Count);
                return fields.Count;
            }
        }

        [Obsolete("iterate fields and add their Count instead.")]
        public override long UniqueTermCount => TermsDict.Count;

        private TermInfosReader TermsDict
        {
            get
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    if (Tis != null)
                    {
                        return Tis;
                    }
                    else
                    {
                        return TisNoIndex;
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                IOUtils.Dispose(Tis, TisNoIndex, /*cfsReader,*/ FreqStream, ProxStream); // LUCENENET NOTE: cfsReader not used
            }
        }

        private class PreTerms : Terms
        {
            private readonly Lucene3xFields outerInstance;

            internal readonly FieldInfo fieldInfo;

            internal PreTerms(Lucene3xFields outerInstance, FieldInfo fieldInfo)
            {
                this.outerInstance = outerInstance;
                this.fieldInfo = fieldInfo;
            }

            public override TermsEnum GetEnumerator()
            {
                var termsEnum = new PreTermsEnum(outerInstance);
                termsEnum.Reset(fieldInfo);
                return termsEnum;
            }

            public override IComparer<BytesRef> Comparer
            {
                get
                {
                    // Pre-flex indexes always sorted in UTF16 order, but
                    // we remap on-the-fly to unicode order
                    if (outerInstance.SortTermsByUnicode)
                    {
                        return BytesRef.UTF8SortedAsUnicodeComparer;
                    }
                    else
                    {
                        return BytesRef.UTF8SortedAsUTF16Comparer;
                    }
                }
            }

            public override long Count => -1;

            public override long SumTotalTermFreq => -1;

            public override long SumDocFreq => -1;

            public override int DocCount => -1;

            // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
            public override bool HasFreqs => IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS) >= 0;

            public override bool HasOffsets
            {
                get
                {
                    // preflex doesn't support this
                    // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                    if (Debugging.AssertsEnabled) Debugging.Assert(IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) < 0);
                    return false;
                }
            }

            // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
            public override bool HasPositions => IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;

            public override bool HasPayloads => fieldInfo.HasPayloads;
        }

        private class PreTermsEnum : TermsEnum
        {
            private readonly Lucene3xFields outerInstance;

            public PreTermsEnum(Lucene3xFields outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            private SegmentTermEnum termEnum;
            private FieldInfo fieldInfo;
            private string internedFieldName;
            private bool skipNext;
            private BytesRef current;

            private SegmentTermEnum seekTermEnum;

            private const sbyte UTF8_NON_BMP_LEAD = unchecked((sbyte)0xf0);
            private const sbyte UTF8_HIGH_BMP_LEAD = unchecked((sbyte)0xee);

            // Returns true if the unicode char is "after" the
            // surrogates in UTF16, ie >= U+E000 and <= U+FFFF:
            private static bool IsHighBMPChar(byte[] b, int idx)
            {
                return (((sbyte)b[idx]) & UTF8_HIGH_BMP_LEAD) == UTF8_HIGH_BMP_LEAD;
            }

            // Returns true if the unicode char in the UTF8 byte
            // sequence starting at idx encodes a char outside of
            // BMP (ie what would be a surrogate pair in UTF16):
            private static bool IsNonBMPChar(byte[] b, int idx)
            {
                return (((sbyte)b[idx]) & UTF8_NON_BMP_LEAD) == UTF8_NON_BMP_LEAD;
            }

            private readonly sbyte[] scratch = new sbyte[4];
            private readonly BytesRef prevTerm = new BytesRef();
            private readonly BytesRef scratchTerm = new BytesRef();
            private int newSuffixStart;

            // Swap in S, in place of E:
            private bool SeekToNonBMP(SegmentTermEnum te, BytesRef term, int pos)
            {
                int savLength = term.Length;

                if (Debugging.AssertsEnabled) Debugging.Assert(term.Offset == 0);

                // The 3 bytes starting at downTo make up 1
                // unicode character:
                if (Debugging.AssertsEnabled) Debugging.Assert(IsHighBMPChar(term.Bytes, pos));

                // NOTE: we cannot make this assert, because
                // AutomatonQuery legitimately sends us malformed UTF8
                // (eg the UTF8 bytes with just 0xee)
                // assert term.length >= pos + 3: "term.length=" + term.length + " pos+3=" + (pos+3) + " byte=" + Integer.toHexString(term.bytes[pos]) + " term=" + term.toString();

                // Save the bytes && length, since we need to
                // restore this if seek "back" finds no matching
                // terms
                if (term.Bytes.Length < 4 + pos)
                {
                    term.Grow(4 + pos);
                }

                scratch[0] = (sbyte)term.Bytes[pos];
                scratch[1] = (sbyte)term.Bytes[pos + 1];
                scratch[2] = (sbyte)term.Bytes[pos + 2];

                term.Bytes[pos] = 0xf0;
                term.Bytes[pos + 1] = 0x90;
                term.Bytes[pos + 2] = 0x80;
                term.Bytes[pos + 3] = 0x80;
                term.Length = 4 + pos;

                if (DEBUG_SURROGATES)
                {
                    Console.WriteLine("      try seek term=" + UnicodeUtil.ToHexString(term.Utf8ToString()));
                }

                // Seek "back":
                outerInstance.TermsDict.SeekEnum(te, new Term(fieldInfo.Name, term), true);

                // Test if the term we seek'd to in fact found a
                // surrogate pair at the same position as the E:
                Term t2 = te.Term();

                // Cannot be null (or move to next field) because at
                // "worst" it'd seek to the same term we are on now,
                // unless we are being called from seek
                if (t2 is null || t2.Field != internedFieldName)
                {
                    return false;
                }

                if (DEBUG_SURROGATES)
                {
                    Console.WriteLine("      got term=" + UnicodeUtil.ToHexString(t2.Text));
                }

                // Now test if prefix is identical and we found
                // a non-BMP char at the same position:
                BytesRef b2 = t2.Bytes;
                if (Debugging.AssertsEnabled) Debugging.Assert(b2.Offset == 0);

                bool matches;
                if (b2.Length >= term.Length && IsNonBMPChar(b2.Bytes, pos))
                {
                    matches = true;
                    for (int i = 0; i < pos; i++)
                    {
                        if (term.Bytes[i] != b2.Bytes[i])
                        {
                            matches = false;
                            break;
                        }
                    }
                }
                else
                {
                    matches = false;
                }

                // Restore term:
                term.Length = savLength;
                term.Bytes[pos] = (byte)scratch[0];
                term.Bytes[pos + 1] = (byte)scratch[1];
                term.Bytes[pos + 2] = (byte)scratch[2];

                return matches;
            }

            // Seek type 2 "continue" (back to the start of the
            // surrogates): scan the stripped suffix from the
            // prior term, backwards. If there was an E in that
            // part, then we try to seek back to S.  If that
            // seek finds a matching term, we go there.
            private bool DoContinue()
            {
                if (DEBUG_SURROGATES)
                {
                    Console.WriteLine("  try cont");
                }

                int downTo = prevTerm.Length - 1;

                bool didSeek = false;

                int limit = Math.Min(newSuffixStart, scratchTerm.Length - 1);

                while (downTo > limit)
                {
                    if (IsHighBMPChar(prevTerm.Bytes, downTo))
                    {
                        if (DEBUG_SURROGATES)
                        {
                            Console.WriteLine("    found E pos=" + downTo + " vs len=" + prevTerm.Length);
                        }

                        if (SeekToNonBMP(seekTermEnum, prevTerm, downTo))
                        {
                            // TODO: more efficient seek?
                            outerInstance.TermsDict.SeekEnum(termEnum, seekTermEnum.Term(), true);
                            //newSuffixStart = downTo+4;
                            newSuffixStart = downTo;
                            scratchTerm.CopyBytes(termEnum.Term().Bytes);
                            didSeek = true;
                            if (DEBUG_SURROGATES)
                            {
                                Console.WriteLine("      seek!");
                            }
                            break;
                        }
                        else
                        {
                            if (DEBUG_SURROGATES)
                            {
                                Console.WriteLine("      no seek");
                            }
                        }
                    }

                    // Shorten prevTerm in place so that we don't redo
                    // this loop if we come back here:
                    if ((prevTerm.Bytes[downTo] & 0xc0) == 0xc0 || (prevTerm.Bytes[downTo] & 0x80) == 0)
                    {
                        prevTerm.Length = downTo;
                    }

                    downTo--;
                }

                return didSeek;
            }

            // Look for seek type 3 ("pop"): if the delta from
            // prev -> current was replacing an S with an E,
            // we must now seek to beyond that E.  this seek
            // "finishes" the dance at this character
            // position.
            private bool DoPop()
            {
                if (DEBUG_SURROGATES)
                {
                    Console.WriteLine("  try pop");
                }

                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(newSuffixStart <= prevTerm.Length);
                    Debugging.Assert(newSuffixStart < scratchTerm.Length || newSuffixStart == 0);
                }

                if (prevTerm.Length > newSuffixStart && IsNonBMPChar(prevTerm.Bytes, newSuffixStart) && IsHighBMPChar(scratchTerm.Bytes, newSuffixStart))
                {
                    // Seek type 2 -- put 0xFF at this position:
                    scratchTerm.Bytes[newSuffixStart] = 0xff;
                    scratchTerm.Length = newSuffixStart + 1;

                    if (DEBUG_SURROGATES)
                    {
                        Console.WriteLine("    seek to term=" + UnicodeUtil.ToHexString(scratchTerm.Utf8ToString()) + " " + scratchTerm.ToString());
                    }

                    // TODO: more efficient seek?  can we simply swap
                    // the enums?
                    outerInstance.TermsDict.SeekEnum(termEnum, new Term(fieldInfo.Name, scratchTerm), true);

                    Term t2 = termEnum.Term();

                    // We could hit EOF or different field since this
                    // was a seek "forward":
                    if (t2 != null && t2.Field == internedFieldName)
                    {
                        if (DEBUG_SURROGATES)
                        {
                            Console.WriteLine("      got term=" + UnicodeUtil.ToHexString(t2.Text) + " " + t2.Bytes);
                        }

                        BytesRef b2 = t2.Bytes;
                        if (Debugging.AssertsEnabled) Debugging.Assert(b2.Offset == 0);

                        // Set newSuffixStart -- we can't use
                        // termEnum's since the above seek may have
                        // done no scanning (eg, term was precisely
                        // and index term, or, was in the term seek
                        // cache):
                        scratchTerm.CopyBytes(b2);
                        SetNewSuffixStart(prevTerm, scratchTerm);

                        return true;
                    }
                    else if (newSuffixStart != 0 || scratchTerm.Length != 0)
                    {
                        if (DEBUG_SURROGATES)
                        {
                            Console.WriteLine("      got term=null (or next field)");
                        }
                        newSuffixStart = 0;
                        scratchTerm.Length = 0;
                        return true;
                    }
                }

                return false;
            }

            // Pre-flex indices store terms in UTF16 sort order, but
            // certain queries require Unicode codepoint order; this
            // method carefully seeks around surrogates to handle
            // this impedance mismatch

            private void SurrogateDance()
            {
                if (!unicodeSortOrder)
                {
                    return;
                }

                // We are invoked after TIS.next() (by UTF16 order) to
                // possibly seek to a different "next" (by unicode
                // order) term.

                // We scan only the "delta" from the last term to the
                // current term, in UTF8 bytes.  We look at 1) the bytes
                // stripped from the prior term, and then 2) the bytes
                // appended to that prior term's prefix.

                // We don't care about specific UTF8 sequences, just
                // the "category" of the UTF16 character.  Category S
                // is a high/low surrogate pair (it non-BMP).
                // Category E is any BMP char > UNI_SUR_LOW_END (and <
                // U+FFFF). Category A is the rest (any unicode char
                // <= UNI_SUR_HIGH_START).

                // The core issue is that pre-flex indices sort the
                // characters as ASE, while flex must sort as AES.  So
                // when scanning, when we hit S, we must 1) seek
                // forward to E and enum the terms there, then 2) seek
                // back to S and enum all terms there, then 3) seek to
                // after E.  Three different seek points (1, 2, 3).

                // We can easily detect S in UTF8: if a byte has
                // prefix 11110 (0xf0), then that byte and the
                // following 3 bytes encode a single unicode codepoint
                // in S.  Similarly, we can detect E: if a byte has
                // prefix 1110111 (0xee), then that byte and the
                // following 2 bytes encode a single unicode codepoint
                // in E.

                // Note that this is really a recursive process --
                // maybe the char at pos 2 needs to dance, but any
                // point in its dance, suddenly pos 4 needs to dance
                // so you must finish pos 4 before returning to pos
                // 2.  But then during pos 4's dance maybe pos 7 needs
                // to dance, etc.  However, despite being recursive,
                // we don't need to hold any state because the state
                // can always be derived by looking at prior term &
                // current term.

                // TODO: can we avoid this copy?
                if (termEnum.Term() is null || termEnum.Term().Field != internedFieldName)
                {
                    scratchTerm.Length = 0;
                }
                else
                {
                    scratchTerm.CopyBytes(termEnum.Term().Bytes);
                }

                if (DEBUG_SURROGATES)
                {
                    Console.WriteLine("  dance");
                    Console.WriteLine("    prev=" + UnicodeUtil.ToHexString(prevTerm.Utf8ToString()));
                    Console.WriteLine("         " + prevTerm.ToString());
                    Console.WriteLine("    term=" + UnicodeUtil.ToHexString(scratchTerm.Utf8ToString()));
                    Console.WriteLine("         " + scratchTerm.ToString());
                }

                // this code assumes TermInfosReader/SegmentTermEnum
                // always use BytesRef.offset == 0
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(prevTerm.Offset == 0);
                    Debugging.Assert(scratchTerm.Offset == 0);
                }

                // Need to loop here because we may need to do multiple
                // pops, and possibly a continue in the end, ie:
                //
                //  cont
                //  pop, cont
                //  pop, pop, cont
                //  <nothing>
                //

                while (true)
                {
                    if (DoContinue())
                    {
                        break;
                    }
                    else
                    {
                        if (!DoPop())
                        {
                            break;
                        }
                    }
                }

                if (DEBUG_SURROGATES)
                {
                    Console.WriteLine("  finish bmp ends");
                }

                DoPushes();
            }

            // Look for seek type 1 ("push"): if the newly added
            // suffix contains any S, we must try to seek to the
            // corresponding E.  If we find a match, we go there;
            // else we keep looking for additional S's in the new
            // suffix.  this "starts" the dance, at this character
            // position:
            private void DoPushes()
            {
                int upTo = newSuffixStart;
                if (DEBUG_SURROGATES)
                {
                    Console.WriteLine("  try push newSuffixStart=" + newSuffixStart + " scratchLen=" + scratchTerm.Length);
                }

                while (upTo < scratchTerm.Length)
                {
                    if (IsNonBMPChar(scratchTerm.Bytes, upTo) && (upTo > newSuffixStart || (upTo >= prevTerm.Length || (!IsNonBMPChar(prevTerm.Bytes, upTo) && !IsHighBMPChar(prevTerm.Bytes, upTo)))))
                    {
                        // A non-BMP char (4 bytes UTF8) starts here:
                        if (Debugging.AssertsEnabled) Debugging.Assert(scratchTerm.Length >= upTo + 4);

                        int savLength = scratchTerm.Length;
                        scratch[0] = (sbyte)scratchTerm.Bytes[upTo];
                        scratch[1] = (sbyte)scratchTerm.Bytes[upTo + 1];
                        scratch[2] = (sbyte)scratchTerm.Bytes[upTo + 2];

                        scratchTerm.Bytes[upTo] = unchecked((byte)UTF8_HIGH_BMP_LEAD);
                        scratchTerm.Bytes[upTo + 1] = 0x80;
                        scratchTerm.Bytes[upTo + 2] = 0x80;
                        scratchTerm.Length = upTo + 3;

                        if (DEBUG_SURROGATES)
                        {
                            Console.WriteLine("    try seek 1 pos=" + upTo + " term=" + UnicodeUtil.ToHexString(scratchTerm.Utf8ToString()) + " " + scratchTerm.ToString() + " len=" + scratchTerm.Length);
                        }

                        // Seek "forward":
                        // TODO: more efficient seek?
                        outerInstance.TermsDict.SeekEnum(seekTermEnum, new Term(fieldInfo.Name, scratchTerm), true);

                        scratchTerm.Bytes[upTo] = (byte)scratch[0];
                        scratchTerm.Bytes[upTo + 1] = (byte)scratch[1];
                        scratchTerm.Bytes[upTo + 2] = (byte)scratch[2];
                        scratchTerm.Length = savLength;

                        // Did we find a match?
                        Term t2 = seekTermEnum.Term();

                        if (DEBUG_SURROGATES)
                        {
                            if (t2 is null)
                            {
                                Console.WriteLine("      hit term=null");
                            }
                            else
                            {
                                Console.WriteLine($"      hit term={UnicodeUtil.ToHexString(t2.Text)} {t2?.Bytes}");
                            }
                        }

                        // Since this was a seek "forward", we could hit
                        // EOF or a different field:
                        bool matches;

                        if (t2 != null && t2.Field == internedFieldName)
                        {
                            BytesRef b2 = t2.Bytes;
                            if (Debugging.AssertsEnabled) Debugging.Assert(b2.Offset == 0);
                            if (b2.Length >= upTo + 3 && IsHighBMPChar(b2.Bytes, upTo))
                            {
                                matches = true;
                                for (int i = 0; i < upTo; i++)
                                {
                                    if (scratchTerm.Bytes[i] != b2.Bytes[i])
                                    {
                                        matches = false;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                matches = false;
                            }
                        }
                        else
                        {
                            matches = false;
                        }

                        if (matches)
                        {
                            if (DEBUG_SURROGATES)
                            {
                                Console.WriteLine("      matches!");
                            }

                            // OK seek "back"
                            // TODO: more efficient seek?
                            outerInstance.TermsDict.SeekEnum(termEnum, seekTermEnum.Term(), true);

                            scratchTerm.CopyBytes(seekTermEnum.Term().Bytes);

                            // +3 because we don't need to check the char
                            // at upTo: we know it's > BMP
                            upTo += 3;

                            // NOTE: we keep iterating, now, since this
                            // can easily "recurse".  Ie, after seeking
                            // forward at a certain char position, we may
                            // find another surrogate in our [new] suffix
                            // and must then do another seek (recurse)
                        }
                        else
                        {
                            upTo++;
                        }
                    }
                    else
                    {
                        upTo++;
                    }
                }
            }

            private bool unicodeSortOrder;

            internal virtual void Reset(FieldInfo fieldInfo)
            {
                //System.out.println("pff.reset te=" + termEnum);
                this.fieldInfo = fieldInfo;
                
                internedFieldName = fieldInfo.Name.Intern();

                Term term = new Term(internedFieldName);
                if (termEnum is null)
                {
                    termEnum = outerInstance.TermsDict.Terms(term);
                    seekTermEnum = outerInstance.TermsDict.Terms(term);
                    //System.out.println("  term=" + termEnum.term());
                }
                else
                {
                    outerInstance.TermsDict.SeekEnum(termEnum, term, true);
                }
                skipNext = true;

                unicodeSortOrder = outerInstance.SortTermsByUnicode;

                Term t = termEnum.Term();
                if (t != null && t.Field == internedFieldName)
                {
                    newSuffixStart = 0;
                    prevTerm.Length = 0;
                    SurrogateDance();
                }
            }

            public override IComparer<BytesRef> Comparer
            {
                get
                {
                    // Pre-flex indexes always sorted in UTF16 order, but
                    // we remap on-the-fly to unicode order
                    if (unicodeSortOrder)
                    {
                        return BytesRef.UTF8SortedAsUnicodeComparer;
                    }
                    else
                    {
                        return BytesRef.UTF8SortedAsUTF16Comparer;
                    }
                }
            }

            public override void SeekExact(long ord)
            {
                throw UnsupportedOperationException.Create();
            }

            public override long Ord => throw UnsupportedOperationException.Create();

            public override SeekStatus SeekCeil(BytesRef term)
            {
                if (DEBUG_SURROGATES)
                {
                    Console.WriteLine("TE.seek target=" + UnicodeUtil.ToHexString(term.Utf8ToString()));
                }
                skipNext = false;
                TermInfosReader tis = outerInstance.TermsDict;
                Term t0 = new Term(fieldInfo.Name, term);

                if (Debugging.AssertsEnabled) Debugging.Assert(termEnum != null);

                tis.SeekEnum(termEnum, t0, false);

                Term t = termEnum.Term();

                if (t != null && t.Field == internedFieldName && term.BytesEquals(t.Bytes))
                {
                    // If we found an exact match, no need to do the
                    // surrogate dance
                    if (DEBUG_SURROGATES)
                    {
                        Console.WriteLine("  seek exact match");
                    }
                    current = t.Bytes;
                    return SeekStatus.FOUND;
                }
                else if (t is null || t.Field != internedFieldName)
                {
                    // TODO: maybe we can handle this like the next()
                    // into null?  set term as prevTerm then dance?

                    if (DEBUG_SURROGATES)
                    {
                        Console.WriteLine("  seek hit EOF");
                    }

                    // We hit EOF; try end-case surrogate dance: if we
                    // find an E, try swapping in S, backwards:
                    scratchTerm.CopyBytes(term);

                    if (Debugging.AssertsEnabled) Debugging.Assert(scratchTerm.Offset == 0);

                    for (int i = scratchTerm.Length - 1; i >= 0; i--)
                    {
                        if (IsHighBMPChar(scratchTerm.Bytes, i))
                        {
                            if (DEBUG_SURROGATES)
                            {
                                Console.WriteLine("    found E pos=" + i + "; try seek");
                            }

                            if (SeekToNonBMP(seekTermEnum, scratchTerm, i))
                            {
                                scratchTerm.CopyBytes(seekTermEnum.Term().Bytes);
                                outerInstance.TermsDict.SeekEnum(termEnum, seekTermEnum.Term(), false);

                                newSuffixStart = 1 + i;

                                DoPushes();

                                // Found a match
                                // TODO: faster seek?
                                current = termEnum.Term().Bytes;
                                return SeekStatus.NOT_FOUND;
                            }
                        }
                    }

                    if (DEBUG_SURROGATES)
                    {
                        Console.WriteLine("  seek END");
                    }

                    current = null;
                    return SeekStatus.END;
                }
                else
                {
                    // We found a non-exact but non-null term; this one
                    // is fun -- just treat it like next, by pretending
                    // requested term was prev:
                    prevTerm.CopyBytes(term);

                    if (DEBUG_SURROGATES)
                    {
                        Console.WriteLine("  seek hit non-exact term=" + UnicodeUtil.ToHexString(t.Text));
                    }

                    BytesRef br = t.Bytes;
                    if (Debugging.AssertsEnabled) Debugging.Assert(br.Offset == 0);

                    SetNewSuffixStart(term, br);

                    SurrogateDance();

                    Term t2 = termEnum.Term();
                    if (t2 is null || t2.Field != internedFieldName)
                    {
                        // PreFlex codec interns field names; verify:
                        if (Debugging.AssertsEnabled) Debugging.Assert(t2 is null || !t2.Field.Equals(internedFieldName, StringComparison.Ordinal));
                        current = null;
                        return SeekStatus.END;
                    }
                    else
                    {
                        current = t2.Bytes;
                        if (Debugging.AssertsEnabled) Debugging.Assert(!unicodeSortOrder || term.CompareTo(current) < 0,"term={0} vs current={1}",
                            // LUCENENET specific - use wrapper BytesRefFormatter struct to defer building the string unless string.Format() is called
                            new BytesRefFormatter(term, BytesRefFormat.UTF8AsHex), new BytesRefFormatter(current, BytesRefFormat.UTF8AsHex));
                        return SeekStatus.NOT_FOUND;
                    }
                }
            }

            private void SetNewSuffixStart(BytesRef br1, BytesRef br2)
            {
                int limit = Math.Min(br1.Length, br2.Length);
                int lastStart = 0;
                for (int i = 0; i < limit; i++)
                {
                    if ((br1.Bytes[br1.Offset + i] & 0xc0) == 0xc0 || (br1.Bytes[br1.Offset + i] & 0x80) == 0)
                    {
                        lastStart = i;
                    }
                    if (br1.Bytes[br1.Offset + i] != br2.Bytes[br2.Offset + i])
                    {
                        newSuffixStart = lastStart;
                        if (DEBUG_SURROGATES)
                        {
                            Console.WriteLine("    set newSuffixStart=" + newSuffixStart);
                        }
                        return;
                    }
                }
                newSuffixStart = limit;
                if (DEBUG_SURROGATES)
                {
                    Console.WriteLine("    set newSuffixStart=" + newSuffixStart);
                }
            }

            public override bool MoveNext()
            {
                if (DEBUG_SURROGATES)
                {
                    Console.WriteLine("TE.MoveNext()");
                }
                if (skipNext)
                {
                    if (DEBUG_SURROGATES)
                    {
                        Console.WriteLine("  skipNext=true");
                    }
                    skipNext = false;
                    if (termEnum.Term() is null)
                    {
                        return false;
                        // PreFlex codec interns field names:
                    }
                    else if (termEnum.Term().Field != internedFieldName)
                    {
                        return false;
                    }
                    else
                    {
                        current = termEnum.Term().Bytes;
                        return current != null;
                    }
                }

                // TODO: can we use STE's prevBuffer here?
                prevTerm.CopyBytes(termEnum.Term().Bytes);

                if (termEnum.Next() && termEnum.Term().Field == internedFieldName)
                {
                    newSuffixStart = termEnum.newSuffixStart;
                    if (DEBUG_SURROGATES)
                    {
                        Console.WriteLine("  newSuffixStart=" + newSuffixStart);
                    }
                    SurrogateDance();
                    Term t = termEnum.Term();
                    if (t is null || t.Field != internedFieldName)
                    {
                        // PreFlex codec interns field names; verify:
                        if (Debugging.AssertsEnabled) Debugging.Assert(t is null || !t.Field.Equals(internedFieldName, StringComparison.Ordinal));
                        current = null;
                        return false;
                    }
                    else
                    {
                        current = t.Bytes;
                        return true;
                    }
                }
                else
                {
                    // this field is exhausted, but we have to give
                    // surrogateDance a chance to seek back:
                    if (DEBUG_SURROGATES)
                    {
                        Console.WriteLine("  force cont");
                    }
                    //newSuffixStart = prevTerm.length;
                    newSuffixStart = 0;
                    SurrogateDance();

                    Term t = termEnum.Term();
                    if (t is null || t.Field != internedFieldName)
                    {
                        // PreFlex codec interns field names; verify:
                        if (Debugging.AssertsEnabled) Debugging.Assert(t is null || !t.Field.Equals(internedFieldName, StringComparison.Ordinal));
                        return false;
                    }
                    else
                    {
                        current = t.Bytes;
                        return true;
                    }
                }
            }

            [Obsolete("Use MoveNext() and Term instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public override BytesRef Next()
            {
                if (MoveNext())
                    return current;
                return null;
            }

            public override BytesRef Term => current;

            public override int DocFreq => termEnum.DocFreq;

            public override long TotalTermFreq => -1;

            public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, DocsFlags flags)
            {
                if (reuse is null || !(reuse is PreDocsEnum docsEnum) || docsEnum.FreqStream != outerInstance.FreqStream)
                    docsEnum = new PreDocsEnum(outerInstance);

                return docsEnum.Reset(termEnum, liveDocs);
            }

            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags)
            {
                if (fieldInfo.IndexOptions != IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                    return null;

                if (reuse is null || !(reuse is PreDocsAndPositionsEnum docsPosEnum) || docsPosEnum.FreqStream != outerInstance.FreqStream)
                    docsPosEnum = new PreDocsAndPositionsEnum(outerInstance);

                return docsPosEnum.Reset(termEnum, liveDocs);
            }
        }

        private sealed class PreDocsEnum : DocsEnum
        {
            private readonly Lucene3xFields outerInstance;

            internal readonly SegmentTermDocs docs;
            private int docID = -1;

            internal PreDocsEnum(Lucene3xFields outerInstance)
            {
                this.outerInstance = outerInstance;
                docs = new SegmentTermDocs(outerInstance.FreqStream, outerInstance.TermsDict, outerInstance.fieldInfos);
            }

            internal IndexInput FreqStream => outerInstance.FreqStream;

            public PreDocsEnum Reset(SegmentTermEnum termEnum, IBits liveDocs)
            {
                docs.LiveDocs = liveDocs;
                docs.Seek(termEnum);
                docs.freq = 1;
                docID = -1;
                return this;
            }

            public override int NextDoc()
            {
                if (docs.Next())
                {
                    return docID = docs.Doc;
                }
                else
                {
                    return docID = NO_MORE_DOCS;
                }
            }

            public override int Advance(int target)
            {
                if (docs.SkipTo(target))
                {
                    return docID = docs.Doc;
                }
                else
                {
                    return docID = NO_MORE_DOCS;
                }
            }

            public override int Freq => docs.Freq;

            public override int DocID => docID;

            public override long GetCost()
            {
                return docs.m_df;
            }
        }

        private sealed class PreDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private readonly Lucene3xFields outerInstance;

            private readonly SegmentTermPositions pos;
            private int docID = -1;

            internal PreDocsAndPositionsEnum(Lucene3xFields outerInstance)
            {
                this.outerInstance = outerInstance;
                pos = new SegmentTermPositions(outerInstance.FreqStream, outerInstance.ProxStream, outerInstance.TermsDict, outerInstance.fieldInfos);
            }

            internal IndexInput FreqStream => outerInstance.FreqStream;

            public DocsAndPositionsEnum Reset(SegmentTermEnum termEnum, IBits liveDocs)
            {
                pos.LiveDocs = liveDocs;
                pos.Seek(termEnum);
                docID = -1;
                return this;
            }

            public override int NextDoc()
            {
                if (pos.Next())
                {
                    return docID = pos.Doc;
                }
                else
                {
                    return docID = NO_MORE_DOCS;
                }
            }

            public override int Advance(int target)
            {
                if (pos.SkipTo(target))
                {
                    return docID = pos.Doc;
                }
                else
                {
                    return docID = NO_MORE_DOCS;
                }
            }

            public override int Freq => pos.Freq;

            public override int DocID => docID;

            public override int NextPosition()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(docID != NO_MORE_DOCS);
                return pos.NextPosition();
            }

            public override int StartOffset => -1;

            public override int EndOffset => -1;

            public override BytesRef GetPayload()
            {
                return pos.GetPayload();
            }

            public override long GetCost()
            {
                return pos.m_df;
            }
        }

        public override long RamBytesUsed()
        {
            if (Tis != null)
            {
                return Tis.RamBytesUsed();
            }
            else
            {
                // when there is no index, there is almost nothing loaded into RAM
                return 0L;
            }
        }

        public override void CheckIntegrity()
        {
        }
    }
}